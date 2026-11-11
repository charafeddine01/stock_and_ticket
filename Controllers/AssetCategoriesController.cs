using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockPortal.Data;
using StockPortal.Models;
using StockPortal.Services;

namespace StockPortal.Controllers
{
    public class AssetCategoriesController : Controller
    {
        private readonly StockDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public AssetCategoriesController(StockDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        // GET: AssetCategories
        public async Task<IActionResult> Index(string? department, string? search = null, string? filter = null, string? tab = "categories")
        {
            if (_currentUserService.IsEndUser())
            {
                return Forbid();
            }

            var activeTab = string.Equals(tab, "issues", StringComparison.OrdinalIgnoreCase) ? "issues" : "categories";

            // 1. Asset Categories Query
            var catQuery = _context.AssetCategories.Where(c => c.IsActive).AsQueryable();

            if (_currentUserService.IsMaintenance())
            {
                var myTeam = _currentUserService.GetMaintenanceTeam() ?? MaintenanceTeams.IT;
                catQuery = catQuery.Where(c => c.Department == myTeam);
            }
            else if (_currentUserService.IsAdmin())
            {
                if (!string.IsNullOrWhiteSpace(department) && !string.Equals(department, "ALL", StringComparison.OrdinalIgnoreCase))
                {
                    catQuery = catQuery.Where(c => c.Department == department);
                }
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                catQuery = catQuery.Where(c => c.Name.ToLower().Contains(term) ||
                                         (c.NameAr != null && c.NameAr.ToLower().Contains(term)) ||
                                         (c.Description != null && c.Description.ToLower().Contains(term)));
            }

            if (string.Equals(filter, "with_assets", StringComparison.OrdinalIgnoreCase))
            {
                catQuery = catQuery.Where(c => c.Assets.Any());
            }
            else if (string.Equals(filter, "empty", StringComparison.OrdinalIgnoreCase))
            {
                catQuery = catQuery.Where(c => !c.Assets.Any());
            }

            var categories = await catQuery
                .Include(c => c.Assets)
                .Include(c => c.IssueCategories)
                .OrderBy(c => c.Department)
                .ThenBy(c => c.Name)
                .ToListAsync();

            // 2. Issue Categories Query
            var issuesQuery = _context.IssueCategories.Where(i => i.IsActive).AsQueryable();
            if (_currentUserService.IsMaintenance())
            {
                var myTeam = _currentUserService.GetMaintenanceTeam() ?? MaintenanceTeams.IT;
                issuesQuery = issuesQuery.Where(i => i.Department == myTeam);
            }
            else if (_currentUserService.IsAdmin())
            {
                if (!string.IsNullOrWhiteSpace(department) && !string.Equals(department, "ALL", StringComparison.OrdinalIgnoreCase))
                {
                    issuesQuery = issuesQuery.Where(i => i.Department == department);
                }
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                issuesQuery = issuesQuery.Where(i => i.Name.ToLower().Contains(term) ||
                                                     (i.NameAr != null && i.NameAr.ToLower().Contains(term)) ||
                                                     (i.Description != null && i.Description.ToLower().Contains(term)));
            }

            var issues = await issuesQuery
                .Include(i => i.AssetCategories)
                .OrderBy(i => i.Department)
                .ThenBy(i => i.Name)
                .ToListAsync();

            ViewBag.ActiveTab = activeTab;
            ViewBag.IssueCategories = issues;
            ViewBag.AssetCategoriesCount = categories.Count;
            ViewBag.IssueCategoriesCount = issues.Count;
            ViewBag.AllAssetCategories = await _context.AssetCategories.Where(c => c.IsActive).OrderBy(c => c.Department).ThenBy(c => c.Name).ToListAsync();

            ViewBag.SelectedDepartment = department;
            ViewBag.SearchTerm = search;
            ViewBag.SelectedFilter = filter;
            ViewBag.AvailableDepartments = MaintenanceTeams.All;
            ViewBag.IsAdmin = _currentUserService.IsAdmin();
            ViewBag.UserDepartment = _currentUserService.GetMaintenanceTeam() ?? MaintenanceTeams.IT;

            return View(categories);
        }

        // GET: AssetCategories/Create
        public async Task<IActionResult> Create()
        {
            if (_currentUserService.IsEndUser()) return Forbid();

            var defaultDept = _currentUserService.IsMaintenance()
                ? (_currentUserService.GetMaintenanceTeam() ?? MaintenanceTeams.IT)
                : MaintenanceTeams.IT;

            var model = new AssetCategory
            {
                Department = defaultDept
            };

            ViewBag.AvailableDepartments = MaintenanceTeams.All;
            ViewBag.DepartmentIssues = await _context.IssueCategories
                .Where(i => i.IsActive && i.Department == defaultDept)
                .OrderBy(i => i.Name)
                .ToListAsync();
            ViewBag.AllIssues = await _context.IssueCategories
                .Where(i => i.IsActive)
                .OrderBy(i => i.Department)
                .ThenBy(i => i.Name)
                .ToListAsync();

            return View(model);
        }

        // POST: AssetCategories/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AssetCategory model, int[]? selectedIssueIds = null)
        {
            if (_currentUserService.IsEndUser()) return Forbid();

            if (_currentUserService.IsMaintenance())
            {
                model.Department = _currentUserService.GetMaintenanceTeam() ?? MaintenanceTeams.IT;
            }

            if (string.IsNullOrWhiteSpace(model.Name))
            {
                ModelState.AddModelError("Name", "Category Name is required.");
            }

            if (await _context.AssetCategories.AnyAsync(c => c.IsActive && c.Department == model.Department && c.Name.ToLower() == model.Name.Trim().ToLower()))
            {
                ModelState.AddModelError("Name", $"A category named '{model.Name}' already exists in {model.Department}.");
            }

            if (ModelState.IsValid)
            {
                model.Name = model.Name.Trim();
                model.NameAr = model.NameAr?.Trim();
                model.Description = model.Description?.Trim();
                model.CreatedAt = DateTime.Now;
                model.IsActive = true;

                if (selectedIssueIds != null && selectedIssueIds.Any())
                {
                    var idList = selectedIssueIds.ToList();
                    var selectedIssues = await _context.IssueCategories
                        .Where(i => idList.Contains(i.Id))
                        .ToListAsync();
                    foreach (var issue in selectedIssues)
                    {
                        model.IssueCategories.Add(issue);
                    }
                }

                _context.AssetCategories.Add(model);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Category '{model.Name}' created successfully for {model.Department} with {model.IssueCategories.Count} linked issues.";
                return RedirectToAction(nameof(Index), new { department = model.Department });
            }

            ViewBag.AvailableDepartments = MaintenanceTeams.All;
            ViewBag.DepartmentIssues = await _context.IssueCategories
                .Where(i => i.IsActive && i.Department == model.Department)
                .OrderBy(i => i.Name)
                .ToListAsync();
            ViewBag.AllIssues = await _context.IssueCategories
                .Where(i => i.IsActive)
                .OrderBy(i => i.Department)
                .ThenBy(i => i.Name)
                .ToListAsync();
            ViewBag.SelectedIssueIds = selectedIssueIds?.ToList() ?? new List<int>();

            return View(model);
        }

        // GET: AssetCategories/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            if (_currentUserService.IsEndUser()) return Forbid();

            var category = await _context.AssetCategories
                .Include(c => c.IssueCategories)
                .FirstOrDefaultAsync(c => c.Id == id);
            if (category == null || !category.IsActive) return NotFound();

            if (_currentUserService.IsMaintenance())
            {
                var myTeam = _currentUserService.GetMaintenanceTeam() ?? MaintenanceTeams.IT;
                if (!string.Equals(category.Department, myTeam, StringComparison.OrdinalIgnoreCase))
                {
                    return Forbid();
                }
            }

            ViewBag.AvailableDepartments = MaintenanceTeams.All;
            ViewBag.DepartmentIssues = await _context.IssueCategories
                .Where(i => i.IsActive && i.Department == category.Department)
                .OrderBy(i => i.Name)
                .ToListAsync();
            ViewBag.AllIssues = await _context.IssueCategories
                .Where(i => i.IsActive)
                .OrderBy(i => i.Department)
                .ThenBy(i => i.Name)
                .ToListAsync();
            ViewBag.SelectedIssueIds = category.IssueCategories.Select(i => i.Id).ToList();

            return View(category);
        }

