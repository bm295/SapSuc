using SuccessFactorsLike.Search;

internal static class VectorSearchTests
{
    public static void Run()
    {
        var embedding = new HashingEmbedding();
        var a = embedding.Embed("nghỉ phép");
        Check(Math.Abs(VectorMath.Dot(a, a) - 1) < 0.00001, "Unit normalization");
        Check(a.SequenceEqual(embedding.Embed("NGHỈ PHÉP")), "Stable tokenization");
        Reject(() => embedding.Embed("!!!"));
        Reject(() => VectorMath.Normalize(new[] { float.NaN }));
        Reject(() => VectorMath.Normalize(new[] { 0f }));
        var input = new[] { 1f, 0f };
        var index = new VectorIndex("test", 2, new[]
        {
            (new SearchDocument("a", "A"), input),
            (new SearchDocument("b", "B"), new[] { 0f, 1f }),
            (new SearchDocument("c", "C"), new[] { -1f, 0f })
        });
        input[0] = -1; // Index owns a defensive normalized copy.
        var result = index.Compare("test", new[] { 1f, 0f }, 1);
        Check(result.Exact.Hits[0].Id == "a", "Cosine ranking");
        Check(result.Indexed.Hits[0].Id == "a" && result.RecallAtK == 1, "Identical vector found by LSH");
        Check(result.Indexed.Candidates <= result.Documents, "Candidate deduplication");
        Reject(() => index.Compare("other-model", new[] { 1f, 0f }));
        Reject(() => index.Compare("test", new[] { 1f }));
        Reject(() => index.Compare("test", new[] { 1f, 0f }, 0));
        Reject(() => new VectorIndex("test", 2, new[]
        {
            (new SearchDocument("a", "A"), new[] { 1f, 0f }),
            (new SearchDocument("a", "B"), new[] { 0f, 1f })
        }));
        var empty = new VectorIndex("test", 2, Array.Empty<(SearchDocument, float[])>());
        Check(empty.Compare("test", new[] { 1f, 0f }).Exact.Hits.Count == 0, "Empty corpus");
        var lab = new HrSearchLab();
        Check(lab.Compare("yêu cầu nghỉ phép", 1).Exact.Hits[0].Id == "leave", "HR policy retrieval");
        Parallel.For(0, 50, _ => Check(lab.Compare("nghỉ phép", 1).Exact.Hits[0].Id == "leave", "Concurrent queries"));
    }
    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }
    private static void Reject(Action action)
    {
        try { action(); }
        catch (ArgumentException) { return; }
        throw new Exception("Expected ArgumentException");
    }
}
