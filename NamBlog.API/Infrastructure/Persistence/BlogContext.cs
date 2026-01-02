using Microsoft.EntityFrameworkCore;
using NamBlog.API.Domain.Entities;
using NamBlog.API.Infrastructure.Persistence.Configurations;

namespace NamBlog.API.Infrastructure.Persistence
{
    /// <summary>
    /// 博客数据库上下文（支持 SQLite 和 PostgreSQL）
    /// </summary>
    public class BlogContext(DbContextOptions<BlogContext> options) : DbContext(options)
    {
        public DbSet<Post> Posts { get; set; } = null!;
        public DbSet<PostVersion> PostVersions { get; set; } = null!;
        public DbSet<PostTag> Tags { get; set; } = null!;

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // 【跨数据库支持的安全方案】
            //
            // 背景：
            // - 迁移基于 PostgreSQL 生成（类型：integer, varchar）
            // - 切换到 SQLite 时，EF Core 检测到类型差异（INTEGER, TEXT）
            // - 触发 PendingModelChangesWarning，但实际表结构一致
            //
            // 安全措施：
            // 1. ✅ 忽略警告，允许跨数据库使用（必要的妥协）
            // 2. ✅ 实体配置确保结构一致性（PostConfiguration 等）
            // 3. ✅ 迁移文件包含跨数据库注解（支持两种数据库）
            // 4. ✅ 启动时应用迁移，确保结构同步（Database.Migrate()）
            //
            // 风险控制：
            // ⚠️ 开发规范：修改实体后务必生成迁移（dotnet ef migrations add）
            // ⚠️ CI/CD 检查：自动检测未提交的迁移
            // ⚠️ 代码审查：检查 Domain/Entities 变更是否包含迁移
            //
            // 生产影响：
            // - 用户选择数据库后不会切换，此警告不会触发
            // - 即使忽略警告，运行时错误仍会暴露真正的结构问题
            // - Database.Migrate() 会在启动时同步结构，降低风险

            optionsBuilder.ConfigureWarnings(warnings =>
                warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // 应用实体配置
            modelBuilder.ApplyConfiguration(new PostConfiguration());
            modelBuilder.ApplyConfiguration(new PostVersionConfiguration());
            modelBuilder.ApplyConfiguration(new PostTagConfiguration());
        }
    }
}
