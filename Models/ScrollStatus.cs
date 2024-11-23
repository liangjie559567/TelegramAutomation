using TelegramAutomation.Models;

namespace TelegramAutomation.Models
{
    public class ScrollStatus
    {
        public bool IsAtBottom { get; set; }
        public bool IsAtTop { get; set; }
        public double ScrollPosition { get; set; }
        public double ScrollHeight { get; set; }
        public MessagePosition? FirstMessagePosition { get; set; }
    }

    public class MessagePosition
    {
        public double OffsetTop { get; set; }
        public DOMRect? BoundingRect { get; set; }
    }
} 