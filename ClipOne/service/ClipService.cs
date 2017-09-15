using ClipOne.model;
using ClipOne.util;
using ClipOne.view;
using HtmlAgilityPack;
using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
 
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Xml;

namespace ClipOne.service
{
    class ClipService
    {
        /// <summary>
        /// 图片类型，通过在内容前面增加前缀来标识
        /// </summary>
        public   const string IMAGE_TYPE = "image";
        /// <summary>
        /// html类型，通过在内容前面增加前缀来标识
        /// </summary>
        public const string HTML_TYPE = "html";

       
        /// <summary>
        /// 文件类型，通过在内容前面增加前缀来标识
        /// </summary>
        public const string FILE_TYPE = "file";

        /// <summary>
        /// QQ富文本类型
        /// </summary>
        public const string QQ_RICH_TYPE = "QQ_Unicode_RichEdit_Format";

        /// <summary>
        /// Q文本类型
        /// </summary>
        public const string TEXT_TYPE = "text";

        /// <summary>
        /// 缓存目录
        /// </summary>
        public static string cacheDir = "cache";
        /// <summary>
        /// 设置开机启动
        /// </summary>
        public static void SetStartup(bool isAutoStartup)
        {

            RegistryKey reg = Registry.CurrentUser.CreateSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run");
            string exePath = System.Windows.Forms.Application.ExecutablePath;
            string exeName = System.Windows.Forms.Application.ProductName;
            if (!isAutoStartup)
            {
                if (reg.GetValue(exeName) != null)
                {

                    reg.DeleteValue(exeName);
                }
            }
            else
            {

                reg.SetValue(exeName, exePath);
            }

        }

        /// <summary>
        /// 持久化
        /// </summary>
        public static void SaveData(List<ClipModel> resultList,string storePath)
        {
            string json = JsonConvert.SerializeObject(resultList);
            File.WriteAllText(storePath, json);
        }

        /// <summary>
        /// 保存图片到缓存目录
        /// </summary>
        /// <param name="bs"></param>
        /// <returns></returns>
        public static string SaveImage(BitmapSource bs)
        {
            string path = cacheDir + "/" + Guid.NewGuid().ToString() + ".bmp";
            BitmapEncoder encoder = new BmpBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bs));

