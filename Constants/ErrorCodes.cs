namespace TelegramAutomation.Constants
{
    public static class ErrorCodes
    {
        // Chrome 相关错误
        public const string CHROME_NOT_FOUND = "CHROME_NOT_FOUND";
        public const string CHROME_VERSION_MISMATCH = "CHROME_VERSION_MISMATCH";
        public const string CHROME_DRIVER_ERROR = "CHROME_DRIVER_ERROR";
        public const string INITIALIZATION_ERROR = "INITIALIZATION_ERROR";

        // 登录相关错误
        public const string LOGIN_FAILED = "LOGIN_FAILED";
        public const string LOGIN_TIMEOUT = "LOGIN_TIMEOUT";
        public const string LOGIN_INVALID = "LOGIN_INVALID";

        // 网络相关错误
        public const string NETWORK_ERROR = "NETWORK_ERROR";
        public const string TIMEOUT_ERROR = "TIMEOUT_ERROR";
        public const string CONNECTION_ERROR = "CONNECTION_ERROR";

        // 其他错误
        public const string UNKNOWN_ERROR = "UNKNOWN_ERROR";

        // 频道相关错误
        public const string CHANNEL_NOT_FOUND = "CHANNEL_NOT_FOUND";
        public const string CHANNEL_SWITCH_FAILED = "CHANNEL_SWITCH_FAILED";
    }
} 