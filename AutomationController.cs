using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using NLog;
using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using WindowsInput;
using WindowsInput.Native;
using System.Reflection;
using TelegramAutomation.Models;
using TelegramAutomation.Services;
using System.Diagnostics;
using System.Collections.Concurrent;
using Microsoft.Extensions.Configuration;
using System.Text.Json;
using TelegramAutomation.Exceptions;
using TelegramAutomation.Helpers;

namespace TelegramAutomation
{
    public class AutomationController : IDisposable
    {
        private const string SESSION_FILE = "session.json";
        
        private readonly ILogger _logger = LogManager.GetCurrentClassLogger();
        private readonly IWebDriver _driver;
        private readonly WebDriverWait _wait;
        private bool _isLoggedIn;
        private readonly AppSettings _config;
        private string _currentPhoneNumber = string.Empty;
        private readonly int MAX_LOGIN_RETRIES = 3;
        private readonly int[] RETRY_DELAYS = { 1000, 2000, 5000 }; // 递增延迟
        private readonly TimeSpan LOGIN_TIMEOUT = TimeSpan.FromMinutes(2);
        private const int MAX_LOGIN_ATTEMPTS = 3;
        private const int LOGIN_TIMEOUT_SECONDS = 120;

        private readonly Dictionary<string, By> _loginElements = new()
        {
            { "PhoneInput", By.CssSelector("input[type='tel'][aria-label='Phone number']") },
            { "NextButton", By.CssSelector("button[type='submit'][aria-label='Next']") },
            { "CodeInput", By.CssSelector("input.form-control[inputmode='numeric'][aria-label='Code']") },
            { "SignInButton", By.CssSelector("button[type='submit'][aria-label='Sign In']") },
            { "UserInfo", By.CssSelector("div.user-info") },
            { "ChatList", By.CssSelector("div.chat-list") },
            { "ErrorMessage", By.CssSelector("div.error-message, div.alert-error") },
            { "LoadingIndicator", By.CssSelector("div.loading-progress") }
        };

        // 添加重试配置
        private const int MAX_RETRY_ATTEMPTS = 3;
        private const int DEFAULT_TIMEOUT = 30; // 默认超时时间（秒）
        private const int ELEMENT_TIMEOUT = 10; // 元素等待超时时间（秒）

        private readonly IKeyboardSimulator _keyboard;
        private readonly AppSettings _appSettings;

        public AutomationController(AppSettings settings)
        {
            _appSettings = settings;
            _keyboard = new InputSimulator().Keyboard;
            _config = LoadConfiguration();
            _driver = InitializeWebDriver();
            _wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(_config.WaitTimeout));
        }

        private AppSettings LoadConfiguration()
        {
            // 从配置文件加载设置
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build()
                .Get<AppSettings>();

            return config ?? new AppSettings();
        }

        private IWebDriver InitializeWebDriver()
        {
            var options = new ChromeOptions();
            options.AddArgument("--start-maximized");
            options.AddArgument("--disable-notifications");
            
            return new ChromeDriver(options);
        }

        public async Task RequestVerificationCode(string phoneNumber)
        {
            _logger.LogMethodEntry();
            try
            {
                if (string.IsNullOrWhiteSpace(phoneNumber))
                    throw new LoginException("手机号码不能为空", "PHONE_NUMBER_EMPTY");

                _logger.Info($"开始请求验证码: {phoneNumber}");
                
                await EnsureLoginPageLoaded();
                var phoneInput = await WaitForElementInteractive(_loginElements["PhoneInput"]);
                await ClearAndTypeWithDelay(phoneInput, phoneNumber);
                
                var nextButton = await WaitForElementInteractive(_loginElements["NextButton"]);
                await ClickWithRetry(nextButton);
                
                _logger.LogOperationResult(true, "发送验证码");
            }
            catch (Exception ex)
            {
                _logger.LogException(ex, "请求验证码失败");
                throw new LoginException("请求验证码失败", "VERIFICATION_CODE_REQUEST_FAILED", ex);
            }
            finally
            {
                _logger.LogMethodExit();
            }
        }

