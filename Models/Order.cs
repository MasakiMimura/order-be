using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace OrderBE.Models
{
    [Table("order")]
    public class Order
    {
        [Key]
        [Column("order_id")]
        public int OrderId { get; set; }

        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }

        [Column("member_card_no")]
        [MaxLength(20)]
        public string? MemberCardNo { get; set; }

        [Required]
        [Column("total", TypeName = "decimal(10,2)")]
        public decimal Total { get; set; }

        [Required]
        [Column("status")]
        [MaxLength(16)]
        public string Status { get; set; } = string.Empty;

        [Column("confirmed")]
        public bool? Confirmed { get; set; }

        [Column("confirmed_at")]
        public DateTime? ConfirmedAt { get; set; }

        [Column("payment_method")]
        [MaxLength(20)]
        public string? PaymentMethod { get; set; }

        [Column("points_used")]
        [Precision(10, 2)]
        public decimal? PointsUsed { get; set; }

        [Column("member_new_balance")]
        [Precision(10, 2)]
        public decimal? MemberNewBalance { get; set; }

        [Column("paid_at")]
        public DateTime? PaidAt { get; set; }

        [Column("paid")]
        public bool? Paid { get; set; }

        public List<OrderItem> Items { get; set; } = new List<OrderItem>();
    }
}
