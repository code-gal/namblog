using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NamBlog.API.Domain.Entities;
using NamBlog.API.Domain.Interfaces;

namespace NamBlog.API.Infrastructure.Persistence.Repositories
{
    /// <summary>
    /// 文章仓储实现
    /// </summary>
    public class PostRepository(BlogContext context) : IPostRepository
    {
        public async Task<Post?> GetByIdAsync(int postId)
        {
            return await context.Posts
                .Include(p => p.Versions)
                .Include(p => p.Tags)
                .Include(p => p.MainVersion)
                .AsSplitQuery() // 拆分为多个查询，避免笛卡尔积
                .FirstOrDefaultAsync(p => p.PostId == postId);
        }

        public async Task<Post?> GetBySlugAsync(string slug)
        {
            return await context.Posts
                .Include(p => p.Versions)
                .Include(p => p.Tags)
                .Include(p => p.MainVersion)
                .AsSplitQuery() // 拆分为多个查询，避免笛卡尔积
                .FirstOrDefaultAsync(p => p.Slug == slug);
        }

        public IQueryable<Post> GetAll()
        {
            return context.Posts
                .Include(p => p.Versions)
                .Include(p => p.Tags)
                .Include(p => p.MainVersion)
                .AsSplitQuery(); // 拆分为多个查询，避免笛卡尔积
        }

        public async Task AddAsync(Post post)
        {
            await context.Posts.AddAsync(post);
        }

        public void Update(Post post)
        {
            context.Posts.Update(post);
        }

        public void Delete(Post post)
        {
            context.Posts.Remove(post);
        }

        public async Task<int> SaveChangesAsync()
        {
            return await context.SaveChangesAsync();
        }

        public async Task<bool> SlugExistsAsync(string slug, int? excludePostId = null)
        {
            var query = context.Posts.Where(p => p.Slug == slug);
            if (excludePostId.HasValue)
            {
                query = query.Where(p => p.PostId != excludePostId.Value);
            }

            return await query.AnyAsync();
        }

        public async Task<bool> TitleExistsAsync(string title, int? excludePostId = null)
        {
            var query = context.Posts.Where(p => p.Title == title);
            if (excludePostId.HasValue)
            {
                query = query.Where(p => p.PostId != excludePostId.Value);
            }

            return await query.AnyAsync();
        }

        public async Task<List<(string Name, int Count)>> GetTagsStatisticsAsync(string? category = null, bool includeUnpublished = false)
        {
            // 从 Tags 表出发，使用子查询统计，避免笛卡尔积
            var query = context.Tags.AsNoTracking();

            var statistics = await query
                .Select(t => new
                {
                    t.Name,
                    // EF Core 将生成子查询：SELECT COUNT(*) FROM Posts WHERE ...
                    Count = t.Posts.Count(p =>
                        (includeUnpublished || p.IsPublished) &&
                        (string.IsNullOrEmpty(category) || p.Category == category))
                })
                .Where(x => x.Count > 0)  // 过滤掉没有文章的标签
                .ToListAsync();

            // 在内存中转换为元组（EF Core 表达式树不支持元组）
            return [.. statistics.Select(x => (x.Name, x.Count))];
        }

        public async Task<List<(string Name, int Count)>> GetCategoriesStatisticsAsync(string[]? tags = null, bool includeUnpublished = false)
        {
            // 优化：直接查询 Posts 表，不使用 Include 避免加载不必要的关系
            var query = context.Posts.AsNoTracking();

            // 过滤未发布的文章
            if (!includeUnpublished)
            {
                query = query.Where(p => p.IsPublished);
            }

            // 按标签过滤（AND 逻辑：文章必须包含所有指定标签）
            if (tags != null && tags.Length > 0)
            {
                foreach (var tag in tags)
                {
                    var trimmedTag = tag.Trim();
                    // EF Core 将生成 EXISTS 子查询
                    query = query.Where(p => p.Tags.Any(t => t.Name == trimmedTag));
                }
            }

            // 按分类分组统计（一篇文章只有一个分类，无需去重）
            var statistics = await query
                .GroupBy(p => p.Category)
                .Select(g => new { Name = g.Key, Count = g.Count() })
                .ToListAsync();

            // 在内存中转换为元组（EF Core 表达式树不支持元组）
            return [.. statistics.Select(x => (x.Name, x.Count))];
        }
    }
}
