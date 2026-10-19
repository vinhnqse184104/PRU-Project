# Hành trình M4 chương 1–5

Mở **Thach Sanh → M4 → Mo hanh trinh chuong 1-5**, hoặc tìm scene `Scene_M4_Story` trong Project. Nhấn Play, bấm vào Game rồi Enter để bắt đầu. N bắt đầu lượt mới và xóa tiến trình cốt truyện cũ. Không xóa cài đặt âm thanh hay dữ liệu PlayerPrefs khác.

## Luồng chơi

1. Chương 1: đến bảng nhận nhiệm vụ bằng E; qua cầu; chuột trái chặt cây; E nhặt đủ 5 bó củi. Enter sang chương 2.
2. Chương 2: E nói với Lý Thông; E / Space / chuột trái đọc tiếp; dùng WASD đi theo Lý Thông về nhà. Đọc hết hội thoại kết nghĩa, Enter tới miếu. Màn nối giải thích Lý Thông nhờ Thạch Sanh canh miếu.
3. Chương 3: chuột trái vung rìu, WASD né đòn Chằn Tinh. Thua: R chơi lại. Thắng: Enter mang đầu Chằn Tinh về nhà Lý Thông. Đi qua cửa đang mở, đến gần Lý Thông và E. Enter / Space / chuột trái đọc hết hội thoại, tự sang chương 4.
4. Chương 4: người chơi điều khiển Lý Thông. Đi đến lính hộ tống có nhãn vàng, E để vào chầu; đoạn hộ tống tự đi. Enter / Space / chuột trái đọc thoại với vua và mẹ Lý Thông. Tự sang chương 5.
5. Chương 5: đọc hội thoại vụ bắt Công Chúa. Cảnh theo dấu: giữ chuột phải và rê để ngắm, chuột trái bắn trúng Đại Bàng; đi theo từng mốc vàng và E xem ba dấu vết; E xác nhận cửa hang. Hiện màn hoàn thành chương 1–5; Enter về menu. Không tự vào chương 6.

Điều khiển thông thường: WASD di chuyển, Shift chạy, Space nhảy, giữ chuột phải và rê xoay góc nhìn. Các cảnh hội thoại có hướng dẫn riêng.

## Lưu / tiếp tục

ESC hoặc nút VỀ MENU trả về màn hình đầu. Tiếp tục chơi lại từ **đầu cảnh gần nhất**, không lưu vị trí hoặc từng bó củi / từng dấu vết giữa cảnh. Khi trở về nhà Lý Thông, checkpoint phân biệt đúng lần báo tin sau Chằn Tinh và lần Lý Thông đã làm quan. Thua Chằn Tinh không mở chương tiếp theo. Chơi lại R sau chiến thắng phải hạ boss lại mới được đi tiếp.

Khi hoàn thành cả năm chương, menu chỉ cho bắt đầu lượt mới. Muốn thử riêng các module cũ, mở scene trực tiếp rồi Play: cơ chế chuyển chương mới chỉ hoạt động trong lượt bắt đầu bằng `Scene_M4_Story`.

## Ghi chú ghép nhóm

- Chương 1–3 dùng các cảnh M4 hiện có; đoạn báo tin, cung điện, mẹ Lý Thông và bắt Công Chúa dùng scene nhóm hiện có. Không thay bản đồ gốc bằng nền mới.
- `M4StoryFlow` tự thêm tại runtime, chỉ trong story session. Không tạo player, HP hoặc camera trùng với các scene gameplay.
- Module không sửa gameplay chương 6. Cảnh theo dấu dùng bản đồ thử M4, chưa có animation kéo cung chuyên dụng.
- Build Settings đặt menu đầu tiên, bổ sung ba scene M4 chương 1–3 và giữ các scene nhóm đã có.
- `Chapter2ReturnManager` dùng đúng đường dẫn scene chương 4 và kiểm tra Build Settings trước khi chuyển. Các scene nhóm không cần lưu lại để gắn adapter.
- Các key lưu riêng: `PRU_M4_StoryCheckpoint`, `PRU_M4_StoryFlags`, `PRU_M4_StoryCompleted`.
