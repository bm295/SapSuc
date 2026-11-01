# Thực hành timeout, retries, rate limits và model errors

API mới `POST /api/hr-assistant` chỉ tư vấn HR chung; không đọc hay thay đổi hồ sơ nhân viên.
Client dùng JSON chat-completions (`messages`, `choices`, `finish_reason`).
Không cần SDK hoặc gói resilience bên ngoài.

## Chạy

Yêu cầu .NET 9. Đặt cấu hình bằng environment variables hoặc user-secrets;
không commit API key:

```bash
export Llm__Enabled=true
export Llm__Endpoint=https://YOUR_HOST/v1/chat/completions
export Llm__Model=YOUR_MODEL
export Llm__ApiKey=YOUR_KEY
dotnet run --project src/SapSuc.Web
```

Gọi URL/port được in khi ứng dụng khởi động:

```bash
curl http://localhost:5000/api/hr-assistant \
  -H 'Content-Type: application/json' \
  -d '{"prompt":"Gợi ý cách viết mục tiêu học tập cho nhân viên mới"}'
```

Đây là API mẫu chưa có xác thực. Chỉ bật trong môi trường thực hành riêng;
trước khi mở cho người dùng cần bổ sung xác thực, phân quyền và quota theo tài khoản.
Không gửi dữ liệu nhân sự nhạy cảm trong prompt. Không gửi prompt, key hoặc provider body vào log.

## Chính sách

| Tình huống | Hành vi |
|---|---|
| Mỗi attempt | Timeout 10 giây, bao gồm đọc body |
| Toàn bộ request | Timeout 30 giây, bao gồm backoff |
| 408, 429, 500, 502, 503, 504 hoặc lỗi mạng | Tối đa 2 retries; exponential backoff với full jitter |
| Retry-After | Hỗ trợ seconds/date; không retry sớm hơn chỉ dẫn; nếu vượt MaxDelay thì trả lỗi ngay kèm header |
| 401/403 | 503 `llm_configuration_error`; không retry |
| Các HTTP lỗi khác | 502 `llm_request_rejected`; không retry |
| JSON sai/thiếu, rỗng hoặc body >64 KiB | 502 `llm_invalid_response`; không retry |
| Model refusal | 422 `llm_refusal`; không retry |
| finish_reason khác stop | 502 `llm_incomplete_response`; không retry |
| Hết thời gian | 504 `llm_timeout` |
| Caller ngắt kết nối | Truyền cancellation; không tiếp tục retry |
| API local quá tải | 10 requests/phút cho toàn process, không queue; 429 + Retry-After |

`Llm__AttemptTimeoutMs`, `TotalTimeoutMs`, `MaxRetries`, `BaseDelayMs`, `MaxDelayMs`
có thể ghi đè các mặc định trong `LlmOptions`. Kiểm tra cấu hình khi bật API.
Rate limit local chỉ áp dụng từng instance; nhiều replica cần quota dùng chung.
Retry có thể phát sinh thêm chi phí vì provider có thể đã xử lý attempt bị timeout.
Không áp dụng retry này cho thao tác HR có side effect hoặc tool calls.

## Kiểm thử và bài tập

```bash
dotnet build SapSuc.sln --configuration Release
dotnet run --project tests/SapSuc.Tests --configuration Release
```

Fake HTTP handler kiểm tra lỗi tạm thời rồi thành công, hết retries, lỗi 4xx,
Retry-After dài, response hỏng/truncated, lỗi mạng, timeout và cancellation;
không cần key, không gọi model thật. CI chạy cùng các lệnh trên.
Thử gọi API 11 lần liên tiếp để quan sát rate limit local; đổi MaxRetries về 0
và so sánh số attempts. Response lỗi có `code` và `traceId` để đối chiếu log.
