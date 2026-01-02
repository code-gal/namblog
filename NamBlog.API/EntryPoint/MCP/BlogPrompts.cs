using System.ComponentModel;
using System.IO;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

namespace NamBlog.API.EntryPoint.MCP
{
    /// <summary>
    /// MCP 提示词模板集合 - 博客管理
    /// 提供可重用的提示词模板，指导 AI 如何使用博客管理工具
    /// 从 mcp-prompts.json 配置文件中加载
    ///
    /// 设计原则：
    /// 1. 不依赖基础设施层 - 符合 DDD 架构
    /// 2. 提供静态指导模板，不包含业务逻辑
    /// 3. 参数化模板，支持动态替换
    /// </summary>
    [McpServerPromptType]
    public class BlogPrompts
    {
        private readonly ILogger<BlogPrompts> _logger;
        private readonly McpPromptsConfig _config;

        public BlogPrompts(
            IWebHostEnvironment environment,
            IConfiguration configuration,
            ILogger<BlogPrompts> logger)
        {
            _logger = logger;

            // 加载 mcp-prompts.json 配置
            var dataRootPath = configuration["Storage:DataRootPath"] ?? "./data";
            var configPath = Path.Combine(environment.ContentRootPath, dataRootPath, "config", "mcp-prompts.json");

            if (File.Exists(configPath))
            {
                try
                {
                    var json = File.ReadAllText(configPath);
                    _config = JsonSerializer.Deserialize<McpPromptsConfig>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    }) ?? new McpPromptsConfig();

                    _logger.LogInformation("MCP Prompts: 成功加载 {Count} 个提示词模板", _config.Prompts.Count);
                }
                catch (System.Exception ex)
                {
                    _logger.LogError(ex, "MCP Prompts: 加载配置文件失败");
                    _config = new McpPromptsConfig();
                }
            }
            else
            {
                _logger.LogWarning("MCP Prompts: 配置文件不存在 - {Path}", configPath);
                _config = new McpPromptsConfig();
            }
        }

        /// <summary>
        /// 创建博客文章的指导提示词
        /// </summary>
        [McpServerPrompt(Name = "create_article_prompt")]
        [Description("指导 AI 如何组合工具创建一篇完整的博客文章。包括选择分类、标签、创建内容、生成 HTML 等步骤。可选参数：topic（文章主题）。")]
        public ChatMessage CreateArticle(
            [Description("文章主题或关键词（可选）")] string? topic = null)
        {
            _logger.LogInformation("MCP Prompt: create_article_prompt - Topic:{Topic}", topic ?? "未指定");

            if (!_config.Prompts.TryGetValue("create_article", out var template))
            {
                return new ChatMessage(ChatRole.User, "错误：提示词模板未找到");
            }

            // 替换模板变量
            var content = template.Template.Replace("{{topic}}", topic ?? "指定主题");

            return new ChatMessage(ChatRole.User, content);
        }

        /// <summary>
        /// 优化文章质量的指导提示词
        /// </summary>
        [McpServerPrompt(Name = "optimize_article_prompt")]
        [Description("指导 AI 如何检查和优化现有文章，提高文章质量和可读性。包括标题、内容结构、元数据等多方面检查清单。使用时需要告诉 AI 具体要优化哪篇文章。")]
        public ChatMessage OptimizeArticle()
        {
            // _logger.LogDebug("MCP Prompt: optimize_article_prompt");

            if (!_config.Prompts.TryGetValue("optimize_article", out var template))
            {
                return new ChatMessage(ChatRole.User, "错误：提示词模板未找到");
            }

            // 不替换变量，返回通用指导模板
            // AI 会根据用户的具体请求（如"优化文章 my-article"）自行填充
            return new ChatMessage(ChatRole.User, template.Template);
        }

        /// <summary>
        /// 文章元数据规范的指导提示词
        /// </summary>
        [McpServerPrompt(Name = "metadata_guidelines_prompt")]
        [Description("提供文章元数据的格式要求和长度限制，包括标题、slug、分类、标签、摘要等字段的验证规则。确保提交的数据符合后端验证要求。")]
        public ChatMessage MetadataGuidelines()
        {
            // _logger.LogDebug("MCP Prompt: metadata_guidelines_prompt");

            if (!_config.Prompts.TryGetValue("metadata_guidelines", out var template))
            {
                return new ChatMessage(ChatRole.User, "错误：提示词模板未找到");
            }

            return new ChatMessage(ChatRole.System, template.Template);
        }

        /// <summary>
        /// HTML 提交规范的指导提示词
        /// </summary>
        [McpServerPrompt(Name = "html_guidelines_prompt")]
        [Description("提供手动提交 HTML 版本时的安全和样式规范，包括文档结构要求、可信 CDN 域名、验证模式等。帮助 AI 生成符合规范的 HTML 内容。")]
        public ChatMessage HtmlGuidelines()
        {
            // _logger.LogDebug("MCP Prompt: html_guidelines_prompt");

            if (!_config.Prompts.TryGetValue("html_guidelines", out var template))
            {
                return new ChatMessage(ChatRole.User, "错误：提示词模板未找到");
            }

            return new ChatMessage(ChatRole.System, template.Template);
        }
    }
}
