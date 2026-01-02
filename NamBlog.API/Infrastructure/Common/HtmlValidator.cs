using System;
using System.Collections.Generic;
using System.Linq;
using HtmlAgilityPack;
using NamBlog.API.Infrastructure.Agents;

namespace NamBlog.API.Infrastructure.Common
{
    /// <summary>
    /// HTML 验证服务实现
    /// </summary>
    public static class HtmlValidator
    {
        /// <summary>
        /// 验证 HTML 内容
        /// </summary>
        /// <param name="html">HTML 内容</param>
        /// <param name="trustedDomains">可信域名列表（可选）</param>
        /// <param name="checkExternalScripts">是否检查外部脚本</param>
        /// <param name="validationMode">验证模式</param>
        /// <returns>验证结果和警告列表</returns>
        public static (bool IsValid, string? ErrorMessage, List<string>? Warnings) ValidateHtml(
            string html,
            List<string>? trustedDomains = null,
            bool checkExternalScripts = true,
            HtmlValidationMode validationMode = HtmlValidationMode.Warning)
        {
            var warnings = new List<string>();

            if (string.IsNullOrWhiteSpace(html))
            {
                return (false, "HTML 内容为空", null);
            }

            // 1. 检查基本 HTML 结构
            if (!html.Contains("<!DOCTYPE html>", StringComparison.OrdinalIgnoreCase))
            {
                return (false, "缺少 DOCTYPE 声明", null);
            }

            if (!html.Contains("<html", StringComparison.OrdinalIgnoreCase) ||
                !html.Contains("</html>", StringComparison.OrdinalIgnoreCase))
            {
                return (false, "缺少完整的 <html> 标签", null);
            }

            if (!html.Contains("<head", StringComparison.OrdinalIgnoreCase) ||
                !html.Contains("</head>", StringComparison.OrdinalIgnoreCase))
            {
                return (false, "缺少 <head> 标签", null);
            }

            if (!html.Contains("<body", StringComparison.OrdinalIgnoreCase) ||
                !html.Contains("</body>", StringComparison.OrdinalIgnoreCase))
            {
                return (false, "缺少 <body> 标签", null);
            }

            // 2. 使用 HtmlAgilityPack 验证标签闭合
            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(html);

            if (htmlDoc.ParseErrors != null && htmlDoc.ParseErrors.Any())
            {
                var firstError = htmlDoc.ParseErrors.First();
                return (false, $"HTML 解析错误：{firstError.Reason}（第 {firstError.Line} 行）", null);
            }

            // 3. 检查是否包含必要的 html、head、body 节点
            var htmlNode = htmlDoc.DocumentNode.SelectSingleNode("//html");
            if (htmlNode == null)
            {
                return (false, "无法找到有效的 <html> 节点", null);
            }

            var headNode = htmlDoc.DocumentNode.SelectSingleNode("//head");
            if (headNode == null)
            {
                return (false, "无法找到有效的 <head> 节点", null);
            }

            var bodyNode = htmlDoc.DocumentNode.SelectSingleNode("//body");
            if (bodyNode == null)
            {
                return (false, "无法找到有效的 <body> 节点", null);
            }

            // 4. 检查外部脚本（根据验证模式处理）
            if (checkExternalScripts && validationMode != HtmlValidationMode.Permissive)
            {
                var scriptWarnings = ValidateExternalScripts(htmlDoc, trustedDomains ?? [], validationMode);

                if (validationMode == HtmlValidationMode.Strict && scriptWarnings.Count > 0)
                {
                    // 严格模式：发现非可信脚本时直接返回错误
                    return (false, scriptWarnings[0], null);
                }

                // 警告模式：记录警告但允许继续
                warnings.AddRange(scriptWarnings);
            }

            return (true, null, warnings.Count > 0 ? warnings : null);
        }

        /// <summary>
        /// 验证外部脚本安全性
        /// </summary>
        private static List<string> ValidateExternalScripts(
            HtmlDocument htmlDoc,
            List<string> trustedDomains,
            HtmlValidationMode validationMode)
        {
            var warnings = new List<string>();
            var externalScripts = htmlDoc.DocumentNode.SelectNodes("//script[@src]");

            if (externalScripts == null || externalScripts.Count <= 0)
            {
                return warnings;
            }

            foreach (var script in externalScripts)
            {
                var src = script.GetAttributeValue("src", "").Trim();
                if (string.IsNullOrEmpty(src))
                {
                    continue;
                }

                // 允许相对路径和本地资源
                if (IsRelativePath(src) || IsLocalhost(src))
                {
                    continue;
                }

                // 检查是否为可信域名
                if (!IsTrustedDomain(src, trustedDomains))
                {
                    var message = validationMode == HtmlValidationMode.Strict
                        ? $"检测到来自非可信域名的外部脚本（已阻止）: {src}"
                        : $"检测到来自非可信域名的外部脚本: {src}";

                    warnings.Add(message);
                }
            }

            return warnings;
        }

        /// <summary>
        /// 检查域名是否可信（支持域名级别匹配）
        /// </summary>
        private static bool IsTrustedDomain(string url, List<string> trustedDomains)
        {
            if (trustedDomains == null || trustedDomains.Count <= 0)
            {
                return false;
            }

            try
            {
                var uri = new Uri(url, UriKind.Absolute);
                return trustedDomains.Any(domain =>
                    uri.Host.Equals(domain, StringComparison.OrdinalIgnoreCase) ||
                    uri.Host.EndsWith($".{domain}", StringComparison.OrdinalIgnoreCase));
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 检查是否为本地主机
        /// </summary>
        private static bool IsLocalhost(string url) =>
            url.StartsWith("http://localhost", StringComparison.OrdinalIgnoreCase) ||
            url.StartsWith("https://localhost", StringComparison.OrdinalIgnoreCase) ||
            url.StartsWith("http://127.0.0.1", StringComparison.OrdinalIgnoreCase) ||
            url.StartsWith("https://127.0.0.1", StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// 判断是否为相对路径
        /// </summary>
        private static bool IsRelativePath(string path) =>
            !path.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
            !path.StartsWith("https://", StringComparison.OrdinalIgnoreCase) &&
            !path.StartsWith("//", StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// 清理 AI 生成的 HTML，移除可能的 markdown 代码块包裹
        /// </summary>
        public static string CleanAiGeneratedHtml(string rawHtml)
        {
            if (string.IsNullOrWhiteSpace(rawHtml))
                return rawHtml;

            var cleaned = rawHtml.Trim();
            // 移除开头的 ```html 或 ```
            if (cleaned.StartsWith("```html", StringComparison.OrdinalIgnoreCase))
            {
                cleaned = cleaned.AsSpan(7).TrimStart().ToString();
            }
            else if (cleaned.StartsWith("```"))
            {
                var firstLineEnd = cleaned.IndexOf('\n');
                if (firstLineEnd > 0)
                {
                    cleaned = cleaned.AsSpan(firstLineEnd + 1).TrimStart().ToString();
                }
            }

            if (cleaned.EndsWith("```"))
            {
                cleaned = cleaned.AsSpan(0, cleaned.Length - 3).TrimEnd().ToString();
            }

            return cleaned.Trim();
        }
    }
}
