using ClipOne.model;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ClipOne.service
{
    public class HotkeyDto
    {
        public int Modifier { get; set; }
        public int Key { get; set; }
    }

    public class TrayMenuDto
    {
        public List<string> Skins { get; set; } = new();
        public string CurrentSkin { get; set; } = "";
        public string CurrentThemeMode { get; set; } = "";
        public bool AutoStartup { get; set; }
    }

    [JsonSourceGenerationOptions(
        WriteIndented = true,
        PropertyNamingPolicy = JsonKnownNamingPolicy.Unspecified,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never
    )]
    [JsonSerializable(typeof(Config))]
    [JsonSerializable(typeof(ClipModel))]
    [JsonSerializable(typeof(List<ClipModel>))]
    [JsonSerializable(typeof(ClipModel[]))]
    [JsonSerializable(typeof(HotkeyDto))]
    [JsonSerializable(typeof(List<string>))]
    [JsonSerializable(typeof(TrayMenuDto))]
    public partial class ClipJsonContext : JsonSerializerContext
    {
    }
}
