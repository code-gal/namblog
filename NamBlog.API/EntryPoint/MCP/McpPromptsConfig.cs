using System.Collections.Generic;

namespace NamBlog.API.EntryPoint.MCP
{
    /// <summary>
    /// MCP 提示词配置（映射 mcp-prompts.json）
    /// </summary>
    public class McpPromptsConfig
    {
        public Dictionary<string, PromptTemplate> Prompts { get; set; } = [];
    }

    /// <summary>
    /// 提示词模板
    /// </summary>
    public class PromptTemplate
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Template { get; set; } = string.Empty;
        public List<PromptParameter> Parameters { get; set; } = [];
    }

    /// <summary>
    /// 提示词参数
    /// </summary>
    public class PromptParameter
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool Required { get; set; }
    }
}
