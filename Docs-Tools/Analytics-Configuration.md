# Analytics Configuration Guide

> [中文版本](网站统计脚本配置说明.md) | English Version

## Overview

NamBlog supports dynamically embedding website analytics scripts (such as Umami, Google Analytics, Baidu Analytics, etc.) through configuration files, without modifying frontend code.

## Configuration Methods

### Method 1: Configuration File (Recommended)

Add `AnalyticsScript` field in `data/config/config.json`:

```json
{
  "Blog": {
    "BlogName": "My Blog",
    "Blogger": "Blogger Name",
    "AnalyticsScript": "<script async defer data-website-id=\"your-website-id\" src=\"https://analytics.yourdomain.com/script.js\"></script>"
  }
}
```

### Method 2: Environment Variable (Docker Deployment)

Set environment variable when starting Docker Compose or container:

```yaml
# docker-compose.yml
services:
  namblog:
    environment:
      - Blog__AnalyticsScript=<script async defer data-website-id="your-id" src="https://analytics.yourdomain.com/script.js"></script>
```

Or via command line:

```bash
docker run -e 'Blog__AnalyticsScript=<script async defer data-website-id="your-id" src="https://analytics.yourdomain.com/script.js"></script>' ...
```

## Supported Analytics Platforms

### 1. Umami (Recommended)

Umami is a lightweight, open-source, privacy-friendly website analytics tool.

```json
{
  "AnalyticsScript": "<script async defer data-website-id=\"your-website-id\" src=\"https://analytics.yourdomain.com/script.js\"></script>"
}
```

### 2. Google Analytics 4

```json
{
  "AnalyticsScript": "<script async src=\"https://www.googletagmanager.com/gtag/js?id=G-XXXXXXXXXX\"></script><script>window.dataLayer = window.dataLayer || [];function gtag(){dataLayer.push(arguments);}gtag('js', new Date());gtag('config', 'G-XXXXXXXXXX');</script>"
}
```

### 3. Baidu Analytics

```json
{
  "AnalyticsScript": "<script>var _hmt = _hmt || [];(function() {var hm = document.createElement(\"script\");hm.src = \"https://hm.baidu.com/hm.js?your-baidu-token\";var s = document.getElementsByTagName(\"script\")[0];s.parentNode.insertBefore(hm, s);})();</script>"
}
```

### 4. 51.la (No Code Analytics)

```json
{
  "AnalyticsScript": "<script charset=\"UTF-8\" id=\"LA_COLLECT\" src=\"//sdk.51.la/js-sdk-pro.min.js\"></script><script>LA.init({id:\"your-51la-id\",ck:\"your-51la-key\"})</script>"
}
```

## Configuration Notes

### Hot Reload Support

Blog configuration supports hot reload. After modifying `config.json`, **no application restart needed**. Just refresh the frontend page to take effect.

### Security

- Analytics scripts are configured by administrators and stored only in server-side configuration files
- Frontend obtains scripts via GraphQL API and injects them dynamically
- Recommended to only use trusted analytics service providers

### Important Notes

1. **HTML Escaping**: When configuring in JSON, ensure proper escaping of double quotes or use single quotes
2. **Multiple Scripts**: To use multiple analytics platforms simultaneously, concatenate multiple `<script>` tags together
3. **Async Loading**: Recommended to use `async` or `defer` attributes for analytics scripts to avoid blocking page load
4. **Empty Value Handling**: Leave empty or don't configure this field to not load any analytics scripts

### Example: Multiple Analytics Scripts

```json
{
  "AnalyticsScript": "<script async defer data-website-id=\"umami-id\" src=\"https://analytics.example.com/script.js\"></script><script async src=\"https://www.googletagmanager.com/gtag/js?id=G-XXXXX\"></script>"
}
```

## Technical Implementation

### Backend

1. [BlogInfo.cs](../NamBlog.API/Application/DTOs/BlogInfo.cs#L48) - Add `AnalyticsScript` property
2. [BlogBasicType.cs](../NamBlog.API/EntryPoint/GraphiQL/Queries/BlogBasicType.cs#L61) - GraphQL Schema definition
3. Configuration file supports hot reload (`IOptionsSnapshot<BlogInfo>`)

### Frontend

1. [Footer.js](../NamBlog.Web/js/components/Footer.js) - GraphQL query to get script
2. Dynamically create `<script>` tag and inject to page bottom
3. Error tolerance handling, analytics script loading failure doesn't affect normal usage

## Verification

1. Modify configuration file to add analytics script
2. Refresh frontend page
3. Open browser developer tools (F12)
4. Check Network tab to confirm analytics script is loaded
5. Check Console tab to confirm no errors
6. Visit analytics platform to check if data is being reported

## FAQ

### Q: Configuration not taking effect?

A: Check the following:
- JSON format is correct (note escaping)
- Refresh frontend page (Ctrl+F5 for force refresh)
- Check browser console for errors
- Confirm analytics script URL is correct

### Q: Does it support environment variable configuration?

A: Yes, use double underscore separator: `Blog__AnalyticsScript`

### Q: How long does it take for configuration to take effect?

A: Blog configuration supports hot reload, takes effect immediately after modification, refresh frontend page.

### Q: Will analytics script affect page performance?

A: Using `async` or `defer` attributes avoids blocking page rendering, minimal impact on performance.
