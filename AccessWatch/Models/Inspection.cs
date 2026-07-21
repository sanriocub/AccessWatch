using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AccessWatch.Models
{
    public class Inspection
    {
        [Key]
        public int InspectionId { get; set; }

        [Required]
        public int ReportId { get; set; }

        [ForeignKey(nameof(ReportId))]
        public AccessibilityReport Report { get; set; }

        [Required]
        public int InspectorId { get; set; }

        [ForeignKey(nameof(InspectorId))]
        public User Inspector { get; set; }

        [StringLength(1000)]
        public string Findings { get; set; }

        [Range(1, 5)]
        public int? AccessibilityRating { get; set; }

        [StringLength(1000)]
        public string RecommendedAction { get; set; }

        public bool ForwardedForMaintenance { get; set; } = false;

        public DateTime? InspectedAt { get; set; }
    }
}