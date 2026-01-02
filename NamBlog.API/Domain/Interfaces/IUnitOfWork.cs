using System.Threading;
using System.Threading.Tasks;

namespace NamBlog.API.Domain.Interfaces
{
    /// <summary>
    /// 工作单元接口 - 统一管理事务边界
    /// </summary>
    public interface IUnitOfWork
    {
        /// <summary>
        /// 保存所有更改到数据库
        /// </summary>
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
