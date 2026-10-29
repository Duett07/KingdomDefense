# KINGDOM DEFENSE - BẢO VỆ VƯƠNG QUỐC

> **Nền tảng phát triển:** Unity 6 (6000.6.7f1) | C# .NET 8 / Roslyn  
> **Nền tảng hỗ trợ (Multiplatform):** PC Windows (x64) & Di động Android (.apk)  

[![Unity 6](https://img.shields.io/badge/Unity-6000.6.7f1-black)](https://unity.com/)
[![C#](https://img.shields.io/badge/Language-C%23%2012%20%2F%20.NET%208-blue)](https://docs.microsoft.com/en-us/dotnet/csharp/)
[![Platform](https://img.shields.io/badge/Platform-Windows%20%7C%20Android-green)](https://unity.com/)
[![Framerate](https://img.shields.io/badge/Framerate-60%20FPS%20Locked-brightgreen)](#)
[![Tests](https://img.shields.io/badge/Tests-Passed%20(100%25)-success)](#)

---

## GIỚI THIỆU TỔNG QUAN

**Kingdom Defense (Bảo Vệ Vương Quốc)** là một tựa game chiến thuật phòng thủ tháp (3D Tower Defense) kết hợp yếu tố tương tác thời gian thực (Real-time Combat & Melee Allies). Người chơi đảm nhận trọng trách của một vị Thống soái Hoàng gia: xây dựng và nâng cấp hệ thống tháp phòng thủ dọc theo hẻm núi, triển khai lực lượng Hiệp sĩ Cổng Thành giáp lá cà, và giải phóng các kỹ năng tối thượng nhằm ngăn chặn 10 đợt xâm lăng tàn khốc của Đại quân Hắc ám do **Ma Vương Orcus the Destroyer** dẫn đầu.

Dự án được xây dựng với mục tiêu thể hiện trọn vẹn các kiến thức chuyên sâu của bộ môn **Môi trường ảo**:
- Mô hình hóa không gian 3D, camera phối cảnh nghiêng (Isometric 60°).
- Giải thuật tìm đường và di chuyển dọc đường mòn (Waypoints System).
- Xử lý va chạm vật lý thời gian thực, đạn đạo hình học (Ballistics) và sát thương diện rộng (AoE Splash).
- Tối ưu hóa hiệu năng bằng các mẫu thiết kế: **Object Pooling**, **Singleton**, **Observer**, **Procedural Generation**, đạt mốc **60 FPS** ổn định trên cả máy tính và thiết bị di động.

---

## TÍNH NĂNG NỔI BẬT (CORE FEATURES)

### 1. Hệ Thống Tháp Phòng Thủ (4 Chủng Loại Đa Dạng)
Mỗi loại tháp sở hữu cơ chế hỏa lực, sát thương và hiệu ứng chiến thuật riêng biệt:

| Loại Tháp | Cơ Chế Tấn Công | Loại Sát Thương | Ưu Điểm Chiến Thuật | Chi Phí Xây |
| :--- | :--- | :--- | :--- | :--- |
| **Tháp Cung Thủ (Archer)** | Bắn tên liên thanh tầm xa | Vật lý (Physical) | Tốc độ bắn rất nhanh (1.6/s), diệt quái chạy nhanh | 100 Vàng |
| **Tháp Pháo Binh (Cannon)** | Bắn đạn pháo cầu vồng | Nổ diện rộng (Splash AoE) | Bán kính nổ 2.5m, quét sạch quái đi theo đàn | 125 Vàng |
| **Tháp Pháp Sư (Mage)** | Phóng cầu phép thuật tầm cao | Phép thuật (Magic) | Xuyên giáp vật lý, khắc tinh của quái tank giáp dày | 150 Vàng |
| **Tháp Băng Phong (Frost)** | Hào quang băng giá 360° | Làm chậm (Slow Aura) | Giảm 45% tốc độ quái vật trong 4.5m, kiểm soát đường | 175 Vàng |

- **Hệ thống Nâng cấp 3 Cấp độ (Level 1 -> 3):**
  - Cấp 2: Gia tăng tầm quét và sát thương cơ bản.
  - Cấp 3: Mở khóa hiệu ứng hào quang Hoàng gia (Royal Golden Aura) và diện mạo tối thượng.
- **Hệ thống Bán Tháp (Sell System):** Hoàn trả ngay **70% tổng vốn đầu tư**, giải phóng ô cắm tháp để tái cơ cấu đội hình chiến thuật linh hoạt.

---

### 2. Hiệp Sĩ Cổng Thành & Doanh Trại Hoàng Gia (Castle Barracks & Knights)
- **Cơ chế Chặn đường Giáp lá cà (Path Blocking & Melee Combat):**
  - Doanh trại Cổng Thành duy trì đội hình 3 Hiệp sĩ tinh nhuệ mang kiếm và khiên, án ngữ ngay lòng đường mòn dẫn vào Hoàng Thành.
  - Khi quái vật chạm trán, Hiệp sĩ sẽ chặn đường và giao tranh giáp lá cà, giữ chân quái vật trong tầm hỏa lực của các tháp phòng thủ.
- **Thanh máu 3D nổi trên đầu (Overhead Health Bars):**
  - Cả hiệp sĩ và quái vật đều có thanh máu trực quan trên đầu (Billboard tự xoay về Camera), tự đổi màu Xanh -> Vàng -> Đỏ theo mức độ tổn thương.
- **Cơ chế Tái sinh (Resurrection):** Khi hiệp sĩ hy sinh anh dũng, doanh trại sẽ tự động tuyển mộ và xuất quân hiệp sĩ mới sau 6 giây.

---

### 3. Hệ Thống Quái Vật (5 Chủng Loại & Trùm Cuối)
Độ khó tăng tiến liên tục qua 10 Wave chiến dịch với chiến thuật khắc chế đa dạng:

1. **Goblin Runner:** Tốc độ thần tốc (4.5 m/s), máu thấp, chuyên đánh lừa hệ thống ngắm bắn.
2. **Orc Warrior:** Chiến binh cận chiến lì lợm (350 HP), giáp vật lý 25%, áp sát dồn dập.
3. **Stone Golem:** Quái thú đá khổng lồ (1200 HP), giáp vật lý 50%, miễn nhiễm đẩy lùi.
4. **Dark Bat (Dơi Quỷ):** Quái bay trên không, bỏ qua toàn bộ chướng ngại và hiệp sĩ dưới mặt đất.
5. **Trùm Cuối - Ma Vương Orcus the Destroyer (Wave 10):**
   - Lượng máu cực lớn (3600 HP), giáp vật lý 35%, kháng phép 25%.
   - **Kỹ năng Dậm Đất Chấn Động (Stun Quake):** Mỗi 8 giây dậm mạnh tạo sóng chấn động 6.0m làm rung màn hình và **làm choáng toàn bộ tháp phòng thủ trong 3.0 giây**.

---

### 4. Kỹ Năng Tối Thượng: Mưa Thiên Thạch (Meteor Strike)
- Phím tắt nhanh **`[5]`** để giải phóng cơn mưa thiên thạch từ bầu trời xuống tọa độ chỉ định.
- Gây sát thương nổ hủy diệt trên diện rộng kèm rung chấn màn hình (Camera Shake Trauma FX), xoay chuyển cục diện trận đánh hiểm nghèo.

---

### 5. Hệ Thống Âm Thanh Thủ Tục (Procedural Audio Synthesizer)
- Toàn bộ hiệu ứng âm thanh (SFX) và nhạc hiệu được tổng hợp trực tiếp bằng mã C# Digital Signal Processing (DSP) ở tần số chuẩn 44.1kHz:
  - Tiếng bắn cung nhọn vút, tiếng nổ đại pháo uy lực, tiếng phép thuật và sương băng, tiếng tù và chiến trận (Wave Horn), tiếng tiền vàng rơi lách cách.
  - **Ưu điểm:** Dung lượng bản build siêu nhẹ, hoàn toàn độc lập, không phụ thuộc vào bất kỳ file âm thanh nén ngoài nào.

---

### 6. Sinh Bản Đồ Tự Động (Procedural Map Generator)
- Tự động dựng toàn bộ chiến trường 3D sống động từ mã nguồn:
  - Cổng thành uy nghi với tháp canh đá và ngọn đuốc rực lửa.
  - Đoạn đường mòn uốn lượn mềm mại, mở rộng trước cổng thành.
  - Cầu đá cổ kính bắc qua dòng sông xanh biếc, vách núi hoa cương và cối xay gió.
  - Các ô cắm tháp (Build Nodes) bằng đá với hiệu ứng tương tác đổi màu trực quan khi rê chuột.

---

## BẢNG ĐIỀU KHIỂN & PHÍM TẮT (CONTROLS)

| Thao Tác | Phím Tắt / Chuột | Chức Năng |
| :--- | :--- | :--- |
| **Chọn Tháp Cung** | Phím **`[1]`** | Chọn nhanh Tháp Cung Thủ (100 Vàng) |
| **Chọn Tháp Pháo** | Phím **`[2]`** | Chọn nhanh Tháp Pháo Binh (125 Vàng) |
| **Chọn Tháp Phép** | Phím **`[3]`** | Chọn nhanh Tháp Pháp Sư (150 Vàng) |
| **Chọn Tháp Băng** | Phím **`[4]`** | Chọn nhanh Tháp Băng Phong (175 Vàng) |
| **Kỹ năng Thiên Thạch** | Phím **`[5]`** | Kích hoạt kỹ năng Tối thượng Mưa Thiên Thạch |
| **Tạm Dừng / Tiếp Tục** | Phím **`[Space]`** | Tạm dừng hoặc tiếp tục màn chơi (Pause / Resume) |
| **Hủy Chọn / Đóng Menu** | Phím **`[Esc]`** | Hủy chọn tháp đang cầm hoặc đóng Menu nâng cấp |
| **Xây Tháp / Chọn Tháp** | **Click Chuột Trái** | Click vào ô Node để đặt tháp, hoặc click vào tháp để mở Menu |
| **Điều Khiển Camera** | **Chuột Phải / Kéo** | Xoay góc nhìn hoặc trượt camera bao quát chiến trường |
| **Tốc Độ Trận Đấu** | **Nút HUD (1x / 2x / 4x)** | Tăng tốc độ mô phỏng thời gian trận đấu |

---

## KIẾN TRÚC MÃ NGUỒN (PROJECT ARCHITECTURE)

Toàn bộ mã nguồn C# được bố trí trong `Assets/_KingdomDefense/Scripts/` theo cấu trúc module hóa chuẩn công nghiệp:

```text
Assets/_KingdomDefense/
├── Materials/                # Vật liệu chính thức (M_Knight, M_BossOrcus, M_TowerDefense,...)
├── Models/                   # Mô hình 3D fbx (Characters, Weapons, Environment)
├── Prefabs/                  # Prefabs đóng gói sẵn (Enemies, Towers, Projectiles)
├── Resources/                # Tài nguyên tự động nạp độc lập trên Mobile / Standalone
│   ├── Allies/               # Model Hiệp sĩ đồng minh
│   ├── Enemies/              # 5 Prefab Quái vật hoàn chỉnh
│   ├── Materials/            # Vật liệu nạp động
│   └── Weapons/              # Vũ khí nạp động
├── Scenes/                   # Scene chính: Battlefield.unity
└── Scripts/
    ├── Allies/               # CastleBarracks.cs, CastleKnight.cs (Doanh trại & Hiệp sĩ)
    ├── Audio/                # AudioManager.cs, ProceduralAudioSynthesizer.cs
    ├── Build/                # BuildManager.cs, Node.cs (Hệ thống xây tháp)
    ├── Camera/               # CameraController.cs (Trauma Screen Shake)
    ├── Core/                 # GameManager.cs, ObjectPool.cs, KingdomDefenseBuildPipeline.cs
    │   └── Editor/           # Bộ Test tự động: BalanceTests, EdgeCaseTests, CastleKnightTests,...
    ├── Enemy/                # Enemy.cs, BossOrcus.cs, WaveSpawner.cs, EnemyFactory.cs
    ├── Map/                  # MapGenerator.cs (Thuật toán sinh địa hình 3D)
    ├── Skills/               # MeteorSkill.cs, MeteorProjectile.cs
    ├── Towers/               # Tower.cs, ArcherTower, CannonTower, MageTower, FrostTower
    ├── UI/                   # GameHUD.cs, TowerUI.cs (Menu nâng cấp/bán tháp)
    └── VFX/                  # ParticleEffectSpawner.cs, VFXManager.cs
```

---

## HƯỚNG DẪN CÀI ĐẶT & CHẠY DỰ ÁN

### 1. Yêu Cầu Môi Trường
- **Unity Editor:** Phiên bản **`6000.6.7f1`** (Unity 6 LTS) hoặc mới hơn.
- **Module Hỗ trợ:**
  - *Windows Build Support (IL2CPP / Mono)*
  - *Android Build Support* (OpenJDK, Android SDK & NDK kèm sẵn trong Unity Hub)
- **Hệ điều hành:** Linux (Ubuntu 20.04+), Windows 10/11, hoặc macOS.

### 2. Mở Dự Án trong Unity Editor
1. Khởi động **Unity Hub**.
2. Chọn **Add** -> Trỏ tới thư mục: `KingdomDefense`.
3. Mở dự án với phiên bản Unity 6 (`6000.6.7f1`).
4. Trong cửa sổ Project, truy cập đường dẫn:  
   `Assets/_KingdomDefense/Scenes/Battlefield.unity` và click đúp để mở Scene.
5. Nhấn nút **Play** tại thanh công cụ trên cùng để thưởng thức trò chơi ngay lập tức.

---

## HƯỚNG DẪN ĐÓNG GÓI BẢN BUILD (BUILD PIPELINE)

Dự án tích hợp sẵn công cụ tự động hóa đóng gói đa nền tảng qua C# Script:

### Cách 1: Đóng gói từ Unity Editor Menu
- Mở menu trên thanh công cụ Unity:
  - **`Kingdom Defense` -> `Build Windows (x64)`**: Đóng gói bản chạy độc lập Windows (`Builds/Windows/KingdomDefense.exe`).
  - **`Kingdom Defense` -> `Build Android (APK)`**: Đóng gói tệp cài đặt cho điện thoại Android (`Builds/Android/KingdomDefense.apk`).
  - **`Kingdom Defense` -> `Build 5 Enemy Prefabs`**: Tự động lắp ráp và lưu lại 5 Prefabs quái vật.

### Cách 2: Đóng gói từ Dòng lệnh (Batchmode CLI)
```bash
# Đóng gói bản chạy Windows
/path/to/unity/Editor/Unity -batchmode -quit -projectPath . -executeMethod KingdomDefense.Core.KingdomDefenseBuildPipeline.BuildWindowsCLI -logFile Builds/build_win.log

# Đóng gói file APK Android
/path/to/unity/Editor/Unity -batchmode -quit -projectPath . -executeMethod KingdomDefense.Core.KingdomDefenseBuildPipeline.BuildAndroidCLI -logFile Builds/build_android.log
```

---

## HỆ THỐNG KIỂM THỬ TỰ ĐỘNG (AUTOMATED TESTING)

Dự án được xây dựng theo chuẩn **Test-Driven Development (TDD)** với 100% mã nguồn kiểm thử đạt chuẩn NUnit Test Framework:

1. **`BalanceTests.cs`:** Kiểm tra độ cân bằng tiến trình 10 Wave, phân bổ kinh tế vàng và sự hiện diện của Boss Orcus.
2. **`AudioTests.cs`:** Kiểm tra quá trình sinh sóng âm thủ tục của Procedural Synthesizer và AudioManager.
3. **`EdgeCaseTests.cs`:** Kiểm tra các trường hợp góc chết:
   - Spam click mua/bán tháp trên cùng Node.
   - Bán nhiều tháp liên tiếp qua giao diện TowerUI.
   - Bán tháp khi đạn đang bay dở trên không.
   - Đảm bảo Boss Orcus sở hữu đầy đủ vật liệu, không bao giờ bị lỗi khối trắng.
4. **`CastleKnightTests.cs`:** Kiểm tra hành vi chặn đường, giao tranh và hồi sinh của hiệp sĩ đồng minh.
5. **`PerformanceTests.cs`:** Đo đạc hiệu năng render, đảm bảo giới hạn Draw Call và khung hình ổn định 60 FPS.

> Để chạy toàn bộ test trong Unity: Mở cửa sổ **`Window` -> `General` -> `Test Runner`** -> Click **`Run All`**.

---

## THÔNG TIN TÁC GIẢ & BẢN QUYỀN

- **Học phần:** Môi trường ảo (Virtual Environment)
- **Cơ sở đào tạo:** Học viện Kỹ thuật Quân sự (MTA)
- **Tác giả:** Nhóm sinh viên thực hiện đồ án môn học
- **Giấy phép (License):** Đồ án phục vụ mục đích nghiên cứu học thuật và giáo dục. Toàn bộ tài nguyên 3D mẫu thuộc bản quyền bộ KayKit Medieval Pack (CC0 Public Domain).

---
*Chúc bạn có những trải nghiệm thủ thành kịch tính cùng **Kingdom Defense**!*
