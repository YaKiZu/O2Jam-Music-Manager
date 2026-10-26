# O2Jam Music Manager 🎵

<p align="center">
  <img src="O2JamManager/app_icon.png" alt="O2Jam Music Manager Logo" width="128" height="128" />
</p>

<p align="center">
  <strong>A high-performance, modern, and portable Windows tool to manage, preview, edit metadata, and safely reindex O2Jam (<code>.ojn</code> / <code>.ojm</code>) music charts.</strong>
</p>

<p align="center">
  <img src="https://img.shields.io/badge/Platform-Windows%20x64-blue?style=for-the-badge&logo=windows" alt="Platform" />
  <img src="https://img.shields.io/badge/.NET-8.0%20WPF-purple?style=for-the-badge&logo=dotnet" alt=".NET 8" />
  <img src="https://img.shields.io/badge/Release-v1.0.0-green?style=for-the-badge" alt="Release" />
  <img src="https://img.shields.io/badge/License-MIT-orange?style=for-the-badge" alt="License" />
</p>

---

## 📥 Download Portable Version (Windows x64 - No Install Required)

Get the latest release ready-to-run with zero dependencies or installation:

[![Download Portable Zip](https://img.shields.io/badge/Download-O2JamMusicManager__Portable__v1.0.0.zip-f43f5e?style=for-the-badge&logo=github&logoColor=white)](../../releases/latest/download/O2JamMusicManager_Portable_v1.0.0.zip)

> 🚀 **Quick Start**:
> 1. Download **[O2JamMusicManager_Portable_v1.0.0.zip](../../releases/latest/download/O2JamMusicManager_Portable_v1.0.0.zip)**.
> 2. Extract the archive anywhere on your PC.
> 3. Double-click `O2JamMusicManager.exe` to launch immediately!
> 
> You can also browse all version notes on the **[Releases Page](../../releases/latest)**.

---

## ✨ Features

### ⚡ 60+ FPS Hardware-Accelerated DataGrid
- **Instant Virtualization**: Engineered with custom `Mode=OneTime` bindings and lightweight POCO architecture for silky smooth scrolling across thousands of charts.
- **Natural File Sorting**: Numerical awareness when sorting filename headers (e.g., `o2ma2` appears before `o2ma10`).
- **Real-Time Search & Filters**: Instant full-text filtering across Title, Artist, Noter, and Song ID, along with status filters (`.OJM Present`, `.OJM Missing`).

### 📝 Complete `.OJN` Header Metadata Editor
- Directly modify `.ojn` binary headers: **Title, Artist, Noter, Genre, BPM**, and **Difficulty Levels (EX / NX / HX)**.
- Real-time in-memory updates with instantaneous row reflection upon clicking **"Save Changes"**.

### 🖼️ Cover & Pixel-Art Thumbnail Changer
- **Cover Image (800x600)**: Extract, view, and replace with JPEG images automatically converted and optimized for O2Jam.
- **Crisp Thumbnail (80x80)**: Replace or generate dedicated 80x80 pixel-art bitmaps with nearest-neighbor resampling for maximum sharpness.
- **Export Utility**: Export existing embedded covers to standard `.jpg` / `.bmp` files.

### 🎧 Built-in Audio Player & `.OJM` Sound Engine
- Integrated audio preview for O2Jam charts with real-time waveform decoding (M30 and WAV sample bank reconstruction via NAudio & NVorbis).
- Interactive seeker bar, playback timer, and volume control (defaulted to 50%).

### 🔢 Instant "Rename ID"
- Change song identifiers on the fly (`o2maXXXX.ojn` / `o2maXXXX.ojm`).
- Automatically syncs internal `.ojn` header IDs and physical file names with collision safety checks.

### 🛡️ Dual Auto-Index Engine
- **Full Reindex (0-9999)**: Sequential ID reassignment starting from any chosen index with customizable step.
- **Custom Auto-Index (1000-9999)**: Safely organizes user-imported custom charts (`ID >= 1000`) while **strictly locking and protecting official charts (`ID 0-999`)**.
- Automatic atomic file renaming with temporary collision-free passes.

### 📋 One-Click `OJNList.dat` Generator
- Scan and compile accurate `OJNList.dat` binary index files directly compatible with O2Jam clients and offline emulators.

### 🎨 Modern Cyberpunk Dark Theme
- Tailored dark palette (`#0e0e1a` / `#6c5ce7` / `#f43f5e`) matching Windows 11 immersive dark mode with rounded thumb scrollbars.

---

## ⌨️ Keyboard Shortcuts

| Shortcut | Action |
| :--- | :--- |
| `Space` | Play / Stop selected chart audio preview |
| `Alt + Up` | Move selected chart up in order |
| `Alt + Down` | Move selected chart down in order |
| `Del` | Delete selected chart (.ojn + .ojm) with confirmation |
| `F5` / `Ctrl + R` | Refresh current folder |
| `Ctrl + F` | Focus search bar |
| `Enter` | Save metadata changes |

---

## 🛠️ Building from Source

### Prerequisites
- Windows 10/11 (x64)
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

### Clone & Build
```powershell
git clone https://github.com/YaKiZu/O2Jam-Music-Manager.git
cd O2Jam-Music-Manager

# Build and run tests
dotnet test O2JamManager.sln

# Run development build
dotnet run --project O2JamManager/O2JamManager.csproj

# Publish standalone single-file binary
dotnet publish O2JamManager/O2JamManager.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=false -p:IncludeNativeLibrariesForSelfExtract=true -p:PublishReadyToRun=true -o publish_standalone
```

---

## 📄 License

This project is licensed under the [MIT License](LICENSE).
Feel free to use, modify, and distribute for the O2Jam community!
