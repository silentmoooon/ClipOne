# 目标
全面精简与优化 ClipOne 存储结构：
1. 字段精简：在 events.jsonl 中移除冗余的 DeviceId 字段（从父目录设备名直接推断），并对布尔、数值及字符串默认空值启用条件忽略，消除单条记录的冗余元数据。
2. 图片解耦：将剪贴板大图片从 events.jsonl 的 Base64 内联文本中解耦，持久化为 data/assets/{id}.bmp 独立文件，events.jsonl 仅记录相对路径，彻底解决日志文件体积膨胀问题，并通过 asset:// 协议流式按需加载，大幅提升界面响应速度。

# 范围
- `model/ClipModel.cs`：对 `DeviceId` 标记 `[JsonIgnore]`；对 `IsDeleted`、`DeleteTimestamp`、`NeedOverride`、`PlainText` 等默认值字段启用条件忽略；紧凑 JSON 输出。
- `service/StorageService.cs`：
  - 读取各设备 `events.jsonl` 时，从所属目录名称安全推断 `DeviceId`；
  - 保存新增图片时将二进制存入 `_assetsDir`，`ClipValue` 仅保存相对路径 `assets/{id}.bmp`；
  - 清空与日志 Compaction 时联动清理 `_assetsDir` 中的无用孤立文件。
- `service/ClipService.cs`：兼容读取 asset 相对路径、物理绝对路径与历史旧 Base64 数据还原剪贴板位图。
- `Program.cs` & `html/js/main.js`：注册 `asset://` 自定义 Scheme 协议流式供给图片，前端支持渲染 `asset://` 与向下兼容历史 Base64 图片。

# 非目标
- 不改动非图片类型（text、file、html、wechat、qq）的主内容表现方式。
- 不改动多设备分片目录组织结构。

# 验收示例
- Scenario: 存储记录精简且无冗余字段
  - WHEN ClipOne 保存新增普通文本或状态条目至 events.jsonl
  - THEN 生成的 JSON 文本行中不再包含 DeviceId、IsDeleted:false、DeleteTimestamp:0、NeedOverride:false 以及多余的空字符串
  - AND 重新加载时条目数据完整有效。
- Scenario: 图片存储与日志完全解耦
  - WHEN 用户复制图片并触发剪贴板存储
  - THEN 图片二进制被保存到 data/assets/{id}.bmp，events.jsonl 中该记录仅保存相对文件路径
  - AND 该记录在 events.jsonl 中体积由几兆字节缩小为几十字节
  - AND 前端能通过 asset:// 正常渲染预览，点击或快捷键粘贴能完整恢复至系统剪贴板。
- Scenario: 清理与 Compaction 联动回收资源
  - WHEN 执行 ClearHistory 或 Compaction 触发瘦身
  - THEN 废弃的资产图片文件被物理清理，不会在磁盘留下孤立无用的大文件。

# 约束与不变量
- 保持对已有历史 JSONL 中旧 Base64 数据的前向与后向兼容读取。
- 保持 Native AOT 极限剪裁与静态编译兼容。

# 决策
- `DeviceId` 移除存储（`[JsonIgnore]`），由分片目录名推断。
- 默认值字段（`IsDeleted`, `DeleteTimestamp`, `NeedOverride`, `PlainText`）按需省略（`WhenWritingDefault`）。
- 新增图片存储解耦至 `data/assets/{id}.bmp`，WebView2 通过 `asset://` 自定义 Scheme 流式加载，粘贴时自动解析路径。
- 清空与 Compaction 同步清理 `assets` 目录中的孤立垃圾文件。

# 待解决问题
（无）

# 验证预期
- 新增文本记录体积缩减 60%+。
- 新增图片记录在 events.jsonl 中体积缩减 99.9%+。
- 图片预览、多设备同步读取、粘贴还原功能完全正常。
- 编译通过，0 警告 0 错误。
