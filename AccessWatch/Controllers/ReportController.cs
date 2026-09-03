using System.Net.Http.Json;
using System.Security.Claims;
using AccessWatch.Models;
using AccessWatch.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AccessWatch.Controllers
{
    [Authorize(Roles = "PersonWithDisability")]
    public class ReportController : Controller
    {
        private readonly AccessWatchDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<ReportController> _logger;

        // NEW API Gateway endpoint
        private const string AwsImageUploadEndpoint =
            "https://yd98kpymqg.execute-api.us-east-1.amazonaws.com/upload";

        public ReportController(
            AccessWatchDbContext context,
            IWebHostEnvironment env,
            IHttpClientFactory httpClientFactory,
            ILogger<ReportController> logger)
        {
            _context = context;
            _env = env;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
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
            {
                return View(model);
            }

            string? imageUrl = null;

            if (model.Image != null && model.Image.Length > 0)
            {
                // -------------------------------------------------
                // 1. Save the image locally
                // -------------------------------------------------

                var uploadsFolder = Path.Combine(
                    _env.WebRootPath,
                    "uploads");

                Directory.CreateDirectory(uploadsFolder);

                var fileName =
                    $"{Guid.NewGuid()}{Path.GetExtension(model.Image.FileName)}";

                var filePath = Path.Combine(
                    uploadsFolder,
                    fileName);

                using (var stream =
                    new FileStream(filePath, FileMode.Create))
                {
                    await model.Image.CopyToAsync(stream);
                }

                imageUrl = $"/uploads/{fileName}";

                // -------------------------------------------------
                // 2. Send the same image to AWS
                // AccessWatch -> API Gateway -> Lambda -> S3
                // -------------------------------------------------

                try
                {
                    await UploadImageToAwsAsync(
                        model.Image,
                        fileName);
                }
                catch (Exception ex)
                {
                    // Keep the original report submission working
                    // even if the AWS service is temporarily unavailable.
                    _logger.LogWarning(
                        ex,
                        "The report image was saved locally, but the AWS upload failed.");
                }
            }

            var report = new AccessibilityReport
            {
                SubmittedByUserId = GetCurrentUserId(),

                Description =
                    $"{model.Description}\n\nLocation: {model.Location}",

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
            var report = await _context.Reports
                .Include(r => r.Inspection)
                .Include(r => r.Repair)
                .FirstOrDefaultAsync(r => r.ReportId == id);

            if (report == null)
            {
                return NotFound();
            }

            if (report.SubmittedByUserId != GetCurrentUserId())
            {
                return Forbid();
            }

            return View(report);
        }

        // ---------------------------------------------------------
        // Upload image to AWS API Gateway
        // ---------------------------------------------------------
        private async Task UploadImageToAwsAsync(
            IFormFile image,
            string fileName)
        {
            using var memoryStream = new MemoryStream();

            await image.CopyToAsync(memoryStream);

            var fileBytes = memoryStream.ToArray();

            var base64File =
                Convert.ToBase64String(fileBytes);

            var requestBody = new
            {
                fileData = base64File,
                fileName = fileName,
                contentType = string.IsNullOrWhiteSpace(image.ContentType)
                    ? "application/octet-stream"
                    : image.ContentType
            };

            var client =
                _httpClientFactory.CreateClient();

            var response =
                await client.PostAsJsonAsync(
                    AwsImageUploadEndpoint,
                    requestBody);

            response.EnsureSuccessStatusCode();
        }

        // ---------------------------------------------------------
        // Get logged-in user's ID
        // ---------------------------------------------------------
        private int GetCurrentUserId()
        {
            var idClaim =
                User.FindFirstValue(
                    ClaimTypes.NameIdentifier);

            return int.Parse(idClaim!);
        }
    }
}