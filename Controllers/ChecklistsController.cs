using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using StockPortal.Data;
using StockPortal.Models;
using StockPortal.Services;

namespace StockPortal.Controllers
{
    public class ChecklistsController : Controller
    {
        private readonly StockDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public ChecklistsController(StockDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        // GET: Checklists
        public async Task<IActionResult> Index(string? department = null, int? categoryId = null)
        {
            var query = _context.ChecklistForms
                .Include(c => c.Category)
                .AsQueryable();

            if (_currentUserService.IsMaintenance())
            {
                var myTeam = _currentUserService.GetMaintenanceTeam();
                query = query.Where(c => c.Department == myTeam);
                department = myTeam;
            }
            else if (!string.IsNullOrWhiteSpace(department))
            {
                query = query.Where(c => c.Department == department);
            }

            if (categoryId.HasValue)
            {
                query = query.Where(c => c.CategoryId == categoryId.Value);
            }

            ViewBag.SelectedDepartment = department;
            ViewBag.SelectedCategoryId = categoryId;
            ViewBag.Categories = await _context.AssetCategories.Where(c => c.IsActive).OrderBy(c => c.Name).ToListAsync();
            ViewBag.Departments = MaintenanceTeams.All;

            var list = await query.OrderByDescending(c => c.Id).ToListAsync();
            return View(list);
        }

        // GET: Checklists/Create
        public async Task<IActionResult> Create()
        {
            if (!_currentUserService.CanManageAssets()) return Forbid();

            string userDept = _currentUserService.GetMaintenanceTeam() ?? MaintenanceTeams.IT;
            ViewBag.Categories = new SelectList(await _context.AssetCategories.Where(c => c.IsActive && (_currentUserService.IsAdmin() || c.Department == userDept)).OrderBy(c => c.Name).ToListAsync(), "Id", "Name");
            ViewBag.Departments = MaintenanceTeams.All;

            return View(new ChecklistForm
            {
                Department = userDept,
                ChecklistType = "Periodic Maintenance",
                Items = new List<string>
                {
                    "Visual inspection of casing and chassis for physical damage",
                    "Power supply, battery backup, and ground wiring check",
                    "Clean ventilation grilles, cooling fans, and dust filters",
                    "Verify display, indicators, alarms, and operating buttons",
                    "Run internal self-test diagnostics & record firmware version"
                }
            });
        }

        // POST: Checklists/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ChecklistForm model, string[]? actionItems)
        {
            if (!_currentUserService.CanManageAssets()) return Forbid();

            if (_currentUserService.IsMaintenance())
            {
                model.Department = _currentUserService.GetMaintenanceTeam() ?? MaintenanceTeams.IT;
            }

            var cleanItems = (actionItems ?? Array.Empty<string>())
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s.Trim())
                .ToList();

            if (!cleanItems.Any())
            {
                ModelState.AddModelError("ItemsJson", "Please add at least one checklist action item.");
            }

            if (ModelState.IsValid)
            {
                model.Items = cleanItems;
                model.CreatedAt = DateTime.Now;
                model.IsActive = true;

                _context.ChecklistForms.Add(model);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Checklist template '{model.Title}' created successfully!";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Categories = new SelectList(await _context.AssetCategories.Where(c => c.IsActive && (_currentUserService.IsAdmin() || c.Department == model.Department)).OrderBy(c => c.Name).ToListAsync(), "Id", "Name", model.CategoryId);
            ViewBag.Departments = MaintenanceTeams.All;
            return View(model);
        }

        // GET: Checklists/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var checklist = await _context.ChecklistForms.FindAsync(id);
            if (checklist == null) return NotFound();

            if (_currentUserService.IsMaintenance() && checklist.Department != _currentUserService.GetMaintenanceTeam())
            {
                return Forbid();
            }

            ViewBag.Categories = new SelectList(await _context.AssetCategories.Where(c => c.IsActive && (_currentUserService.IsAdmin() || c.Department == checklist.Department)).OrderBy(c => c.Name).ToListAsync(), "Id", "Name", checklist.CategoryId);
            ViewBag.Departments = MaintenanceTeams.All;

            return View(checklist);
        }

        // POST: Checklists/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ChecklistForm model, string[]? actionItems)
        {
            if (id != model.Id) return NotFound();
            var existing = await _context.ChecklistForms.FindAsync(id);
            if (existing == null) return NotFound();

            if (_currentUserService.IsMaintenance() && existing.Department != _currentUserService.GetMaintenanceTeam())
            {
                return Forbid();
            }

            var cleanItems = (actionItems ?? Array.Empty<string>())
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s.Trim())
                .ToList();