        // POST: AssetCategories/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, AssetCategory model, int[]? selectedIssueIds = null)
        {
            if (_currentUserService.IsEndUser()) return Forbid();

            var category = await _context.AssetCategories
                .Include(c => c.IssueCategories)
                .FirstOrDefaultAsync(c => c.Id == id);
            if (category == null || !category.IsActive) return NotFound();

            if (_currentUserService.IsMaintenance())
            {
                var myTeam = _currentUserService.GetMaintenanceTeam() ?? MaintenanceTeams.IT;
                if (!string.Equals(category.Department, myTeam, StringComparison.OrdinalIgnoreCase))
                {
                    return Forbid();
                }
            }

            if (string.IsNullOrWhiteSpace(model.Name))
            {
                ModelState.AddModelError("Name", "Category Name is required.");
            }

            if (await _context.AssetCategories.AnyAsync(c => c.Id != id && c.IsActive && c.Department == category.Department && c.Name.ToLower() == model.Name.Trim().ToLower()))
            {
                ModelState.AddModelError("Name", $"Another category named '{model.Name}' already exists in {category.Department}.");
            }

            if (ModelState.IsValid)
            {
                category.Name = model.Name.Trim();
                category.NameAr = model.NameAr?.Trim();
                category.Description = model.Description?.Trim();
                if (_currentUserService.IsAdmin())
                {
                    category.Department = model.Department;
                }

                // Synchronize many-to-many IssueCategories
                category.IssueCategories.Clear();
                if (selectedIssueIds != null && selectedIssueIds.Any())
                {
                    var idList = selectedIssueIds.ToList();
                    var selectedIssues = await _context.IssueCategories
                        .Where(i => idList.Contains(i.Id))
                        .ToListAsync();
                    foreach (var issue in selectedIssues)
                    {
                        category.IssueCategories.Add(issue);
                    }
                }

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Category '{category.Name}' updated successfully with {category.IssueCategories.Count} linked issues.";
                return RedirectToAction(nameof(Index), new { department = category.Department });
            }

            ViewBag.AvailableDepartments = MaintenanceTeams.All;
            ViewBag.DepartmentIssues = await _context.IssueCategories
                .Where(i => i.IsActive && i.Department == category.Department)
                .OrderBy(i => i.Name)
                .ToListAsync();
            ViewBag.AllIssues = await _context.IssueCategories
                .Where(i => i.IsActive)
                .OrderBy(i => i.Department)
                .ThenBy(i => i.Name)
                .ToListAsync();
            ViewBag.SelectedIssueIds = selectedIssueIds?.ToList() ?? new List<int>();

            return View(model);
        }

