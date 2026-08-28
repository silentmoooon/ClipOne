# 美化托盘右键菜单原生深色支持 (Tray Context Menu Modern & Dark Mode)

## Goal

将 ClipOne 的托盘右键菜单从经典/旧版 Windows 纯白外观升级为 Windows 10 / 11 原生深色/浅色自适应菜单，并在菜单单选项（皮肤列表、主题模式）上使用现代单选圆点（Radio Checkmark），消除“Win98 / WinForms 原始白底灰边”的陈旧感，使其与 Windows 现代系统及 ClipOne 主题保持一致。

## Background & Current State

- 当前 [TrayIconManager.cs](file:///d:/dev/.personal/ClipOne/util/TrayIconManager.cs#L178-L285) 使用 `CreatePopupMenu` + `AppendMenuW` + `TrackPopupMenuEx` 弹出菜单。
- 宿主窗口为 [NativeMessageWindow.cs](file:///d:/dev/.personal/ClipOne/util/NativeMessageWindow.cs)。
- 默认情况下，未开启 UxTheme 暗色策略与沉浸式暗色属性的 Win32 消息窗口，其弹出的弹出菜单始终呈现为经典 Win32 白底菜单，且即使系统处于深色模式也不会变暗。
- 多选/单选标记目前均使用 `MF_CHECKED`，导致皮肤切换和主题切换显示为勾选框（Checkmark）而非单选点（Radio Item）。

## Requirements

1. **暗色/浅色自适应 (UxTheme Dark Mode Integration)**:
   - 封装 `uxtheme.dll` 内部 API（Ordinal 135: `SetPreferredAppMode` / `AllowDarkModeForApp`，Ordinal 133: `AllowDarkModeForWindow`，Ordinal 136: `FlushMenuThemes`）。
   - 在程序启动及右键菜单弹出前，根据当前 ClipOne 设置的 `ThemeMode`（`System` / `Light` / `Dark`）动态配置应用与窗口的暗色模式，并刷新菜单主题缓存。
   - 在 Windows 10 (1809+) 和 Windows 11 上自动生效。
2. **菜单项单选样式优化 (Radio Menu Items)**:
   - 对“皮肤”子菜单和“主题模式”子菜单等单选项，使用 Win32 `CheckMenuRadioItem` 替代原有的 `MF_CHECKED` 方框勾选，呈现更规范的圆形单选点。
3. **沉浸式深色窗口属性 (DWM Immersive Dark Mode)**:
   - 对消息窗口应用 `DWMWA_USE_IMMERSIVE_DARK_MODE` (20 / 19)，确保窗口与其托管的上下文菜单完全一致。
4. **Native AOT 与系统兼容性**:
   - 所有 Win32 / UxTheme API 均通过安全的动态解析（`GetProcAddress` 优雅降级），在旧版或不支持的环境中无缝回退，不产生异常，保证 Native AOT 零反射与稳定性。

## Acceptance Criteria

- [x] **深色模式测试**：在 Windows 处于深色模式或 ClipOne 设为 Dark 时，右键托盘图标弹出的菜单具有黑色/深灰色背景及现代高亮条。
- [x] **浅色模式测试**：在 Windows 处于浅色模式或 ClipOne 设为 Light 时，菜单正常显示为浅色。
- [x] **单选标记测试**：“皮肤”子菜单与“主题模式”子菜单中的当前选中项呈现圆点单选样式（Radio Item），切换后正确定位。
- [x] **开机自启复选框**：“开机自启”项保留标准对勾复选框（Checkmark）。
- [x] **构建验证**：`dotnet build` 及 `dotnet build -c Release` 正常通过。

## Out of Scope

- 引入额外的大型 UI 框架或重构为 Web 浮层（保持原生 Win32 的轻量零开销）。
