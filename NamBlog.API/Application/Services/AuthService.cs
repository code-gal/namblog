using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace NamBlog.API.Application.Services
{
    /// <summary>
    /// 认证服务 - 实现单管理员登录
    /// </summary>
    public class AuthService(IConfiguration config, ILogger<AuthService> logger)
    {
        private readonly IConfiguration _config = config;
        private readonly ILogger<AuthService> _logger = logger;

        /// <summary>
        /// 管理员登录
        /// </summary>
        /// <param name="username">用户名</param>
        /// <param name="password">密码（明文）</param>
        /// <returns>JWT Token，失败返回 null</returns>
        public string? Login(string username, string password)
        {
            // 输入验证
            if (string.IsNullOrWhiteSpace(username) || username.Length > 50)
            {
                _logger.LogWarning("登录失败：用户名格式无效");
                return null;
            }

            if (string.IsNullOrWhiteSpace(password) || password.Length > 100)
            {
                _logger.LogWarning("登录失败：密码格式无效");
                return null;
            }

            var adminUsername = _config["Admin:Username"];
            var adminPasswordHash = _config["Admin:PasswordHash"];

            if (string.IsNullOrEmpty(adminUsername) || string.IsNullOrEmpty(adminPasswordHash))
            {
                _logger.LogError("设置文件中的“admin配置”缺失了");
                return null;
            }

            // 验证用户名
            if (username != adminUsername)
            {
                _logger.LogWarning("登录失败：用户名错误 '{Username}'", username);
                return null;
            }

            // 验证密码（使用 BCrypt）
            try
            {
                if (!BCrypt.Net.BCrypt.Verify(password, adminPasswordHash))
                {
                    _logger.LogWarning("登录失败：密码错误，用户 '{Username}'", username);
                    return null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "验证密码哈希时发生错误");
                return null;
            }

            // 生成 JWT Token
            var token = GenerateJwtToken(username);
            _logger.LogInformation("用户 '{Username}' 登录成功", username);
            return token;
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
}
