using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MythNote.Web.Models;

[Table("note")]
public class Note
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("path")]
    [Required]
    [MaxLength(50)]
    public string Path { get; set; }

    [Column("title")]
    [Required]
    [MaxLength(255)]
    public string Title { get; set; }

    [Column("body")]
    [Required]
    public string Body { get; set; }

    [Column("tags")]
    [MaxLength(255)]
    public string Tags { get; set; }

    [Column("deleted")]
    public bool Deleted { get; set; }

    public bool Pinned { get; set; }

    [Column("created")]
    [Required]
    public string Created { get; set; }

    [Column("modified")]
    [Required]
    public string Modified { get; set; }

    [Column("created_at")]
    public long CreatedAt { get; set; }

    [Column("updated_at")]
    public long UpdatedAt { get; set; }
}