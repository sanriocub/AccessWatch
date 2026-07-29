using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AccessWatch.Models
{
    public enum ReportStatus
    {
        Submitted = 0,
        UnderReview = 1,
        Rejected = 2,
        Assigned = 3,
        Inspected = 4,
        InRepair = 5,
        Completed = 6,
        InProgress = 7
    }

    public class AccessibilityReport
    {
        [Key]
        public int ReportId { get; set; }

        // --- Submitted by Person with Disability ---
        [Required]
        public int SubmittedByUserId { get; set; }

        [ForeignKey(nameof(SubmittedByUserId))]
        public User SubmittedBy { get; set; }

        [Required, StringLength(1000)]
        public string Description { get; set; }

        public int? FacilityId { get; set; }

        [ForeignKey(nameof(FacilityId))]
        public Facility Facility { get; set; }

        public int? CategoryId { get; set; }

        [ForeignKey(nameof(CategoryId))]
        public Category Category { get; set; }

        public string ImageUrl { get; set; }

        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

        public ReportStatus Status { get; set; } = ReportStatus.Submitted;

        // --- Reviewed by Platform Administrator ---
        public int? ReviewedByUserId { get; set; }

        [ForeignKey(nameof(ReviewedByUserId))]
        public User ReviewedBy { get; set; }

        public bool? ReviewApproved { get; set; }

        public DateTime? ReviewedAt { get; set; }

        // --- Assigned to Accessibility Inspector ---
        public int? AssignedInspectorId { get; set; }

        [ForeignKey(nameof(AssignedInspectorId))]
        public User AssignedInspector { get; set; }

        public DateTime? AssignedAt { get; set; }

        // --- Navigation to downstream records ---
        public Inspection Inspection { get; set; }
        public Repair Repair { get; set; }
    }
}
