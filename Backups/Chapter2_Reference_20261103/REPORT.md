# Nhà Lý Thông — bản dựng theo tham chiếu 03/11/2026

Đã sửa thực tế `C:/Users/Admin/source/repos/PRU-Project/Assets/Scenes/Chapter2_LyThong.unity`. Nhóm mới: `Chapter2_Reference_Redesign`. Nhóm môi trường từ lần trước vẫn giữ lại. Không dùng ảnh tham chiếu làm mặt phẳng hay phông nền trong Scene.

## Đọc cấu trúc và những phần được bảo vệ

Đã quét cấu trúc Assets: trước khi sửa có 1.310 prefab, 685 FBX, 383 material, 154 shader, 22 Scene và 114 file C#. Đây là kiểm kê toàn bộ thư mục, không có nghĩa đã đọc thủ công nội dung mọi file asset. Đã đọc phần liên quan trực tiếp Chương 2 và dùng báo cáo GameObject/collider/material của lượt audit Unity trước để xác định vùng bảo vệ.

Project dùng Unity 6000.6.0f1, URP 17.6.0, Input System 1.20.0, AI Navigation 2.0.15. Build Settings chứa 6 Scene cốt truyện từ RungThachSanh tới Chapter6_HangDaiBang. Các nhóm asset chính: nhanvat, amthanh, phongcanh, Scenes, Medieval_house_lite, MyDreamGameStudio, Toby Fredson, Free_Rocks, SeosipjagakComplex, VillagePack và các gói hang động. Material TTFE và Unreal có khác biệt shader với URP; không thay hàng loạt material gốc.

Các mối liên hệ quan trọng:

- LyThongGuide: NavMeshAgent dẫn tới homePoint, điều khiển người chơi trong luồng cutscene, hội thoại mẹ và bí mật, phối hợp DoorInteraction; tự tìm Directional Light và đặt màu hoàng hôn lúc Start. Không sửa hoặc thêm Directional Light gây nhận nhầm.
- CameraFollow: camera orbit theo target bằng input đang có. Hướng camera ban đầu không nhìn thẳng tới nhà. Không đổi camera để ép bố cục ảnh mẫu.
- PlayerMovement, Animator, QuestManager, StoryStateManager, ChapterDialogueUIHelper và các controller/cutscene khác giữ nguyên code.
- Chapter2ReturnManager và Chapter2OfficialReturnCutscene xử lý lần quay lại và camera hội thoại riêng; giữ nguyên các vị trí và tham chiếu đó.
- DoorInteraction giữ nguyên trigger, cửa, thao tác Q và các phương thức mở cửa mà cutscene dùng.
- Giữ nhà chính `medieval_house_lite_v2 (1)` ở `(84.57, 0.25, 25.48)`, cửa và nội thất. Giữ điểm nhà/mẹ gần `(81.98, 0.267, 31.29)`, điểm vào nhà `(81.75, 1.03, 25.38)` và spawn nhân vật `(97.64, 0.33, 89.05)` như dữ liệu Scene gốc.
- Đường NavMesh từ NPC đến nhà đi chéo từ vùng spawn tới `(81.98, 0.286, 31.29)`. Tất cả chi tiết sân mới không có collider; không bake NavMesh, không sửa TerrainData.

## Thay đổi thực tế lần này

- Thêm mái phủ bằng mesh 900 viên ngói cong, gộp thành một mesh, cùng cột/kèo hiên gỗ hai đầu. Cửa nhiệm vụ, tường và collider nhà gốc không bị xóa hoặc dịch chuyển.
- Thêm 2 cây hoa: model ShrubTree_C có LOD được tái sử dụng cho thân/tán và thêm tổng cộng 1.300 hoa năm cánh bằng mesh 3D. Cây chính đặt để tạo tán phía trái khi nhìn vào sân. Không dùng hình ảnh billboard từ ảnh tham chiếu.
- Thêm đống rơm, các sợi rơm bằng mesh và lồng tre dạng vòm. Đây là lồng trang trí, chưa có gà sống hoặc animation gà.
- Thêm 4 nhà xa sử dụng House_1_URP, 5 khối núi/đồi bằng mesh có độ cao gồ ghề phía sau làng.
- Bầu trời shader mới có trăng tròn, sao và mây procedural; chỉnh sương nhẹ và ánh sáng nền xanh. Thêm 1 đèn xanh dịu cho sân và 1 đèn ấm dưới cây, đều không có bóng đổ realtime để hạn chế chi phí.
- Giữ đường đất, hàng rào, chum, xe, hoa/cỏ và các đạo cụ đã thêm trong nhóm Chapter2_Environment_Enhancement từ lần trước.

Toàn bộ nhà, cây, mái, lồng tre, rơm và núi là geometry 3D. Bầu trời dùng skybox shader, không phải ảnh chụp cảnh được đặt phía sau nhân vật.

