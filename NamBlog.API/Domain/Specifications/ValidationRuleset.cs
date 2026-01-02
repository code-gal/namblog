namespace NamBlog.API.Domain.Specifications
{
    /// <summary>
    /// 验证规则集 - 所有验证规则的单一来源
    /// DDD 原则：验证规则属于领域知识，集中管理避免不一致
    /// </summary>
    public static class ValidationRuleset
    {
        #region ==================== Post ====================

        public static class Post
        {
            /// <summary>
            /// 标题验证规则：1-100 字符，必填
            /// </summary>
            public static readonly ValidationRule Title = new(
                errorMessage: "Title length must be between 1 and 100 characters.",
                minLength: 1,
                maxLength: 100
            );

            /// <summary>
            /// Slug 验证规则：1-50 字符，仅小写字母、数字、连字符
            /// </summary>
            public static readonly ValidationRule Slug = new(
                errorMessage: "Slug can only contain lowercase letters, numbers, and hyphens.",
                minLength: 1,
                maxLength: 50,
                regexPattern: @"^[a-z0-9-]+$"
            );

            /// <summary>
            /// 分类验证规则：1-15 字符
            /// </summary>
            public static readonly ValidationRule Category = new(
                errorMessage: "Category length must be between 1 and 15 characters.",
                minLength: 1,
                maxLength: 15
            );

            /// <summary>
            /// 摘要验证规则：0-500 字符，可选
            /// </summary>
            public static readonly ValidationRule Excerpt = new(
                errorMessage: "Abstract length must not exceed 500 characters.",
                minLength: 0,
                maxLength: 500
            );

            /// <summary>
            /// Markdown 验证规则：1-500000 字符，必填
            /// </summary>
            public static readonly ValidationRule Markdown = new(
                errorMessage: "Markdown length must be between 1 and 500000 characters.",
                minLength: 1,
                maxLength: 500_000
            );

            /// <summary>
            /// 文件名验证规则：1-255 字符
            /// </summary>
            public static readonly ValidationRule FileName = new(
                errorMessage: "File name length must be between 1 and 255 characters.",
                minLength: 1,
                maxLength: 255
            );

            /// <summary>
            /// 文件路径验证规则：0-500 字符，可选
            /// </summary>
            public static readonly ValidationRule FilePath = new(
                errorMessage: "File path length must not exceed 500 characters.",
                minLength: 0,
                maxLength: 500
            );

            /// <summary>
            /// 作者名验证规则：0-20 字符，可选
            /// </summary>
            public static readonly ValidationRule Author = new(
                errorMessage: "Author name length cannot exceed 20 characters.",
                minLength: 0,
                maxLength: 20
            );

            /// <summary>
            /// 标签数组验证规则：0-10 个标签，每个标签遵循 Tag 规则
            /// </summary>
            public static readonly ArrayValidationRule Tags = new(
                errorMessage: "The number of tags cannot exceed 10.",
                minCount: 0,
                maxCount: 10,
                elementRule: Tag
            );
        }

        #endregion

        #region ==================== PostTag ====================

        /// <summary>
        /// 标签名称验证规则：1-20 字符，必填
        /// </summary>
        public static readonly ValidationRule Tag = new(
            errorMessage: "Tag length must be between 1 and 20 characters.",
            minLength: 1,
            maxLength: 20
        );

        #endregion
    }
}
