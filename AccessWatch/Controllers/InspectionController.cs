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
                    r.Status == ReportStatus.Assigned)
                .OrderByDescending(r => r.AssignedAt)
                .ToListAsync();

            return View(assignedCases);
        }
    }
}