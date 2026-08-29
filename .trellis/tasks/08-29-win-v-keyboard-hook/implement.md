# Implementation Plan: Support Win+V Hotkey via Low-Level Keyboard Hook

## Implementation Steps

- [x] **Step 1: 扩展 `util/HotKeyManager.cs`**
  - 声明 `SetWindowsHookEx`、`UnhookWindowsHookEx`、`CallNextHookEx`、`GetAsyncKeyState`、`keybd_event` 等 Win32 P/Invoke。
  - 定义 `WH_KEYBOARD_LL`、`WM_KEYDOWN`、`WM_KEYUP`、`WM_SYSKEYDOWN`、`WM_SYSKEYUP`、`VK_LWIN`、`VK_RWIN` 等常量及 `KBDLLHOOKSTRUCT` 结构体。
  - 实现低级别键盘钩子回调函数 `HookCallback`：准确判定 `Win+V`，防开始菜单误弹，向目标窗口发送 `WM_HOTKEY`，吞噬按键事件。
  - 实现对外统一接口 `Register(IntPtr hWnd, int atom, int modifier, int key)` 和 `Unregister(IntPtr hWnd, int atom)`。
- [x] **Step 2: 更新 `Program.cs` 热键调用点**
  - 将启动初始化的 `HotKeyManager.RegisterHotKey` 替换为 `HotKeyManager.Register`。
  - 将退出清理的 `HotKeyManager.UnregisterHotKey` 替换为 `HotKeyManager.Unregister`。
  - 将 `SaveHotkey` 消息处理中的 `HotKeyManager.UnregisterHotKey` 和 `HotKeyManager.RegisterHotKey` 替换为 `HotKeyManager.Unregister` 和 `HotKeyManager.Register`。
- [x] **Step 3: 编译验证**
  - 使用官方 `dotnet-install.ps1` 成功安装 .NET 10.0.400 SDK 并配置用户环境变量。
  - 成功执行 `dotnet build` (Debug) 和 `dotnet build -c Release`，均为 0 警告、0 错误，生成 `ClipOne.exe`。
  - Native AOT 发布待系统安装 Visual Studio C++ 链接器环境 (`link.exe`) 后即可直接发布。
- [ ] **Step 4: 功能与回归验证**
  - 验证默认 `Win+V` 快捷键能正确唤起 ClipOne 且不触发系统剪贴板。
  - 验证切换其他快捷键（如 `Alt+V`）和切回 `Win+V` 均能正常工作。
