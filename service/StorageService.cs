using ClipOne.model;
using ClipOne.util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;

namespace ClipOne.service
{
    public class StorageService : IDisposable
    {
        private readonly ConfigService _configService;
        private readonly object _lock = new object();
        private readonly int _maxRecords = 300;
        private const int CompactionThreshold = 30;
        private int _pendingStaleCount = 0;
        private readonly Dictionary<string, ClipModel> _foreignTombstones = new Dictionary<string, ClipModel>();

        private string _syncRoot = string.Empty;
        private string _myDeviceDir = string.Empty;
        private string _myEventsFile = string.Empty;
        private string _devicesDir = string.Empty;
        private string _assetsDir = string.Empty;

        private List<ClipModel> _history = new List<ClipModel>();
        private FileSystemWatcher? _watcher;
        private Timer? _debounceTimer;

        public event Action? OnHistoryChanged;

        public StorageService(ConfigService configService)
        {
            _configService = configService;
            InitializeStoragePaths();
            MigrateOldHistoryIfPresent();
            ReloadAllHistory();
            StartWatcher();
        }

        public string SyncRootDirectory => _syncRoot;

        private void InitializeStoragePaths()
        {
            var config = _configService.GetConfig();
            if (!string.IsNullOrWhiteSpace(config.SyncFolder) && Directory.Exists(config.SyncFolder))
            {
                _syncRoot = config.SyncFolder;
            }
            else
            {
                _syncRoot = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data");
            }

            _devicesDir = Path.Combine(_syncRoot, "devices");
            _assetsDir = Path.Combine(_syncRoot, "assets");
            _myDeviceDir = Path.Combine(_devicesDir, DeviceManager.DeviceFolderTag);
            _myEventsFile = Path.Combine(_myDeviceDir, "events.jsonl");

            if (!Directory.Exists(_myDeviceDir))
            {
                Directory.CreateDirectory(_myDeviceDir);
            }
            if (!Directory.Exists(_assetsDir))
            {
                Directory.CreateDirectory(_assetsDir);
            }
        }

        private void StartWatcher()
        {
            try
            {
                if (!Directory.Exists(_devicesDir))
                {
                    Directory.CreateDirectory(_devicesDir);
                }

                _watcher = new FileSystemWatcher(_devicesDir, "*.jsonl")
                {
                    IncludeSubdirectories = true,
                    NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.Size
                };

                _watcher.Changed += OnDeviceFileChanged;
                _watcher.Created += OnDeviceFileChanged;
                _watcher.EnableRaisingEvents = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine($"Failed to start FileSystemWatcher: {ex.Message}");
            }
        }

