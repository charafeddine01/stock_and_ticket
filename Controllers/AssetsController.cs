using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using StockPortal.Data;
using StockPortal.Models;
using StockPortal.ViewModels;
using StockPortal.Services;

namespace StockPortal.Controllers
{
    public class AssetsController : Controller
    {
        private readonly StockDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public AssetsController(StockDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        // GET: Assets
        public async Task<IActionResult> Index(string? search = null, string? status = null, string? type = null, int? roomId = null, string? warranty = null, string? calibration = null, int? categoryId = null)
        {
            var query = _context.Assets
                .Include(a => a.User)
                .Include(a => a.Room)
                .Include(a => a.Supplier)
                .Include(a => a.CustodyType)
                .Include(a => a.Category)
                .AsQueryable();

            // RBAC Guard: End users can view ONLY assets currently in their custody; Maintenance users view ONLY assets for their designated team
            if (_currentUserService.IsEndUser())
            {
                int currentUserId = _currentUserService.GetUserId();
                query = query.Where(a => a.UserId == currentUserId);
            }
            else if (_currentUserService.IsMaintenance())
            {
                var myTeam = _currentUserService.GetMaintenanceTeam();
                query = query.Where(a => a.MaintenanceTeam == myTeam);
            }

            if (categoryId.HasValue)
            {
                query = query.Where(a => a.CategoryId == categoryId.Value);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                query = query.Where(a =>
                    (a.HospitalNo != null && a.HospitalNo.ToLower().Contains(s)) ||
                    (a.SerialNo != null && a.SerialNo.ToLower().Contains(s)) ||
                    (a.Brand != null && a.Brand.ToLower().Contains(s)) ||
                    (a.Type != null && a.Type.ToLower().Contains(s)) ||
                    (a.User != null && a.User.FullName != null && a.User.FullName.ToLower().Contains(s))
                );
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(a => a.Status == status);
            }

            if (!string.IsNullOrWhiteSpace(type))
            {
                query = query.Where(a => a.Type == type);
            }

            if (roomId.HasValue)
            {
                query = query.Where(a => a.RoomId == roomId.Value);
            }

            if (!string.IsNullOrWhiteSpace(warranty))
            {
                var now = DateTime.Now.Date;
                if (warranty == "expired")
                {
                    query = query.Where(a => a.WarrantyTill.HasValue && a.WarrantyTill.Value < now);
                }
                else if (warranty == "expiring30")
                {
                    var future30 = now.AddDays(30);
                    query = query.Where(a => a.WarrantyTill.HasValue && a.WarrantyTill.Value >= now && a.WarrantyTill.Value <= future30);
                }
                else if (warranty == "active")
                {
                    query = query.Where(a => a.WarrantyTill.HasValue && a.WarrantyTill.Value >= now);
                }
            }

            if (!string.IsNullOrWhiteSpace(calibration))
            {
                var now = DateTime.Now.Date;
                if (calibration == "required")
                {
                    query = query.Where(a => a.NeedsCalibration == true);
                }
                else if (calibration == "overdue")
                {
                    query = query.Where(a => a.NeedsCalibration == true && a.NextCalibration.HasValue && a.NextCalibration.Value < now);
                }
                else if (calibration == "due30")
                {
                    var future30 = now.AddDays(30);
                    query = query.Where(a => a.NeedsCalibration == true && a.NextCalibration.HasValue && a.NextCalibration.Value >= now && a.NextCalibration.Value <= future30);
                }
                else if (calibration == "valid")
                {
                    var future30 = now.AddDays(30);
                    query = query.Where(a => a.NeedsCalibration == true && a.NextCalibration.HasValue && a.NextCalibration.Value > future30);
                }
            }

            ViewBag.Search = search;
            ViewBag.SelectedStatus = status;
            ViewBag.SelectedType = type;
            ViewBag.SelectedRoomId = roomId;
            ViewBag.SelectedWarranty = warranty;
            ViewBag.SelectedCalibration = calibration;
            ViewBag.SelectedCategoryId = categoryId;

            ViewBag.Rooms = await _context.Rooms.OrderBy(r => r.Floor).ThenBy(r => r.RoomNo).ToListAsync();
            ViewBag.Types = await _context.Assets.Where(a => a.Type != null).Select(a => a.Type!).Distinct().ToListAsync();

            var catQuery = _context.AssetCategories.Where(c => c.IsActive).AsQueryable();
            if (_currentUserService.IsMaintenance())
            {
                var myTeam = _currentUserService.GetMaintenanceTeam();
                catQuery = catQuery.Where(c => c.Department == myTeam);
            }
            ViewBag.Categories = await catQuery.OrderBy(c => c.Name).ToListAsync();

            var list = await query.OrderByDescending(a => a.Id).ToListAsync();
            return View(list);
        }

        // GET: Assets/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var asset = await _context.Assets
                .Include(a => a.User)
                .Include(a => a.Room)
                .Include(a => a.Supplier)
                .Include(a => a.CustodyType)
                .Include(a => a.Category)
                .Include(a => a.CompatibleSpareParts).ThenInclude(cp => cp.SparePart)
                .Include(a => a.History).ThenInclude(h => h.User)
                .Include(a => a.History).ThenInclude(h => h.Room)
                .Include(a => a.History).ThenInclude(h => h.CustodyType)
                .Include(a => a.MaintenanceRecords).ThenInclude(m => m.Technician)
                .Include(a => a.MaintenanceRecords).ThenInclude(m => m.Supplier)
                .Include(a => a.Tickets).ThenInclude(t => t.Status)
                .Include(a => a.Tickets).ThenInclude(t => t.Handler).ThenInclude(h => h!.TechUser)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (asset == null) return NotFound();

            // RBAC Guard
            if (!_currentUserService.CanAccessAsset(asset))
            {
                return Forbid();
            }

            var calProtocols = await _context.CalibrationForms
                .Where(cf => cf.IsActive && (cf.Department == asset.MaintenanceTeam || (asset.CategoryId != null && cf.CategoryId == asset.CategoryId)))
                .OrderBy(cf => cf.Name)
                .ToListAsync();

            var checklists = await _context.ChecklistForms
                .Where(cl => cl.IsActive && (cl.Department == asset.MaintenanceTeam || (asset.CategoryId != null && cl.CategoryId == asset.CategoryId)))
                .OrderBy(cl => cl.Title)
                .ToListAsync();

            var spareParts = await _context.SpareParts
                .Where(sp => sp.IsActive && sp.Department == asset.MaintenanceTeam)
                .OrderBy(sp => sp.Name)
                .ToListAsync();

            var technicians = await _context.Users
                .Where(u => u.IsActive == true && (u.MaintenanceTeam == asset.MaintenanceTeam || u.Role == SystemRoles.Admin))
                .OrderBy(u => u.FullName)
                .ToListAsync();

            var vm = new AssetDetailsViewModel
            {
                Asset = asset,
                AvailableUsers = await _context.Users.Where(u => u.IsActive == true).OrderBy(u => u.FullName).ToListAsync(),
                AvailableRooms = await _context.Rooms.OrderBy(r => r.Floor).ThenBy(r => r.RoomNo).ToListAsync(),
                AvailableCustodyTypes = await _context.CustodyTypes.ToListAsync(),
                NewUserId = asset.UserId ?? 0,
                NewRoomId = asset.RoomId ?? 0,
                NewCustodyTypeId = asset.CustodyTypeId ?? 1,
                AvailableCalibrationProtocols = calProtocols,
                AvailableChecklists = checklists,
                AvailableSpareParts = spareParts,
                AvailableTechnicians = technicians
            };

            return View(vm);
        }

