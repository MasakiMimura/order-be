using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using {ProjectName}.Data;

namespace {ProjectName}.Tests.Unit.{DomainName}
{
    /// <summary>
    /// カスタムWebApplicationFactory
    /// ContentRootを明示的に設定してWebApplicationFactoryの自動検出問題を回避
    /// InMemoryデータベースを使用してテストを高速化
    /// </summary>
    public class CustomWebApplicationFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            var projectDir = GetProjectPath();
            builder.UseContentRoot(projectDir);

            builder.ConfigureServices(services =>
            {
                // {DbContextName}に関連するすべての登録を削除
                var descriptorsToRemove = services
                    .Where(d => d.ServiceType == typeof(DbContextOptions<{DbContextName}>) ||
                                d.ServiceType == typeof({DbContextName}))
                    .ToList();

                foreach (var descriptor in descriptorsToRemove)
                {
                    services.Remove(descriptor);
                }

                // InMemoryデータベースを使用するDbContextOptionsを直接登録
                var options = new DbContextOptionsBuilder<{DbContextName}>()
                    .UseInMemoryDatabase("{TestDatabaseName}")
                    .Options;

                services.AddSingleton<DbContextOptions<{DbContextName}>>(options);
                services.AddScoped<{DbContextName}>();
            });
        }

        /// <summary>
        /// プロジェクトルートディレクトリを取得
        /// {ProjectFileName}.csproj ファイルを基準に検索
        /// </summary>
        private static string GetProjectPath()
        {
            var currentDirectory = Directory.GetCurrentDirectory();
            var directory = new DirectoryInfo(currentDirectory);

            while (directory != null && !File.Exists(Path.Combine(directory.FullName, "{ProjectFileName}.csproj")))
            {
                directory = directory.Parent;
            }

            if (directory == null)
            {
                throw new InvalidOperationException($"Could not find project root from {currentDirectory}");
            }

            return directory.FullName;
        }
    }
}
