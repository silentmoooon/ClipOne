# Clipboard Logic

The host app intercepts clipboard updates using standard Win32 hooks (`WM_CLIPBOARDUPDATE`) and parses them into `ClipModel` using `ClipService.cs` (pure Win32 / WinRT native clipboard APIs, 0 WPF dependencies).

## Formats Handled

`ClipService.cs` implements specific extractors for each data format:
- `HandleWeChat`: WeChat RichEdit Format (XML).
- `HandleQQ`: QQ Unicode RichEdit Format (XML + HTML image base64 conversion).
- `HandleHtml`: Standard `HTML Format`. Extracts snippet between `<!--StartFragment-->` and `<!--EndFragment-->`.
- `HandleImage`: Extracts DIB / DIBV5 / Bitmap into Base64 BMP.
- `HandleFile`: Reads `CF_HDROP` file lists and checks `Preferred DropEffect`.
- `HandleText`: Fallback `CF_UNICODETEXT` / `CF_TEXT`.

## Pushing to Clipboard

When setting values back into the clipboard:
1. Turn OFF the global listener hook first (`WinAPIHelper.RemoveClipboardFormatListener(_hWnd)`) to avoid self-feedback loops.
2. Set the data (`clipService.SetValueToClipboard(clip)`).
3. Emulate paste (`KeyboardKit.Keyboard.SendPaste()`).
4. Turn ON the global listener hook (`WinAPIHelper.AddClipboardFormatListener(_hWnd)`).
