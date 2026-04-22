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

---

# WindowController: Cursor-based Culling

## Mục tiêu
Thay thế vòng lặp `foreach (_windowDataList)` hiện tại (O(n) mỗi frame, n=1000+) bằng cursor-based culling sử dụng `WindowManager.SortedWindows` + `WindowManager.MaxEndBeats`.

## Các bước triển khai

### 1. Sử dụng SortedWindows thay cho _windowDataList
- `LoadWindows()`: nhận `WindowManager` thay vì `List<WindowData>`. Đọc trực tiếp từ `WindowManager.SortedWindows`.
- Bỏ field `_windowDataList`, thay bằng tham chiếu tới `WindowManager`.

### 2. Thêm cursor state
- Thêm field `private int _windowCursor = 0;` để track vị trí hiện tại trong `SortedWindows`.

### 3. Forward Sync (giống NoteController)
- Trong `ForceUpdate`, thay vì duyệt toàn bộ list, bắt đầu từ `_windowCursor`.
- Advance cursor qua các window đã despawn (`currentBeat > EndBeatEndOut`).
- Break sớm khi gặp window có `StartBeat > currentBeat + buffer` (chưa cần spawn).

### 4. Backward Sync (dùng MaxEndBeats)
- Khi `currentBeat < lastBeat` (tua ngược), dùng binary search trên `MaxEndBeats` để tìm cursor cần lùi về.
- Logic tương tự đoạn backward sync trong `NoteController` (L160-L184):
  ```
  while (cursor > 0 && MaxEndBeats != null)
  {
      binary search trên MaxEndBeats[0..cursor-1]
      tìm index nhỏ nhất mà MaxEndBeats[index] >= currentBeat - buffer
      cursor = newCursor;
  }
  ```

### 5. Xử lý invalidation
- Khi `WindowManager.OnWindowChanged` fire (window thêm/xóa, lifecycle thay đổi), reset cursor về 0 hoặc dùng binary search để tìm lại vị trí hợp lệ.
- Cập nhật lại `_cursors` dictionary khi `SortedWindows` thay đổi.

## Lưu ý
- Giữ nguyên `_activeWindows` dictionary cho việc lookup theo ID (O(1)).
- `_cursors` (Storyboard) vẫn cần track theo window ID, không bị ảnh hưởng bởi sort order.
- Cần test cẩn thận trường hợp: tua ngược, window bật Unresponsive giữa chừng, window thêm/xóa runtime.
