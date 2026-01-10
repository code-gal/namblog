# Language Configuration Guide

> [中文版本](语言配置说明.md) | English Version

NamBlog supports bilingual Chinese and English interface, and allows bloggers to create custom language packs.

## Internationalization Scope

NamBlog's internationalization covers the following:

### Frontend Interface
- ✅ Navigation bar, buttons, form labels
- ✅ Article list, category, tag pages
- ✅ Login interface and editor interface
- ✅ Notification messages, error messages
- ✅ Common operation text (save, cancel, delete, etc.)

### Backend Responses
- ✅ API error messages and status information
- ✅ Validation error messages
- ✅ Operation success/failure feedback

### Currently Not Included
- ❌ User-created article content (managed by bloggers themselves)
- ❌ Development logs and debug information (kept in English)
- ❌ GraphQL Schema descriptions (developer tools)
- ❌ Prompts and MCP descriptions (used by AI)

## Default Behavior (Out of the Box)

**No configuration needed, the system will automatically:**

1. 🌐 **Auto-detect browser language**
   - Chinese browser detected → Display Chinese interface
   - Other languages detected → Display English interface

2. 💾 **Remember user choice**
   - After user switches language, the choice is saved in browser localStorage
   - Next visit automatically uses the last selected language

3. ✅ **No configuration file needed**
   - Works out of the box, suitable for most scenarios
   - Configuration only needed when forcing a specific language

**Default priority:**
1. Language preference in localStorage (if exists)
2. Browser language auto-detection
3. English (fallback)

---

## Supported Languages

### Built-in Languages
- `zh-CN`: Simplified Chinese
- `en-US`: English

### Custom Languages
Bloggers can create translation files for any language, for example:
- `ja-JP`: Japanese
- `fr-FR`: French
- `de-DE`: German
- etc.

## Language Selection Priority

When `config.js` is configured, the system determines the language in the following priority:

1. **LANGUAGE in config.js** - Force specific language (highest priority)
2. **CUSTOM_LOCALE_CODE in config.js** - Custom language pack
3. **localStorage.getItem('locale')** - User's last selected language
4. **Browser language detection** - `navigator.language` (auto-detect)
5. **English** - Default fallback

> 💡 **Tip**: If you don't configure `config.js`, the system defaults to priority 3 → 4 → 5

## Quick Configuration

### Method 1: Directly Edit config.js

Edit the configuration area at the top of `NamBlog.Web/js/config.js`:

```javascript
// ==================== Configuration Area (Can be modified in production) ====================

/**
 * Language settings
 * - 'zh-CN': Chinese
 * - 'en-US': English  
 * - null: Auto-detect browser language
 */
const LANGUAGE = 'en-US';  // Change to your desired language

/**
 * Custom language pack URL (optional)
 */
const CUSTOM_LOCALE_URL = null;
const CUSTOM_LOCALE_CODE = null;

// ==================== Configuration Area End ====================
```

### Method 2: Docker Mount config.js (Recommended for Production)

1. Copy `config.js` locally:
   ```bash
   cp NamBlog.Web/js/config.js /path/to/my-config.js
   ```

2. Modify configuration:
   ```javascript
   const LANGUAGE = 'en-US';  // Your language
   ```

3. Use docker-compose.yml:
   ```yaml
   services:
     namblog:
       image: namblog
       volumes:
         - ./my-config.js:/app/wwwroot/js/config.js:ro
   ```

## Creating Custom Language Packs

### Step 1: Create Language File

1. Copy an existing language file as template:
   ```bash
   # Use English as template (recommended)
   cp NamBlog.Web/js/i18n/locales/en-US.js \
      NamBlog.Web/js/i18n/locales/ja-JP.js
   
   # Or use Chinese as template
   cp NamBlog.Web/js/i18n/locales/zh-CN.js \
      NamBlog.Web/js/i18n/locales/ja-JP.js
   ```

2. Translate all text (keep keys unchanged, only change values):
   ```javascript
   export default {
       nav: {
           home: 'ホーム',        // Japanese: Home
           categories: 'カテゴリ', // Japanese: Categories
           // ... translate other keys
       },
       // ...
   };
   ```

### Step 2: Configure Custom Language

Edit `config.js`:

```javascript
const LANGUAGE = null;  // Leave empty, use custom language
const CUSTOM_LOCALE_URL = './js/i18n/locales/ja-JP.js';
const CUSTOM_LOCALE_CODE = 'ja-JP';
```

### Step 3: Deploy Custom Language with Docker

Use docker-compose.yml:

```yaml
services:
  namblog:
    image: namblog
    volumes:
      - ./my-config.js:/app/wwwroot/js/config.js:ro
      - ./my-locales/ja-JP.js:/app/wwwroot/js/i18n/locales/ja-JP.js:ro
```

## Configuration Examples

### Example 1: Force Chinese

```javascript
// config.js
const LANGUAGE = 'zh-CN';
const CUSTOM_LOCALE_URL = null;
const CUSTOM_LOCALE_CODE = null;
```

### Example 2: Force English

```javascript
// config.js
const LANGUAGE = 'en-US';
const CUSTOM_LOCALE_URL = null;
const CUSTOM_LOCALE_CODE = null;
```

### Example 3: Use Custom Japanese

```javascript
// config.js
const LANGUAGE = null;  // Leave empty, use custom language
const CUSTOM_LOCALE_URL = './js/i18n/locales/ja-JP.js';
const CUSTOM_LOCALE_CODE = 'ja-JP';
```

### Example 4: Auto-detect Browser Language

```javascript
// config.js
const LANGUAGE = null;  // Leave empty
const CUSTOM_LOCALE_URL = null;
const CUSTOM_LOCALE_CODE = null;
```

## Common Issues

### 1. Configuration not taking effect?

- Clear browser cache: `localStorage.clear()`
- Force refresh: Ctrl+F5 or Cmd+Shift+R
- Check config.js syntax
- View browser console errors

### 2. Custom language pack failing to load?

Check:
- File path is correct
- File format is ES6 module (export default)
- File encoding is UTF-8
- View console warning messages

### 3. Configuration not working after Docker deployment?

```bash
# Check file in container
docker exec -it <container> cat /app/wwwroot/js/config.js

# Check language file
docker exec -it <container> cat /app/wwwroot/js/i18n/locales/ja-JP.js

# View container logs
docker logs <container>
```

### 4. How to switch back to auto-detection?

```javascript
const LANGUAGE = null;
const CUSTOM_LOCALE_URL = null;
const CUSTOM_LOCALE_CODE = null;
```

### 5. Can multiple languages be supported simultaneously?

Current version only uses one language at a time. To support user language switching:
- Add language switcher button in navigation bar
- Call `setLocale('zh-CN')` or `setLocale('en-US')`
- User choice is saved in localStorage

## Translation File Structure

Custom language files must contain all the following nodes (refer to `en-US.js` or `zh-CN.js`):

```javascript
export default {
    nav: { ... },        // Navigation bar (15 keys)
    common: { ... },     // Common text (14 keys)
    auth: { ... },       // Authentication (13 keys)
    article: { ... },    // Article related (~30 keys)
    editor: { ... },     // Editor (~80 keys)
    pagination: { ... }, // Pagination (6 keys)
    search: { ... },     // Search (3 keys)
    errors: { ... }      // Error messages (5 keys)
};
```

**Important**:
- ✅ Keep all key names unchanged, only translate values
- ✅ Keep placeholders like `{count}`, `{page}` as is
- ✅ File must be valid JavaScript ES6 module
- ✅ Ensure file encoding is UTF-8
