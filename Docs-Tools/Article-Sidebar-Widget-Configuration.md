# Article Sidebar Widget Configuration Guide

> [中文版本](文章页侧边栏组件配置说明.md) | English Version

## Feature Overview

NamBlog allows you to add custom HTML content below the category list in the article page navigation panel (right-side collapsible panel) to display QR codes, ads, links, and other personalized widgets.

## Use Cases

- � Third-party comment widgets (Giscus, Utterances, etc.)
- �💰 Donation/tip QR codes
- 📱 WeChat official account follow cards
- 📧 Email subscription entry
- 🔗 Friendly links
- 📺 Ad slots (iframe supported)
- 🎨 Any custom HTML content

## Configuration Methods

### Method 1: Configuration File (Recommended)

Add the `ArticleSidebarWidget` field in `data/config/config.json`:

```json
{
  "Blog": {
    "BlogName": "My Blog",
    "Blogger": "Blogger",
    "ArticleSidebarWidget": "<div style='text-align:center; padding:12px; background:rgba(243,244,246,0.5); border-radius:8px;'><h4 style='font-size:14px; margin:0 0 8px;'>❤️ Support</h4><img src='/images/donate-qrcode.png' style='width:120px; height:120px; border-radius:4px;' /><p style='font-size:12px; opacity:0.7; margin:8px 0 0;'>Thank you for your support</p></div>"
  }
}
```

### Method 2: Environment Variables (Docker Deployment)

Set environment variables when starting Docker Compose or container:

```yaml
# docker-compose.yml
services:
  namblog:
    environment:
      - Blog__ArticleSidebarWidget=<div style='text-align:center;'>...</div>
```

Or command line:

```bash
docker run -e 'Blog__ArticleSidebarWidget=<div>...</div>' ...
```

## Configuration Examples

> 💡 **Dark Mode Note**: All examples below use relative transparency or system-adaptable colors that work well with dark/light theme switching.

### 1. Simple Image QR Code

```json
{
  "ArticleSidebarWidget": "<img src='/images/qrcode.png' style='width:120px; display:block; margin:0 auto;' />"
}
```

### 2. Donation Card with Title (Dark Mode Friendly)

```json
{
  "ArticleSidebarWidget": "<div style='text-align:center; padding:16px; background:rgba(243,244,246,0.5); border-radius:8px;'><h4 style='font-size:14px; font-weight:600; margin:0 0 12px;'>❤️ Support</h4><img src='/images/donate.png' style='width:100px; height:100px; border-radius:4px; margin:0 auto;' /><p style='font-size:12px; opacity:0.7; margin:8px 0 0;'>If this article helps you<br>Feel free to donate</p></div>"
}
```

### 3. WeChat Official Account Follow

```json
{
  "ArticleSidebarWidget": "<div style='text-align:center; padding:14px; background:rgba(243,244,246,0.3); border:1px solid rgba(229,231,235,0.5); border-radius:8px; box-shadow:0 1px 3px rgba(0,0,0,0.05);'><h4 style='font-size:13px; font-weight:600; margin:0 0 10px;'>📱 Follow Us</h4><img src='/images/wechat-official.png' style='width:100px; height:100px; border-radius:4px; margin:0 auto 8px;' /><p style='font-size:11px; opacity:0.6; margin:0; line-height:1.5;'>Get latest article updates</p></div>"
}
```

### 4. Multiple Recommended Links (Related Reading)

```json
{
  "ArticleSidebarWidget": "<div style='padding:14px; background:rgba(243,244,246,0.4); border-radius:8px;'><h4 style='font-size:13px; font-weight:600; margin:0 0 10px; border-bottom:2px solid currentColor; padding-bottom:6px; opacity:0.9;'>📚 Recommended</h4><a href='/article/vue-tutorial' style='display:block; padding:8px 0; text-decoration:none; font-size:13px; border-bottom:1px solid rgba(229,231,235,0.5); opacity:0.85;'>→ Vue 3 Tutorial</a><a href='/article/react-hooks' style='display:block; padding:8px 0; text-decoration:none; font-size:13px; border-bottom:1px solid rgba(229,231,235,0.5); opacity:0.85;'>→ React Hooks Guide</a><a href='/article/typescript-guide' style='display:block; padding:8px 0; text-decoration:none; font-size:13px; opacity:0.85;'>→ TypeScript Practical Guide</a></div>"
}
```

### 5. Template Variables (Dynamic Article Info)

