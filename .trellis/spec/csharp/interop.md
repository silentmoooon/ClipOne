# WebView2 Interop

We use `Microsoft.Web.WebView2` to host our HTML/JS-based interface.

## C# to JS

C# executes JavaScript inside the WebView using `ExecuteScriptAsync`.

Example from `MainWindow.xaml.cs`:
```csharp
// Add data by serializing the ClipModel to JSON and URL encoding it
string json = JsonConvert.SerializeObject(clip);
json = HttpUtility.UrlEncode(json);
webView1.CoreWebView2.ExecuteScriptAsync($"addData('{json}')");
```
**Important**: Note the use of `HttpUtility.UrlEncode` before sending JSON, which `addData` decodes on the JS side using `decodeURIComponent(data.replace(/\+/g, "%20"))`.

## JS to C#

JavaScript sends messages back to C# via `window.chrome.webview.postMessage(string)`.
The WPF host receives this in `CoreWebView2_WebMessageReceived`.

We use a pipe `|` delimited format: `Command|Argument`.

Commands supported:
- `PasteValue`: Paste a single item.
- `PasteValueList`: Paste multiple items.
- `SetToClipBoard`: Copy but don't paste.
- `esc`: Close the window.
- `search`: Initiate search.

Example:
```csharp
string value = e.TryGetWebMessageAsString();
string[] args = value.Split(new char[] { '|' }, 2);
if (args[0] == "PasteValue") {
    PasteValue(args[1]);
}
```
