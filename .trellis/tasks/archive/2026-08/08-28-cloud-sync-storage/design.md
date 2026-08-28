# Design: 多端无冲突云盘同步存储架构 (Cloud Sync Storage)

## 1. 架构概览与目录规范

```text
[同步根目录 SyncFolder] (默认: 程序目录/data 或 自定义路径)
  ├── devices/
  │    ├── device_{DeviceId}/
  │    │     ├── 2026-08.jsonl        <-- 每日/月增量事件日志 (Append-only)
  │    │     └── state.json           <-- 该设备最后压缩的快照
  │    ├── device_{OtherDeviceId}/
  │    │     └── 2026-08.jsonl
  │    └── ...
  └── assets/                         <-- 媒体附件 (按 SHA256 命名内容寻址 CAS)
       ├── 7a8b9c...png
       └── ...
```

## 2. 核心组件与数据流

1. **设备标识管理 (DeviceIdentity)**:
   - 从 `HKCU\Software\ClipOne\DeviceId` 读取，若不存在则生成 `Guid.NewGuid().ToString("N")[..8]`；
   - 设备名从 `Environment.MachineName` 获取（如 `PC-Desktop-a1b2c3d4`）。
   - 保存在机器专属注册表/LocalAppData，保证即使整个应用文件夹通过网盘同步，两台电脑也不会产生 ID 冲突。

2. **数据模型 (ClipItem & ClipEvent)**:
   - `Id`: `string` (UUID)
   - `DeviceId`: `string`
   - `Timestamp`: `long` (UTC 毫秒时间戳)
   - `Type`: `string` (text, html, qq, wechat, image, file)
   - `ClipValue`: `string`
   - `DisplayValue`: `string`
   - `PlainText`: `string`
   - `IsDeleted`: `bool`
   - `DeleteTimestamp`: `long`

3. **写入流程 (Append-Only Write)**:
   - 新增记录：生成事件，追加写入本地设备文件 `devices/device_{DeviceId}/{year-month}.jsonl`；
   - 若为图片/大型二进制：提取 SHA256 哈希，保存到 `assets/{hash}.png`，在 `ClipValue`/`DisplayValue` 中使用本地相对引用；
   - 删除记录：追加一条 `{ "Id": "...", "IsDeleted": true, "DeleteTimestamp": ... }` 墓碑事件。

4. **聚合与实时监听 (Aggregator & Live Watcher)**:
   - 启动时扫描 `devices/` 下所有设备的 `.jsonl` 文件，合并所有事件（按时间戳排序，墓碑覆盖删除项）；
   - 使用 `FileSystemWatcher` 监听 `devices/` 目录变动，若有其他设备的新增/删除事件写入，毫秒级增量合并并推送 WebMessage 刷新 UI。

5. **向后兼容与平滑迁移**:
   - 若存在旧版 `config/history.json` 且 `data/` 为空，自动将历史数据导入为当前设备的初始事件。
