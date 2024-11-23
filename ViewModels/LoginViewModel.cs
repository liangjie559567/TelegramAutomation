using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using OpenQA.Selenium.Interactions;
using SeleniumExtras.WaitHelpers;
using System;
using System.Linq;
using System.Threading.Tasks;
using NLog;
using TelegramAutomation.Exceptions;
using TelegramAutomation.Constants;
using System.Threading;
using System.Collections.ObjectModel;

namespace TelegramAutomation.ViewModels
{
    public class LoginViewModel
    {
        private readonly IWebDriver _driver;
        private readonly ILogger _logger = LogManager.GetCurrentClassLogger();

        public LoginViewModel(IWebDriver driver)
        {
            _driver = driver;
        }

        public async Task EnterPhoneNumber(string phoneNumber)
        {
            try
            {
                _logger.Info("==================== 开始手机号码输入操作 ====================");
                _logger.Info($"当前时间: {DateTime.Now}");
                _logger.Info($"目标手机号: {phoneNumber}");

                // 等待页面加载
                await Task.Delay(3000);
                _logger.Debug("页面等待完成");

                // 使用 Actions 模拟键盘
                var actions = new Actions(_driver);
                _logger.Debug("准备执行键盘操作");

                // 1. 先按 Backspace 删除默认的 +86
                _logger.Debug("开始删除默认号码...");
                for (int i = 0; i < 3; i++)
                {
                    actions.SendKeys(Keys.Backspace)
                          .Pause(TimeSpan.FromMilliseconds(200))
                          .Build()
                          .Perform();
                    _logger.Debug($"已按下 Backspace {i + 1} 次");
                }

                await Task.Delay(500);
                _logger.Debug("默认号码删除完成");

                // 2. 输入新号码
                _logger.Debug("开始输入新号码...");
                foreach (char digit in phoneNumber)
                {
                    actions.SendKeys(digit.ToString())
                          .Pause(TimeSpan.FromMilliseconds(200))
                          .Build()
                          .Perform();
                    _logger.Debug($"已输入数字: {digit}");
                }

                await Task.Delay(1000);
                _logger.Debug("新号码输入完成");

                // 3. 按回车键确认
                _logger.Debug("准备按下回车键...");
                actions.SendKeys(Keys.Enter)
                       .Build()
                       .Perform();
                _logger.Debug("回车键按下完成");

                _logger.Info("手机号码输入完成");
                await Task.Delay(2000);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"手机号码输入失败: {ex.Message}");
                throw new ChromeException("手机号码输入失败: " + ex.Message, ErrorCodes.LOGIN_FAILED, ex);
            }
        }

