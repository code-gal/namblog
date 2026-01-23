# Configuration System Guide

> [中文版本](配置系统说明.md) | English Version

## Configuration Files Overview

NamBlog uses the following configuration files:

| Configuration File | Location | Purpose | Hot Reload |
|---------|------|------|--------|
| **config.json** | `data/config/` | Blog basic configuration (blog info, AI, CORS, etc.) | Partial |
| **prompts.json** | `data/config/` | AI content generation system prompts and rules | ✅ Supported |
| **mcp-prompts.json** | `data/config/` | Prompt templates used by MCP clients | ✅ Supported |

**Frontend Language Configuration**:
- NamBlog supports bilingual Chinese and English interface. Custom language or create custom language packs via frontend config file
- See: [Language Configuration Guide](Language-Configuration.md)

**Auto-initialization Mechanism**:
- config.json needs to be customized beforehand. For the other two configurations, if the `data/config/` directory doesn't exist on first startup, the system will automatically copy template files from `wwwroot/config/`
- During Docker container deployment, configuration files are automatically mapped to the host's `./data/config/` directory for easy user customization

## Configuration Loading Order

Configurations are loaded in the following order, with later loaded configurations overriding earlier ones:

```
1. appsettings.json              (Default configuration)
2. appsettings.Development.json  (Development environment configuration)
3. data/config/config.json       (User custom configuration, Docker mount)
4. Environment variables         (Sensitive info: JWT Secret, API Key, etc.)
```

## Hot Reload Support

NamBlog supports **hot reload** for the following configurations (no application restart needed after modification):

| Configuration Item | Hot Reload Support | Description |
|--------|----------|------|
| **Blog** | ✅ Supported | Blog name, avatar, etc. take effect immediately |
| **AI** | ✅ Supported | Model, Temperature, etc. take effect immediately |
| **Cors** | ✅ Supported | CORS configuration takes effect immediately |
| **Logging** | ✅ Supported | Log level takes effect immediately |
| **FileWatcher** | ⚠️ Restart Needed | File monitoring configuration requires restart |
| **Seo** | ⚠️ Restart Needed | SEO crawler configuration requires restart |
| **Storage** | ❌ Not Configurable | Data directory path cannot be configured in config.json |
| **Admin** | ⚠️ Restart Needed | Admin account configuration requires restart |
| **Jwt** | ⚠️ Restart Needed | JWT configuration requires restart |
| **MCP** | ⚠️ Restart Needed | MCP configuration requires restart |

### Technical Notes

- **BlogSettings**: Uses `IOptionsSnapshot<T>`, updates on each GraphQL request
- **AISettings**: Uses `IOptionsMonitor<T>`, monitors configuration changes in real-time
- **Other Configurations**: Use `IOptions<T>`, bound at application startup, requires restart to update

## User Custom Configuration

### Configuration File Location

- **Template File**: `/wwwroot/config/config.json.template`
- **Actual Configuration**: `data/config/config.json` (needs manual creation)

### Usage

1. **Initial Configuration**:
   ```bash
   copy /wwwroot/config/config.json.template data/config/config.json
   ```

2. **Modify Configuration**:
   Edit `config.json`, keep only the configuration items you need to override

3. **Configuration Takes Effect**:
   - **Configurations supporting hot reload** (Blog, AI, Cors, Logging): Takes effect immediately after modification
   - **Configurations requiring restart** (Admin, Jwt, MCP, FileWatcher): Requires application restart after modification
   ```bash
   # Restart application (Windows PowerShell)
   # Press Ctrl+C in the terminal running the application, then:
   dotnet run
   
   # Docker environment
   docker restart <container>
   ```

### Configuration Examples

**Minimal Configuration** (only modify blog info):
```json
{
  "Blog": {
    "BlogName": "My Tech Blog",
    "Blogger": "John Doe",
    "Email": "john@example.com"
  }
}
```

**Complete Configuration** (override all configurable items):
```json
{
  "Blog": {
    "BlogName": "My Blog",
    "Blogger": "Blogger Name",
    "Icon": "/resources/icon/favicon.ico",
    "Email": "your-email@example.com",
    "Domain": "https://yourdomain.com",
    "Slogan": "This is my personal blog",
    "Avatar": "/resources/icon/avatar.jpg",
    "AnalyticsScript": "<script async defer data-website-id=\"your-website-id\" src=\"https://analytics.example.com/script.js\"></script>",
    "OuterChains": [
      {
        "Name": "GitHub",
        "Link": "https://github.com/yourusername",
        "SVG": "/resources/icon/github.svg"
      }
    ]
  },
  "AI": {
    "Model": "gpt-4",
    "Temperature": "0.7",
    "TimeoutSeconds": 600
  },
  "Cors": {
    "AllowedOrigins": "http://localhost:3000,https://yourdomain.com"
  }
}
```

