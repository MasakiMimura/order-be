using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OrderBE.Data;

namespace OrderBE.Tests.Unit.Product
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
                // ProductDbContextに関連するすべての登録を削除
                var descriptorsToRemove = services
                    .Where(d => d.ServiceType == typeof(DbContextOptions<ProductDbContext>) ||
                                d.ServiceType == typeof(ProductDbContext))
                    .ToList();

                foreach (var descriptor in descriptorsToRemove)
                {
                    services.Remove(descriptor);
                }

                // InMemoryデータベースを使用するDbContextOptionsを直接登録
                var options = new DbContextOptionsBuilder<ProductDbContext>()
                    .UseInMemoryDatabase($"ProductTestDb_{Guid.NewGuid()}")
                    .Options;

                services.AddSingleton<DbContextOptions<ProductDbContext>>(options);
                services.AddScoped<ProductDbContext>();
            });
        }

        /// <summary>
        /// プロジェクトルートディレクトリを取得
        /// OrderBE.csproj ファイルを基準に検索
        /// </summary>
        private static string GetProjectPath()
        {
            var currentDirectory = Directory.GetCurrentDirectory();
            var directory = new DirectoryInfo(currentDirectory);

            while (directory != null && !File.Exists(Path.Combine(directory.FullName, "OrderBE.csproj")))
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
