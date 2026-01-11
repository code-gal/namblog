/**
 * 文章API模块
 * 封装所有文章相关的GraphQL操作
 */
import { request } from './client.js';

/**
 * 获取文章分类列表
 * 注意：编辑页需要显示所有分类（包括在导航栏隐藏的分类），以便用户可以选择
 */
export async function fetchCategories() {
    const query = `
        query GetCategories {
            blog {
                listCollection {
                    categorys {
                        name
                        count
                    }
                }
            }
        }
    `;
    const data = await request(query);
    // 返回所有分类，不进行过滤
    return data.blog.listCollection.categorys
        .map(c => c.name);
}

/**
 * 获取指定版本的HTML内容
 */
export async function getVersionHtml(id, versionName) {
    const query = `
        query GetVersionHtml($id: Int!, $versionName: String!) {
            blog {
                article {
                    getVersionHtml(id: $id, versionName: $versionName)
                }
            }
        }
    `;
    const data = await request(query, { id, versionName });
    return data.blog.article.getVersionHtml;
}

/**
 * 获取文章详情（用于编辑）
 */
export async function getArticleForEdit(slug) {
    const query = `
        query GetArticleForEdit($slug: String!) {
            blog {
                article {
                    article(slug: $slug) {
                        id
                        title
                        slug
                        category
                        markdown
                        mainVersionHtml
                        aiPrompts
                        isPublished
                        isFeatured
                        versions {
                            versionName
                            createdAt
                            aiPrompt
                        }
                    }
                }
            }
        }
    `;
    const data = await request(query, { slug });
    return data.blog.article.article;
}

/**
 * 保存文章（创建或更新元数据，不生成HTML版本）
 */
export async function saveArticle(input) {
    const query = `
        mutation SaveArticle($input: SaveArticleInput!) {
            blog {
                article {
                    saveArticle(input: $input) {
                        postId
                        title
                        slug
                        category
                        isPublished
                        isFeatured
                    }
                }
            }
        }
    `;
    const result = await request(query, { input });
    return result.blog.article.saveArticle;
}

/**
 * 提交文章（创建新版本，可能生成HTML）
 */
export async function submitArticle(input) {
    const query = `
        mutation SubmitArticle($input: SubmitArticleInput!) {
            blog {
                article {
                    submitArticle(input: $input) {
                        slug
                    }
                }
            }
        }
    `;
    const result = await request(query, { input });
    return result.blog.article.submitArticle;
}

/**
 * 删除指定版本（如果是最后一个版本则删除整篇文章）
 */
export async function deleteVersion(id, versionName) {
    const query = `
        mutation DeleteVersion($id: Int!, $versionName: String!) {
            blog {
                article {
                    deleteVersion(id: $id, versionName: $versionName)
                }
            }
        }
    `;
    const result = await request(query, { id, versionName });
    return result.blog.article.deleteVersion;
}

/**
 * 切换文章发布状态
 */
export async function togglePublish(id) {
    const query = `
        mutation TogglePublish($id: Int!) {
            blog {
                article {
                    togglePublish(id: $id) {
                        isPublished
                    }
                }
            }
        }
    `;
    const result = await request(query, { id });
    return result.blog.article.togglePublish;
}
