using System;
using System.Collections.Generic;
using NamBlog.API.Domain.Entities;

namespace NamBlog.API.Application.DTOs
{
    /// <summary>
    /// 文章元数据 DTO（用于返回保存后的元数据，包含管理员编辑页所需的完整数据）
    /// </summary>
    public record ArticleMetadataDto(
        int PostId,
        string? Title,
        string? Slug,
        string? Excerpt,
        string Category,
        string[]? Tags,

        // 管理员编辑页所需的扩展字段
        List<CategoryStatistic> AllCategories,  // 所有分类（含统计）
        List<TagStatistic> AllTags,             // 所有标签（含统计）
        string[] AiPrompts,                      // AI提示词历史
        string[] VersionNames,                   // 版本名称列表
        MainVersionDetail? MainVersion,          // 主版本详细信息
        bool IsPublished,
        bool IsFeatured,

        DateTimeOffset CreateTime,
        DateTimeOffset LastModified);

    /// <summary>
    /// 主版本详细信息（包含HTML内容和验证状态）
    /// </summary>
    public record MainVersionDetail(
        string VersionName,
        string Html,
        HtmlValidationStatus ValidationStatus,
        string? HtmlValidationError);

    /// <summary>
    /// 文章版本提交结果 DTO（用于返回创建版本后的结果）
    /// 说明：提交成功后前端跳转到文章页，失败则显示错误信息
    /// </summary>
    public record ArticleVersionSubmitDto(string Slug);
}
