using ClipOne.model;
using ClipOne.util;
using ClipOne.view;
using HtmlAgilityPack;
using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;

using System.Text;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Xml;

namespace ClipOne.service
{
    class ClipService
    {
        private readonly Config config;
        public ClipService(Config config)
        {
            this.config = config;
        }
        /// <summary>
        /// 图片类型，通过在内容前面增加前缀来标识
        /// </summary>
        public const string IMAGE_TYPE = "image";
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
        /// 微信富文本类型
        /// </summary>
        public const string WECHAT_TYPE = "WeChat_RichEdit_Format";

        /// <summary>
        /// 文本类型
        /// </summary>
        public const string TEXT_TYPE = "text";


        /// <summary>
        /// 保存图片到缓存目录
        /// </summary>
        /// <param name="bs"></param>
        /// <returns></returns>
        public string SaveImage(BitmapSource bs)
        {
            string path = MainWindow.cacheDir + Path.DirectorySeparatorChar + Guid.NewGuid().ToString() + ".jpg";
            JpegBitmapEncoder jpegEncoder = new JpegBitmapEncoder();
            jpegEncoder.Frames.Add(BitmapFrame.Create(bs));

            using (FileStream fs = File.OpenWrite(path))
            {
                jpegEncoder.Save(fs);

            }

            return path;
        }
        /// <summary>
        /// 设置条目到剪切板
        /// </summary>
        /// <param name="result"></param>
        public void SetValueToClipboard(ClipModel result)
        {

            if (result.Type == WECHAT_TYPE)
            {
                MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(result.ClipValue));
                var dataObject = new DataObject();
                dataObject.SetData(WECHAT_TYPE, ms);
                dataObject.SetData(DataFormats.Text, result.PlainText);
                dataObject.SetData(DataFormats.UnicodeText, result.PlainText);

                Clipboard.SetDataObject(dataObject, true);

            }
            else if (result.Type == IMAGE_TYPE)
            {
                bool isExplorer = false;
                Process[] ps = Process.GetProcesses();
                WinAPIHelper.GetWindowThreadProcessId(WinAPIHelper.GetForegroundWindow(), out int pid);

                foreach (Process p in ps)
                {
                    if (p.Id == pid && p.ProcessName.ToLower() == "explorer")
                    {
                        isExplorer = true;
                        break;
                    }
                }
                //如果是桌面或者资源管理器则直接粘贴为文件
                if (isExplorer)
                {

                    string[] tmp = Path.GetFullPath(result.ClipValue).Split(',');

                    IDataObject data = new DataObject(DataFormats.FileDrop, tmp);
                    MemoryStream memo = new MemoryStream(4);
                    byte[] bytes = new byte[] { (byte)(5), 0, 0, 0 };
                    memo.Write(bytes, 0, bytes.Length);
                    data.SetData("Preferred DropEffect", memo);

                    Clipboard.SetDataObject(data, true);
                }
                else
                {
                    BitmapImage bitImg = new BitmapImage();
                    bitImg.BeginInit();
                    bitImg.UriSource = new Uri(result.ClipValue, UriKind.Relative);

                    bitImg.EndInit();
                    IDataObject data = new DataObject(DataFormats.Bitmap, bitImg);
                    Clipboard.SetDataObject(data, true);

                }


            }
            else if (result.Type == HTML_TYPE)
            {
                var dataObject = new DataObject();
                dataObject.SetData(DataFormats.Html, result.ClipValue);
                dataObject.SetData(DataFormats.Text, result.PlainText);
                dataObject.SetData(DataFormats.UnicodeText, result.PlainText);
                Clipboard.SetDataObject(dataObject, true);
            }
            else if (result.Type == QQ_RICH_TYPE)
            {


                MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(result.ClipValue));
                var dataObject = new DataObject();
                dataObject.SetData(QQ_RICH_TYPE, ms);
                dataObject.SetData(DataFormats.Text, result.PlainText);
                dataObject.SetData(DataFormats.UnicodeText, result.PlainText);

                Clipboard.SetDataObject(dataObject, true);
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
                    data.SetData("Preferred DropEffect", memo);

