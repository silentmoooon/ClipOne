# Implementation Plan — AOT and Trimming Refactoring

## Ordered Checklist

### Phase 1: Eliminate Reflection (System.Text.Json)
- [ ] **1.1**: Define `ClipJsonContext.cs` with source-generated serializer options for `Config`, `ClipModel`, `List<ClipModel>`, and JS message envelopes.
- [ ] **1.2**: Update [service/ConfigService.cs](file:///d:/dev/.personal/ClipOne/service/ConfigService.cs) to use `System.Text.Json` via `ClipJsonContext`.
- [ ] **1.3**: Update [service/StorageService.cs](file:///d:/dev/.personal/ClipOne/service/StorageService.cs) to use `System.Text.Json` via `ClipJsonContext`.
- [ ] **1.4**: Remove `Newtonsoft.Json` PackageReference from [ClipOne.csproj](file:///d:/dev/.personal/ClipOne/ClipOne.csproj).

### Phase 2: Native Win32 / WinRT Clipboard Engine
- [ ] **2.1**: Implement native clipboard format constants and helper structs in [util/WinAPIHelper.cs](file:///d:/dev/.personal/ClipOne/util/WinAPIHelper.cs).
- [ ] **2.2**: Rewrite [service/ClipService.cs](file:///d:/dev/.personal/ClipOne/service/ClipService.cs) with native Win32/WinRT clipboard APIs:
  - Text & UnicodeText
  - HTML format fragment parser
  - File drop lists (HDROP)
  - Image / DIB / JPEG conversions to Base64 (using native GDI+ / WIC or pure AOT-safe image handling)
  - Custom WeChat and QQ rich text formats
- [ ] **2.3**: Verify clipboard read/write roundtrip for all supported formats without any WPF dependencies.

### Phase 3: Decouple WPF & Implement Photino Window Host
- [ ] **3.1**: Implement native Win32 System Tray & Context Menu manager (`TrayIconManager.cs`).
- [ ] **3.2**: Add in-page Hotkey Settings Modal to [html/index.html](file:///d:/dev/.personal/ClipOne/html/index.html) and [html/js/main.js](file:///d:/dev/.personal/ClipOne/html/js/main.js).
- [ ] **3.3**: Create [Program.cs](file:///d:/dev/.personal/ClipOne/Program.cs) with:
  - Single-instance Mutex check
  - Global unhandled exception logging
  - PhotinoWindow initialization (chromeless, topmost, auto-hide on blur)
  - Native Win32 message loop / hook for `WM_CLIPBOARDUPDATE` and `WM_HOTKEY`
  - Web message handler bridging JS calls to C# methods
- [ ] **3.4**: Delete all WPF artifacts:
  - [App.xaml](file:///d:/dev/.personal/ClipOne/App.xaml) & [App.xaml.cs](file:///d:/dev/.personal/ClipOne/App.xaml.cs)
  - [view/MainWindow.xaml](file:///d:/dev/.personal/ClipOne/view/MainWindow.xaml) & [view/MainWindow.xaml.cs](file:///d:/dev/.personal/ClipOne/view/MainWindow.xaml.cs)
  - [view/SetHotKeyForm.xaml](file:///d:/dev/.personal/ClipOne/view/SetHotKeyForm.xaml) & [view/SetHotKeyForm.xaml.cs](file:///d:/dev/.personal/ClipOne/view/SetHotKeyForm.xaml.cs)
- [ ] **3.5**: Clean up [ClipOne.csproj](file:///d:/dev/.personal/ClipOne/ClipOne.csproj):
  - Remove `<UseWPF>true</UseWPF>`
  - Remove `H.NotifyIcon.Wpf`
  - Add `Photino.NET` PackageReference
  - Configure `<PublishAot>true</PublishAot>` and trimming properties.

### Phase 4: Verification & AOT Build
- [ ] **4.1**: Test Debug build: verify hotkey trigger, popup positioning, auto-hide, copy/paste, tray menu, hotkey modal, skin/theme switching.
- [ ] **4.2**: Run `dotnet publish -c Release -r win-x64` to verify successful Native AOT compilation with zero trim warnings.

## Validation Commands

```powershell
# 1. Build and test Debug
dotnet build -c Debug

# 2. Run Debug
dotnet run

# 3. Publish Native AOT Release
dotnet publish -c Release -r win-x64
```
