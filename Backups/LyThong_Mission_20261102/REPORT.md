# Kiểm tra nhiệm vụ tìm Lý Thông dưới gốc đa

Dự án: `C:/Users/Admin/source/repos/PRU-Project`.
Scene: `Assets/Scenes/RungThachSanh.unity`.

## GameObject và Transform đã lưu

| Đối tượng | Đường dẫn Hierarchy | Position | Rotation (Euler) | Scale |
|---|---|---|---|---|
| Lý Thông | `RungThachSanh/LyThong` | `(91.252, 0.39, 78.07)` | `(0, 200, 0)` | `(1, 1, 1)` |
| Cây đa gốc | `RungThachSanh/cayda` | `(94.6, 4.62, 81)` | `(270, 0, 0)` | `(800, 800, 800)` |
| Thạch Sanh | `RungThachSanh/thachsanh` | `(96.46, 0, 92.28)` | `(0, 0, 0)` | `(1, 1, 1)` |

Chỉ có một root GameObject `LyThong`. Cây mục tiêu là `cayda` gốc, không phải bốn cây đa trang trí mới. Không chỉnh các Transform, trigger, collider hoặc NavMesh trong tác vụ kiểm tra này.

## Script thực sự điều khiển nhiệm vụ

- `Assets/QuestManager.cs`, gắn trên `thachsanh`. Trường `lyThongNPC` trỏ đúng tới root `LyThong`.
- `Assets/nhanvat/lythong/LyThongInteraction.cs`, gắn trên `LyThong`. Trường `questManager` trỏ đúng component QuestManager trên `thachsanh`.
- `Assets/StoryStateManager.cs` giữ trạng thái cốt truyện đã lưu; `Chapter1NewGameHandler.cs` xử lý nút bắt đầu lại/F1. Không gọi reset hoặc sửa các file này để kiểm tra.
- Hội thoại dùng panel `Chapter1_DialogueCanvas/.../DialoguePanel` được tham chiếu sẵn. `ChapterDialogueUIHelper.cs` chỉ là hỗ trợ UI có sẵn, không phải nhiệm vụ mới.

Trong scene hiện tại, yêu cầu nhiệm vụ đầu tiên được cấu hình là 1 củi, 1 gà, 1 hươu, 1 ngựa, 1 hổ (giá trị Inspector, khác giá trị mặc định khai báo trong script). Khi đủ mục tiêu, `CompleteQuestOne()` chờ 3 giây rồi chuyển sang nhiệm vụ 2 và bật `LyThong`.

Mục tiêu phải đến là chính vị trí root `LyThong`, không có waypoint mới. `SphereCollider` trên Lý Thông là trigger bán kính 3 m, tâm local `(0,0,0)`. Script có kiểm tra khoảng cách dự phòng `triggerDistance=3` và cờ `isMet` để tránh kích hoạt lặp.

Việc gặp Lý Thông tự động kích hoạt qua trigger/khoảng cách; không có yêu cầu bấm E. Hội thoại tiếp tục bằng Enter, numpad Enter, Space hoặc chuột trái theo `WaitForKeyPress()` gốc. E/R/T/F vẫn là các phím chiến đấu có sẵn.

`WakeUpSequence()` gốc thực hiện đứng dậy trong 1,6 giây, hướng về người chơi, bật model đứng, rồi gọi `MeetLyThong()` để chuyển nhiệm vụ 2 sang 3 và phát hội thoại. Việc xoay/điều chỉnh Y khi đứng dậy là hành vi runtime đã có, không phải thay đổi Transform lưu trong scene. Model con `LyThong_StandingModel` do script gốc tự tạo; không phải một NPC nhiệm vụ thứ hai do tác vụ này thêm.

## Lối tiếp cận và môi trường

Unity tìm được `NavMeshPathStatus.PathComplete` tới các điểm trong vùng tương tác từ 8 hướng lấy mẫu: 0, 45, 90, 135, 180, 225, 270 và 315 độ. Đây là kiểm tra đường từ điểm xuất phát đến điểm tiếp cận quanh NPC, không phải tám lượt đi bộ thủ công.

