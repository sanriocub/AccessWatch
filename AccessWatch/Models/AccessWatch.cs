using Microsoft.EntityFrameworkCore;

namespace AccessWatch.Models
{
    public class AccessWatchDbContext : DbContext
    {
        public AccessWatchDbContext(DbContextOptions<AccessWatchDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<AccessibilityReport> Reports { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Facility> Facilities { get; set; }
        public DbSet<Inspection> Inspections { get; set; }
        public DbSet<Repair> Repairs { get; set; }
        public DbSet<Notification> Notifications { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // One report -> one inspection, one repair (each optional until that stage is reached)
            modelBuilder.Entity<AccessibilityReport>()
                .HasOne(r => r.Inspection)
                .WithOne(i => i.Report)
                .HasForeignKey<Inspection>(i => i.ReportId);

            modelBuilder.Entity<AccessibilityReport>()
                .HasOne(r => r.Repair)
                .WithOne(rep => rep.Report)
                .HasForeignKey<Repair>(rep => rep.ReportId);

            // Prevent cascade-delete chains across the four FK relationships on User
            modelBuilder.Entity<AccessibilityReport>()
                .HasOne(r => r.SubmittedBy)
                .WithMany()
                .HasForeignKey(r => r.SubmittedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<AccessibilityReport>()
                .HasOne(r => r.ReviewedBy)
                .WithMany()
                .HasForeignKey(r => r.ReviewedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<AccessibilityReport>()
                .HasOne(r => r.AssignedInspector)
                .WithMany()
                .HasForeignKey(r => r.AssignedInspectorId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}