## Important Configuration Items

### Items Not Configurable in config.json

The following configuration items can **only** be modified via `appsettings.json`, `appsettings.Local.json`, or **environment variables**:

- **Storage.DataRootPath**: Data directory path
  - Reason: Circular dependency exists (need to know data directory before loading config.json)
  - Modification method:
    ```bash
    # Windows (PowerShell)
    $env:Storage__DataRootPath = "D:\MyData"
    
    # Linux/Mac
    export Storage__DataRootPath=/var/namblog/data
    
    # Docker
    docker run -e Storage__DataRootPath=/app/data ...
    ```

### Static Resource Path Conventions

- **Icon paths**: Use `/resources/icon/...` (corresponds to `data/resources/icon/`)
- **Article HTML**: Accessed via `/posts/{slug}/v{version}/index.html`
- **Example**:
  ```json
  {
    "Blog": {
      "Icon": "/resources/icon/favicon.ico",
      "Avatar": "/resources/icon/my-avatar.png",
      "SVG": "/resources/icon/github.svg"
    }
  }
  ```

### Analytics Script Configuration

Support embedding third-party analytics scripts in footer (e.g., Umami, Google Analytics, etc.):

- **Configuration Item**: `Blog.AnalyticsScript`
- **Hot Reload**: ✅ Supported, takes effect immediately after modification
- **Detailed Guide**: See [Analytics Configuration Guide](Analytics-Configuration.md)
- **Example**:
  ```json
  {
    "Blog": {
      "AnalyticsScript": "<script async defer data-website-id=\"xxx\" src=\"https://analytics.example.com/script.js\"></script>"
    }
  }
  ```

### SEO Configuration

Control search engine optimization and crawler behavior.

| Configuration Item | Type | Description |
|-------------------|------|-------------|
| **BotUserAgents** | string[] | Crawler User-Agent keyword list (case-insensitive) |

**Features**:
- Automatically rewrite to static HTML when detecting User-Agents in the list accessing `/article/{slug}`
- Support search engine crawlers (Google, Bing, Baidu, etc.)
- Support AI crawlers (ChatGPT, Claude, Perplexity, etc.)
- Support social sharing preview (Facebook, Twitter, LinkedIn, etc.)
- Works with `/sitemap.xml` and `/robots.txt` to improve SEO

**Default Configuration**:
```json
{
  "Seo": {
    "BotUserAgents": [
      "googlebot", "bingbot", "baiduspider", "yandexbot",
      "gptbot", "claudebot", "perplexitybot",
      "facebookexternalhit", "twitterbot", "linkedinbot",
      ...
    ]
  }
}
```

**Hot Reload**: ⚠️ Requires application restart

**Auto-generated SEO Endpoints**:
- `/sitemap.xml` - Sitemap (includes all published articles)
- `/robots.txt` - Crawler rules (points to sitemap)

**How to Add New Crawlers**:
```json
{
  "Seo": {
    "BotUserAgents": [
      ...existing configuration,
      "new-bot-name"  // Add new crawler keyword
    ]
  }
}
```

### Frontend Configuration

Frontend configuration file: `./js/config.js`

#### Hidden Categories Configuration

Control which categories are not displayed in navigation bar and category list via `HIDDEN_CATEGORIES` constant:

- **Configuration Item**: `HIDDEN_CATEGORIES`
- **Default Value**: `['pages']`
- **Location**: `NamBlog.Web/js/config.js`
- **Description**:
  - Configured categories won't be displayed in navigation bar, category list, etc.
  - But articles in these categories can still be accessed via homepage list, tag pages, direct links, etc.
  - Suitable for special pages (e.g., "About", "Privacy Policy", etc.)

- **Example**:
  ```javascript
  // Hide multiple categories
  export const HIDDEN_CATEGORIES = ['pages', 'drafts', 'private'];
  ```


## FAQ

