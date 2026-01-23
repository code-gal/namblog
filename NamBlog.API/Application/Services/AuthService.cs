using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using NamBlog.API.Application.Common;

namespace NamBlog.API.Application.Services
{
    /// <summary>
    /// 认证服务 - 实现单管理员登录（集成登录限流）
    /// </summary>
    public class AuthService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<AuthService> _logger;
        private readonly IMemoryCache _cache;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly int _maxAttempts;
        private readonly int _lockoutMinutes;

        public AuthService(
            IConfiguration config,
            ILogger<AuthService> logger,
            IMemoryCache cache,
            IHttpContextAccessor httpContextAccessor)
        {
            _config = config;
            _logger = logger;
            _cache = cache;
            _httpContextAccessor = httpContextAccessor;
            _maxAttempts = int.Parse(config["RateLimit:MaxLoginAttempts"] ?? "5");
            _lockoutMinutes = int.Parse(config["RateLimit:LockoutMinutes"] ?? "5");
        }

        /// <summary>
        /// 管理员登录（集成限流）
        /// </summary>
        /// <param name="username">用户名</param>
        /// <param name="password">密码（明文）</param>
        /// <returns>(JWT Token, 限流错误信息)，认证失败时 Token 为 null</returns>
        public (string? Token, string? RateLimitError) Login(string username, string password)
        {
            // 获取客户端 IP
            var ipAddress = GetClientIpAddress();
            var cacheKey = CacheKeys.LoginRateLimit(ipAddress);

            // 检查是否被锁定
            if (_cache.TryGetValue(cacheKey, out LoginAttemptRecord? record) && record != null)
            {
                if (record.LockedUntil > DateTime.UtcNow)
                {
                    var remainingSeconds = (int)(record.LockedUntil - DateTime.UtcNow).TotalSeconds;
                    _logger.LogWarning("登录限流 - IP {IP} 处于锁定状态，剩余 {Seconds} 秒", ipAddress, remainingSeconds);
                    return (null, $"登录尝试次数过多，请 {remainingSeconds} 秒后重试");
                }

                // 锁定已过期，清除记录
                if (record.LockedUntil != default)
                {
                    _cache.Remove(cacheKey);
                }
            }

            // 输入验证
            if (string.IsNullOrWhiteSpace(username) || username.Length > 50)
            {
                _logger.LogWarning("登录失败：用户名格式无效");
                RecordFailure(ipAddress);
                return (null, null);
            }

            if (string.IsNullOrWhiteSpace(password) || password.Length > 100)
            {
                _logger.LogWarning("登录失败：密码格式无效");
                RecordFailure(ipAddress);
                return (null, null);
            }

            var adminUsername = _config["Admin:Username"];
            var adminPasswordHash = _config["Admin:PasswordHash"];

            if (string.IsNullOrEmpty(adminUsername) || string.IsNullOrEmpty(adminPasswordHash))
            {
                _logger.LogError("设置文件中的 admin 配置缺失了");
                return (null, null);
            }

            // 验证用户名
            if (username != adminUsername)
            {
                _logger.LogWarning("登录失败：用户名错误 '{Username}'", username);
                RecordFailure(ipAddress);
                return (null, null);
            }

            // 验证密码（使用 BCrypt）
            try
            {
                if (!BCrypt.Net.BCrypt.Verify(password, adminPasswordHash))
                {
                    _logger.LogWarning("登录失败：密码错误，用户 '{Username}'", username);
                    RecordFailure(ipAddress);
                    return (null, null);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "验证密码哈希时发生错误");
                RecordFailure(ipAddress);
                return (null, null);
            }

            // 登录成功：清除失败记录
            _cache.Remove(cacheKey);

            // 生成 JWT Token
            var token = GenerateJwtToken(username);
            _logger.LogInformation("用户 '{Username}' 登录成功，IP: {IP}", username, ipAddress);
            return (token, null);
        }

        /// <summary>
        /// 记录登录失败
        /// </summary>
        private void RecordFailure(string ipAddress)
        {
            var cacheKey = CacheKeys.LoginRateLimit(ipAddress);

            var record = _cache.GetOrCreate(cacheKey, entry =>
            {
                entry.SlidingExpiration = TimeSpan.FromMinutes(_lockoutMinutes + 5);
                return new LoginAttemptRecord();
            })!;

            record.FailedAttempts++;
            record.LastAttemptTime = DateTime.UtcNow;

            if (record.FailedAttempts >= _maxAttempts)
            {
                record.LockedUntil = DateTime.UtcNow.AddMinutes(_lockoutMinutes);
                _logger.LogWarning("登录限流 - IP {IP} 失败 {Count} 次，锁定 {Minutes} 分钟",
                    ipAddress, record.FailedAttempts, _lockoutMinutes);
            }

            _cache.Set(cacheKey, record, TimeSpan.FromMinutes(_lockoutMinutes + 5));
        }

        /// <summary>
        /// 获取客户端 IP 地址
        /// </summary>
        private string GetClientIpAddress()
        {
            var context = _httpContextAccessor.HttpContext;
            if (context == null)
                return "unknown";

            // 优先从 X-Forwarded-For 获取（反向代理场景）
            var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrEmpty(forwardedFor))
            {
                return forwardedFor.Split(',')[0].Trim();
            }

            return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        }

        /// <summary>
        /// 生成 JWT Token
        /// </summary>
        private string GenerateJwtToken(string username)
        {
            var secret = _config["Jwt:Secret"];
            var issuer = _config["Jwt:Issuer"];
            var audience = _config["Jwt:Audience"];
            var expirationMinutes = int.Parse(_config["Jwt:ExpirationMinutes"] ?? "1440");

            if (string.IsNullOrEmpty(secret) || secret.Length < 32)
            {
                // 配置错误应该在应用启动时抛出，这是合理的异常使用
                throw new InvalidOperationException(
                    "JWT Secret 配置错误：密钥必须至少 32 字符。请检查 config.json 中的 Jwt:Secret 配置。");
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.Name, username),
                new Claim(ClaimTypes.Role, "Admin"),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
            };

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(expirationMinutes),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }

    /// <summary>
    /// 登录尝试记录
    /// </summary>
    internal class LoginAttemptRecord
    {
        public int FailedAttempts { get; set; }
        public DateTime LastAttemptTime { get; set; }
        public DateTime LockedUntil { get; set; }
    }
}
