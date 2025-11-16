using System.Linq;

namespace DuckovCustomModel.Core.Data
{
    public class SoundInfo
    {
        public string Path { get; set; } = string.Empty;
        public string[] Tags { get; set; } = [];

        public void Initialize()
        {
            if (Tags is not { Length: > 0 })
            {
                Tags = [SoundTags.Normal];
                return;
            }

            var validTags = SoundTags.ValidTags;
            var tagSet = (from tag in Tags
                where !string.IsNullOrWhiteSpace(tag)
                select tag.ToLowerInvariant().Trim()
                into normalizedTag
                where validTags.Contains(normalizedTag)
                select normalizedTag).ToList();

            if (tagSet.Count == 0) tagSet.Add(SoundTags.Normal);
            Tags = tagSet.ToArray();
        }
    }
}
