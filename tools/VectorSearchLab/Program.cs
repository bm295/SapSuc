using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using SuccessFactorsLike.Search;

var semantic = args.Contains("--semantic");
var query = args.FirstOrDefault(x => x != "--semantic") ?? "Tôi muốn xin nghỉ vài hôm thì làm thế nào?";
try
{
    SearchComparison result;
    if (!semantic) result = new HrSearchLab().Compare(query);
    else
    {
        var key = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("Set OPENAI_API_KEY for --semantic.");
        const string model = "text-embedding-3-small";
        const int dimensions = 256;
        var documents = new[]
        {
            new SearchDocument("leave", "Nhân viên gửi yêu cầu nghỉ phép cho quản lý phê duyệt."),
            new SearchDocument("salary", "Điều chỉnh lương theo ngân sách và ngày hiệu lực."),
            new SearchDocument("goals", "Thiết lập mục tiêu hiệu suất có thể đo lường."),
            new SearchDocument("access", "Quyền truy cập giới hạn phạm vi hồ sơ được phép xem.")
        };
        if (string.IsNullOrWhiteSpace(query) || query.Length > 4000) throw new ArgumentException("Query must contain 1–4000 characters.");
        using var client = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false }) { Timeout = TimeSpan.FromSeconds(30) };
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", key);
        using var response = await client.PostAsJsonAsync("https://api.openai.com/v1/embeddings", new
        {
            model, dimensions, encoding_format = "float", input = documents.Select(x => x.Text).Append(query).ToArray()
        });
        // Do not log provider bodies (may contain submitted text).
        if (!response.IsSuccessStatusCode) throw new InvalidOperationException($"Embedding provider returned HTTP {(int)response.StatusCode}.");
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var rows = body.RootElement.GetProperty("data").EnumerateArray().OrderBy(x => x.GetProperty("index").GetInt32()).ToArray();
        if (rows.Length != documents.Length + 1 || rows.Where((x, i) => x.GetProperty("index").GetInt32() != i).Any())
            throw new InvalidOperationException("Invalid embedding response indexes.");
        var vectors = rows.Select(x => x.GetProperty("embedding").EnumerateArray().Select(v => v.GetSingle()).ToArray()).ToArray();
        var space = $"openai:{model}:{dimensions}:hr-lab-v1";
        var index = new VectorIndex(space, dimensions, documents.Select((d, i) => (d, vectors[i])));
        result = index.Compare(space, vectors[^1]);
    }
    Console.WriteLine(JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
    return 0;
}
catch (Exception error) when (error is ArgumentException or InvalidOperationException or HttpRequestException or TaskCanceledException or JsonException or KeyNotFoundException)
{
    Console.Error.WriteLine(error is TaskCanceledException ? "Embedding request timed out." : error.Message);
    return 1;
}
