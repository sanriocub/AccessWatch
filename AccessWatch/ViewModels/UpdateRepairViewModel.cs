using System.ComponentModel.DataAnnotations;
using AccessWatch.Models;
using Microsoft.AspNetCore.Http;

namespace AccessWatch.ViewModels
{
    public class UpdateRepairViewModel
    {
        public int ReportId { get; set; }

        public AccessibilityReport? Report { get; set; }

        [Required]
        public RepairProgress Progress { get; set; } = RepairProgress.NotStarted;

        [Required(ErrorMessage = "Corrective actions are required.")]
        [StringLength(1000)]
        public string CorrectiveActions { get; set; } = string.Empty;

        public IFormFile? CompletionEvidence { get; set; }

        public string? ExistingCompletionEvidenceUrl { get; set; }

        public bool MarkCompleted { get; set; }
    }
}
