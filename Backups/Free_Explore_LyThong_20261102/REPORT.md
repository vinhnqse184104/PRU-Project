# Luồng tự do khám phá và tìm Lý Thông

Dự án đã sửa: `C:/Users/Admin/source/repos/PRU-Project`.
Scene sử dụng: `Assets/Scenes/RungThachSanh.unity`.

## Ba script đã thay đổi

1. `Assets/QuestManager.cs`
   - Riêng scene `RungThachSanh` khởi tạo tại trạng thái 2 (tìm Lý Thông) và bật chính NPC đã được tham chiếu sẵn. Không cần thu củi hoặc săn thú để mở NPC.
   - Giữ các trường số lượng/yêu cầu cũ và logic nhiệm vụ cũ cho các scene khác; không xóa dữ liệu nhiệm vụ.
   - Dùng `BangNhiemVu` có sẵn để hiện chữ nhỏ trên trái sau khi gameplay bắt đầu: “Nhiệm vụ: Hãy khám phá khu rừng và tìm Lý Thông dưới gốc cây đa.” Sau 6 giây gameplay, đổi thành “Mục tiêu: Tìm Lý Thông”. Bộ đếm thời gian chưa chạy khi đang xem intro.
   - `MeetLyThong()` chỉ tiếp tục trạng thái 2 → 3 sau khi người chơi đã bắt đầu gameplay. Không gọi gặp NPC hoặc hội thoại từ hàm bắt đầu.
   - Toàn bộ coroutine `KichBanHoiThoai`, bốn câu thoại, âm thanh và đoạn chuyển Chapter 2 giữ nguyên.
2. `Assets/GioiThieuCotTruyen.cs`
   - Thêm lời gọi `OnForestGameplayStarted()` sau khi nút BẮT ĐẦU CHƠI đã mở điều khiển và đưa `Time.timeScale` về 1.
   - Chỉ áp dụng cho RungThachSanh. Không đổi phần phát video, movement, Camera hoặc chiến đấu.
3. `Assets/Chapter1NewGameHandler.cs`
   - F1/nút New Game vẫn gọi hàm reset tiến trình cốt truyện có sẵn, sau đó nạp lại `RungThachSanh` thay vì reset một phần và ghi đè vị trí nhân vật bằng tọa độ hard-code.
   - Nạp lại scene đặt lại cả `LyThongInteraction.isMet`, mô hình đứng và coroutine hội thoại; tránh chơi lại mà NPC vẫn mang cờ đã gặp. Intro sẽ được mở lại khi chơi lượt mới.
   - Ẩn cảnh báo dữ liệu chơi cũ sau khi người chơi đã bắt đầu khám phá, để không che mục tiêu trên trái. F1 vẫn hoạt động.

`Assets/StoryStateManager.cs` đã được kiểm tra và không sửa. Bắt đầu gameplay bình thường không xóa PlayerPrefs. Việc xóa các cờ cốt truyện khi người chơi chủ động chọn New Game, và việc reset trước khi chuyển Chapter 2 sau hội thoại, giữ cơ chế đã có.

## Thành phần giữ nguyên

File scene giống hệt bản sao trước tác vụ, SHA256:
`a41186d02de58ce47a4c9b38d314430bfc8232188f96576c7f5dc9a89cf44750`.

Vì không ghi lại scene, toàn bộ Camera, Camera Follow, Transform Thạch Sanh và Lý Thông, cây đa, trigger, collider, NavMesh và bố trí rừng đã lưu giữ nguyên. Không thêm nhân vật, cây đa, waypoint hoặc marker. Không sửa `LyThongInteraction.cs`, `PlayerMovement.cs`, các hệ thống nhảy/chiến đấu, `LyThongGuide.cs` hoặc các script nhiệm vụ chương sau.

Vị trí gốc: Thạch Sanh `(96.46, 0, 92.28)`; Lý Thông `(91.252, 0.39, 78.07)`; cây đa `cayda` `(94.6, 4.62, 81)`. Không có thao tác dịch chuyển Thạch Sanh đến NPC trong code mới. Các thay đổi RectTransform chỉ tác động bảng mục tiêu hiện có trong lúc chạy, không đổi thanh máu/hướng dẫn E R T F.

