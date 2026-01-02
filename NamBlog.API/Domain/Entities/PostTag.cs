using System;
using System.Collections.Generic;
using NamBlog.API.Domain.Specifications;

namespace NamBlog.API.Domain.Entities
{
    /// <summary>
    /// 标签实体
    /// 设计：独立的Tag表，与Post多对多关系，支持高效的标签查询和统计
    /// </summary>
    public class PostTag
    {
        #region ===================== 属性 =====================

        public int TagId { get; private set; }

        /// <summary>
        /// 标签名称（唯一）
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// 导航属性：使用此标签的文章列表
        /// 说明：集合类型在构造函数中初始化，避免NullReferenceException
        /// </summary>
        public ICollection<Post> Posts { get; private set; }

        #endregion

        #region ===================== 构造方法 =====================

        /// <summary>
        /// EF Core 需要的无参构造函数（private）
        /// </summary>
        private PostTag()
        {
            Name = string.Empty;
            Posts = [];
        }

        /// <summary>
        /// 私有构造函数，供静态工厂方法使用
        /// </summary>
        private PostTag(string name)
        {
            ValidateName(name);
            Name = name;
            Posts = [];
        }

        #endregion

        #region ===================== 工厂方法 =====================

        /// <summary>
        /// 创建新标签
        /// </summary>
        public static PostTag Create(string name)
        {
            return new PostTag(name);
        }

        #endregion

        #region ===================== 验证方法 =====================

        private static void ValidateName(string name)
        {
            if (!ValidationRuleset.Tag.IsValid(name))
                throw new ArgumentException(
                    ValidationRuleset.Tag.GetValidationError(name, "Tag name"),
                    nameof(name));
        }

        #endregion

        #region ===================== 业务方法 =====================

        /// <summary>
        /// 检查是否为孤立Tag（没有关联任何文章）
        /// 说明：应用层可以定期清理孤立Tag以节省空间
        /// 示例：context.Tags.Where(t => !t.Posts.Any()).ToList()
        /// </summary>
        public bool IsOrphan() => Posts.Count == 0;

        #endregion
    }
}
