using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MythNote.Web.Models;

[Table("note_tag")]
public class NoteTag
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("note_id")]
    public int NoteId { get; set; }

    [Column("tag_id")]
    public int TagId { get; set; }

    [Column("created_at")]
    public long CreatedAt { get; set; }

    [Column("updated_at")]
    public long UpdatedAt { get; set; }
        
        
    public Note Note { get; set; }
        
    public Tag Tag { get; set; }
}