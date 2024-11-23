using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using NLog;
using System;
using System.Threading.Tasks;
using TelegramAutomation.Models;
using System.Linq;
using System.Collections.Generic;
using System.Text.Json;

namespace TelegramAutomation.Services
{
    public class ScrollHelper
    {
        private readonly IWebDriver _driver;
        private readonly ILogger _logger = LogManager.GetCurrentClassLogger();
        private readonly int _scrollTimeout;
        private readonly int _scrollDelay;

        public ScrollHelper(IWebDriver driver, int scrollTimeout = 30000, int scrollDelay = 1000)
        {
            _driver = driver;
            _scrollTimeout = scrollTimeout;
            _scrollDelay = scrollDelay;
        }

        public async Task<bool> ScrollToLoadMessagesAsync(int targetMessageCount, IProgress<string> progress)
        {
            var startTime = DateTime.Now;
            var lastMessageCount = 0;
            var noNewMessageCount = 0;
            var currentMessages = 0;

            while ((DateTime.Now - startTime).TotalMilliseconds < _scrollTimeout)
            {
                try
                {
                    // 获取当前消息数量
                    currentMessages = GetCurrentMessageCount();
                    progress.Report($"已加载消息数量: {currentMessages}");

                    // 检查是否达到目标数量
                    if (currentMessages >= targetMessageCount)
                    {
                        _logger.Info($"已达到目标消息数量: {targetMessageCount}");
                        return true;
                    }

                    // 检查是否有新消息加载
                    if (currentMessages == lastMessageCount)
                    {
                        noNewMessageCount++;
                        if (noNewMessageCount >= 3) // 连续3次没有新消息，检查是否到底
                        {
                            var isAtBottom = await CheckIfAtBottomAsync();
                            if (isAtBottom)
                            {
                                _logger.Info("已到达底部，停止加载");
                                return false;
                            }
                        }
                    }
                    else
                    {
                        noNewMessageCount = 0;
                        lastMessageCount = currentMessages;
                    }

                    // 执行滚动
                    await SmartScrollAsync();
                    await Task.Delay(_scrollDelay); // 等待消息加载
                }
                catch (Exception ex)
                {
                    _logger.Error($"滚动加载消息失败: {ex.Message}");
                    return false;
                }
            }

            _logger.Warn($"加载超时，当前消息数量: {currentMessages}");
            return false;
        }

