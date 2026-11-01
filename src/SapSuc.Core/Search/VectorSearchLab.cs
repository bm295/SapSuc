using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace SuccessFactorsLike.Search;

public interface ITextEmbedding
{
    string SpaceId { get; }
    int Dimensions { get; }
    float[] Embed(string text);
}

// Reproducible lexical baseline, NOT a trained semantic embedding model.
public sealed class HashingEmbedding : ITextEmbedding
{
    public int Dimensions => 256;
    public string SpaceId => "hashing-word-v1:256";
    public float[] Embed(string text)
    {
        if (string.IsNullOrWhiteSpace(text) || text.Length > 4000)
            throw new ArgumentException("Text must contain 1–4000 characters.");
        var vector = new float[Dimensions];
        foreach (Match token in Regex.Matches(text.Normalize().ToLowerInvariant(), @"[\p{L}\p{N}]+"))
        {
            var hash = SHA256.HashData(Encoding.UTF8.GetBytes(token.Value));
            vector[hash[0]] += (hash[1] & 1) == 0 ? 1 : -1;
        }
        return VectorMath.Normalize(vector);
    }
}

public static class VectorMath
{
    public static float[] Normalize(float[] vector)
    {
        if (vector.Length == 0 || vector.Any(x => !float.IsFinite(x)))
            throw new ArgumentException("Vector must be finite and nonempty.");
        var norm = Math.Sqrt(vector.Sum(x => (double)x * x));
        if (norm == 0) throw new ArgumentException("Vector must be nonzero.");
        return vector.Select(x => (float)(x / norm)).ToArray();
    }
    // Both vectors are unit length, so dot product equals cosine similarity.
    public static double Dot(float[] a, float[] b)
    {
        if (a.Length != b.Length) throw new ArgumentException("Dimension mismatch.");
        return a.Zip(b, (x, y) => (double)x * y).Sum();
    }
}

public sealed record SearchDocument(string Id, string Text);
public sealed record SearchHit(string Id, string Text, double Score);
public sealed record SearchResult(IReadOnlyList<SearchHit> Hits, int Candidates, double Milliseconds);
public sealed record SearchComparison(string SpaceId, int Dimensions, int Documents,
    SearchResult Exact, SearchResult Indexed, double RecallAtK);

// Immutable random-hyperplane LSH. Build once; queries never mutate shared state.
public sealed class VectorIndex
{
    private readonly SearchDocument[] documents;
    private readonly float[][] vectors;
    private readonly float[][][] planes;
    private readonly Dictionary<int, List<int>>[] buckets;
    private readonly int dimensions;
    public string SpaceId { get; }

    public VectorIndex(string spaceId, int dimensions,
        IEnumerable<(SearchDocument Document, float[] Vector)> entries, int tables = 8, int bits = 4)
    {
        if (string.IsNullOrWhiteSpace(spaceId) || dimensions < 1 || tables is < 1 or > 32 || bits is < 1 or > 16)
            throw new ArgumentException("Invalid index configuration.");
        SpaceId = spaceId;
        this.dimensions = dimensions;
        var data = entries.ToArray();
        if (data.Any(x => string.IsNullOrWhiteSpace(x.Document.Id)) ||
            data.Select(x => x.Document.Id).Distinct(StringComparer.Ordinal).Count() != data.Length)
            throw new ArgumentException("Document IDs must be nonempty and unique.");
        documents = data.Select(x => x.Document).ToArray();
        vectors = data.Select(x => x.Vector.Length == dimensions ? VectorMath.Normalize(x.Vector)
            : throw new ArgumentException("Dimension mismatch.")).ToArray();
        var random = new Random(295);
        planes = Enumerable.Range(0, tables).Select(_ => Enumerable.Range(0, bits)
            .Select(_ => Enumerable.Range(0, dimensions).Select(_ => (float)(random.NextDouble() * 2 - 1)).ToArray()).ToArray()).ToArray();
        buckets = Enumerable.Range(0, tables).Select(_ => new Dictionary<int, List<int>>()).ToArray();
        for (var t = 0; t < tables; t++)
            for (var i = 0; i < vectors.Length; i++)
            {
                var key = Signature(vectors[i], t);
                if (!buckets[t].TryGetValue(key, out var bucket)) buckets[t][key] = bucket = new();
                bucket.Add(i);
            }
    }

    private int Signature(float[] vector, int table)
    {
        var key = 0;
        for (var bit = 0; bit < planes[table].Length; bit++)
            if (VectorMath.Dot(vector, planes[table][bit]) >= 0) key |= 1 << bit;
        return key;
    }

    public SearchComparison Compare(string spaceId, float[] query, int k = 3)
    {
        if (spaceId != SpaceId || query.Length != dimensions) throw new ArgumentException("Embedding space mismatch; rebuild index.");
        if (k is < 1 or > 20) throw new ArgumentException("k must be between 1 and 20.");
        var unit = VectorMath.Normalize(query);
        var exact = Search(unit, k, false);
        var indexed = Search(unit, k, true);
        var overlap = exact.Hits.Select(x => x.Id).Intersect(indexed.Hits.Select(x => x.Id)).Count();
        return new(SpaceId, dimensions, documents.Length, exact, indexed,
            exact.Hits.Count == 0 ? 1 : (double)overlap / exact.Hits.Count);
    }

    private SearchResult Search(float[] query, int k, bool indexed)
    {
        var timer = Stopwatch.StartNew();
        var candidates = new HashSet<int>();
        if (indexed)
        {
            for (var t = 0; t < buckets.Length; t++)
                if (buckets[t].TryGetValue(Signature(query, t), out var found)) candidates.UnionWith(found);
        }
        else candidates.UnionWith(Enumerable.Range(0, vectors.Length));
        var hits = candidates.Select(i => new SearchHit(documents[i].Id, documents[i].Text, VectorMath.Dot(query, vectors[i])))
            .OrderByDescending(x => x.Score).ThenBy(x => x.Id, StringComparer.Ordinal).Take(k).ToArray();
        timer.Stop();
        return new(hits, candidates.Count, timer.Elapsed.TotalMilliseconds);
    }
}

public sealed class HrSearchLab
{
    private readonly ITextEmbedding embedding = new HashingEmbedding();
    private readonly VectorIndex index;
    public HrSearchLab()
    {
        // Only synthetic public policies; never index employee profiles or salary records here.
        var policies = new[]
        {
            new SearchDocument("leave", "Nhân viên gửi yêu cầu nghỉ phép. Quản lý phê duyệt nghỉ phép theo số ngày còn lại."),
            new SearchDocument("salary", "Điều chỉnh lương cần phê duyệt ngân sách và ngày hiệu lực."),
            new SearchDocument("goals", "Mục tiêu hiệu suất cần chỉ số đo lường và hạn hoàn thành."),
            new SearchDocument("proxy", "Ủy quyền phê duyệt có ngày bắt đầu và ngày kết thúc."),
            new SearchDocument("access", "Quản lý chỉ được xem hồ sơ nhân viên thuộc phạm vi quyền truy cập."),
            new SearchDocument("review", "Đánh giá hiệu suất gồm tự đánh giá và phản hồi của quản lý.")
        };
        index = new(embedding.SpaceId, embedding.Dimensions, policies.Select(d => (d, embedding.Embed(d.Text))));
    }
    public SearchComparison Compare(string query, int k = 3) => index.Compare(embedding.SpaceId, embedding.Embed(query), k);
}
