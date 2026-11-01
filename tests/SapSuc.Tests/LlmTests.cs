using System.Net;
using SuccessFactorsLike.Integration.Llm;

internal static class LlmTests
{
    private const string Success = "{\"choices\":[{\"finish_reason\":\"stop\",\"message\":{\"content\":\"guidance\"}}]}";
    public static void Run() => RunAsync().GetAwaiter().GetResult();

    private static async Task RunAsync()
    {
        foreach (var status in new[] { 429, 500, 502, 503, 504, 408 })
        {
            var handler = new FakeHandler((n, _) => Task.FromResult(Response(n == 1 ? status : 200)));
            var answer = await Client(handler).CompleteAsync("help");
            Check(answer == "guidance" && handler.Calls == 2, $"Retry {status}");
        }
        foreach (var status in new[] { 400, 401, 403, 404, 422 })
        {
            var handler = new FakeHandler((_, _) => Task.FromResult(Response(status)));
            await Error(Client(handler), status is 401 or 403 ? "llm_configuration_error" : "llm_request_rejected");
            Check(handler.Calls == 1, $"Do not retry {status}");
        }
        var exhausted = new FakeHandler((_, _) => Task.FromResult(Response(503)));
        await Error(Client(exhausted), "llm_unavailable");
        Check(exhausted.Calls == 3, "Bound retries");

        var longWait = new FakeHandler((_, _) =>
        {
            var response = Response(429);
            response.Headers.RetryAfter = new System.Net.Http.Headers.RetryConditionHeaderValue(TimeSpan.FromHours(1));
            return Task.FromResult(response);
        });
        await Error(Client(longWait), "llm_rate_limited");
        Check(longWait.Calls == 1, "Do not shorten Retry-After");

        foreach (var body in new[] { "broken", "{}", "{\"choices\":[]}", Success.Replace("stop", "length") })
        {
            var invalid = new FakeHandler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(body) }));
            await Error(Client(invalid), body.Contains("length") ? "llm_incomplete_response" : "llm_invalid_response");
            Check(invalid.Calls == 1, "Do not retry malformed output");
        }
        var network = new FakeHandler((n, _) => n == 1 ? throw new HttpRequestException() : Task.FromResult(Response(200)));
        Check(await Client(network).CompleteAsync("help") == "guidance", "Network recovery");

        var slow = new FakeHandler(async (_, token) => { await Task.Delay(1000, token); return Response(200); });
        var slowClient = Client(slow, timeout: 20);
        await Error(slowClient, "llm_timeout");
        Check(slow.Calls <= 3, "Timeout bound");
        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();
        try { await Client(slow).CompleteAsync("help", cancelled.Token); throw new Exception("Cancellation lost"); }
        catch (OperationCanceledException) { }
    }

    private static LlmClient Client(FakeHandler handler, int timeout = 5000) => new(new HttpClient(handler) { Timeout = Timeout.InfiniteTimeSpan }, new()
    {
        Endpoint = "https://model.invalid/chat/completions", ApiKey = "test", Model = "test",
        AttemptTimeoutMs = timeout, TotalTimeoutMs = 15000, BaseDelayMs = 1, MaxDelayMs = 2
    });
    private static HttpResponseMessage Response(int status) => new((HttpStatusCode)status) { Content = new StringContent(Success) };
    private static async Task Error(LlmClient client, string code)
    {
        try { await client.CompleteAsync("help"); throw new Exception($"Expected {code}"); }
        catch (LlmException error) { Check(error.Code == code, $"Expected {code}, got {error.Code}"); }
    }
    private static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
    private sealed class FakeHandler(Func<int, CancellationToken, Task<HttpResponseMessage>> send) : HttpMessageHandler
    {
        public int Calls { get; private set; }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token) => send(++Calls, token);
    }
}