## Kiểm thử bằng Unity Editor 6000.6.0f1

Phép thử cuối đã đạt (`runtime-result.txt`):

- Khởi tạo với cờ đã chơi chương sau: scene RungThachSanh vẫn vào trạng thái tìm NPC, Lý Thông có sẵn, không tự phát hội thoại, spawn giữ nguyên.
- Bấm bắt đầu qua chính `BamNutBatDau()` đang được nút UI sử dụng: mở gameplay và hiện câu hướng dẫn chính xác ở trên trái.
- Sau 6 giây gameplay: câu ngắn hiện đúng, vẫn không tự phát hội thoại.
- Gọi chức năng New Game/F1 hiện có: scene nạp lại, mục tiêu và cờ tương tác của NPC được khởi tạo lại đúng, spawn lấy từ scene gốc.
- Dùng phím WASD mô phỏng qua `PlayerMovement`/CharacterController có sẵn để đi đến trigger. Không gọi `AddKill` hoặc `CollectWood`, không đổi Transform nhân vật trực tiếp. Cả năm bộ đếm vẫn bằng 0 khi tương tác xảy ra.
- Trigger hiện có hoạt động; trạng thái chuyển 2 → 3; panel hội thoại bật. Enter chuyển đúng bốn câu cũ.
- Sau hội thoại: `Chapter2_LyThong` được nạp, `LyThongGuide` bật ở trạng thái `GuideHome`, tham chiếu nhà/người chơi hợp lệ; `BedSleepTrigger` vẫn trỏ tới `Chapter3_MieuChanTinh`.
- Mã hội thoại và chuyển chương được đối chiếu giống bản sao gốc; các trường dữ liệu nhiệm vụ cũ giữ nguyên. `StoryStateManager.cs` giống từng byte bản sao gốc. Chi tiết trong `verification.json`.
- Sáu khóa PlayerPrefs liên quan được sao lưu trước thử, khôi phục sau khi thoát Play; hai file `test-prefs-before.json`/`test-prefs-after.json` giống nhau. Thiết lập chạy nền chỉ bật tạm cho bộ kiểm tra và đã khôi phục.

Ảnh `objective-full.png` là giao diện cuối đã xem trực quan: không còn cảnh báo cũ che chữ. `dialogue-original.png` là ảnh hội thoại trong Play.

Một lượt kiểm tra trung gian dừng ở bộ đếm thời gian khi Unity mất focus; bộ kiểm tra đã được chỉnh để chạy nền/tự tiếp tục khi Editor tạm dừng do chẩn đoán, rồi chạy lại đạt toàn luồng. Không dùng kết quả timeout làm bằng chứng gameplay đạt. Lượt đầu cũng giúp phát hiện cảnh báo chơi cũ che mục tiêu; lỗi chồng chữ đã được xử lý trong NewGameHandler.

## Giới hạn

Đã kiểm thử nối tiếp tới bước dẫn về nhà ở Chapter 2 và kiểm tra mục tiêu chuyển Chapter 3 còn hợp lệ; chưa chơi hết nội dung các chương 2–6. Không kiểm thử toàn bộ bản đồ hay đo FPS. Các cảnh báo shader/terrain tree, video/RenderTexture và API obsolete có sẵn không được mở rộng sửa trong tác vụ đổi luồng nhiệm vụ.

## Cách kiểm tra và hoàn tác

Mở đúng dự án repos, mở RungThachSanh, nhấn Play rồi BẮT ĐẦU CHƠI (hoặc bỏ qua intro bằng thao tác cũ). Không cần săn thú/thu củi. Đi tìm Lý Thông ở gốc đa và đến trong vùng tương tác 3 m; hội thoại tiếp tục bằng Enter/Space/chuột trái như cũ.

Bản sao ba script trước chỉnh nằm trong thư mục báo cáo với đuôi `.cs.before.txt`. Để quay lại luồng cũ, khôi phục đúng ba script đó; không cần thay scene hoặc xóa asset môi trường. Công cụ Editor kiểm thử tạm được chuyển ra Backups, không để lại component hoặc mã thử trong Assets/bản build.