        private async Task<IWebElement> WaitForElementInteractive(By locator, int timeoutSeconds = 10)
        {
            var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(timeoutSeconds));
            return await wait.Until(d => {
                try
                {
                    var element = d.FindElement(locator);
                    if (!element.Displayed || !element.Enabled)
                        return null;
                    
                    // 检查元素是否被其他元素遮挡
                    var clickable = (bool)((IJavaScriptExecutor)d).ExecuteScript(@"
                        var elem = arguments[0];
                        var rect = elem.getBoundingClientRect();
                        var cx = rect.left + rect.width/2;
                        var cy = rect.top + rect.height/2;
                        return document.elementFromPoint(cx, cy) === elem;", element);
                    
                    return clickable ? element : null;
                }
                catch
                {
                    return null;
                }
            });
        }

        private async Task ClearAndTypeWithDelay(IWebElement element, string text)
        {
            element.Clear();
            await Task.Delay(500);
            
            foreach (var c in text)
            {
                element.SendKeys(c.ToString());
                await Task.Delay(Random.Shared.Next(50, 150));
            }
        }

        private async Task ClickWithRetry(IWebElement element, int maxRetries = 3)
        {
            for (int i = 0; i < maxRetries; i++)
            {
                try
                {
                    await Task.Delay(500); // 等待元素可交互
                    element.Click();
                    return;
                }
                catch (Exception ex) when (i < maxRetries - 1)
                {
                    _logger.Warn(ex, $"点击元素失败，尝试重试 {i + 1}/{maxRetries}");
                    await Task.Delay(1000 * (i + 1)); // 递增延迟
                }
            }
            throw new TelegramAutomationException("点击元素失败，已达到最大重试次数");
        }

        public async Task<bool> LoginWithRetry(string phoneNumber, string verificationCode)
        {
            int attempts = 0;
            while (attempts < MAX_LOGIN_ATTEMPTS)
            {
                try
                {
                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(LOGIN_TIMEOUT_SECONDS));
                    await LoginWithTimeout(phoneNumber, verificationCode, cts.Token);
                    return true;
                }
                catch (OperationCanceledException)
                {
                    _logger.Warn($"登录超时 (尝试 {attempts + 1}/{MAX_LOGIN_ATTEMPTS})");
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, $"登录失败 (尝试 {attempts + 1}/{MAX_LOGIN_ATTEMPTS})");
                }

