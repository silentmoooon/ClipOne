# Support Win+V Hotkey via Low-Level Keyboard Hook

## Goal

允许用户在 Windows 10/11 系统中将 ClipOne 的唤起全局快捷键设置为系统默认占用的 `Win+V`。当快捷键为 `Win+V` 时，使用 `SetWindowsHookEx(WH_KEYBOARD_LL)` 低级别键盘钩子拦截并触发剪贴板历史窗口；当快捷键为其他按键组合时，保持使用原有的 Win32 `RegisterHotKey` 机制。

## Background & Current State

1. Windows 系统内置了剪贴板历史记录功能，独占了全局 `Win+V` 热键，导致普通 Win32 API `RegisterHotKey` 注册 `Win+V` 时直接失败。
2. ClipOne 默认配置中的快捷键正是 `HotkeyModifier = 8` (`WindowsKey`), `HotkeyKey = 86` ('V')。由于 `RegisterHotKey` 失败，导致首次安装或默认配置下 `Win+V` 无法唤起 ClipOne。
3. 当前热键注册和注销分散在 `Program.cs` 的启动初始化、窗口关闭 (`OnWindowClosing`) 和热键修改保存 (`SaveHotkey`) 中，直接调用 `HotKeyManager.RegisterHotKey` / `UnregisterHotKey`。

## Requirements

- **REQ-HK-1 (智能双轨注册)**：
  - 当配置或用户设置的热键组合为 `Win + V` (`Modifier == 8` 且 `Key == 86`) 时，使用 Win32 `SetWindowsHookEx(WH_KEYBOARD_LL)` 安装全局低级别键盘钩子。
  - 当热键为任何其他组合时，使用原有的 `RegisterHotKey`。
- **REQ-HK-2 (Win+V 拦截与消歧)**：
  - 在底层键盘钩子中检测到 `Win + V` 按下时：
    1. 阻断该按键事件向系统传递（返回 1），避免同时弹出 Windows 自带的剪贴板窗口。
    2. 触发虚拟事件防误触，避免用户松开 Win 键时意外触发 Windows 开始菜单。
    3. 异步向 `NativeMessageWindow` 发送 `WM_HOTKEY` 消息，统一通过 `OnNativeMessage` 调用 `ShowPopupWindow()`。
  - 严格检查仅在修饰键为 Win（无 Ctrl/Alt/Shift 干扰）且按键为 V 时触发。
- **REQ-HK-3 (热键动态切换与注销)**：
  - 提供统一的 `HotKeyManager.Register` 和 `HotKeyManager.Unregister` 封装，自动处理钩子与 `RegisterHotKey` 之间的无缝切换。
  - 当用户在前端设置界面将快捷键从 `Win+V` 更改为其他快捷键（如 `Alt+V`）时，自动卸载钩子并重新注册 `RegisterHotKey`。
  - 当从其他快捷键改回 `Win+V` 时，自动注销 `RegisterHotKey` 并安装钩子。
  - 应用程序退出时，确保钩子和热键均被干净注销。
- **REQ-HK-4 (Native AOT 与 GC 兼容)**：
  - 键盘钩子委托需由静态强引用持有，避免在 Native AOT 与 Full Trim 环境下因 GC 回收委托导致 Access Violation 崩溃。

## Out of Scope

- 修改前端热键设置 UI 的交互逻辑或外观。
- 支持其他非 Win+V 且被系统占用的特殊快捷键（如 Win+L 等）。

## Acceptance Criteria

- [x] 默认配置下（`Win+V`），启动 ClipOne 后按下 `Win+V` 可正常弹出 ClipOne 剪贴板窗口，且不弹出 Windows 自带的剪贴板面板。
- [x] 松开 `Win` 键时，不会错误弹出 Windows 开始菜单。
- [x] 在设置界面将热键修改为 `Alt+V`（或 `Ctrl+Shift+V` 等），成功切换为 Win32 `RegisterHotKey` 方式，按下新热键可唤起窗口，`Win+V` 不再唤起。
- [x] 在设置界面重新修改回 `Win+V`，成功切回 `SetWindowsHookEx` 方式，`Win+V` 可再次正常唤起。
- [x] 应用程序关闭退出时，钩子被正确卸载，无资源泄漏或进程残留。
- [x] `dotnet build` 与 `dotnet build -c Release` 编译通过，无修剪或 P/Invoke 告警。
