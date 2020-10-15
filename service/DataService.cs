using ClipOne.model;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;

namespace ClipOne.service
{
    class DataService
    {
        private static readonly string cacheDir = "cache";
        private static readonly string cacheName = "cache.dat";
        private static readonly string cacheFilePath = cacheDir + "/" + cacheName;
        readonly Timer threadTimer;
        private readonly int maxCount;
        public  readonly List<ClipModel> clips = new List<ClipModel>();
        
        public DataService(int maxCount)
        {
            this.maxCount = maxCount;
            if (!Directory.Exists(cacheDir))
            {
                Directory.CreateDirectory(cacheDir);
            }
            if (File.Exists(cacheFilePath))
            {
               clips.AddRange(JsonConvert.DeserializeObject<List<ClipModel>>(File.ReadAllText(cacheFilePath)));
            }
            
            //2分钟保存一次
            threadTimer = new Timer(Save, null, TimeSpan.FromMinutes(2), TimeSpan.FromMinutes(2));
         
           

        }

        public void Put(ClipModel clip)
        {
            clips.Insert(0, clip);
           
            if (clips.Count > maxCount)
            {
                clips.RemoveAt(clips.Count-1);
            }
        }

        public List<ClipModel> PutAndGet(ClipModel clip)
        {
            clips.Insert(0, clip);

            if (clips.Count > maxCount)
            {
                clips.RemoveAt(clips.Count - 1);
            }
            return clips;
        }

        public List<ClipModel> Get()
        {
            return clips;
        }
        public ClipModel Get(int index)
        {
            return clips[index];
        }

        /// <summary>
        ///  根据起止index取值,需要区分正序和倒序start>end为正序,从上至下取值,start<end为倒序,从下至上取值
        /// </summary>
        /// <param name="startIndex"></param>
        /// <param name="endIndex"></param>
        /// <returns></returns>
        public List<ClipModel> Get(int startIndex ,int endIndex)
        {
            List<ClipModel> list = new List<ClipModel>(Math.Abs(endIndex-startIndex)+1);
            //正序
            if (startIndex < endIndex)
            {
               for(int i = startIndex; i <= endIndex; i++)
                {
                    list.Add(clips[i]);
                }
            }
            //倒序
            else if(startIndex> endIndex)
            {
                for (int i = startIndex; i >= endIndex; i--)
                {
                    list.Add(clips[i]);
                }
            }else
            {
                list.Add(clips[startIndex]);
            }
            return list;
        }

        public List<ClipModel> Get(List<int> indexes)
        {
            List<ClipModel> list = new List<ClipModel>(indexes.Count);
            indexes.ForEach(i=>list.Add(clips[i]));
            return list;
        }

        public void Del(int index)
        {
           
            clips.RemoveAt(index);
        }

        public List<ClipModel> Search(string value)
        {
           return clips.FindAll((clip) => { return clip.Type == value || clip.Type != ClipService.IMAGE_TYPE && clip.ClipValue.ToLower().IndexOf(value) >= 0; });
        }

        public void Clear()
        {

            clips.Clear();
        }

        public void Top(int index)
        {
            
            ClipModel clip = clips[index];
            Del(index);
            Put(clip);
        }

        public ClipModel GetAndTop(int index)
        {

            ClipModel clip = clips[index];
            Del(index);
            Put(clip);
            return clip;
        }

        /// <summary>
        /// 根据起止索引将各项移动到头部,为了方便使用,将会保留顺序,如果start<end则保留正序,反之则为倒序
        /// </summary>
        /// <param name="startIndex"></param>
        /// <param name="endIndex"></param>
        public void Top(int startIndex,int endIndex)
        {
            if (startIndex < endIndex)
            {
                for (int i = endIndex; i >= startIndex; i--)
                {
                    Top(endIndex);

                }
                
            }
            else if (startIndex > endIndex)
            {
                for (int i = endIndex; i <= startIndex; i++)
                {
                    Top(i);

                }
            }
            else
            {
                Top(startIndex);
            }
        }

        /// <summary>
        /// 根据起止索引将各项移动到头部,为了方便使用,将会保留顺序,如果start<end则保留正序,反之则为倒序
        /// </summary>
        /// <param name="startIndex"></param>
        /// <param name="endIndex"></param>
        public List<ClipModel> GetAndTop(int startIndex, int endIndex)
        {
            List<ClipModel> list = new List<ClipModel>(Math.Abs(endIndex - startIndex) + 1);
            if (startIndex < endIndex)
            {
                for (int i = endIndex; i >= startIndex; i--)
                {
                    list.Add(clips[endIndex]);
                    Top(endIndex);

                }

            }
            else if (startIndex > endIndex)
            {
                for (int i = endIndex; i <= startIndex; i++)
                {
                    list.Add(clips[i]);
                    Top(i);

                }
            }
            else
            {
                Top(startIndex);
            }
            return list;
        }

        /// <summary>
        /// 根据索引list移动到头部,保留list中的顺序
        /// </summary>
        /// <param name="indexes"></param>
        public void Top(List<int> indexes)
        {
            Dictionary<int, ClipModel> keyValues = new Dictionary<int, ClipModel>(indexes.Count);
            List<int> tempIndexes = new List<int>(indexes.ToArray());

            tempIndexes.Sort();
            tempIndexes.Reverse();
            foreach (var index in tempIndexes)
            {
                keyValues.Add(index, clips[index]);
                Del(index);
            }
            foreach (var index in indexes)
            {
                Put(keyValues[index]);
            }

        }

        /// <summary>
        /// 根据索引list移动到头部,保留list中的顺序
        /// </summary>
        /// <param name="indexes"></param>
        public List<ClipModel> GetAndTop(List<int> indexes)

        {
            Dictionary<int, ClipModel> keyValues = new Dictionary<int, ClipModel>(indexes.Count);
            List<int> tempIndexes = new List<int>(indexes.ToArray());

            tempIndexes.Sort();
            tempIndexes.Reverse();
            foreach (var index in tempIndexes)
            {
                keyValues.Add(index, clips[index]);
                Del(index);
            }
            foreach (var index in indexes)
            {
                Put(keyValues[index]);
            }

            return keyValues.Values.ToList();
        }

        private void Save(object value)
        {
            Trace.WriteLine("save");
            string json = JsonConvert.SerializeObject(clips);
            File.WriteAllText(cacheDir + "/" + cacheName, json);

        }

        public void Close()
        {
            threadTimer.Change(Timeout.Infinite, Timeout.Infinite);
            threadTimer.Dispose();
            Save(null);
        }

    }
}
