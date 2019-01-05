using System.Collections.Generic;

namespace NosCore.Travis
{
    public class DiscordObject
    {
        public string Content { get; set; }

        public string Username { get; set; }

        public string Avatar_url { get; set; }

        public bool Tts { get; set; }

        public string File { get; set; }

        public Embed Embed { get; set; }
    }
}