        // ==========================================
        // Issue Categories CRUD Actions (Task 3)
        // ==========================================

        // POST: AssetCategories/CreateIssue
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateIssue(IssueCategory model, int[]? linkedAssetCategoryIds = null, string? returnTab = "issues")
        {
            if (_currentUserService.IsEndUser()) return Forbid();

            if (_currentUserService.IsMaintenance())
            {
                model.Department = _currentUserService.GetMaintenanceTeam() ?? MaintenanceTeams.IT;
            }

            if (string.IsNullOrWhiteSpace(model.Name))
            {
                TempData["ErrorMessage"] = "Issue Category Name is required.";
                return RedirectToAction(nameof(Index), new { tab = returnTab, department = model.Department });
            }

            if (await _context.IssueCategories.AnyAsync(i => i.IsActive && i.Department == model.Department && i.Name.ToLower() == model.Name.Trim().ToLower()))
            {
                TempData["ErrorMessage"] = $"An issue named '{model.Name}' already exists in {model.Department}.";
                return RedirectToAction(nameof(Index), new { tab = returnTab, department = model.Department });
            }

            model.Name = model.Name.Trim();
            model.NameAr = model.NameAr?.Trim();
            model.Description = model.Description?.Trim();
            model.CreatedAt = DateTime.Now;
            model.IsActive = true;

            if (linkedAssetCategoryIds != null && linkedAssetCategoryIds.Any())
            {
                var idList = linkedAssetCategoryIds.ToList();
                var linkedCats = await _context.AssetCategories
                    .Where(c => idList.Contains(c.Id))
                    .ToListAsync();
                foreach (var c in linkedCats)
                {
                    model.AssetCategories.Add(c);
                }
            }

            _context.IssueCategories.Add(model);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Issue Category '{model.Name}' created successfully for {model.Department}.";
            return RedirectToAction(nameof(Index), new { tab = returnTab, department = model.Department });
        }

