# M4 – Chương 2: Gặp Lý Thông

Trạng thái: đã tạo cảnh và kiểm tra Play Mode bằng Unity **6000.6.0f1** trên bản sao project. Cả cảnh nền phẳng và cảnh bản đồ nhóm đều chơi được trọn nhiệm vụ bằng phím. Module chưa được commit/push.

Terrain Chương 2 đã được khôi phục từ bản gốc và kiểm tra đọc thành công trong Unity. Module cho phép thử trọn phần gameplay: gặp Lý Thông, dùng WASD điều khiển Thạch Sanh đi theo về nhà, vào sân và đọc hội thoại kết nghĩa. Cảnh bản đồ là cảnh chơi chính; nền phẳng là cảnh tham chiếu để kiểm tra riêng gameplay. Cả hai dừng sau kết nghĩa, không tự mở Chương 3.

| Asset | Dùng để |
| --- | --- |
| `Assets/Gameplay_M4/Chapter2/Scene_M4_Chapter2.unity` | Cảnh thử với nền phẳng; dùng để kiểm tra điều khiển và luồng nhiệm vụ |
| `Assets/Gameplay_M4/Chapter2/Scene_M4_Chapter2_Map.unity` | Cảnh chơi chính: bản đồ nhóm + quest M4, tạo riêng từ `Assets/Scenes/Chapter2_LyThong.unity` |
| `Assets/Gameplay_M4/Chapter2/Prefabs/M4_Chapter2_System.prefab` | Quest, Lý Thông, waypoint và vùng nhà dùng lại trong scene khác; cần gán player/movement của scene và chuẩn bị NavMesh |
| `Assets/Gameplay_M4/Chapter2/Navigation/NavMesh-M4_Chapter2.asset` | Dữ liệu NavMesh cho cảnh thử nền phẳng; không dùng thay NavMesh bản đồ nhóm |

Cảnh bản đồ mới dùng bốn waypoint trên đường đến sân nhà, ánh sáng chiều rõ hơn và tắt bộ tự dẫn Thạch Sanh, logic ngủ cùng tự mở Chương 3 cũ để bạn chơi đủ nhiệm vụ M4. File `Assets/Scenes/Chapter2_LyThong.unity` ban đầu được giữ nguyên. Prefab không chứa player hoặc NavMesh, để bạn gán đúng nhân vật và đường đi của cảnh đang ghép.

## Kiểm tra đã thực hiện

- Unity đọc đúng TerrainData gốc; không thay bằng địa hình khác. Giữ nguyên GUID của Terrain và `.meta`. Git được cấu hình giữ byte dữ liệu Terrain/NavMesh.
- Cảnh nền phẳng: sự kiện phím thật kiểm tra đi 3,5 m/s, chạy 6 m/s, nhảy khoảng 0,95 m, va chạm tường và chơi hết nhiệm vụ bằng WASD/E.
- Cảnh bản đồ nhóm: tự điều khiển bằng WASD từ điểm xuất phát đến sân nhà; nhân vật có collider nền suốt tuyến, mở hai đoạn thoại và hoàn thành đúng một lần.
- Cả hai cảnh: kiểm tra khoảng cách/vật cản, khóa và trả quyền di chuyển, đường không tới được và thử lại, NPC chờ/đi tiếp, bật/tắt quest, vùng sân và sự kiện vào/ra. NPC đến nhà trước không hoàn thành thay người chơi.
- Ảnh Game View 1280 × 720 của cảnh bản đồ được kiểm tra: địa hình, nhân vật và HUD hiển thị bình thường.

Animation Chương 2 dùng controller/clip riêng. Clip chạy của Lý Thông trong nguồn nhóm chưa được import dạng Humanoid, nên controller M4 dùng bản sao clip chạy Humanoid của Thạch Sanh để retarget lên avatar Lý Thông. Asset và controller M2 ban đầu được giữ nguyên.

## Chơi thử

