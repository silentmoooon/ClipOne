using ClipOne.model;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;

namespace ClipOne.service
{
    class StorageService
    {
        private readonly string historyPath = "config\\history.json";
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
                    _history = JsonConvert.DeserializeObject<List<ClipModel>>(json) ?? new List<ClipModel>();
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
                string json = JsonConvert.SerializeObject(_history);
                File.WriteAllText(historyPath, json);
            }
            catch {}
        }

        public void ClearHistory()
        {
            _history.Clear();
            SaveHistory();
        }
    }
}