            if (!cleanItems.Any())
            {
                ModelState.AddModelError("ItemsJson", "Please add at least one checklist action item.");
            }

            if (ModelState.IsValid)
            {
                existing.Title = model.Title;
                existing.CategoryId = model.CategoryId;
                existing.ChecklistType = model.ChecklistType;
                existing.IsActive = model.IsActive;
                if (_currentUserService.IsAdmin())
                {
                    existing.Department = model.Department;
                }
                existing.Items = cleanItems;

                _context.Update(existing);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Checklist template '{existing.Title}' updated successfully!";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Categories = new SelectList(await _context.AssetCategories.Where(c => c.IsActive && (_currentUserService.IsAdmin() || c.Department == existing.Department)).OrderBy(c => c.Name).ToListAsync(), "Id", "Name", existing.CategoryId);
            ViewBag.Departments = MaintenanceTeams.All;
            return View(model);
        }

        // POST: Checklists/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            if (!_currentUserService.CanManageAssets()) return Forbid();
            var checklist = await _context.ChecklistForms.FindAsync(id);
            if (checklist != null)
            {
                if (_currentUserService.IsMaintenance() && checklist.Department != _currentUserService.GetMaintenanceTeam())
                {
                    return Forbid();
                }

                _context.ChecklistForms.Remove(checklist);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Checklist '{checklist.Title}' was deleted.";
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Checklists/PrintChecklist (Task 8)
        [HttpGet]
        public async Task<IActionResult> PrintChecklist(int? id = null, int? categoryId = null, string? department = null, int[]? selectedAssetIds = null)
        {
            var checklistQuery = _context.ChecklistForms
                .Include(c => c.Category)
                .Where(c => c.IsActive)
                .AsQueryable();

            if (_currentUserService.IsMaintenance())
            {
                department = _currentUserService.GetMaintenanceTeam();
            }

            if (!string.IsNullOrWhiteSpace(department))
            {
                checklistQuery = checklistQuery.Where(c => c.Department == department);
            }

            ChecklistForm? activeChecklist = null;
            if (id.HasValue && id.Value > 0)
            {
                activeChecklist = await checklistQuery.FirstOrDefaultAsync(c => c.Id == id.Value);
            }
            if (activeChecklist == null && categoryId.HasValue)
            {
                activeChecklist = await checklistQuery.FirstOrDefaultAsync(c => c.CategoryId == categoryId.Value);
            }
            if (activeChecklist == null)
            {
                activeChecklist = await checklistQuery.FirstOrDefaultAsync();
            }

            if (activeChecklist == null)
            {
                // Fallback virtual default checklist if none created yet
                activeChecklist = new ChecklistForm
                {
                    Title = "Hospital Equipment Periodic Inspection Checklist",
                    Department = department ?? MaintenanceTeams.IT,
                    ChecklistType = "Periodic Maintenance",
                    Items = new List<string>
                    {
                        "External chassis, connectors, and structural integrity check",
                        "Power supply, cord, and electrical safety verification",
                        "Cleaning of vents, fans, filters, and operational components",
                        "Sensor / transducer / input responsiveness verification",
                        "Functional operation check, alarm trigger test, and sign-off"
                    }
                };
            }

            // Load candidate devices for this checklist's category or department
            var assetsQuery = _context.Assets
                .Include(a => a.Room)
                .Include(a => a.User)
                .Include(a => a.Category)
                .Where(a => a.Status != "Retired")
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(activeChecklist.Department))
            {
                assetsQuery = assetsQuery.Where(a => a.MaintenanceTeam == activeChecklist.Department);
            }
            if (activeChecklist.CategoryId.HasValue)
            {
                assetsQuery = assetsQuery.Where(a => a.CategoryId == activeChecklist.CategoryId.Value);
            }

            var allCandidateAssets = await assetsQuery.OrderBy(a => a.HospitalNo ?? a.Id.ToString()).ToListAsync();

            List<Asset> devicesToPrint;
            if (selectedAssetIds != null && selectedAssetIds.Length > 0)
            {
                devicesToPrint = allCandidateAssets.Where(a => selectedAssetIds.Contains(a.Id)).ToList();
            }
            else
            {
                devicesToPrint = allCandidateAssets.Take(15).ToList();
            }

            ViewBag.ActiveChecklist = activeChecklist;
            ViewBag.AllCandidateAssets = allCandidateAssets;
            ViewBag.AvailableChecklists = await _context.ChecklistForms.Where(c => c.IsActive && (string.IsNullOrEmpty(department) || c.Department == department)).ToListAsync();
            ViewBag.SelectedAssetIds = devicesToPrint.Select(d => d.Id).ToList();

            return View(devicesToPrint);
        }
    }
}
