using System;

namespace TelegramAutomation.Models
{
    public class DownloadProgressEventArgs : EventArgs
    {
        public string FileName { get; }
        public string FileSize { get; }
        public double Progress { get; }
        public string Status { get; }
        public string Message { get; }

        public DownloadProgressEventArgs(
            string fileName, 
            string fileSize, 
            double progress, 
            string status, 
            string message)
        {
            FileName = fileName;
            FileSize = fileSize;
            Progress = progress;
            Status = status;
            Message = message;
        }
    }
} 