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
        public async Task<IActionResult> Index(string? department, string? search = null, string? filter = null)
        {
            if (_currentUserService.IsEndUser())
            {
                return Forbid();
            }

            var query = _context.AssetCategories.Where(c => c.IsActive).AsQueryable();

            if (_currentUserService.IsMaintenance())
            {
                var myTeam = _currentUserService.GetMaintenanceTeam() ?? MaintenanceTeams.IT;
                query = query.Where(c => c.Department == myTeam);
            }
            else if (_currentUserService.IsAdmin())
            {
                if (!string.IsNullOrWhiteSpace(department) && !string.Equals(department, "ALL", StringComparison.OrdinalIgnoreCase))
                {
                    query = query.Where(c => c.Department == department);
                }
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                query = query.Where(c => c.Name.ToLower().Contains(term) ||
                                         (c.NameAr != null && c.NameAr.ToLower().Contains(term)) ||
                                         (c.Description != null && c.Description.ToLower().Contains(term)));
            }

            if (string.Equals(filter, "with_assets", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(c => c.Assets.Any());
            }
            else if (string.Equals(filter, "empty", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(c => !c.Assets.Any());
            }

            var categories = await query
                .Include(c => c.Assets)
                .OrderBy(c => c.Department)
                .ThenBy(c => c.Name)
                .ToListAsync();

            ViewBag.SelectedDepartment = department;
            ViewBag.SearchTerm = search;
            ViewBag.SelectedFilter = filter;
            ViewBag.AvailableDepartments = MaintenanceTeams.All;
            ViewBag.IsAdmin = _currentUserService.IsAdmin();
            ViewBag.UserDepartment = _currentUserService.GetMaintenanceTeam() ?? MaintenanceTeams.IT;

            return View(categories);
        }

        // GET: AssetCategories/Create
        public IActionResult Create()
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
            return View(model);
        }

        // POST: AssetCategories/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AssetCategory model)
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

                _context.AssetCategories.Add(model);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Category '{model.Name}' created successfully for {model.Department}.";
                return RedirectToAction(nameof(Index), new { department = model.Department });
            }

            ViewBag.AvailableDepartments = MaintenanceTeams.All;
            return View(model);
        }

        // GET: AssetCategories/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            if (_currentUserService.IsEndUser()) return Forbid();

            var category = await _context.AssetCategories.FindAsync(id);
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
            return View(category);
        }

        // POST: AssetCategories/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, AssetCategory model)
        {
            if (_currentUserService.IsEndUser()) return Forbid();

            var category = await _context.AssetCategories.FindAsync(id);
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

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Category '{category.Name}' updated successfully.";
                return RedirectToAction(nameof(Index), new { department = category.Department });
            }

            ViewBag.AvailableDepartments = MaintenanceTeams.All;
            return View(model);
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
