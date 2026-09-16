using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using AccessWatch.Models;
using AccessWatch.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AccessWatch.Controllers
{
    [Authorize(Roles = "PersonWithDisability")]
    public class ReportController : Controller
    {
        private readonly AccessWatchDbContext _context;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<ReportController> _logger;

        // TODO: confirm the exact response JSON shape with Omer if this still fails after the URL fix
        private const string AwsImageUploadEndpoint =
            "https://xfbtheaieb.execute-api.us-east-1.amazonaws.com/upload";

        public ReportController(
            AccessWatchDbContext context,
            IHttpClientFactory httpClientFactory,
            ILogger<ReportController> logger)
        {
            _context = context;
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
                return View(model);

            string? imageUrl = null;

            if (model.Image != null && model.Image.Length > 0)
            {
                try
                {
                    imageUrl = await UploadImageToAwsAsync(model.Image);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Image upload to AWS failed for a new report submission.");
                    ModelState.AddModelError(string.Empty,
                        "We couldn't upload your image right now. Please try submitting again.");
                    return View(model);
                }
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
            var report = await _context.Reports
                .Include(r => r.Inspection)
                .Include(r => r.Repair)
                .FirstOrDefaultAsync(r => r.ReportId == id);

            if (report == null)
                return NotFound();

            if (report.SubmittedByUserId != GetCurrentUserId())
                return Forbid();

            return View(report);
        }

        // ---------------------------------------------------------
        // Upload image via API Gateway -> Lambda -> S3, returns
        // the S3 URL from the Lambda's response body.
        // ---------------------------------------------------------
        private async Task<string?> UploadImageToAwsAsync(IFormFile image)
        {
            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(image.FileName)}";

            using var memoryStream = new MemoryStream();
            await image.CopyToAsync(memoryStream);
            var base64File = Convert.ToBase64String(memoryStream.ToArray());

            var requestBody = new
            {
                fileData = base64File,
                fileName = fileName,
                contentType = string.IsNullOrWhiteSpace(image.ContentType)
                    ? "application/octet-stream"
                    : image.ContentType
            };

            var client = _httpClientFactory.CreateClient();

            var response = await client.PostAsJsonAsync(AwsImageUploadEndpoint, requestBody);
            response.EnsureSuccessStatusCode();

            // ASSUMPTION: Lambda returns JSON like { "url": "https://bucket.s3.amazonaws.com/..." }
            // Confirm this shape with Omer and adjust the property name below if different.
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var result = await response.Content.ReadFromJsonAsync<UploadResult>(options);

            if (result?.Bucket != null && result?.Key != null)
            {
                return $"https://{result.Bucket}.s3.us-east-1.amazonaws.com/{result.Key}";
            }
            return null;
        }

        private class UploadResult
        {
            public string? Bucket { get; set; }
            public string? Key { get; set; }
        }

        // ---------------------------------------------------------
        // Get logged-in user's ID
        // ---------------------------------------------------------
        private int GetCurrentUserId()
        {
            var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.Parse(idClaim!);
        }
    }
}