        // GET: Assets/Create
        public async Task<IActionResult> Create(int? deviceFormId = null)
        {
            if (!_currentUserService.CanManageAssets()) return Forbid();

            var formsQuery = _context.DeviceForms.AsQueryable();
            if (_currentUserService.IsMaintenance())
            {
                var myTeam = _currentUserService.GetMaintenanceTeam();
                formsQuery = formsQuery.Where(f => f.Department == myTeam);
            }

            var availableForms = await formsQuery.OrderBy(f => f.Name).ToListAsync();
            ViewBag.AvailableDeviceForms = availableForms;

            DeviceForm? selectedForm = null;
            if (deviceFormId.HasValue && deviceFormId.Value > 0)
            {
                selectedForm = availableForms.FirstOrDefault(f => f.Id == deviceFormId.Value);
            }
            if (selectedForm == null && availableForms.Any())
            {
                selectedForm = availableForms.First();
            }

            ViewBag.SelectedDeviceForm = selectedForm;
            await PopulateDropdowns();

            var defaultTeam = selectedForm?.Department ?? (_currentUserService.GetMaintenanceTeam() ?? MaintenanceTeams.IT);

            return View(new Asset
            {
                Status = "In Service",
                BuyingDate = DateTime.Now,
                NeedsPeriodic = false,
                Type = selectedForm?.Name,
                MaintenanceTeam = defaultTeam
            });
        }

        // POST: Assets/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Asset asset, int deviceFormId, IFormCollection? formCollection = null, int[]? compatibleSparePartIds = null)
        {
            if (!_currentUserService.CanManageAssets()) return Forbid();
            formCollection ??= new FormCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>());

            var selectedForm = await _context.DeviceForms.FindAsync(deviceFormId);
            if (selectedForm == null)
            {
                ModelState.AddModelError("deviceFormId", "Please select a valid device specification template.");
            }
            else
            {
                // RBAC check: User can only create equipment in their department
                if (!_currentUserService.CanCreateAssetInDepartment(selectedForm.Department))
                {
                    return Forbid();
                }

                asset.Type = selectedForm.Name;
                asset.MaintenanceTeam = selectedForm.Department;

                // Extract dynamic specification fields
                var specsDict = new Dictionary<string, string>();
                foreach (var field in selectedForm.Fields)
                {
                    var formVal = formCollection[$"spec_{field.Name}"].ToString();
                    if (string.IsNullOrWhiteSpace(formVal))
                    {
                        formVal = formCollection[$"specs[{field.Name}]"].ToString();
                    }

                    if (field.Required && string.IsNullOrWhiteSpace(formVal))
                    {
                        ModelState.AddModelError($"spec_{field.Name}", $"{field.Label} is required.");
                    }

                    if (!string.IsNullOrWhiteSpace(formVal))
                    {
                        specsDict[field.Name] = formVal.Trim();
                    }
                }

                asset.Specs = JsonSerializer.Serialize(specsDict, new JsonSerializerOptions { WriteIndented = false });
            }

            // Validate unique HospitalNo
            if (!string.IsNullOrWhiteSpace(asset.HospitalNo) && 
                await _context.Assets.AnyAsync(a => a.HospitalNo == asset.HospitalNo.Trim()))
            {
                ModelState.AddModelError("HospitalNo", "An asset with this Hospital Tag Number already exists.");
            }

