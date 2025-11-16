using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace DuckovCustomModel
{
    public static class Constant
    {
        public const string ModID = "DuckovCustomModel";
        public const string ModName = "Duckov Custom Model";
        public const string ModVersion = "1.8.0-fix1";
        public const string HarmonyId = "com.ritsukage.DuckovCustomModel";

        public static readonly JsonSerializerSettings JsonSettings = new()
        {
            TypeNameHandling = TypeNameHandling.Auto,
            Formatting = Formatting.Indented,
            Converters = [new StringEnumConverter()],
        };
    }
}
