# Design: Support Win+V Hotkey via Low-Level Keyboard Hook

## Architecture & Responsibilities

将底层键盘钩子逻辑与原有的 `RegisterHotKey` 统一封装进 `HotKeyManager`。上层 `Program.cs` 无需感知底层是通过 Hook 还是 `RegisterHotKey` 注册的，只需调用统一方法。

```mermaid
graph TD
    A[用户/系统配置] --> B{是否为 Win+V?}
    B -- 是 (Mod=8, Key=86) --> C[SetWindowsHookEx WH_KEYBOARD_LL]
    B -- 否 --> D[Win32 RegisterHotKey]
    
    C -- 用户按下 Win+V --> E[LowLevelKeyboardProc]
    E --> F[拦截并吞噬 V 按键 (return 1)]
    E --> G[触发防开始菜单虚拟按键]
    E --> H[PostMessage WM_HOTKEY 到 MsgWindow]
    
    D -- 用户按下热键 --> I[Windows 自动向 MsgWindow 发送 WM_HOTKEY]
    
    H --> J[Program.OnNativeMessage]
    I --> J
    J --> K[ShowPopupWindow 唤起剪贴板窗口]
```

## Detailed Component Design

### 1. `HotKeyManager.cs` 职责扩展

- **新增 Win32 API 导入**：
  - `SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId)`
  - `UnhookWindowsHookEx(IntPtr hhk)`
  - `CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam)`
  - `GetAsyncKeyState(int vKey)`
  - `keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo)`
- **结构体与常量**：
  - `WH_KEYBOARD_LL = 13`
  - `WM_KEYDOWN = 0x0100`, `WM_KEYUP = 0x0101`
  - `WM_SYSKEYDOWN = 0x0104`, `WM_SYSKEYUP = 0x0105`
  - `VK_LWIN = 0x5B`, `VK_RWIN = 0x5C`
  - `VK_CONTROL = 0x11`, `VK_MENU = 0x12`, `VK_SHIFT = 0x10`, `VK_V = 0x56`
  - `KBDLLHOOKSTRUCT` 结构体
- **内部状态管理**：
  - `private static IntPtr _hookId = IntPtr.Zero;`
  - `private static LowLevelKeyboardProc? _hookProc;` (静态引用，防止 GC 回收)
  - `private static IntPtr _targetHwnd = IntPtr.Zero;`
  - `private static int _targetAtom = 0;`
- **公共接口**：
  - `public static bool Register(IntPtr hWnd, int atom, int modifier, int key)`
    - 检查旧状态，执行清理。
    - 若 `modifier == (int)KeyModifiers.WindowsKey && key == 86`：
      - 安装 `WH_KEYBOARD_LL` 钩子。
      - 记录 `_targetHwnd` 和 `_targetAtom`。
      - 成功返回 `true`。
    - 否则：
      - 调用 `RegisterHotKey(hWnd, atom, modifier, key)` 并返回其布尔结果。
  - `public static void Unregister(IntPtr hWnd, int atom)`
    - 若 `_hookId != IntPtr.Zero`，调用 `UnhookWindowsHookEx` 并重置为 `IntPtr.Zero`。
    - 总是调用 `UnregisterHotKey(hWnd, atom)` 清理可能存在的系统热键。

### 2. 键盘钩子处理函数细节 (`LowLevelKeyboardProc`)

1. **`nCode >= 0` 判定**：若 `< 0` 直接传给 `CallNextHookEx`。
2. **按键匹配**：
   - 检查按键是否为 `'V'` (`vkCode == 0x56`)。
   - 检查 `Win` 键状态：`GetAsyncKeyState(VK_LWIN)` 或 `GetAsyncKeyState(VK_RWIN)` 最高位为 1。
   - 检查其他修饰键状态：`Ctrl`, `Alt`, `Shift` 最高位均为 0。
3. **按下处理 (`WM_KEYDOWN` 或 `WM_SYSKEYDOWN`)**：
   - 触发防开始菜单虚拟按键：`keybd_event(0x07, 0, 0, UIntPtr.Zero); keybd_event(0x07, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);` (0x07 为未分配虚键码，不会产生字符或副作用，但会告诉 Windows 此前已有击键发生，从而在松开 Win 键时不弹出开始菜单)。
   - 发送通知：`WinAPIHelper.PostMessage(_targetHwnd, WM_HOTKEY, (IntPtr)_targetAtom, IntPtr.Zero);`。
   - 吞噬事件：返回 `(IntPtr)1`，防止 Windows 系统内置剪贴板历史窗口响应。
4. **抬起处理 (`WM_KEYUP` 或 `WM_SYSKEYUP`)**：
   - 若 `V` 且 `Win` 按下，同样返回 `(IntPtr)1` 吞噬抬起消息，防止按键漏入当前焦点窗口。

### 3. `Program.cs` 接入

- 启动时注册：
  将 `HotKeyManager.RegisterHotKey(...)` 改为 `HotKeyManager.Register(_msgWindow.Handle, _hotkeyAtom, _config.HotkeyModifier, _config.HotkeyKey)`。
- 退出时注销：
  将 `HotKeyManager.UnregisterHotKey(...)` 改为 `HotKeyManager.Unregister(_msgWindow.Handle, _hotkeyAtom)`。
- 动态保存热键 (`SaveHotkey`)：
  使用 `HotKeyManager.Unregister` + `HotKeyManager.Register`。
  保持热键保存失败回滚逻辑不变。

## 兼容性与稳定性分析

- **Native AOT 裁剪安全性**：
  静态字段持久化保存委托引用，不使用反射，类型与结构体使用显式 P/Invoke 布局。
- **线程安全性与性能**：
  `WH_KEYBOARD_LL` 回调在主线程消息泵执行，内部逻辑极简（仅状态检查与非阻塞 `PostMessage`），耗时微秒级，不会造成系统输入卡顿。