            if (ModelState.IsValid && selectedForm != null)
            {
                // Calculate next periodic if needed
                if (asset.NeedsPeriodic == true && asset.PeriodicInterval.HasValue && asset.PeriodicInterval.Value > 0)
                {
                    asset.LastPeriodic ??= DateTime.Now;
                    asset.NextPeriodic = asset.LastPeriodic.Value.AddDays(asset.PeriodicInterval.Value);
                }

                // Calculate next calibration if needed (Task 2)
                if (asset.NeedsCalibration == true && asset.CalibrationInterval.HasValue && asset.CalibrationInterval.Value > 0)
                {
                    asset.LastCalibration ??= DateTime.Now;
                    if (!asset.NextCalibration.HasValue)
                    {
                        asset.NextCalibration = asset.LastCalibration.Value.AddDays(asset.CalibrationInterval.Value);
                    }
                }
                else if (asset.NeedsCalibration != true)
                {
                    asset.NextCalibration = null;
                }

                // Process installed softwares configuration
                var selectedSoftwares = formCollection["selectedSoftwares"].ToArray();
                if (selectedSoftwares != null && selectedSoftwares.Length > 0)
                {
                    asset.InstalledSoftwares = JsonSerializer.Serialize(selectedSoftwares);
                }
                else
                {
                    asset.InstalledSoftwares = "[]";
                }

                _context.Assets.Add(asset);
                await _context.SaveChangesAsync();

                // Save compatible spare parts (Task 2)
                if (compatibleSparePartIds != null && compatibleSparePartIds.Length > 0)
                {
                    foreach (var spId in compatibleSparePartIds.Distinct())
                    {
                        _context.AssetCompatibleSpareParts.Add(new AssetCompatibleSparePart
                        {
                            AssetId = asset.Id,
                            SparePartId = spId,
                            AssignedAt = DateTime.Now
                        });
                    }
                    await _context.SaveChangesAsync();
                }

                // Record initial custody history
                if (asset.UserId.HasValue && asset.RoomId.HasValue)
                {
                    _context.AssetHistories.Add(new AssetHistory
                    {
                        AssetId = asset.Id,
                        UserId = asset.UserId.Value,
                        RoomId = asset.RoomId.Value,
                        CustodyTypeId = asset.CustodyTypeId ?? 1,
                        AssignedOn = DateTime.Now,
                        Notes = "Initial commissioning & assignment"
                    });
                    await _context.SaveChangesAsync();
                }

                TempData["SuccessMessage"] = $"Asset {asset.HospitalNo ?? ("#" + asset.Id)} was registered successfully under {asset.Type} ({asset.MaintenanceTeam})!";
                return RedirectToAction(nameof(Details), new { id = asset.Id });
            }

            var formsQuery = _context.DeviceForms.AsQueryable();
            if (_currentUserService.IsMaintenance())
            {
                var myTeam = _currentUserService.GetMaintenanceTeam();
                formsQuery = formsQuery.Where(f => f.Department == myTeam);
            }
            ViewBag.AvailableDeviceForms = await formsQuery.OrderBy(f => f.Name).ToListAsync();
            ViewBag.SelectedDeviceForm = selectedForm;

            await PopulateDropdowns(asset);
            return View(asset);
        }

        // GET: Assets/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var asset = await _context.Assets.FindAsync(id);
            if (asset == null) return NotFound();

            // RBAC Guard: URL tampering prevention
            if (!_currentUserService.CanAccessAsset(asset) || !_currentUserService.CanManageAsset(asset))
            {
                return Forbid();
            }

            // Find matching device form
            var matchingForm = await _context.DeviceForms.FirstOrDefaultAsync(f => 
                f.Name.ToLower() == (asset.Type ?? "").ToLower() && f.Department == asset.MaintenanceTeam)
                ?? await _context.DeviceForms.FirstOrDefaultAsync(f => f.Department == asset.MaintenanceTeam);

            ViewBag.DeviceForm = matchingForm;

            // Parse specs dictionary
            var specsDict = new Dictionary<string, string>();
            if (!string.IsNullOrWhiteSpace(asset.Specs))
            {
                try
                {
                    specsDict = JsonSerializer.Deserialize<Dictionary<string, string>>(asset.Specs, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
                }
                catch { }
            }
            ViewBag.SpecsDictionary = specsDict;

            // Find existing compatible spare parts (Task 2)
            var selectedSparePartIds = await _context.AssetCompatibleSpareParts
                .Where(cp => cp.AssetId == asset.Id)
                .Select(cp => cp.SparePartId)
                .ToListAsync();
            ViewBag.SelectedSparePartIds = selectedSparePartIds;

            await PopulateDropdowns(asset);
            return View(asset);
        }

        // POST: Assets/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Asset asset, int? deviceFormId = null, IFormCollection? formCollection = null, int[]? compatibleSparePartIds = null)
        {
            if (id != asset.Id) return NotFound();
            formCollection ??= new FormCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>());

