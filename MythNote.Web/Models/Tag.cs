using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MythNote.Web.Models;

[Table("tag")]
public class Tag
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("parent_id")]
    public int ParentId { get; set; }

    [Column("ancestor_ids")]
    [Required]
    public string AncestorIds { get; set; }

    [Column("name")]
    [Required]
    [MaxLength(255)]
    public string Name { get; set; }

    [Column("fullname")]
    [Required]
    [MaxLength(100)]
    public string Fullname { get; set; }

    [Column("count")]
    public int Count { get; set; }

    [Column("created_at")]
    public long CreatedAt { get; set; }

    [Column("updated_at")]
    public long UpdatedAt { get; set; }
}