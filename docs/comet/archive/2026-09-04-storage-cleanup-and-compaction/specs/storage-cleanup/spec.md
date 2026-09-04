# 存储清理与日志压缩规格

## 概述

定义 ClipOne 在 StorageService 中对本地事件日志（events.jsonl）的清空与物理压缩（Compaction）规范，确保磁盘数据有效清除并防止日志无节制膨胀。

## 场景规范

### Scenario: 彻底清空所有设备的历史存储文件
GIVEN ClipOne 正在运行且本地 data/devices 目录下存在一个或多个设备的 events.jsonl 文件
WHEN 用户在界面或托盘菜单触发清空（ClearHistory）
THEN ClipOne 将本地 data/devices 目录下所有存在的 events.jsonl 文件直接物理清空为 0 字节
AND ClipOne 清空内存中的历史记录列表并在前端渲染空列表
AND 当再次调用重新加载（ReloadAllHistory）或重启应用时，历史记录保持为空

### Scenario: 阈值与生命周期触发日志压缩
GIVEN 当前设备的 events.jsonl 累积了已删除条目或超过最大记录数（300 条）的废弃历史
WHEN 废弃行数达到阈值（>= 30 条），或应用启动加载（ReloadAllHistory）及退出（Dispose）时
THEN ClipOne 执行 Compaction 物理重写当前设备的 events.jsonl
AND 仅将当前属于该设备的最新有效条目（且最多 300 条）写回文件，彻底丢弃墓碑记录与超量历史
AND 文件体积显著收敛
