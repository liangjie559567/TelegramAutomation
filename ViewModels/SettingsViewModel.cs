using System;
using System.Collections.Generic;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TelegramAutomation.Models;
using System.Windows.Forms;
using NLog;
using System.Linq;

namespace TelegramAutomation.ViewModels
{
    public partial class SettingsViewModel : ObservableObject
    {
        private readonly Window _window;
        private readonly AppSettings _settings;
        private readonly ILogger _logger = LogManager.GetCurrentClassLogger();

        [ObservableProperty]
        private int _maxConcurrentDownloads;

        [ObservableProperty]
        private string _defaultSavePath = string.Empty;

        [ObservableProperty]
        private string _supportedFileExtensions = string.Empty;

        [ObservableProperty]
        private string _chromePath = string.Empty;

        [ObservableProperty]
        private string _userDataPath = string.Empty;

        [ObservableProperty]
        private string _logPath = string.Empty;

        [ObservableProperty]
        private List<string> _logLevels = new() { "Trace", "Debug", "Info", "Warn", "Error", "Fatal" };

        [ObservableProperty]
        private string _selectedLogLevel = "Info";

        public SettingsViewModel(Window window)
        {
            _window = window;
            _settings = AppSettings.Load();
            LoadSettings();
        }

        private void LoadSettings()
        {
            try
            {
                MaxConcurrentDownloads = _settings.DownloadConfig.MaxConcurrentDownloads;
                DefaultSavePath = _settings.DefaultSavePath;
                SupportedFileExtensions = string.Join(Environment.NewLine, 
                    _settings.DownloadConfig.SupportedFileExtensions ?? Array.Empty<string>());
                ChromePath = _settings.ChromeDriver.SearchPaths.FirstOrDefault() ?? string.Empty;
                UserDataPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "TelegramAutomation",
                    "ChromeProfile"
                );
                LogPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    "TelegramAutomation",
                    "logs"
                );
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "加载设置时出错");
                System.Windows.MessageBox.Show("加载设置时出错: " + ex.Message, "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private void BrowseFolder()
        {
            var dialog = new FolderBrowserDialog
            {
                Description = "选择默认保存路径",
                UseDescriptionForTitle = true,
                SelectedPath = DefaultSavePath
            };

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                DefaultSavePath = dialog.SelectedPath;
            }
        }

        [RelayCommand]
        private void BrowseChrome()
        {
            var dialog = new OpenFileDialog
            {
                Title = "选择 Chrome 浏览器路径",
                Filter = "Chrome|chrome.exe|所有文件|*.*",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)
            };

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                ChromePath = dialog.FileName;
            }
        }

        [RelayCommand]
        private void BrowseUserData()
        {
            var dialog = new FolderBrowserDialog
            {
                Description = "选择用户数据目录",
                UseDescriptionForTitle = true,
                SelectedPath = UserDataPath
            };

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                UserDataPath = dialog.SelectedPath;
            }
        }

        [RelayCommand]
        private void BrowseLogPath()
        {
            var dialog = new FolderBrowserDialog
            {
                Description = "选择日志保存路径",
                UseDescriptionForTitle = true,
                SelectedPath = LogPath
            };

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                LogPath = dialog.SelectedPath;
            }
        }

        [RelayCommand]
        private void Save()
        {
            try
            {
                // 更新下载配置
                _settings.DownloadConfig.MaxConcurrentDownloads = MaxConcurrentDownloads;
                _settings.DefaultSavePath = DefaultSavePath;
                _settings.DownloadConfig.SupportedFileExtensions = SupportedFileExtensions
                    .Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => x.Trim())
                    .Where(x => !string.IsNullOrEmpty(x))
                    .ToArray();

                // 更新 Chrome 配置
                if (!string.IsNullOrEmpty(ChromePath))
                {
                    _settings.ChromeDriver.SearchPaths = new[] { ChromePath };
                }

                // 保存设置
                _settings.Save();

                // 更新日志配置
                var config = LogManager.Configuration;
                foreach (var rule in config.LoggingRules)
                {
                    rule.SetLoggingLevels(LogLevel.FromString(SelectedLogLevel), LogLevel.Fatal);
                }
                LogManager.Configuration = config;

                System.Windows.MessageBox.Show("设置已保存", "提示", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
                _window.DialogResult = true;
                _window.Close();
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "保存设置时出错");
                System.Windows.MessageBox.Show("保存设置时出错: " + ex.Message, "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private void Cancel()
        {
            _window.DialogResult = false;
            _window.Close();
        }
    }
} 