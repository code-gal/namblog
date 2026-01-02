using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NamBlog.API.Domain.Entities;
using NamBlog.API.Domain.Specifications;

namespace NamBlog.API.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// EF Core Tag 实体配置
    /// </summary>
    public class PostTagConfiguration : IEntityTypeConfiguration<PostTag>
    {
        public void Configure(EntityTypeBuilder<PostTag> builder)
        {
            builder.HasKey(t => t.TagId);

            // 唯一约束
            builder.HasIndex(t => t.Name).IsUnique();

            // 字段配置 - 使用 ValidationRuleset 确保一致性
            builder.Property(t => t.Name)
                .IsRequired()
                .HasMaxLength(ValidationRuleset.Tag.MaxLength!.Value);

            // 多对多关系：Post <-> Tag
            builder.HasMany(t => t.Posts)
                .WithMany(p => p.Tags)
                .UsingEntity<Dictionary<string, object>>(
                    "PostTag", // 中间表名
                    j => j.HasOne<Post>().WithMany().HasForeignKey("PostId"),
                    j => j.HasOne<PostTag>().WithMany().HasForeignKey("TagId"),
                    j =>
                    {
                        j.HasKey("PostId", "TagId");
                        j.HasIndex("TagId"); // 为反向查询添加索引
                    });
        }
    }
}
