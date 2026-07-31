# SCEAMS – Phase 130 Business-rule regression

Bộ regression này đối chiếu 11 business rule trong đề bài với service nghiệp vụ,
request kiểm thử Postman và trạng thái HTTP mong đợi. Các request có thể chạy
lại theo thứ tự trong `postman/SCEAMS.postman_collection.json`; biến ID và token
được lấy từ environment local, không ghi cứng vào source.

| Rule | Điều kiện cần kiểm thử | Request/bằng chứng | Kết quả |
|---|---|---|---|
| BR1 | Chỉ đăng ký Event `Approved`, trước deadline | `Phase 91 - Register Event - Student` + `RegistrationService` | PASS |
| BR2 | Capacity không vượt kể cả request đồng thời | Transaction/concurrency trong `RegistrationService`; chạy hai request đăng ký cùng lúc | PASS |
| BR3 | Student không đăng ký trùng Event | `Phase 91` duplicate path, trả `409` | PASS |
| BR4 | Không hủy trong 24 giờ trước StartTime | `Phase 93 - Cancel Registration - Student` + deadline guard | PASS |
| BR5 | Chỉ registration `Attended` mới feedback, một lần/Student/Event | `Phase 101 - Feedback Event - Student` + `FeedbackService` | PASS |
| BR6 | Organizer chỉ thao tác Club/Event mình phụ trách | `Phase 47`, `Phase 77`, `Phase 87`; ownership check từ JWT subject | PASS |
| BR7 | Approve chặn overlap Venue với Event `Approved/Ongoing` | `Phase 83 - Approve Event - Admin`; conflict trả `409` | PASS |
| BR8 | `Completed`/`Cancelled` không sửa thông tin chính | `Phase 77 - Update Event` status guard | PASS |
| BR9 | Membership chỉ chuyển `Active` sau khi được duyệt | `Phase 51`, `Phase 55`, `Phase 57`; trạng thái `Pending/Active/Rejected` | PASS |
| BR10 | Chatbot chỉ trả Event từ retrieval, không bịa | `Phase 121` retrieval trước `Phase 123` ask; fallback khi không có context | PASS |
| BR11 | Tối đa 10 câu hỏi AI/Student/giờ | `Phase 127` trả `429` + `Retry-After`, không gọi provider | PASS |

## Cách chạy

### Kiểm tra source và Postman contract

```bash
bash tools/business-rule-regression.sh
```

### Regression API có dữ liệu demo

```bash
npx --yes newman run postman/SCEAMS.postman_collection.json \
  -e /tmp/sceams-postman-local.json --insecure --bail
```

Environment phải có token của bốn role và các ID đã tạo từ request trước. Không
chạy trên production vì một số request tạo dữ liệu kiểm thử; dùng database
Development riêng rồi xóa dữ liệu test sau khi hoàn tất.

## Tiêu chí pass

- Service không nhận owner/status/role từ client để quyết định quyền.
- Lỗi nghiệp vụ trả `409` hoặc `400` đúng contract, không biến thành `500`.
- Các request đồng thời không tạo registration vượt capacity.
- Regression collection không có assertion thất bại.
