using Microsoft.EntityFrameworkCore;

namespace MythNote.Web.Models
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Note> Notes { get; set; }
        public DbSet<Tag> Tags { get; set; }
        public DbSet<NoteTag> NoteTags { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Note>()
                .HasIndex(n => n.Path)
                .IsUnique();

            modelBuilder.Entity<Tag>()
                .HasIndex(t => t.Fullname)
                .IsUnique();
        }
    }
}
