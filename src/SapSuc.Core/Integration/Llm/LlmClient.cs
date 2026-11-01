using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace SuccessFactorsLike.Integration.Llm;

public sealed class LlmOptions
{
    public string Endpoint { get; set; } = "";
    public string ApiKey { get; set; } = "";
    public string Model { get; set; } = "";
    public int AttemptTimeoutMs { get; set; } = 10000;
    public int TotalTimeoutMs { get; set; } = 30000;
    public int MaxRetries { get; set; } = 2;
    public int BaseDelayMs { get; set; } = 500;
    public int MaxDelayMs { get; set; } = 5000;

    public void Validate()
    {
        if (!Uri.TryCreate(Endpoint, UriKind.Absolute, out var uri) || uri.Scheme != "https" ||
            string.IsNullOrWhiteSpace(ApiKey) || string.IsNullOrWhiteSpace(Model) ||
            AttemptTimeoutMs is < 1 or > 120000 || TotalTimeoutMs is < 1 or > 300000 ||
            MaxRetries is < 0 or > 5 || BaseDelayMs < 1 || MaxDelayMs < BaseDelayMs || MaxDelayMs > 60000)
            throw new ArgumentException("Invalid Llm configuration; configure an HTTPS endpoint, credentials, model and bounded timeouts/retries.");
    }
}

public sealed class LlmException(string code, int status, bool retryable = false, TimeSpan? retryAfter = null)
    : Exception(code)
{
    public string Code { get; } = code;
    public int Status { get; } = status;
    public bool Retryable { get; } = retryable;
    public TimeSpan? RetryAfter { get; } = retryAfter;
}

// Uses the chat-completions wire format. No retries of HR mutations or tool calls.
public sealed class LlmClient(HttpClient http, LlmOptions options)
{
    public async Task<string> CompleteAsync(string prompt, CancellationToken cancellationToken = default)
    {
        options.Validate();
        using var total = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        total.CancelAfter(options.TotalTimeoutMs);
        try
        {
            for (var attempt = 0; ; attempt++)
            {
                LlmException failure;
                using var timeout = CancellationTokenSource.CreateLinkedTokenSource(total.Token);
                timeout.CancelAfter(options.AttemptTimeoutMs);
                try
                {
                    using var request = new HttpRequestMessage(HttpMethod.Post, options.Endpoint);
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", options.ApiKey);
                    request.Content = JsonContent.Create(new
                    {
                        model = options.Model,
                        messages = new[] {
                            new { role = "system", content = "Provide general HR guidance only. Do not make employment decisions or claim to change HR records." },
                            new { role = "user", content = prompt }
                        },
                        max_tokens = 500
                    });
                    using var response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeout.Token);
                    if (response.IsSuccessStatusCode)
                        return await ReadAnswerAsync(response, timeout.Token);

                    var retryAfter = response.Headers.RetryAfter;
                    var delay = retryAfter?.Delta ?? (retryAfter?.Date - DateTimeOffset.UtcNow);
                    if (delay < TimeSpan.Zero) delay = TimeSpan.Zero;
                    failure = response.StatusCode switch
                    {
                        HttpStatusCode.TooManyRequests => new("llm_rate_limited", 429, true, delay),
                        HttpStatusCode.RequestTimeout => new("llm_timeout", 504, true, delay),
                        HttpStatusCode.InternalServerError or HttpStatusCode.BadGateway or
                        HttpStatusCode.ServiceUnavailable or HttpStatusCode.GatewayTimeout => new("llm_unavailable", 503, true, delay),
                        HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden => new("llm_configuration_error", 503),
                        _ => new("llm_request_rejected", 502)
                    };
                }
                catch (OperationCanceledException) when (!total.IsCancellationRequested)
                {
                    failure = new("llm_timeout", 504, true);
                }
                catch (HttpRequestException)
                {
                    failure = new("llm_network_error", 503, true);
                }
                if (!failure.Retryable || attempt >= options.MaxRetries) throw failure;
                // Never shorten a provider's Retry-After. If too long, return control to the caller.
                if (failure.RetryAfter > TimeSpan.FromMilliseconds(options.MaxDelayMs)) throw failure;
                var backoff = Math.Min(options.MaxDelayMs, options.BaseDelayMs * Math.Pow(2, attempt));
                var wait = failure.RetryAfter ?? TimeSpan.FromMilliseconds(Random.Shared.NextDouble() * backoff);
                await Task.Delay(wait, total.Token);
            }
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new LlmException("llm_timeout", 504, true);
        }
    }

    private static async Task<string> ReadAnswerAsync(HttpResponseMessage response, CancellationToken token)
    {
        try
        {
            // Bound provider output before parsing, including chunked responses.
            using var stream = await response.Content.ReadAsStreamAsync(token);
            using var buffer = new MemoryStream();
            var bytes = new byte[8192];
            int read;
            while ((read = await stream.ReadAsync(bytes, token)) > 0)
            {
                if (buffer.Length + read > 65536) throw new LlmException("llm_invalid_response", 502);
                buffer.Write(bytes, 0, read);
            }
            using var json = JsonDocument.Parse(buffer.ToArray());
            var choice = json.RootElement.GetProperty("choices")[0];
            var message = choice.GetProperty("message");
            if (message.TryGetProperty("refusal", out var refusal) && refusal.ValueKind != JsonValueKind.Null)
                throw new LlmException("llm_refusal", 422);
            if (choice.GetProperty("finish_reason").GetString() != "stop")
                throw new LlmException("llm_incomplete_response", 502);
            var content = message.GetProperty("content").GetString();
            if (string.IsNullOrWhiteSpace(content)) throw new LlmException("llm_invalid_response", 502);
            return content;
        }
        catch (Exception ex) when (ex is JsonException or KeyNotFoundException or InvalidOperationException or IndexOutOfRangeException)
        {
            throw new LlmException("llm_invalid_response", 502);
        }
    }
}
