using ClipOne.model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace ClipOne.service
{
    public class StorageService
    {
        private readonly string historyPath = Path.Combine("config", "history.json");
        private List<ClipModel> _history;
        private readonly int _maxRecords = 300;

        public StorageService()
        {
            if (!Directory.Exists("config"))
            {
                Directory.CreateDirectory("config");
            }
            if (!File.Exists(historyPath))
            {
                _history = new List<ClipModel>();
            }
            else
            {
                try
                {
                    string json = File.ReadAllText(historyPath);
                    _history = JsonSerializer.Deserialize(json, ClipJsonContext.Default.ListClipModel) ?? new List<ClipModel>();
                }
                catch
                {
                    _history = new List<ClipModel>();
                }
            }
        }

        public List<ClipModel> GetHistory()
        {
            return _history;
        }

        public bool AddClip(ClipModel clip)
        {
            if (clip == null || string.IsNullOrWhiteSpace(clip.ClipValue))
                return false;

            bool replaced = false;

            // Deduplicate: If an image is added and the immediately preceding clip was a temporary WeChat file clip of the same image or from WeChat cache, remove it
            if (clip.Type == ClipService.IMAGE_TYPE && _history.Count > 0)
            {
                var last = _history[0];
                if (last.Type == ClipService.FILE_TYPE)
                {
                    string lastVal = last.ClipValue.ToLowerInvariant();
                    if (lastVal.Contains("wechat") || lastVal.Contains("tencent") || lastVal.Contains("wxid_") || lastVal.Contains("xwechat") ||
                        lastVal.EndsWith(".png") || lastVal.EndsWith(".jpg") || lastVal.EndsWith(".jpeg") || lastVal.EndsWith(".bmp") || lastVal.EndsWith(".webp"))
                    {
                        _history.RemoveAt(0);
                        replaced = true;
                    }
                }
                else if (last.Type == ClipService.IMAGE_TYPE && !string.IsNullOrEmpty(last.DisplayValue) && !string.IsNullOrEmpty(clip.DisplayValue) && last.DisplayValue == clip.DisplayValue)
                {
                    _history.RemoveAt(0);
                    replaced = true;
                }
            }

            // Remove duplicates
            int removed = _history.RemoveAll(c => c.ClipValue == clip.ClipValue);
            if (removed > 0)
            {
                replaced = true;
            }

            // Add to front
            _history.Insert(0, clip);

            // Trim to max records
            if (_history.Count > _maxRecords)
            {
                _history = _history.GetRange(0, _maxRecords);
            }

            SaveHistory();
            return replaced;
        }

        public void SaveHistory()
        {
            try
            {
                string json = JsonSerializer.Serialize(_history, ClipJsonContext.Default.ListClipModel);
                File.WriteAllText(historyPath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine($"Failed to save history: {ex.Message}");
            }
        }

        public void ClearHistory()
        {
            _history.Clear();
            SaveHistory();
        }
    }
}
