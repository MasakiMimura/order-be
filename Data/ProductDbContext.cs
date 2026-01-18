using Microsoft.EntityFrameworkCore;
using OrderBE.Models;

namespace OrderBE.Data
{
    /// <summary>
    /// 商品サービス用のDbContext
    /// Product（商品）とCategory（カテゴリ）エンティティを管理
    /// </summary>
    public class ProductDbContext : DbContext
    {
        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="options">DbContextオプション</param>
        public ProductDbContext(DbContextOptions<ProductDbContext> options) : base(options)
        {
        }

        /// <summary>
        /// 商品テーブル
        /// </summary>
        public DbSet<Product> Products { get; set; }

        /// <summary>
        /// カテゴリテーブル
        /// </summary>
        public DbSet<Category> Categories { get; set; }

        /// <summary>
        /// モデル構成（リレーションシップ、インデックス等）
        /// </summary>
        /// <param name="modelBuilder">モデルビルダー</param>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Product と Category のリレーションシップ設定
            modelBuilder.Entity<Product>()
                .HasOne(p => p.Category)
                .WithMany(c => c.Products)
                .HasForeignKey(p => p.CategoryId);

            // Product インデックス
            modelBuilder.Entity<Product>()
                .HasIndex(p => p.CategoryId);

            modelBuilder.Entity<Product>()
                .HasIndex(p => p.IsActive);

            // Category インデックス
            modelBuilder.Entity<Category>()
                .HasIndex(c => c.CategoryName)
                .IsUnique();
        }
    }
}
