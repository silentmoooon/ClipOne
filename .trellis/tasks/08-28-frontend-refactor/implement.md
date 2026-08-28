# Implementation Plan: 前端现代化与全套重构

## Ordered Checklist

- [x] **Step 1: CSS 架构与变量体系升级**
  - 在 `html/css/common.css` 中注入 `:root` 变量基准体系；
  - 改造 `#hotkeyModal`、`#searchDiv`、`.tray-menu-modal` 等组件样式，彻底移除硬编码内联样式；
  - 针对所有皮肤（`fluent-light`、`fluent-dark`、`material`、`modern`、`stand`、`striped`、`classic`）适配对应的变量声明。

- [x] **Step 2: 模版与构建依赖清理**
  - 在 `html/index.html` 中移除 jQuery 引入 `<script src="js/jquery-3.3.1.min.js">`；
  - 清理 `#hotkeyModal` 中的内联 style 属性，改用 CSS 类名；
  - 更新 `ClipOne.csproj`，移除 `jquery-3.3.1.min.js` 的打包引用；
  - 删除废弃的 `html/js/jquery-3.3.1.min.js` 文件。

- [x] **Step 3: `main.js` 原生重构与核心特性实现**
  - 编写模块化单例 `ClipApp`；
  - 实现去 jQuery 后的所有 DOM 操作与事件委托绑定；
  - 实现完整的键盘上下导航（`ArrowUp`/`ArrowDown`/`PageUp`/`PageDown`/`Home`/`End`）、选择高亮与视口自动滚动；
  - 实现搜索防抖（Debounce）与搜索状态下的方向键导航；
  - 补齐/重写多选（Ctrl）和区间选（Shift）的事件与状态逻辑；
  - 实现原生平滑阻尼回弹（Rubber-band Bounce）；
  - 接入 Photino WebView 消息监听与分发（`history`、`add`、`hotkeySettings`、`show`、`showTrayMenu`、`changeSkin`）。

- [x] **Step 4: Spec 文档同步更新**
  - 更新 `.trellis/spec/web-ui/javascript.md`，记录新的纯原生 ES6+ 规范与状态机约定，移除对 jQuery 的描述。

- [x] **Step 5: 验证与构建检查**
  - 执行 `dotnet build` 确保项目编译通过；
  - 验证资源文件打包正常，无遗漏或冗余文件。

## Validation Commands

```powershell
dotnet build
```

## Risky Files

- `html/js/main.js`：核心前端交互逻辑。
- `html/index.html`：入口 DOM。
- `html/css/common.css`：全局基础样式。
- `ClipOne.csproj`：打包与构建配置。
