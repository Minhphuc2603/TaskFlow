# TaskFlow — Kế hoạch phát triển tiếp theo

## Tổng quan hiện trạng

Dự án TaskFlow là một ứng dụng quản lý công việc theo kiểu Kanban Board, gồm:
- **Backend**: .NET (Clean Architecture — API / Application / Domain / Infrastructure)
- **Frontend**: Angular 17+ Standalone Components

### Những tính năng đã hoàn chỉnh ✅

| Tính năng | Backend | Frontend |
|---|---|---|
| Đăng ký / Đăng nhập (JWT) | ✅ | ✅ |
| Quản lý Dự án (CRUD) | ✅ | ✅ |
| Mời / Xoá thành viên | ✅ | ✅ |
| Board & Cột (CRUD) | ✅ | ✅ |
| Task (CRUD) | ✅ | ✅ |
| Kéo thả Task / Cột | ✅ | ✅ |
| Bình luận Task | ✅ | ✅ |
| Nhãn (Labels) | ✅ | ✅ |
| Checklist / Subtask | ✅ | ✅ |
| Giao việc (Assignee) | ✅ | ✅ |
| Ưu tiên (Priority) | ✅ | ✅ |
| Hạn chót (Due Date) | ✅ | ✅ |
| Dark/Light mode | — | ✅ |

---

## Những gì còn thiếu / cần làm thêm

### 🔴 Ưu tiên Cao — Tính năng cốt lõi còn thiếu

#### 1. Thông báo & Hoạt động (Activity Log / Notifications)
**Lý do:** Không có lịch sử hoạt động, người dùng không biết ai vừa thay đổi gì trong task hay dự án.
- **Backend**: Thêm entity `ActivityLog`, ghi lại khi task được tạo/chuyển cột/cập nhật/xoá, thêm `NotificationsController`
- **Frontend**: Hiển thị panel "Hoạt động gần đây" trong task detail, badge thông báo trên header

#### 2. Xóa / Sửa Bình luận (Edit & Delete Comments)
**Lý do:** Hiện tại chỉ có thêm comment, không thể xoá hoặc sửa. Logic API và UI đều chưa có.
- **Backend**: Thêm endpoint `DELETE /boards/tasks/{taskId}/comments/{commentId}` và `PUT /boards/tasks/{taskId}/comments/{commentId}`
- **Frontend**: Thêm nút "Sửa" & "Xoá" trên comment của chính mình

#### 3. Trang Profile / Cài đặt tài khoản
**Lý do:** Không có trang nào để người dùng xem/sửa tên, email, mật khẩu.
- **Backend**: Thêm `UserController` với endpoint `GET /users/me`, `PUT /users/me`, `PUT /users/me/password`
- **Frontend**: Tạo trang `/profile` với form chỉnh sửa thông tin cá nhân

#### 4. Board Header: nút "Khởi tạo Task" chưa hoạt động
**Lý do:** Có nút `Khởi tạo Task` trên header board nhưng không có (click) handler — là dead UI.
- **Frontend**: Kết nối nút này với action mở form tạo task nhanh, hoặc xóa nút nếu không cần

---

### 🟡 Ưu tiên Trung bình — UX/UI cần cải thiện

#### 5. Task Card thiếu thông tin trực quan
**Lý do:** Card task hiện chỉ hiển thị tiêu đề và người giao — không có Priority badge, Due Date, số checklist hoàn thành, comment count.
- **Frontend**: Cập nhật `task-card.component.html` để hiển thị:
  - Badge ưu tiên (màu sắc theo priority)
  - Ngày hết hạn (đỏ nếu quá hạn)
  - `✓ x/y` checklist progress
  - 💬 số comment

#### 6. Lọc & Tìm kiếm Task trên Board
**Lý do:** Khi board có nhiều task, không thể lọc theo người dùng/nhãn/ưu tiên.
- **Frontend**: Thêm thanh filter trên board header (lọc theo assignee, priority, label)
- Filter chạy client-side (không cần API mới) để nhanh hơn

