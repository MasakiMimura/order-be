using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OrderBE.Models
{
    /// <summary>
    /// 商品エンティティ
    /// 商品マスタを管理するテーブル
    /// </summary>
    [Table("product")]
    public class Product
    {
        /// <summary>
        /// 商品ID（主キー、自動採番）
        /// </summary>
        [Key]
        [Column("product_id")]
        public int ProductId { get; set; }

        /// <summary>
        /// 商品名（必須）
        /// </summary>
        [Required]
        [Column("product_name")]
        [MaxLength(255)]
        public string ProductName { get; set; } = string.Empty;

        /// <summary>
        /// レシピID（外部キー）
        /// </summary>
        [Column("recipe_id")]
        public int RecipeId { get; set; }

        /// <summary>
        /// カテゴリID（外部キー）
        /// </summary>
        [Column("category_id")]
        public int CategoryId { get; set; }

        /// <summary>
        /// 価格（0以上の整数）
        /// </summary>
        [Required]
        [Column("price")]
        public int Price { get; set; }

        /// <summary>
        /// キャンペーン対象フラグ（デフォルト: false）
        /// </summary>
        [Column("is_campaign")]
        public bool IsCampaign { get; set; } = false;

        /// <summary>
        /// キャンペーン割引率（0-100%、デフォルト: 0）
        /// </summary>
        [Column("campaign_discount_percent")]
        public int CampaignDiscountPercent { get; set; } = 0;

        /// <summary>
        /// 有効フラグ（デフォルト: true）
        /// </summary>
        [Column("is_active")]
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// 作成日時
        /// </summary>
        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// 更新日時
        /// </summary>
        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// 関連するカテゴリエンティティ（ナビゲーションプロパティ）
        /// </summary>
        [ForeignKey("CategoryId")]
        public Category? Category { get; set; }
    }
}
