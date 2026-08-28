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
    public partial class ClipJsonContext : JsonSerializerContext
    {
    }
}
