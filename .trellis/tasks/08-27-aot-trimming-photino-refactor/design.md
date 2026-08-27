# Technical Design — AOT and Trimming Refactoring

## Architecture Overview

ClipOne is refactored from a WPF-hosted WebView2 application into a lightweight **Native AOT C# application** powered by **Photino.NET** (or native Win32 + WebView2) and direct **Win32 / WinRT APIs**.

```
┌──────────────────────────────────────────────────────────────┐
│                       ClipOne App Entry                       │
│                         (Program.cs)                          │
├───────────────┬───────────────────────────────┬──────────────┤
│ Single-Instance│  Win32 Message Pump / Hooks   │ Native Tray  │
│  Mutex Guard  │  (Hotkeys & Clipboard Listener)│ (Shell_Notify)│
└───────┬───────┴───────────────┬───────────────┴──────┬───────┘
        │                       │                      │
┌───────▼───────────────────────▼──────────────────────▼───────┐
│                     Photino.NET Window                       │
│    - Borderless, Topmost, Auto-hide on blur                  │
│    - Renders html/index.html (HTML5/CSS3/jQuery UI)          │
│    - In-page Hotkey Settings Modal (Option A)                │
└───────────────────────────────┬──────────────────────────────┘
                                │ Web Message Bridge (AOT JSON)
┌───────────────────────────────▼──────────────────────────────┐
│                    ClipJsonContext (STJ)                     │
│           (Source-generated JSON Serialization)              │
├───────────────────────────────┬──────────────────────────────┤
│         ConfigService         │        StorageService        │
│     (config/settings.json)    │     (config/history.json)    │
└───────────────────────────────┴──────────────────────────────┘
                                │
┌───────────────────────────────▼──────────────────────────────┐
│                    Native ClipService                        │
│   (WinRT DataTransfer + Win32 Clipboard Formats / Hooks)     │
│   - Text / UnicodeText                                       │
│   - Image / DIB / Base64                                     │
│   - FileDrop / HDROP                                         │
│   - HTML Format Fragment Parser                              │
│   - WeChat & QQ RichEdit Formats                             │
└──────────────────────────────────────────────────────────────┘
```

## Component Details

### 1. JSON Serialization Layer (`ClipJsonContext.cs`)
- Implement `[JsonSourceGenerationOptions(WriteIndented = true, PropertyNamingPolicy = JsonKnownNamingPolicy.Unspecified)]`.
- Register:
  - `Config`
  - `ClipModel`
  - `List<ClipModel>`
  - Bridge messages: `JsEnvelope`, `HotkeyConfigDto`, `HistoryPayload`.
- No reflection paths (`JsonSerializer.Serialize` / `Deserialize` with type info parameters only).

### 2. Native Clipboard Engine (`ClipService.cs`)
- Replace WPF `Clipboard` and `System.Windows.Media.Imaging` with direct Win32 and WinRT calls.
- **Reading**:
  - `OpenClipboard` / `GetClipboardData` / `CloseClipboard` with retry loop (3 attempts).
  - Native format IDs:
    - `CF_UNICODETEXT` (13)
    - `CF_HDROP` (15)
    - `CF_DIB` (8) / `CF_DIBV5` (17) / `CF_BITMAP` (2)
    - `RegisterClipboardFormat("HTML Format")`
    - `RegisterClipboardFormat("WeChat_RichEdit_Format")`
    - `RegisterClipboardFormat("QQ_Unicode_RichEdit_Format")`
    - `RegisterClipboardFormat("Preferred DropEffect")`
- **Writing**:
  - Native global memory allocation (`GlobalAlloc`, `GlobalLock`, `GlobalUnlock`, `SetClipboardData`).
  - Seamless support for copying back complex data types when pasting into target window.
- **Pasting Emulation**:
  - Using `SendInput` in [KeyboardKit.cs](file:///d:/dev/.personal/ClipOne/util/KeyboardKit.cs) (already P/Invoke based, pure C#).

### 3. Application Lifecycle & Window Host (`Program.cs`)
- **Single Instance**: `new Mutex(true, "ClipOne_Unique_Application_Mutex", out bool createdNew)`.
- **Photino Window Configuration**:
  - `SetTitle("ClipOne")`
  - `SetUseOsDefaultSize(false)` / `SetSize(width, height)`
  - `SetTopmost(true)`
  - `SetChromeless(true)` / borderless
  - `SetContextMenuEnabled(false)`
  - `SetDevToolsEnabled(false)` (toggleable from tray)
- **Window Positioning & Behavior**:
  - Track active foreground window prior to popup activation.
  - Position window relative to mouse cursor using `GetCursorPos` and virtual screen bounds.
  - Deactivation hook: Listen to `WM_ACTIVATE` / `WM_KILLFOCUS` or Photino window events to trigger `DiyHide()`.

### 4. Native System Tray (`TrayIconManager.cs`)
- Native Win32 `Shell_NotifyIconW` with custom tray window procedure.
- Context menu built via Win32 `CreatePopupMenu` and `TrackPopupMenuEx`:
  - 清空 (Clear)
  - 刷新 (Reload)
  - 格式 (Formats: QQ, HTML, Image, File, Text)
  - 皮肤 (Skins: Fluent, Modern, Classic, Material, etc.)
  - 主题模式 (Theme: System, Light, Dark)
  - 热键设置 (Hotkey Settings -> triggers Web Modal)
  - 开机自启 (Auto Startup toggle via Registry)
  - 开发者工具 (DevTools)
  - 退出 (Exit)

### 5. Web UI Hotkey Settings Modal
- In [html/index.html](file:///d:/dev/.personal/ClipOne/html/index.html):
  - Add a styled modal container (`#hotkeyModal`) matching current theme tokens.
  - Checkboxes for `Alt`, `Ctrl`, `Shift`, `Win`.
  - Dropdown / key picker for `A-Z`.
  - "保存" (Save) and "取消" (Cancel) buttons.
- In [html/js/main.js](file:///d:/dev/.personal/ClipOne/html/js/main.js):
  - Handle `showHotkeySettings` message from C#.
  - Post `SaveHotkey|{"modifier": 9, "key": 86}` back to C# backend.

## Migration & Compatibility Notes
- Existing user settings in `config/settings.json` and history in `config/history.json` remain 100% compatible.
- All skins and CSS stylesheets under `html/css/` are fully preserved.
- Zero runtime dependency on desktop WPF frameworks (`Microsoft.NETCore.App` + native Windows SDK projections).
