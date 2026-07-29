using AccessWatch.Models;
using AccessWatch.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AccessWatch.Controllers
{
    [Authorize(Roles = "PlatformAdministrator")]
    public class AdminController : Controller
    {
        private readonly AccessWatchDbContext _context;
        private readonly PasswordHasher<User> _passwordHasher = new();

        public AdminController(AccessWatchDbContext context)
        {
            _context = context;
        }

        // GET: /Admin  -- dashboard / monitor system activity
        public async Task<IActionResult> Index()
        {
            ViewBag.TotalUsers = await _context.Users.CountAsync();
            ViewBag.TotalReports = await _context.Reports.CountAsync();
            ViewBag.PendingReview = await _context.Reports.CountAsync(r => r.Status == ReportStatus.Submitted);
            ViewBag.InRepair = await _context.Reports.CountAsync(r => r.Status == ReportStatus.InRepair);
            ViewBag.Completed = await _context.Reports.CountAsync(r => r.Status == ReportStatus.Completed);

            var recentReports = await _context.Reports
                .Include(r => r.SubmittedBy)
                .OrderByDescending(r => r.SubmittedAt)
                .Take(10)
                .ToListAsync();

            return View(recentReports);
        }

        // ---------------- Manage user accounts ----------------

        // GET: /Admin/ManageUsers
        public async Task<IActionResult> ManageUsers()
        {
            var users = await _context.Users.OrderBy(u => u.Role).ThenBy(u => u.Name).ToListAsync();
            return View(users);
        }

        // GET: /Admin/CreateUser
        [HttpGet]
        public IActionResult CreateUser()
        {
            return View(new CreateUserViewModel());
        }

        // POST: /Admin/CreateUser
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateUser(CreateUserViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            bool emailTaken = await _context.Users.AnyAsync(u => u.Email == model.Email);
            if (emailTaken)
            {
                ModelState.AddModelError(nameof(model.Email), "An account with this email already exists.");
                return View(model);
            }

            var user = new User
            {
                Name = model.Name,
                Email = model.Email,
                Role = model.Role
            };
            user.PasswordHash = _passwordHasher.HashPassword(user, model.Password);

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Account created for {user.Name} ({user.Role}).";
            return RedirectToAction(nameof(ManageUsers));
        }

        // GET: /Admin/EditUser/5
        [HttpGet]
        public async Task<IActionResult> EditUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return NotFound();

            var model = new EditUserViewModel
            {
                UserId = user.UserId,
                Name = user.Name,
                Email = user.Email,
                Role = user.Role
            };

            return View(model);
        }

        // POST: /Admin/EditUser
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditUser(EditUserViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _context.Users.FindAsync(model.UserId);
            if (user == null)
                return NotFound();

            bool emailTaken = await _context.Users
                .AnyAsync(u => u.Email == model.Email && u.UserId != model.UserId);
            if (emailTaken)
            {
                ModelState.AddModelError(nameof(model.Email), "That email is already in use by another account.");
                return View(model);
            }

            // Stop an admin from demoting themselves out of the only admin account
            if (user.Role == UserRole.PlatformAdministrator &&
                model.Role != UserRole.PlatformAdministrator &&
                await _context.Users.CountAsync(u => u.Role == UserRole.PlatformAdministrator) <= 1)
            {
                TempData["Error"] = "Cannot change the role of the only remaining administrator.";
                return RedirectToAction(nameof(ManageUsers));
            }

            user.Name = model.Name;
            user.Email = model.Email;
            user.Role = model.Role;

            if (!string.IsNullOrWhiteSpace(model.NewPassword))
            {
                user.PasswordHash = _passwordHasher.HashPassword(user, model.NewPassword);
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = $"Updated account for {user.Name}.";
            return RedirectToAction(nameof(ManageUsers));
        }

        // POST: /Admin/DeleteUser/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return NotFound();

            if (user.Role == UserRole.PlatformAdministrator &&
                await _context.Users.CountAsync(u => u.Role == UserRole.PlatformAdministrator) <= 1)
            {
                TempData["Error"] = "Cannot delete the only remaining administrator account.";
                return RedirectToAction(nameof(ManageUsers));
            }

            bool hasReports = await _context.Reports.AnyAsync(r =>
                r.SubmittedByUserId == id || r.ReviewedByUserId == id || r.AssignedInspectorId == id);

            if (hasReports)
            {
                TempData["Error"] = $"Cannot delete {user.Name} — they have reports linked to their account.";
                return RedirectToAction(nameof(ManageUsers));
            }

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(ManageUsers));
        }

        // ---------------- Review / approve / assign reports ----------------

        // GET: /Admin/ReviewReports
        public async Task<IActionResult> ReviewReports()
        {
            var reports = await _context.Reports
                .Include(r => r.SubmittedBy)
                .Where(r => r.Status == ReportStatus.Submitted)
                .OrderBy(r => r.SubmittedAt)
                .ToListAsync();

            ViewBag.Inspectors = await _context.Users
                .Where(u => u.Role == UserRole.AccessibilityInspector)
                .ToListAsync();

            return View(reports);
        }

        // POST: /Admin/ApproveAndAssign
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveAndAssign(int reportId, int inspectorId)
        {
            var report = await _context.Reports.FindAsync(reportId);
            if (report == null)
                return NotFound();

            var adminId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);

            report.ReviewedByUserId = adminId;
            report.ReviewApproved = true;
            report.ReviewedAt = DateTime.UtcNow;
            report.AssignedInspectorId = inspectorId;
            report.AssignedAt = DateTime.UtcNow;
            report.Status = ReportStatus.Assigned;

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(ReviewReports));
        }

        // POST: /Admin/RejectReport
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectReport(int reportId)
        {
            var report = await _context.Reports.FindAsync(reportId);
            if (report == null)
                return NotFound();

            var adminId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);

            report.ReviewedByUserId = adminId;
            report.ReviewApproved = false;
            report.ReviewedAt = DateTime.UtcNow;
            report.Status = ReportStatus.Rejected;

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(ReviewReports));
        }

        // ---------------- Manage categories ----------------

        // GET: /Admin/ManageCategories
        public async Task<IActionResult> ManageCategories()
        {
            var categories = await _context.Categories.OrderBy(c => c.Name).ToListAsync();
            return View(categories);
        }

        // POST: /Admin/CreateCategory
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCategory(CategoryViewModel model)
        {
            if (!string.IsNullOrWhiteSpace(model.Name))
            {
                _context.Categories.Add(new Category { Name = model.Name, Description = model.Description });
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(ManageCategories));
        }

        // POST: /Admin/DeleteCategory/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category != null)
            {
                _context.Categories.Remove(category);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(ManageCategories));
        }
    }
}