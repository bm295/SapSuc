using SuccessFactorsLike.Integration.Llm;
using System.Threading.RateLimiting;

using SuccessFactorsLike.Integration.AdapterBridge;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
var llmOptions = builder.Configuration.GetSection("Llm").Get<LlmOptions>() ?? new();
builder.Services.AddSingleton(llmOptions);
builder.Services.AddHttpClient<LlmClient>(client => client.Timeout = Timeout.InfiniteTimeSpan)
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false });
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = 429;
    options.AddPolicy("llm", _ => RateLimitPartition.GetFixedWindowLimiter("global", _ => new()
    {
        PermitLimit = 10, Window = TimeSpan.FromMinutes(1), QueueLimit = 0
    }));
    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.Headers.RetryAfter = "60";
        await context.HttpContext.Response.WriteAsJsonAsync(new { code = "api_rate_limited" }, token);
    };
});
builder.Services.AddSingleton(_ => HrPlatformSeeder.Create());
builder.Services.AddSingleton<AdapterImportExecutor>();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddSingleton<SuccessFactorsLike.Search.HrSearchLab>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseRouting();
app.UseRateLimiter();
app.UseSession();
app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

// Explicit opt-in: this sample has no authentication. Add auth before exposing it publicly.
if (builder.Configuration.GetValue<bool>("Llm:Enabled"))
{
    llmOptions.Validate();
    app.MapPost("/api/hr-assistant", async Task<IResult> (HrAssistantRequest input, LlmClient client,
        HttpContext context, ILogger<LlmClient> logger) =>
    {
        if (string.IsNullOrWhiteSpace(input.Prompt) || input.Prompt.Length > 4000)
            return Results.Problem(statusCode: 400, title: "Prompt must contain 1–4000 characters.");
        try
        {
            var answer = await client.CompleteAsync(input.Prompt, context.RequestAborted);
            return Results.Ok(new { answer });
        }
        catch (LlmException error)
        {
            logger.LogWarning("LLM failed: {Code}; TraceId: {TraceId}", error.Code, context.TraceIdentifier);
            if (error.RetryAfter is { } delay)
                context.Response.Headers.RetryAfter = Math.Ceiling(delay.TotalSeconds).ToString(System.Globalization.CultureInfo.InvariantCulture);
            return Results.Problem(statusCode: error.Status, title: error.Code,
                extensions: new Dictionary<string, object?> { ["code"] = error.Code, ["traceId"] = context.TraceIdentifier });
        }
    }).RequireRateLimiting("llm").WithMetadata(new Microsoft.AspNetCore.Mvc.RequestSizeLimitAttribute(20000));
}

// Offline educational endpoint, opt-in because this sample has no authentication.
if (builder.Configuration.GetValue<bool>("VectorSearch:Enabled"))
{
    app.MapPost("/api/hr-search/compare", IResult (VectorSearchRequest input,
        SuccessFactorsLike.Search.HrSearchLab lab) =>
    {
        try { return Results.Ok(lab.Compare(input.Query ?? "", input.K ?? 3)); }
        catch (ArgumentException error)
        { return Results.Problem(statusCode: 400, title: error.Message); }
    }).RequireRateLimiting("llm")
      .WithMetadata(new Microsoft.AspNetCore.Mvc.RequestSizeLimitAttribute(20000));
}

app.Run();

public sealed record VectorSearchRequest(string? Query, int? K);

public sealed record HrAssistantRequest(string? Prompt);
