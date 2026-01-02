using System.Collections.Generic;

namespace NamBlog.API.Infrastructure.Agents
{
    /// <summary>
    /// HTML 验证模式
    /// </summary>
    public enum HtmlValidationMode
    {
        /// <summary>
        /// 严格模式：阻止非可信域名的脚本，验证失败时返回错误
        /// </summary>
        Strict = 0,

        /// <summary>
        /// 警告模式：记录警告但允许生成（推荐）
        /// </summary>
        Warning = 1,

        /// <summary>
        /// 宽松模式：不检查外部脚本（不推荐生产环境使用）
        /// </summary>
        Permissive = 2
    }

    /// <summary>
    /// AI 提示词配置（映射 prompts.json）
    /// </summary>
    public class PromptsConfig
    {
        public MarkdownToHtmlConfig MarkdownToHtml { get; set; } = new();
        public MetadataGenerationConfig MetadataGeneration { get; set; } = new();
    }

    /// <summary>
    /// Markdown 转 HTML 配置
    /// </summary>
    public class MarkdownToHtmlConfig
    {
        /// <summary>
        /// 根系统提示词（最高优先级，不可覆盖）
        /// </summary>
        public string RootSystemPrompt { get; set; } = string.Empty;

        /// <summary>
        /// 用户全局提示词（可选，仅在没有 customPrompt 时生效）
        /// </summary>
        public string UserGlobalPrompt { get; set; } = string.Empty;

        /// <summary>
        /// 推荐资源列表
        /// </summary>
        public List<ResourceConfig> Resources { get; set; } = [];

        /// <summary>
        /// HTML 验证配置
        /// </summary>
        public HtmlValidationConfig Validation { get; set; } = new();
    }

    /// <summary>
    /// 资源配置
    /// </summary>
    public class ResourceConfig
    {
        /// <summary>
        /// 域名（如：cdn.jsdelivr.net）
        /// </summary>
        public string? Domain { get; set; }

        /// <summary>
        /// 完整 URL（可选，优先级高于 Domain）
        /// </summary>
        public string? Url { get; set; }

        /// <summary>
        /// 资源描述
        /// </summary>
        public string Description { get; set; } = string.Empty;
    }

    /// <summary>
    /// HTML 验证配置
    /// </summary>
    public class HtmlValidationConfig
    {
        /// <summary>
        /// 验证模式
        /// </summary>
        public HtmlValidationMode Mode { get; set; } = HtmlValidationMode.Warning;

        /// <summary>
        /// 是否检查外部脚本
        /// </summary>
        public bool CheckExternalScripts { get; set; } = true;

        /// <summary>
        /// 可信域名列表
        /// </summary>
        public List<string> TrustedDomains { get; set; } = [];
    }

    /// <summary>
    /// 元数据生成配置
    /// </summary>
    public class MetadataGenerationConfig
    {
        public string TitlePrompt { get; set; } = string.Empty;
        public string SlugPrompt { get; set; } = string.Empty;
        public string TagsPrompt { get; set; } = string.Empty;
        public string ExcerptPrompt { get; set; } = string.Empty;
    }
}
