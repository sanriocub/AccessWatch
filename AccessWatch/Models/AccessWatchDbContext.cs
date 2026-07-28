using Microsoft.AspNetCore.Identity;
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

            modelBuilder.Entity<AccessibilityReport>()
                .HasOne(r => r.Inspection)
                .WithOne(i => i.Report)
                .HasForeignKey<Inspection>(i => i.ReportId);

            modelBuilder.Entity<AccessibilityReport>()
                .HasOne(r => r.Repair)
                .WithOne(rep => rep.Report)
                .HasForeignKey<Repair>(rep => rep.ReportId);

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