using System;
using System.Windows.Input;
using System.Text;
using System.Threading;
using System.IO;
using System.Threading.Tasks;
using TelegramAutomation.Commands;
using System.Windows.Forms;
using OpenQA.Selenium;
using NLog;
using TelegramAutomation.Models;
using TelegramAutomation.Services;
using System.Diagnostics;
using Microsoft.Win32;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Net.Http;
using System.ComponentModel;
using Microsoft.Extensions.Configuration;

namespace TelegramAutomation.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private readonly ChromeService _chromeService;
        private readonly ILogger _logger = LogManager.GetCurrentClassLogger();
        
        public MainViewModel()
        {
            var settings = LoadAppSettings();
            _chromeService = new ChromeService(settings);
            InitializeCommands();
        }

        private AppSettings LoadAppSettings()
        {
            try
            {
                // 加载配置文件
                var config = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                    .Build();

                var settings = config.Get<AppSettings>();
                if (settings == null)
                    throw new Exception("无法加载应用程序配置");

                return settings;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "加载配置文件失败");
                throw;
            }
        }

        private void InitializeCommands()
        {
            LoginCommand = new RelayCommand(async _ => await ExecuteLoginCommand());
        }

        private async Task ExecuteLoginCommand()
        {
            try
            {
                IsLoading = true;
                Status = "正在初始化Chrome...";

                // 验证Chrome环境
                if (!_chromeService.ValidateChromeEnvironment())
                {
                    throw new ChromeException(
                        "Chrome环境验证失败",
                        ErrorCodes.CHROME_ENVIRONMENT_ERROR
                    );
                }

                // 初始化ChromeDriver
                await _chromeService.InitializeAsync();
                
                Status = "登录成功";
                IsLoggedIn = true;
            }
            catch (Exception ex)
            {
                _logger.LogException(ex, "登录失败");
                Status = $"登录失败: {ex.Message}";
                MessageBox.Show(
                    ex.Message,
                    "登录错误",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
            finally
            {
                IsLoading = false;
            }
        }

        // 属性定义
        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        private string _status = "就绪";
        public string Status
        {
            get => _status;
            set => SetProperty(ref _status, value);
        }

        private bool _isLoggedIn;
        public bool IsLoggedIn
        {
            get => _isLoggedIn;
            set => SetProperty(ref _isLoggedIn, value);
        }

        public ICommand LoginCommand { get; private set; }
    }
}
