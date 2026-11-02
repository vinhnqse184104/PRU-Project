# Đèn lối vào nhà Lý Thông — 03/11/2026

Đã áp dụng trực tiếp vào `Assets/Scenes/Chapter2_LyThong.unity` trong project `C:/Users/Admin/source/repos/PRU-Project`. Nhóm quản lý: `Chapter2_Path_Lanterns`. Có 24 prefab instance thật: 22 cột dọc đường (11 mỗi bên) và 2 đèn treo hiên. Không cần chạy công cụ để thấy thay đổi.

## Thiết kế

- Cột gỗ cao khoảng 2,28–2,35 m, có tay đỡ, thanh giằng, móc treo, khung gỗ và mái che bốn phía.
- Khoảng cách dọc cùng một bên khoảng 4,75–5,35 m, hai bên lệch pha và vị trí ngang thay đổi nhẹ. Bám đường đi hiện tại giữa spawn `(97.64, 0, 89.05)` và nhà `(81.98, 0, 31.29)`.
- Tâm cột cách tim đường 3,25–3,4 m. Kể cả mái/tay đèn, giữ khoảng trống ngang tối thiểu xấp xỉ 2,4 m từ tim đường tới hình học mới. Không di chuyển NPC hoặc các điểm nhiệm vụ.
- Mỗi đèn có Point Light vàng cam, range 6,5 m và intensity 3,2; vật liệu giấy đèn dùng shader emission riêng để luôn phát sáng ở mọi hướng. Đèn mới không phát sinh bóng đổ realtime.
- Tái sử dụng texture gỗ từ bản môi trường Chương 2; mesh khung/đèn và mái được tạo trong Unity, lưu thành prefab có thể tái sử dụng. Không tải/mua asset mới.
- Không có Collider/Rigidbody trên hai prefab. Không bake NavMesh. Không thêm Directional Light; ánh trăng và logic ánh sáng cũ giữ nguyên.

## Đã kiểm tra

- Scene trên ổ đĩa đã chứa đúng một nhóm Chapter2_Path_Lanterns với 24 instance. Có 22 cột và 2 đèn hiên.
- Cả hai prefab có 0 collider/Rigidbody. Tất cả 24 đèn mới dùng LightShadows.None.
- So sánh nội dung các khối Scene cũ: chỉ danh sách SceneRoots cần thêm nhóm mới. Transform/camera/NPC/prefab cũ, waypoint, trigger, Terrain, NavMesh và RenderSettings không thay đổi. Không sửa script gameplay.
- Play: NPC di chuyển gần 58 m tới MotherDialogue, NavMeshAgent báo PathComplete, hội thoại hiển thị. Lượt kiểm tra cuối ghi nhận 0 Error/Exception. Các cờ PlayerPrefs tạm dùng kiểm tra đã khôi phục khi thoát Play.
- Đã xem ảnh Camera.main trong Play, thấy đèn và các vùng sáng trên nền đất. `GameView_arrival.png` lấy trực tiếp từ camera gameplay, giữ controller và góc nhìn hiện tại; ảnh Camera.Render không bao gồm Canvas overlay. `Path_preview.png` dùng camera tạm nhìn dọc lối vào nhà, camera đó không được lưu trong Scene.
- Lượt Play đầu phát sinh lỗi RenderTexture height=0 từ UnityEditor.GameView do bố cục cửa sổ quá thấp. Đã mở Game View riêng có kích thước hợp lệ và kiểm tra lại thành công. Giữ log đầu trong `play_test.first_small_GameView.txt`, kết quả cuối ở `play_test.txt`. Không sửa gameplay để xử lý lỗi Editor này.

Chưa đo FPS dài hạn trên máy đích hoặc kiểm thử mọi hướng tiếp cận, mọi thao tác input và toàn bộ cutscene sau MotherDialogue. Console vẫn có các cảnh báo shader/billboard/shadow atlas cũ của project; không khẳng định toàn bộ Console sạch. Các đèn mới đều không đổ bóng nên không tăng số shadow map.

## File

- Sửa: `Assets/Scenes/Chapter2_LyThong.unity`.
- Thêm: `Assets/Chapter2PathLanterns/VillagePathLantern.prefab`, `VillagePorchLantern.prefab`, `PostLanternRoof.asset`, `PorchLanternRoof.asset`, `LanternWeatheredTimber.mat`, `LanternIron.mat`, `LanternAmberEmission.mat`, `LanternEmission.shader`, cùng `.meta`.
- Công cụ tạo/kiểm tra Editor và Scene trung gian được lưu dạng `.txt` trong thư mục sao lưu, không để lại thành phần runtime điều khiển đèn.
- Sao lưu trước chỉnh: `Backups/Chapter2_PathLanterns_20261103/Chapter2.before.unity.txt` và `Chapter2.before.meta.txt`. Khôi phục bằng cách đóng Scene và chép nội dung bản `.unity.txt` về Scene gốc; giữ nguyên GUID.

Mở Scene và nhấn Play để theo Lý Thông về nhà. Nếu Unity hỏi, chọn Reload để nạp thay đổi đã lưu. Muốn tạm ẩn toàn bộ đèn mới, tắt nhóm `Chapter2_Path_Lanterns` trong Hierarchy.
