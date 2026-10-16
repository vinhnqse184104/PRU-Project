# M4 chương 3 — Combat và Chằn Tinh, Phase 1

Unity `6000.6.0f1`. Mở **Scene_M4_Chapter3_Map.unity** để thử trận boss trong bản sao cảnh nhóm; **Scene_M4_Chapter3.unity** là sân trống để thử combat. Scene nguồn `Assets/Scenes/Chapter3_MieuChanTinh.unity`, các script dùng chung và hai chương M4 trước được giữ nguyên.

## Chơi thử

WASD di chuyển; Shift chạy; Space nhảy; giữ chuột phải để xoay camera; chuột trái chém; **R đặt lại cả trận**. Đứng trong tầm và hướng mặt về Chằn Tinh để chém. Né ra ngoài tầm hoặc khỏi hướng đòn đã báo, rồi phản công khi boss hồi sức.

Thạch Sanh có 100 HP; Chằn Tinh có 210 HP. Mỗi nhát rìu gây 35 sát thương, mỗi đòn boss gây 20. Boss báo đòn trước khi đánh và có thời gian hồi sức. Một đòn chỉ gây sát thương một lần cho mỗi mục tiêu, có kiểm tra tầm, hướng và vật cản. Chết hoặc kết thúc trận khóa sát thương và điều khiển; chơi lại đặt lại HP, vị trí, animation, đường đi và đòn chờ.

## Phạm vi bản hiện tại

- M4: di chuyển trong scene thử, melee combat, HP/damage, Enemy AI dạng boss, hai đòn Phase 1, boss chết, thắng/thua và chơi lại.
- M2: dùng model/texture/animation Chằn Tinh đã có. Các animation Generic được sao chép và nối lại đường dẫn xương trong module M4; không đổi importer hoặc asset M2.
- Quái nhỏ, dơi và rắn chưa được tạo trong bản này. `requireSmallEnemies` mặc định tắt theo yêu cầu làm boss trước. Khi bật phải cung cấp danh sách `requiredEnemies`; danh sách thiếu không tự coi là đã hoàn thành.
- HUD hiện tại phục vụ thử gameplay. UI, âm thanh, camera/cutscene cuối cùng của M5 sẽ ghép sau. Không tự tải chương 4.
- Scene map bắt đầu ở phòng boss. Tuyến đi từ rừng/miếu và việc boss xuất hiện sau quái nhỏ cần ghép với luồng chương của nhóm sau.

## API cho nhóm

Namespace `PRU.M4`:

- `M4Chapter3Health`: `CurrentHealth`, `IsAlive`, `TakeDamage`, `ResetHealth`; sự kiện `Changed`, `Damaged`, `Died`. Thêm component này và collider cho quái nhỏ khi ghép sau.
- `M4Chapter3Player`: di chuyển/camera/rìu; `TryAttack`, `SetInputLocked`; `AttackStarted`, `TargetHit`.
- `M4Chapter3Boss`: Idle → Chase → Telegraph → Strike → Recovery; `State`, `StateTimeRemaining`, `AttackTelegraphed`, `Changed`, `ResetCombat`.
- `M4Chapter3Game`: `State`, `RetryRound`, `SetPaused`, `Changed`, `RoundStarted`, `RoundEnded`, `PausedChanged`. M5 đặt `showTemporaryHud=false`, nghe sự kiện để cập nhật HUD/âm thanh. Gọi `SetPaused(true)` khi mở cutscene và `SetPaused(false)` khi đóng; đòn chờ bị hủy để tránh gây sát thương ngay lúc đóng cutscene.
- `M4Chapter3Navigation`: chỉ đăng ký/gỡ NavMeshData do module sở hữu. Scene map có dữ liệu bake từ collider phòng boss; không phụ thuộc vào NavMesh cũ của nhóm.

Native navigation là file nhị phân. Giữ `.gitattributes` trong module và toàn bộ file `.meta` để Git không đổi byte hoặc GUID.

## Đã kiểm tra

Đã chạy trong Unity `6000.6.0f1` trên bản sao project: kiểm tra sát thương theo tầm/hướng/vật cản, các pha boss, pause, thắng/thua và reset; thử cả sân trống và scene map. Lần kiểm tra cuối dùng bàn phím/chuột mô phỏng qua input thật trong 1.338 frame: sáu nhát rìu hạ boss, chơi lại, để nhân vật thua, rồi chơi lại lần nữa. Đã chụp Game View của scene map để kiểm tra model, vật liệu và HUD. Các công cụ kiểm tra chỉ chạy ở bản sao, không được đưa vào module bàn giao.

## GitHub Desktop

Ở branch `tin-gameplay`, chọn toàn bộ **Chapter3** và **Chapter3.meta**, giữ các Changes cũ riêng. Summary: `M4: thêm combat và boss Chằn Tinh chương 3`. Commit rồi Push sau khi tự chơi thử. Nhóm cần review/merge để đưa phần này vào main.