## Nguồn asset

Dùng House_1_URP trong Medieval Village Pack Vol.1 đã nhập, model ShrubTree_C trong TTFE đã có và vật liệu URP riêng của Chương 2. Các mesh ngói/hoa/rơm/lồng/núi được tạo trong project. Không mua hay tải file thực thi. Không cần đăng nhập/import thêm để thấy các thay đổi này.

Đã tra cứu nguồn hợp lệ bổ sung:

- Unity Asset Store, Free Medieval Village Pack Vol.1: https://assetstore-fallback.unity.com/packages/3d/environments/fantasy/free-medieval-village-pack-vol-1-374316 — đã có trong project; trang nêu Standard Unity Asset Store EULA.
- Poly Haven, Moonless Golf HDRI: https://polyhaven.com/nl/a/moonless_golf — trang ghi CC0. Chưa tải/import HDRI này; bản đã lưu dùng shader bầu trời tự tạo, nên không có bước tải nào bắt buộc.

## Kiểm chứng

- Unity thực sự chạy công cụ dựng, lưu mesh/material và render `Reference_preview.png`.
- Scene chính đã chứa nhóm mới và geometry, không cần nhấn công cụ để áp dụng.
- So sánh 2.181 khối Scene trước lần sửa này: chỉ RenderSettings và danh sách SceneRoots được thay. Tất cả khối cũ khác giữ nguyên nội dung, bao gồm transform, prefab instance, Camera, Trigger, Collider, NavMesh, script reference và nội dung tham chiếu gameplay.
- So sánh hash 91 script ngoài Editor: không file nào thay đổi.
- Play: Lý Thông di chuyển gần 58 m; agent báo PathComplete; tới MotherDialogue, UI hội thoại đang hiển thị; ghi nhận 0 Error/Exception trong lượt thử. Vị trí thay đổi lúc Play là do code nhiệm vụ có sẵn. Các cờ PlayerPrefs tạm dùng kiểm tra được khôi phục khi thoát Play.
- Nhóm bổ sung không có collider; không tạo NPC và không đổi điều kiện nhiệm vụ.
- Console vẫn có cảnh báo sẵn của project về shader/billboard TTFE và shadow atlas. Không khẳng định toàn bộ Console sạch.

Chưa kiểm tra thủ công mọi phím di chuyển/chiến đấu, toàn bộ hội thoại sau MotherDialogue, cutscene quay lại nhà, mọi hướng tiếp cận hoặc FPS dài hạn. Lượt thử theo nhiệm vụ thành công không thay thế các kiểm tra đó.

## Những điểm còn khác ảnh mẫu

Bản này chưa đạt độ chân thực và mức giống ảnh mẫu. Nhà vẫn mang dáng model gỗ châu Âu ban đầu; phần mái phủ mới không biến toàn bộ ngôi nhà thành kiến trúc Việt Nam chính xác. Một phần mái/cửa sổ áp mái cũ còn nhìn thấy. Hoa/rơm/núi tự dựng còn đơn giản; cây nền cũ còn nhạt/xám. Chưa có gà sống, mặt nước phản chiếu, độ chi tiết gốm/tre và hệ mây như ảnh. Chưa tái tạo bố cục làng chính xác vì không di chuyển nhà gốc, waypoint, NPC hoặc camera.

`Reference_preview.png` dùng camera tạm để đánh giá sân và bị hủy ngay sau render; camera này không được lưu. `GameView_arrival.png` lấy trực tiếp từ Camera.main trong Play, giữ hướng controller gốc và không bao gồm Canvas overlay. Hai ảnh không được coi là cùng góc nhìn. Trăng chỉ xuất hiện khi nhìn về hướng tương ứng trên bầu trời.

## File và khôi phục

- Sửa: `Assets/Scenes/Chapter2_LyThong.unity`.
- Thêm: các mesh `.asset`, vật liệu `.mat`, shader và `.meta` trong `Assets/Chapter2ReferenceRedesign/`; danh sách đầy đủ ở `files_created.txt`.
- Sao lưu trước lần sửa này: `Backups/Chapter2_Reference_20261103/Chapter2.before.unity.txt` và `Chapter2.before.meta.txt`.
- Công cụ dựng/kiểm tra và Scene trung gian được lưu trong thư mục sao lưu dưới dạng `.txt` sau khi thực hiện. Không có script gameplay mới.

Khôi phục: đóng Scene, chép nội dung `Chapter2.before.unity.txt` đè vào `Assets/Scenes/Chapter2_LyThong.unity`, mở lại và chọn Reload. Không phải đổi GUID Scene. Nếu giữ bản mới, chỉ mở Scene và nhấn Play; dùng thao tác orbit sẵn có để nhìn về sân nhà. Không thay camera controller để đạt góc xem thử.
