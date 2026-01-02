using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NamBlog.API.Infrastructure.Persistence;

namespace NamBlog.API.Extensions;

/// <summary>
/// 数据库服务注册扩展
/// </summary>
public static class DatabaseServiceExtensions
{
    /// <summary>
    /// 注册数据库服务（支持 SQLite 和 PostgreSQL）
    /// </summary>
    public static IServiceCollection AddDatabaseServices(this IServiceCollection services, IConfiguration configuration)
    {
        var dbProvider = configuration["DatabaseProvider"] ?? "SQLite";
        if (dbProvider == "SQLite")
        {
            var dataRootPath = configuration["Storage:DataRootPath"] ?? "./data";
            services.AddSqliteDatabase(dataRootPath);
        }
        else if (dbProvider == "PostgreSQL")
        {
            services.AddPostgreSqlDatabase(configuration);
        }

        // 数据库种子服务
        services.AddScoped<DatabaseSeeder>();

        return services;
    }
    /// <summary>
    /// 注册 SQLite 数据库
    /// </summary>
    private static IServiceCollection AddSqliteDatabase(this IServiceCollection services, string dataRootPath)
    {
        var dbPath = Path.Combine(dataRootPath, "namblog.db");
        var connectionString = $"Data Source={dbPath}";

        services.AddDbContext<BlogContext>(options =>
            options.UseSqlite(connectionString, b => b.MigrationsAssembly("NamBlog.API"))
        );
        return services;
    }

    /// <summary>
    /// 注册 PostgreSQL 数据库
    /// </summary>
    private static IServiceCollection AddPostgreSqlDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<BlogContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("PostgreSQL"),
                o => { o.EnableRetryOnFailure(); o.MigrationsAssembly("NamBlog.API"); })
        );
        return services;
    }
}
