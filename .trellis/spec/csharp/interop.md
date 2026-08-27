# Photino / WebView2 Interop

We use `Photino.NET` to host our HTML/JS-based interface.

## C# to JS

C# sends JSON messages to JS using `window.SendWebMessage(...)`.

Example:
```csharp
string historyJson = JsonSerializer.Serialize(storageService.GetHistory(), ClipJsonContext.Default.ListClipModel);
window.SendWebMessage("{\"type\": \"history\", \"data\": " + historyJson + "}");
```

## JS to C#

JavaScript sends messages back to C# via `window.chrome.webview.postMessage(string)`.
The host receives this in `OnWebMessageReceived(object sender, string message)`.

Format: `Command|Argument`.

Commands supported:
- `PasteValue`: Paste a single item.
- `PasteValueList`: Paste multiple items.
- `SetToClipBoard`: Copy but don't paste.
- `SaveHotkey`: Save updated global shortcut.
- `esc`: Hide the window.
