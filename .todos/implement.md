# Desktop Capture Shader Pipeline

## Mục tiêu
Cho phép các Gameplay Shader (Blur, Glitch, HueShift, v.v.) tác động lên hình ảnh Desktop phía sau cửa sổ game trong suốt.

## Ý tưởng cốt lõi
1. Mỗi N frame, chụp vùng Desktop tương ứng với vị trí cửa sổ game.
2. Đưa ảnh chụp vào một Texture nền nằm dưới cùng render pipeline.
3. Áp shader lên Texture đó, bật/tắt theo Storyboard Event.
4. Khi không cần, tắt Texture nền để cửa sổ trở lại trong suốt bình thường.

## Công nghệ đề xuất
- **DXGI Desktop Duplication API** (Windows 8+): GPU-to-GPU copy, chi phí ~0.5-2ms/lần.
- **BitBlt** (fallback): Đi qua RAM, chậm hơn (~3-5ms) nhưng tương thích rộng hơn.
- Gọi qua **P/Invoke** từ C#.

## Chiến lược hiệu năng
- Không cần chụp mỗi frame. Chụp ở tần suất thấp hơn (30-60fps) và tái sử dụng ảnh cũ cho các frame trung gian.
- Game logic và render vẫn chạy đầy đủ FPS (120+).

## Lưu ý khi triển khai
- Module capture nên tách biệt hoàn toàn khỏi scene tree, chỉ expose một Texture output duy nhất.
- Module chỉ hoạt động khi có shader yêu cầu, không chạy ngầm khi không cần.
- Cần xử lý các edge case: cửa sổ bị di chuyển, thay đổi kích thước, minimize, multi-monitor.
- Chỉ hỗ trợ Windows, cần có fallback path (bỏ qua feature) cho các nền tảng khác.
