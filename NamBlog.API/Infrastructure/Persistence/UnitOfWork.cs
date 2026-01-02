using System.Threading;
using System.Threading.Tasks;
using NamBlog.API.Domain.Interfaces;

namespace NamBlog.API.Infrastructure.Persistence
{
    /// <summary>
    /// 工作单元实现 - 封装 DbContext 的 SaveChangesAsync
    /// </summary>
    public class UnitOfWork(BlogContext context) : IUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
            => context.SaveChangesAsync(cancellationToken);
    }
}
