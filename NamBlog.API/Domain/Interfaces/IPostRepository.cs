using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NamBlog.API.Domain.Entities;

namespace NamBlog.API.Domain.Interfaces
{
    /// <summary>
    /// 文章仓储接口
    /// </summary>
    public interface IPostRepository
    {
        /// <summary>
        /// 根据 ID 获取文章（包含版本和标签）
        /// </summary>
        public Task<Post?> GetByIdAsync(int postId);

        /// <summary>
        /// 根据 Slug 获取文章
        /// </summary>
        public Task<Post?> GetBySlugAsync(string slug);

        /// <summary>
        /// 获取所有文章（支持查询过滤）
        /// </summary>
        public IQueryable<Post> GetAll();

        /// <summary>
        /// 添加文章
        /// </summary>
        public Task AddAsync(Post post);

        /// <summary>
        /// 更新文章
        /// </summary>
        public void Update(Post post);

        /// <summary>
        /// 删除文章
        /// </summary>
        public void Delete(Post post);

        /// <summary>
        /// 保存更改
        /// </summary>
        public Task<int> SaveChangesAsync();

        /// <summary>
        /// 检查 Slug 是否已存在（排除指定文章ID）
        /// </summary>
        public Task<bool> SlugExistsAsync(string slug, int? excludePostId = null);

        /// <summary>
        /// 检查 Title 是否已存在（排除指定文章ID）
        /// </summary>
        public Task<bool> TitleExistsAsync(string title, int? excludePostId = null);

        /// <summary>
        /// 获取标签统计（每个标签对应的文章数，一篇文章只统计一次）
        /// 返回：(标签名, 文章数量)
        /// </summary>
        public Task<List<(string Name, int Count)>> GetTagsStatisticsAsync(string? category = null, bool includeUnpublished = false);

        /// <summary>
        /// 获取分类统计（每个分类对应的文章数，一篇文章只统计一次）
        /// 返回：(分类名, 文章数量)
        /// </summary>
        public Task<List<(string Name, int Count)>> GetCategoriesStatisticsAsync(string[]? tags = null, bool includeUnpublished = false);
    }
}
