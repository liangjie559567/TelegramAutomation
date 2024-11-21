using NLog;
using System.Runtime.CompilerServices;
using System;

namespace TelegramAutomation.Helpers
{
    public static class LogHelper
    {
        public static void LogMethodEntry(this ILogger logger, 
            [CallerMemberName] string methodName = "",
            [CallerFilePath] string sourceFilePath = "",
            [CallerLineNumber] int sourceLineNumber = 0)
        {
            logger.Debug($"进入方法: {methodName} at {sourceFilePath}:{sourceLineNumber}");
        }

        public static void LogMethodExit(this ILogger logger, 
            [CallerMemberName] string methodName = "")
        {
            logger.Debug($"退出方法: {methodName}");
        }

        public static void LogException(this ILogger logger, Exception ex, string message,
            [CallerMemberName] string methodName = "",
            [CallerFilePath] string sourceFilePath = "",
            [CallerLineNumber] int sourceLineNumber = 0)
        {
            var errorCode = ex is TelegramAutomationException tae ? tae.ErrorCode : "UNKNOWN";
            
            logger.Error(ex, $"错误代码: {errorCode}, 位置: {methodName} at {sourceFilePath}:{sourceLineNumber}, 消息: {message}");
        }

        public static void LogOperationResult(this ILogger logger, bool success, string operation,
            [CallerMemberName] string methodName = "")
        {
            if (success)
                logger.Info($"操作成功: {operation} in {methodName}");
            else
                logger.Warn($"操作失败: {operation} in {methodName}");
        }
    }
} 