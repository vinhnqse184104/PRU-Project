# Báo cáo môi trường — đúng bản source/repos/PRU-Project

Scene được sửa: C:/Users/Admin/source/repos/PRU-Project/Assets/Scenes/RungThachSanh.unity.

## Thay đổi

Chỉ ghép nhóm Forest_Environment_Enhancement gồm 1 cây đa Ancient_Banyan_02, 8 bụi Understory_Shrub_01–08, 6 cụm cỏ Natural_Grass_01–06. Tổng cộng 90 GameObject tính cả cấu trúc con hiển thị, 23 MeshRenderer; danh sách đầy đủ trong new-gameobjects.json. Các chi tiết nằm trong hai cụm cây trang trí cố định cay và cay (1). Đây là bổ sung có giới hạn theo vùng đã xác minh an toàn, chưa phải thiết kế lại toàn bộ rừng. Chưa thêm tre, trúc, dây leo, dương xỉ, đá và thân cây đổ.

Không sao chép đè Scene từ bản source/PRU-Project. Bản source/repos có nhiều dữ liệu mới hơn và được giữ làm nền. Đã sao lưu Scene cùng meta trước khi ghép.

## Thành phần giữ nguyên

669 khối dữ liệu Scene có sẵn được giữ nguyên từng byte; khối SceneRoots chỉ thêm một tham chiếu nhóm mới. Điều này bảo toàn toàn bộ Transform, Animator, Collider, Rigidbody, script và tham chiếu của Lý Thông, các nhân vật, camera, ánh sáng, nhiệm vụ, hội thoại, chiến đấu, Terrain và NavMesh. Lý Thông tại bản đúng có vị trí (91.252, 0.39, 78.07); không dùng vị trí Y=0.19 từ bản khác.

Không sửa script, prefab, mesh, material, TerrainData hoặc NavMeshData. Không thêm Collider, Rigidbody, Animator, NavMeshObstacle hay script gameplay. Vật thể hiển thị dùng Ignore Raycast và nhóm có NavMeshModifier ignoreFromBuild/applyToChildren. Không bake NavMesh.

## Đối chiếu và kiểm tra

- Mọi khối dữ liệu gốc của bản đúng giữ nguyên; mã băm trước/sau trong comparison.json.
- 29 tài nguyên/tham chiếu phụ thuộc của nhóm mới giống hệt giữa hai bản. TerrainData và NavMeshData cũng giống hệt.
- Các prefab instance, Transform và collider của hai cụm cây chứa vật thể mới giống hệt dữ liệu đã kiểm tra. Mọi góc AABB renderer mới nằm trong collider lồi cũ; không nới hình học ra khu vực có thể tiếp cận qua va chạm hiện tại. Các nhân vật tại bản đúng vẫn cách các cụm trang trí ít nhất 22m theo kiểm tra trước đó.
- Unity đang mở bản source/repos đã nạp lại Scene thành công. Không ghi đè dữ liệu bằng thao tác Save trong Unity.
- Kiểm tra diff không có lỗi khoảng trắng. Bản đúng chỉ thay đổi Scene và thêm thư mục Backups.
- Các công cụ kiểm tra trên bản đầu đã biên dịch/chạy thành công, xác nhận 0 missing scripts và các mesh/material mới hợp lệ. validation.txt ghi kết quả này; việc nạp lại và đối chiếu bản đúng được ghi riêng ở đây. Không tuyên bố đã chạy lại bộ kiểm tra bằng một Unity nền trên bản đúng đang mở.

## Lỗi/cảnh báo quan sát và giới hạn

Console của Unity có lỗi RenderTexture.Create failed: colorFormat & depthStencilFormat cannot both be none từ 01:19:25, cùng log video mở đầu 1101.mp4; dữ liệu video không thuộc phạm vi thay đổi này. Có cảnh báo TerrainCollider/MeshCollider và Graphics Ring Buffer. Không sửa các hệ thống này. Không quan sát thấy lỗi C# khi nạp nhóm môi trường.

Chưa chạy Play mode để kiểm thử toàn bộ nhiệm vụ, hội thoại, chiến đấu, tuyến di chuyển hoặc đo FPS. Không tạo lại ảnh trước/sau bằng camera mới hơn của bản source/repos; ảnh của bản trước không được chuyển sang đây để tránh nhầm lẫn.

## Khắc phục bản sửa nhầm

Đã khôi phục Scene ở C:/Users/Admin/source/PRU-Project về đúng bản sao trước tác vụ. Các file gameplay đã được chỉnh từ trước trong thư mục đó được giữ nguyên.

## Khôi phục bản đúng

Đóng Scene hoặc mở Scene khác, rồi sao chép RungThachSanh.before.unity.txt đè vào Assets/Scenes/RungThachSanh.unity trong chính source/repos/PRU-Project. Meta chưa thay đổi; bản dự phòng nằm trong RungThachSanh.before.meta.txt.
