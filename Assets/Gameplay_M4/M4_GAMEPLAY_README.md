# M4 – Gameplay Đốn củi

Mở `Scene_M4_Gameplay.unity` trong Unity rồi bấm Play. Cảnh này có cầu, đường làng, cỏ, đá, Thạch Sanh, bảng nhiệm vụ và 5 cây đốn củi. `Scene_M4_Playtest.unity` là cảnh kiểm tra cầu cũ.

## Chơi thử

1. WASD để di chuyển đến bảng bên phải đầu đường. Nhấn **E** khi xuất hiện hướng dẫn nhận nhiệm vụ.
2. Đi qua cầu đến khu cây ở bờ bên kia. Shift để chạy, Space để nhảy. Giữ chuột phải và kéo để xoay camera.
3. Đến gần một cây. Nhấn **chuột trái 3 lần**, cách nhau khoảng nửa giây. Cây biến mất, hiện gốc cây và một bó củi ở cạnh gốc.
4. Đến gần bó củi, nhấn **E**. Bộ đếm tăng từ 0/5 đến 1/5. Chặt cây chưa được tính là đã nhặt củi.
5. Lặp lại với 5 cây. Bó thứ 5 hiện **Đã hoàn thành**.

R đưa nhân vật về đầu đường và giữ nguyên tiến độ trong lượt chơi. Dừng Play rồi Play lại để bắt đầu nhiệm vụ mới. Rơi xuống suối cũng đưa về đầu đường.

## Các phần M4 đã có trong cảnh

| Phần | Cách kiểm tra |
| --- | --- |
| Cầu gỗ | Nhân vật đi qua cầu hai chiều, có sàn và lan can chặn va chạm |
| Đường làng | Đường nối điểm bắt đầu, cầu và khu đốn củi |
| Cỏ, đá | Trang trí hai bờ; đá có collider đơn giản |
| Interaction | E nhận nhiệm vụ và nhặt củi khi ở gần, có kiểm tra vật cản |
| Chặt cây | 3 nhát hợp lệ; có thời gian chờ giữa các nhát; hiện gốc cây |
| Nhặt củi | Một cây tạo một bó; nhặt mỗi bó chỉ được tính một lần |
| Quest Đốn củi | Nhận nhiệm vụ, đếm củi 0–5 và tự hoàn thành ở bó thứ 5 |

Gốc cây và bó củi dùng hình dáng và texture của Huy. Mesh riêng trong `OptimizedProps` giảm số tam giác của bó củi từ 5.594.618 xuống 115.862, của gốc cây từ 2.339.488 xuống 120.338. Không thay đổi model và material gốc trong `Assets/Props`.

## Đưa lên GitHub Desktop

Commit riêng các file mới này, luôn tick asset cùng file `.meta` đi kèm:

- `Scene_M4_Gameplay.unity` và `.meta`.
- `M4_GAMEPLAY_README.md` và `.meta`.
- Cả thư mục `Prefabs` và `Prefabs.meta`.
- Cả thư mục `OptimizedProps` và `OptimizedProps.meta`.
- Cả thư mục `Materials` và `Materials.meta` bên trong `Gameplay_M4`.
- 5 script mới và `.meta`: `M4WoodQuest`, `M4QuestGiver`, `M4Woodcutter`, `M4ChoppableTree`, `M4WoodPickup`.

Summary gợi ý: **M4: hoàn thiện chặt cây, nhặt củi và nhiệm vụ 5 bó củi**. Commit vào `tin-gameplay`, sau đó **Push origin**.

11 file chưa commit cũ vẫn được giữ riêng: ZIP, scene test cũ, New Material và `_M4_Gameplay`. Chúng không cần cho cảnh mới này.

## Ghép với cảnh chung của nhóm

Đây là cảnh M4 chơi độc lập. Nhóm vẫn cần đưa các object M4 vào cảnh chung trước khi trình bày bản game tổng hợp.

- Giữ nguyên controller nhân vật của M1. Thêm `M4Woodcutter` vào nhân vật, gán `quest` và `characterAnimator`. Không gắn thêm `M4PlaytestController` nếu nhân vật đã có controller của M1.
- Chép `M4_WoodQuest` và `M4_QuestBoard` từ cảnh demo sang cảnh chung. Gán hai object cho nhau ở Inspector.
- Kéo prefab `M4_ChoppableTree` vào vùng rừng, gán `quest` cho mỗi cây và bảo đảm có ít nhất 5 cây. Prefab đã có gốc, vị trí rơi củi và tham chiếu `M4_FirewoodPickup`.
- M2 có thể lấy `State`, `CollectedWood`, `TargetWood` từ `M4WoodQuest` để hiển thị Quest UI. Event `Changed` phát khi nhận quest hoặc nhặt một bó. Tắt `showHud` trên `M4Woodcutter` khi dùng UI chung.
- M5 có thể nhận event `WoodCollected(int amount)` để thêm củi vào inventory. Mỗi lần nhặt phát lượng 1. Event `Completed` chỉ phát một lần khi đủ 5.
- Chỉ tính củi khi nhặt bó củi. Nếu code cảnh chung đang tăng củi lúc cây chết, bỏ phần tăng đó khi dùng hệ thống này để tránh đếm hai lần.

HP, stamina và inventory thuộc phần ghép của M3/M5; cảnh M4 này chưa nối các hệ thống đó. Model cây đứng dùng asset cây có sẵn trong project; animation chém dùng controller Thạch Sanh có sẵn.

## Kiểm thử đã chạy

Unity 6000.6.0f1: compile và Play Mode đạt. Kiểm tra nhận nhiệm vụ một lần, không chặt trước khi nhận, 15 nhát hợp lệ cho 5 cây, cooldown, giới hạn khoảng cách, không nhặt xuyên vật cản, không nhặt trùng, bộ đếm đúng 5, event hoàn thành một lần và đi qua cầu hai chiều. Nên chơi lại bằng bàn phím/chuột trên máy trước khi demo.

