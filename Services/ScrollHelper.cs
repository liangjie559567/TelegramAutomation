using OpenQA.Selenium;
using OpenQA.Selenium.Interactions;
using NLog;
using System;
using System.Threading.Tasks;
using System.Linq;
using OpenQA.Selenium.Support.UI;
using TelegramAutomation.Models;

namespace TelegramAutomation.Services
{
    public class ScrollHelper
    {
        private readonly IWebDriver _driver;
        private readonly ILogger _logger = LogManager.GetCurrentClassLogger();
        private const int PAGE_HEIGHT = 800; // 每次滚动的高度

        public ScrollHelper(IWebDriver driver)
        {
            _driver = driver;
        }

        public async Task<bool> ScrollOnceAsync()
        {
            try
            {
                _logger.Debug("执行单次上键滚动...");
                var js = (IJavaScriptExecutor)_driver;
                
                // 获取当前滚动位置
                var currentScrollTop = Convert.ToDouble(js.ExecuteScript(
                    "return document.querySelector('.bubbles').scrollTop;"
                ));
                
                // 计算新的滚动位置
                var newScrollTop = currentScrollTop - PAGE_HEIGHT;
                
                // 执行滚动
                js.ExecuteScript($@"
                    const container = document.querySelector('.bubbles');
                    if (container) {{
                        container.scrollTo({{
                            top: {newScrollTop},
                            behavior: 'smooth'
                        }});
                    }}
                ");
                
                // 等待滚动完成
                await Task.Delay(1000);
                
                // 验证滚动是否成功
                var actualScrollTop = Convert.ToDouble(js.ExecuteScript(
                    "return document.querySelector('.bubbles').scrollTop;"
                ));
                
                _logger.Debug($"滚动前位置: {currentScrollTop}, 滚动后位置: {actualScrollTop}");
                
                // 如果滚动位置变化超过100像素，认为滚动成功
                return Math.Abs(actualScrollTop - currentScrollTop) > 100;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "执行单次滚动时出错");
                return false;
            }
        }

