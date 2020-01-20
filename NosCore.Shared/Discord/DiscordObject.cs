using System.Collections.Generic;

namespace NosCore.Shared.Discord
{
    public class DiscordObject
    {
        public string Content { get; set; }

        public string Username { get; set; }

        public string Avatar_url { get; set; }

        public bool Tts { get; set; }

        public string File { get; set; }

        public List<Embed> Embeds { get; set; }
    }
}