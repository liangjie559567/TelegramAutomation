public class SessionData
{
    public string PhoneNumber { get; set; } = string.Empty;
    public DateTime LastLoginTime { get; set; }
    public bool SessionValid { get; set; }
    public string? LastError { get; set; }
    public int LoginAttempts { get; set; }
    public string? SessionToken { get; set; }
    public Dictionary<string, string> Cookies { get; set; } = new();
} 