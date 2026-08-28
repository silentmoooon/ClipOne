# Technical Design: 前端现代化与全套重构

## 1. 架构与状态模型 (Architecture & State Management)

在 `html/js/main.js` 中创建命名空间单例对象 `ClipApp`，结构如下：

```javascript
const ClipApp = {
    // 状态
    state: {
        clips: [],          // 所有剪贴板记录 ClipModel[]
        maxRecords: 300,    // 最大存储限制
        searchMode: false,  // 是否搜索中
        searchValue: "",    // 当前搜索词
        selectIndex: 0,     // 当前选中的项在当前列表中的索引
        filteredIndices: [],// 搜索匹配项在 clips 中的真实索引映射
        isShiftPressed: false,
        rangeStartIndex: -1,
        rangeEndIndex: -1,
        isCtrlPressed: false,
        multiIndexList: [], // 多选索引
        bounceOffset: 0,
        bounceTimer: null,
        isBouncing: false
    },
    
    // 初始化与事件绑定
    init(),
    bindEvents(),
    
    // 渲染子系统
    render(),
    renderTable(filteredClips),
    updateSelection(newIndex, scroll = true),
    
    // 交互与导航
    handleKeyDown(e),
    handleKeyUp(e),
    handleMouseAction(e, targetRow),
    navigate(delta),
    
    // 业务动作
    pasteSingle(index),
    pasteRange(start, end),
    pasteMulti(),
    setToClipboard(index),
    deleteItem(index),
    
    // 弹窗与托盘
    show(),
    toggleSearch(forceState),
    openHotkeyModal(mod, key),
    showTrayMenu(data),
    
    // 通信桥梁
    post(cmd, data) {
        window.chrome.webview.postMessage(data ? `${cmd}|${data}` : `${cmd}|1`);
    }
};
```

## 2. 键盘导航状态机 (Keyboard State Machine)

键盘输入统一在 `ClipApp.handleKeyDown` 中以 `e.key` 判断处理：

```
[Normal Mode]
  ├── ArrowDown / ArrowUp: navigate(+1) / navigate(-1) -> updateSelection() -> scrollIntoViewIfNeeded()
  ├── PageDown / PageUp: navigate(+5) / navigate(-5)
  ├── Home / End: select first item / select last item
  ├── Enter: pasteSingle(selectIndex)
  ├── Space: pasteSingle(0)
  ├── Delete / Backspace: deleteItem(selectIndex)
  ├── 1-9: pasteSingle(key - 1)
  ├── A-Z: pasteSingle(keyIndex)
  ├── Ctrl + F: toggleSearch(true)
  └── Escape: post("esc")

[Search Mode]
  ├── ArrowDown / ArrowUp: 在过滤后的匹配项中切换当前高亮
  ├── Enter: 粘贴当前高亮过滤项
  ├── Escape: 优先清空并关闭搜索框，恢复普通模式
  └── 输入文字: 触发 80ms 防抖更新过滤列表并重置高亮至首项
```

## 3. 主题与 CSS 变量设计 (Theming & CSS Variables)

在 `common.css` 中声明默认语义化 CSS 变量（以 Stand/Classic 为基准），在各个皮肤文件夹（`fluent-dark.css`、`fluent-light.css`、`material.css`、`modern.css`、`striped.css` 等）中直接覆写变量：

```css
:root {
    --bg-body: #ffffff;
    --bg-card: #ffffff;
    --bg-card-hover: #e6f3ff;
    --bg-card-selected: #0078d7;
    --bg-modal-overlay: rgba(0, 0, 0, 0.45);
    --bg-modal-card: #ffffff;
    --bg-input: #ffffff;
    --text-primary: #222222;
    --text-secondary: #666666;
    --text-selected: #ffffff;
    --border-color: #d1d1d1;
    --border-card: rgba(0, 0, 0, 0.08);
    --accent-color: #0078d4;
    --accent-hover: #106ebe;
    --shadow-card: 0 2px 8px rgba(0, 0, 0, 0.1);
    --shadow-modal: 0 10px 30px rgba(0, 0, 0, 0.25);
    --font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, "Microsoft YaHei", sans-serif;
}
```

- `#hotkeyModal` 和 `#trayMenuModal` 完全引用这些变量，不再包含硬编码背景/文字颜色。
- 无论切换到浅色（如 `fluent-light`）还是深色（如 `fluent-dark`），所有弹窗、表单控件、按钮均自动保持统一设计风格。

## 4. 安全与 XSS 防御设计 (Security Design)

- 纯文本内容自动采用标准安全转义渲染（`&lt;`、`&gt;`、`&amp;`）。
- 微信表情转换正则在安全转义后进行安全替换（仅允许预置映射的 `data:image/png;base64`）。
- 移除直接的全局 `on*` 注入，使用规范的 DOM 属性与事件委托。
