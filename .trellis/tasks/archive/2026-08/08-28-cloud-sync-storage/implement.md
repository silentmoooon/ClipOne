# Implementation Plan: Cloud Sync Storage

## Checklist

- [ ] 1. 设备标识管理器 `util/DeviceManager.cs`:
  - 从 `HKCU\Software\ClipOne` 读取或生成唯一 `DeviceId` 和 `DeviceName`；
- [ ] 2. 扩展配置 `model/Config.cs` 与序列化源生成器 `service/ClipJsonContext.cs`:
  - 支持 `SyncFolder` 字段（为空时默认指向本地 `data/`）；
  - 注册 `ClipModel`、`ClipEvent`、`Config` 的 AOT 序列化支持；
- [ ] 3. 增强 `model/ClipModel.cs`:
  - 增加 `Id` (GUID), `DeviceId`, `Timestamp`, `IsDeleted`, `DeleteTimestamp` 属性；
- [ ] 4. 重写 `service/StorageService.cs`:
  - 实现分区追加写（`devices/{DeviceId}/{year-month}.jsonl`）；
  - 实现跨设备聚合读与去重排序；
  - 实现 `FileSystemWatcher` 实时感知外部文件变动并触发事件；
  - 实现旧版 `history.json` 的自动迁移；
  - 实现墓碑软删除支持；
- [ ] 5. `Program.cs` 联动:
  - 监听 `StorageService.OnHistoryUpdated` 事件并自动推送 `history` 消息到 WebView 前端；
- [ ] 6. 更新 `clear.bat`:
  - 增加对注册表 `HKCU\Software\ClipOne` 和 `%LOCALAPPDATA%\ClipOne` 的清理指令；
- [ ] 7. 编译验证与 Native AOT 测试:
  - `dotnet build` 验证 0 错误 0 警告；
  - `dotnet publish -c Release -r win-x64` 发布验证。
