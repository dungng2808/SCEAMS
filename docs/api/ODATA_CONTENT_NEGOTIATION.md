# OData và JSON/XML content negotiation – Phase 132

API bật `$select`, `$filter`, `$orderby`, `$expand`, `$count` và giới hạn
`$top` tối đa 50. Endpoint công khai được dùng trong ví dụ là
`GET /api/events`.

## OData filter/order/top

```bash
curl -k -H 'Accept: application/json' \
  'https://localhost:7195/api/events?$filter=Status%20eq%20%27Approved%27&$orderby=StartTime&$top=10'
```

Response JSON mẫu được lưu tại
`docs/api/examples/events-odata-approved.json`. `status` là enum số trong
JSON serializer hiện tại; XML formatter serialize enum thành tên `Approved`.

## `$select` và `$expand`

```bash
curl -k -H 'Accept: application/json' \
  'https://localhost:7195/api/events?$select=Id,Title,StartTime&$expand=Club($select=Id,Name),Venue($select=Id,Name)&$top=1'
```

Response mẫu: `docs/api/examples/events-odata-select-expand.json`.

## JSON và XML cùng endpoint

```bash
curl -k -H 'Accept: application/json' \
  'https://localhost:7195/api/events?$top=1'

curl -k -H 'Accept: application/xml' \
  'https://localhost:7195/api/events?$top=1'
```

Hai response dùng cùng DTO; content type lần lượt là
`application/json; charset=utf-8` và `application/xml; charset=utf-8`. XML mẫu
được lưu tại `docs/api/examples/events-odata-top1.xml`.

## An toàn truy vấn

- OData được cấu hình `SetMaxTop(50)` và không ghép query string thủ công.
- Chỉ bật các query option cần thiết (`Select`, `Filter`, `OrderBy`, `Expand`,
  `Count`).
- Request có `$top` lớn hơn giới hạn bị từ chối; response lỗi dùng
  `ProblemDetails`.

Postman collection tương ứng: `postman/SCEAMS.odata-content-negotiation.postman_collection.json`.
