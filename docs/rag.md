# Retrieval-augmented generation với trích dẫn nguồn

Luồng: Markdown → chunk theo mục → embedding → exact cosine top-k → lọc điểm → LLM → kiểm tra sourceIds → câu trả lời `[S1]` và danh sách nguồn.

## Chạy thử

Dùng .NET 9. Cấu hình endpoint chat completions HTTPS và model theo nhà cung cấp đã chọn; không đưa API key vào Git.

```bash
export Llm__Enabled=true
export Rag__Enabled=true
export Llm__Endpoint='https://YOUR_PROVIDER/chat/completions'
export Llm__ApiKey='YOUR_SECRET'
export Llm__Model='YOUR_MODEL'
dotnet run --project src/SapSuc.Web --urls http://localhost:5090
```

```bash
curl http://localhost:5090/api/hr-assistant/rag \
  -H 'Content-Type: application/json' \
  -d '{"question":"Ai phê duyệt yêu cầu nghỉ phép?","k":3,"minScore":0.2}'
```

`Rag:Enabled` mặc định false và yêu cầu `Llm:Enabled`. Dùng lại giới hạn 10 request/phút, body 20 KB, timeout tổng, retry và cancellation của LlmClient. Lỗi input trả 400; model trả JSON/citation không hợp lệ trả 502 `rag_invalid_citations`; lỗi provider giữ mã hiện có.

Kết quả có `status`, `answer`, `citations`, `embeddingSpace`. Mỗi citation chứa `id`, `sourcePath`, `title`, `startLine`, `endLine`, `quote`, `score`. Quote và metadata lấy trực tiếp từ snapshot corpus trên server, không lấy từ model. Các mã nguồn chỉ có ý nghĩa trong response đó; ứng dụng hiển thị answer và quote dưới dạng plain text, không render HTML từ model. Không có nguồn đủ điểm hoặc model trả claims rỗng: `insufficient_evidence`, không có citations. Không có hit thì không gọi provider.

## Corpus và giới hạn

Corpus là chính sách HR tổng hợp tại `src/SapSuc.Core/Rag/hr-policies.md`, được embed vào assembly. Mỗi mục `##` tạo một chunk, giữ số dòng 1-based và giới hạn 2.000 ký tự. Cập nhật tài liệu cần build/deploy lại. Các trích dẫn chỉ tới snapshot đã build; không phải hệ thống quản lý phiên bản tài liệu.

Dùng lại HashingEmbedding 256 chiều để chạy tái lập, không phải embedding ngữ nghĩa đã huấn luyện. Điểm cosine không phải độ tin cậy; ngưỡng 0.2 là giá trị demo cần đánh giá trên bộ câu hỏi thực tế. Exact search phù hợp corpus nhỏ; VectorIndex hiện có tính thêm nhánh LSH khi Compare, nhưng chỉ dùng kết quả Exact để tạo context. Có thể thay ITextEmbedding bằng model ngữ nghĩa cùng không gian cho query và corpus, rồi rebuild index.

Model trả JSON gồm `claims`, mỗi claim có `text` và `sourceIds`. Server yêu cầu mọi claim có nguồn trong tập đã truy xuất, chặn mã nguồn lạ và tự gắn citation. Điều này kiểm tra tính hợp lệ của tham chiếu, không chứng minh nội dung claim được nguồn hỗ trợ về mặt ngữ nghĩa. Prompt coi câu hỏi và context là dữ liệu không đáng tin cậy; vẫn cần đánh giá groundedness/prompt injection trước ứng dụng thực tế.

Không index hồ sơ nhân viên/lương. Endpoint mẫu chưa có authentication; bổ sung auth và lọc quyền trước retrieval nếu dùng tài liệu nội bộ.

## Kiểm thử

```bash
dotnet build SapSuc.sln --configuration Release
dotnet run --project tests/SapSuc.Tests --configuration Release
```

Runner offline kiểm tra provenance, nội dung context, citation đúng/sai/ngoài tập retrieval, JSON hỏng, abstention, hủy request, ngưỡng không hợp lệ và biên chunk CRLF. Không cần API key. Kiểm thử provider thật cần cấu hình riêng, không nằm trong runner offline.
