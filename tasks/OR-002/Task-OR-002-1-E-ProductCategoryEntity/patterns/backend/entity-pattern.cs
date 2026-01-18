using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace {ProjectName}.Models
{
    /// <summary>
    /// {EntityDescription}
    /// データベーステーブル: {table_name}
    /// </summary>
    [Table("{table_name}")]
    public class {EntityName}
    {
        /// <summary>
        /// Primary Key: {EntityName}の一意識別子
        /// </summary>
        [Key]
        [Column("{id_column}")]
        public int {EntityName}Id { get; set; }

        /// <summary>
        /// {PropertyDescription}
        /// </summary>
        [Required]
        [Column("{property_column}")]
        public string {PropertyName} { get; set; }

        // Add additional properties based on database schema
        // Example:
        // [Column("{column_name}")]
        // public string PropertyName { get; set; }
        //
        // For nullable properties:
        // [Column("{column_name}")]
        // public int? NullableProperty { get; set; }
        //
        // For foreign key relationships:
        // [Column("{foreign_key_column}")]
        // public int ForeignKeyId { get; set; }
        // public {RelatedEntity} RelatedEntity { get; set; }
    }
}
