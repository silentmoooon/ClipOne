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

        public void AddClip(ClipModel clip)
        {
            if (clip == null || string.IsNullOrWhiteSpace(clip.ClipValue))
                return;

            // Remove duplicates
            _history.RemoveAll(c => c.ClipValue == clip.ClipValue);

            // Add to front
            _history.Insert(0, clip);

            // Trim to max records
            if (_history.Count > _maxRecords)
            {
                _history = _history.GetRange(0, _maxRecords);
            }

            SaveHistory();
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