                attempts++;
                if (attempts < MAX_LOGIN_ATTEMPTS)
                {
                    await Task.Delay(RETRY_DELAYS[attempts - 1]);
                }
            }

            return false;
        }

        private async Task LoginWithTimeout(string phoneNumber, string verificationCode, CancellationToken token)
        {
            try
            {
                _logger.Info($"开始登录: {phoneNumber}");

                // 等待验证码输入框出现
                var codeInput = _wait.Until(d => d.FindElement(By.CssSelector("input[type='text']")));
                
                // 清除并输入验证码
                codeInput.Clear();
                await Task.Delay(500);

                // 模拟人工输入验证码
                foreach (var c in verificationCode)
                {
                    SimulateKeyPress(c.ToString());
                    await Task.Delay(Random.Shared.Next(50, 150));
                }

                // 点击登录按钮
                var loginButton = _wait.Until(d => d.FindElement(By.CssSelector("button[type='submit']")));
                loginButton.Click();

                // 等待登录完成
                await Task.Delay(_config.LoginWaitTime);

                // 验证登录状态
                _isLoggedIn = await VerifyLoginStatus();

                if (!_isLoggedIn)
                {
                    throw new Exception("登录验证失败");
                }

                _logger.Info("登录成功");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "登录失败");
                throw;
            }
        }

        private async Task<bool> VerifyLoginStatus()
        {
            try
            {
                // 检多个可能的登录成功标志
                var checks = new List<Func<IWebDriver, bool>>
                {
                    d => d.FindElements(By.CssSelector(".chat-list")).Count > 0,
                    d => d.FindElements(By.CssSelector(".messages-container")).Count > 0,
                    d => d.FindElements(By.CssSelector(".tgme_page_extra")).Count > 0
                };

                foreach (var check in checks)
                {
                    try
                    {
                        if (await Task.Run(() => check(_driver)))
                            return true;
                    }
                    catch (StaleElementReferenceException)
                    {
                        // 元素可能已更新，继续检查下一个
                        continue;
                    }
                }

                // 检查是否存在错误消息
                var errorElements = _driver.FindElements(By.CssSelector(".error-message, .alert-error"));
                if (errorElements.Any())
                {
                    _logger.Error($"登录失败: {errorElements.First().Text}");
                    return false;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "验证登录状态时发生错误");
                return false;
            }
        }

        private void SimulateKeyPress(string key)
        {
            _keyboard.TextEntry(key);
        }

        private async Task<T> RetryOperation<T>(Func<Task<T>> operation, int maxRetries = 3)
        {
            for (int i = 0; i < maxRetries; i++)
            {
                try
                {
                    return await operation();
                }
                catch (Exception ex)
                {
                    if (i == maxRetries - 1) throw;
                    _logger.Warn(ex, $"操作失败，正在重试 ({i + 1}/{maxRetries})");
                    await Task.Delay(1000 * (i + 1)); // 指数退避
                }
            }
            throw new Exception("重试次数超过上限");
        }

        public void Dispose()
        {
            try
            {
                _driver?.Quit();
                _driver?.Dispose();
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "关闭浏览器失败");
            }
        }

        private async Task SaveSession()
        {
            try
            {
                var sessionData = new SessionData
                {
                    PhoneNumber = _currentPhoneNumber,
                    LastLoginTime = DateTime.UtcNow,
                    SessionValid = _isLoggedIn
                };

                await File.WriteAllTextAsync(
                    SESSION_FILE, 
                    JsonSerializer.Serialize(sessionData)
                );
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "保存会话信息失败");
            }
        }

        private async Task<bool> LoadSession()
        {
            try
            {
                if (!File.Exists(SESSION_FILE))
                    return false;

                var sessionData = JsonSerializer.Deserialize<SessionData>(
                    await File.ReadAllTextAsync(SESSION_FILE)
                );

                if (sessionData == null)
                    return false;

                // 检查会话是否过期（24小时）
                if (DateTime.UtcNow - sessionData.LastLoginTime > TimeSpan.FromHours(24))
                    return false;

                _currentPhoneNumber = sessionData.PhoneNumber;
                _isLoggedIn = sessionData.SessionValid;
                return _isLoggedIn;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "加载会话信息失败");
                return false;
            }
        }

        public async Task InitializeAsync()
        {
            try
            {
                // 启动时尝试恢复会话
                if (await LoadSession())
                {
                    _logger.Info($"成功恢复会话: {_currentPhoneNumber}");
                    // 验证会话是否真的有效
                    if (!await VerifyLoginStatus())
                    {
                        _logger.Warn("话已失效，需要重新登录");
                        _isLoggedIn = false;
                        await File.DeleteAsync(SESSION_FILE);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "初始化会话失败");
                _isLoggedIn = false;
            }
        }

        private async Task<bool> VerifyLoginStatusComprehensive()
        {
            try
            {
                // 1. 检查基本登录状态
                if (!await VerifyLoginStatus())
                    return false;

                // 2. 检查会话完整性
                try
                {
                    await _driver.FindElement(By.CssSelector(".user-info"))
                        .FindElement(By.CssSelector(".phone-number"));
                    
                    // 3. 尝试访问基本功能
                    await _driver.FindElement(By.CssSelector(".chat-list"));
                    
                    return true;
                }
                catch (NoSuchElementException)
                {
                    _logger.Warn("登录状态验证失败：无法访问关键元素");
                    return false;
                }
                catch (StaleElementReferenceException)
                {
                    _logger.Warn("登录状态验证失败：页面元素已过期");
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "登录状态验证失败");
                return false;
            }
        }

        private async Task WaitForElement(By locator, int timeoutSeconds = 10)
        {
            try
            {
                var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(timeoutSeconds));
                wait.Until(d => d.FindElement(locator).Displayed && d.FindElement(locator).Enabled);
            }
            catch (WebDriverTimeoutException)
            {
                throw new Exception($"LOGIN_ELEMENT_NOT_FOUND: 未找到元素 {locator}");
            }
        }

        private async Task EnsureLoginPageLoaded()
        {
            try
            {
                // 等待页面完全加载
                await WaitForPageLoad();
                
                // 检查是否在登录页面
                if (!IsElementPresent(_loginElements["PhoneInput"]))
                {
                    // 如果不在登录页面，尝试导航到登录页面
                    _driver.Navigate().GoToUrl("https://web.telegram.org/");
                    await WaitForPageLoad();
                    
                    // 再次检查
                    if (!IsElementPresent(_loginElements["PhoneInput"]))
                    {
                        throw new Exception("LOGIN_PAGE_ERROR: 无法加载登录页面");
                    }
                }
                
                // 等待所有必要元素加载完成
                await WaitForElementInteractive(_loginElements["PhoneInput"]);
                await WaitForElementInteractive(_loginElements["NextButton"]);
                
                _logger.Info("登录页面加载完成");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "加载登录页面失败");
                throw new Exception("LOGIN_PAGE_ERROR: 登录页面加载失败或格式异常", ex);
            }
        }

        private async Task WaitForPageLoad()
        {
            try
            {
                await Task.Run(() => {
                    var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(30));
                    wait.Until(d => ((IJavaScriptExecutor)d).ExecuteScript("return document.readyState").Equals("complete"));
                });
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "等待页面加载超时");
                throw;
            }
        }

        private bool IsElementPresent(By locator, int timeoutSeconds = 5)
        {
            try
            {
                var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(timeoutSeconds));
                wait.Until(d => d.FindElement(locator).Displayed);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> SubmitVerificationCode(string code)
        {
            try
            {
                // 等验证码输入框
                var codeInput = await WaitForElementInteractive(_loginElements["CodeInput"]);
                
                // 清除并输入验证码
                await ClearAndTypeWithDelay(codeInput, code);
                
                // 等待登录按钮并点击
                var signInButton = await WaitForElementInteractive(_loginElements["SignInButton"]);
                await ClickWithRetry(signInButton);
                
                // 等待登录完成
                var loginSuccess = await WaitForLoginComplete();
                if (loginSuccess)
                {
                    _isLoggedIn = true;
                    await SaveSession();
                }
                
                return loginSuccess;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "提交验证码失败");
                return false;
            }
        }

        // 添加通用的重试方法
        private async Task<T> RetryWithTimeout<T>(Func<Task<T>> operation, string operationName, int timeoutSeconds = DEFAULT_TIMEOUT)
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
            for (int attempt = 0; attempt < MAX_RETRY_ATTEMPTS; attempt++)
            {
                try
                {
                    return await operation().WaitAsync(cts.Token);
                }
                catch (TimeoutException)
                {
                    _logger.Warn($"{operationName} 操作超时 (尝试 {attempt + 1}/{MAX_RETRY_ATTEMPTS})");
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, $"{operationName} 操作失败 (尝试 {attempt + 1}/{MAX_RETRY_ATTEMPTS})");
                }

                if (attempt < MAX_RETRY_ATTEMPTS - 1)
                {
                    await Task.Delay(RETRY_DELAYS[attempt], cts.Token);
                }
            }
            throw new Exception($"{operationName} 操作失败，已重试 {MAX_RETRY_ATTEMPTS} 次");
        }

        // 添加元素等待方法
        private async Task<IWebElement> WaitForElementWithRetry(By locator, string elementName, int timeoutSeconds = ELEMENT_TIMEOUT)
        {
            return await RetryWithTimeout(async () =>
            {
                var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(timeoutSeconds));
                return await Task.Run(() => wait.Until(d =>
                {
                    try
                    {
                        var element = d.FindElement(locator);
                        if (!element.Displayed || !element.Enabled)
                            return null;

                        // 检查元素是否被遮挡
                        var clickable = (bool)((IJavaScriptExecutor)d).ExecuteScript(@"
                            var elem = arguments[0];
                            var rect = elem.getBoundingClientRect();
                            var cx = rect.left + rect.width/2;
                            var cy = rect.top + rect.height/2;
                            var elemAtPoint = document.elementFromPoint(cx, cy);
                            return elemAtPoint === elem || elem.contains(elemAtPoint);
                        ", element);

                        return clickable ? element : null;
                    }
                    catch
                    {
                        return null;
                    }
                }));
            }, $"等待元素 {elementName}");
        }

        // 修改登录方法，添加更多等��和重试
        public async Task<bool> Login(string phoneNumber, string verificationCode)
        {
            try
            {
                _logger.Info($"开始登录: {phoneNumber}");

                // 等待验证码输入框
                var codeInput = await WaitForElementWithRetry(
                    _loginElements["CodeInput"], 
                    "验证码输入框"
                );

                // 清除并输入验证码
                await RetryWithTimeout(async () =>
                {
                    await ClearAndTypeWithDelay(codeInput, verificationCode);
                    return true;
                }, "输入验证码");

                // 等待并点击登录按钮
                var signInButton = await WaitForElementWithRetry(
                    _loginElements["SignInButton"], 
                    "登录按钮"
                );

                await RetryWithTimeout(async () =>
                {
                    await ClickWithRetry(signInButton);
                    return true;
                }, "点击登录按钮");

                // 等待登录完成
                var loginSuccess = await RetryWithTimeout(
                    async () => await VerifyLoginStatusComprehensive(),
                    "验证登录状态"
                );

                if (loginSuccess)
                {
                    _isLoggedIn = true;
                    _currentPhoneNumber = phoneNumber;
                    await SaveSession();
                    _logger.Info("登录成功");
                }

                return loginSuccess;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "登录失败");
                throw;
            }
        }

        // 添加页面加载等待方法
        private async Task WaitForPageLoadComplete(int timeoutSeconds = DEFAULT_TIMEOUT)
        {
            var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(timeoutSeconds));
            await Task.Run(() => wait.Until(d =>
                ((IJavaScriptExecutor)d).ExecuteScript("return document.readyState").Equals("complete") &&
                ((IJavaScriptExecutor)d).ExecuteScript("return jQuery.active").Equals(0)
            ));
        }

        // 添加网络空闲等待方法
        private async Task WaitForNetworkIdle(int timeoutSeconds = DEFAULT_TIMEOUT)
        {
            await Task.Run(() =>
            {
                var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(timeoutSeconds));
                wait.Until(d => ((IJavaScriptExecutor)d).ExecuteScript(@"
                    return window.performance.getEntriesByType('resource')
                        .filter(r => !r.responseEnd).length === 0;
                "));
            });
        }

        // 修改异步等待的写法
        private async Task WaitForElement(IWebElement element)
        {
            // 使用显式等待替代直接 await element
            await Task.Delay(_appSettings.WaitTimeout);
            if (!element.Displayed)
            {
                throw new Exception("Element not found");
            }
        }

        // 添加缺失的方法
        public async Task StartAutomation(string channelUrl, string savePath, 
            IProgress<string> progress, CancellationToken token)
        {
            try
            {
                progress.Report("正在初始化...");
                await EnsureLoginPageLoaded();

                progress.Report("正在导航到频道...");
                await NavigateToChannel(channelUrl);

                progress.Report("开始下载内容...");
                await DownloadChannelContent(savePath, progress, token);
            }
            catch (OperationCanceledException)
            {
                _logger.Info("任务已取消");
                throw;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "自动化任务失败");
                throw;
            }
        }

        // 实现停止逻辑
        public async Task Stop()
        {
            try
            {
                _logger.Info("正在停止任务...");
                // 清理资源
                await CleanupResources();
                _logger.Info("任务已停止");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "停止任务失败");
                throw;
            }
        }

        // 实现清理会话逻辑
        public async Task ClearSession()
        {
            try
            {
                _logger.Info("正在清理会话...");
                
                // 删除会话文件
                if (File.Exists(SESSION_FILE))
                {
                    await DeleteFileAsync(SESSION_FILE);
                }

                // 清理浏览器缓存
                await ClearBrowserCache();

                // 重置状态
                _isLoggedIn = false;
                _currentPhoneNumber = string.Empty;

                _logger.Info("会话已清理");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "清理会话失败");
                throw;
            }
        }

        // 添加等待登录完成的方法
        private async Task<bool> WaitForLoginComplete(int timeoutSeconds = 30)
        {
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
                var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(timeoutSeconds));

                // 等待登录成功的标志
                await Task.Run(() => wait.Until(d => 
                    d.FindElements(By.CssSelector(".chat-list")).Count > 0 ||
                    d.FindElements(By.CssSelector(".error-message")).Count > 0
                ), cts.Token);

                // 检查是否登录成功
                return await VerifyLoginStatus();
            }
            catch (WebDriverTimeoutException)
            {
                _logger.Error("登录等待超时");
                return false;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "等待登录完成时发生错误");
                return false;
            }
        }

        // 添加导航到频道的方法
        private async Task NavigateToChannel(string channelUrl)
        {
            try
            {
                _driver.Navigate().GoToUrl(channelUrl);
                await WaitForPageLoadComplete();
                
                // 等待频道内容加载
                var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(30));
                await Task.Run(() => wait.Until(d => 
                    d.FindElements(By.CssSelector(".message-list")).Count > 0
                ));
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "导航到频道失败");
                throw;
            }
        }

        // 添加下载频道内容的方法
        private async Task DownloadChannelContent(string savePath, 
            IProgress<string> progress, CancellationToken token)
        {
            try
            {
                // 创建保存目录
                Directory.CreateDirectory(savePath);

                // 获取所有消息元素
                var messages = await GetChannelMessages(token);
                var total = messages.Count;
                var current = 0;

                foreach (var message in messages)
                {
                    token.ThrowIfCancellationRequested();
                    current++;

                    try
                    {
                        // 处理每条消息
                        await ProcessMessage(message, savePath, progress);
                        progress.Report($"处理进度: {current}/{total}");
                    }
                    catch (Exception ex)
                    {
                        _logger.Error(ex, $"处理消息失败 ({current}/{total})");
                        // 继续处理下一条消息
                    }
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "下载频道内容失败");
                throw;
            }
        }

        // 添加获取频道消息的方法
        private async Task<IReadOnlyCollection<IWebElement>> GetChannelMessages(CancellationToken token)
        {
            var messages = new List<IWebElement>();
            var lastCount = 0;
            var sameCountTimes = 0;
            const int MAX_SAME_COUNT = 3;

            while (true)
            {
                token.ThrowIfCancellationRequested();

                // 获取当前页面的消息
                var currentMessages = _driver.FindElements(By.CssSelector(".message"));
                
                if (currentMessages.Count == lastCount)
                {
                    sameCountTimes++;
                    if (sameCountTimes >= MAX_SAME_COUNT)
                    {
                        break; // 可能已到达底部
                    }
                }
                else
                {
                    sameCountTimes = 0;
                }

                lastCount = currentMessages.Count;
                messages = currentMessages.ToList();

                // 滚动到底部
                await ScrollToBottom();
                await Task.Delay(500, token); // 等待新内容加载
            }

            return messages;
        }

        // 添加滚动到底部的方法
        private async Task ScrollToBottom()
        {
            await Task.Run(() => {
                ((IJavaScriptExecutor)_driver).ExecuteScript(
                    "window.scrollTo(0, document.body.scrollHeight);"
                );
            });
        }

        // 添加清理浏览器缓存的方法
        private async Task ClearBrowserCache()
        {
            try
            {
                await Task.Run(() => {
                    _driver.Navigate().GoToUrl("chrome://settings/clearBrowserData");
                    // 等待页面加载
                    Thread.Sleep(1000);
                    // 模拟按下 Tab 和 Enter 键来清理数据
                    _keyboard.KeyPress(VirtualKeyCode.TAB);
                    _keyboard.KeyPress(VirtualKeyCode.RETURN);
                });
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "清理浏览器缓存失败");
            }
        }

        // 添加清理资源的方法
        private async Task CleanupResources()
        {
            try
            {
                // 关闭所有打开的窗口
                var windows = _driver.WindowHandles.ToList();
                foreach (var window in windows)
                {
                    _driver.SwitchTo().Window(window);
                    _driver.Close();
                }

                // 清理临时文件
                var tempFiles = Directory.GetFiles(Path.GetTempPath(), "scoped_dir*");
                foreach (var file in tempFiles)
                {
                    try
                    {
                        await DeleteFileAsync(file);
                    }
                    catch (Exception ex)
                    {
                        _logger.Warn(ex, $"清理临时文件失败: {file}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "清理资源失败");
            }
        }

        // 修改异步等待方法
        private async Task WaitForElement(IWebElement element, int timeoutMs = 5000)
        {
            var wait = new WebDriverWait(_driver, TimeSpan.FromMilliseconds(timeoutMs));
            await Task.Run(() => wait.Until(d => element.Displayed));
        }

        // 修改文件删除方法
        private async Task DeleteFileAsync(string filePath)
        {
            await Task.Run(() => {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
            });
        }
    }
}
