namespace NamBlog.API.Domain.ValueObjects
{
    /// <summary>
    /// HTML 生成状态枚举
    /// </summary>
    public enum HtmlGenerationStatus
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
    /// HTML 流式渲染进度
    /// </summary>
    public record HtmlRenderProgress
    {
        /// <summary>
        /// 生成状态
        /// </summary>
        public HtmlGenerationStatus Status { get; init; }

        /// <summary>
        /// HTML 片段（流式推送的内容块）
        /// </summary>
        public string? Chunk { get; init; }

        /// <summary>
        /// 生成进度（0-100）
        /// </summary>
        public int Progress { get; init; }

        /// <summary>
        /// 错误信息（仅在 Status = Failed 时有值）
        /// </summary>
        public string? Error { get; init; }
    }
}
