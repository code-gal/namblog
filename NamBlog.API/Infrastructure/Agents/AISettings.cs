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

        /// <summary>
        /// AI 生成超时时间（秒），默认 600 秒（10 分钟）
        /// 生成长文章需要更多时间，可根据实际情况调整
        /// </summary>
        public int TimeoutSeconds { get; set; } = 600;
    }
}
