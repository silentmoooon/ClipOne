using ClipOne.model;
using ClipOne.util;
using HtmlAgilityPack;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace ClipOne.service
{
    public class ClipService
    {
        private readonly Config config;

        public const string IMAGE_TYPE = "image";
        public const string HTML_TYPE = "html";
        public const string FILE_TYPE = "file";
        public const string QQ_RICH_TYPE = "QQ_Unicode_RichEdit_Format";
        public const string WECHAT_TYPE = "WeChat_RichEdit_Format";
        public const string TEXT_TYPE = "text";

        private static readonly uint FORMAT_WECHAT = WinAPIHelper.RegisterClipboardFormat(WECHAT_TYPE);
        private static readonly uint FORMAT_QQ = WinAPIHelper.RegisterClipboardFormat(QQ_RICH_TYPE);
        private static readonly uint FORMAT_HTML = WinAPIHelper.RegisterClipboardFormat("HTML Format");
        private static readonly uint FORMAT_DROPEFFECT = WinAPIHelper.RegisterClipboardFormat("Preferred DropEffect");

        public ClipService(Config config)
        {
            this.config = config;
        }

        public ClipModel HandClip()
        {
            ClipModel clip = new ClipModel();

            for (int i = 0; i < 3; i++)
            {
                if (!WinAPIHelper.OpenClipboard(IntPtr.Zero))
                {
                    System.Threading.Thread.Sleep(25);
                    continue;
                }

                try
                {
                    // 1. WeChat RichEdit Format
                    if ((config.SupportFormat & ClipType.qq) != 0 && FORMAT_WECHAT != 0 && WinAPIHelper.IsClipboardFormatAvailable(FORMAT_WECHAT))
                    {
                        HandleWeChat(clip);
                    }
                    // 2. QQ Unicode RichEdit Format
                    else if ((config.SupportFormat & ClipType.qq) != 0 && FORMAT_QQ != 0 && WinAPIHelper.IsClipboardFormatAvailable(FORMAT_QQ))
                    {
                        HandleQQ(clip);
                    }
                    // 3. HTML Format
                    else if ((config.SupportFormat & ClipType.html) != 0 && FORMAT_HTML != 0 && WinAPIHelper.IsClipboardFormatAvailable(FORMAT_HTML))
                    {
                        HandleHtml(clip);
                    }
                    // 4. Image (DIB / Bitmap)
                    else if ((config.SupportFormat & ClipType.image) != 0 && (WinAPIHelper.IsClipboardFormatAvailable(WinAPIHelper.CF_DIB) || WinAPIHelper.IsClipboardFormatAvailable(WinAPIHelper.CF_DIBV5)))
                    {
                        HandleImage(clip);
                    }
                    // 5. File Drop (HDROP)
                    else if ((config.SupportFormat & ClipType.file) != 0 && WinAPIHelper.IsClipboardFormatAvailable(WinAPIHelper.CF_HDROP))
                    {
                        HandleFile(clip);
                    }
                    // 6. Text (Unicode)
                    else if (WinAPIHelper.IsClipboardFormatAvailable(WinAPIHelper.CF_UNICODETEXT) || WinAPIHelper.IsClipboardFormatAvailable(WinAPIHelper.CF_TEXT))
                    {
                        HandleText(clip);
                    }

                    return clip;
                }
                catch (Exception ex)
                {
                    Trace.WriteLine($"Error reading clipboard: {ex.Message}");
                }
                finally
                {
                    WinAPIHelper.CloseClipboard();
                }
            }

            return clip;
        }

        public void HandleText(ClipModel clip)
        {
            string text = GetClipboardUnicodeText();
            if (string.IsNullOrEmpty(text))
                return;

            clip.Type = TEXT_TYPE;
            clip.ClipValue = text;
            clip.DisplayValue = FormatDisplayText(text);
        }

        public void HandleFile(ClipModel clip)
        {
            // Check Preferred DropEffect to ignore 'cut/move'
            IntPtr hEffect = WinAPIHelper.GetClipboardData(FORMAT_DROPEFFECT);
            if (hEffect != IntPtr.Zero)
            {
                IntPtr pEffect = WinAPIHelper.GlobalLock(hEffect);
                if (pEffect != IntPtr.Zero)
                {
                    try
                    {
                        byte effect = Marshal.ReadByte(pEffect);
                        // 2 = DROPEFFECT_MOVE
                        if ((effect & 2) != 0)
                        {
                            return;
                        }
                    }
                    finally
                    {
                        WinAPIHelper.GlobalUnlock(hEffect);
                    }
                }
            }

            IntPtr hDrop = WinAPIHelper.GetClipboardData(WinAPIHelper.CF_HDROP);
            if (hDrop == IntPtr.Zero) return;

            uint fileCount = WinAPIHelper.DragQueryFileW(hDrop, 0xFFFFFFFF, null, 0);
            if (fileCount == 0) return;

            string[] files = new string[fileCount];
            StringBuilder sb = new StringBuilder(1024);
            for (uint i = 0; i < fileCount; i++)
            {
                sb.Clear();
                WinAPIHelper.DragQueryFileW(hDrop, i, sb, 1024);
                files[i] = sb.ToString();
            }

            if (files.Length == 1 && File.Exists(files[0]))
            {
                string filePath = files[0];
                string ext = Path.GetExtension(filePath).ToLowerInvariant();
                string pathLower = filePath.ToLowerInvariant();
                string fileName = Path.GetFileNameWithoutExtension(filePath);
                bool isImageExt = ext == ".png" || ext == ".jpg" || ext == ".jpeg" || ext == ".bmp" || ext == ".gif" || ext == ".webp";
                bool isWeChatCache = pathLower.Contains("wechat") || pathLower.Contains("tencent") || pathLower.Contains("wxid_") || pathLower.Contains("xwechat") ||
                    ((pathLower.Contains("temp") || pathLower.Contains("tmp")) && fileName.Length == 32);

                if (isImageExt && isWeChatCache)
                {
                    // WeChat image copy: WeChat writes CF_HDROP first and CF_DIB immediately after.
                    // Ignore the temporary file drop so HandleImage can cleanly capture CF_DIB.
                    return;
                }
            }

            clip.Type = FILE_TYPE;
            clip.ClipValue = string.Join(",", files);

            string displayStr = $"<b>{files.Length} file{(files.Length > 1 ? "s" : "")}</b>";
            for (int j = 0; j < files.Length; j++)
            {
                if (j < 5)
                {
                    displayStr += "<br>" + Path.GetFileName(files[j]);
                }
                else if (j == 5)
                {
                    displayStr += "<br>...";
                    break;
                }
            }
            clip.DisplayValue = displayStr;
        }

        public void HandleHtml(ClipModel clip)
        {
            string htmlStr = GetClipboardHtmlText();
            string plainText = GetClipboardUnicodeText();

            if (string.IsNullOrEmpty(htmlStr))
            {
                HandleText(clip);
                return;
            }

            // If html contains <img> tags, check if it's pure image or rich HTML
            if (GetOccurTimes(htmlStr.ToLowerInvariant(), "<img") > GetOccurTimes(plainText.ToLowerInvariant(), "<img"))
            {
                // If there is no accompanying plain text (or whitespace only) and CF_DIB is available, treat as pure image
                if (string.IsNullOrWhiteSpace(plainText) && (WinAPIHelper.IsClipboardFormatAvailable(WinAPIHelper.CF_DIB) || WinAPIHelper.IsClipboardFormatAvailable(WinAPIHelper.CF_DIBV5)))
                {
                    HandleImage(clip);
                    return;
                }

                clip.ClipValue = htmlStr;
                string fragment = ExtractHtmlFragment(htmlStr);
                clip.DisplayValue = fragment;
                clip.PlainText = plainText;
                clip.Type = HTML_TYPE;

                if (string.IsNullOrEmpty(plainText) && htmlStr.ToLowerInvariant().Contains("gif"))
                {
                    clip.NeedOverride = true;
                }
            }
            else
            {
                HandleText(clip);
            }
        }

        public void HandleImage(ClipModel clip)
        {
            IntPtr hMem = WinAPIHelper.GetClipboardData(WinAPIHelper.CF_DIB);
            if (hMem == IntPtr.Zero)
            {
                hMem = WinAPIHelper.GetClipboardData(WinAPIHelper.CF_DIBV5);
            }

            if (hMem == IntPtr.Zero) return;

            IntPtr pDib = WinAPIHelper.GlobalLock(hMem);
            if (pDib == IntPtr.Zero) return;

            try
            {
                int dibSize = (int)WinAPIHelper.GlobalSize(hMem).ToUInt32();
                if (dibSize <= 0) return;

                byte[] dibBytes = new byte[dibSize];
                Marshal.Copy(pDib, dibBytes, 0, dibSize);

                byte[] bmpBytes = ConvertDibToBmp(dibBytes);
                string base64 = Convert.ToBase64String(bmpBytes);

                clip.Type = IMAGE_TYPE;
                clip.DisplayValue = "image.jpg";
                clip.ClipValue = base64;
            }
            finally
            {
                WinAPIHelper.GlobalUnlock(hMem);
            }
        }

        public void HandleWeChat(ClipModel clip)
        {
            byte[]? bytes = GetClipboardRawBytes(FORMAT_WECHAT);
            if (bytes == null || bytes.Length == 0) return;

            string xmlStr = Encoding.UTF8.GetString(bytes).TrimEnd('\0');
            string plainText = GetClipboardUnicodeText();

            clip.PlainText = plainText;
            clip.Type = WECHAT_TYPE;
            clip.ClipValue = xmlStr;

            try
            {
                System.Xml.XmlDocument doc = new System.Xml.XmlDocument();
                doc.LoadXml(xmlStr);

                string displayValue = string.Empty;
                string value = string.Empty;
                bool onlyText = true;

                if (doc.DocumentElement != null)
                {
                    foreach (System.Xml.XmlNode node in doc.DocumentElement.ChildNodes)
                    {
                        if (node.Name == "EditElement" && node.Attributes?["type"]?.Value == "0")
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
                }

                if (onlyText && !string.IsNullOrEmpty(value))
                {
                    clip.Type = TEXT_TYPE;
                    clip.ClipValue = value;
                }

                clip.DisplayValue = displayValue;
            }
            catch
            {
                clip.DisplayValue = plainText;
            }
        }

        public void HandleQQ(ClipModel clip)
        {
            byte[]? bytes = GetClipboardRawBytes(FORMAT_QQ);
            if (bytes == null || bytes.Length == 0) return;

            string xmlStr = Encoding.UTF8.GetString(bytes).TrimEnd('\0');
            int closeIdx = xmlStr.IndexOf("</QQRichEditFormat>", StringComparison.OrdinalIgnoreCase);
            if (closeIdx >= 0)
            {
                xmlStr = xmlStr.Substring(0, closeIdx + "</QQRichEditFormat>".Length);
            }

            string plainText = GetClipboardUnicodeText();
            clip.PlainText = plainText;

            try
            {
                System.Xml.XmlDocument doc = new System.Xml.XmlDocument();
                doc.LoadXml(xmlStr);
                var nodeList = doc.SelectNodes("QQRichEditFormat/EditElement[@type='1']|QQRichEditFormat/EditElement[@type='2']|QQRichEditFormat/EditElement[@type='3']|QQRichEditFormat/EditElement[@type='5']");

                if (GetOccurTimes(xmlStr, "filepath") == 1 && xmlStr.IndexOf("<![CDATA[", StringComparison.OrdinalIgnoreCase) < 0 && nodeList != null && nodeList.Count > 0)
                {
                    string? filePath = nodeList[0]?.Attributes?["filepath"]?.Value?.Replace("file:///", "");
                    if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
                    {
                        clip.Type = IMAGE_TYPE;
                        clip.DisplayValue = string.Empty;
                        clip.ClipValue = Convert.ToBase64String(File.ReadAllBytes(filePath));
                        clip.PlainText = string.Empty;
                        return;
                    }
                }

                string htmlStr = GetClipboardHtmlText();
                if (!string.IsNullOrEmpty(htmlStr))
                {
                    string fragment = ExtractHtmlFragment(htmlStr);
                    if (fragment.ToLowerInvariant().Contains("<img"))
                    {
                        HtmlDocument hDoc = new HtmlDocument();
                        hDoc.LoadHtml(fragment);
                        var imgNodes = hDoc.DocumentNode.SelectNodes("//img");
                        if (imgNodes != null && nodeList != null)
                        {
                            int ii = 0;
                            foreach (var node in imgNodes)
                            {
                                string src = node.GetAttributeValue("src", string.Empty);
                                string? filePath = src == "file:///" && ii < nodeList.Count ? nodeList[ii]?.Attributes?["filepath"]?.Value : src;
                                if (!string.IsNullOrEmpty(filePath))
                                {
                                    filePath = filePath.Replace("file:///", "");
                                    if (File.Exists(filePath))
                                    {
                                        src = "data:image/png;base64," + Convert.ToBase64String(File.ReadAllBytes(filePath));
                                        node.SetAttributeValue("src", src);
                                    }
                                }
                                ii++;
                            }
                            fragment = hDoc.DocumentNode.OuterHtml;
                        }
                    }
                    clip.DisplayValue = fragment;
                }
                else
                {
                    clip.DisplayValue = plainText;
                }

                clip.Type = QQ_RICH_TYPE;
                clip.ClipValue = xmlStr;
            }
            catch
            {
                clip.Type = QQ_RICH_TYPE;
                clip.ClipValue = xmlStr;
                clip.DisplayValue = plainText;
            }
        }

        public void SetValueToClipboard(ClipModel result)
        {
            if (result == null) return;

            for (int i = 0; i < 3; i++)
            {
                if (!WinAPIHelper.OpenClipboard(IntPtr.Zero))
                {
                    System.Threading.Thread.Sleep(25);
                    continue;
                }

                try
                {
                    WinAPIHelper.EmptyClipboard();

                    if (result.Type == WECHAT_TYPE)
                    {
                        SetClipboardRawBytes(FORMAT_WECHAT, Encoding.UTF8.GetBytes(result.ClipValue));
                        if (!string.IsNullOrEmpty(result.PlainText))
                        {
                            SetClipboardUnicodeText(result.PlainText);
                        }
                    }
                    else if (result.Type == QQ_RICH_TYPE)
                    {
                        SetClipboardRawBytes(FORMAT_QQ, Encoding.UTF8.GetBytes(result.ClipValue));
                        if (!string.IsNullOrEmpty(result.PlainText))
                        {
                            SetClipboardUnicodeText(result.PlainText);
                        }
                    }
                    else if (result.Type == HTML_TYPE)
                    {
                        SetClipboardRawBytes(FORMAT_HTML, Encoding.UTF8.GetBytes(result.ClipValue));
                        if (!string.IsNullOrEmpty(result.PlainText))
                        {
                            SetClipboardUnicodeText(result.PlainText);
                        }
                    }
                    else if (result.Type == IMAGE_TYPE)
                    {
                        byte[] bmpBytes = Convert.FromBase64String(result.ClipValue);
                        byte[] dibBytes = ExtractDibFromBmp(bmpBytes);
                        SetClipboardRawBytes(WinAPIHelper.CF_DIB, dibBytes);

                        // Preferred DropEffect = 5 (Copy)
                        SetClipboardRawBytes(FORMAT_DROPEFFECT, new byte[] { 5, 0, 0, 0 });

                        if (!string.IsNullOrEmpty(result.DisplayValue) && File.Exists(result.DisplayValue))
                        {
                            SetClipboardFiles(new string[] { result.DisplayValue });
                        }
                    }
                    else if (result.Type == FILE_TYPE)
                    {
                        string[] files = result.ClipValue.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                        SetClipboardFiles(files);
                        SetClipboardRawBytes(FORMAT_DROPEFFECT, new byte[] { 5, 0, 0, 0 });
                    }
                    else
                    {
                        SetClipboardUnicodeText(result.ClipValue ?? string.Empty);
                    }

                    break;
                }
                catch (Exception ex)
                {
                    Trace.WriteLine($"Error setting clipboard: {ex.Message}");
                }
                finally
                {
                    WinAPIHelper.CloseClipboard();
                }
            }
        }

        #region Helpers

        private static string GetClipboardUnicodeText()
        {
            IntPtr hMem = WinAPIHelper.GetClipboardData(WinAPIHelper.CF_UNICODETEXT);
            if (hMem == IntPtr.Zero)
            {
                hMem = WinAPIHelper.GetClipboardData(WinAPIHelper.CF_TEXT);
                if (hMem == IntPtr.Zero) return string.Empty;

                IntPtr pStr = WinAPIHelper.GlobalLock(hMem);
                if (pStr == IntPtr.Zero) return string.Empty;
                try
                {
                    return Marshal.PtrToStringAnsi(pStr) ?? string.Empty;
                }
                finally
                {
                    WinAPIHelper.GlobalUnlock(hMem);
                }
            }

            IntPtr pUni = WinAPIHelper.GlobalLock(hMem);
            if (pUni == IntPtr.Zero) return string.Empty;
            try
            {
                return Marshal.PtrToStringUni(pUni) ?? string.Empty;
            }
            finally
            {
                WinAPIHelper.GlobalUnlock(hMem);
            }
        }

        private static string GetClipboardHtmlText()
        {
            byte[]? bytes = GetClipboardRawBytes(FORMAT_HTML);
            if (bytes == null || bytes.Length == 0) return string.Empty;
            return Encoding.UTF8.GetString(bytes).TrimEnd('\0');
        }

        private static byte[]? GetClipboardRawBytes(uint format)
        {
            if (format == 0) return null;
            IntPtr hMem = WinAPIHelper.GetClipboardData(format);
            if (hMem == IntPtr.Zero) return null;

            IntPtr pMem = WinAPIHelper.GlobalLock(hMem);
            if (pMem == IntPtr.Zero) return null;

            try
            {
                int size = (int)WinAPIHelper.GlobalSize(hMem).ToUInt32();
                if (size <= 0) return null;

                byte[] bytes = new byte[size];
                Marshal.Copy(pMem, bytes, 0, size);
                return bytes;
            }
            finally
            {
                WinAPIHelper.GlobalUnlock(hMem);
            }
        }

        private static void SetClipboardUnicodeText(string text)
        {
            if (text == null) text = string.Empty;
            byte[] bytes = Encoding.Unicode.GetBytes(text + "\0");

            IntPtr hMem = WinAPIHelper.GlobalAlloc(WinAPIHelper.GHND, (UIntPtr)bytes.Length);
            if (hMem == IntPtr.Zero) return;

            IntPtr pMem = WinAPIHelper.GlobalLock(hMem);
            if (pMem != IntPtr.Zero)
            {
                Marshal.Copy(bytes, 0, pMem, bytes.Length);
                WinAPIHelper.GlobalUnlock(hMem);
                WinAPIHelper.SetClipboardData(WinAPIHelper.CF_UNICODETEXT, hMem);
            }
        }

        private static void SetClipboardRawBytes(uint format, byte[] bytes)
        {
            if (format == 0 || bytes == null || bytes.Length == 0) return;

            IntPtr hMem = WinAPIHelper.GlobalAlloc(WinAPIHelper.GHND, (UIntPtr)bytes.Length);
            if (hMem == IntPtr.Zero) return;

            IntPtr pMem = WinAPIHelper.GlobalLock(hMem);
            if (pMem != IntPtr.Zero)
            {
                Marshal.Copy(bytes, 0, pMem, bytes.Length);
                WinAPIHelper.GlobalUnlock(hMem);
                WinAPIHelper.SetClipboardData(format, hMem);
            }
        }

        private static void SetClipboardFiles(string[] files)
        {
            if (files == null || files.Length == 0) return;

            StringBuilder sb = new StringBuilder();
            foreach (string file in files)
            {
                sb.Append(file).Append('\0');
            }
            sb.Append('\0');

            byte[] fileBytes = Encoding.Unicode.GetBytes(sb.ToString());
            int structSize = Marshal.SizeOf<WinAPIHelper.DROPFILES>();
            int totalSize = structSize + fileBytes.Length;

            IntPtr hMem = WinAPIHelper.GlobalAlloc(WinAPIHelper.GHND, (UIntPtr)totalSize);
            if (hMem == IntPtr.Zero) return;

            IntPtr pMem = WinAPIHelper.GlobalLock(hMem);
            if (pMem != IntPtr.Zero)
            {
                WinAPIHelper.DROPFILES df = new WinAPIHelper.DROPFILES
                {
                    pFiles = structSize,
                    fWide = true
                };
                Marshal.StructureToPtr(df, pMem, false);
                Marshal.Copy(fileBytes, 0, IntPtr.Add(pMem, structSize), fileBytes.Length);

                WinAPIHelper.GlobalUnlock(hMem);
                WinAPIHelper.SetClipboardData(WinAPIHelper.CF_HDROP, hMem);
            }
        }

        private static byte[] ConvertDibToBmp(byte[] dibBytes)
        {
            if (dibBytes == null || dibBytes.Length < 40) return dibBytes ?? Array.Empty<byte>();

            int biSize = BitConverter.ToInt32(dibBytes, 0);
            short biBitCount = BitConverter.ToInt16(dibBytes, 14);
            int biCompression = BitConverter.ToInt32(dibBytes, 16);
            int biClrUsed = BitConverter.ToInt32(dibBytes, 32);

            int colorTableEntries = 0;
            if (biClrUsed != 0)
            {
                colorTableEntries = biClrUsed;
            }
            else if (biBitCount <= 8)
            {
                colorTableEntries = 1 << biBitCount;
            }
            else if ((biBitCount == 16 || biBitCount == 32) && biCompression == 3)
            {
                colorTableEntries = 3; // 3 DWORD masks
            }

            int colorTableSize = colorTableEntries * 4;
            int offBits = 14 + biSize + colorTableSize;
            int fileSize = 14 + dibBytes.Length;

            byte[] bmp = new byte[fileSize];
            // 'BM'
            bmp[0] = 0x42;
            bmp[1] = 0x4D;
            // File size
            BitConverter.GetBytes(fileSize).CopyTo(bmp, 2);
            // Reserved
            bmp[6] = bmp[7] = bmp[8] = bmp[9] = 0;
            // OffBits
            BitConverter.GetBytes(offBits).CopyTo(bmp, 10);
            // DIB body
            Buffer.BlockCopy(dibBytes, 0, bmp, 14, dibBytes.Length);

            return bmp;
        }

        private static byte[] ExtractDibFromBmp(byte[] bmpBytes)
        {
            if (bmpBytes == null || bmpBytes.Length <= 14) return bmpBytes ?? Array.Empty<byte>();

            if (bmpBytes[0] == 0x42 && bmpBytes[1] == 0x4D)
            {
                byte[] dib = new byte[bmpBytes.Length - 14];
                Buffer.BlockCopy(bmpBytes, 14, dib, 0, dib.Length);
                return dib;
            }

            return bmpBytes;
        }

        private static string ExtractHtmlFragment(string html)
        {
            if (string.IsNullOrWhiteSpace(html)) return string.Empty;

            // 1. Try standard <!--StartFragment--> comments (case-insensitive, flexible spacing)
            string[] startTags = new[] { "<!--StartFragment-->", "<!-- StartFragment -->", "<!--StartFragment -->", "<!-- StartFragment-->" };
            int startComment = -1;
            int startCommentLen = 0;
            foreach (var tag in startTags)
            {
                int idx = html.IndexOf(tag, StringComparison.OrdinalIgnoreCase);
                if (idx >= 0)
                {
                    startComment = idx;
                    startCommentLen = tag.Length;
                    break;
                }
            }

            if (startComment >= 0)
            {
                string[] endTags = new[] { "<!--EndFragment-->", "<!-- EndFragment -->", "<!--EndFragment -->", "<!-- EndFragment-->" };
                int endComment = -1;
                foreach (var tag in endTags)
                {
                    int idx = html.IndexOf(tag, startComment + startCommentLen, StringComparison.OrdinalIgnoreCase);
                    if (idx >= 0)
                    {
                        endComment = idx;
                        break;
                    }
                }

                if (endComment > startComment)
                {
                    return html.Substring(startComment + startCommentLen, endComment - (startComment + startCommentLen)).Trim();
                }
            }

            // 2. Try byte offset extraction if raw bytes available
            byte[]? rawBytes = GetClipboardRawBytes(FORMAT_HTML);
            if (rawBytes != null && rawBytes.Length > 0)
            {
                string headerStr = Encoding.ASCII.GetString(rawBytes, 0, Math.Min(rawBytes.Length, 512));
                var matchStart = System.Text.RegularExpressions.Regex.Match(headerStr, @"StartFragment:(\d+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                var matchEnd = System.Text.RegularExpressions.Regex.Match(headerStr, @"EndFragment:(\d+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);

                if (matchStart.Success && matchEnd.Success &&
                    int.TryParse(matchStart.Groups[1].Value, out int startOffset) &&
                    int.TryParse(matchEnd.Groups[1].Value, out int endOffset) &&
                    startOffset >= 0 && endOffset > startOffset && endOffset <= rawBytes.Length)
                {
                    string fragment = Encoding.UTF8.GetString(rawBytes, startOffset, endOffset - startOffset).Trim();
                    if (!string.IsNullOrEmpty(fragment))
                    {
                        return fragment;
                    }
                }
            }

            // 3. Fallback: Strip CF_HTML header lines and inline headers
            string cleaned = System.Text.RegularExpressions.Regex.Replace(html, @"^(Version:\d+\.\d+|StartHTML:\d+|EndHTML:\d+|StartFragment:\d+|EndFragment:\d+|StartSelection:\d+|EndSelection:\d+|SourceURL:.*?)\r?\n?", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Multiline).Trim();
            cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"^(Version:\d+\.\d+|StartHTML:\d+|EndHTML:\d+|StartFragment:\d+|EndFragment:\d+|StartSelection:\d+|EndSelection:\d+)\s*", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase).Trim();

            return cleaned;
        }

        private static string FormatDisplayText(string text)
        {
            string escaped = text.Replace("<", "&lt;").Replace(">", "&gt;");
            string[] lines = escaped.Split('\n');
            if (lines.Length <= 1) return escaped;

            StringBuilder sb = new StringBuilder(lines[0]);
            for (int i = 1; i < lines.Length; i++)
            {
                if (i < 5)
                {
                    sb.Append("<br>").Append(lines[i]);
                }
                else if (i == 5 && i < lines.Length - 1)
                {
                    sb.Append("<br>...");
                    break;
                }
            }
            return sb.ToString();
        }

        private static int GetOccurTimes(string str, string value)
        {
            if (string.IsNullOrEmpty(str) || string.IsNullOrEmpty(value)) return 0;
            return (str.Length - str.Replace(value, "").Length) / value.Length;
        }

        #endregion
    }
}
