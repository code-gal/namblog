using System;
using System.Collections.Generic;
using NamBlog.API.Domain.Entities;

namespace NamBlog.API.Application.DTOs
{
    /// <summary>
    /// 文章列表项 DTO（用于列表页，精简字段）
    /// </summary>
    public record ArticleListItemDto(
        int Id,
        string Title,
        string Slug,
        string Excerpt,
        string Category,
        string[] Tags,
        bool IsPublished,
        bool IsFeatured,
        DateTimeOffset? PublishedAt,
        DateTimeOffset LastModified);

    /// <summary>
    /// 文章详情 DTO（用于详情页，包含所有信息）
    /// </summary>
    public record ArticleDetailDto
    {
        public int Id { get; init; }
        public string Title { get; init; } = string.Empty;
        public string Slug { get; init; } = string.Empty;
        public string Author { get; init; } = "Anonymous";
        public string Excerpt { get; init; } = string.Empty;
        public string Category { get; init; } = string.Empty;
        public string[] Tags { get; init; } = [];
        public string[] AiPrompts { get; init; } = [];  // 前端需要，用于显示使用的 AI 提示词历史
        public bool IsPublished { get; init; } = false;
        public bool IsFeatured { get; init; } = false;
        public DateTimeOffset? PublishedAt { get; init; }
        public DateTimeOffset CreateTime { get; init; }
        public DateTimeOffset LastModified { get; init; }
        public List<ArticleVersionDto> Versions { get; init; } = [];
        public ArticleVersionDto? MainVersion { get; init; }  // 当前发布的版本

        // 内部字段，用于 GraphQL Resolver 读取文件，不暴露到 Schema
        internal string FilePath { get; init; } = string.Empty;
        internal string FileName { get; init; } = string.Empty;
    }

    /// <summary>
    /// 文章版本 DTO（避免直接暴露领域实体 PostVersion）
    /// 说明：用于展示查询结果中的版本信息
    /// </summary>
    public record ArticleVersionDto(
        int VersionId,
        string VersionName,
        string? AiPrompt,
        HtmlValidationStatus ValidationStatus,
        string? ValidationError,
        DateTimeOffset CreatedAt);

    /// <summary>
    /// 文章列表查询结果（包含列表和总数）
    /// </summary>
    public record ArticleListResult
    {
        public List<ArticleListItemDto> Items { get; init; } = [];
        public int TotalCount { get; init; } = 0;
    }
}
