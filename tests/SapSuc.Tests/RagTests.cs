using SuccessFactorsLike.Rag;
using SuccessFactorsLike.Search;
using SuccessFactorsLike.Integration.Llm;

internal static class RagTests
{
    public static void Run() => RunAsync().GetAwaiter().GetResult();
    private static async Task RunAsync()
    {
        var rag = RagService.CreateDefault();
        var source = rag.Retrieve("yêu cầu nghỉ phép", 1).Single();
        Check(source.Id == "S1" && source.StartLine == 5 && source.EndLine == 5, "Source provenance");
        var result = await rag.AskAsync("yêu cầu nghỉ phép", (prompt, _) =>
        {
            using var data = System.Text.Json.JsonDocument.Parse(prompt.Split("DATA:\n")[1]);
            Check(data.RootElement.GetProperty("sources")[0].GetProperty("Quote").GetString() == source.Quote,
                "Retrieved context reaches generator");
            return Task.FromResult("{\"claims\":[{\"text\":\"Quản lý phê duyệt nghỉ phép.\",\"sourceIds\":[\"S1\"]}]}");
        }, 1);
        Check(result.Status == "answered" && result.Answer.EndsWith("[S1]") &&
            result.Citations.Single().Quote == source.Quote, "Grounded citation mapping");
        var empty = new RagService(new HashingEmbedding(), Array.Empty<SourceChunk>());
        var abstain = await empty.AskAsync("nghỉ phép", (_, _) => throw new Exception("Must skip generator"));
        Check(abstain.Status == "insufficient_evidence" && abstain.Citations.Count == 0, "Empty retrieval abstains");
        Check((await rag.AskAsync("nghỉ phép", (_, _) => Task.FromResult("{\"claims\":[]}"))).Status ==
            "insufficient_evidence", "Model abstention");
        foreach (var json in new[]
        {
            "not json", "null", "{}", "{\"claims\":[null]}",
            "{\"claims\":[{\"text\":\"claim\",\"sourceIds\":[\"S999\"]}]}",
            "{\"claims\":[{\"text\":\"claim\",\"sourceIds\":[]}]}",
            "{\"claims\":[{\"text\":\"claim [S99]\",\"sourceIds\":[\"S1\"]}]}"
        })
        {
            try { await rag.AskAsync("nghỉ phép", (_, _) => Task.FromResult(json), 1); }
            catch (LlmException ex) when (ex.Code == "rag_invalid_citations") { continue; }
            throw new Exception("Invalid citations must be rejected");
        }
        // Valid corpus ID is still invalid when outside this request's retrieved set.
        try
        {
            await rag.AskAsync("nghỉ phép", (_, _) => Task.FromResult(
                "{\"claims\":[{\"text\":\"claim\",\"sourceIds\":[\"S2\"]}]}"), 1);
            throw new Exception("Non-retrieved citation accepted");
        }
        catch (LlmException ex) when (ex.Code == "rag_invalid_citations") { }
        foreach (var score in new[] { double.NaN, double.PositiveInfinity, -0.1, 1.1 })
        {
            try { rag.Retrieve("nghỉ phép", 1, score); }
            catch (ArgumentException) { continue; }
            throw new Exception("Invalid score accepted");
        }
        using var cancel = new CancellationTokenSource();
        cancel.Cancel();
        try
        {
            await rag.AskAsync("nghỉ phép", (_, _) => throw new Exception("Must not generate"), cancellationToken: cancel.Token);
            throw new Exception("Cancellation ignored");
        }
        catch (OperationCanceledException) { }
        var chunks = RagService.ChunkMarkdown("# Demo\r\n## One\r\nLine A\r\nLine B\r\n\r\n## Two\r\nLine C", "demo.md");
        Check(chunks.Count == 2 && chunks[0].StartLine == 3 && chunks[0].EndLine == 4 &&
            chunks[1].StartLine == 7, "Chunk line boundaries");
    }
    private static void Check(bool value, string message)
    { if (!value) throw new Exception(message); }
}
