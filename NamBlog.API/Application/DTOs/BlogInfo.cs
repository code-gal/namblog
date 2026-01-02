using System.Collections.Generic;

namespace NamBlog.API.Application.DTOs
{
    /// <summary>
    /// 博客全局配置信息
    /// </summary>
    public record BlogInfo
    {
        /// <summary>
        /// 博客名称
        /// </summary>
        public string? BlogName { get; init; }

        /// <summary>
        /// 博主名称/昵称
        /// </summary>
        public string? Blogger { get; init; }

        /// <summary>
        /// 网站图标 (Favicon) 链接
        /// </summary>
        public string? Icon { get; init; }

        /// <summary>
        /// 联系邮箱
        /// </summary>
        public string? Email { get; init; }

        /// <summary>
        /// 博主个人头像链接
        /// </summary>
        public string? Avatar { get; init; }

        /// <summary>
        /// 博客绑定的主域名
        /// </summary>
        public string? Domain { get; init; }

        /// <summary>
        /// 首页显示的标语/签名
        /// </summary>
        public string? Slogan { get; init; }

        /// <summary>
        /// 首页底部的外部链接集合（如 Github, 知乎等）
        /// </summary>
        public List<OuterChain>? OuterChains { get; init; }
    }

    /// <summary>
    /// 外部链接项
    /// </summary>
    public record OuterChain
    {
        /// <summary>
        /// 链接名称（如 "Github"）
        /// </summary>
        public string? Name { get; init; }

        /// <summary>
        /// 跳转地址
        /// </summary>
        public string? Link { get; init; }

        /// <summary>
        /// 图标的 SVG 代码或路径
        /// </summary>
        public string? Svg { get; init; }
    }
}
