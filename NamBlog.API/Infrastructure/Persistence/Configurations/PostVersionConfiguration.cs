using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NamBlog.API.Domain.Entities;

namespace NamBlog.API.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// EF Core PostVersion 实体配置
    /// </summary>
    public class PostVersionConfiguration : IEntityTypeConfiguration<PostVersion>
    {
        public void Configure(EntityTypeBuilder<PostVersion> builder)
        {
            builder.HasKey(pv => pv.PostVersionId);

            // 复合唯一索引：同一篇文章不能有重复的版本名称
            builder.HasIndex(pv => new { pv.PostId, pv.VersionName }).IsUnique();

            // 字段配置
            builder.Property(pv => pv.VersionName)
                .IsRequired()
                .HasMaxLength(30);

            builder.Property(pv => pv.AiPrompt);
            // AI提示词不限制长度，数据库使用TEXT类型

            builder.Property(pv => pv.HtmlValidationError);
            // 验证错误信息不限制长度，数据库使用TEXT类型

            builder.Property(pv => pv.ValidationStatus)
                .IsRequired()
                .HasConversion<int>(); // 枚举存储为整数

            builder.Property(pv => pv.CreatedAt)
                .IsRequired();

            // 外键关系：PostVersion -> Post（一对多）
            builder.HasOne(pv => pv.Post)
                .WithMany(p => p.Versions)
                .HasForeignKey(pv => pv.PostId)
                .OnDelete(DeleteBehavior.Cascade); // 删除文章时级联删除所有版本
        }
    }
}
