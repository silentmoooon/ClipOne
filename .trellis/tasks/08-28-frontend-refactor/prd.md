# PRD: 前端现代化与全套重构 (Frontend Modernization & Full Refactoring)

## Goal

对 ClipOne 的前端 Web UI（`html/index.html`、`html/js/main.js`、`html/css/*`）进行彻底的现代化重构。在**零外部第三方 JS 依赖（去 jQuery 化）**、**轻量高效原生 ES6+** 的基础上，实现**完整的键盘方向键与快捷导航**、**高效 DOM 增量/防抖渲染**、**全套深浅色主题 CSS 变量无缝联动**与**XSS/安全防御**，显著提升剪贴板主窗口与托盘菜单的启动性能、内存占用和交互丝滑度。

---

## Confirmed Facts & Technical Constraints

1. **宿主环境**：Windows 11 / 10 + Photino.NET 4.0.x（内部为最新 WebView2 运行时，完全原生支持现代 ES2022+、CSS Variables、CSS Flex/Grid、`scrollIntoView` 等现代 Web 特性）。
2. **通信协议**：
   - JS 发送给 C#：`window.chrome.webview.postMessage("Command|payload")`。
   - C# 发送给 JS：`window.chrome.webview.addEventListener('message', e => ...)`，消息类型包括 `history`、`add`、`hotkeySettings`、`show`、`showTrayMenu`、`changeSkin`。
   - 握手协议：页面加载完毕必须发送 `window.chrome.webview.postMessage("ready|1")`。
3. **资源依赖**：
   - 构建时 `html/**` 全部复制到输出目录 (`CopyToOutputDirectory=Always`)。
   - 移除 jQuery (`jquery-3.3.1.min.js`) 可以直接节省 87KB 静态体积并加快页面首次解析执行。

---

## Requirements

### 1. 架构原生化与去 jQuery (Zero Dependency)
- [REQ-1.1] 彻底移除 `jquery-3.3.1.min.js`，将所有选择器、事件绑定、DOM 操作、动画改写为原生标准 Web API。
- [REQ-1.2] 将全局散落变量封装至模块化单例对象 `ClipApp` 中，避免全局命名空间污染，状态结构清晰可控。
- [REQ-1.3] 废弃内联 HTML 事件监听（`onmouseup`、`onmouseenter`），改由父级容器事件委托（Event Delegation）统一调度。

### 2. 键盘全功能导航补齐 (Keyboard Navigation & UX)
- [REQ-2.1] **方向键导航**：支持 `ArrowUp`、`ArrowDown` 在正常模式与搜索模式下上下移动选中项，自动滚动（`scrollIntoView`）保持视野可见。
- [REQ-2.2] **快速翻页与定位**：支持 `PageUp`、`PageDown`、`Home`、`End` 快速定位首尾或大跨度跳跃。
- [REQ-2.3] **常规快捷键保持与增强**：
  - 数字键 `1-9` / 字母键 `A-Z` 直接粘贴对应项；
  - `Space` 直接粘贴第 0 项（首项）；
  - `Enter` 粘贴当前高亮选中的项；
  - `Delete` / `Backspace` 删除当前选中的项；
  - `Ctrl + F` 切换呼出/隐藏搜索栏；
  - `Escape` 优先退出搜索状态，再次按退出隐藏主窗口（`esc|1`）；
  - `Shift + 鼠标点击` 保持区间多选高亮，松开 Shift 执行连续粘贴；
  - `Ctrl + 鼠标点击` 保持分散多选高亮，松开 Ctrl 执行合并粘贴。

### 3. 渲染性能与内存优化 (Rendering & Performance)
- [REQ-3.1] **搜索防抖（Debounce）**：搜索输入框加入轻量防抖（约 80ms），避免每个字符输入都同步触发全量 DOM 遍历重构。
- [REQ-3.2] **DOM 高效更新**：优化 `displayData` 和单项 `add` 逻辑，减少垃圾回收（GC）和重排重绘。
- [REQ-3.3] **图片懒渲染与容错**：为 Base64 图片添加 `loading="lazy"`，并优化超大图显示排版与破损防溃容错。

### 4. 主题统一与 CSS 变量化 (Theming & CSS Variables)
- [REQ-4.1] **热键弹窗主题联动**：重构 `#hotkeyModal`，移除硬编码的深色内联样式（`background:#2b2b2b` 等），改为通过 CSS 变量适配浅色与深色主题。
- [REQ-4.2] **托盘菜单与主界面风格统一**：在 `common.css` 中定义全局主题变量基准（`--bg-body`、`--bg-card`、`--text-main`、`--text-sub`、`--accent-color`、`--border-color` 等），各皮肤（`fluent-light`、`fluent-dark`、`classic`、`stand`、`material`、`modern`、`striped`）通过变量覆盖，实现无死角的主题一致性。

### 5. 安全性与代码规范 (Security & Clean Code)
- [REQ-5.1] **富文本 XSS 与 HTML 结构防御**：安全处理纯文本与富文本的展示字符串，避免非法标签或脚本注入干扰。
- [REQ-5.2] **移除所有已废弃 API**：全面淘汰 `event.keyCode`（改用 `e.key` / `e.code`）、`window.event`、`document.onselectstart`。

---

## Out of Scope

- 不引入 React/Vue/Svelte 等重型前端框架（保持零打包、极速原生架构）。
- 不重构 C# 端已稳定的剪贴板核心监听机制和 IPC 协议定义。

---

## Acceptance Criteria

- [x] 页面在没有引入 `jquery-3.3.1.min.js` 的情况下所有功能完全正常运行。
- [x] 按 `ArrowDown` / `ArrowUp` 能够准确在剪贴板条目间切换高亮并自动跟随滚动。
- [x] 在搜索框输入关键词后，可通过方向键上下挑选搜索结果，按 `Enter` 准确粘贴该项。
- [x] 在 `fluent-light` 浅色主题下打开热键设置窗口（唤醒热键），弹窗呈现优雅的浅色毛玻璃/卡片风格，文字清晰可读，不再是突兀的黑灰色块。
- [x] 托盘菜单切换各皮肤与深浅色模式即时生效，无样式错乱。
- [x] 项目成功编译并通过构建（`dotnet build`）。