        public async Task ClickLoginButton()
        {
            try
            {
                _logger.Info("正在查找登录按钮...");
                var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(30));

                // 等待页面加载完成
                wait.Until(driver => ((IJavaScriptExecutor)driver)
                    .ExecuteScript("return document.readyState").Equals("complete"));
                
                await Task.Delay(5000); // 等待页面渲染

                // 如果 JavaScript 点击失败，尝试使用 Selenium 方法
                var loginButton = wait.Until(driver => {
                    var elements = driver.FindElements(By.CssSelector("a, span, div"));
                    return elements.FirstOrDefault(e => 
                        e.Text.Trim() == "LOG IN BY PHONE NUMBER" && 
                        e.Displayed);
                });
                
                if (loginButton != null)
                {
                    var actions = new Actions(_driver);
                    actions.MoveToElement(loginButton)
                           .Pause(TimeSpan.FromSeconds(1))
                           .Click()
                           .Build()
                           .Perform();
                    
                    _logger.Info("使用 Selenium Actions 点击成功");
                    await Task.Delay(2000);
                }
                else
                {
                    throw new ChromeException("未找到登录按钮", ErrorCodes.LOGIN_FAILED);
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "点击登录按钮失败");
                throw new ChromeException("点击登录按钮失败: " + ex.Message, ErrorCodes.LOGIN_FAILED, ex);
            }
        }

        public async Task<string> WaitForVerificationCode()
        {
            var verificationCode = string.Empty;

            _logger.Info("等待用户输入验证码...");
            Console.WriteLine("\n���输入收到的验证码: ");

            // 创建一个任务完成源
            var taskCompletionSource = new TaskCompletionSource<string>();

            // 在新线程中读取控制台输入
            await Task.Run(() =>
            {
                try
                {
                    verificationCode = Console.ReadLine()?.Trim() ?? "";
                    taskCompletionSource.SetResult(verificationCode);
                }
                catch (Exception ex)
                {
                    taskCompletionSource.SetException(ex);
                }
            });

            // 等待用户输入，设置超时时间为5分钟
            using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));
            try
            {
                verificationCode = await taskCompletionSource.Task.WaitAsync(cts.Token);
                _logger.Info($"收到验证码: {verificationCode}");
                return verificationCode;
            }
            catch (OperationCanceledException)
            {
                throw new ChromeException("验证码输入超时", ErrorCodes.LOGIN_TIMEOUT);
            }
        }

        public async Task EnterVerificationCode(string code)
        {
            try
            {
                _logger.Info("==================== ��始验证码输入操作 ====================");
                _logger.Info($"当前时间: {DateTime.Now}");
                _logger.Info($"验证码: {code}");

                // 输入验证码
                var actions = new Actions(_driver);
                foreach (char digit in code)
                {
                    actions.SendKeys(digit.ToString())
                          .Pause(TimeSpan.FromMilliseconds(200))
                          .Build()
                          .Perform();
                }

                await Task.Delay(2000);
                _logger.Info("验证码输入完成");

                // 等待登录完成并验证
                var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(30));
                var loginSuccess = wait.Until(d => {
                    try
                    {
                        // 检查是否有错误消息
                        var errorElements = d.FindElements(By.CssSelector(
                            ".error-message, .alert-error, [class*='error'], .error"
                        ));
                        
                        if (errorElements.Any(e => e.Displayed))
                        {
                            var errorMessage = errorElements.First().Text;
                            _logger.Error($"登录失败，错误信息: {errorMessage}");
                            throw new ChromeException($"登录失败: {errorMessage}", ErrorCodes.LOGIN_FAILED);
                        }

                        // 检查是否存在聊天列表或其他登录成功的标志
                        var chatElements = d.FindElements(By.CssSelector(
                            ".chat-list, .dialogs-container, .messages-container, " +
                            ".sidebar-header, .chat-background, .new-message-button"
                        ));

                        if (chatElements.Any(e => e.Displayed))
                        {
                            _logger.Info("检测到聊天界面，登录成功");
                            return true;
                        }

                        return false;
                    }
                    catch (ChromeException)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        _logger.Debug($"验证登录状态时出错: {ex.Message}");
                        return false;
                    }
                });

                if (!loginSuccess)
                {
                    throw new ChromeException("登录验证超时", ErrorCodes.LOGIN_FAILED);
                }

                _logger.Info("登录验证成功");
                await Task.Delay(2000);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "验证码输入或验证失败");
                throw;
            }
        }

        public async Task NavigateToChannel(string channelName)
        {
            try
            {
                _logger.Info($"==================== 开始切换到频道: {channelName} ====================");
                await Task.Delay(3000);

                // 1. 定位并点击搜索框
                var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(10));
                IWebElement searchBox;

                try
                {
                    searchBox = wait.Until(d => {
                        var elements = d.FindElements(By.CssSelector("div.input-search input"));
                        var element = elements.FirstOrDefault(e => e.Displayed && e.Enabled);
                        if (element == null)
                        {
                            throw new NoSuchElementException("找不到搜索框");
                        }
                        return element;
                    });
                    _logger.Debug($"找到搜索框，使用选择器: div.input-search input");
                }
                catch (Exception ex)
                {
                    _logger.Debug($"使用主选择器查找搜索框失败: {ex.Message}");
                    throw new ChromeException("找不到搜索框", ErrorCodes.CHANNEL_NOT_FOUND, ex);
                }

                // 2. 清除并输入频道名称
                try 
                {
                    // 先尝试使用 JavaScript 点击和聚焦搜索框
                    ((IJavaScriptExecutor)_driver).ExecuteScript(@"
                        arguments[0].click();
                        arguments[0].focus();
                    ", searchBox);
                    await Task.Delay(500);

                    // 再尝试使用 Actions 点击
                    var actions = new Actions(_driver);
                    actions.MoveToElement(searchBox)
                           .Click()
                           .Build()
                           .Perform();
                    await Task.Delay(500);

                    // 清除现有内容
                    searchBox.Clear();
                    await Task.Delay(500);

                    // 使用 Actions 模拟真实的键盘输入
                    actions = new Actions(_driver);
                    foreach (char c in channelName)
                    {
                        actions.SendKeys(c.ToString())
                               .Pause(TimeSpan.FromMilliseconds(100));
                    }
                    actions.Build().Perform();
                    await Task.Delay(1000);

                    // 按回车键
                    actions = new Actions(_driver);
                    actions.SendKeys(Keys.Enter)
                           .Build()
                           .Perform();
                    await Task.Delay(2000);

                    _logger.Debug($"输入频道名称: {channelName}");
                }
                catch (Exception ex)
                {
                    _logger.Debug($"输入频道名称失败: {ex.Message}");
                    throw new ChromeException("输入频道名称失败", ErrorCodes.CHANNEL_NOT_FOUND, ex);
                }

                // 3. 等待搜索结果
                var searchResults = wait.Until(d => {
                    try
                    {
                        // 等待搜索结果加载
                        var results = d.FindElements(By.CssSelector("div.search-group-contacts a.row, div.search-group div.selector-user"));
                        _logger.Debug($"找到 {results.Count} 个搜索结果");
                        
                        var visibleResults = results.Where(e => {
                            try {
                                if (!e.Displayed) return false;
                                
                                var title = e.FindElement(By.CssSelector("span.peer-title")).Text;
                                _logger.Debug($"检查结果: {title}");
                                return title.Contains(channelName, StringComparison.OrdinalIgnoreCase);
                            }
                            catch {
                                return false;
                            }
                        }).ToList();

                        _logger.Debug($"找到 {visibleResults.Count} 个匹配的频道");
                        return visibleResults;
                    }
                    catch (Exception ex)
                    {
                        _logger.Debug($"搜索结果检查失败: {ex.Message}");
                        return new List<IWebElement>();
                    }
                });

                if (searchResults.Any())
                {
                    var firstResult = searchResults.First();
                    try 
                    {
                        // 先滚动到元素位置
                        ((IJavaScriptExecutor)_driver).ExecuteScript("arguments[0].scrollIntoView(true);", firstResult);
                        await Task.Delay(500);

                        // 直接点击
                        firstResult.Click();
                        await Task.Delay(2000);

                        // 验证是否进入频道
                        var success = wait.Until(d => {
                            try
                            {
                                return d.FindElements(By.CssSelector("div.chat-info")).Any() ||
                                       d.Url.Contains("/c/");
                            }
                            catch
                            {
                                return false;
                            }
                        });

                        if (success)
                        {
                            _logger.Info("已成功进入频道");
                            return;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.Error($"点击失败: {ex.Message}");
                        throw new ChromeException("点击频道失败", ErrorCodes.CHANNEL_SWITCH_FAILED, ex);
                    }
                }

                throw new ChromeException($"未找到频道: {channelName}", ErrorCodes.CHANNEL_NOT_FOUND);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"切换频道失败: {ex.Message}");
                throw new ChromeException($"切换频道失败: {ex.Message}", ErrorCodes.CHANNEL_SWITCH_FAILED, ex);
            }
        }
    }
} 