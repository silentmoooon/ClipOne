using ClipOne.model;
using ClipOne.util;
using ClipOne.view;
using HtmlAgilityPack;
using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
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

        public const string WECHAT_TYPE = "WeChat_RichEdit_Format";

        /// <summary>
        /// Q文本类型
        /// </summary>
        public const string TEXT_TYPE = "text";




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
        /// 保存图片到缓存目录
        /// </summary>
        /// <param name="bs"></param>
        /// <returns></returns>
        public static string SaveImage(BitmapSource bs)
        {
            string path = MainWindow.cacheDir + "/" + Guid.NewGuid().ToString() + ".bmp";
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
        public static void SetValueToClipboard(ClipModel result)
        {

            if (result.Type == WECHAT_TYPE)
            {
                MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(result.ClipValue));
                IDataObject data = new DataObject(WECHAT_TYPE, ms);
                Clipboard.SetDataObject(data,true);
            }
            else if (result.Type == IMAGE_TYPE)
            {
                try
                {

                    BitmapImage bitImg = new BitmapImage();
                    bitImg.BeginInit();
                    bitImg.UriSource = new Uri(result.ClipValue, UriKind.Relative);
                    bitImg.EndInit();
                    IDataObject data = new DataObject(DataFormats.Bitmap, bitImg);
                    Clipboard.SetDataObject(data,true);


                }
                catch { return; }
            }
            else if (result.Type == HTML_TYPE)
            {
                var dataObject = new DataObject();
                dataObject.SetData(DataFormats.Html, result.ClipValue);
                //dataObject.SetData(DataFormats.Text, "aaa");
                //dataObject.SetData(DataFormats.UnicodeText, "aaa");
                // IDataObject data = new DataObject(DataFormats.Html, result.ClipValue);
                Clipboard.SetDataObject(dataObject,true);
            }
            else if (result.Type == QQ_RICH_TYPE)
            {


                MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(result.ClipValue));
                IDataObject data = new DataObject(QQ_RICH_TYPE, ms);
                Clipboard.SetDataObject(data,true);
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
                    
                    Clipboard.SetDataObject(data,true);
                }
                catch { return; }
            }
            else
            {
                try
                {

                    IDataObject data = new DataObject(DataFormats.Text, result.ClipValue);

                    System.Windows.Forms.Clipboard.SetDataObject(data,true);

                }
                catch (Exception e)
                {
                    
                }
            }
        }




        /// <summary>
        /// 处理剪切板文字类型
        /// </summary>
        /// <param name="clip"></param>
        public static void HandClipText(ClipModel clip)
        {

            for (int i = 0; i < 3; i++)
            {
                try
                {
                    string textStr = Clipboard.GetText();

                    if ((MainWindow.supportFormat & ClipType.html) != 0 && Clipboard.ContainsData(DataFormats.Html))
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
                catch (Exception)
                {

                }
            }




        }

        /// <summary>
        /// 处理剪切板文件类型
        /// </summary>
        /// <param name="clip"></param>
        public static void HandleClipFile(ClipModel clip)
        {

            string[] files = (string[])Clipboard.GetData(DataFormats.FileDrop);
            MemoryStream vMemoryStream = (MemoryStream)Clipboard.GetDataObject().GetData("Preferred DropEffect",true);

            DragDropEffects vDragDropEffects =
            (DragDropEffects)vMemoryStream.ReadByte();

            //如果是剪切类型,不加入
            if ((vDragDropEffects & DragDropEffects.Move) == DragDropEffects.Move) {
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
        public static void HandleClipHtml(ClipModel clip)
        {

            string htmlStr = Clipboard.GetData(DataFormats.Html).ToString().ToLower();
            Console.WriteLine(htmlStr);
            clip.ClipValue = htmlStr;
            string startTag = "<!--startfragment-->";
            //QQ上的多了个空格
            if (!htmlStr.Contains(startTag)){
                startTag = "<!--startfragment -->";
            }
            string endTag = "<!--endfragment-->";
            htmlStr = htmlStr.Substring(htmlStr.IndexOf(startTag) + startTag.Length, htmlStr.IndexOf(endTag) - (htmlStr.IndexOf(startTag) + startTag.Length));

            Console.WriteLine("--");
            Console.WriteLine(htmlStr);
            clip.DisplayValue = htmlStr;

            clip.Type = HTML_TYPE;


        }

        /// <summary>
        /// 处理剪切板图片类型
        /// </summary>
        /// <param name="clip"></param>
        public static void HandleClipImage(ClipModel clip)
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
        public static void HandleClipWeChat(ClipModel clip)
        {

            MemoryStream stream = (MemoryStream)Clipboard.GetData(WECHAT_TYPE);
            byte[] b = stream.ToArray();
            string xmlStr = System.Text.Encoding.UTF8.GetString(b);

            clip.Type = WECHAT_TYPE;
            clip.ClipValue = xmlStr;

            XmlDocument document = new XmlDocument();
            document.LoadXml(xmlStr);
            String displayValue = string.Empty;
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
        public static void HandleClipQQ(ClipModel clip)
        {

            MemoryStream stream = (MemoryStream)Clipboard.GetData(QQ_RICH_TYPE);
            byte[] b = stream.ToArray();
            string xmlStr = System.Text.Encoding.UTF8.GetString(b);
            xmlStr = xmlStr.Substring(0, xmlStr.IndexOf("</QQRichEditFormat>") + "</QQRichEditFormat>".Length);



            XmlDocument document = new XmlDocument();
            document.LoadXml(xmlStr);
            XmlNodeList nodeList = document.SelectNodes("QQRichEditFormat/EditElement[@type='1']|QQRichEditFormat/EditElement[@type='2']|QQRichEditFormat/EditElement[@type='5']");

            int ii = 0;
            string htmlStr = Clipboard.GetData(DataFormats.Html).ToString().ToLower();
            
            string startTag = "<!--startfragment -->";
            string endTag = "<!--endfragment-->";
            htmlStr = htmlStr.Substring(htmlStr.IndexOf(startTag) + startTag.Length, htmlStr.IndexOf(endTag) - (htmlStr.IndexOf(startTag) + startTag.Length));


            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(htmlStr);
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
                string toPath = MainWindow.cacheDir + "/" + Guid.NewGuid().ToString() + Path.GetExtension(filePath);
                try
                {
                    File.Copy(filePath.Replace("file:///", ""), toPath);
                }
                catch
                {

                }
                src = "../" + toPath;

                node.SetAttributeValue("src", src);

                ii++;
            }
            htmlStr = doc.DocumentNode.OuterHtml;

            clip.Type = QQ_RICH_TYPE;
            clip.ClipValue = xmlStr;

            clip.DisplayValue = htmlStr;


        }

         
    }
}
