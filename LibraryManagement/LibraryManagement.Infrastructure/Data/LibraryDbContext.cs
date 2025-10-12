using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using LibraryManagement.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace LibraryManagement.Infrastructure.Data
{
    public class LibraryDbContext : IdentityDbContext<ApplicationUser, IdentityRole, string>
    {
        public LibraryDbContext(DbContextOptions<LibraryDbContext> options) : base(options) { }

        public DbSet<Book> Books { get; set; }
        public DbSet<Library> Libraries { get; set; }
        public DbSet<BorrowRecord> BorrowRecords { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<ApplicationUser>(entity =>
            {

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(100);
                entity.Property(e => e.Phone)
                    .IsRequired()
                    .HasMaxLength(20);
                entity.Property(e => e.MembershipDate)
                    .IsRequired()
                    .HasDefaultValueSql("NOW()");
                entity.Property(e => e.IsActive)
                    .IsRequired()
                    .HasDefaultValue(true);

            });

            modelBuilder.Entity<Library>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(200);
                entity.Property(e => e.Location)
                    .IsRequired()
                    .HasMaxLength(100);
                entity.Property(e => e.Description)
                    .HasMaxLength(500);
                entity.HasIndex(e => e.Name)
                    .IsUnique();

                entity.Property(e => e.CreatedAt)
                    .IsRequired()
                    .HasDefaultValueSql("NOW()");
                entity.Property(e => e.UpdatedAt);
            });

            modelBuilder.Entity<Book>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Title)
                    .IsRequired()
                    .HasMaxLength(200);
                entity.Property(e => e.Author)
                    .IsRequired()
                    .HasMaxLength(100);
                entity.Property(e => e.ISBN)
                    .IsRequired()
                    .HasMaxLength(20);
                entity.Property(e => e.PublishedYear)
                    .IsRequired();
                entity.Property(e => e.TotalCopies)
                    .IsRequired();
                entity.Property(e => e.CopiesAvailable)
                    .IsRequired();

                entity.HasIndex(e => e.ISBN)
                    .IsUnique();

                entity.HasOne(b => b.Library)
                    .WithMany(l => l.Books)
                    .HasForeignKey(b => b.LibraryId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.Property(e => e.CreatedAt)
                    .IsRequired()
                    .HasDefaultValueSql("NOW()");
                entity.Property(e => e.UpdatedAt);
            });

            modelBuilder.Entity<BorrowRecord>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.BorrowDate)
                    .IsRequired()
                    .HasDefaultValueSql("NOW()");
                entity.Property(e => e.DueDate)
                    .IsRequired();
                entity.Property(e => e.ReturnDate);
                entity.Property(e => e.FineAmount)
                    .HasPrecision(10, 2);
                entity.Property(e => e.Notes)
                    .HasMaxLength(500);

                entity.HasOne(br => br.Book)
                    .WithMany(b => b.BorrowRecords)
                    .HasForeignKey(br => br.BookId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(br => br.User)
                    .WithMany(u => u.BorrowRecords)
                    .HasForeignKey(br => br.UserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.Property(e => e.CreatedAt)
                    .IsRequired()
                    .HasDefaultValueSql("NOW()");
                entity.Property(e => e.UpdatedAt);
            });

        }
    }
}