```json
{
  "ArticleSidebarWidget": "<div style='padding:12px; background:rgba(254,243,199,0.3); border-left:3px solid rgba(245,158,11,0.6); border-radius:4px;'><p style='font-size:12px; margin:0 0 6px; font-weight:600; opacity:0.8;'>📖 Reading</p><p style='font-size:13px; margin:0; font-weight:500; opacity:0.9;'>{{articleTitle}}</p><a href='/donate?article={{articleSlug}}' style='display:inline-block; margin-top:8px; padding:4px 12px; background:rgba(245,158,11,0.2); border-radius:4px; font-size:11px; text-decoration:none; opacity:0.85;'>Support This Article</a></div>"
}
```

### 6. Dual QR Codes (WeChat + Alipay)

```json
{
  "ArticleSidebarWidget": "<div style='text-align:center; padding:14px; background:rgba(249,250,251,0.5); border-radius:8px;'><h4 style='font-size:13px; font-weight:600; margin:0 0 10px;'>💰 Donate</h4><div style='display:flex; gap:10px; justify-content:center;'><div><img src='/images/wechat-pay.png' style='width:80px; height:80px; border-radius:4px;' /><p style='font-size:11px; opacity:0.7; margin:4px 0 0;'>WeChat</p></div><div><img src='/images/alipay.png' style='width:80px; height:80px; border-radius:4px;' /><p style='font-size:11px; opacity:0.7; margin:4px 0 0;'>Alipay</p></div></div></div>"
}
```

### 7. Email Subscription Entry

```json
{
  "ArticleSidebarWidget": "<div style='padding:14px; background:rgba(236,253,245,0.4); border:1px solid rgba(16,185,129,0.3); border-radius:8px;'><h4 style='font-size:13px; font-weight:600; margin:0 0 8px; opacity:0.9;'>📧 Subscribe</h4><p style='font-size:12px; margin:0 0 10px; line-height:1.5; opacity:0.8;'>Receive latest tech articles weekly</p><a href='mailto:subscribe@example.com?subject=Subscribe' style='display:block; text-align:center; padding:8px; background:rgba(16,185,129,0.2); border-radius:4px; font-size:12px; font-weight:500; text-decoration:none; opacity:0.9;'>Subscribe Now</a></div>"
}
```

### 8. Ad Slot (iframe)

```json
{
  "ArticleSidebarWidget": "<iframe src='https://ad-service.com/widget' style='width:100%; height:250px; border:0; display:block; border-radius:8px;'></iframe>"
}
```

### 9. Third-party Comment Widget (Giscus - GitHub Discussions)

```json
{
  "ArticleSidebarWidget": "<div style='padding:12px; background:rgba(243,244,246,0.4); border-radius:8px;'><h4 style='font-size:13px; font-weight:600; margin:0 0 10px; opacity:0.9;'>💬 Comments</h4><p style='font-size:12px; opacity:0.7; margin:0 0 8px; line-height:1.5;'>Join the discussion on GitHub Discussions</p><a href='https://github.com/your-repo/discussions' target='_blank' style='display:block; text-align:center; padding:8px; background:rgba(37,99,235,0.1); border-radius:4px; font-size:12px; text-decoration:none; opacity:0.85;'>👉 Comment</a></div>"
}
```

### 10. Comment Widget (Utterances - with Template Variables)

```json
{
  "ArticleSidebarWidget": "<div style='padding:12px; background:rgba(243,244,246,0.4); border-radius:8px;'><h4 style='font-size:13px; font-weight:600; margin:0 0 8px; opacity:0.9;'>💬 Discussion</h4><p style='font-size:12px; opacity:0.7; margin:0 0 8px;'>What do you think about \"{{articleTitle}}\"?</p><a href='https://github.com/your-repo/issues?q=is:issue+{{articleSlug}}' target='_blank' style='display:block; text-align:center; padding:8px; background:rgba(16,185,129,0.1); border-radius:4px; font-size:12px; text-decoration:none; opacity:0.85;'>Join Discussion</a></div>"
}
```

## Template Variables Support (Optional)

You can use article metadata variables in the configuration, which will be automatically replaced during rendering:

- `{{articleTitle}}` - Article title
- `{{articleSlug}}` - Article slug
- `{{articleId}}` - Article ID

**Example:**

```json
{
  "ArticleSidebarWidget": "<div style='padding:12px;'><p style='font-size:12px; opacity:0.7;'>Reading: <strong>{{articleTitle}}</strong></p><a href='/donate?article={{articleSlug}}' style='opacity:0.85;'>Support this article</a></div>"
}
```

## Style Recommendations

Since custom widgets are located in Shadow DOM, **inline styles must be used** for proper display.

### ✅ Recommended Style Approach (Dark Mode Friendly)

**Use transparency and rgba colors**:
```html
<div style="padding:12px; background:rgba(243,244,246,0.5); border-radius:8px;">
  <h4 style="font-size:14px; margin:0 0 8px; opacity:0.9;">Title</h4>
  <img src="/image.png" style="width:120px; display:block; margin:0 auto;" />
</div>
```

