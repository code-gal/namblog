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

        /// <summary>
        /// 网站统计脚本（如 Umami、Google Analytics 等），将在页脚动态注入
        /// </summary>
        public string? AnalyticsScript { get; init; }

        /// <summary>
        /// 文章页侧边栏自定义组件（如二维码、广告等），显示在导航面板分类列表下方
        /// 支持 HTML 内容（建议使用内联样式），配置为空时不显示
        /// </summary>
        public string? ArticleSidebarWidget { get; init; }
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