        // POST: AssetCategories/EditIssue
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditIssue(int id, IssueCategory model, int[]? linkedAssetCategoryIds = null, string? returnTab = "issues")
        {
            if (_currentUserService.IsEndUser()) return Forbid();

            var issue = await _context.IssueCategories
                .Include(i => i.AssetCategories)
                .FirstOrDefaultAsync(i => i.Id == id);
            if (issue == null || !issue.IsActive) return NotFound();

            if (_currentUserService.IsMaintenance())
            {
                var myTeam = _currentUserService.GetMaintenanceTeam() ?? MaintenanceTeams.IT;
                if (!string.Equals(issue.Department, myTeam, StringComparison.OrdinalIgnoreCase))
                {
                    return Forbid();
                }
            }

            if (string.IsNullOrWhiteSpace(model.Name))
            {
                TempData["ErrorMessage"] = "Issue Category Name is required.";
                return RedirectToAction(nameof(Index), new { tab = returnTab, department = issue.Department });
            }

            issue.Name = model.Name.Trim();
            issue.NameAr = model.NameAr?.Trim();
            issue.Description = model.Description?.Trim();
            if (_currentUserService.IsAdmin() && !string.IsNullOrWhiteSpace(model.Department))
            {
                issue.Department = model.Department;
            }

            // Update linked asset categories
            issue.AssetCategories.Clear();
            if (linkedAssetCategoryIds != null && linkedAssetCategoryIds.Any())
            {
                var idList = linkedAssetCategoryIds.ToList();
                var linkedCats = await _context.AssetCategories
                    .Where(c => idList.Contains(c.Id))
                    .ToListAsync();
                foreach (var c in linkedCats)
                {
                    issue.AssetCategories.Add(c);
                }
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Issue Category '{issue.Name}' updated successfully.";
            return RedirectToAction(nameof(Index), new { tab = returnTab, department = issue.Department });
        }

        // POST: AssetCategories/DeleteIssue/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteIssue(int id, string? returnTab = "issues")
        {
            if (_currentUserService.IsEndUser()) return Forbid();

            var issue = await _context.IssueCategories.FindAsync(id);
            if (issue == null) return NotFound();

            if (_currentUserService.IsMaintenance())
            {
                var myTeam = _currentUserService.GetMaintenanceTeam() ?? MaintenanceTeams.IT;
                if (!string.Equals(issue.Department, myTeam, StringComparison.OrdinalIgnoreCase))
                {
                    return Forbid();
                }
            }

            issue.IsActive = false;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Issue Category '{issue.Name}' removed successfully.";
            return RedirectToAction(nameof(Index), new { tab = returnTab, department = issue.Department });
        }

        // AJAX GET: AssetCategories/GetDepartmentIssues?department=IT
        [HttpGet]
        public async Task<IActionResult> GetDepartmentIssues(string? department)
        {
            var dept = string.IsNullOrWhiteSpace(department) ? MaintenanceTeams.IT : department.Trim().ToUpper();
            var issues = await _context.IssueCategories
                .Where(i => i.IsActive && i.Department.ToUpper() == dept)
                .OrderBy(i => i.Name)
                .Select(i => new {
                    i.Id,
                    i.Name,
                    i.NameAr,
                    i.Department,
                    DisplayName = i.DisplayName
                })
                .ToListAsync();

            return Json(issues);
        }

        // POST: AssetCategories/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            if (_currentUserService.IsEndUser()) return Forbid();

            var category = await _context.AssetCategories.Include(c => c.Assets).FirstOrDefaultAsync(c => c.Id == id);
            if (category == null) return NotFound();

            if (_currentUserService.IsMaintenance())
            {
                var myTeam = _currentUserService.GetMaintenanceTeam() ?? MaintenanceTeams.IT;
                if (!string.Equals(category.Department, myTeam, StringComparison.OrdinalIgnoreCase))
                {
                    return Forbid();
                }
            }

            // Soft delete
            category.IsActive = false;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Category '{category.Name}' deleted.";
            return RedirectToAction(nameof(Index), new { department = category.Department });
        }

        // AJAX POST: AssetCategories/QuickCreate
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> QuickCreate(string name, string? nameAr, string department, string? description)
        {
            if (_currentUserService.IsEndUser()) return Json(new { success = false, message = "Access denied." });

            if (string.IsNullOrWhiteSpace(name))
            {
                return Json(new { success = false, message = "Category name is required." });
            }

            string dept = department;
            if (_currentUserService.IsMaintenance())
            {
                dept = _currentUserService.GetMaintenanceTeam() ?? MaintenanceTeams.IT;
            }

            var existing = await _context.AssetCategories.FirstOrDefaultAsync(c => c.IsActive && c.Department == dept && c.Name.ToLower() == name.Trim().ToLower());
            if (existing != null)
            {
                return Json(new { success = true, id = existing.Id, name = existing.Name, nameAr = existing.NameAr });
            }

            var cat = new AssetCategory
            {
                Name = name.Trim(),
                NameAr = nameAr?.Trim(),
                Department = dept,
                Description = description?.Trim(),
                CreatedAt = DateTime.Now,
                IsActive = true
            };

            _context.AssetCategories.Add(cat);
            await _context.SaveChangesAsync();

            return Json(new { success = true, id = cat.Id, name = cat.Name, nameAr = cat.NameAr });
        }

        // AJAX GET: AssetCategories/GetByDepartment
        [HttpGet]
        public async Task<IActionResult> GetByDepartment(string department)
        {
            var query = _context.AssetCategories.Where(c => c.IsActive);
            if (!string.IsNullOrWhiteSpace(department) && !string.Equals(department, "ALL", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(c => c.Department == department);
            }

            var list = await query
                .OrderBy(c => c.Name)
                .Select(c => new { c.Id, c.Name, c.NameAr, c.DisplayName, c.Department })
                .ToListAsync();

            return Json(list);
        }
    }
}
