# Clipboard Logic

The host app intercepts clipboard updates using standard Win32 hooks (`WM_CLIPBOARDUPDATE`) and parses them into `ClipModel` using `ClipService.cs`.

## Formats Handled

`ClipService.cs` implements specific extractors for each data format:
- `HandleWeChat`: WeChat RichEdit Format (XML).
- `HandleQQ`: QQ Unicode RichEdit Format (XML).
- `HandleHtml`: Standard `DataFormats.Html`. Extracts image nodes if present.
- `HandleImage`: Extracts dib/image into base64.
- `HandleFile`: Reads `DataFormats.FileDrop` and renders multiple paths.
- `HandleText`: Fallback `DataFormats.UnicodeText`.

## Common Mistakes
- Not checking `Clipboard.ContainsData` carefully before extracting.
- Failing to properly catch exceptions on `Clipboard.GetText()` or `Clipboard.GetData()` (which can throw if the clipboard is locked by another process).
- Missing the loop for retries (e.g., `ClipService.HandClip` loops 3 times to account for OpenClipboard failures).

## Pushing to Clipboard

When setting values back into the clipboard:
1. Turn OFF the global listener hook first (to avoid self-feedback loops).
2. Set the data (`Clipboard.SetDataObject`).
3. Turn ON the global listener hook.

Example from `SinglePaste` in `MainWindow.xaml.cs`:
```csharp
WinAPIHelper.RemoveClipboardFormatListener(wpfHwnd);
clipService.SetValueToClipboard(clip);
SendPasteKey();
WinAPIHelper.AddClipboardFormatListener(wpfHwnd);
```