            var existing = await _context.Assets.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id);
            if (existing == null) return NotFound();

            // RBAC Guard: URL tampering prevention
            if (!_currentUserService.CanAccessAsset(existing) || !_currentUserService.CanManageAsset(existing))
            {
                return Forbid();
            }

            // Task 9: Retain original purchase price from database (Cost is not editable/visible in Edit view)
            asset.Price = existing.Price;

            // Preserve maintenance team if not Admin
            if (_currentUserService.IsMaintenance())
            {
                asset.MaintenanceTeam = existing.MaintenanceTeam;
            }

            // Process dynamic specification fields
            DeviceForm? selectedForm = null;
            if (deviceFormId.HasValue && deviceFormId.Value > 0)
            {
                selectedForm = await _context.DeviceForms.FindAsync(deviceFormId.Value);
            }
            if (selectedForm == null)
            {
                selectedForm = await _context.DeviceForms.FirstOrDefaultAsync(f => 
                    f.Name.ToLower() == (asset.Type ?? "").ToLower() && f.Department == asset.MaintenanceTeam);
            }

            if (selectedForm != null)
            {
                var specsDict = new Dictionary<string, string>();
                foreach (var field in selectedForm.Fields)
                {
                    var formVal = formCollection[$"spec_{field.Name}"].ToString();
                    if (string.IsNullOrWhiteSpace(formVal))
                    {
                        formVal = formCollection[$"specs[{field.Name}]"].ToString();
                    }
                    if (!string.IsNullOrWhiteSpace(formVal))
                    {
                        specsDict[field.Name] = formVal.Trim();
                    }
                }
                asset.Specs = JsonSerializer.Serialize(specsDict, new JsonSerializerOptions { WriteIndented = false });
            }

            // Validate unique HospitalNo if changed
            if (!string.IsNullOrWhiteSpace(asset.HospitalNo) && 
                await _context.Assets.AnyAsync(a => a.HospitalNo == asset.HospitalNo.Trim() && a.Id != id))
            {
                ModelState.AddModelError("HospitalNo", "An asset with this Hospital Tag Number already exists.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    if (asset.NeedsPeriodic == true && asset.PeriodicInterval.HasValue && asset.PeriodicInterval.Value > 0)
                    {
                        if (asset.LastPeriodic.HasValue)
                        {
                            asset.NextPeriodic = asset.LastPeriodic.Value.AddDays(asset.PeriodicInterval.Value);
                        }
                    }
                    else
                    {
                        asset.NextPeriodic = null;
                    }

                    // Calculate next calibration if needed (Task 2)
                    if (asset.NeedsCalibration == true && asset.CalibrationInterval.HasValue && asset.CalibrationInterval.Value > 0)
                    {
                        if (asset.LastCalibration.HasValue)
                        {
                            asset.NextCalibration = asset.LastCalibration.Value.AddDays(asset.CalibrationInterval.Value);
                        }
                    }
                    else if (asset.NeedsCalibration != true)
                    {
                        asset.NextCalibration = null;
                    }

                    // Process installed softwares configuration
                    var selectedSoftwares = formCollection["selectedSoftwares"].ToArray();
                    if (selectedSoftwares != null && selectedSoftwares.Length > 0)
                    {
                        asset.InstalledSoftwares = JsonSerializer.Serialize(selectedSoftwares);
                    }
                    else
                    {
                        asset.InstalledSoftwares = "[]";
                    }

                    _context.Update(asset);
                    await _context.SaveChangesAsync();

                    // Synchronize compatible spare parts (Task 2)
                    var currentRelations = await _context.AssetCompatibleSpareParts.Where(cp => cp.AssetId == asset.Id).ToListAsync();
                    var targetIds = (compatibleSparePartIds ?? Array.Empty<int>()).Distinct().ToHashSet();

                    var toRemove = currentRelations.Where(r => !targetIds.Contains(r.SparePartId)).ToList();
                    if (toRemove.Any())
                    {
                        _context.AssetCompatibleSpareParts.RemoveRange(toRemove);
                    }

                    var currentIds = currentRelations.Select(r => r.SparePartId).ToHashSet();
                    var toAdd = targetIds.Where(tid => !currentIds.Contains(tid)).Select(tid => new AssetCompatibleSparePart
                    {
                        AssetId = asset.Id,
                        SparePartId = tid,
                        AssignedAt = DateTime.Now
                    }).ToList();

                    if (toAdd.Any())
                    {
                        _context.AssetCompatibleSpareParts.AddRange(toAdd);
                    }

                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Asset specifications & details updated successfully!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Assets.Any(e => e.Id == asset.Id)) return NotFound();
                    throw;
                }
                return RedirectToAction(nameof(Details), new { id = asset.Id });
            }

            ViewBag.DeviceForm = selectedForm;
            await PopulateDropdowns(asset);
            return View(asset);
        }

        // POST: Assets/TransferCustody
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TransferCustody(int assetId, int newUserId, int newRoomId, int newCustodyTypeId, string? notes)
        {
            var asset = await _context.Assets.FindAsync(assetId);
            if (asset == null) return NotFound();

            if (!_currentUserService.CanManageAsset(asset)) return Forbid();

            // Close existing history record if open
            var previousHistory = await _context.AssetHistories
                .Where(h => h.AssetId == assetId && h.ReturnedOn == null)
                .OrderByDescending(h => h.AssignedOn)
                .FirstOrDefaultAsync();

            if (previousHistory != null)
            {
                previousHistory.ReturnedOn = DateTime.Now;
            }

            // Create new history entry
            var history = new AssetHistory
            {
                AssetId = asset.Id,
                UserId = newUserId,
                RoomId = newRoomId,
                CustodyTypeId = newCustodyTypeId,
                AssignedOn = DateTime.Now,
                Notes = notes ?? "Custody re-assigned"
            };
            _context.AssetHistories.Add(history);

            // Update asset record
            asset.UserId = newUserId;
            asset.RoomId = newRoomId;
            asset.CustodyTypeId = newCustodyTypeId;
            _context.Update(asset);

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Asset custody and location updated successfully!";
            return RedirectToAction(nameof(Details), new { id = assetId });
        }

        // POST: Assets/Retire/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Retire(int id, string? reason)
        {
            var asset = await _context.Assets.FindAsync(id);
            if (asset == null) return NotFound();

            if (!_currentUserService.CanManageAsset(asset)) return Forbid();

            asset.Status = "Retired";
            _context.Update(asset);

            // Record history notes
            _context.AssetHistories.Add(new AssetHistory
            {
                AssetId = asset.Id,
                UserId = asset.UserId ?? 1,
                RoomId = asset.RoomId ?? 1,
                CustodyTypeId = asset.CustodyTypeId ?? 1,
                AssignedOn = DateTime.Now,
                ReturnedOn = DateTime.Now,
                Notes = $"Asset retired / decommissioned. Reason: {reason ?? "End of life cycle"}"
            });

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Asset {asset.HospitalNo ?? ("#" + asset.Id)} was marked as Retired.";
            return RedirectToAction(nameof(Details), new { id });
        }

        // GET: Assets/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (!_currentUserService.IsAdmin()) return Forbid();
            if (id == null) return NotFound();

            var asset = await _context.Assets
                .Include(a => a.User)
                .Include(a => a.Room)
                .Include(a => a.Tickets)
                .Include(a => a.MaintenanceRecords)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (asset == null) return NotFound();

            return View(asset);
        }

        // POST: Assets/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (!_currentUserService.IsAdmin()) return Forbid();

            var asset = await _context.Assets
                .Include(a => a.Tickets)
                .Include(a => a.MaintenanceRecords)
                .Include(a => a.History)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (asset != null)
            {
                var tag = asset.HospitalNo ?? ("#" + asset.Id);

                // If asset has active tickets, prevent deletion
                if (asset.Tickets.Any(t => t.StatusId != 5 && t.StatusId != 4))
                {
                    TempData["ErrorMessage"] = $"Cannot delete asset {tag} because it has open or pending tickets. Close or reassign tickets first, or retire the asset.";
                    return RedirectToAction(nameof(Details), new { id });
                }

                _context.Assets.Remove(asset);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Asset {tag} was deleted from inventory.";
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Assets/PrintTag/5
        public async Task<IActionResult> PrintTag(int? id)
        {
            if (id == null) return NotFound();

            var asset = await _context.Assets
                .Include(a => a.User)
                .Include(a => a.Room)
                .Include(a => a.Supplier)
                .Include(a => a.CustodyType)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (asset == null) return NotFound();

            if (!_currentUserService.CanAccessAsset(asset)) return Forbid();

            return View(asset);
        }

        // GET: Assets/PrintBulkTags
        public async Task<IActionResult> PrintBulkTags(
            string? search = null, 
            string? status = null, 
            string? type = null, 
            int? roomId = null, 
            int? categoryId = null,
            string? warranty = null, 
            string? calibration = null, 
            string? ids = null)
        {
            var query = _context.Assets
                .Include(a => a.User)
                .Include(a => a.Room)
                .Include(a => a.Supplier)
                .Include(a => a.CustodyType)
                .AsQueryable();

            if (_currentUserService.IsEndUser())
            {
                int currentUserId = _currentUserService.GetUserId();
                query = query.Where(a => a.UserId == currentUserId);
            }
            else if (_currentUserService.IsMaintenance())
            {
                var myTeam = _currentUserService.GetMaintenanceTeam();
                query = query.Where(a => a.MaintenanceTeam == myTeam);
            }

            if (!string.IsNullOrWhiteSpace(ids))
            {
                if (string.Equals(ids.Trim(), "none", StringComparison.OrdinalIgnoreCase))
                {
                    query = query.Where(a => false);
                }
                else
                {
                    var idList = ids.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                                    .Select(s => int.TryParse(s, out var v) ? v : 0)
                                    .Where(v => v > 0)
                                    .ToList();
                    if (idList.Any())
                    {
                        query = query.Where(a => idList.Contains(a.Id));
                    }
                    else
                    {
                        query = query.Where(a => false);
                    }
                }
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(search))
                {
                    var s = search.Trim().ToLower();
                    query = query.Where(a =>
                        (a.HospitalNo != null && a.HospitalNo.ToLower().Contains(s)) ||
                        (a.SerialNo != null && a.SerialNo.ToLower().Contains(s)) ||
                        (a.Brand != null && a.Brand.ToLower().Contains(s)) ||
                        (a.Type != null && a.Type.ToLower().Contains(s)) ||
                        (a.User != null && a.User.FullName != null && a.User.FullName.ToLower().Contains(s))
                    );
                }

                if (!string.IsNullOrWhiteSpace(status)) query = query.Where(a => a.Status == status);
                if (!string.IsNullOrWhiteSpace(type)) query = query.Where(a => a.Type == type);
                if (roomId.HasValue) query = query.Where(a => a.RoomId == roomId.Value);
                if (categoryId.HasValue) query = query.Where(a => a.CategoryId == categoryId.Value);

                if (!string.IsNullOrWhiteSpace(warranty))
                {
                    var now = DateTime.Now.Date;
                    if (warranty == "expired")
                    {
                        query = query.Where(a => a.WarrantyTill.HasValue && a.WarrantyTill.Value < now);
                    }
                    else if (warranty == "expiring30")
                    {
                        var future30 = now.AddDays(30);
                        query = query.Where(a => a.WarrantyTill.HasValue && a.WarrantyTill.Value >= now && a.WarrantyTill.Value <= future30);
                    }
                    else if (warranty == "active")
                    {
                        query = query.Where(a => a.WarrantyTill.HasValue && a.WarrantyTill.Value >= now);
                    }
                }

                if (!string.IsNullOrWhiteSpace(calibration))
                {
                    var now = DateTime.Now.Date;
                    if (calibration == "required")
                    {
                        query = query.Where(a => a.NeedsCalibration == true);
                    }
                    else if (calibration == "overdue")
                    {
                        query = query.Where(a => a.NeedsCalibration == true && a.NextCalibration.HasValue && a.NextCalibration.Value < now);
                    }
                    else if (calibration == "due30")
                    {
                        var future30 = now.AddDays(30);
                        query = query.Where(a => a.NeedsCalibration == true && a.NextCalibration.HasValue && a.NextCalibration.Value >= now && a.NextCalibration.Value <= future30);
                    }
                    else if (calibration == "valid")
                    {
                        var future30 = now.AddDays(30);
                        query = query.Where(a => a.NeedsCalibration == true && a.NextCalibration.HasValue && a.NextCalibration.Value > future30);
                    }
                }
            }

            var assets = await query.OrderBy(a => a.HospitalNo ?? a.Id.ToString()).ToListAsync();
            return View(assets);
        }

        // POST: Assets/LogCalibration (Task 2)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LogCalibration(int assetId, DateTime calibrationDate, int? intervalDays, string? certNo, string? calibratedBy, string? notes)
        {
            var asset = await _context.Assets.FindAsync(assetId);
            if (asset == null) return NotFound();

            if (!_currentUserService.CanManageAsset(asset)) return Forbid();

            asset.NeedsCalibration = true;
            if (intervalDays.HasValue && intervalDays.Value > 0)
            {
                asset.CalibrationInterval = intervalDays.Value;
            }
            asset.LastCalibration = calibrationDate;
            int interval = asset.CalibrationInterval ?? 180;
            asset.NextCalibration = calibrationDate.AddDays(interval);
            if (!string.IsNullOrWhiteSpace(certNo)) asset.CalibrationCertNo = certNo.Trim();
            if (!string.IsNullOrWhiteSpace(calibratedBy)) asset.CalibratedBy = calibratedBy.Trim();
            if (!string.IsNullOrWhiteSpace(notes)) asset.CalibrationNotes = notes.Trim();

            // Record verification in asset maintenance logs
            _context.AssetMaintenances.Add(new AssetMaintenance
            {
                AssetId = asset.Id,
                MaintenanceType = 2, // Periodic Inspection / Calibration
                TimStamp = DateTime.Now,
                UserId = _currentUserService.GetUserId(),
                Description = $"Periodic Calibration completed on {calibrationDate:yyyy-MM-dd}. Certificate #{certNo ?? "N/A"} issued by {calibratedBy ?? "Authorized Metrology Body"}. Next due date: {asset.NextCalibration:yyyy-MM-dd}. Notes: {notes ?? "Compliant with standard specifications."}"
            });

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Calibration successfully recorded for {asset.HospitalNo ?? ("#" + asset.Id)}! Next calibration due on {asset.NextCalibration:MMM dd, yyyy}.";
            return RedirectToAction(nameof(Details), new { id = asset.Id });
        }

        // GET: Assets/ExportCsv
        public async Task<IActionResult> ExportCsv(
            string? search = null, 
            string? status = null, 
            string? type = null, 
            int? roomId = null, 
            int? categoryId = null,
            string? warranty = null, 
            string? calibration = null, 
            string? ids = null)
        {
            var query = _context.Assets
                .Include(a => a.User)
                .Include(a => a.Room)
                .Include(a => a.Supplier)
                .Include(a => a.CustodyType)
                .AsQueryable();

            if (_currentUserService.IsEndUser())
            {
                int currentUserId = _currentUserService.GetUserId();
                query = query.Where(a => a.UserId == currentUserId);
            }
            else if (_currentUserService.IsMaintenance())
            {
                var myTeam = _currentUserService.GetMaintenanceTeam();
                query = query.Where(a => a.MaintenanceTeam == myTeam);
            }

            if (!string.IsNullOrWhiteSpace(ids))
            {
                if (string.Equals(ids.Trim(), "none", StringComparison.OrdinalIgnoreCase))
                {
                    query = query.Where(a => false);
                }
                else
                {
                    var idList = ids.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                                    .Select(s => int.TryParse(s, out var v) ? v : 0)
                                    .Where(v => v > 0)
                                    .ToList();
                    if (idList.Any())
                    {
                        query = query.Where(a => idList.Contains(a.Id));
                    }
                    else
                    {
                        query = query.Where(a => false);
                    }
                }
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(search))
                {
                    var s = search.Trim().ToLower();
                    query = query.Where(a =>
                        (a.HospitalNo != null && a.HospitalNo.ToLower().Contains(s)) ||
                        (a.SerialNo != null && a.SerialNo.ToLower().Contains(s)) ||
                        (a.Brand != null && a.Brand.ToLower().Contains(s)) ||
                        (a.Type != null && a.Type.ToLower().Contains(s)) ||
                        (a.User != null && a.User.FullName != null && a.User.FullName.ToLower().Contains(s))
                    );
                }

                if (!string.IsNullOrWhiteSpace(status)) query = query.Where(a => a.Status == status);
                if (!string.IsNullOrWhiteSpace(type)) query = query.Where(a => a.Type == type);
                if (roomId.HasValue) query = query.Where(a => a.RoomId == roomId.Value);
                if (categoryId.HasValue) query = query.Where(a => a.CategoryId == categoryId.Value);

                if (!string.IsNullOrWhiteSpace(warranty))
                {
                    var now = DateTime.Now.Date;
                    if (warranty == "expired")
                    {
                        query = query.Where(a => a.WarrantyTill.HasValue && a.WarrantyTill.Value < now);
                    }
                    else if (warranty == "expiring30")
                    {
                        var future30 = now.AddDays(30);
                        query = query.Where(a => a.WarrantyTill.HasValue && a.WarrantyTill.Value >= now && a.WarrantyTill.Value <= future30);
                    }
                    else if (warranty == "active")
                    {
                        query = query.Where(a => a.WarrantyTill.HasValue && a.WarrantyTill.Value >= now);
                    }
                }

                if (!string.IsNullOrWhiteSpace(calibration))
                {
                    var now = DateTime.Now.Date;
                    if (calibration == "required")
                    {
                        query = query.Where(a => a.NeedsCalibration == true);
                    }
                    else if (calibration == "overdue")
                    {
                        query = query.Where(a => a.NeedsCalibration == true && a.NextCalibration.HasValue && a.NextCalibration.Value < now);
                    }
                    else if (calibration == "due30")
                    {
                        var future30 = now.AddDays(30);
                        query = query.Where(a => a.NeedsCalibration == true && a.NextCalibration.HasValue && a.NextCalibration.Value >= now && a.NextCalibration.Value <= future30);
                    }
                    else if (calibration == "valid")
                    {
                        var future30 = now.AddDays(30);
                        query = query.Where(a => a.NeedsCalibration == true && a.NextCalibration.HasValue && a.NextCalibration.Value > future30);
                    }
                }
            }

            var assets = await query.OrderBy(a => a.Id).ToListAsync();

            var sb = new StringBuilder();
            sb.AppendLine("ID,Hospital Tag,Maintenance Team,Serial Number,Brand,Model,Type,Status,Room Location,Custodian,Custody Type,Supplier,Price,Buying Date,Warranty Expiration,Needs Periodic,Next Periodic,Needs Calibration,Calibration Interval,Last Calibration,Next Calibration,Calibration Cert,Calibrated By");

            foreach (var a in assets)
            {
                sb.AppendLine($"\"{a.Id}\",\"{EscapeCsv(a.HospitalNo)}\",\"{EscapeCsv(a.MaintenanceTeam)}\",\"{EscapeCsv(a.SerialNo)}\",\"{EscapeCsv(a.Brand)}\",\"{a.ModelNo}\",\"{EscapeCsv(a.Type)}\",\"{EscapeCsv(a.Status)}\",\"{EscapeCsv(a.Room?.DisplayName)}\",\"{EscapeCsv(a.User?.FullName)}\",\"{EscapeCsv(a.CustodyType?.Type)}\",\"{EscapeCsv(a.Supplier?.Name)}\",\"{a.Price:F2}\",\"{a.BuyingDate:yyyy-MM-dd}\",\"{a.WarrantyTill:yyyy-MM-dd}\",\"{(a.NeedsPeriodic == true ? "Yes" : "No")}\",\"{a.NextPeriodic:yyyy-MM-dd}\",\"{(a.NeedsCalibration == true ? "Yes" : "No")}\",\"{a.CalibrationInterval}\",\"{a.LastCalibration:yyyy-MM-dd}\",\"{a.NextCalibration:yyyy-MM-dd}\",\"{EscapeCsv(a.CalibrationCertNo)}\",\"{EscapeCsv(a.CalibratedBy)}\"");
            }

            var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
            return File(bytes, "text/csv", $"Assets_Export_{DateTime.Now:yyyyMMdd_HHmm}.csv");
        }

        private static string EscapeCsv(string? value)
        {
            if (string.IsNullOrEmpty(value)) return "";
            return value.Replace("\"", "\"\"");
        }

        private async Task PopulateDropdowns(Asset? asset = null)
        {
            ViewBag.Users = new SelectList(await _context.Users.Where(u => u.IsActive == true).OrderBy(u => u.FullName).ToListAsync(), "Id", "FullName", asset?.UserId);
            ViewBag.Rooms = new SelectList(await _context.Rooms.OrderBy(r => r.Floor).ThenBy(r => r.RoomNo).ToListAsync(), "Id", "DisplayName", asset?.RoomId);
            string? targetTeam = asset?.MaintenanceTeam;
            if (string.IsNullOrWhiteSpace(targetTeam) && _currentUserService.IsMaintenance())
            {
                targetTeam = _currentUserService.GetMaintenanceTeam();
            }

            var allSuppliers = await _context.Suppliers.Where(s => s.IsActive == true).OrderBy(s => s.Name).ToListAsync();
            var filteredSuppliers = !string.IsNullOrWhiteSpace(targetTeam)
                ? allSuppliers.Where(s => s.SupportsDepartment(targetTeam)).ToList()
                : allSuppliers;

            ViewBag.Suppliers = new SelectList(filteredSuppliers, "Id", "Name", asset?.SupplierId);
            ViewBag.CustodyTypes = new SelectList(await _context.CustodyTypes.ToListAsync(), "Id", "Type", asset?.CustodyTypeId);
            ViewBag.MaintenanceTeams = new SelectList(MaintenanceTeams.All, asset?.MaintenanceTeam ?? MaintenanceTeams.IT);
            ViewBag.SoftwareCatalog = await _context.SoftwareCatalog.Where(s => s.IsActive).OrderBy(s => s.Category).ThenBy(s => s.Name).ToListAsync();

            // Categories (Task 6)
            var catQuery = _context.AssetCategories.Where(c => c.IsActive).AsQueryable();
            if (!string.IsNullOrWhiteSpace(targetTeam))
            {
                catQuery = catQuery.Where(c => c.Department == targetTeam);
            }
            ViewBag.Categories = new SelectList(await catQuery.OrderBy(c => c.Name).ToListAsync(), "Id", "Name", asset?.CategoryId);

            // Available Spare Parts for compatibility (Task 2)
            var partsQuery = _context.SpareParts.Where(sp => sp.IsActive).AsQueryable();
            if (!string.IsNullOrWhiteSpace(targetTeam))
            {
                partsQuery = partsQuery.Where(sp => sp.Department == targetTeam);
            }
            ViewBag.AvailableSpareParts = await partsQuery.OrderBy(sp => sp.Name).ToListAsync();
        }

        // GET: Assets/GetInstalledSoftwares?assetId=123
        [HttpGet]
        public async Task<IActionResult> GetInstalledSoftwares(int assetId)
        {
            var asset = await _context.Assets.FindAsync(assetId);
            if (asset == null) return Json(new { success = false, softwares = Array.Empty<string>() });
            return Json(new { success = true, softwares = asset.InstalledSoftwaresList });
        }

        // POST: Assets/UpdateInstalledSoftwares
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateInstalledSoftwares(int assetId, string[]? selectedSoftwares)
        {
            var asset = await _context.Assets.FindAsync(assetId);
            if (asset == null) return NotFound();
            if (!_currentUserService.CanManageAsset(asset)) return Forbid();

            asset.InstalledSoftwares = JsonSerializer.Serialize(selectedSoftwares ?? Array.Empty<string>());
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Installed workstation software list updated successfully!";
            return RedirectToAction(nameof(Details), new { id = assetId });
        }

        // POST: Assets/RecordVendorMaintenanceVisit (Task 5)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RecordVendorMaintenanceVisit(int assetId, DateTime visitDate, string? engineerName, string? notes)
        {
            var asset = await _context.Assets.FindAsync(assetId);
            if (asset == null) return NotFound();
            if (!_currentUserService.CanManageAsset(asset)) return Forbid();

            var visits = asset.VendorVisitsList;
            int visitNo = (asset.VendorMaintenanceCompletedCount) + 1;
            visits.Add(new VendorVisitItem
            {
                VisitNumber = visitNo,
                VisitDate = visitDate,
                EngineerName = string.IsNullOrWhiteSpace(engineerName) ? "Vendor Service Engineer" : engineerName.Trim(),
                Notes = notes?.Trim(),
                RecordedAt = DateTime.Now
            });

            asset.VendorVisitsList = visits;
            asset.VendorMaintenanceCompletedCount = visits.Count;

            // Also record in asset maintenance logs
            _context.AssetMaintenances.Add(new AssetMaintenance
            {
                AssetId = asset.Id,
                MaintenanceType = 3, // Vendor Maintenance
                TimStamp = DateTime.Now,
                UserId = _currentUserService.GetUserId(),
                SupplierId = asset.SupplierId,
                WorkOrderStatus = "Completed",
                Description = $"Vendor Free After-Sale Maintenance Visit #{visitNo} conducted on {visitDate:yyyy-MM-dd} by {engineerName ?? "Vendor Engineer"}. Notes: {notes ?? "Periodic free checkup completed successfully."}"
            });

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Vendor Maintenance Visit #{visitNo} was successfully recorded for asset {asset.HospitalNo ?? ("#" + asset.Id)}!";
            return RedirectToAction(nameof(Details), new { id = assetId });
        }

        // GET: Assets/VendorMaintenanceSticker/5 (Task 5)
        [HttpGet]
        public async Task<IActionResult> VendorMaintenanceSticker(int id)
        {
            var asset = await _context.Assets
                .Include(a => a.Supplier)
                .Include(a => a.Room)
                .Include(a => a.Category)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (asset == null) return NotFound();
            if (!_currentUserService.CanAccessAsset(asset)) return Forbid();

            return View(asset);
        }

        // POST: Assets/LogInternalMaintenance (Task 10)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LogInternalMaintenance(int assetId, int? technicianId, string description, string? actionTaken, int[]? usedSparePartIds)
        {
            var asset = await _context.Assets.FindAsync(assetId);
            if (asset == null) return NotFound();
            if (!_currentUserService.CanManageAsset(asset)) return Forbid();

            int techId = technicianId ?? _currentUserService.GetUserId();

            var partsList = new List<string>();
            if (usedSparePartIds != null && usedSparePartIds.Length > 0)
            {
                foreach (var partId in usedSparePartIds.Distinct())
                {
                    var part = await _context.SpareParts.FindAsync(partId);
                    if (part != null)
                    {
                        partsList.Add(part.Name);
                        if (part.Quantity > 0)
                        {
                            part.Quantity--;
                            _context.SparePartTransactions.Add(new SparePartTransaction
                            {
                                SparePartId = part.Id,
                                AssetId = asset.Id,
                                TransactionType = "Usage in Maintenance",
                                QuantityChanged = -1,
                                QuantityAfter = part.Quantity,
                                UserId = techId,
                                Notes = $"Used in internal maintenance of asset {asset.HospitalNo ?? ("#" + asset.Id)}",
                                Timestamp = DateTime.Now
                            });
                        }
                    }
                }
            }

            var partsStr = partsList.Any() ? string.Join(", ", partsList) : null;

            var maintenanceRecord = new AssetMaintenance
            {
                AssetId = asset.Id,
                MaintenanceType = 1, // Internal Maintenance
                TimStamp = DateTime.Now,
                UserId = techId,
                SupplierId = null, // Internal
                WorkOrderStatus = "Completed",
                Description = description,
                ActionTaken = actionTaken,
                PartsReplaced = partsStr
            };

            _context.AssetMaintenances.Add(maintenanceRecord);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Internal maintenance log successfully saved for asset {asset.HospitalNo ?? ("#" + asset.Id)}!";
            return RedirectToAction(nameof(Details), new { id = assetId });
        }

        // POST: Assets/LogCalibrationProtocol (Task 7)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LogCalibrationProtocol(int assetId, int? calibrationFormId, DateTime calibrationDate, int? intervalDays, string? certNo, string? calibratedBy, string? notes, IFormCollection formCollection)
        {
            var asset = await _context.Assets.FindAsync(assetId);
            if (asset == null) return NotFound();
            if (!_currentUserService.CanManageAsset(asset)) return Forbid();

            asset.NeedsCalibration = true;
            if (intervalDays.HasValue && intervalDays.Value > 0)
            {
                asset.CalibrationInterval = intervalDays.Value;
            }
            asset.LastCalibration = calibrationDate;
            int interval = asset.CalibrationInterval ?? 180;
            asset.NextCalibration = calibrationDate.AddDays(interval);
            if (!string.IsNullOrWhiteSpace(certNo)) asset.CalibrationCertNo = certNo.Trim();
            if (!string.IsNullOrWhiteSpace(calibratedBy)) asset.CalibratedBy = calibratedBy.Trim();
            if (!string.IsNullOrWhiteSpace(notes)) asset.CalibrationNotes = notes.Trim();

            // Extract dynamic calibration protocol fields if a calibration form was selected
            var dynamicData = new Dictionary<string, string>();
            string protocolName = "Standard Calibration";
            if (calibrationFormId.HasValue && calibrationFormId.Value > 0)
            {
                var calForm = await _context.CalibrationForms.FindAsync(calibrationFormId.Value);
                if (calForm != null)
                {
                    protocolName = calForm.Name;
                    foreach (var field in calForm.Fields)
                    {
                        var val = formCollection[$"cal_{field.Name}"].ToString();
                        if (string.IsNullOrWhiteSpace(val))
                        {
                            val = formCollection[$"calib_{field.Name}"].ToString();
                        }
                        if (!string.IsNullOrWhiteSpace(val))
                        {
                            dynamicData[field.Name] = val.Trim();
                        }
                    }
                }
            }

            var protocolPayload = new
            {
                ProtocolName = protocolName,
                CalibrationDate = calibrationDate,
                CertNo = certNo,
                CalibratedBy = calibratedBy,
                Parameters = dynamicData,
                Notes = notes
            };

            asset.LastCalibrationData = JsonSerializer.Serialize(protocolPayload, new JsonSerializerOptions { WriteIndented = false });

            // Record verification in asset maintenance logs
            _context.AssetMaintenances.Add(new AssetMaintenance
            {
                AssetId = asset.Id,
                MaintenanceType = 2, // Periodic Inspection / Calibration
                TimStamp = DateTime.Now,
                UserId = _currentUserService.GetUserId(),
                SupplierId = null,
                WorkOrderStatus = "Completed",
                Description = $"Calibration Protocol [{protocolName}] completed on {calibrationDate:yyyy-MM-dd}. Certificate #{certNo ?? "N/A"} issued by {calibratedBy ?? "Authorized Metrology Body"}. Next due: {asset.NextCalibration:yyyy-MM-dd}. Notes: {notes ?? "Compliant"}"
            });

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Calibration protocol successfully saved for {asset.HospitalNo ?? ("#" + asset.Id)}! Next calibration due on {asset.NextCalibration:MMM dd, yyyy}.";
            return RedirectToAction(nameof(Details), new { id = asset.Id });
        }

        // POST: Assets/MarkChecklistDone (Task 8)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkChecklistDone(int assetId, string checklistTitle, string? inspectorName, string? notes)
        {
            var asset = await _context.Assets.FindAsync(assetId);
            if (asset == null) return NotFound();
            if (!_currentUserService.CanManageAsset(asset)) return Forbid();

            string inspector = !string.IsNullOrWhiteSpace(inspectorName) 
                ? inspectorName.Trim() 
                : (_currentUserService.GetFullName() ?? "Technician");

            asset.LastChecklistDate = DateTime.Now;
            asset.LastChecklistBy = inspector;

            _context.AssetMaintenances.Add(new AssetMaintenance
            {
                AssetId = asset.Id,
                MaintenanceType = 2, // Periodic Inspection / Checklist
                TimStamp = DateTime.Now,
                UserId = _currentUserService.GetUserId(),
                WorkOrderStatus = "Completed",
                Description = $"Periodic Checklist '{checklistTitle}' verified and signed off by {inspector}. Notes: {notes ?? "All checklist items inspected and passed."}"
            });

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Checklist verification recorded for {asset.HospitalNo ?? ("#" + asset.Id)} by {inspector}!";
            return RedirectToAction(nameof(Details), new { id = assetId });
        }
    }
}
