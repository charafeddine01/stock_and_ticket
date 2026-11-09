using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockPortal.Data;
using StockPortal.Models;
using StockPortal.Services;

namespace StockPortal.Controllers
{
    public class SparePartFormsController : Controller
    {
        private readonly StockDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public SparePartFormsController(StockDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        // GET: SparePartForms
        public async Task<IActionResult> Index()
        {
            if (_currentUserService.IsEndUser())
            {
                return Forbid();
            }

            var query = _context.SparePartForms.AsQueryable();

            if (_currentUserService.IsMaintenance())
            {
                var myTeam = _currentUserService.GetMaintenanceTeam();
                query = query.Where(f => f.Department == myTeam);
            }

            var forms = await query.OrderBy(f => f.Department).ThenBy(f => f.Name).ToListAsync();
            return View(forms);
        }

        // GET: SparePartForms/Create
        public IActionResult Create()
        {
            if (_currentUserService.IsEndUser())
            {
                return Forbid();
            }

            var defaultDept = _currentUserService.IsMaintenance()
                ? (_currentUserService.GetMaintenanceTeam() ?? MaintenanceTeams.IT)
                : MaintenanceTeams.IT;

            var model = new SparePartForm
            {
                Department = defaultDept,
                Form = "[]"
            };

            ViewBag.AvailableTeams = MaintenanceTeams.All;
            return View(model);
        }

        // POST: SparePartForms/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(SparePartForm sparePartForm)
        {
            if (_currentUserService.IsEndUser())
            {
                return Forbid();
            }

            if (_currentUserService.IsMaintenance())
            {
                sparePartForm.Department = _currentUserService.GetMaintenanceTeam() ?? MaintenanceTeams.IT;
            }

            if (string.IsNullOrWhiteSpace(sparePartForm.Name))
            {
                ModelState.AddModelError("Name", "Form / Template Name is required.");
            }

            if (await _context.SparePartForms.AnyAsync(f => f.Name.ToLower() == sparePartForm.Name.Trim().ToLower() && f.Department == sparePartForm.Department))
            {
                ModelState.AddModelError("Name", $"A spare part form for '{sparePartForm.Name}' already exists in {sparePartForm.Department}.");
            }

            if (string.IsNullOrWhiteSpace(sparePartForm.Form) || sparePartForm.Form == "[]")
            {
                ModelState.AddModelError("Form", "Please add at least one specification field to this spare part form.");
            }

            if (ModelState.IsValid)
            {
                sparePartForm.Name = sparePartForm.Name.Trim();
                sparePartForm.CreatedAt = DateTime.Now;

                _context.SparePartForms.Add(sparePartForm);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Spare part form '{sparePartForm.Name}' created successfully for {sparePartForm.Department}.";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.AvailableTeams = MaintenanceTeams.All;
            return View(sparePartForm);
        }

        // GET: SparePartForms/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            if (_currentUserService.IsEndUser()) return Forbid();

            var form = await _context.SparePartForms.FindAsync(id);
            if (form == null) return NotFound();

            if (_currentUserService.IsMaintenance())
            {
                var myTeam = _currentUserService.GetMaintenanceTeam();
                if (!string.Equals(form.Department, myTeam, StringComparison.OrdinalIgnoreCase))
                {
                    return Forbid();
                }
            }

            ViewBag.AvailableTeams = MaintenanceTeams.All;
            return View(form);
        }

        // POST: SparePartForms/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, SparePartForm sparePartForm)
        {
            if (_currentUserService.IsEndUser()) return Forbid();

            var form = await _context.SparePartForms.FindAsync(id);
            if (form == null) return NotFound();

            if (_currentUserService.IsMaintenance())
            {
                var myTeam = _currentUserService.GetMaintenanceTeam();
                if (!string.Equals(form.Department, myTeam, StringComparison.OrdinalIgnoreCase))
                {
                    return Forbid();
                }
            }

            if (string.IsNullOrWhiteSpace(sparePartForm.Name))
            {
                ModelState.AddModelError("Name", "Form / Template Name is required.");
            }

            if (await _context.SparePartForms.AnyAsync(f => f.Id != id && f.Name.ToLower() == sparePartForm.Name.Trim().ToLower() && f.Department == form.Department))
            {
                ModelState.AddModelError("Name", $"Another form for '{sparePartForm.Name}' already exists in {form.Department}.");
            }

            if (string.IsNullOrWhiteSpace(sparePartForm.Form) || sparePartForm.Form == "[]")
            {
                ModelState.AddModelError("Form", "Please add at least one specification field to this spare part form.");
            }

            if (ModelState.IsValid)
            {
                form.Name = sparePartForm.Name.Trim();
                if (_currentUserService.IsAdmin())
                {
                    form.Department = sparePartForm.Department;
                }
                form.Form = sparePartForm.Form;

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Spare part form '{form.Name}' updated successfully.";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.AvailableTeams = MaintenanceTeams.All;
            return View(form);
        }

        // POST: SparePartForms/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            if (_currentUserService.IsEndUser()) return Forbid();

            var form = await _context.SparePartForms.Include(f => f.SpareParts).FirstOrDefaultAsync(f => f.Id == id);
            if (form == null) return NotFound();

            if (_currentUserService.IsMaintenance())
            {
                var myTeam = _currentUserService.GetMaintenanceTeam();
                if (!string.Equals(form.Department, myTeam, StringComparison.OrdinalIgnoreCase))
                {
                    return Forbid();
                }
            }

            _context.SparePartForms.Remove(form);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Spare part form '{form.Name}' deleted.";
            return RedirectToAction(nameof(Index));
        }

        // AJAX GET: SparePartForms/GetFormDetails/5
        [HttpGet]
        public async Task<IActionResult> GetFormDetails(int id)
        {
            var form = await _context.SparePartForms.FindAsync(id);
            if (form == null) return NotFound();

            return Json(new
            {
                id = form.Id,
                name = form.Name,
                department = form.Department,
                fields = form.Fields
            });
        }
    }
}