Mở cảnh thử Chương 2, bấm **Play**, rồi bấm vào tab **Game** để nhận phím.

| Phím | Tác dụng |
| --- | --- |
| WASD | Tự điều khiển Thạch Sanh |
| Shift | Chạy |
| Space | Nhảy khi đang di chuyển; đọc tiếp khi đang hội thoại |
| Giữ chuột phải và kéo | Xoay góc nhìn |
| E | Nói chuyện khi đứng gần Lý Thông; đọc tiếp khi đang hội thoại |
| Chuột trái | Đọc tiếp khi đang hội thoại |
| R | Đưa Thạch Sanh về điểm xuất phát; giữ nguyên nhiệm vụ và vị trí Lý Thông |

1. Đến gần Lý Thông, nhấn **E**. Cần đứng trong khoảng 3 m và không có vật cản giữa hai người.
2. Nhấn **E**, **Space** hoặc **chuột trái** để đọc từng câu. Thạch Sanh tạm dừng di chuyển trong hội thoại.
3. Đọc hết thoại đầu, dùng **WASD** đi theo Lý Thông. Bạn tự điều khiển nhân vật suốt đoạn đường.
4. Nếu cách hơn 8 m, Lý Thông dừng chờ. Đến gần trong 5 m để anh ấy đi tiếp. Đây là giá trị mặc định, chỉnh được trong Inspector.
5. Lý Thông đến nhà sẽ đợi. Đi vào vùng sân, ở trong khoảng 3 m quanh điểm nhà và đứng gần Lý Thông để bắt đầu thoại cuối.
6. Đọc hết hội thoại kết nghĩa: hiện **Đã hoàn thành chương 2**. Bạn lấy lại quyền di chuyển.

Muốn chơi lại nhiệm vụ từ đầu: tắt **Play**, rồi bật lại. **R chỉ đưa nhân vật về điểm xuất phát**; nhấn R giữa lúc theo NPC có thể khiến Lý Thông đứng lại chờ bạn.

Nếu Lý Thông chưa đi tiếp được, đến gần anh ấy rồi nhấn **E** để thử lại. Retry giữ waypoint hiện tại và kiểm tra lại đường; không nhảy cóc nhiệm vụ hay đưa NPC tức thời về nhà.

## Phần M4 và phần phối hợp M5

M4 quản lý tương tác có kiểm tra khoảng cách/vật cản, tiến trình nhiệm vụ, NPC đi theo các điểm trên NavMesh, chờ người chơi, trigger sân nhà và điều kiện hoàn thành. Mô hình/animation của nhóm được dùng lại trong demo.

Hộp thoại và bảng nhiệm vụ hiện tại giúp thử luồng chơi. M5 có thể thay nội dung thoại, giao diện nhiệm vụ/hội thoại, camera trình bày và âm thanh qua các hook bên dưới. Câu thoại mẫu chỉnh được bằng `openingDialogue` và `finalDialogue` trong Inspector. Demo gameplay cần được ghép với phần trình bày của nhóm sau đó.

## Ghép vào cảnh nhóm sau khi địa hình đúng

