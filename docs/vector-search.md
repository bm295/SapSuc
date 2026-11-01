# Lab: embeddings, similarity search và vector indexes

## Mục tiêu

Biến chính sách HR thành vector, tìm top-k bằng cosine similarity, rồi so sánh
exact search với một vector index LSH chạy hoàn toàn trong bộ nhớ.
Không gửi hồ sơ nhân viên, lương hay dữ liệu thật của repo vào embedding service.

## 1. Chạy offline, không cần API key

Yêu cầu .NET SDK 9. Từ thư mục gốc repo:

```bash
dotnet run --project tools/VectorSearchLab -- "yêu cầu nghỉ phép"
dotnet run --project tests/SapSuc.Tests
```

`HashingEmbedding` tách từ Unicode, băm ổn định vào 256 chiều rồi chuẩn hóa L2.
Đây là embedding từ vựng để học pipeline, KHÔNG phải mô hình hiểu ngữ nghĩa.
Các câu đồng nghĩa ít trùng từ có thể không tìm đúng; va chạm hash cũng có thể
làm các từ không liên quan có cùng chiều. Chạy lại với câu hỏi diễn đạt khác để thấy giới hạn.

## 2. Thử embedding ngữ nghĩa thật

Đặt `OPENAI_API_KEY` trong biến môi trường của shell, không đưa khóa vào Git.

```bash
dotnet run --project tools/VectorSearchLab -- --semantic "Tôi muốn vắng mặt vài hôm thì phải làm sao?"
```

Chế độ này gửi một batch gồm 4 chính sách giả và câu hỏi tới embeddings API,
dùng `text-embedding-3-small`, 256 chiều. Có chi phí theo dịch vụ; chỉ chạy khi
chủ động thêm `--semantic`. HTTP timeout 30 giây; lỗi trả exit code 1, không
tự retry để tránh khó kiểm soát chi phí. Không gọi LLM sinh câu trả lời.
Không có API key thì bài offline và toàn bộ automated tests vẫn chạy được.

Đọc hợp đồng request/response tại
[Embeddings API](https://developers.openai.com/api/reference/resources/embeddings/methods/create)
và [embedding guide](https://developers.openai.com/api/docs/guides/embeddings).

## 3. API so sánh offline

```bash
VectorSearch__Enabled=true dotnet run --project src/SapSuc.Web -- --urls http://localhost:5080
```

```bash
curl http://localhost:5080/api/hr-search/compare \
  -H 'Content-Type: application/json' \
  -d '{"query":"yêu cầu nghỉ phép","k":3}'
```

API mặc định tắt, chỉ dùng hashing offline; không cần bật `Llm:Enabled`.
Query 1–4000 ký tự, phải có chữ/số; k từ 1–20. Dữ liệu không hợp lệ trả 400,
giới hạn request dùng chung policy 10 request/phút với HR assistant và trả 429.
Ứng dụng mẫu chưa có authentication: chỉ chạy local. Mọi dữ liệu mẫu đều công khai,
không có endpoint ghi hoặc đưa dữ liệu nhân viên vào index.

## 4. Đọc kết quả và mã nguồn

| Trường | Ý nghĩa |
| --- | --- |
| SpaceId, Dimensions | Nhận diện mô hình, cách tiền xử lý và số chiều |
| Exact.Hits | Top-k sau khi chấm điểm mọi vector |
| Indexed.Hits | Top-k sau khi lấy ứng viên từ các bucket LSH |
| Candidates | Số vector được tính cosine; exact luôn bằng số tài liệu |
| Milliseconds | Thời gian truy vấn, gồm chọn ứng viên và xếp hạng; không gồm embedding/build |
| RecallAtK | Số ID indexed trùng exact top-k / số kết quả exact |

Cosine = dot(a,b)/(norm(a)*norm(b)). Index chuẩn hóa vector lúc build và query,
nên dot product chính là cosine. Điểm nằm gần [-1,1], không phải xác suất đúng.
Không so sánh vector từ các embedding spaces khác nhau dù cùng số chiều.
Đổi model, số chiều hoặc tiền xử lý phải tạo lại toàn bộ index.

LSH dùng 8 bảng, mỗi bảng có 4 siêu phẳng ngẫu nhiên cố định; dấu dot product
tạo khóa bucket. Query hợp các bucket trùng khóa, rồi tính cosine chính xác
trên tập ứng viên. Đây là approximate nearest-neighbor index thật; không âm thầm
fallback sang exact khi không có ứng viên. Vì vậy có thể thiếu kết quả hoặc recall < 1.
Tăng tables thường tăng recall và số ứng viên; tăng bits thường giảm ứng viên
nhưng có thể bỏ sót hàng xóm. Chỉnh tham số constructor `VectorIndex` để thử.

Với 4–6 tài liệu, index có thể chậm hơn exact và lấy gần hết tài liệu.
Không suy ra khả năng tăng tốc production từ lab nhỏ này. Thử thêm hàng nghìn
tài liệu, warm-up, chạy nhiều câu hỏi rồi so recall và median/p95 latency;
tách thời gian embedding, build và query. Recall ở đây chỉ đo độ gần với exact,
không chứng minh kết quả đúng ý người dùng: cần bộ câu hỏi có nhãn relevance riêng.

## 5. Bài tập

1. So sánh câu trùng từ và câu đồng nghĩa giữa offline/semantic; ghi top-1 và score.
2. Thay tables/bits, ghi Candidates, RecallAtK và latency trên cùng corpus/query.
3. Thử vector 0, NaN, sai số chiều hoặc sai SpaceId: phải bị từ chối.
4. Giải thích vì sao top-k luôn có thể chứa kết quả không liên quan; chọn threshold
   bằng dữ liệu gán nhãn thay vì đoán một giá trị chung cho mọi embedding model.
5. Trước khi dùng cho HR thật: lọc quyền truy cập trước retrieval, thiết kế cập nhật/xóa,
   lưu metadata phiên bản và đánh giá index bền vững như HNSW hoặc IVF.

Index hiện bất biến, build một lần và mất khi tiến trình dừng. Đây là lab LSH,
không triển khai HNSW/IVF hay vector database. `VectorIndex` nhận vector độc lập
với provider, nên có thể dùng embedding từ model local hoặc dịch vụ khác.
