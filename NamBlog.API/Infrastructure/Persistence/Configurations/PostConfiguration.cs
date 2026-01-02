using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NamBlog.API.Domain.Entities;
using NamBlog.API.Domain.Specifications;

namespace NamBlog.API.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// EF Core Post实体映射配置
    /// </summary>
    public class PostConfiguration : IEntityTypeConfiguration<Post>
    {
        public void Configure(EntityTypeBuilder<Post> builder)
        {
            builder.HasKey(p => p.PostId);

            // 唯一约束
            builder.HasIndex(p => p.Title).IsUnique();
            builder.HasIndex(p => p.Slug).IsUnique();

            // FilePath + FileName 组合唯一约束
            builder.HasIndex(p => new { p.FilePath, p.FileName }).IsUnique();

            // 普通索引
            builder.HasIndex(p => p.Category);
            builder.HasIndex(p => p.IsFeatured);
            builder.HasIndex(p => p.IsPublished);

            // 字段配置 - 使用 ValidationRuleset 确保一致性
            builder.Property(p => p.Title)
                .IsRequired()
                .HasMaxLength(ValidationRuleset.Post.Title.MaxLength!.Value);

            builder.Property(p => p.Slug)
                .IsRequired()
                .HasMaxLength(ValidationRuleset.Post.Slug.MaxLength!.Value);

            builder.Property(p => p.FileName)
                .IsRequired()
                .HasMaxLength(ValidationRuleset.Post.FileName.MaxLength!.Value);

            builder.Property(p => p.FilePath)
                .HasMaxLength(ValidationRuleset.Post.FilePath.MaxLength!.Value);

            builder.Property(p => p.Category)
                .IsRequired()
                .HasMaxLength(ValidationRuleset.Post.Category.MaxLength!.Value);

            builder.Property(p => p.Excerpt)
                .HasMaxLength(ValidationRuleset.Post.Excerpt.MaxLength!.Value);

            builder.Property(p => p.Author)
                .HasMaxLength(ValidationRuleset.Post.Author.MaxLength!.Value)
                .HasDefaultValue("Anonymous");

            // 时间戳字段（由领域模型管理，不使用数据库默认值）
            builder.Property(p => p.CreateTime).IsRequired();
            builder.Property(p => p.LastModified).IsRequired();
            builder.Property(p => p.PublishedAt); // 可为空

            // 关系配置：Post -> Versions（一对多）
            builder.HasMany(p => p.Versions)
                .WithOne(v => v.Post)
                .HasForeignKey(v => v.PostId)
                .OnDelete(DeleteBehavior.Cascade);

            // 关系配置：Post -> MainVersion（自引用）
            builder.HasOne(p => p.MainVersion)
                .WithMany()
                .HasForeignKey(p => p.MainVersionId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