**Use currentColor and relative values**:
```html
<div style="padding:12px; border-bottom:2px solid currentColor; opacity:0.8;">
  <a href="/link" style="opacity:0.85; text-decoration:none;">Link text</a>
</div>
```

### ❌ Not Recommended Approach

```html
<!-- External CSS classes won't work -->
<div class="my-custom-class">
  <img src="/image.png" class="my-image" />
</div>

<!-- Hardcoded dark background, looks inconsistent in light mode -->
<div style="background:#1f2937; color:#fff;">
  <p>This will look jarring in light mode</p>
</div>
```

### 🎨 Dark Mode Best Practices

1. **Background Colors**: Use `rgba()` with transparency, e.g., `rgba(243,244,246,0.5)`
2. **Text Colors**: Use `opacity` to control transparency instead of hardcoded colors
3. **Borders**: Use `rgba()` or `currentColor`
4. **Links**: Rely on system default link colors (auto-adapts to theme) or use `opacity`
5. **Avoid**: Hardcoded `#fff` or `#000` absolute colors

## Built-in Defensive Styles

The system automatically provides the following protections, ensuring normal display even without styles:

- ✅ All elements `max-width: 100%` (prevents overflow)
- ✅ Images auto-center, auto-fit
- ✅ Iframe auto-fits container width
- ✅ Text auto-wraps
- ✅ Links default blue, underline on hover
- ✅ Auto-adapts to dark/light mode

## Responsive Design

Navigation panel width:
- Mobile: 256px
- Desktop: 300px

All content auto-fits panel width. Recommended:
- Image width no more than 120px
- iframes use percentage width (`width: 100%`)

## Configuration Details

### Hot Reload Support

Blog configuration supports hot reload. After modifying `config.json`, **no application restart needed**—just refresh the article page.

### Display Position

Custom widgets display below the category list in the article page navigation panel, separated by a divider line.

### Empty Configuration Handling

If not configured or configured as empty string, the custom widget area will **not display** and won't occupy space.

### Dark Mode Adaptation

Custom widgets automatically adapt to dark/light themes:
- Divider line color auto-switches
- Link color auto-adjusts
- Recommend using neutral background colors in configuration for dark mode compatibility

**Dark mode friendly configuration example:**

```json
{
  "ArticleSidebarWidget": "<div style='text-align:center; padding:12px; background:rgba(243, 244, 246, 0.5); border-radius:8px;'><img src='/donate.png' style='width:100px;' /></div>"
}
```

## Security

- Custom widgets are configured by administrators, stored only in server-side configuration files
- Frontend retrieves via GraphQL API and injects dynamically
- Content rendered in Shadow DOM, isolated from article content
- Recommend using only trusted content and third-party services

## Notes

1. **HTML Escaping**: When configuring in JSON, ensure proper escaping of double quotes or use single quotes
2. **Must Use Inline Styles**: External CSS classes won't work in Shadow DOM
3. **Image Paths**: Use relative paths (e.g., `/images/...`) or full URLs
4. **Script Support**: Currently mainly supports static HTML, images, links, and iframes
5. **Content Length**: Keep it concise to avoid affecting user experience

## Troubleshooting

### Issue: No display after configuration

1. Check if JSON format is correct
2. Confirm `ArticleSidebarWidget` field spelling is correct
3. Refresh article page (not homepage)
4. Check browser console for errors

### Issue: Styles not working

- Make sure to use **inline styles** (`style="..."`)
- Don't use external CSS classes

### Issue: Images not displaying

- Check if image path is correct

### Issue: Content overflow

- Check if fixed width is set (e.g., `width: 500px`)
- Use percentage width or rely on default `max-width: 100%`

## Complete Configuration Example

```json
{
  "Blog": {
    "BlogName": "My Tech Blog",
    "Blogger": "John Doe",
    "Slogan": "Sharing tech, recording life",
    "Domain": "https://example.com",
    "AnalyticsScript": "<script async src='...'></script>",
    "ArticleSidebarWidget": "<div style='text-align:center; padding:16px; background:rgba(249,250,251,0.5); border-radius:8px;'><h4 style='font-size:14px; font-weight:600; margin:0 0 12px;'>❤️ Support Author</h4><img src='/images/wechat-pay.png' style='width:100px; height:100px; border-radius:4px; margin:0 auto 8px;' /><p style='font-size:12px; opacity:0.7; margin:0;'>If the article helps you<br>Feel free to donate</p></div>"
  }
}
```

## Related Documentation

- [Analytics Script Configuration Guide](Analytics-Configuration.md)
- [Configuration System Guide](Configuration-Guide.md)
