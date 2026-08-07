using System.Security.Claims;
using AccessWatch.Models;
using AccessWatch.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AccessWatch.Controllers
{
    [Authorize(Roles = "FacilityMaintenanceOfficer")]
    public class FacilityController : Controller
    {
        private static readonly string[] AllowedImageExtensions = [".jpg", ".jpeg", ".png", ".webp"];
        private const long MaximumImageSize = 5 * 1024 * 1024;

        private readonly AccessWatchDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public FacilityController(AccessWatchDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        public async Task<IActionResult> Index()
        {
            if (!TryGetCurrentUserId(out int maintenanceOfficerId))
            {
                return Forbid();
            }

            var repairTasks = await _context.Reports
                .AsNoTracking()
                .Include(r => r.SubmittedBy)
                .Include(r => r.Facility)
                .Include(r => r.Category)
                .Include(r => r.Inspection)
                .Include(r => r.Repair)
                .Where(r =>
                    r.Status == ReportStatus.InRepair &&
                    r.Inspection != null &&
                    r.Inspection.ForwardedForMaintenance &&
                    (r.Repair == null || r.Repair.MaintenanceOfficerId == maintenanceOfficerId))
                .OrderByDescending(r => r.Inspection!.InspectedAt)
                .ToListAsync();

            return View(repairTasks);
        }

        // View previously completed repair tasks for the signed-in maintenance officer.
        [HttpGet]
        public async Task<IActionResult> History()
        {
            if (!TryGetCurrentUserId(out int maintenanceOfficerId))
            {
                return Forbid();
            }

            var history = await _context.Repairs
                .AsNoTracking()
                .Include(r => r.Report)
                    .ThenInclude(report => report.SubmittedBy)
                .Include(r => r.Report)
                    .ThenInclude(report => report.Facility)
                .Include(r => r.Report)
                    .ThenInclude(report => report.Category)
                .Include(r => r.Report)
                    .ThenInclude(report => report.Inspection)
                .Where(r =>
                    r.MaintenanceOfficerId == maintenanceOfficerId &&
                    (r.IsCompleted ||
                     r.CompletedAt != null ||
                     r.Progress == RepairProgress.Completed ||
                     r.Report.Status == ReportStatus.Completed))
                .OrderByDescending(r => r.CompletedAt)
                .ThenByDescending(r => r.RepairId)
                .ToListAsync();

            return View(history);
        }

        // Read-only details for an item in repair history.
        [HttpGet]
        public async Task<IActionResult> HistoryDetails(int id)
        {
            if (!TryGetCurrentUserId(out int maintenanceOfficerId))
            {
                return Forbid();
            }

            var repair = await _context.Repairs
                .AsNoTracking()
                .Include(r => r.Report)
                    .ThenInclude(report => report.SubmittedBy)
                .Include(r => r.Report)
                    .ThenInclude(report => report.Facility)
                .Include(r => r.Report)
                    .ThenInclude(report => report.Category)
                .Include(r => r.Report)
                    .ThenInclude(report => report.Inspection)
                .FirstOrDefaultAsync(r =>
                    r.RepairId == id &&
                    r.MaintenanceOfficerId == maintenanceOfficerId &&
                    (r.IsCompleted ||
                     r.CompletedAt != null ||
                     r.Progress == RepairProgress.Completed ||
                     r.Report.Status == ReportStatus.Completed));

            if (repair == null)
            {
                return NotFound();
            }

            return View(repair);
        }

        [HttpGet]
        public async Task<IActionResult> UpdateRepair(int id)
        {
            if (!TryGetCurrentUserId(out int maintenanceOfficerId))
            {
                return Forbid();
            }

            var report = await LoadRepairTaskAsync(id);

            if (report == null)
            {
                return NotFound();
            }

            if (report.Repair != null && report.Repair.MaintenanceOfficerId != maintenanceOfficerId)
            {
                return Forbid();
            }

            var model = new UpdateRepairViewModel
            {
                ReportId = report.ReportId,
                Report = report,
                Progress = report.Repair?.Progress ?? RepairProgress.NotStarted,
                CorrectiveActions = report.Repair?.CorrectiveActions ?? string.Empty,
                ExistingCompletionEvidenceUrl = report.Repair?.CompletionEvidenceUrl,
                MarkCompleted = false
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateRepair(UpdateRepairViewModel model)
        {
            if (!TryGetCurrentUserId(out int maintenanceOfficerId))
            {
                return Forbid();
            }

            var report = await LoadRepairTaskAsync(model.ReportId);

            if (report == null)
            {
                return NotFound();
            }

            if (report.Repair != null && report.Repair.MaintenanceOfficerId != maintenanceOfficerId)
            {
                return Forbid();
            }

            if (model.Progress == RepairProgress.Completed && !model.MarkCompleted)
            {
                ModelState.AddModelError(nameof(model.Progress), "Use the mark completed option to complete the repair task.");
            }

            if (model.CompletionEvidence != null)
            {
                ValidateCompletionEvidence(model.CompletionEvidence);
            }

            if (!ModelState.IsValid)
            {
                model.Report = report;
                model.ExistingCompletionEvidenceUrl = report.Repair?.CompletionEvidenceUrl;
                return View(model);
            }

            var repair = report.Repair;

            if (repair == null)
            {
                repair = new Repair
                {
                    ReportId = report.ReportId,
                    MaintenanceOfficerId = maintenanceOfficerId
                };

                _context.Repairs.Add(repair);
            }

            repair.Progress = model.MarkCompleted ? RepairProgress.Completed : model.Progress;
            repair.CorrectiveActions = model.CorrectiveActions.Trim();
            repair.IsCompleted = model.MarkCompleted;
            repair.CompletedAt = model.MarkCompleted ? DateTime.UtcNow : null;

            if (model.CompletionEvidence != null)
            {
                repair.CompletionEvidenceUrl = await SaveCompletionEvidenceAsync(model.CompletionEvidence);
            }

            if (model.MarkCompleted)
            {
                report.Status = ReportStatus.Completed;
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = model.MarkCompleted
                ? "The repair task was marked as completed."
                : "The repair progress was updated successfully.";

            return RedirectToAction(nameof(Index));
        }

        private async Task<AccessibilityReport?> LoadRepairTaskAsync(int reportId)
        {
            return await _context.Reports
                .Include(r => r.SubmittedBy)
                .Include(r => r.Facility)
                .Include(r => r.Category)
                .Include(r => r.Inspection)
                .Include(r => r.Repair)
                .FirstOrDefaultAsync(r =>
                    r.ReportId == reportId &&
                    r.Status == ReportStatus.InRepair &&
                    r.Inspection != null &&
                    r.Inspection.ForwardedForMaintenance);
        }

        private void ValidateCompletionEvidence(IFormFile file)
        {
            if (file.Length == 0)
            {
                ModelState.AddModelError(nameof(UpdateRepairViewModel.CompletionEvidence), "Select a valid image file.");
                return;
            }

            if (file.Length > MaximumImageSize)
            {
                ModelState.AddModelError(nameof(UpdateRepairViewModel.CompletionEvidence), "The image must not exceed 5 MB.");
            }

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedImageExtensions.Contains(extension))
            {
                ModelState.AddModelError(nameof(UpdateRepairViewModel.CompletionEvidence), "Only JPG, JPEG, PNG, and WEBP images are allowed.");
            }
        }

        private async Task<string> SaveCompletionEvidenceAsync(IFormFile file)
        {
            var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "repairs");
            Directory.CreateDirectory(uploadsFolder);

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            var fileName = $"{Guid.NewGuid()}{extension}";
            var filePath = Path.Combine(uploadsFolder, fileName);

            await using var stream = new FileStream(filePath, FileMode.Create);
            await file.CopyToAsync(stream);

            return $"/uploads/repairs/{fileName}";
        }

        private bool TryGetCurrentUserId(out int userId)
        {
            return int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out userId);
        }
    }
}
