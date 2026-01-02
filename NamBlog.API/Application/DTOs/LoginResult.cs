namespace NamBlog.API.Application.DTOs
{
    /// <summary>
    /// 登录结果
    /// </summary>
    public record LoginResult
    {
        /// <summary>
        /// 是否成功
        /// </summary>
        public required bool Success { get; init; }

        /// <summary>
        /// JWT Token（成功时返回）
        /// </summary>
        public string? Token { get; init; }

        /// <summary>
        /// 错误消息（失败时返回）
        /// </summary>
        public string? Message { get; init; }

        /// <summary>
        /// 错误代码
        /// </summary>
        public string? ErrorCode { get; init; }
    }
}