Bounds renderer gần Lý Thông nhất trong nhóm môi trường tăng cường cách khoảng 14,45 m; không có renderer của nhóm lọt vào khoảng đệm 4 m quanh NPC. Nhóm môi trường không có collider. Vì vậy không phát hiện cây/đá mới chắn lối tìm NPC, và không cần sửa hoặc dời đồ trang trí để bảo vệ nhiệm vụ.

## Kiểm thử Play đã thành công

Bộ kiểm tra Editor tạm phát các sự kiện công khai `CollectWood`/`AddKill` để thiết lập điều kiện nhiệm vụ đầu, sau đó dùng phím di chuyển mô phỏng qua `PlayerMovement` có sẵn; không dịch chuyển Transform trực tiếp và không thay QuestManager bằng hệ thống khác.

Kết quả xác nhận: nhiệm vụ 1 → 2; NPC bật đúng; nhân vật đi từ điểm spawn theo đường NavMesh; vùng tương tác kích hoạt; nhiệm vụ 2 → 3; `DialoguePanel` bật; câu đầu của Thạch Sanh hiện đúng; Enter chuyển sang câu đáp của Lý Thông. Trong phiên thành công, trigger được phát hiện ở vị trí Thạch Sanh khoảng `(88.56, 0.08, 79.70)`. Có đúng một model đứng được tạo bởi script gốc.

Phép thử dừng trước khi hoàn tất bốn câu thoại/chuyển chương, để tránh thay đổi tiến trình lưu. `PlayerPrefs` của cốt truyện được đối chiếu không đổi ở phiên thành công. Ảnh thật trong Play: `dialogue-northwest.png`. Nhật ký phiên thành công: `runtime-northwest.txt`.

Lượt chọn điểm tiếp cận hướng đông (90 độ) có đường NavMesh hoàn chỉnh nhưng bộ điều khiển kiểm tra hết giới hạn 65 giây trước khi ghi nhận tương tác. Kết quả là **TIMEOUT**, không phải một lần kiểm tra đạt; nhật ký: `runtime-east.txt`. Chưa xác định nguyên nhân do bám đường/va chạm vật lý sẵn có hay giới hạn bộ kiểm tra; không suy diễn thành lỗi nhiệm vụ hoặc tự sửa collider/NavMesh. Chưa xác minh Play thành công từ mọi hướng. `PlayerPrefs unchanged=True` ở cuối lượt này.

Các dòng `PlayerPrefs unchanged=False` xuất hiện khi mở/đóng riêng phiên Play để nạp lại bộ kiểm tra là artefact của biến snapshot bộ kiểm tra bị khởi tạo lại trong domain reload; không dùng các dòng đó làm kết quả kiểm tra tiến trình. Nhật ký phiên hoàn chỉnh và file đối chiếu scene là bằng chứng được dùng cho kết luận.

## Phạm vi và giới hạn

Không viết lại/sửa/xóa script gameplay. Không tạo NPC nhiệm vụ mới, không thêm chỉ đường, không thay nội dung nhiệm vụ, không chỉnh môi trường hoặc lưu thay đổi Play vào scene. Chỉ thêm báo cáo và giữ bản mã kiểm tra trong thư mục Backups sau khi dọn công cụ tạm khỏi Assets.

Không kiểm thử toàn bộ săn thú/đốn củi bằng thao tác chiến đấu; các sự kiện hoàn thành được phát để tập trung kiểm tra nhiệm vụ tìm NPC. Không đi hết bốn câu hội thoại/chuyển Chapter 2. Đường 8 hướng đã được kiểm tra NavMesh; kết quả Play cụ thể nằm trong từng nhật ký, không coi PathComplete là bằng chứng cho tám lượt Play thực tế.

Trong quá trình chuẩn bị có một lượt kiểm tra ban đầu không đủ thời gian coroutine/trạng thái Pause; đã sửa bộ kiểm tra và chạy lại thành công, không sửa nhiệm vụ. Có log RenderTexture/video intro và một lỗi đọc file yêu cầu kiểm tra do trùng thời điểm ghi; các lỗi này không phải lỗi QuestManager hay LyThongInteraction. Cửa sổ native có timeout khi yêu cầu foreground; phép thử được thực hiện qua Unity Editor và ảnh Game View do Unity chụp.
