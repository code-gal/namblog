using System.Linq;
using Mapster;
using NamBlog.API.Domain.Entities;
using NamBlog.API.Domain.ValueObjects;

namespace NamBlog.API.Application.DTOs
{
    /// <summary>
    /// Mapster 映射配置（Post/PostVersion 实体 ↔ DTO）
    /// </summary>
    public static class ArticleMappingConfig
    {
        public static void Configure()
        {
            // HtmlRenderProgress → HtmlConversionProgress（Domain ValueObject → Application DTO）
            TypeAdapterConfig<HtmlRenderProgress, HtmlConversionProgress>.NewConfig()
                .Map(dest => dest.Status, src => MapHtmlStatus(src.Status))
                .Map(dest => dest.Chunk, src => src.Chunk)
                .Map(dest => dest.Progress, src => src.Progress)
                .Map(dest => dest.Error, src => src.Error);

            // Post → ArticleListItemDto（列表项，精简字段）
            TypeAdapterConfig<Post, ArticleListItemDto>.NewConfig()
                .Map(dest => dest.Id, src => src.PostId)
                .Map(dest => dest.Excerpt, src => src.Excerpt ?? "")
                .Map(dest => dest.Tags, src => src.Tags.Select(t => t.Name).ToArray());

            // Post → ArticleDetailDto（详情，完整字段）
            TypeAdapterConfig<Post, ArticleDetailDto>.NewConfig()
                .Map(dest => dest.Id, src => src.PostId)
                .Map(dest => dest.Excerpt, src => src.Excerpt ?? "")
                .Map(dest => dest.Tags, src => src.Tags.Select(t => t.Name).ToArray())
                .Map(dest => dest.AiPrompts, src => src.Versions.Select(v => v.AiPrompt ?? "").Where(p => !string.IsNullOrEmpty(p)).ToArray())
                .Map(dest => dest.Versions, src => src.Versions)
                .Map(dest => dest.MainVersion, src => src.MainVersion)
                .Map(dest => dest.FilePath, src => src.FilePath)
                .Map(dest => dest.FileName, src => src.FileName);

            // PostVersion → ArticleVersionDto（版本信息，用于查询展示）
            TypeAdapterConfig<PostVersion, ArticleVersionDto>.NewConfig()
                .Map(dest => dest.VersionId, src => src.PostVersionId)
                .Map(dest => dest.ValidationError, src => src.HtmlValidationError);
        }

        /// <summary>
        /// Domain 层 HtmlGenerationStatus 映射为 Application 层 HtmlConversionStatus
        /// </summary>
        private static HtmlConversionStatus MapHtmlStatus(HtmlGenerationStatus status)
        {
            return status switch
            {
                HtmlGenerationStatus.Generating => HtmlConversionStatus.Generating,
                HtmlGenerationStatus.Completed => HtmlConversionStatus.Completed,
                HtmlGenerationStatus.Failed => HtmlConversionStatus.Failed,
                _ => HtmlConversionStatus.Failed
            };
        }
    }
}
