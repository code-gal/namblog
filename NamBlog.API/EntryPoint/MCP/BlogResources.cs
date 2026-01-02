using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace NamBlog.API.EntryPoint.MCP
{
    /// <summary>
    /// MCP 资源集合 - 静态资源与元数据
    /// 通过 URI 提供对静态资源和元数据的只读访问
    /// Resources 用于向 AI 提供上下文信息，而不是执行操作
    ///
    /// 设计原则：
    /// 1. 不调用业务服务（ApplicationService）- 符合 DDD 架构
    /// 2. 不依赖基础设施层 - 只读取配置文件
    /// 3. 只提供简单的文件访问和元数据
    /// 4. 文本文件直接返回内容，方便 AI 读取
    ///
    /// 注意：MCP Resources 使用 UriTemplate 语法（RFC 6570）
    /// - 简单参数: {id} - 匹配单个路径段
    /// - 通配符参数: {*path} 在 C# SDK 中可能不支持
    /// </summary>
    [McpServerResourceType]
    public class BlogResources(
        IWebHostEnvironment environment,
        IConfiguration configuration,
        ILogger<BlogResources> logger)
    {
        private readonly IWebHostEnvironment _environment = environment;
        private readonly IConfiguration _configuration = configuration;
        private readonly ILogger<BlogResources> _logger = logger;

        /// <summary>
        /// 共享的 JSON 序列化选项（避免中文被 Unicode 转义）
        /// </summary>
        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        /// <summary>
        /// 列出 MCP 提示词元数据
        /// 资源 URI 格式：mcp://prompts
        /// 固定 URI - 无参数
        /// </summary>
        [McpServerResource(UriTemplate = "mcp://prompts", Name = "MCP 提示词列表")]
        [Description("列出所有 MCP 提示词的元数据，包括名称、描述、参数等信息。用于让 AI 了解有哪些可用的提示词模板。")]
        public ResourceContents ListMcpPrompts(RequestContext<ReadResourceRequestParams> requestContext)
        {
            // _logger.LogDebug("MCP Resource: 列出 MCP 提示词");

            var dataRootPath = _configuration["Storage:DataRootPath"] ?? "./data";
            var configPath = Path.Combine(_environment.ContentRootPath, dataRootPath, "config", "mcp-prompts.json");

            if (!File.Exists(configPath))
            {
                return CreateTextResource(requestContext.Params?.Uri ?? "", JsonSerializer.Serialize(new
                {
                    prompts = Array.Empty<object>(),
                    message = "MCP 提示词配置文件不存在"
                }, _jsonOptions));
            }

            try
            {
                var json = File.ReadAllText(configPath);
                var config = JsonSerializer.Deserialize<McpPromptsConfig>(json, _jsonOptions);

                var promptList = config?.Prompts.Select(p => new
                {
                    id = p.Key,
                    name = p.Value.Name,
                    description = p.Value.Description,
                    parameters = p.Value.Parameters.Select(param => new
                    {
                        name = param.Name,
                        description = param.Description,
                        required = param.Required
                    })
                }).ToList();

                return CreateTextResource(
                    requestContext.Params?.Uri ?? "",
                    JsonSerializer.Serialize(new
                    {
                        count = promptList?.Count ?? 0,
                        prompts = promptList
                    }, _jsonOptions)
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MCP Resource: 读取 MCP 提示词配置失败");
                return CreateTextResource(requestContext.Params?.Uri ?? "", $"错误：{ex.Message}");
            }
        }

        /// <summary>
        /// 列出静态资源目录结构
        /// 资源 URI 格式：resources://list/{subdir}
        /// 模板资源 - 使用路径参数而非查询字符串
        /// </summary>
        [McpServerResource(UriTemplate = "resources://list/{subdir}", Name = "列出资源目录")]
        [Description("列出 /resources 目录下的文件和子目录。参数 subdir 指定子目录名称（如 'icon', 'css'）。返回 JSON 格式的文件列表。")]
        public ResourceContents ListResources(
            RequestContext<ReadResourceRequestParams> requestContext,
            string subdir)
        {
            // _logger.LogDebug("MCP Resource: 列出静态资源 - SubDir:{SubDir}", subdir);

            var dataRootPath = _configuration["Storage:DataRootPath"] ?? "./data";
            var baseResourcesPath = Path.Combine(_environment.ContentRootPath, dataRootPath, "resources");
            var targetPath = Path.Combine(baseResourcesPath, subdir);

            // 安全检查：防止路径遍历
            var normalizedPath = Path.GetFullPath(targetPath);
            var normalizedBase = Path.GetFullPath(baseResourcesPath);

            if (!normalizedPath.StartsWith(normalizedBase))
            {
                return CreateTextResource(requestContext.Params?.Uri ?? "", "错误：非法路径");
            }

            if (!Directory.Exists(normalizedPath))
            {
                return CreateTextResource(requestContext.Params?.Uri ?? "", JsonSerializer.Serialize(new
                {
                    directories = Array.Empty<string>(),
                    files = Array.Empty<object>(),
                    message = $"目录 '{subdir}' 不存在"
                }, _jsonOptions));
            }

            // 获取子目录
            var directories = Directory.GetDirectories(normalizedPath)
                .Select(d => Path.GetFileName(d))
                .ToList();

            // 获取文件
            var files = Directory.GetFiles(normalizedPath)
                .Select(f =>
                {
                    var fileName = Path.GetFileName(f);
                    return new
                    {
                        name = fileName,
                        path = $"{subdir}/{fileName}",
                        size = new FileInfo(f).Length,
                        extension = Path.GetExtension(f)
                    };
                })
                .ToList();

            var result = new
            {
                currentDirectory = subdir,
                directories,
                files,
                note = "使用 'resources://file/{filename}' 获取同目录下的文件内容"
            };

            return CreateTextResource(
                requestContext.Params?.Uri ?? "",
                JsonSerializer.Serialize(result, _jsonOptions)
            );
        }

        /// <summary>
        /// 列出资源根目录
        /// 固定 URI - 无参数
        /// </summary>
        [McpServerResource(UriTemplate = "resources://root", Name = "列出根目录")]
        [Description("列出 /resources 根目录下的所有子目录和文件。")]
        public ResourceContents ListRootResources(RequestContext<ReadResourceRequestParams> requestContext)
        {
            // _logger.LogDebug("MCP Resource: 列出资源根目录");

            var dataRootPath = _configuration["Storage:DataRootPath"] ?? "./data";
            var baseResourcesPath = Path.Combine(_environment.ContentRootPath, dataRootPath, "resources");

            if (!Directory.Exists(baseResourcesPath))
            {
                return CreateTextResource(requestContext.Params?.Uri ?? "", JsonSerializer.Serialize(new
                {
                    directories = Array.Empty<string>(),
                    files = Array.Empty<object>(),
                    message = "resources 目录不存在"
                }, _jsonOptions));
            }

            var directories = Directory.GetDirectories(baseResourcesPath)
                .Select(d => Path.GetFileName(d))
                .ToList();

            var files = Directory.GetFiles(baseResourcesPath)
                .Select(f => new
                {
                    name = Path.GetFileName(f),
                    size = new FileInfo(f).Length,
                    extension = Path.GetExtension(f)
                })
                .ToList();

            var result = new
            {
                currentDirectory = "/",
                directories,
                files,
                note = "使用 'resources://list/{subdir}' 查看子目录内容"
            };

            return CreateTextResource(
                requestContext.Params?.Uri ?? "",
                JsonSerializer.Serialize(result, _jsonOptions)
            );
        }

        /// <summary>
        /// 获取推荐的资源列表
        /// 资源 URI 格式：resources://recommended
        /// 固定 URI - 无参数
        /// </summary>
        [McpServerResource(UriTemplate = "resources://recommended", Name = "推荐的 CDN 资源")]
        [Description("获取后端推荐的 CDN 资源列表（用于 Markdown 转 HTML）。包括推荐的 CDN 域名、资源 URL、可信域名白名单等信息。")]
        public ResourceContents GetRecommendedResources(RequestContext<ReadResourceRequestParams> requestContext)
        {
            // _logger.LogDebug("MCP Resource: 获取推荐资源列表");

            var dataRootPath = _configuration["Storage:DataRootPath"] ?? "./data";
            var promptsPath = Path.Combine(_environment.ContentRootPath, dataRootPath, "config", "prompts.json");

            if (!File.Exists(promptsPath))
            {
                return CreateTextResource(requestContext.Params?.Uri ?? "", JsonSerializer.Serialize(new
                {
                    error = "配置文件不存在",
                    message = "prompts.json 文件未找到"
                }, _jsonOptions));
            }

            try
            {
                var json = File.ReadAllText(promptsPath);
                using var doc = JsonDocument.Parse(json);

                // 直接解析 JSON，不依赖 PromptsConfig
                var root = doc.RootElement;
                var promptsSection = root.GetProperty("Prompts");
                var markdownToHtml = promptsSection.GetProperty("MarkdownToHtml");

                // 提取推荐资源
                var resources = markdownToHtml.GetProperty("Resources")
                    .EnumerateArray()
                    .Select(r => new
                    {
                        domain = r.TryGetProperty("Domain", out var d) ? d.GetString() : null,
                        url = r.TryGetProperty("Url", out var u) ? u.GetString() : null,
                        description = r.TryGetProperty("Description", out var desc) ? desc.GetString() : null
                    })
                    .ToList();

                // 提取验证配置
                var validation = markdownToHtml.GetProperty("Validation");
                var validationMode = validation.GetProperty("Mode").GetString();
                var checkExternalScripts = validation.GetProperty("CheckExternalScripts").GetBoolean();
                var trustedDomains = validation.GetProperty("TrustedDomains")
                    .EnumerateArray()
                    .Select(d => d.GetString())
                    .ToList();

                var result = new
                {
                    validationMode,
                    checkExternalScripts,
                    trustedDomains,
                    recommendedResources = resources,
                    note = "这些资源在生成 HTML 时可以安全使用"
                };

                return CreateTextResource(
                    requestContext.Params?.Uri ?? "",
                    JsonSerializer.Serialize(result, _jsonOptions)
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MCP Resource: 读取推荐资源列表失败");
                return CreateTextResource(requestContext.Params?.Uri ?? "", $"错误：{ex.Message}");
            }
        }

        /// <summary>
        /// 获取文本文件内容
        /// 资源 URI 格式：resources://file/{filename}
        /// 简化版 - 仅支持根目录文件，不支持通配符路径
        /// </summary>
        [McpServerResource(UriTemplate = "resources://file/{filename}", Name = "获取文件内容")]
        [Description("获取 resources 根目录下的文本文件内容（仅支持 txt、md、json、yaml、cs 等）。参数 filename 是文件名。")]
        public ResourceContents GetTextFile(
            RequestContext<ReadResourceRequestParams> requestContext,
            string filename)
        {
            // _logger.LogDebug("MCP Resource: 获取文本文件 - Filename:{Filename}", filename);

            var dataRootPath = _configuration["Storage:DataRootPath"] ?? "./data";
            var baseResourcesPath = Path.Combine(_environment.ContentRootPath, dataRootPath, "resources");
            var filePath = Path.Combine(baseResourcesPath, filename);

            // 安全检查：防止路径遍历（filename 不应包含目录分隔符）
            if (filename.Contains('/') || filename.Contains('\\') || filename.Contains(".."))
            {
                return CreateTextResource(requestContext.Params?.Uri ?? "", "错误：文件名不能包含路径分隔符");
            }

            var normalizedPath = Path.GetFullPath(filePath);
            var normalizedBase = Path.GetFullPath(baseResourcesPath);

            if (!normalizedPath.StartsWith(normalizedBase))
            {
                return CreateTextResource(requestContext.Params?.Uri ?? "", "错误：非法路径访问");
            }

            if (!File.Exists(normalizedPath))
            {
                return CreateTextResource(requestContext.Params?.Uri ?? "", $"错误：文件 '{filename}' 不存在");
            }

            // 检查文件类型（仅支持文本类型）
            var extension = Path.GetExtension(filename).ToLower();
            var textExtensions = new[] { ".txt", ".md", ".json", ".yaml", ".yml", ".cs", ".css", ".js", ".html", ".xml", ".svg" };

            if (!textExtensions.Contains(extension))
            {
                return CreateTextResource(
                    requestContext.Params?.Uri ?? "",
                    $"错误：不支持的文件类型 '{extension}'。仅支持文本文件：{string.Join(", ", textExtensions)}"
                );
            }

            try
            {
                var content = File.ReadAllText(normalizedPath);
                return CreateTextResource(requestContext.Params?.Uri ?? "", content);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MCP Resource: 读取文件失败 - Filename:{Filename}", filename);
                return CreateTextResource(requestContext.Params?.Uri ?? "", $"错误：无法读取文件 '{filename}'");
            }
        }

        #region 辅助方法

        /// <summary>
        /// 创建文本资源内容
        /// </summary>
        private static TextResourceContents CreateTextResource(string uri, string text, string mimeType = "application/json")
        {
            return new TextResourceContents
            {
                Uri = uri,
                MimeType = mimeType,
                Text = text
            };
        }

        #endregion
    }
}
