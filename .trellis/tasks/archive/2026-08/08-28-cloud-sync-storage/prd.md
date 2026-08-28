# PRD: 多端无冲突云盘同步存储架构 (Cloud Sync Storage)

## 1. 目标与用户价值 (Goal & User Value)
实现 ClipOne 剪贴板历史记录的多端云盘同步（支持 OneDrive、坚果云、iCloud、Syncthing、Google Drive、WebDAV 挂载盘等），保证多设备并发复制互不冲突、不产生网盘“冲突副本”、支持跨设备删除同步（墓碑机制），并在无网络/未配置同步目录时保持原有极致的本地运行性能。

## 2. 已确认的技术事实 (Confirmed Facts)
- 当前环境：.NET 10 (`net10.0-windows10.0.26100.0`), Native AOT, x64, Photino.NET 3.2.3。
- 序列化机制：零反射 `System.Text.Json` Source Generator (`ClipJsonContext.cs`)。
- 现有存储：`StorageService.cs` 目前单文件读写 `config/history.json`。
- Git 分支：已为该功能创建独立分支 `feature/cloud-sync-storage`。

## 3. 核心需求 (Requirements)
- **R1: 设备隔离追加写入 (Device-Partitioned Append-Only)**：
  - 自动分配/持久化本地 `DeviceId` (如 GUID)。
  - 每台设备仅向 `devices/{DeviceId}/` 写入自己的历史事件日志（JSONL 格式），彻底避免多设备写争抢与网盘冲突副本。
- **R2: 跨设备聚合与实时感知 (Multi-Device Aggregation & Live Watch)**：
  - 启动时聚合各设备分片数据，按时间戳 (UTC Timestamp) 倒序去重合并。
  - 使用 `FileSystemWatcher` 实时感知云盘同步下来的其他设备新条目，无需手动刷新。
- **R3: 墓碑软删除机制 (Tombstone)**：
  - 支持跨设备删除同步，记录删除墓碑事件，防止其他设备旧数据“复活”。
- **R4: 向后兼容与渐进启用**：
  - 若用户未配置云同步目录，平滑回退使用本地存储；若已存在旧版 `config/history.json`，自动无损迁移。

## 4. 验收标准 (Acceptance Criteria)
- [ ] AC1: 设备 A 与设备 B 配置同一同步目录后，在 A 设备复制的内容能在 B 设备的 ClipOne 历史列表中毫秒级/自动呈现。
- [ ] AC2: A、B 两设备同时复制时，同步目录下生成各自独立的日志文件，网盘无任何冲突副本生成。
- [ ] AC3: 在任一设备按 Delete 键删除某条记录，其他设备感知后同步移除该条目。
- [ ] AC4: 未开启同步目录时，本地单机功能与性能不受任何影响。

## 5. 待决定的产品与架构选项 (Open Decision)
- **同步目录交互与模式决策**：是采用“基于本地网盘文件夹路径（Folder-based，推荐）”，还是“在应用内内置 WebDAV/网络服务连接”？
