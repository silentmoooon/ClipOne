# PRD — AOT and Trimming Refactoring (System.Text.Json, WinRT/Win32 Clipboard, Photino)

## Goal

Refactor ClipOne to completely decouple from WPF runtime dependencies and reflection-based libraries (`Newtonsoft.Json`). Enable **Native AOT compilation** and **extreme binary trimming** on Windows x64 (.NET 10 / .NET 9), while maintaining all core clipboard history, instant search, format handling, and pasting capabilities.

## Background & Technical Constraints

- **Repository**: [ClipOne.csproj](file:///d:/dev/.personal/ClipOne/ClipOne.csproj), targeting `net10.0-windows10.0.26100.0` on `x64`.
- **Existing Limitations**:
  - `Newtonsoft.Json` relies heavily on runtime reflection and is incompatible with Native AOT without extensive trimming warnings / breaks.
  - `System.Windows.Clipboard`, `System.Windows.Media.Imaging`, and `System.Windows.Controls` tie the application directly to the massive WPF runtime (`UseWPF=true`), preventing Native AOT and lightweight binary output.
  - `H.NotifyIcon.Wpf` relies on WPF element bindings and resources.
  - `SetHotKeyForm.xaml` is a legacy WPF dialog for setting hotkeys.
- **Key Architectural Decisions**:
  - **JSON Engine**: `System.Text.Json` with source-generated `JsonSerializerContext`.
  - **Clipboard Engine**: Native WinRT (`Windows.ApplicationModel.DataTransfer.Clipboard`) and Win32 clipboard APIs for AOT-safe rich data handling (Text, Image/Base64, Files, HTML, WeChat, QQ rich text).
  - **Window Host**: `Photino.NET` with borderless, topmost, auto-hide popup behavior.
  - **Hotkey Settings UI**: In-page Web modal dialog inside `html/index.html` (Decision: Option A), eliminating the need for any secondary WPF or native dialogs.
  - **Tray & System Integration**: Win32 native `Shell_NotifyIcon` + Win32 Context Menu + `RegisterHotKey` message loop.

## Requirements

### 1. Zero-Reflection JSON Engine (`System.Text.Json`)
- **REQ-JSON-1**: Replace all `Newtonsoft.Json` calls in [ConfigService.cs](file:///d:/dev/.personal/ClipOne/service/ConfigService.cs), [StorageService.cs](file:///d:/dev/.personal/ClipOne/service/StorageService.cs), and web message bridges.
- **REQ-JSON-2**: Define `[JsonSerializable]` source generator context for `Config`, `ClipModel`, `List<ClipModel>`, and all JS bridge envelope payloads.
- **REQ-JSON-3**: Ensure backward and forward compatibility with existing `config/settings.json` and `config/history.json`.

### 2. AOT-Safe Native Clipboard Engine
- **REQ-CLIP-1**: Rewrite [ClipService.cs](file:///d:/dev/.personal/ClipOne/service/ClipService.cs) to remove all `System.Windows.*` namespaces.
- **REQ-CLIP-2**: Implement reading and writing for:
  - Plain text (`Text`)
  - HTML formatted text (`Html` with fragment extraction)
  - Files (`FileDrop` / StorageItems with Preferred DropEffect)
  - Images (JPEG/PNG/DIB to Base64 data URLs without WPF Imaging)
  - Custom Rich Text formats: WeChat (`WeChat_RichEdit_Format`) and QQ (`QQ_Unicode_RichEdit_Format`).
- **REQ-CLIP-3**: Maintain retry logic for clipboard access locks and support automatic GIF re-copy override.

### 3. Complete WPF Stripping & Photino Window Host
- **REQ-UI-1**: Remove `<UseWPF>true</UseWPF>`, `H.NotifyIcon.Wpf`, and delete all `.xaml` and `.xaml.cs` files ([App.xaml](file:///d:/dev/.personal/ClipOne/App.xaml), [view/MainWindow.xaml](file:///d:/dev/.personal/ClipOne/view/MainWindow.xaml), [view/SetHotKeyForm.xaml](file:///d:/dev/.personal/ClipOne/view/SetHotKeyForm.xaml)).
- **REQ-UI-2**: Create `Program.cs` entry point hosting `PhotinoWindow` configured for borderless, topmost, cursor-relative positioning, and auto-hiding when deactivated.
- **REQ-UI-3**: Implement native Win32 system tray icon (`Shell_NotifyIcon`) and popup context menu (Clear, Reload, Format filters, Skin picker, Theme mode, Hotkey setting, DevTools, Exit).
- **REQ-UI-4**: Implement global hotkey registration (`RegisterHotKey` / `WM_HOTKEY`) and clipboard listener (`AddClipboardFormatListener` / `WM_CLIPBOARDUPDATE`).
- **REQ-UI-5**: Embed the "Set Hotkey" configuration modal directly inside `html/index.html` and `html/js/main.js`, bridging configuration saves back to C#.

### 4. Native AOT & Extreme Trimming
- **REQ-AOT-1**: Configure `<PublishAot>true</PublishAot>` and `<TrimMode>full</TrimMode>` in `ClipOne.csproj`.
- **REQ-AOT-2**: Verify standalone single-binary release build completes with zero AOT analysis warnings.

## Acceptance Criteria

- [ ] `dotnet build` and `dotnet publish -c Release -r win-x64` succeed with Native AOT enabled.
- [ ] No WPF or XAML references remain in the repository.
- [ ] Pressing the configured global shortcut (e.g. `Win+V` or `Alt+V`) opens the popup at cursor position with instant response.
- [ ] Losing focus / clicking outside immediately hides the popup window.
- [ ] Copying text, files, images, HTML, WeChat messages, and QQ messages adds them to history and updates the Web UI in real-time.
- [ ] Clicking an item or pressing number shortcuts pastes the selected item directly into the active background application.
- [ ] System tray icon is functional with full right-click context menu options.
- [ ] Hotkey settings modal opens in the Web UI, allows configuring modifiers/keys, and successfully registers the new shortcut.
- [ ] Settings (`config/settings.json`) and history (`config/history.json`) persist correctly across application restarts.
