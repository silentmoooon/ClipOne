# 目标

修复在微信中复制图文混合内容或单张图片时，ClipOne 剪贴板历史中图片被错误显示为 `[表情]` 的问题，使其在剪贴板列表中正确渲染为真实图片，同时保持粘贴时能够还原图片与文本的完整格式。

# 范围

- `service/ClipService.cs` 中的 `HandleWeChat(ClipModel clip)`：
  - 解析微信 `WeChat_RichEdit_Format` XML 时，若 `EditElement` 具有 `filepath` 属性且本地文件存在且为图片，将其读取为 Base64 Data URI，并在 `DisplayValue` 中以 `<img>` 标签呈现，替换原先写死的 `[表情]`。
  - 若复制内容为纯单张图片（仅有一个包含有效图片 `filepath` 的元素且无文本），则将 `clip.Type` 设为 `image`，便于全局标准图片预览与跨应用粘贴。
  - 若为图文混合内容，保持 `clip.Type` 为 `WECHAT_TYPE`，完整保留 `xmlStr` 作为 `ClipValue`，`PlainText` 保留纯文本，确保粘贴回微信时完全还原。
  - 微信图文混合中的纯文本元素经过 HTML 转义与换行格式化，确保排版安全不破坏页面。

# 非目标

- 不修改 QQ 富文本或网页 HTML 的既有解析分支。
- 不引入重型外部图像处理库，保持 Native AOT 纯原生兼容。

# 验收示例

- A1: 当从微信复制包含图片与文字的混合内容时，ClipOne 剪贴板历史列表中该条目真实展示图片与文字，而非 `[表情]`。
- A2: 当在 ClipOne 中选中该图文混合记录并粘贴回微信输入框时，能够完整还原图片与文字。
- A3: 当从微信仅复制单张图片时，ClipOne 将其识别为图片类型并正常展示与粘贴。

# 约束与不变量

- 保持 Native AOT 零反射兼容与 x64 构建通过。
- 保证剪贴板数据还原时的编码与 Win32 原生 API 兼容。

# 决策

- 在 `HandleWeChat` 中对具有 `filepath` 的 `EditElement` 进行文件存在性与扩展名检测，有效图片使用 `data:{mime};base64,...` 嵌入 `DisplayValue`。
- 文本节点经过 `FormatDisplayText` 转义，防止文本中的 `<`、`>` 干扰 HTML 标签渲染。

# 待解决问题

无

# 验证预期

- 执行 `dotnet build` 编译成功。
- 针对实际捕获的微信剪贴板 XML 数据执行验证，确保 `DisplayValue` 正确生成包含真实图片 Data URI 的 `<img>` 标签且无 `[表情]`。
