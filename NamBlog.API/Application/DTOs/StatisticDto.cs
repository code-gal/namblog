namespace NamBlog.API.Application.DTOs
{
    /// <summary>
    /// 标签统计 DTO
    /// </summary>
    public record TagStatistic(string Name, int Count);

    /// <summary>
    /// 分类统计 DTO
    /// </summary>
    public record CategoryStatistic(string Name, int Count);
}
