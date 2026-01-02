using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using NamBlog.API.Domain.Specifications;

namespace NamBlog.API.Domain.Entities
{
    /// <summary>
    /// 文章聚合根 (充血领域模型)
    /// 核心：一篇文章的本质是 Markdown 文档 + 由 AI 生成的属性和 HTML 版本
    /// </summary>
    public partial class Post
    {
        #region ===================== 属性 =====================
        public int PostId { get; private set; }

        /// <summary>
        /// 标题（唯一）
        /// </summary>
        public string? Title { get; private set; }

        /// <summary>
        /// URL 后缀标识（唯一）
        /// </summary>
        public string? Slug { get; private set; }

        /// <summary>
        /// Markdown 文件名（不含扩展名）
        /// </summary>
        public string FileName { get; private set; }

        /// <summary>
        /// Markdown 文件相对路径（相对于 markdown 根目录）
        /// </summary>
        public string FilePath { get; private set; }

        /// <summary>
        /// 作者名字
        /// </summary>
        public string Author { get; private set; }

        /// <summary>
        /// 摘要
        /// </summary>
        public string? Excerpt { get; private set; }

        /// <summary>
        /// 专栏/分类
        /// </summary>
        public string Category { get; private set; }

        /// <summary>
        /// 标签集合（导航属性）
        /// 说明：集合类型在构造函数中初始化，避免NullReferenceException，支持业务方法直接调用.Add()等
        /// </summary>
        public ICollection<PostTag> Tags { get; private set; }

        /// <summary>
        /// 版本集合（导航属性）
        /// 说明：集合类型在构造函数中初始化，避免NullReferenceException，支持业务方法直接调用.Add()等
        /// </summary>
        public ICollection<PostVersion> Versions { get; private set; }

        /// <summary>
        /// 主版本ID（外键，可为空）
        /// 说明：同时定义FK和导航属性是EF Core最佳实践，性能更好（可直接访问ID而不加载对象）
        /// </summary>
        public int? MainVersionId { get; private set; }

        /// <summary>
        /// 主版本（导航属性）
        /// 说明：提供对象引用，方便访问版本详细信息（如VersionName、AiPrompt等）
        /// </summary>
        public PostVersion? MainVersion { get; private set; }

        /// <summary>
        /// 是否公开（主版本）
        /// </summary>
        public bool IsPublished { get; private set; }

        /// <summary>
        /// 是否为精选文章（收藏）
        /// </summary>
        public bool IsFeatured { get; private set; }

