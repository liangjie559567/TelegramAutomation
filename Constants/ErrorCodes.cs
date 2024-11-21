namespace TelegramAutomation.Constants
{
    public static class ErrorCodes
    {
        // Chrome 相关错误
        public const string CHROME_NOT_FOUND = "CHROME001";
        public const string CHROME_VERSION_MISMATCH = "CHROME002";
        public const string CHROME_DRIVER_ERROR = "CHROME003";
        public const string CHROME_ENVIRONMENT_ERROR = "CHROME004";
        public const string CHROME_LAUNCH_ERROR = "CHROME005";

        // 登录相关错误
        public const string LOGIN_FAILED = "LOGIN001";
        public const string LOGIN_TIMEOUT = "LOGIN002";
        public const string LOGIN_INVALID_CODE = "LOGIN003";

        // 下载相关错误
        public const string DOWNLOAD_FAILED = "DOWN001";
        public const string DOWNLOAD_TIMEOUT = "DOWN002";
        public const string DOWNLOAD_PATH_ERROR = "DOWN003";

        // 通用错误
        public const string UNKNOWN_ERROR = "ERR001";
        public const string CONFIG_ERROR = "ERR002";
        public const string NETWORK_ERROR = "ERR003";
    }
} 