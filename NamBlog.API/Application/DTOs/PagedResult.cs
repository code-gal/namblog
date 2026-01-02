using System.Collections.Generic;

namespace NamBlog.API.Application.DTOs
{
    /// <summary>
    /// 分页信息（包含当前页码、页数、是否有上下页等元信息）
    /// </summary>
    public record PageInfo
    {
        /// <summary>
        /// 当前页码（从1开始）
        /// </summary>
        public int CurrentPage { get; init; } = 1;

        /// <summary>
        /// 每页大小
        /// </summary>
        public int PageSize { get; init; } = 10;

        /// <summary>
        /// 总记录数
        /// </summary>
        public int TotalCount { get; init; } = 0;

        /// <summary>
        /// 总页数
        /// </summary>
        public int TotalPages { get; init; } = 0;

        /// <summary>
        /// 是否有上一页
        /// </summary>
        public bool HasPreviousPage { get; init; } = false;

        /// <summary>
        /// 是否有下一页
        /// </summary>
        public bool HasNextPage { get; init; } = false;

        /// <summary>
        /// 根据总数和页码计算分页信息
        /// </summary>
        /// <param name="currentPage">当前页码（最小为1）</param>
        /// <param name="pageSize">每页大小（最大100）</param>
        /// <param name="totalCount">总记录数</param>
        /// <returns>分页信息</returns>
        public static PageInfo Create(int currentPage, int pageSize, int totalCount)
        {
            // 页码最小为1
            currentPage = currentPage < 1 ? 1 : currentPage;
            // 页大小限制在1-100之间
            pageSize = pageSize < 1 ? 10 : (pageSize > 100 ? 100 : pageSize);

            var totalPages = totalCount > 0 ? (int)System.Math.Ceiling(totalCount / (double)pageSize) : 0;

            // 如果当前页超过总页数，调整为最后一页
            if (currentPage > totalPages && totalPages > 0)
            {
                currentPage = totalPages;
            }

            return new PageInfo
            {
                CurrentPage = currentPage,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = totalPages,
                HasPreviousPage = currentPage > 1,
                HasNextPage = currentPage < totalPages
            };
        }
    }

    /// <summary>
    /// 分页结果（泛型，支持任意类型的分页数据）
    /// </summary>
    public record PagedResult<T>
    {
        /// <summary>
        /// 当前页的数据列表
        /// </summary>
        public List<T> Items { get; init; } = [];

        /// <summary>
        /// 分页信息
        /// </summary>
        public PageInfo PageInfo { get; init; } = new();
    }
}