        /// <summary>
        /// 发布时间
        /// </summary>
        public DateTimeOffset? PublishedAt { get; private set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTimeOffset CreateTime { get; private set; }

        /// <summary>
        /// 最后修改时间
        /// </summary>
        public DateTimeOffset LastModified { get; private set; }

        #endregion

        #region ===================== 构造方法（创建） =====================

        /// <summary>
        /// EF Core 需要的无参构造函数（private）
        /// </summary>
        private Post()
        {
            Title = string.Empty;
            Slug = string.Empty;
            FileName = string.Empty;
            FilePath = string.Empty;
            Author = "Anonymous";
            Category = "Unclassified";
            Tags = [];
            Versions = [];
            CreateTime = DateTimeOffset.UtcNow;
            LastModified = DateTimeOffset.UtcNow;
        }

        /// <summary>
        /// 私有构造函数，供静态工厂方法使用
        /// </summary>
        private Post(
            string fileName,
            string? filePath,
            string? author,
            string? category)
        {
            FileName = fileName;
            FilePath = filePath ?? string.Empty;
            Author = author ?? "Anonymous";
            Category = category ?? "Unclassified";
            Tags = [];
            Versions = [];
            CreateTime = DateTimeOffset.UtcNow;
            LastModified = DateTimeOffset.UtcNow;
        }

        //

        /// <summary>
        /// 静态工厂方法：从前端用户输入创建文章
        /// 核心业务：必须有 Markdown 才能创建文章
        /// </summary>
        /// <param name="fileName">Markdown文件名</param>
        /// <param name="filePath">Markdown文件路径</param>
        /// <param name="author">作者</param>
        /// <param name="category">分类</param>
        /// <returns>新的 Post 实体</returns>
        public static Post CreateFromUserInput(
            string fileName,
            string author,
            string? category)
        {
            return new Post(
                fileName: fileName,
                filePath: string.Empty,
                author: author,
                category: category
            );
        }

        /// <summary>
        /// 静态工厂方法：从后端文件系统监控创建文章
        /// </summary>
        public static Post CreateFromFileSystem(
            string fileName,
            string? filePath,
            string author)
        {
            return new Post(
                fileName: fileName,
                filePath: filePath,
                author: author,
                category: filePath ?? "Unclassified");
        }

        #endregion

        #region ===================== 业务方法（应用） =====================

        /// <summary>
        /// 业务方法1：更新元数据（不创建新版本）
        /// </summary>
        public void UpdateMetadata(
            string? title = null,
            string? slug = null,
            string? category = null,
            IEnumerable<PostTag>? tags = null,
            string? excerpt = null)
        {
            if (title is not null)
            {
                ValidateTitle(title);
                Title = title;
            }

            if (slug is not null)
            {
                ValidateSlug(slug);
                Slug = slug;
            }

            if (category is not null)
            {
                ValidateCategory(category);
                Category = category;
            }

            if (tags is not null)
            {
                ValidateTagCount(tags);
                Tags.Clear();
                foreach (var tag in tags)
                    Tags.Add(tag);
            }

            if (excerpt is not null)
            {
                ValidateExcerpt(excerpt);
                Excerpt = excerpt;
            }

            LastModified = DateTimeOffset.UtcNow;
        }

        /// <summary>
        /// 业务方法2：提交新版本
        /// 注意：这个方法不负责生成 HTML，它只负责创建版本记录
        /// HTML 生成是外部服务（IAIService）的职责
        /// </summary>
        /// <param name="aiPrompt">使用的 AI 提示词（可选）</param>
        /// <returns>新创建的 PostVersion 实体</returns>
        public PostVersion SubmitNewVersion(string? aiPrompt = null)
        {
            // 创建新版本实体（PostVersionId 由 EF Core 在 SaveChanges 时分配）
            var newVersion = PostVersion.Create(PostId, aiPrompt);

            // 将新版本添加到集合
            Versions.Add(newVersion);

            // 如果是第一个版本，自动设为主版本（但不改变发布状态）
            // 注意：此时 newVersion.PostVersionId 还是 0，需要在 SaveChanges 后才能获取
            // 但 EF Core 会自动处理导航属性的关联
            MainVersion ??= newVersion;

            LastModified = DateTimeOffset.UtcNow;

            return newVersion;
        }

        /// <summary>
        /// 业务方法3：删除指定版本
        /// 领域规则：
        /// 1. 如果只剩一个版本，抛出异常，应用层应删除整篇文章
        /// 2. 如果删除的是当前主版本，自动切换到最新版本并取消发布
        /// 说明：数据库配置为 SetNull，如果直接删除 PostVersion，MainVersionId 会自动置空
        /// </summary>
        public void RemoveVersion(PostVersion version)
        {
            if (!Versions.Contains(version))
            {
                throw new InvalidOperationException($"Version '{version.VersionName}' does not exist.");
            }

            // 先检查：如果只剩一个版本，不允许删除（应用层应删除整篇文章）
            if (Versions.Count <= 1)
            {
                throw new InvalidOperationException("Cannot remove the last version. Please delete the entire post instead.");
            }

            // 如果删除的是当前主版本，先切换到其他最新版本并取消发布
            if (MainVersion == version)
            {
                IsPublished = false;
                // 找到除了当前版本外的最新版本
                MainVersion = Versions
                    .Where(v => v != version)
                    .OrderByDescending(v => v.CreatedAt)
                    .First(); // 此时肯定有其他版本，因为 Count > 1
            }

            // 最后从集合中移除
            Versions.Remove(version);

            LastModified = DateTimeOffset.UtcNow;
        }

        /// <summary>
        /// 业务方法4：发布文章（切换到指定版本并设为已发布）
        /// 领域规则：必须有版本才能发布
        /// </summary>
        public void Publish(PostVersion? version = null)
        {
            // 如果没有任何版本，不能发布
            if (Versions.Count == 0)
            {
                throw new InvalidOperationException("Cannot publish: post has no versions.");
            }

            // 如果指定了版本，验证版本是否存在并切换
            if (version is not null)
            {
                if (Versions.Contains(version))
                    MainVersion = version;
                else
                    throw new InvalidOperationException($"Cannot publish: version '{version.VersionName}' does not exist.");
            }
            else
            {
                // 如果没有指定版本，检查是否有主版本
                if (MainVersion is null)
                {
                    throw new InvalidOperationException("Cannot publish: no main version specified.");
                }
            }

            IsPublished = true;
            PublishedAt = DateTimeOffset.UtcNow;
            LastModified = DateTimeOffset.UtcNow;
        }

        /// <summary>
        /// 业务方法5：取消发布
        /// </summary>
        public void Unpublish()
        {
            IsPublished = false;
            LastModified = DateTimeOffset.UtcNow;
        }

        /// <summary>
        /// 业务方法6：切换主版本（不改变发布状态）
        /// </summary>
        public void SwitchPublishedVersion(PostVersion version)
        {
            if (!Versions.Contains(version))
            {
                throw new InvalidOperationException($"Version '{version.VersionName}' does not exist.");
            }

            MainVersion = version;
            LastModified = DateTimeOffset.UtcNow;
        }

        /// <summary>
        /// 业务方法7：设为精选
        /// 领域规则：必须有版本才能设为精选
        /// </summary>
        public void Feature()
        {
            if (Versions.Count == 0)
            {
                throw new InvalidOperationException("Cannot feature: post has no versions.");
            }

            IsFeatured = true;
            LastModified = DateTimeOffset.UtcNow;
        }

        /// <summary>
        /// 业务方法8：取消精选
        /// </summary>
        public void Unfeature()
        {
            IsFeatured = false;
            LastModified = DateTimeOffset.UtcNow;
        }

        /// <summary>
        /// 业务方法8.1：准备删除整篇文章
        /// 说明：删除Post前需要打破循环引用（Post.MainVersionId <-> PostVersion.PostId），
        /// 否则EF Core会抛出循环依赖错误。此方法将MainVersion置空，便于安全删除。
        /// </summary>
        public void PrepareForDeletion()
        {
            MainVersion = null;
            MainVersionId = null;
        }

        /// <summary>
        /// 业务方法9：应用 AI 生成的元数据（用于创建后的初始化）
        /// </summary>
        public void ApplyAiGeneratedMetadata(string? title, string? slug, string? filename, string? excerpt, IEnumerable<PostTag>? tags)
        {
            if (title is not null)
            {
                ValidateTitle(title);
                Title = title;
            }

            if (slug is not null)
            {
                ValidateSlug(slug);
                Slug = slug;
            }

            if (filename is not null)
            {
                ValidateFileName(filename);
                FileName = filename;
            }

            if (excerpt is not null)
            {
                ValidateExcerpt(excerpt);
                Excerpt = excerpt;
            }

            if (tags is not null)
            {
                ValidateTagCount(tags);
                Tags.Clear();
                foreach (var tag in tags)
                    Tags.Add(tag);
            }

            LastModified = DateTimeOffset.UtcNow;
        }

        /// <summary>
        /// 业务方法10：重命名文件（修改文件名和路径）
        /// 用于文件系统监控检测到文件重命名时同步更新
        /// </summary>
        public void RenameFile(string newFileName, string? newFilePath)
        {
            ValidateFileName(newFileName);

            FileName = newFileName;
            FilePath = newFilePath ?? string.Empty;

            // 自动更新分类（基于新路径）
            Category = string.IsNullOrEmpty(FilePath)
                ? "Unclassified"
                : FilePath.Split('/')[0];

            LastModified = DateTimeOffset.UtcNow;
        }

        #endregion

        #region ===================== 验证方法（规则） =====================

        private static void ValidateTitle(string title)
        {
            if (!ValidationRuleset.Post.Title.IsValid(title))
                throw new ArgumentException(
                    ValidationRuleset.Post.Title.GetValidationError(title, "Title"),
                    nameof(title));
        }

        private static void ValidateSlug(string slug)
        {
            if (!ValidationRuleset.Post.Slug.IsValid(slug))
                throw new ArgumentException(
                    ValidationRuleset.Post.Slug.GetValidationError(slug, "Slug"),
                    nameof(slug));
        }

        private static void ValidateCategory(string category)
        {
            if (!ValidationRuleset.Post.Category.IsValid(category))
                throw new ArgumentException(
                    ValidationRuleset.Post.Category.GetValidationError(category, "Category"),
                    nameof(category));
        }

        private static void ValidateExcerpt(string excerpt)
        {
            if (!ValidationRuleset.Post.Excerpt.IsValid(excerpt))
                throw new ArgumentException(
                    ValidationRuleset.Post.Excerpt.GetValidationError(excerpt, "Excerpt"),
                    nameof(excerpt));
        }

        private static void ValidateTagCount(IEnumerable<PostTag> tags)
        {
            var tagArray = tags.Select(t => t.Name).ToArray();
            if (!ValidationRuleset.Post.Tags.IsValid(tagArray))
                throw new ArgumentException(
                    ValidationRuleset.Post.Tags.GetValidationError(tagArray, "Tags"),
                    nameof(tags));
        }

        private static void ValidateFileName(string fileName)
        {
            if (!ValidationRuleset.Post.FileName.IsValid(fileName))
                throw new ArgumentException(
                    ValidationRuleset.Post.FileName.GetValidationError(fileName, "FileName"),
                    nameof(fileName));

            // 额外的文件名特殊字符检查
            char[] invalidChars = ['<', '>', ':', '"', '/', '\\', '|', '?', '*'];
            if (fileName.IndexOfAny(invalidChars) >= 0)
            {
                throw new ArgumentException(
                    $"File names cannot contain the following characters: {string.Join(", ", invalidChars)}",
                    nameof(fileName));
            }
        }

        [System.Text.RegularExpressions.GeneratedRegex(@"^[a-z0-9-]+$")]
        private static partial System.Text.RegularExpressions.Regex SlugRegex();

        #endregion
    }
}

