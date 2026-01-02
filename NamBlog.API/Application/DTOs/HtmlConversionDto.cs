namespace NamBlog.API.Application.DTOs
{
    /// <summary>
    /// HTML 转换状态枚举（Application 层 DTO）
    /// </summary>
    public enum HtmlConversionStatus
    {
        /// <summary>
        /// 生成中
        /// </summary>
        Generating,

        /// <summary>
        /// 已完成
        /// </summary>
        Completed,

        /// <summary>
        /// 失败
        /// </summary>
        Failed
    }

    /// <summary>
    /// HTML 转换进度（Application 层 DTO）
    /// </summary>
    public record HtmlConversionProgress
    {
        /// <summary>
        /// 转换状态
        /// </summary>
        public HtmlConversionStatus Status { get; init; }

        /// <summary>
        /// HTML 片段（流式推送的内容块）
        /// </summary>
        public string? Chunk { get; init; }

        /// <summary>
        /// 转换进度（0-100）
        /// </summary>
        public int Progress { get; init; }

        /// <summary>
        /// 错误信息（仅在 Status = Failed 时有值）
        /// </summary>
        public string? Error { get; init; }
    }

    /// <summary>
    /// HTML 转换最终结果（用于 MCP 工具返回）
    /// </summary>
    public record HtmlConversionResult
    {
        /// <summary>
        /// 转换状态
        /// </summary>
        public HtmlConversionStatus Status { get; init; }

        /// <summary>
        /// 完整的 HTML 内容
        /// </summary>
        public string Html { get; init; } = string.Empty;

        /// <summary>
        /// 错误信息（仅在 Status = Failed 时有值）
        /// </summary>
        public string? Error { get; init; }
    }
}
