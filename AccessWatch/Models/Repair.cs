using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AccessWatch.Models
{
    public enum RepairProgress
    {
        NotStarted,
        InProgress,
        Completed
    }

    public class Repair
    {
        [Key]
        public int RepairId { get; set; }

        [Required]
        public int ReportId { get; set; }

        [ForeignKey(nameof(ReportId))]
        public AccessibilityReport Report { get; set; }

        [Required]
        public int MaintenanceOfficerId { get; set; }

        [ForeignKey(nameof(MaintenanceOfficerId))]
        public User MaintenanceOfficer { get; set; }

        public RepairProgress Progress { get; set; } = RepairProgress.NotStarted;

        [StringLength(1000)]
        public string CorrectiveActions { get; set; }

        public string CompletionEvidenceUrl { get; set; }

        public bool IsCompleted { get; set; } = false;

        public DateTime? CompletedAt { get; set; }
    }
}