            FileStream fs = new FileStream(path, FileMode.Create);
            encoder.Save(fs);
            fs.Close();
            return path;
        }
        /// <summary>
        /// 设置条目到剪切板
        /// </summary>
        /// <param name="result"></param>
        public static void SetValueToClip(ClipModel result)
        {
            if (result.Type==IMAGE_TYPE)
            {
                try
                {
                    
                    BitmapImage bitImg = new BitmapImage();
                    bitImg.BeginInit();
                    bitImg.UriSource = new Uri(result.ClipValue, UriKind.Relative);
                    bitImg.EndInit();
                    IDataObject data = new DataObject(DataFormats.Bitmap, bitImg);
                    Clipboard.SetDataObject(data, false);


                }
                catch {  return; }
            }
            else if (result.Type == HTML_TYPE)
            {
                
                IDataObject data = new DataObject(DataFormats.Html, result.ClipValue);
                Clipboard.SetDataObject(data, false);
            }
            else if (result.Type == QQ_RICH_TYPE)
            {

                
                MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(result.ClipValue));
                IDataObject data = new DataObject(QQ_RICH_TYPE, ms);
                Clipboard.SetDataObject(data, false);
            }
            else if (result.Type == FILE_TYPE)
            {
                string[] tmp = result.ClipValue.Split(',');
                 
                
                try
                {
                    IDataObject data = new DataObject(DataFormats.FileDrop, tmp);
                    MemoryStream memo = new MemoryStream(4);
                    byte[] bytes = new byte[] { (byte)(5), 0, 0, 0 };
                    memo.Write(bytes, 0, bytes.Length);
                    data.SetData("PreferredDropEffect", memo);
                    Clipboard.SetDataObject(data, false);
                }
                catch  {   return; }
            }
            else
            {
                try
                {
                    IDataObject data = new DataObject(DataFormats.Text, result.ClipValue);
                    System.Windows.Forms.Clipboard.SetDataObject(data, false);

                }
                catch   {   }
            }
        }

        

        /// <summary>
        /// 程序启动时清理缓存目录中的失效图片，如果缓存目录中的图片在持久化的图片信息中找不到对应记录，则删掉
        /// </summary>
        /// <param name="cacheDir">缓存目录</param>
        /// <param name="lastSaveImg">已经持久化的图片信息</param>
        public static void ClearExpireImage( object lastSaveImg)
        {


            List<string> imageList = Directory.EnumerateFiles(cacheDir).ToList();
            foreach (string str in imageList)
            {
                if (!lastSaveImg.ToString().Contains(str.Replace("\\", "/")))
                {
                    File.Delete(str);
                }
            }

        }
        public static void HandClipText(ClipModel clip)
        {
            for (int i = 0; i < 3; i++)
            {
                try
                {
                   
                    string textStr = Clipboard.GetText();

                    if ((MainWindow.supportFormat&ClipType.html)!=0&& Clipboard.ContainsData(DataFormats.Html))
                    {
                        
                        try
                        {
                            string htmlStr = Clipboard.GetData(DataFormats.Html).ToString();
                            //文字类型和html类型中"img"出现的次数一样则说明可以以text类型来解析
                            if (!string.IsNullOrEmpty(htmlStr) && textStr.GetOccurTimes("img") < htmlStr.Split("\r\n".ToCharArray())[16].GetOccurTimes("img"))
                            {

                                clip.ClipValue = htmlStr;
                                //html内容会固定出现在第16行。
                                clip.DisplayValue = htmlStr.Split("\r\n".ToCharArray())[16];
                                clip.Type = HTML_TYPE;
                                clip.Height = 165;
                                return;
                            }
                           

                        }
                        catch { }
                    }

                    clip.ClipValue = textStr;
                    clip.DisplayValue = textStr.Replace("<", "&lt;").Replace(">", "&gt;");
                    clip.Type = TEXT_TYPE;

                    string[] array = clip.DisplayValue.Split('\n');

                    string tempStr = array[0];
                    if (array.Length > 0)
                    {
                        for (int j = 1; j < array.Length; j++)
                        {
                            if (j < 5)
                            {
                                tempStr += "<br>" + array[j];
                            }
                            else if (j == 5&&j<array.Length-1)
                            {
                                tempStr += "<br>...";
                                break;
                            }
                        }
                    }

                    clip.DisplayValue = tempStr;


                    if (array.Length > 5)

                    {
                        clip.Height = 6 * 22;
                    }
                    else if (array.Length > 1)
                    {
                        clip.Height = (array.Length) * 22;
                    }
                    else
                    {
                        clip.Height = 33;
                    }

                    return;

                }
                catch
                {


                }
            }
        }

        public static void HandleClipFile(ClipModel clip)
        {
            for (int i = 0; i < 3; i++)
            {
                try
                {
                    string[] files = (string[])Clipboard.GetData(DataFormats.FileDrop);

                    clip.Type = FILE_TYPE;
                    clip.ClipValue = string.Join(",", files);



                    //组装显示内容，按文件名分行
                    string displayStr = files.Length + " file";
                    if (files.Length > 1)
                    {
                        displayStr += "s";
                    }
                    int j = 0;
                    foreach (string str in files)
                    {
                        if (j < 5)
                        {
                            displayStr += "<br>" + Path.GetFileName(str);
                        }
                        else if (j == 5)
                        {
                            displayStr += "<br>...";
                            break;
                        }
                        j++;
                    }


                    clip.DisplayValue = displayStr;

                    if (files.Length >= 5)

                    {
                        clip.Height = 6 * 22;
                    }
                    else
                    {
                        clip.Height = (files.Length + 1) * 22;
                    }

                    break;
                }
                catch
                {


                }
            }
        }

        public static void HandleClipHtml(ClipModel clip)
        {
            for (int i = 0; i < 3; i++)
            {
                try
                {

                    string htmlStr = Clipboard.GetData(DataFormats.Html).ToString();



                    clip.ClipValue = htmlStr;
                    //html内容会固定出现在第16行。
                    clip.DisplayValue = htmlStr.Split("\r\n".ToCharArray())[16];
                    clip.Type = HTML_TYPE;
                    clip.Height = 165;

                    break;
                }
                catch
                {

                }
            }
        }

        public static void HandleClipImage(ClipModel clip)
        {
            for (int i = 0; i < 3; i++)
            {
                try
                {


                    BitmapSource bs = Clipboard.GetImage();

                    string path = SaveImage(bs);

                    clip.Type = IMAGE_TYPE;
                    clip.ClipValue = path;
                    clip.DisplayValue = path;
                    clip.Height = (int)bs.Height;
                    if (bs.Height > 165)
                    {
                        clip.Height = 165;
                    }
                    break;

                }
                catch
                {


                }
            }
        }

        public static void HandleClipQQ(ClipModel clip)
        {
            for (int i = 0; i < 3; i++)
            {
                try
                {

                    MemoryStream stream = (MemoryStream)Clipboard.GetData(QQ_RICH_TYPE);
                    byte[] b = stream.ToArray();
                    string xmlStr = System.Text.Encoding.UTF8.GetString(b);
                    xmlStr = xmlStr.Substring(0, xmlStr.IndexOf("</QQRichEditFormat>") + "</QQRichEditFormat>".Length);

                    string htmlStr = Clipboard.GetData(DataFormats.Html).ToString();

                    //qq的html内容会固定出现在第14行。
                    htmlStr = htmlStr.Split("\r\n".ToCharArray())[14];
                    if (htmlStr.Contains("\"file:///\""))
                    {

                        XmlDocument document = new XmlDocument();
                        document.LoadXml(xmlStr);
                        foreach (XmlNode node in document.DocumentElement.ChildNodes)
                        {
                            if (node.Name == "EditElement" && node.Attributes["type"].Value == "5") //图片类型
                            {
                                string filePath = node.Attributes["filepath"].Value;
                                if (!htmlStr.Contains(Path.GetFileName(filePath)))
                                {
                                    htmlStr = htmlStr.ReplaceFirst("\"file:///\"", "\"file:///" + filePath.Replace("\\", "/") + "\"");
                                }
                            }
                        }

                    }
                    clip.Type = QQ_RICH_TYPE;
                    clip.ClipValue = xmlStr;

                    if (htmlStr.IndexOf("%") >= 0)
                    {
                        HtmlDocument doc = new HtmlDocument();
                        doc.LoadHtml(htmlStr);
                        foreach (HtmlNode node in doc.DocumentNode.SelectNodes("//img"))
                        {
                            string src = node.GetAttributeValue("src", string.Empty);
                            if (src.IndexOf("%") >= 0)
                            {
                                src = src.Replace("%", "%25");
                            }
                            node.SetAttributeValue("src", src);
                        }
                        htmlStr = doc.DocumentNode.OuterHtml;
                    }
                    clip.DisplayValue = htmlStr;
                    clip.Height = 165;
                    break;



                }
                catch
                {


                }
            }
        }



    }
}
