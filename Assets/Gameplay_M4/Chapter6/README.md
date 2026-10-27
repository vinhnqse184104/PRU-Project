# M4 chương 6 — Hang Đại Bàng

Mở menu Unity **Thach Sanh → M4 → Mo chuong 6 - Hang Dai Bang**, hoặc tìm `t:Scene Scene_M4_Chapter6_Map` trong Assets. Nhấn Play rồi bấm vào Game để nhận phím.

Luồng: bắt đầu dưới dây thừng → hạ 2 linh thạch canh hang → giải 3 ấn mở cổng → tìm Công Chúa → bế nàng về điểm dây thừng → E hoàn thành chương 6. Đoạn kéo lên và phản bội thuộc chương 7, chưa chạy trong nhiệm vụ này.

| Phím | Tác dụng |
|---|---|
| WASD / Shift / Space | Đi / chạy / nhảy |
| Giữ chuột phải và rê | Xoay camera |
| Chuột trái | Vung rìu; đòn có tầm đánh, hướng, thời gian hồi và kiểm tra vật cản |
| E | Tương tác ấn, giúp Công Chúa, xác nhận điểm dây thừng |
| R | Chơi lại chương 6 từ đầu, hồi HP và đặt lại toàn bộ quái/bẫy/cổng/câu đố/Công Chúa |
| Esc | Về menu; Tiếp tục chơi lại từ đầu cảnh đã lưu |

Đánh quái khi chúng hồi sức, tránh lúc nhãn SẮP ĐÁNH xuất hiện. Bẫy nhọn báo màu vàng trước khi đỏ; đi qua khi xanh hoặc vòng bên cạnh. HP về 0 thì nhiệm vụ dừng, quái và bẫy không gây thêm sát thương; R để thử lại.

Sau khi hạ đủ quái, thứ tự ấn là **MẶT TRỜI → TRĂNG → SAO**. Bấm sai sẽ tắt các ấn và phải nhập lại. Cổng chỉ mở khi đúng cả ba. Tương tác từ xa, qua vật cản hoặc giúp Công Chúa trước khi mở cổng không hoàn thành thay nhiệm vụ.

## Tài nguyên và phân công

- Bản đồ/cảnh mới lấy từ `vinh2` commit `19b1afd` (25/10), đã bao gồm `Loan3` `13a74ad` (22/10). Tạo scene M4 riêng, giữ scene nhóm nguyên vẹn.
- Công Chúa nằm dưới đất, mẫu Công Chúa được bế, dây thừng và vách đá dùng nguồn nhóm. Bắt đầu sau đoạn thả dây để tập trung gameplay M4.
- Linh thạch dùng đá Huy `props-Huy` `c163080` (23/10), làm mẫu quái tạm vì chưa thấy model dơi/rắn M2 mới trong bản đã kiểm tra. Có thể thay visual riêng mà giữ AI/HP/navigation. Không tuyên bố đã hoàn thành asset M2.
- Kiếm Huy thêm ngày 22/10 chưa cần cho nhiệm vụ dùng rìu nên không nhập vào module.
- Bẫy và ấn là hình minh họa dựng trong Unity để thử logic; có thể thay mesh M3 sau. HUD là bản gameplay, M5 có thể thay bằng UI chính thức.
- Tái sử dụng combat/HP và camera M4 chương 3, không gắn thêm PlayerMovement/PlayerHealth song song. Không đánh boss Đại Bàng tại chương 6: phần này ưu tiên khám phá và tìm Công Chúa theo tài liệu.
- Chương 5 trong story session sẽ mở chương 6 bằng Enter ở màn hoàn thành. Mở riêng scene chương 5 vẫn giữ chế độ thử độc lập. Menu `Scene_M4_Story` cập nhật hành trình 1–6 và checkpoint chương 6.

## Sự kiện ghép UI/cutscene

`M4Chapter6Quest` cung cấp `onGateOpened`, `onPrincessFound`, `onChapterCompleted` (mỗi sự kiện một lần trong mỗi lượt), `State`, `RunesActivated`, `GuardiansDefeated`, `Notice`, `CurrentTarget`, `TryInteract`, `RestartChapter`, `BackToMenu`. Cờ hoàn thành riêng là `PRU_M4_Chapter6Completed`; lượt mới từ menu xóa cờ này. Không lưu từng ấn hay vị trí giữa cảnh.

Runtime không dựa vào sự kiện animation để gây sát thương. Dữ liệu `Navigation/*.asset` được đánh dấu binary trong `.gitattributes` để tránh hỏng NavMesh khi đưa lên Git.