        public async Task SmartScrollAsync()
        {
            try
            {
                _logger.Debug("开始执行上键滚动...");
                
                // 1. 先找到聊天窗口并点击右侧空白区域
                var chatWindow = _driver.FindElement(By.CssSelector(".chat.tabs-tab.active"));
                var actions = new Actions(_driver);
                
                // 获取窗口的位置和大小
                var location = chatWindow.Location;
                var size = chatWindow.Size;
                
                _logger.Debug($"聊天窗口位置: x={location.X}, y={location.Y}, width={size.Width}, height={size.Height}");
                
                // 先移动到元素中心，然后再向右移动一点
                actions.MoveToElement(chatWindow)  // 移动到元素中心
                       .MoveByOffset(size.Width / 4, 0)  // 向右移动1/4宽
                       .Click()
                       .Perform();
                
                _logger.Debug("已点击右侧区域");
                await Task.Delay(100);  // 等待点击生效
                
                // 2. 记录上一次的第一条消息位置
                var lastFirstMessageY = double.MinValue;
                var samePositionCount = 0;
                var noChangeCount = 0;
                
                // 3. 持续按上键直到真正到达顶部
                while (true)
                {
                    // 获取当前所有消息
                    var messages = _driver.FindElements(By.CssSelector(".message"));
                    if (messages.Count == 0)
                    {
                        _logger.Debug("未找到任何消息，继续尝试");
                        await Task.Delay(100);
                        continue;
                    }

                    // 获取第一条消息的位置
                    var firstMessage = messages.First();
                    var firstMessageRect = firstMessage.GetAttribute("data-mid");
                    var firstMessageY = firstMessage.Location.Y;
                    
                    _logger.Debug($"当前第一条消息ID: {firstMessageRect}, Y位置: {firstMessageY}, 上次位置: {lastFirstMessageY}");
                    
                    // 如果位置没有变化，计数器加1
                    if (Math.Abs(firstMessageY - lastFirstMessageY) < 1)
                    {
                        samePositionCount++;
                        noChangeCount++;
                        _logger.Debug($"位置未变化，计数: {samePositionCount}, 总计: {noChangeCount}");
                        
                        // 如果连续5次位置都没变，尝试多按几次上键
                        if (samePositionCount >= 5)
                        {
                            _logger.Debug("连续5次位置未变，尝试多按几次上键");
                            // 快速连按3次
                            for (int i = 0; i < 3; i++)
                            {
                                actions.SendKeys(Keys.ArrowUp)
                                       .Pause(TimeSpan.FromMilliseconds(20))
                                       .Perform();
                            }
                            samePositionCount = 0;  // 重置连续计数
                            await Task.Delay(200);
                            continue;
                        }

                        // 如果总共20次都没有变化，才认为到达顶部
                        if (noChangeCount >= 20)
                        {
                            _logger.Debug("总共20次位置未变，判定已到达顶部");
                            break;
                        }
                    }
                    else
                    {
                        // 位置有变化，重置计数器
                        samePositionCount = 0;
                        noChangeCount = 0;
                        lastFirstMessageY = firstMessageY;
                    }
                    
                    // 平滑滚动：每次按键后等待较短时间
                    actions.SendKeys(Keys.ArrowUp)
                           .Pause(TimeSpan.FromMilliseconds(20))
                           .Perform();
                    
                    // 每按10次键后稍微暂停一下，让内容加载
                    if (noChangeCount % 10 == 0)
                    {
                        await Task.Delay(100);
                    }
                    else
                    {
                        await Task.Delay(20);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "执行上键滚动时出错");
            }
        }

        public async Task<ScrollStatus> GetScrollStatusAsync()
        {
            try
            {
                var js = (IJavaScriptExecutor)_driver;
                var status = new ScrollStatus();

                // 获取滚动位置和高度
                var script = @"
                    const container = document.querySelector('.bubbles');
                    if (!container) return null;
                    return {
                        scrollTop: container.scrollTop,
                        scrollHeight: container.scrollHeight,
                        clientHeight: container.clientHeight
                    }";

                var result = await Task.Run(() => js.ExecuteScript(script));
                
                if (result == null)
                {
                    return status;
                }

                var dict = (Dictionary<string, object>)result;
                status.ScrollPosition = Convert.ToDouble(dict["scrollTop"]);
                status.ScrollHeight = Convert.ToDouble(dict["scrollHeight"]);
                
                // 检查是否在顶部或底部
                status.IsAtTop = status.ScrollPosition <= 0;
                status.IsAtBottom = status.ScrollPosition + Convert.ToDouble(dict["clientHeight"]) >= status.ScrollHeight;

                // 获取第一条消息位置
                var firstMessage = _driver.FindElements(By.CssSelector(".message"))?[0];
                if (firstMessage != null)
                {
                    status.FirstMessagePosition = new MessagePosition
                    {
                        OffsetTop = Convert.ToDouble(((IJavaScriptExecutor)_driver).ExecuteScript(
                            "return arguments[0].offsetTop;", firstMessage))
                    };
                }

                return status;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "获取滚动状态失败");
                return new ScrollStatus();
            }
        }

        public bool IsElementInViewport(IWebElement element)
        {
            try
            {
                var js = (IJavaScriptExecutor)_driver;
                var script = @"
                    const rect = arguments[0].getBoundingClientRect();
                    const container = document.querySelector('.bubbles');
                    if (!container) return false;
                    const containerRect = container.getBoundingClientRect();
                    return (
                        rect.top >= containerRect.top &&
                        rect.bottom <= containerRect.bottom
                    );";

                return (bool)js.ExecuteScript(script, element);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "检查元素是否在视口内时出错");
                return false;
            }
        }

        public async Task FocusChatInput()
        {
            try
            {
                var chatInput = _driver.FindElement(By.CssSelector(".input-message-input"));
                var js = (IJavaScriptExecutor)_driver;
                
                // 使用JavaScript模拟点击，更可靠
                js.ExecuteScript("arguments[0].click();", chatInput);
                await Task.Delay(100);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "聚焦聊天输入框时出错");
            }
        }

        public async Task<bool> CheckIfAtTopAsync()
        {
            try
            {
                var messages = await Task.Run(() => _driver.FindElements(By.CssSelector(".message")));
                if (messages.Count == 0) return false;

                var firstMessage = messages[0];
                var firstMessageY = firstMessage.Location.Y;
                
                return firstMessageY > -50;  // 允许一点误差
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "检查是否到达顶部失败");
                return false;
            }
        }
    }
}
