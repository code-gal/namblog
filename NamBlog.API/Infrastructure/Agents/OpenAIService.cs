using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NamBlog.API.Application.Common;
using NamBlog.API.Domain.Interfaces;
using NamBlog.API.Domain.ValueObjects;
using NamBlog.API.Infrastructure.Common;

namespace NamBlog.API.Infrastructure.Agents
{
    /// <summary>
    /// OpenAI 服务实现（基于 Microsoft.Extensions.AI）
    /// </summary>
    public partial class OpenAIService(
        IChatClient chatClient,
        IOptionsMonitor<AISettings> aiSettings,
        IOptionsMonitor<PromptsConfig> promptsConfig,
        ILogger<OpenAIService> logger) : IAIService
    {
        private AISettings AiSettings => aiSettings.CurrentValue;
        private PromptsConfig PromptsConfig => promptsConfig.CurrentValue;

        // 内置默认提示词（当 JSON 配置缺失或错误时使用）
        private const string _builtinRootPrompt = @"你是一个专业的 Markdown 到 HTML 转换助手。

**任务**：将用户提供的 Markdown 文章转换为一个独立的、美化的、可交互的 HTML 页面。

**要求**：
1. 输出必须是一个完整的 HTML 文件（包含 <!DOCTYPE html>）
2. 内联 CSS 样式，确保页面美观
3. 支持代码高亮
4. 响应式设计
5. **直接输出 HTML 代码，不要用 ```html 或任何 markdown 代码块标记包裹**
6. 不要添加任何解释性文字，只输出 HTML
7. 允许内联 JavaScript（在 <script> 标签内）用于简单交互功能";

        private const string _defaultTitlePrompt = "根据以下 Markdown 内容生成一个简洁、准确的文章标题，最多80个字符。只返回标题文本，不要添加引号或其他格式。";
        private const string _defaultSlugPrompt = "将以下标题转换为适合URL的slug（小写、连字符分隔，最多40个字符）。只返回slug文本。";
        private const string _defaultTagsPrompt = "根据以下 Markdown 内容生成 1-10 个相关标签，单个标签2-15 字符。\n\n返回 JSON 数组格式，如：[\"标签1\", \"标签2\", \"标签3\"]\n\n如果无法返回 JSON，也可以每行一个标签。";
        private const string _defaultExcerptPrompt = "根据以下 Markdown 内容生成一个和文章相同语言的简洁的摘要（50-400字符）。只返回摘要文本，不要添加引号或其他格式。";

        /// <summary>
        /// 组装最终提示词
        /// </summary>
        /// <param name="customPrompt">自定义提示词（PostVersion.AiPrompt）</param>
        /// <returns>组装后的完整提示词</returns>
        private string BuildFinalPrompt(string? customPrompt = null)
        {
            var config = PromptsConfig;
            var sb = new StringBuilder();

            // 1. 根系统提示词（最高优先级，必须存在）
            var rootPrompt = !string.IsNullOrWhiteSpace(config.MarkdownToHtml.RootSystemPrompt)
                ? config.MarkdownToHtml.RootSystemPrompt
                : _builtinRootPrompt;

            sb.AppendLine(rootPrompt);
            sb.AppendLine();

            // 2. 自定义提示词（优先级高于用户全局提示词）
            if (!string.IsNullOrWhiteSpace(customPrompt))
            {
                sb.AppendLine("**用户自定义要求**：");
                sb.AppendLine(customPrompt);
                sb.AppendLine();
            }
            // 3. 用户全局提示词（仅在没有自定义提示词时使用）
            else if (!string.IsNullOrWhiteSpace(config.MarkdownToHtml.UserGlobalPrompt))
            {
                sb.AppendLine("**用户全局偏好**：");
                sb.AppendLine(config.MarkdownToHtml.UserGlobalPrompt);
                sb.AppendLine();
            }

            // 4. 资源列表提示词
            if (config.MarkdownToHtml.Resources?.Count > 0)
            {
                sb.AppendLine("**推荐资源列表**（可选引用）：");
                foreach (var resource in config.MarkdownToHtml.Resources)
                {
                    if (!string.IsNullOrWhiteSpace(resource.Url))
                    {
                        sb.AppendLine($"- {resource.Description}: {resource.Url}");
                    }
                    else if (!string.IsNullOrWhiteSpace(resource.Domain))
                    {
                        sb.AppendLine($"- {resource.Description}（可信域名: {resource.Domain}）");
                    }
                }

                sb.AppendLine();
            }

            return sb.ToString().Trim();
        }

        public async Task<Result<string>> RenderMarkdownToHtmlAsync(string markdown, string? customPrompt = null)
        {
            const int MaxRetries = 3;
            var timeoutSeconds = AiSettings.TimeoutSeconds; // 从配置读取超时时间

            // 组装最终提示词
            var systemPrompt = BuildFinalPrompt(customPrompt);

            // 构建初始聊天消息
            var messages = new List<ChatMessage>
            {
                new(ChatRole.System, systemPrompt),
                new(ChatRole.User, markdown)
            };

            // Agent 工作流：验证失败时自动重试
            for (int attempt = 1; attempt <= MaxRetries; attempt++)
            {
                try
                {
                    // 创建超时取消令牌
                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));

                    var chatOptions = new ChatOptions
                    {
                        MaxOutputTokens = AiSettings.MaxTokens,
                        Temperature = (float)AiSettings.Temperature,
                        ModelId = AiSettings.Model
                    };

                    var response = await chatClient.GetResponseAsync(messages, chatOptions, cts.Token);

                    if (response == null || string.IsNullOrEmpty(response.Text))
                    {
                        logger.LogError("AI返回空响应 - 尝试 {Attempt}/{MaxRetries}", attempt, MaxRetries);

                        if (attempt == MaxRetries)
                        {
                            return Result.Failure<string>("OpenAI API 返回空响应，请检查 API 配置或稍后重试", ErrorCodes.ExternalServiceError);
                        }

                        continue;
                    }

                    var rawHtml = response.Text;
                    // 清理 AI 生成的 HTML（移除可能的 markdown 代码块包裹）
                    var html = HtmlValidator.CleanAiGeneratedHtml(rawHtml);

                    // 验证 HTML 格式
                    var config = PromptsConfig;
                    var trustedDomains = config.MarkdownToHtml.Validation?.TrustedDomains;
                    var checkScripts = config.MarkdownToHtml.Validation?.CheckExternalScripts ?? true;
                    var validationMode = config.MarkdownToHtml.Validation?.Mode ?? HtmlValidationMode.Warning;

                    (bool isValid, string? errorMessage, List<string>? warnings) = HtmlValidator.ValidateHtml(
                        html,
                        trustedDomains,
                        checkScripts,
                        validationMode);

                    if (isValid)
                    {
                        // 记录警告信息（如果有）
                        if (warnings?.Count > 0)
                        {
                            foreach (var warning in warnings)
                            {
                                logger.LogWarning("HTML 验证警告: {Warning}", warning);
                            }
                        }

                        logger.LogInformation("AI生成HTML成功 - 尝试: {Attempt}, 长度: {Length}", attempt, html.Length);
                        return Result.Success(html);
                    }

                    logger.LogWarning("HTML验证失败 - 尝试 {Attempt}/{MaxRetries}: {Error}", attempt, MaxRetries, errorMessage);

                    // 如果不是最后一次尝试，添加反馈消息让 AI 修正
                    if (attempt < MaxRetries)
                    {
                        messages.Add(new(ChatRole.Assistant, html));
                        messages.Add(new(ChatRole.User,
                            $"生成的 HTML 格式有误：{errorMessage}。请修正错误并重新生成完整的 HTML 文件。"));
                    }
                }
                catch (OperationCanceledException)
                {
                    logger.LogError("AI调用超时 - {Timeout}秒, 尝试 {Attempt}/{MaxRetries}", timeoutSeconds, attempt, MaxRetries);

                    if (attempt == MaxRetries)
                    {
                        logger.LogError("AI服务超时，已达最大重试次数");
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "AI调用失败 - 尝试 {Attempt}/{MaxRetries}", attempt, MaxRetries);

                    if (attempt == MaxRetries)
                    {
                        logger.LogError("AI服务调用失败，已达最大重试次数");
                    }
                }
            }

            // 所有重试都失败，返回错误信息
            var errorMsg = $"HTML 生成失败：经过 {MaxRetries} 次尝试仍无法生成有效的 HTML。这可能是由于 AI API 超时、网络问题或生成的内容不符合格式要求。";
            logger.LogError("AI生成HTML最终失败");
            return Result.Failure<string>(errorMsg, ErrorCodes.ExternalServiceError);
        }

        public async IAsyncEnumerable<HtmlRenderProgress> RenderMarkdownToHtmlStreamAsync(
            string markdown,
            string? customPrompt = null,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var timeoutSeconds = AiSettings.TimeoutSeconds; // 从配置读取超时时间

            logger.LogInformation("开始流式生成HTML - Markdown长度: {Length}", markdown.Length);

            yield return new HtmlRenderProgress
            {
                Status = HtmlGenerationStatus.Generating,
                Progress = 0
            };

            var systemPrompt = BuildFinalPrompt(customPrompt);

            var messages = new List<ChatMessage>
            {
                new(ChatRole.System, systemPrompt),
                new(ChatRole.User, markdown)
            };

            var chatOptions = new ChatOptions
            {
                MaxOutputTokens = AiSettings.MaxTokens,
                Temperature = (float)AiSettings.Temperature,
                ModelId = AiSettings.Model
            };

            var htmlBuilder = new StringBuilder();
            var chunkCount = 0;
            string? errorResult = null;
            var progressUpdates = new List<HtmlRenderProgress>();

            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

            try
            {
                await foreach (var update in chatClient.GetStreamingResponseAsync(messages, chatOptions, linkedCts.Token))
                {
                    if (!string.IsNullOrEmpty(update.Text))
                    {
                        htmlBuilder.Append(update.Text);
                        chunkCount++;

                        if (chunkCount % 5 == 0)
                        {
                            var progress = Math.Min(90, chunkCount * 2);
                            progressUpdates.Add(new HtmlRenderProgress
                            {
                                Status = HtmlGenerationStatus.Generating,
                                Chunk = update.Text,
                                Progress = progress
                            });
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                logger.LogError("流式生成超时 - {TimeoutSeconds}秒", timeoutSeconds);
                errorResult = $"生成超时（{timeoutSeconds} 秒），请稍后重试";
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "流式生成失败");
                errorResult = $"生成失败：{ex.Message}";
            }

            foreach (var progressUpdate in progressUpdates)
            {
                yield return progressUpdate;
            }

            if (errorResult != null)
            {
                yield return new HtmlRenderProgress
                {
                    Status = HtmlGenerationStatus.Failed,
                    Error = errorResult,
                    Progress = 0
                };
                yield break;
            }

            var rawHtml = htmlBuilder.ToString();
            var cleanedHtml = HtmlValidator.CleanAiGeneratedHtml(rawHtml);

            var config = PromptsConfig;
            var trustedDomains = config.MarkdownToHtml.Validation?.TrustedDomains;
            var checkScripts = config.MarkdownToHtml.Validation?.CheckExternalScripts ?? true;
            var validationMode = config.MarkdownToHtml.Validation?.Mode ?? HtmlValidationMode.Warning;

            (bool isValid, string? errorMessage, List<string>? warnings) = HtmlValidator.ValidateHtml(
                cleanedHtml,
                trustedDomains,
                checkScripts,
                validationMode);

            logger.LogInformation("流式生成完成 - 块数: {ChunkCount}, 长度: {Length}, 验证: {IsValid}",
                chunkCount, cleanedHtml.Length, isValid);

            if (isValid)
            {
                if (warnings?.Count > 0)
                {
                    foreach (var warning in warnings)
                    {
                        logger.LogWarning("HTML 验证警告: {Warning}", warning);
                    }
                }

                yield return new HtmlRenderProgress
                {
                    Status = HtmlGenerationStatus.Completed,
                    Chunk = cleanedHtml,
                    Progress = 100
                };
            }
            else
            {
                logger.LogWarning("HTML验证失败: {Error}", errorMessage);
                yield return new HtmlRenderProgress
                {
                    Status = HtmlGenerationStatus.Failed,
                    Error = $"生成的 HTML 格式有误：{errorMessage}",
                    Progress = 0
                };
            }
        }

        public async Task<Result<string>> GenerateTitleAsync(string markdownContent, string? titlePrompt = null)
        {
            var config = PromptsConfig;
            var prompt = titlePrompt
                ?? config.MetadataGeneration?.TitlePrompt
                ?? _defaultTitlePrompt;

            var messages = new List<ChatMessage>
            {
                new(ChatRole.System, prompt),
                new(ChatRole.User, markdownContent)
            };

            try
            {
                var chatOptions = new ChatOptions
                {
                    MaxOutputTokens = AiSettings.MaxTokens,
                    Temperature = (float)AiSettings.Temperature,
                    ModelId = AiSettings.Model
                };

                var response = await chatClient.GetResponseAsync(messages, chatOptions);
                var title = response?.Text?.Trim() ?? string.Empty;
                title = title.Trim('"', '\'', '「', '」');

                if (string.IsNullOrWhiteSpace(title))
                {
                    return Result.Failure<string>("AI 生成的标题为空", ErrorCodes.ExternalServiceError);
                }

                logger.LogDebug("AI生成标题: {Title}", title);
                return Result.Success(title);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "生成标题失败");
                return Result.Failure<string>($"生成标题失败：{ex.Message}", ErrorCodes.ExternalServiceError);
            }
        }

        public async Task<Result<string>> GenerateSlugAsync(string title, string? slugPrompt = null)
        {
            var config = PromptsConfig;
            var prompt = slugPrompt
                ?? config.MetadataGeneration?.SlugPrompt
                ?? _defaultSlugPrompt;

            var messages = new List<ChatMessage>
            {
                new(ChatRole.System, prompt),
                new(ChatRole.User, $"文章标题：{title}")
            };

            try
            {
                var chatOptions = new ChatOptions
                {
                    MaxOutputTokens = AiSettings.MaxTokens,
                    Temperature = (float)AiSettings.Temperature,
                    ModelId = AiSettings.Model
                };

                var response = await chatClient.GetResponseAsync(messages, chatOptions);
                var slug = response?.Text?.Trim().ToLower() ?? string.Empty;
                slug = slug.Replace(" ", "-").Replace("_", "-");

                if (string.IsNullOrWhiteSpace(slug))
                {
                    return Result.Failure<string>("AI 生成的 slug 为空", ErrorCodes.ExternalServiceError);
                }

                // 截断到最大50个字符（数据库限制）
                if (slug.Length > 50)
                {
                    slug = slug[..50];
                    // 确保不以连字符结尾
                    slug = slug.TrimEnd('-');
                    logger.LogDebug("AI生成的Slug过长，已截断至: {Slug}", slug);
                }

                logger.LogDebug("AI生成Slug: {Slug}", slug);
                return Result.Success(slug);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "生成 Slug 失败");
                return Result.Failure<string>($"生成 Slug 失败：{ex.Message}", ErrorCodes.ExternalServiceError);
            }
        }

        public async Task<Result<string[]>> GenerateTagsAsync(string markdownContent, string? tagsPrompt = null)
        {
            var config = PromptsConfig;
            var prompt = tagsPrompt
                ?? config.MetadataGeneration?.TagsPrompt
                ?? _defaultTagsPrompt;

            var messages = new List<ChatMessage>
            {
                new(ChatRole.System, prompt),
                new(ChatRole.User, markdownContent)
            };

            try
            {
                var chatOptions = new ChatOptions
                {
                    MaxOutputTokens = AiSettings.MaxTokens,
                    Temperature = (float)AiSettings.Temperature,
                    ModelId = AiSettings.Model
                };

                var response = await chatClient.GetResponseAsync(messages, chatOptions);
                var tagsText = response?.Text?.Trim() ?? string.Empty;

                if (string.IsNullOrWhiteSpace(tagsText))
                {
                    return Result.Failure<string[]>("AI 生成的标签为空", ErrorCodes.ExternalServiceError);
                }

                // 清理 AI 生成的文本，移除可能的 markdown 代码块标记
                tagsText = CleanAiGeneratedJson(tagsText);

                // 1️⃣ 尝试 JSON 解析（优先级高）
                if (tagsText.Contains('[') && tagsText.Contains(']'))
                {
                    try
                    {
                        // 提取 JSON 数组部分
                        var startIndex = tagsText.IndexOf('[');
                        var endIndex = tagsText.LastIndexOf(']');

                        if (startIndex >= 0 && endIndex > startIndex)
                        {
                            var jsonText = tagsText.Substring(startIndex, endIndex - startIndex + 1);
                            var tags = JsonSerializer.Deserialize<string[]>(jsonText);

                            if (tags != null && tags.Length > 0)
                            {
                                // 清理每个标签，移除空白和无效字符
                                var cleanedTags = tags
                                    .Select(t => t?.Trim())
                                    .Where(t => !string.IsNullOrWhiteSpace(t))
                                    .Take(10)
                                    .Cast<string>() // 保证为 string[]，移除 null
                                    .ToArray();

                                if (cleanedTags.Length > 0)
                                {
                                    logger.LogDebug("AI生成标签(JSON): {Tags}", string.Join(", ", cleanedTags));
                                    return Result.Success(cleanedTags);
                                }
                            }
                        }
                    }
                    catch (JsonException ex)
                    {
                        // JSON 解析失败，尝试按行分割
                        logger.LogWarning(ex, "标签 JSON 解析失败，尝试按行分割方案");
                    }
                }

                // 2️⃣ 降级方案：按行分割
                var tagsList = tagsText.Split(['\n', '\r'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(t => t.Trim().TrimStart('-', '*', '•', ' ').Trim())
                    .Where(t => !string.IsNullOrWhiteSpace(t))
                    .Take(10)
                    .ToArray();

                if (tagsList.Length == 0)
                {
                    return Result.Failure<string[]>("无法解析 AI 生成的标签", ErrorCodes.ExternalServiceError);
                }

                logger.LogDebug("AI生成标签(行分割): {Tags}", string.Join(", ", tagsList));
                return Result.Success(tagsList);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "生成标签失败");
                return Result.Failure<string[]>($"生成标签失败：{ex.Message}", ErrorCodes.ExternalServiceError);
            }
        }

        /// <summary>
        /// 清理 AI 生成的 JSON 文本，移除 markdown 代码块标记和其他干扰字符
        /// </summary>
        private static string CleanAiGeneratedJson(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            // 移除 markdown 代码块标记 ```json 和 ```
            text = JsonCodeBlockRegex().Replace(text, "");
            text = CodeBlockRegex().Replace(text, "");

            // 移除常见的前缀说明文字（如 "标签：" "Tags:" 等）
            text = TextBeforeJsonArrayRegex().Replace(text, "");

            return text.Trim();
        }

        [GeneratedRegex(@"```json\s*", RegexOptions.IgnoreCase)]
        private static partial Regex JsonCodeBlockRegex();

        [GeneratedRegex(@"```\s*")]
        private static partial Regex CodeBlockRegex();

        [GeneratedRegex(@"^[\s\S]*?(?=\[)")]
        private static partial Regex TextBeforeJsonArrayRegex();

        public async Task<Result<string>> GenerateExcerptAsync(string markdownContent, string? excerptPrompt = null)
        {
            var config = PromptsConfig;
            var prompt = excerptPrompt
                ?? config.MetadataGeneration?.ExcerptPrompt
                ?? _defaultExcerptPrompt;

            var messages = new List<ChatMessage>
            {
                new(ChatRole.System, prompt),
                new(ChatRole.User, markdownContent)
            };

            try
            {
                var chatOptions = new ChatOptions
                {
                    MaxOutputTokens = AiSettings.MaxTokens,
                    Temperature = (float)AiSettings.Temperature,
                    ModelId = AiSettings.Model
                };

                var response = await chatClient.GetResponseAsync(messages, chatOptions);
                var excerpt = response?.Text?.Trim() ?? string.Empty;
                excerpt = excerpt.Trim('"', '\'', '「', '」');

                if (string.IsNullOrWhiteSpace(excerpt))
                {
                    return Result.Failure<string>("AI 生成的摘要为空", ErrorCodes.ExternalServiceError);
                }

                logger.LogDebug("AI生成摘要: {Excerpt}", excerpt[..Math.Min(50, excerpt.Length)]);
                return Result.Success(excerpt);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "生成摘要失败");
                return Result.Failure<string>($"生成摘要失败：{ex.Message}", ErrorCodes.ExternalServiceError);
            }
        }
    }
}
