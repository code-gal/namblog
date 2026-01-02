using System.Collections.Generic;
using System.Threading.Tasks;
using NamBlog.API.Domain.Entities;

namespace NamBlog.API.Domain.Interfaces
{
    /// <summary>
    /// 标签仓储接口
    /// </summary>
    public interface ITagRepository
    {
        /// <summary>
        /// 根据名称获取标签
        /// </summary>
        public Task<PostTag?> GetByNameAsync(string name);

        /// <summary>
        /// 根据名称批量获取或创建标签
        /// </summary>
        public Task<IEnumerable<PostTag>> GetOrCreateTagsAsync(IEnumerable<string> tagNames);

        /// <summary>
        /// 获取所有标签
        /// </summary>
        public Task<IEnumerable<PostTag>> GetAllAsync();

        /// <summary>
        /// 删除孤立标签（没有关联文章的标签）
        /// </summary>
        public Task DeleteOrphanedTagsAsync();

        /// <summary>
        /// 保存更改
        /// </summary>
        public Task<int> SaveChangesAsync();
    }
}
