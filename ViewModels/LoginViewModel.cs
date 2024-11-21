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

                // 使用 Actions 模拟键盘操作
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

                // 使用 JavaScript 查找和点击按钮
                var jsExecutor = (IJavaScriptExecutor)_driver;
                var script = @"
                    function findAndClickLoginButton() {
                        // 查找所有链接元素
                        const links = Array.from(document.querySelectorAll('a, span, div'));
                        
                        // 查找紫色的 'LOG IN BY PHONE NUMBER' 文本
                        const loginLink = links.find(link => {
                            const text = link.textContent.trim();
                            if (text === 'LOG IN BY PHONE NUMBER') {
                                const style = window.getComputedStyle(link);
                                const color = style.color;
                                // 检查是否为紫色
                                return color.includes('rgb(126, 116, 223)') || 
                                       color.includes('rgb(147, 112, 219)') ||
                                       color.includes('rgb(138, 43, 226)');
                            }
                            return false;
                        });

                        if (loginLink) {
                            console.log('找到登录链接:', loginLink);
                            loginLink.scrollIntoView({behavior: 'smooth', block: 'center'});
                            
                            // 创建并分发点击事件
                            const clickEvent = new MouseEvent('click', {
                                view: window,
                                bubbles: true,
                                cancelable: true
                            });
                            loginLink.dispatchEvent(clickEvent);
                            return true;
                        }
                        return false;
                    }
                    return findAndClickLoginButton();
                ";

                var result = jsExecutor.ExecuteScript(script);

                if (result is bool success && success)
                {
                    _logger.Info("登录按钮点击成功");
                    await Task.Delay(2000);
                }
                else
                {
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
            Console.WriteLine("\n请输入收到的验证码: ");

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
                _logger.Info("==================== 开始验证码输入操作 ====================");
                _logger.Info($"当前时间: {DateTime.Now}");
                _logger.Info($"验证码: {code}");

                // 等待页面加载
                await Task.Delay(2000);

                // 使用 Actions 模拟键盘操���
                var actions = new Actions(_driver);

                // 输入验证码
                foreach (char digit in code)
                {
                    actions.SendKeys(digit.ToString())
                          .Pause(TimeSpan.FromMilliseconds(200))
                          .Build()
                          .Perform();
                }

                await Task.Delay(1000);

                // 按回车确认
                actions.SendKeys(Keys.Enter)
                       .Build()
                       .Perform();

                _logger.Info("验证码输入完成");
                await Task.Delay(2000);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"证码输入失败: {ex.Message}");
                throw new ChromeException("验证码输入失败: " + ex.Message, ErrorCodes.LOGIN_FAILED, ex);
            }
        }

        public async Task NavigateToChannel(string channelName)
        {
            try
            {
                _logger.Info($"==================== 开始切换到频道: {channelName} ====================");
                await Task.Delay(3000); // 等待登录完成

                // 1. 点击搜索框
                var actions = new Actions(_driver);
                actions.SendKeys(Keys.Control + "k")  // 使用快捷键 Ctrl+K 打开搜索
                       .Build()
                       .Perform();

                await Task.Delay(1000);

                // 2. 输入频道名称
                _logger.Debug($"输入频道名称: {channelName}");
                actions = new Actions(_driver);
                actions.SendKeys(channelName)
                       .Pause(TimeSpan.FromMilliseconds(500))
                       .Build()
                       .Perform();

                await Task.Delay(2000);

                // 3. 等待搜索结果并点击
                var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(10));

                try
                {
                    // 等待搜索结果加载
                    var searchResults = wait.Until(driver => {
                        var elements = driver.FindElements(By.CssSelector(".search-result, .chatlist-chat, .chat-item, .ListItem"));
                        return elements.Any(e => e.Displayed) ? elements : null;
                    });

                    if (searchResults != null)
                    {
                        // 找到匹配的结果
                        var targetResult = searchResults.FirstOrDefault(e => 
                            e.Text.Contains(channelName, StringComparison.OrdinalIgnoreCase));

                        if (targetResult != null)
                        {
                            _logger.Debug($"找到搜索结果: {targetResult.Text}");

                            // 使用 JavaScript 点击
                            var jsExecutor = (IJavaScriptExecutor)_driver;
                            jsExecutor.ExecuteScript("arguments[0].scrollIntoView({behavior: 'smooth', block: 'center'});", targetResult);
                            await Task.Delay(1000);

                            // 尝试多种点击方式
                            try
                            {
                                // 1. 直接点击
                                targetResult.Click();
                            }
                            catch
                            {
                                try
                                {
                                    // 2. JavaScript 点击
                                    jsExecutor.ExecuteScript("arguments[0].click();", targetResult);
                                }
                                catch
                                {
                                    // 3. Actions 点击
                                    actions = new Actions(_driver);
                                    actions.MoveToElement(targetResult)
                                           .Click()
                                           .Build()
                                           .Perform();
                                }
                            }

                            // 等待页面加载
                            await Task.Delay(2000);

                            // 验证是否成功进入频道
                            var channelTitle = wait.Until(driver => {
                                var titles = driver.FindElements(By.CssSelector(".chat-info, .peer-title"));
                                return titles.FirstOrDefault(t => t.Displayed && t.Text.Contains(channelName, StringComparison.OrdinalIgnoreCase));
                            });

                            if (channelTitle != null)
                            {
                                _logger.Info($"已成功进入频道: {channelTitle.Text}");
                            }
                            else
                            {
                                throw new ChromeException($"无法验证是否已进入频道: {channelName}", ErrorCodes.CHANNEL_SWITCH_FAILED);
                            }
                        }
                        else
                        {
                            throw new ChromeException($"未找到匹配的频道: {channelName}", ErrorCodes.CHANNEL_NOT_FOUND);
                        }
                    }
                    else
                    {
                        throw new ChromeException($"搜索结果为空: {channelName}", ErrorCodes.CHANNEL_NOT_FOUND);
                    }
                }
                catch (WebDriverTimeoutException)
                {
                    _logger.Error($"搜索超时: {channelName}");
                    throw new ChromeException($"搜索超时: {channelName}", ErrorCodes.CHANNEL_NOT_FOUND);
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"切换频道失败: {ex.Message}");
                throw new ChromeException($"切换频道失败: {ex.Message}", ErrorCodes.CHANNEL_SWITCH_FAILED, ex);
            }
        }
    }
} 