1. Xác nhận Terrain hiển thị và collider nền hoạt động. Dùng bộ điều khiển Thạch Sanh đang có của nhóm; chỉ cần một bộ điều khiển trên nhân vật.
2. Bake NavMesh phù hợp với agent của Lý Thông. NPC phải bắt đầu trên NavMesh và đường về nhà phải đi được toàn bộ. Khi đổi nền/vật cản, bake lại trước khi thử.
3. Tạo Empty GameObject làm waypoint theo thứ tự, cùng một Empty làm `homePoint` ở sân nhà. Đặt các điểm đứng yên, sát mặt nền. Không đặt waypoint/homePoint dưới NPC đang di chuyển hay cánh cửa có animation.
4. Gắn `M4Chapter2Quest` vào object quản lý. Gán `player` là root Thạch Sanh, `playerMovement` là đúng component di chuyển đang dùng, `npcAgent` là NavMeshAgent Lý Thông và `npcAnimator` nếu có. Gán `waypoints` theo thứ tự và `homePoint`; code tự thêm homePoint vào cuối tuyến.
5. Tạo object ở sân nhà với **BoxCollider**, bật **Is Trigger**, rồi gắn `M4Chapter2HomeTrigger`. Gán `homeTrigger.quest` tới quest và `quest.homeTrigger` tới trigger. Box của demo rộng 6 × cao 3 × dài 6 m, center `(0, 1.5, 0)`; chỉnh theo sân nhà thật.
6. Tắt bộ dẫn đường/hội thoại cũ đang điều khiển cùng Lý Thông khi thử module mới. Kiểm tra NPC đi đủ tuyến, chờ khi người chơi ở xa và chỉ mở thoại cuối sau khi cả hai đến sân nhà.

`homeTrigger` chỉ tính đúng Thạch Sanh được gán, kể cả collider con; NPC/vật khác không được tính là người chơi. Vùng nhà kết hợp với `playerArrivalRadius` và kiểm tra khoảng cách/vật cản tới NPC. Lý Thông đến nhà trước chưa hoàn thành nhiệm vụ. Kiểm tra hình học giúp trigger nhận đúng người chơi bắt đầu bên trong hoặc được đưa về vị trí mới.

Quest nhớ trạng thái bật/tắt của `playerMovement`, tạm khóa component khi hội thoại rồi trả lại trạng thái trước đó. Khi quest bị tắt/bật, đoạn dẫn đường giữ tiến độ và hội thoại có thể hiển thị lại câu đang đọc.

## Hook cho giao diện M5

Tắt `showTemporaryHud` để dùng UI riêng; tắt `showDemoLabel` khi ghép vào cảnh chung. Nếu UI/input M5 tự gọi các phương thức dưới đây, tắt thêm `useBuiltInInput` để một lần bấm không bị cả UI lẫn quest xử lý.

| Hook / phương thức | Dùng để |
| --- | --- |
| `Changed(M4Chapter2Quest quest)` | Cập nhật bảng nhiệm vụ qua `State`, `StatusMessage`, `WaypointIndex`, `NpcPaused`; báo khi trạng thái, câu thoại hoặc trạng thái chờ thay đổi |
| `DialogueRequested(string text)` | Hiện câu thoại hiện tại khi bắt đầu, đọc câu mới hoặc bật lại quest đang hội thoại |
| `DialogueClosed()` | Đóng hộp thoại khi kết thúc đoạn nói chuyện hoặc khi component bị tắt |
| `Completed()` | Nhận hoàn thành đúng một lần, sau câu cuối của hội thoại kết nghĩa |
| `TryInteract()` | Nút tương tác: mở thoại phù hợp hoặc thử lại đường bị chặn; trả `false` nếu chưa đủ điều kiện |
| `TryBeginDialogue()` | Yêu cầu mở thoại đầu/cuối theo trạng thái, vẫn kiểm tra các điều kiện |
| `AdvanceDialogue()` | Nút “Tiếp tục”: đọc câu tiếp theo, chỉ hoạt động khi có hội thoại |
| `homeTrigger.Entered` / `Exited` | Người chơi vào/ra sân, một lần cho mỗi lần occupancy đổi; tắt trigger đang có người cũng phát `Exited` |

`CurrentDialogue` và `DialogueIndex` giúp UI đọc trạng thái hiện tại. `PathFailureReason` dành cho debug waypoint/NavMesh; người chơi nhận hướng dẫn tiếng Việt từ `StatusMessage`.

M5 nên đăng ký event khi UI bật và hủy đăng ký khi UI tắt. Dùng `Completed` làm tín hiệu hoàn thành; `Changed` có thể phát nhiều lần trong một nhiệm vụ. Nhóm chỉ nối progression/Chương 3 sau khi thống nhất luồng chung.