                    Clipboard.SetDataObject(data, true);
                }
                catch { return; }
            }
            else
            {

                IDataObject data = new DataObject(DataFormats.Text, result.ClipValue);

                System.Windows.Forms.Clipboard.SetDataObject(data, true);


            }
        }



        public ClipModel HandClip()
        {
            ClipModel clip = new ClipModel();
            try
            {
                //处理剪切板微信自定义格式
                if ((config.SupportFormat & ClipType.qq) != 0 && Clipboard.ContainsData(WECHAT_TYPE))
                {
                    HandleWeChat(clip);

                }

                //处理剪切板QQ自定义格式
                else if ((config.SupportFormat & ClipType.qq) != 0 && Clipboard.ContainsData(QQ_RICH_TYPE))
                {

                    HandleQQ(clip);

                }

                //处理HTML类型
                else if ((config.SupportFormat & ClipType.html) != 0 && Clipboard.ContainsData(DataFormats.Html))
                {

                    HandleHtml(clip);

                }
                //处理图片类型
                else if ((config.SupportFormat & ClipType.image) != 0 && (Clipboard.ContainsImage() || Clipboard.ContainsData(DataFormats.Dib)))
                {
                    HandleImage(clip);

                }
                //处理剪切板文件
                else if ((config.SupportFormat & ClipType.file) != 0 && Clipboard.ContainsFileDropList())
                {
                    HandleFile(clip);

                }
                //处理剪切板文字
                else if (Clipboard.ContainsText())
                {

                    HandleText(clip);

                }

            }
            catch { }
            return clip;
        }


        /// <summary>
        /// 处理剪切板文字类型
        /// </summary>
        /// <param name="clip"></param>
        public void HandleText(ClipModel clip)
        {

            string textStr = string.Empty;

            try
            {
                textStr = Clipboard.GetText();

            }
            catch
            {
                if (Clipboard.ContainsData(DataFormats.UnicodeText))
                {
                    textStr = (string)Clipboard.GetData(DataFormats.UnicodeText);
                }
            }
            if (textStr == string.Empty)
            {
                return;
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
                    else if (j == 5 && j < array.Length - 1)
                    {
                        tempStr += "<br>...";
                        break;
                    }
                }
            }

            clip.DisplayValue = tempStr;
            return;





        }

        /// <summary>
        /// 处理剪切板文件类型
        /// </summary>
        /// <param name="clip"></param>
        public void HandleFile(ClipModel clip)
        {

            string[] files = (string[])Clipboard.GetData(DataFormats.FileDrop);
            MemoryStream vMemoryStream = (MemoryStream)Clipboard.GetDataObject().GetData("Preferred DropEffect", true);

            DragDropEffects vDragDropEffects =
            (DragDropEffects)vMemoryStream.ReadByte();

            //如果是剪切类型,不加入
            if ((vDragDropEffects & DragDropEffects.Move) == DragDropEffects.Move)
            {
                return;
            }



            clip.Type = FILE_TYPE;
            clip.ClipValue = string.Join(",", files);



            //组装显示内容，按文件名分行
            string displayStr = "<b>" + files.Length + " file";
            if (files.Length > 1)
            {
                displayStr += "s";
            }
            displayStr += "</b>";
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

        }


        /// <summary>
        /// 处理剪切板HTML类型
        /// </summary>
        /// <param name="clip"></param>
        public void HandleHtml(ClipModel clip)
        {

            string htmlStr = Clipboard.GetData(DataFormats.Html).ToString().Replace("&amp;", "&");

            string plainText = Clipboard.GetText();
 

            //只有当html内容中有图片才当作html格式处理,否则做文本处理
            if (GetOccurTimes(htmlStr.ToLower(), "<img") > GetOccurTimes(plainText.ToLower(), "<img"))
            {

                clip.ClipValue = htmlStr;
               
                string startTag = "<!--StartFragment-->";
                string endTag = "<!--EndFragment-->";
                //QQ上的多了个空格
                if (!htmlStr.Contains(startTag))
                {
                    startTag = "<!--StartFragment -->";
                }

                try
                {
                    htmlStr = htmlStr.Substring(htmlStr.IndexOf(startTag) + startTag.Length, htmlStr.IndexOf(endTag) - (htmlStr.IndexOf(startTag) + startTag.Length));
                }
                catch { }
                clip.DisplayValue = htmlStr;
                clip.PlainText = plainText;

                clip.Type = HTML_TYPE;
            }
            else
            {
                HandleText(clip);

            }




        }

        /// <summary>
        /// 处理剪切板图片类型
        /// </summary>
        /// <param name="clip"></param>
        public void HandleImage(ClipModel clip)
        {
            BitmapSource bs = Clipboard.GetImage();

            string path = SaveImage(bs);

            clip.Type = IMAGE_TYPE;
            clip.ClipValue = path;
            clip.DisplayValue = path;


        }

        /// <summary>
        /// 处理剪切板微信类型
        /// </summary>
        /// <param name="clip"></param>
        public void HandleWeChat(ClipModel clip)
        {

            MemoryStream stream = (MemoryStream)Clipboard.GetData(WECHAT_TYPE);
            string plainText = Clipboard.GetText();
            clip.PlainText = plainText;
            byte[] b = stream.ToArray();
            string xmlStr = System.Text.Encoding.UTF8.GetString(b);

            clip.Type = WECHAT_TYPE;
            clip.ClipValue = xmlStr;

            XmlDocument document = new XmlDocument();
            document.LoadXml(xmlStr);
            string displayValue = string.Empty;
            string value = string.Empty;
            bool onlyText = true;
            foreach (XmlNode node in document.DocumentElement.ChildNodes)
            {
                if (node.Name == "EditElement" && node.Attributes["type"].Value == "0") //文字类型
                {
                    displayValue += node.InnerText;
                    value += node.InnerText;

                }
                else
                {
                    onlyText = false;
                    displayValue += "[表情]";
                    value += " ";
                }
            }
            if (onlyText)
            {
                clip.Type = TEXT_TYPE;
                clip.ClipValue = value;
            }

            clip.DisplayValue = displayValue;



        }

        /// <summary>
        /// 处理剪切板QQ类型
        /// </summary>
        /// <param name="clip"></param>
        public void HandleQQ(ClipModel clip)
        {

            MemoryStream stream = (MemoryStream)Clipboard.GetData(QQ_RICH_TYPE);
            string plainText = Clipboard.GetText();
            clip.PlainText = plainText;
            byte[] b = stream.ToArray();
            string xmlStr = System.Text.Encoding.UTF8.GetString(b);
            xmlStr = xmlStr.Substring(0, xmlStr.IndexOf("</QQRichEditFormat>") + "</QQRichEditFormat>".Length);



            XmlDocument document = new XmlDocument();
            document.LoadXml(xmlStr);
            XmlNodeList nodeList = document.SelectNodes("QQRichEditFormat/EditElement[@type='1']|QQRichEditFormat/EditElement[@type='2']|QQRichEditFormat/EditElement[@type='5']");

            int ii = 0;
            string htmlStr = Clipboard.GetData(DataFormats.Html).ToString();
           
            string startTag;
            if (htmlStr.IndexOf("<!--StartFragment-->") > 0)
            {
                startTag = "<!--StartFragment-->";
            }
            else
            {
                startTag = "<!--StartFragment -->";
            }
            string endTag = "<!--EndFragment-->";
            htmlStr = htmlStr.Substring(htmlStr.IndexOf(startTag) + startTag.Length, htmlStr.IndexOf(endTag) - (htmlStr.IndexOf(startTag) + startTag.Length));

            //如果有img标签
            if (htmlStr.ToLower().IndexOf("<img") >= 0)
            {
                List<string> images = new List<string>();
                HtmlDocument doc = new HtmlDocument();
                doc.LoadHtml(htmlStr);
                 
                var nodes = doc.DocumentNode.SelectNodes("//img");
                if (nodes != null)
                {
                    foreach (HtmlNode node in doc.DocumentNode.SelectNodes("//img"))
                    {
                        string filePath = string.Empty;
                        string src = node.GetAttributeValue("src", string.Empty);
                        if (src == "file:///")
                        {
                            filePath = nodeList[ii].Attributes["filepath"].Value;
                        }
                        else
                        {
                            filePath = src;
                        }
                        string toPath = MainWindow.cacheDir + Path.DirectorySeparatorChar + Guid.NewGuid().ToString() + Path.GetExtension(filePath);
                        try
                        {
                            File.Copy(filePath.Replace("file:///", ""), toPath);
                        }
                        catch
                        {

                        }
                        images.Add(toPath);
                        src = "../" + toPath;

                        node.SetAttributeValue("src", src);

                        ii++;
                    }

                    htmlStr = doc.DocumentNode.OuterHtml;
                    clip.Images = string.Join(",", images);
                }
            }
            clip.Type = QQ_RICH_TYPE;
            clip.ClipValue = xmlStr;

            clip.DisplayValue = htmlStr;



        }
        /// <summary>
        /// 得到字符串B在当前字符串内出现的次数
        /// </summary>
        /// <param name="s"></param>
        /// <param name="str"></param>
        /// <returns></returns>
        private int GetOccurTimes(string str, string value)
        {
            return (str.Length - str.Replace(value, "").Length) / value.Length;
        }

    }
}
