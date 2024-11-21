using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace TelegramAutomation.Models
{
    public class DownloadItem : INotifyPropertyChanged
    {
        private string _fileName = string.Empty;
        private long _fileSize;
        private int _progress;
        private string _status = "等待中";
        private bool _isPaused;

        public string FileName 
        { 
            get => _fileName;
            set => SetProperty(ref _fileName, value);
        }

        public long FileSize 
        { 
            get => _fileSize;
            set => SetProperty(ref _fileSize, value);
        }

        public string FileSizeDisplay => FormatFileSize(FileSize);

        public int Progress 
        { 
            get => _progress;
            set => SetProperty(ref _progress, value);
        }

        public string Status 
        { 
            get => _status;
            set => SetProperty(ref _status, value);
        }

        public bool IsPaused 
        { 
            get => _isPaused;
            set => SetProperty(ref _isPaused, value);
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        private string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            int order = 0;
            double size = bytes;
            while (size >= 1024 && order < sizes.Length - 1)
            {
                order++;
                size /= 1024;
            }
            return $"{size:0.##} {sizes[order]}";
        }
    }
} 