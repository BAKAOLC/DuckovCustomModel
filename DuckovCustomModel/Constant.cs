using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace DuckovCustomModel
{
    public static class Constant
    {
        public const string ModID = "DuckovCustomModel";
        public const string ModName = "Duckov Custom Model";
        public const string ModVersion = "1.5.0";
        public const string HarmonyId = "com.ritsukage.DuckovCustomModel";

        public const string SoundTagNormal = "normal";
        public const string SoundTagSurprise = "surprise";
        public const string SoundTagDeath = "death";

        public static readonly JsonSerializerSettings JsonSettings = new()
        {
            TypeNameHandling = TypeNameHandling.Auto,
            Formatting = Formatting.Indented,
            Converters = [new StringEnumConverter()],
        };
    }
}