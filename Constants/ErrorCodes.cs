namespace TelegramAutomation.Constants
{
    public static class ErrorCodes
    {
        // Chrome 相关错误
        public const string CHROME_NOT_FOUND = "CHROME_NOT_FOUND";
        public const string CHROME_VERSION_MISMATCH = "CHROME_VERSION_MISMATCH";
        public const string CHROME_DRIVER_ERROR = "CHROME_DRIVER_ERROR";

        // 登录相关错误
        public const string PHONE_NUMBER_EMPTY = "PHONE_NUMBER_EMPTY";
        public const string PHONE_NUMBER_INVALID = "PHONE_NUMBER_INVALID";
        public const string VERIFICATION_CODE_EMPTY = "VERIFICATION_CODE_EMPTY";
        public const string VERIFICATION_CODE_INVALID = "VERIFICATION_CODE_INVALID";
        public const string LOGIN_FAILED = "LOGIN_FAILED";
        public const string SESSION_EXPIRED = "SESSION_EXPIRED";

        // 网络相关错误
        public const string NETWORK_ERROR = "NETWORK_ERROR";
        public const string TIMEOUT_ERROR = "TIMEOUT_ERROR";
        public const string CONNECTION_ERROR = "CONNECTION_ERROR";

        // 其他错误
        public const string UNKNOWN_ERROR = "UNKNOWN_ERROR";
        public const string INITIALIZATION_ERROR = "INITIALIZATION_ERROR";
        public const string RESOURCE_ERROR = "RESOURCE_ERROR";
    }
} 