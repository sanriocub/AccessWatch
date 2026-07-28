using System.Security.Claims;
using AccessWatch.Models;
using AccessWatch.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AccessWatch.Controllers
{
    [Authorize]
    public class ReportController : Controller
    {
        private readonly AccessWatchDbContext _context;
        private readonly IWebHostEnvironment _env;

        public ReportController(AccessWatchDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // GET: /Report/MyReports
        public async Task<IActionResult> MyReports()
        {
            int userId = GetCurrentUserId();

            var reports = await _context.Reports
                .Where(r => r.SubmittedByUserId == userId)
                .OrderByDescending(r => r.SubmittedAt)
                .ToListAsync();

            return View(reports);
        }

        // GET: /Report/Submit
        [HttpGet]
        public IActionResult Submit()
        {
            return View(new SubmitReportViewModel());
        }

        // POST: /Report/Submit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Submit(SubmitReportViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            string? imageUrl = null;
            if (model.Image != null && model.Image.Length > 0)
            {
                var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads");
                Directory.CreateDirectory(uploadsFolder);

                var fileName = $"{Guid.NewGuid()}{Path.GetExtension(model.Image.FileName)}";
                var filePath = Path.Combine(uploadsFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await model.Image.CopyToAsync(stream);
                }

                imageUrl = $"/uploads/{fileName}";
            }

            var report = new AccessibilityReport
            {
                SubmittedByUserId = GetCurrentUserId(),
                Description = $"{model.Description}\n\nLocation: {model.Location}",
                ImageUrl = imageUrl,
                Status = ReportStatus.Submitted,
                SubmittedAt = DateTime.UtcNow
            };

            _context.Reports.Add(report);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(MyReports));
        }

        // GET: /Report/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var report = await _context.Reports.FirstOrDefaultAsync(r => r.ReportId == id);
            if (report == null)
                return NotFound();

            if (report.SubmittedByUserId != GetCurrentUserId())
                return Forbid();

            return View(report);
        }

        private int GetCurrentUserId()
        {
            var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.Parse(idClaim!);
        }
    }
}