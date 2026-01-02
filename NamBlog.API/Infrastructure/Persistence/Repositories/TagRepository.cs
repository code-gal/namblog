using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NamBlog.API.Domain.Entities;
using NamBlog.API.Domain.Interfaces;

namespace NamBlog.API.Infrastructure.Persistence.Repositories
{
    /// <summary>
    /// 标签仓储实现
    /// </summary>
    public class TagRepository(BlogContext context) : ITagRepository
    {
        public async Task<PostTag?> GetByNameAsync(string name)
        {
            return await context.Tags
                .FirstOrDefaultAsync(t => t.Name == name);
        }

        public async Task<IEnumerable<PostTag>> GetOrCreateTagsAsync(IEnumerable<string> tagNames)
        {
            var tags = new List<PostTag>();
            foreach (var tagName in tagNames)
            {
                var tag = await GetByNameAsync(tagName);
                if (tag == null)
                {
                    tag = PostTag.Create(tagName);
                    await context.Tags.AddAsync(tag);
                }

                tags.Add(tag);
            }

            return tags;
        }

        public async Task<IEnumerable<PostTag>> GetAllAsync()
        {
            return await context.Tags
                .Include(t => t.Posts)
                .ToListAsync();
        }

        public async Task DeleteOrphanedTagsAsync()
        {
            var orphanedTags = await context.Tags
                .Include(t => t.Posts)
                .Where(t => !t.Posts.Any())
                .ToListAsync();

            context.Tags.RemoveRange(orphanedTags);
        }

        public async Task<int> SaveChangesAsync()
        {
            return await context.SaveChangesAsync();
        }
    }
}
