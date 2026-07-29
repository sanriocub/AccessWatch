using System.Security.Claims;
using AccessWatch.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AccessWatch.Controllers
{
    [Authorize(Roles = "AccessibilityInspector")]
    public class InspectionController : Controller
    {
        private readonly AccessWatchDbContext _context;

        public InspectionController(AccessWatchDbContext context)
        {
            _context = context;
        }

        // Function 1: View Assigned Cases
        public async Task<IActionResult> Index()
        {
            var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(userIdValue, out int inspectorId))
            {
                return Forbid();
            }

            var assignedCases = await _context.Reports
                .Include(r => r.SubmittedBy)
                .Include(r => r.Facility)
                .Include(r => r.Category)
                .Where(r =>
                    r.AssignedInspectorId == inspectorId &&
                    (r.Status == ReportStatus.Assigned ||
                     r.Status == ReportStatus.InProgress))
                .OrderByDescending(r => r.AssignedAt)
                .ToListAsync();

            return View(assignedCases);
        }

        // Function 2: Update Inspection Status
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> StartInspection(int reportId)
        {
            var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(userIdValue, out int inspectorId))
            {
                return Forbid();
            }

            var report = await _context.Reports
                .FirstOrDefaultAsync(r =>
                    r.ReportId == reportId &&
                    r.AssignedInspectorId == inspectorId);

            if (report == null)
            {
                return NotFound();
            }

            if (report.Status == ReportStatus.Assigned)
            {
                report.Status = ReportStatus.InProgress;
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        // Functions 3-6: Display Inspection Form
        [HttpGet]
        public async Task<IActionResult> Findings(int id)
        {
            var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(userIdValue, out int inspectorId))
            {
                return Forbid();
            }

            var report = await _context.Reports
                .Include(r => r.SubmittedBy)
                .Include(r => r.Facility)
                .Include(r => r.Category)
                .Include(r => r.Inspection)
                .FirstOrDefaultAsync(r =>
                    r.ReportId == id &&
                    r.AssignedInspectorId == inspectorId &&
                    r.Status == ReportStatus.InProgress);

            if (report == null)
            {
                return NotFound();
            }

            return View(report);
        }

        // Functions 3-6: Submit Inspection
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitInspection(
            int reportId,
            string findings,
            int accessibilityRating,
            string recommendedAction,
            bool forwardedForMaintenance)
        {
            var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(userIdValue, out int inspectorId))
            {
                return Forbid();
            }

            var report = await _context.Reports
                .Include(r => r.Inspection)
                .FirstOrDefaultAsync(r =>
                    r.ReportId == reportId &&
                    r.AssignedInspectorId == inspectorId &&
                    r.Status == ReportStatus.InProgress);

            if (report == null)
            {
                return NotFound();
            }

            if (string.IsNullOrWhiteSpace(findings) ||
                string.IsNullOrWhiteSpace(recommendedAction) ||
                accessibilityRating < 1 ||
                accessibilityRating > 5)
            {
                TempData["Error"] =
                    "Please complete all inspection fields and provide a rating from 1 to 5.";

                return RedirectToAction(
                    nameof(Findings),
                    new { id = reportId });
            }

            var inspection = report.Inspection;

            if (inspection == null)
            {
                inspection = new Inspection
                {
                    ReportId = report.ReportId,
                    InspectorId = inspectorId
                };

                _context.Inspections.Add(inspection);
            }

            // Function 3: Submit Inspection Findings
            inspection.Findings = findings;

            // Function 4: Provide Accessibility Rating
            inspection.AccessibilityRating = accessibilityRating;

            // Function 5: Recommend Corrective Action
            inspection.RecommendedAction = recommendedAction;

            // Function 6: Forward Issue for Maintenance
            inspection.ForwardedForMaintenance = forwardedForMaintenance;

            inspection.InspectedAt = DateTime.UtcNow;

            report.Status = forwardedForMaintenance
                ? ReportStatus.InRepair
                : ReportStatus.Inspected;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Inspection submitted successfully.";

            return RedirectToAction(nameof(Index));
        }

        // View Inspection History
        [HttpGet]
        public async Task<IActionResult> History()
        {
            var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(userIdValue, out int inspectorId))
            {
                return Forbid();
            }

            var history = await _context.Inspections
                .Include(i => i.Report)
                    .ThenInclude(r => r.SubmittedBy)
                .Include(i => i.Report)
                    .ThenInclude(r => r.Facility)
                .Include(i => i.Report)
                    .ThenInclude(r => r.Category)
                .Where(i =>
                    i.InspectorId == inspectorId &&
                    i.InspectedAt != null)
                .OrderByDescending(i => i.InspectedAt)
                .ToListAsync();

            return View(history);
        }
    }
}