# 存储精简与图片解耦规格

## 概述

定义 ClipOne 在 events.jsonl 中的序列化精简规则以及图片二进制从日志中解耦为独立资产文件的完整技术规范。

## 场景规范

### Scenario: 存储记录精简且无冗余字段
GIVEN ClipModel 准备保存到 events.jsonl
WHEN 执行序列化
THEN 输出的 JSON 文本中完全不包含 DeviceId 属性
AND 默认属性（IsDeleted: false, DeleteTimestamp: 0, NeedOverride: false, PlainText 为空等）被条件忽略省略
AND 当从文件读取反序列化时，由所属父目录名赋值给内存中的 DeviceId 属性，其余默认值正确初始化

### Scenario: 图片存储与日志完全解耦
GIVEN 用户在系统内复制图片并生成图片记录（Type 为 image）
WHEN StorageService 处理该条目
THEN 图片二进制被持久化为 data/assets/{id}.bmp 文件
AND 该记录在 events.jsonl 中仅保存相对路径（如 assets/{id}.bmp），不再内联数兆字节的 Base64 字符串
AND 前端通过 asset:// 协议流式获取图片并正常渲染预览
AND 用户点击或快捷键粘贴该条目时，ClipService 正确读取该图片并无损写入 Windows 剪贴板

### Scenario: 清理与 Compaction 联动回收资源
GIVEN data/assets 目录中保存了若干图片资产文件
WHEN 用户执行清空（ClearHistory）或日志 Compaction 淘汰了旧记录
THEN 对应的资产图片文件被物理删除回收，避免在磁盘留下孤立无用的大文件