        private void OnDeviceFileChanged(object sender, FileSystemEventArgs e)
        {
            // Ignore writes to our own file to prevent redundant reload loops
            if (string.Equals(e.FullPath, _myEventsFile, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            // Debounce multiple rapid file events
            _debounceTimer?.Dispose();
            _debounceTimer = new Timer(_ =>
            {
                ReloadAllHistory();
                OnHistoryChanged?.Invoke();
            }, null, 300, Timeout.Infinite);
        }

        private void MigrateOldHistoryIfPresent()
        {
            try
            {
                string oldHistoryPath = Path.Combine("config", "history.json");
                if (File.Exists(oldHistoryPath) && (!File.Exists(_myEventsFile) || new FileInfo(_myEventsFile).Length == 0))
                {
                    string json = File.ReadAllText(oldHistoryPath);
                    var oldList = JsonSerializer.Deserialize(json, ClipJsonContext.Default.ListClipModel);
                    if (oldList != null && oldList.Count > 0)
                    {
                        long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                        var lines = new List<string>();
                        for (int i = oldList.Count - 1; i >= 0; i--)
                        {
                            var clip = oldList[i];
                            clip.Id = Guid.NewGuid().ToString("N");
                            clip.DeviceId = DeviceManager.DeviceId;
                            clip.Timestamp = now - (oldList.Count - i) * 1000;
                            string line = JsonSerializer.Serialize(clip, ClipJsonContext.Default.ClipModel);
                            lines.Add(line.Replace("\r", "").Replace("\n", ""));
                        }

                        File.AppendAllLines(_myEventsFile, lines);
                        File.Move(oldHistoryPath, oldHistoryPath + ".bak", overwrite: true);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine($"Migration error: {ex.Message}");
            }
        }

        public List<ClipModel> GetHistory()
        {
            lock (_lock)
            {
                return new List<ClipModel>(_history);
            }
        }

        public void ReloadAllHistory()
        {
            lock (_lock)
            {
                var itemsMap = new Dictionary<string, ClipModel>();
                var deletedMap = new Dictionary<string, long>();
                int myFileTotalLines = 0;
                _foreignTombstones.Clear();

                try
                {
                    if (Directory.Exists(_devicesDir))
                    {
                        var jsonlFiles = Directory.GetFiles(_devicesDir, "*.jsonl", SearchOption.AllDirectories);
                        foreach (var file in jsonlFiles)
                        {
                            bool isMyFile = string.Equals(file, _myEventsFile, StringComparison.OrdinalIgnoreCase);
                            string folderName = Path.GetFileName(Path.GetDirectoryName(file)) ?? "";
                            string inferredDeviceId = "";
                            if (string.Equals(folderName, DeviceManager.DeviceFolderTag, StringComparison.OrdinalIgnoreCase) || isMyFile)
                            {
                                inferredDeviceId = DeviceManager.DeviceId;
                            }
                            else if (!string.IsNullOrEmpty(folderName))
                            {
                                int lastIdx = folderName.LastIndexOf('_');
                                inferredDeviceId = lastIdx >= 0 ? folderName.Substring(lastIdx + 1) : folderName;
                            }

                            try
                            {
                                using var fs = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                                using var reader = new StreamReader(fs);
                                string? line;
                                while ((line = reader.ReadLine()) != null)
                                {
                                    if (string.IsNullOrWhiteSpace(line)) continue;
                                    if (isMyFile) myFileTotalLines++;
                                    try
                                    {
                                        var model = JsonSerializer.Deserialize(line, ClipJsonContext.Default.ClipModel);
                                        if (model != null && !string.IsNullOrEmpty(model.Id))
                                        {
                                            if (string.IsNullOrEmpty(model.DeviceId))
                                            {
                                                model.DeviceId = inferredDeviceId;
                                            }

                                            if (model.IsDeleted)
                                            {
                                                deletedMap[model.Id] = Math.Max(deletedMap.GetValueOrDefault(model.Id, 0), model.DeleteTimestamp);
                                            }
                                            else
                                            {
                                                itemsMap[model.Id] = model;
                                            }
                                        }
                                    }
                                    catch
                                    {
                                    }
                                }
                            }
                            catch
                            {
                            }
                        }
                    }

                    // Identify active tombstones for foreign items
                    foreach (var kvp in deletedMap)
                    {
                        if (itemsMap.TryGetValue(kvp.Key, out var clip) && !string.IsNullOrEmpty(clip.DeviceId) && clip.DeviceId != DeviceManager.DeviceId)
                        {
                            _foreignTombstones[kvp.Key] = new ClipModel
                            {
                                Id = kvp.Key,
                                DeviceId = DeviceManager.DeviceId,
                                Timestamp = clip.Timestamp,
                                IsDeleted = true,
                                DeleteTimestamp = kvp.Value
                            };
                        }
                    }

                    // Filter out tombstoned items
                    var validItems = itemsMap.Values
                        .Where(item => !deletedMap.ContainsKey(item.Id) || (deletedMap.TryGetValue(item.Id, out var delTime) && delTime < item.Timestamp))
                        .ToList();

                    // Deduplicate by ClipValue (keep latest timestamp)
                    var deduplicated = validItems
                        .GroupBy(c => c.ClipValue)
                        .Select(g => g.OrderByDescending(c => c.Timestamp).First())
                        .OrderByDescending(c => c.Timestamp)
                        .Take(_maxRecords)
                        .ToList();

                    _history = deduplicated;

                    // If our events file has stale/deleted/overflowed lines, compact it on startup/reload
                    int myActiveCount = _history.Count(c => c.DeviceId == DeviceManager.DeviceId || string.IsNullOrEmpty(c.DeviceId));
                    if (myFileTotalLines > myActiveCount + _foreignTombstones.Count)
                    {
                        CompactMyEventsFileLocked();
                    }
                    else
                    {
                        _pendingStaleCount = 0;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Trace.WriteLine($"Failed to reload history: {ex.Message}");
                }
            }
        }

        public bool AddClip(ClipModel clip)
        {
            if (clip == null || string.IsNullOrWhiteSpace(clip.ClipValue))
                return false;

            lock (_lock)
            {
                clip.Id = Guid.NewGuid().ToString("N");
                clip.DeviceId = DeviceManager.DeviceId;
                clip.Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                clip.IsDeleted = false;

                if (clip.Type == ClipService.IMAGE_TYPE && !string.IsNullOrEmpty(clip.ClipValue))
                {
                    SaveImageToAssets(clip);
                }

                // Deduplicate with recent in-memory items
                if (clip.Type == ClipService.IMAGE_TYPE && _history.Count > 0)
                {
                    var last = _history[0];
                    if (last.Type == ClipService.FILE_TYPE)
                    {
                        string lastVal = last.ClipValue.ToLowerInvariant();
                        if (lastVal.Contains("wechat") || lastVal.Contains("tencent") || lastVal.Contains("wxid_") || lastVal.Contains("xwechat") ||
                            lastVal.EndsWith(".png") || lastVal.EndsWith(".jpg") || lastVal.EndsWith(".jpeg") || lastVal.EndsWith(".bmp") || lastVal.EndsWith(".webp"))
                        {
                            SoftDeleteClipInternal(last);
                        }
                    }
                    else if (last.Type == ClipService.IMAGE_TYPE && !string.IsNullOrEmpty(last.DisplayValue) && !string.IsNullOrEmpty(clip.DisplayValue) && last.DisplayValue == clip.DisplayValue)
                    {
                        SoftDeleteClipInternal(last);
                    }
                }

                // If identical ClipValue already exists in memory, mark older instances as deleted
                var duplicates = _history.Where(c => c.ClipValue == clip.ClipValue).ToList();
                foreach (var dup in duplicates)
                {
                    SoftDeleteClipInternal(dup);
                }

                _history.RemoveAll(c => c.ClipValue == clip.ClipValue);
                _history.Insert(0, clip);

                if (_history.Count > _maxRecords)
                {
                    int overflowCount = _history.Count - _maxRecords;
                    _history = _history.GetRange(0, _maxRecords);
                    _pendingStaleCount += overflowCount;
                }

                if (_pendingStaleCount >= CompactionThreshold)
                {
                    CompactMyEventsFileLocked();
                }
                else
                {
                    AppendEventToMyFile(clip);
                }

                return duplicates.Count > 0;
            }
        }

        private void SoftDeleteClipInternal(ClipModel clip)
        {
            var tombstone = new ClipModel
            {
                Id = clip.Id,
                DeviceId = DeviceManager.DeviceId,
                Timestamp = clip.Timestamp,
                IsDeleted = true,
                DeleteTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };

            if (!string.IsNullOrEmpty(clip.DeviceId) && clip.DeviceId != DeviceManager.DeviceId)
            {
                _foreignTombstones[clip.Id] = tombstone;
            }

            AppendEventToMyFile(tombstone);
            _pendingStaleCount++;
        }

        public void DeleteClip(int index)
        {
            lock (_lock)
            {
                if (index >= 0 && index < _history.Count)
                {
                    var clip = _history[index];
                    _history.RemoveAt(index);
                    SoftDeleteClipInternal(clip);
                    if (_pendingStaleCount >= CompactionThreshold)
                    {
                        CompactMyEventsFileLocked();
                    }
                }
            }
        }

        public void DeleteClipById(string id)
        {
            lock (_lock)
            {
                var clip = _history.FirstOrDefault(c => c.Id == id);
                if (clip != null)
                {
                    _history.Remove(clip);
                    SoftDeleteClipInternal(clip);
                    if (_pendingStaleCount >= CompactionThreshold)
                    {
                        CompactMyEventsFileLocked();
                    }
                }
            }
        }

        private void SaveImageToAssets(ClipModel clip)
        {
            try
            {
                string val = clip.ClipValue;
                if (!val.StartsWith("assets/") && !val.StartsWith("assets\\"))
                {
                    if (!Directory.Exists(_assetsDir))
                    {
                        Directory.CreateDirectory(_assetsDir);
                    }

                    byte[] bytes = Convert.FromBase64String(val);
                    string assetFileName = $"{clip.Id}.bmp";
                    string assetPath = Path.Combine(_assetsDir, assetFileName);
                    File.WriteAllBytes(assetPath, bytes);
                    clip.ClipValue = $"assets/{assetFileName}";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine($"SaveImageToAssets error: {ex.Message}");
            }
        }

        public void ClearHistory()
        {
            lock (_lock)
            {
                _history.Clear();
                _foreignTombstones.Clear();
                _pendingStaleCount = 0;

                try
                {
                    if (Directory.Exists(_devicesDir))
                    {
                        var jsonlFiles = Directory.GetFiles(_devicesDir, "*.jsonl", SearchOption.AllDirectories);
                        foreach (var file in jsonlFiles)
                        {
                            try
                            {
                                using var fs = new FileStream(file, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Trace.WriteLine($"Failed to clear {file}: {ex.Message}");
                            }
                        }
                    }

                    if (File.Exists(_myEventsFile))
                    {
                        using var fs = new FileStream(_myEventsFile, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
                    }

                    if (Directory.Exists(_assetsDir))
                    {
                        var assetFiles = Directory.GetFiles(_assetsDir, "*.*");
                        foreach (var assetFile in assetFiles)
                        {
                            try
                            {
                                File.Delete(assetFile);
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Trace.WriteLine($"Failed to delete asset {assetFile}: {ex.Message}");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Trace.WriteLine($"ClearHistory error: {ex.Message}");
                }
            }
        }

        public void CompactHistory()
        {
            lock (_lock)
            {
                CompactMyEventsFileLocked();
            }
        }

        private void CompactMyEventsFileLocked()
        {
            try
            {
                if (!Directory.Exists(_myDeviceDir))
                {
                    Directory.CreateDirectory(_myDeviceDir);
                }

                var activeClips = _history
                    .Where(c => !c.IsDeleted && (c.DeviceId == DeviceManager.DeviceId || string.IsNullOrEmpty(c.DeviceId)))
                    .OrderBy(c => c.Timestamp)
                    .ToList();

                var lines = new List<string>(activeClips.Count + _foreignTombstones.Count);
                foreach (var clip in activeClips)
                {
                    string line = JsonSerializer.Serialize(clip, ClipJsonContext.Default.ClipModel);
                    lines.Add(line.Replace("\r", "").Replace("\n", ""));
                }

                foreach (var tombstone in _foreignTombstones.Values)
                {
                    string line = JsonSerializer.Serialize(tombstone, ClipJsonContext.Default.ClipModel);
                    lines.Add(line.Replace("\r", "").Replace("\n", ""));
                }

                using (var fs = new FileStream(_myEventsFile, FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
                using (var writer = new StreamWriter(fs))
                {
                    foreach (var line in lines)
                    {
                        writer.WriteLine(line);
                    }
                }

                CleanOrphanedAssetsLocked();

                _pendingStaleCount = 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine($"Compaction error: {ex.Message}");
            }
        }

        private void CleanOrphanedAssetsLocked()
        {
            try
            {
                if (Directory.Exists(_assetsDir))
                {
                    var referenced = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    foreach (var item in _history)
                    {
                        if (item.Type == ClipService.IMAGE_TYPE && !string.IsNullOrEmpty(item.ClipValue))
                        {
                            if (item.ClipValue.StartsWith("assets/") || item.ClipValue.StartsWith("assets\\"))
                            {
                                referenced.Add(Path.GetFileName(item.ClipValue));
                            }
                        }
                    }

                    var diskAssets = Directory.GetFiles(_assetsDir, "*.*");
                    foreach (var file in diskAssets)
                    {
                        string name = Path.GetFileName(file);
                        if (!referenced.Contains(name))
                        {
                            try
                            {
                                File.Delete(file);
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Trace.WriteLine($"Failed to delete orphan asset {file}: {ex.Message}");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine($"CleanOrphanedAssets error: {ex.Message}");
            }
        }

        private void AppendEventToMyFile(ClipModel clip)
        {
            try
            {
                if (!Directory.Exists(_myDeviceDir))
                {
                    Directory.CreateDirectory(_myDeviceDir);
                }

                string line = JsonSerializer.Serialize(clip, ClipJsonContext.Default.ClipModel);
                line = line.Replace("\r", "").Replace("\n", "");
                File.AppendAllText(_myEventsFile, line + Environment.NewLine);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine($"Failed to write event: {ex.Message}");
            }
        }

        public void Dispose()
        {
            lock (_lock)
            {
                if (_pendingStaleCount > 0)
                {
                    CompactMyEventsFileLocked();
                }
            }
            _watcher?.Dispose();
            _debounceTimer?.Dispose();
        }
    }
}
