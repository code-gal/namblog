using System;

namespace NamBlog.API.Domain.Entities
{
    /// <summary>
    /// HTML 验证状态枚举
    /// </summary>
    public enum HtmlValidationStatus
    {
        /// <summary>
        /// 未验证
        /// </summary>
        NotValidated = 0,

        /// <summary>
        /// 验证通过
        /// </summary>
        Valid = 1,

        /// <summary>
        /// 验证失败
        /// </summary>
        Invalid = 2
    }

    /// <summary>
    /// 文章版本实体
    /// 设计：每次AI生成HTML时创建一个版本记录
    /// 职责：管理单个版本的元数据、验证状态、AI提示词等信息
    /// </summary>
    public class PostVersion
    {
        #region ===================== 属性 =====================

        public int PostVersionId { get; private set; }

        /// <summary>
        /// 所属文章ID（外键）
        /// </summary>
        public int PostId { get; private set; }

        /// <summary>
        /// 版本名称（时间格式："2025-12-24 15:01:24"）
        /// 用途：1. 唯一标识版本  2. 拼接HTML文件路径
        /// </summary>
        public string VersionName { get; private set; }

        /// <summary>
        /// AI 提示词（生成此版本时使用的提示词）
        /// </summary>
        public string? AiPrompt { get; private set; }

        /// <summary>
        /// HTML 验证状态
        /// </summary>
        public HtmlValidationStatus ValidationStatus { get; private set; }

        /// <summary>
        /// HTML 验证错误信息（仅当 ValidationStatus = Invalid 时有值）
        /// </summary>
        public string? HtmlValidationError { get; private set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTimeOffset CreatedAt { get; private set; }

        /// <summary>
        /// 导航属性：所属文章
        /// </summary>
        public Post Post { get; private set; } = null!;

        #endregion

        #region ===================== 构造方法 =====================

        /// <summary>
        /// EF Core 需要的无参构造函数（private）
        /// </summary>
        private PostVersion()
        {
            VersionName = string.Empty;
            CreatedAt = DateTimeOffset.UtcNow;
        }

        /// <summary>
        /// 私有构造函数，供静态工厂方法使用
        /// </summary>
        private PostVersion(int postId, string versionName, string? aiPrompt)
        {
            PostId = postId;
            VersionName = versionName;
            AiPrompt = aiPrompt;
            ValidationStatus = HtmlValidationStatus.NotValidated;
            CreatedAt = DateTimeOffset.UtcNow;
        }

        #endregion

        #region ===================== 工厂方法 =====================

        /// <summary>
        /// 创建新版本
        /// </summary>
        public static PostVersion Create(int postId, string? aiPrompt = null)
        {
            // 生成当前时间作为版本名称（使用连字符代替冒号，避免 Windows 路径非法字符）
            var versionName = DateTimeOffset.Now.ToString("yyyy-MM-dd HH-mm-ss");
            return new PostVersion(postId, versionName, aiPrompt);
        }

        #endregion

        #region ===================== 业务方法 =====================

        /// <summary>
        /// 标记验证通过
        /// </summary>
        public void MarkAsValid()
        {
            ValidationStatus = HtmlValidationStatus.Valid;
            HtmlValidationError = null;
        }

        /// <summary>
        /// 标记验证失败
        /// </summary>
        public void MarkAsInvalid(string errorMessage)
        {
            if (string.IsNullOrWhiteSpace(errorMessage))
                throw new ArgumentException("Error message cannot be empty.", nameof(errorMessage));

            ValidationStatus = HtmlValidationStatus.Invalid;
            HtmlValidationError = errorMessage;
        }

        /// <summary>
        /// 重置验证状态
        /// </summary>
        public void ResetValidation()
        {
            ValidationStatus = HtmlValidationStatus.NotValidated;
            HtmlValidationError = null;
        }

        #endregion
    }
}
