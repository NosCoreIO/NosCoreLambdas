using System;
using System.Collections.Generic;

namespace NosCore.Shared.Discord
{
    public class Embed
    {
        public string Title { get; set; }
        public string Type { get; set; }
        public string Description { get; set; }
        public string URL { get; set; }
        public DateTime? Timestamp { get; set; }
        public int? Color { get; set; }
        public Author Author { get; set; }
        public List<Field> Fields { get; set; }
    }
}