### Q1: config.json modifications not taking effect?
**A**:
- Check if configuration items are correct (JSON format, field name case)
- Check "Hot Reload Support" table to confirm if this configuration requires restart
- **Configurations requiring restart**: Admin, Jwt, MCP, FileWatcher
- **Configurations supporting hot reload**: Blog, AI, Cors, Logging
- Check application logs to confirm if configuration file was loaded

### Q2: Icon path 404?
**A**:
- Confirm file exists in `data/resources/icon/` directory
- Use path format: `/resources/icon/...`
- If no custom icons are provided, default icons will be automatically copied from `wwwroot/images/icon/` on first run

### Q3: How to reset to default configuration?
**A**:
- Delete `data/config/config.json`
- Restart application, system will use default configuration from `appsettings.json`

### Q4: How to configure during Docker deployment?
**A**:
- Create `./data/config/config.json` on host machine
- Mount to container via `-v ./data:/app/data`
- Use environment variables for sensitive info: `-e Jwt__Secret=...`

---

## Detailed Configuration File Guide

### prompts.json - AI Content Generation Configuration

`prompts.json` is one of the most important configuration files, used to control backend AI content generation behavior. It defines system prompts, resource recommendations, and validation rules.

**Configuration Location**:
- Template file: `wwwroot/config/prompts.json`
- Actual configuration: `data/config/prompts.json` (automatically copied on first startup)

**Hot Reload Support**: ✅ Fully supported, takes effect immediately after modification

#### 1. Markdown to HTML Configuration

**Core System Prompt (RootSystemPrompt)**:
- Defines how AI converts Markdown to HTML
- Includes format requirements, style principles, security rules, etc.
- ⚠️ **Modify with caution**: This is the core instruction for the entire conversion process

**Global User Prompt (UserGlobalPrompt)**:
- Supplement user's style preferences
- For example: "Use modern flat design style", "Responsive layout, mobile-first"
- Can be freely adjusted according to personal preferences

**External Resource Recommendations (Resources)**:
- CDN domain list (jsDelivr, Cloudflare, etc.)
- URLs and usage instructions for common libraries (Highlight.js, KaTeX, Mermaid, etc.)
- Each resource includes detailed usage conditions and initialization methods
- 💡 **Suggestion**: Download commonly used resources to `data/resources/` directory, let AI prioritize local resources

**HTML Validation (Validation)**:
- **Mode validation mode**:
  - `Strict`: Block scripts from untrusted domains
  - `Warning` (recommended): Log warnings but allow generation
  - `Permissive`: Don't check external scripts (not recommended for production)
- **CheckExternalScripts**: Whether to check external script resources
- **TrustedDomains**: Trusted CDN domain whitelist

#### 2. Metadata Auto-generation Configuration

When users don't provide title, slug, tags, or summary, AI will auto-generate based on these prompts:

- **TitlePrompt**: How to generate article title from Markdown content
- **SlugPrompt**: How to generate URL-friendly slug from title
- **TagsPrompt**: How to extract article-related tags (return JSON array)
- **ExcerptPrompt**: How to generate article summary

**Notes**:
- All prompts include strict length limits (refer to `Domain/Specifications/ValidationRuleset.cs`)
- Recommended to set smaller length limits to avoid generating overly long content
- Generated content must comply with backend validation rules, otherwise will be rejected

### mcp-prompts.json - MCP Tool Usage Guide

`mcp-prompts.json` provides prompt templates for MCP clients (such as Claude Desktop, Cherry Studio), guiding AI on how to combine blog management tools.

**Configuration Location**:
- Template file: `wwwroot/config/mcp-prompts.json`
- Actual configuration: `data/config/mcp-prompts.json` (automatically copied on first startup)

**Hot Reload Support**: ✅ Fully supported, takes effect immediately after modification

#### Built-in Prompt Templates

1. **Create Blog Article (create_article)**:
   - Guides AI on how to create a complete blog article
   - Includes steps for category selection, tag setting, HTML generation, etc.
   - Supports parameter: `topic` (article topic)

2. **Optimize Article Quality (optimize_article)**:
   - Provides article quality checklist
   - Guides AI to check title, content, metadata, etc.
   - Includes optimization suggestions and publishing process

3. **Article Metadata Guidelines (metadata_guidelines)**:
   - Explains validation rules and length limits for all fields
   - Avoid submitting data that doesn't meet specifications

4. **HTML Submission Guidelines (html_guidelines)**:
   - Security and style requirements when manually submitting HTML
   - External resource whitelist and validation rules
