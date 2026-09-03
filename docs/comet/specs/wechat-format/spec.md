# 微信剪贴板富文本与图片处理规格

## 概述

定义 ClipOne 监听并处理微信 Windows 客户端富文本剪贴板格式（`WeChat_RichEdit_Format`）的完整行为规范。

## 场景规范

### Scenario: 复制微信图文混合内容并在剪贴板历史中呈现
GIVEN 微信输入框中包含图片及附带文字（可含微信表情代码）
WHEN 用户复制该图文混合内容触发剪贴板更新
THEN ClipOne 将其识别为微信富文本类型（`WeChat_RichEdit_Format`）
AND ClipOne 将其中的图片元素转换为 Base64 嵌入的 HTML `<img>` 标签
AND 剪贴板历史列表中该条目完整展示图片与文字，而非展示为 `[表情]`
AND 保留原始 XML 格式供后续无损粘贴还原

### Scenario: 粘贴微信图文混合内容回微信
GIVEN 剪贴板历史中已保存微信图文混合条目
WHEN 用户选中该条目并执行粘贴
THEN ClipOne 将原始 `WeChat_RichEdit_Format` 数据及 UnicodeText 写入 Windows 系统剪贴板
AND 粘贴到微信输入框时能够正确还原图片与文字

### Scenario: 复制微信纯单张图片
GIVEN 微信中仅复制了一张图片且无伴随文字
WHEN ClipOne 处理该剪贴板事件
THEN ClipOne 将其识别为通用图片类型（`image`）
AND 在剪贴板历史中以标准图片卡片呈现
AND 允许粘贴至任意支持位图的应用程序