#### 7. Revert UI khi API Move Task thất bại
**Lý do:** Trong `board.component.ts` có comment `// Ideally we should revert the UI state here if it fails` — chưa xử lý rollback khi drag-drop thất bại.
- **Frontend**: Lưu state trước khi drop, rollback nếu API trả về lỗi

#### 8. Rename Cột (Edit Column Name)
**Lý do:** Chỉ có thể thêm/xoá cột, không sửa tên/màu sắc.
- **Backend**: Thêm `PUT /boards/columns/{columnId}` endpoint
- **Frontend**: Click vào tên cột để inline-edit

#### 9. Edit Project (tên/mô tả từ Dashboard)
**Lý do:** API `PUT /projects/{id}` đã có nhưng UI không có nút sửa dự án.
- **Frontend**: Thêm nút edit trên project card, mở modal chỉnh sửa

---

### 🟢 Ưu tiên Thấp — Polish & Build-out

#### 10. Upload ảnh đại diện dự án (Cover Image)
**Lý do:** Model `Project` đã có field `coverImageUrl` nhưng không dùng đến — gradient là placeholder.
- **Backend**: Endpoint upload file hoặc nhận URL ảnh khi tạo/sửa project
- **Frontend**: Cho phép chọn ảnh khi tạo project, hiển thị ảnh thay gradient

#### 11. Real-time updates (SignalR)
**Lý do:** Nhiều người làm chung board nhưng không ai thấy thay đổi của nhau theo thời gian thực.
- **Backend**: Tích hợp SignalR Hub
- **Frontend**: Subscribe vào hub, cập nhật board khi nhận event

#### 12. Due Date — Highlight overdue tasks
**Lý do:** Due date đã lưu nhưng không có highlight khi task đã quá hạn trên card.
- **Frontend**: Thêm class CSS `overdue` khi `dueDate < now`, hiển thị màu đỏ

#### 13. Responsive / Mobile layout
**Lý do:** Board dùng horizontal scroll, trên mobile trải nghiệm kém.
- **Frontend**: Tối ưu layout cho màn hình nhỏ

#### 14. Error Handling tập trung
**Lý do:** Hiện tại mỗi subscribe đều `console.error` riêng, không có toast/notification cho người dùng.
- **Frontend**: Tạo `NotificationService` / toast component, thay thế các `console.error` bằng hiển thị lỗi thân thiện

---

## Đề xuất thứ tự thực hiện

```
Sprint 1 (Core & Fixes)
  ├── [4] Fix nút "Khởi tạo Task" dead UI
  ├── [7] Revert khi moveTask thất bại
  ├── [2] Xoá/Sửa bình luận
  └── [5] Task Card thêm thông tin

Sprint 2 (Features)
  ├── [6] Filter & Tìm kiếm Task
  ├── [8] Rename Column
  ├── [9] Edit Project
  └── [3] Trang Profile

Sprint 3 (Polish)
  ├── [12] Highlight overdue tasks
  ├── [14] Error Handling / Toast
  ├── [1] Activity Log
  └── [10] Cover Image Upload

Sprint 4 (Advanced)
  └── [11] Real-time SignalR
```

## Ghi chú kỹ thuật

> [!NOTE]
> Tất cả filter task (mục 6) nên chạy **client-side** để tránh phức tạp API. Chỉ cần thêm signal filter state trong `board.component.ts` và pipe qua columns trước khi render.

> [!TIP]
> Mục 5 (Task Card info) là cải tiến UX có impact cao nhất với effort thấp nhất — nên làm trước.

> [!WARNING]
> Mục 11 (SignalR) là công việc phức tạp nhất, cần thiết kế cẩn thận để tránh conflict race condition khi nhiều người kéo thả cùng lúc.
