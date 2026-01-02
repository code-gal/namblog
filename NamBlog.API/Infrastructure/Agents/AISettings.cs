namespace NamBlog.API.Infrastructure.Agents
{
    /// <summary>
    /// AI 配置
    /// </summary>
    public class AISettings
    {
        public required string ApiKey { get; set; }
        public required string BaseUrl { get; set; }
        public required string Model { get; set; }
        public int MaxTokens { get; set; } = 4096;
        public double Temperature { get; set; } = 0.7;
    }
}
