# SUPER PLATFORM ADVENTURE — C# WinForms

Dự án game đi cảnh kiểu Mario, đồ họa được vẽ bằng GDI+ (không sử dụng sprite Nintendo).

## Yêu cầu
- Windows 10/11
- Visual Studio 2022 (bản mới có hỗ trợ .NET 10) hoặc Visual Studio 2026
- Workload **.NET desktop development** và **.NET 10 SDK**

## Cách chạy
1. Giải nén `MarioAdventureWinForms.zip`.
2. Visual Studio > **Open a project or solution** > mở `MarioAdventureWinForms.csproj`.
3. Bấm **F5** hoặc **Ctrl + F5**.
4. Nếu Visual Studio báo thiếu .NET 10 SDK, vào Visual Studio Installer > Modify > Individual components để cài SDK hỗ trợ; hoặc thay `net10.0-windows` thành một target đã được cài (cần phiên bản hỗ trợ WinForms).

## Điều khiển
- A / D hoặc mũi tên trái/phải: di chuyển
- SPACE / W / mũi tên lên: nhảy
- P / ESC: tạm dừng, tiếp tục
- R: chơi lại màn hiện tại
- M: bật/tắt âm thanh

## 6 tiêu chí
1. **Mức độ (4đ)**: 3 màn tăng độ dài và tốc độ địch; trọng lực, nhảy, va chạm theo trục; xu; quái tuần tra, nhảy lên đầu địch để diệt; bẫy gai, vực, 3 mạng, cờ đích, thắng/thua.
2. **Giao diện (2đ)**: menu, nhập tên, HUD điểm/xu/mạng/màn/thời gian/tiến độ, nút menu qua các chức năng, tạm dừng.
3. **Thành tích (1đ)**: ghi/đọc `scores.tsv` và hiển thị top 10. File dữ liệu tại `%LOCALAPPDATA%\MarioAdventureWinForms\scores.tsv`.
4. **Âm thanh (1đ)**: có nhạc nền WAV được tạo riêng, hiệu ứng Windows, nút bật/tắt ở menu và phím M trong game.
5. **Hướng dẫn (1đ)**: menu hướng dẫn, cách chơi, điều khiển. **Nhớ sửa tên thành viên nhóm** trong `MainMenu.cs` hàm `ShowHelp()`.
6. **Bẫy lỗi (1đ)**: kiểm tra file lưu điểm, dòng lỗi bị bỏ qua; ngoại lệ đọc/ghi có thông báo; giới hạn vị trí và xử lý rơi vực.

## Lưu ý trước khi nộp
- Điền đầy đủ thành viên nhóm, mã sinh viên, lớp và thông tin giảng viên trong `ShowHelp()`.
- Nên chạy thử cả 3 màn trên máy Windows thật, quay video demo gameplay và kiểm tra các chức năng đọc/ghi điểm, âm thanh, pause.
- Đây là mã nguồn đã tạo nhưng **chưa được build/chạy thực tế trên Windows trong môi trường hiện tại**. Việc đáp ứng tiêu chí là thiết kế; điểm cuối cùng do giảng viên quyết định.

## Cập nhật vị trí xu (09/10/2026)
- Điều chỉnh độ cao nền tảng có xu về 435/425/415 (thay vì 410/355/300).
- Từ mặt đất y=520, các nền tảng này cao tối đa 105 px, nằm trong tầm nhảy khoảng 133 px của nhân vật.
- Giữ nguyên bản sửa căn giữa tiêu đề từ gói CanGiuaTieuDe.


## Cập nhật ba màn & AI quái
- Màn 1: Đồng cỏ (xanh), vực thưa và ít gai.
- Màn 2: Sa mạc (cam), nền tảng dày hơn, bẫy nhiều hơn.
- Màn 3: Đêm trăng (tím), nền tảng nhỏ hơn và quái nhiều hơn.
- Quái tuần tra kiểm tra nền đất trước bước chân, quay đầu khi gặp vực hoặc gai.
- Menu đã giảm cỡ tiêu đề và căn giữa tự động.

## Bản cập nhật địa hình thực sự khác biệt (09/10/2026)
- Màn 1: đồi cỏ gồm các khu đất cao thấp, vực rộng 80–85 px, cầu/bục thấp.
- Màn 2: sa mạc với các bậc đất nâng dần, khe sâu 90–95 px, cột/bục đá ở độ cao khác nhau.
- Màn 3: tàn tích đêm trăng, nhiều đảo đá và vực, các tầng địa hình lên xuống liên tiếp.
- Quái tuần tra theo cao độ của chính đoạn đất chúng đứng, không đi xuống vực hoặc tụt khỏi mép bậc.
- Xu được rải trên các đoạn đất và trên bục; các chênh cao được giới hạn theo khả năng nhảy.
- **Chưa chạy test trên Windows tại môi trường tạo dự án.**

## Sửa lỗi 11/10/2026
- R khôi phục điểm và số xu về đầu màn, giữ số mạng còn lại, tránh kiếm điểm lặp.
- Đồng hồ dừng khi pause hoặc chuyển sang cửa sổ khác.
- Đồng bộ nhãn âm thanh khi về menu; bật âm trong pause không phát nhạc.
- Chặn lặp phím P/M/R khi giữ phím; bắt lỗi mở game và vòng cập nhật.
- Sửa mục file WAV thành None Update, tránh khai báo trùng file mặc định.
- Thành viên nhóm: Hoàng Đình Trọng, Trần Anh Minh và Nguyễn Hữu Hoàng Nam.

### Kiểm tra trên Windows trước khi nộp
1. Build dự án và mở game; chơi đủ ba màn.
2. Thu xu, nhấn R: điểm/xu trở về đầu màn, mạng không tăng.
3. Pause 10 giây rồi tiếp tục: thời gian chơi không tăng thêm 10 giây.
4. Tắt âm bằng M, về menu: nhãn hiện TẮT; bật âm trong pause không có nhạc.
5. Thắng/hết mạng, mở bảng thành tích và mở lại ứng dụng: điểm vẫn được lưu.
6. Điền thông tin nhóm, quay video demo và đưa mã nguồn lên GitHub.

Bản sửa được kiểm tra tĩnh; chưa build/chạy trên Windows trong môi trường hiện tại.

## Thành viên nhóm
- Hoàng Đình Trọng - 51.01.104.108
- Trần Anh Minh - 50.01.103.042
- Nguyễn Hữu Hoàng Nam - 45.01.104.146 