namespace TelegramAutomation.Constants
{
    public static class ErrorCodes
    {
        // 系统级错误
        public const string UNKNOWN_ERROR = "ERR000";
        public const string CONFIG_ERROR = "ERR001";
        public const string NETWORK_ERROR = "ERR003";
        public const string INITIALIZATION_ERROR = "ERR004";

        // Chrome 相关错误
        public const string CHROME_NOT_FOUND = "CHR001";
        public const string CHROME_VERSION_ERROR = "CHR002";
        public const string CHROME_VERSION_MISMATCH = "CHR003";
        public const string CHROME_DRIVER_ERROR = "CHR004";
        public const string CHROME_LAUNCH_ERROR = "CHR005";
        public const string CHROME_ENVIRONMENT_ERROR = "CHR006";

        // 登录相关错误
        public const string LOGIN_FAILED = "LOG001";
        public const string LOGIN_TIMEOUT = "LOG002";
        public const string LOGIN_INVALID_CODE = "LOGIN003";
        public const string LOGIN_SESSION_EXPIRED = "LOGIN004";

        // 下载相关错误
        public const string DOWNLOAD_FAILED = "DOWN001";
        public const string DOWNLOAD_TIMEOUT = "DOWN002";
        public const string DOWNLOAD_PATH_ERROR = "DOWN003";
        public const string DOWNLOAD_NETWORK_ERROR = "DOWN004";

        // 资源相关错误
        public const string RESOURCE_NOT_FOUND = "RES001";
        public const string RESOURCE_ACCESS_DENIED = "RES002";
        public const string RESOURCE_BUSY = "RES003";
    }
} 