        public async Task SmartScrollAsync()
        {
            try
            {
                await Task.Run(() =>
                {
                    ((IJavaScriptExecutor)_driver).ExecuteScript(@"
                        function smartScroll() {
                            const container = document.querySelector('.bubbles');
                            if (!container) return;
                            
                            // 获取所有消息元素
                            const messages = Array.from(container.querySelectorAll('.message'));
                            if (!messages.length) return;
                            
                            // 获取当前可见的消息
                            const visibleMessages = messages.filter(msg => {
                                const rect = msg.getBoundingClientRect();
                                return rect.top >= 0 && rect.bottom <= window.innerHeight;
                            });
                            
                            if (visibleMessages.length > 0) {
                                // 获取第一条可见消息的索引
                                const firstVisibleIndex = messages.indexOf(visibleMessages[0]);
                                
                                // 计算目标滚动位置（前一条消息）
                                const targetIndex = Math.max(0, firstVisibleIndex - 1);
                                const targetMessage = messages[targetIndex];
                                
                                // 滚动到目标消息
                                if (targetMessage && targetIndex < firstVisibleIndex) {
                                    const targetTop = targetMessage.offsetTop;
                                    container.scrollTo({
                                        top: Math.max(0, targetTop - 100),
                                        behavior: 'instant'
                                    });
                                    
                                    // 触发滚动事件以确保加载
                                    container.dispatchEvent(new Event('scroll'));
                                }
                            }
                        }
                        smartScroll();
                    ");
                });
                
                // 等待内容加载
                await Task.Delay(2000);
            }
            catch (Exception ex)
            {
                _logger.Error($"智能滚动失败: {ex.Message}");
                throw;
            }
        }

        public bool IsElementInViewport(IWebElement element)
        {
            try
            {
                var js = (IJavaScriptExecutor)_driver;
                var result = js.ExecuteScript(@"
                    function isInViewport(el) {
                        const container = document.querySelector('.bubbles');
                        if (!container) return false;
                        
                        const rect = el.getBoundingClientRect();
                        const containerRect = container.getBoundingClientRect();
                        
                        return (
                            rect.top >= containerRect.top - 10 &&
                            rect.bottom <= containerRect.bottom + 10 &&
                            rect.height > 0 &&
                            rect.width > 0
                        );
                    }
                    return isInViewport(arguments[0]);
                ", element);

                return result is bool isVisible && isVisible;
            }
            catch (Exception ex)
            {
                _logger.Warn($"检查元素可见性失败: {ex.Message}");
                return false;
            }
        }

        private int GetCurrentMessageCount()
        {
            try
            {
                return _driver.FindElements(By.CssSelector(".message")).Count;
            }
            catch (Exception ex)
            {
                _logger.Warn($"获取消息数量失败: {ex.Message}");
                return 0;
            }
        }

        private async Task<bool> CheckIfAtBottomAsync()
        {
            try
            {
                // 使用 await Task.Run 包装 JavaScript 执行
                var result = await Task.Run(() => 
                {
                    return ((IJavaScriptExecutor)_driver).ExecuteScript(@"
                        const container = document.querySelector('.bubbles');
                        if (!container) return false;
                        return container.scrollTop + container.clientHeight >= container.scrollHeight - 50;
                    ");
                });

                return result is bool isAtBottom && isAtBottom;
            }
            catch (Exception ex)
            {
                _logger.Warn($"检查是否到底失败: {ex.Message}");
                return false;
            }
        }

        public async Task<ScrollStatus?> GetScrollStatusAsync()
        {
            try
            {
                _logger.Debug("获取滚动状态...");
                
                // 声明变量
                ScrollStatus? scrollStatus = null;
                
                // 执行JavaScript获取滚动状态
                var jsResult = await Task.Run(() => 
                {
                    var result = ((IJavaScriptExecutor)_driver).ExecuteScript(@"
                        const container = document.querySelector('.bubbles');
                        if (!container) {
                            console.log('未找到消息容器');
                            return null;
                        }
                        
                        const messages = container.querySelectorAll('.message');
                        console.log('找到消息数量:', messages.length);
                        
                        const firstMessage = messages.length > 0 ? messages[0] : null;
                        const lastMessage = messages.length > 0 ? messages[messages.length - 1] : null;
                        
                        return {
                            containerHeight: container.clientHeight,
                            scrollHeight: container.scrollHeight,
                            scrollTop: container.scrollTop,
                            messageCount: messages.length,
                            firstMessageY: firstMessage ? firstMessage.getBoundingClientRect().top : null,
                            lastMessageY: lastMessage ? lastMessage.getBoundingClientRect().top : null
                        };
                    ");

                    return result;
                });

                // 转换结果
                if (jsResult != null)
                {
                    scrollStatus = jsResult as ScrollStatus;
                    _logger.Debug($"滚动状态: {JsonSerializer.Serialize(scrollStatus)}");
                }
                else
                {
                    _logger.Debug("获取滚动状态失败，返回null");
                }

                return scrollStatus;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "获取滚动状态失败");
                return null;
            }
        }

        private async Task<double> GetScrollPositionAsync()
        {
            return await Task.Run(() => 
                (double)((IJavaScriptExecutor)_driver).ExecuteScript(
                    "return document.querySelector('.bubbles').scrollTop;"
                )
            );
        }
    }

    // 修改 ScrollStatus 类为可空类型
    public class ScrollStatus
    {
        public double? ContainerHeight { get; set; }
        public double? ScrollHeight { get; set; }
        public double? ScrollTop { get; set; }
        public int? MessageCount { get; set; }
        public double? FirstMessageY { get; set; }
        public double? LastMessageY { get; set; }
        public double? ScrollPosition => ScrollTop;
        public bool? IsAtBottom => ScrollTop.HasValue && ScrollHeight.HasValue && 
                                  ContainerHeight.HasValue && 
                                  (ScrollTop.Value + ContainerHeight.Value >= ScrollHeight.Value - 50);
    }
} 