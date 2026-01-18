using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OrderBE.Models
{
    /// <summary>
    /// カテゴリマスタエンティティ
    /// 商品カテゴリを管理するテーブル
    /// </summary>
    [Table("category")]
    public class Category
    {
        /// <summary>
        /// カテゴリID（主キー、自動採番）
        /// </summary>
        [Key]
        [Column("category_id")]
        public int CategoryId { get; set; }

        /// <summary>
        /// カテゴリ名（一意制約、必須）
        /// </summary>
        [Required]
        [Column("category_name")]
        [MaxLength(255)]
        public string CategoryName { get; set; } = string.Empty;

        /// <summary>
        /// 表示順序（デフォルト: 0）
        /// </summary>
        [Column("display_order")]
        public int DisplayOrder { get; set; } = 0;

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
        /// このカテゴリに属する商品のコレクション（1対多）
        /// </summary>
        public ICollection<Product> Products { get; set; } = new List<Product>();
    }
}
