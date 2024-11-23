using System.Collections.Generic;

namespace TelegramAutomation.Models
{
    public class MessageContent
    {
        public string Id { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public List<string> Links { get; set; } = new List<string>();
        public List<FileInfo> Files { get; set; } = new List<FileInfo>();

        public class FileInfo
        {
            public string Name { get; set; } = string.Empty;
            public string Size { get; set; } = string.Empty;
            public string Type { get; set; } = string.Empty;
            public string Url { get; set; } = string.Empty;
        }
    }
} 