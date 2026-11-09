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
using StockPortal.ViewModels;

namespace StockPortal.Controllers
{
    public class SparePartsController : Controller
    {
        private readonly StockDbContext _context;
        private readonly ICurrentUserService _currentUserService;
        private readonly INotificationService _notificationService;

        public SparePartsController(
            StockDbContext context,
            ICurrentUserService currentUserService,
            INotificationService notificationService)
        {
            _context = context;
            _currentUserService = currentUserService;
            _notificationService = notificationService;
        }

        // GET: /SpareParts
        public async Task<IActionResult> Index(string? department, string? category, string? search, bool? lowStock)
        {
            // End Users have no access to reserve stock
            if (_currentUserService.IsEndUser())
            {
                return Forbid();
            }

            var query = _context.SpareParts
                .Where(p => p.IsActive)
                .AsQueryable();

            // RBAC Enforcement: Maintenance technicians are strictly isolated to their own department
            string? activeDept = department;
            if (_currentUserService.IsMaintenance())
            {
                var myTeam = _currentUserService.GetMaintenanceTeam() ?? MaintenanceTeams.IT;
                activeDept = myTeam;
                query = query.Where(p => p.Department == myTeam);
            }
            else if (!string.IsNullOrWhiteSpace(activeDept) && !string.Equals(activeDept, "ALL", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(p => p.Department == activeDept);
            }

            if (!string.IsNullOrWhiteSpace(category))
            {
                query = query.Where(p => p.Category == category);
            }

            if (lowStock == true)
            {
                query = query.Where(p => p.Quantity <= p.MinReserveQuantity);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                query = query.Where(p =>
                    p.Name.ToLower().Contains(s) ||
                    (p.NameAr != null && p.NameAr.ToLower().Contains(s)) ||
                    (p.PartNumber != null && p.PartNumber.ToLower().Contains(s)) ||
                    (p.CompatibleEquipment != null && p.CompatibleEquipment.ToLower().Contains(s)) ||
                    (p.StorageLocation != null && p.StorageLocation.ToLower().Contains(s))
                );
            }

            var items = await query
                .Include(p => p.SparePartForm)
                .Include(p => p.CompatibleAssets).ThenInclude(ca => ca.Asset)
                .OrderBy(p => p.Department)
                .ThenBy(p => p.Name)
                .ToListAsync();

            // Calculate metrics across relevant scope
            var scopeQuery = _context.SpareParts.Where(p => p.IsActive);
            if (_currentUserService.IsMaintenance())
            {
                var myTeam = _currentUserService.GetMaintenanceTeam() ?? MaintenanceTeams.IT;
                scopeQuery = scopeQuery.Where(p => p.Department == myTeam);
            }
            else if (!string.IsNullOrWhiteSpace(activeDept) && !string.Equals(activeDept, "ALL", StringComparison.OrdinalIgnoreCase))
            {
                scopeQuery = scopeQuery.Where(p => p.Department == activeDept);
            }

            var allScopeItems = await scopeQuery.ToListAsync();

            // Available forms for this department (Task 1)
            var formsQuery = _context.SparePartForms.AsQueryable();
            if (_currentUserService.IsMaintenance())
            {
                var myTeam = _currentUserService.GetMaintenanceTeam() ?? MaintenanceTeams.IT;
                formsQuery = formsQuery.Where(f => f.Department == myTeam);
            }
            else if (!string.IsNullOrWhiteSpace(activeDept) && !string.Equals(activeDept, "ALL", StringComparison.OrdinalIgnoreCase))
            {
                formsQuery = formsQuery.Where(f => f.Department == activeDept);
            }
            var availableForms = await formsQuery.OrderBy(f => f.Name).ToListAsync();

            // Available assets for compatibility (Task 2)
            var assetsQuery = _context.Assets.AsQueryable();
            if (_currentUserService.IsMaintenance())
            {
                var myTeam = _currentUserService.GetMaintenanceTeam() ?? MaintenanceTeams.IT;
                assetsQuery = assetsQuery.Where(a => a.MaintenanceTeam == myTeam);
            }
            else if (!string.IsNullOrWhiteSpace(activeDept) && !string.Equals(activeDept, "ALL", StringComparison.OrdinalIgnoreCase))
            {
                assetsQuery = assetsQuery.Where(a => a.MaintenanceTeam == activeDept);
            }
            var availableAssets = await assetsQuery.OrderBy(a => a.HospitalNo ?? a.SerialNo ?? a.Type).ToListAsync();

            var vm = new SparePartsIndexViewModel
            {
                Items = items,
                CurrentDepartment = activeDept,
                SelectedCategory = category,
                Search = search,
                OnlyLowStock = lowStock == true,
                TotalItems = allScopeItems.Count,
                TotalQuantity = allScopeItems.Sum(p => p.Quantity),
                LowStockCount = allScopeItems.Count(p => p.IsLowStock),
                DualNatureCount = allScopeItems.Count(p => p.CanFunctionAsAsset),
                AvailableDepartments = MaintenanceTeams.All.ToList(),
                AvailableForms = availableForms,
                AvailableAssets = availableAssets,
                Users = await _context.Users.Where(u => u.IsActive == true).OrderBy(u => u.FullName).ToListAsync(),
                Rooms = await _context.Rooms.OrderBy(r => r.RoomNo).ToListAsync()
            };

            return View(vm);
        }

        // POST: /SpareParts/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(SparePart model, int? formId, int[]? compatibleAssetIds, IFormCollection formCollection)
        {
            if (_currentUserService.IsEndUser()) return Forbid();

            // Maintenance technicians can only add items to their own department
            if (_currentUserService.IsMaintenance())
            {
                var myTeam = _currentUserService.GetMaintenanceTeam() ?? MaintenanceTeams.IT;
                model.Department = myTeam;
            }

            if (string.IsNullOrWhiteSpace(model.Name))
            {
                TempData["ErrorMessage"] = "Part/Item Name is required.";
                return RedirectToAction(nameof(Index), new { department = model.Department });
            }

            model.FormId = (formId.HasValue && formId.Value > 0) ? formId.Value : null;

            // Extract dynamic specs from selected SparePartForm (Task 1)
            var specsDict = new Dictionary<string, string>();
            if (model.FormId.HasValue)
            {
                var selectedForm = await _context.SparePartForms.FindAsync(model.FormId.Value);
                if (selectedForm != null)
                {
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
                }
            }
            model.Specs = JsonSerializer.Serialize(specsDict);

            // Task 9: Cost removed across application except adding asset
            model.UnitCost = null;

            model.CreatedAt = DateTime.Now;
            model.UpdatedAt = DateTime.Now;
            model.IsActive = true;

            _context.SpareParts.Add(model);
            await _context.SaveChangesAsync();

            // Save compatible devices (Task 2)
            if (compatibleAssetIds != null && compatibleAssetIds.Length > 0)
            {
                foreach (var aid in compatibleAssetIds.Where(id => id > 0).Distinct())
                {
                    _context.AssetCompatibleSpareParts.Add(new AssetCompatibleSparePart
                    {
                        AssetId = aid,
                        SparePartId = model.Id,
                        AssignedAt = DateTime.Now
                    });
                }
                await _context.SaveChangesAsync();
            }

            // If initial stock quantity is greater than zero, log initial replenishment
            if (model.Quantity > 0)
            {
                var transaction = new SparePartTransaction
                {
                    SparePartId = model.Id,
                    UserId = _currentUserService.GetUserId(),
                    TransactionType = "Replenishment",
                    QuantityChanged = model.Quantity,
                    QuantityAfter = model.Quantity,
                    Notes = "Initial reserve stock entry.",
                    Timestamp = DateTime.Now
                };
                _context.SparePartTransactions.Add(transaction);
                await _context.SaveChangesAsync();
            }

            TempData["SuccessMessage"] = $"Reserve item '{model.Name}' added to {model.Department} stock successfully.";
            return RedirectToAction(nameof(Index), new { department = model.Department });
        }

        // POST: /SpareParts/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(SparePart model, int? formId, int[]? compatibleAssetIds, IFormCollection formCollection)
        {
            if (_currentUserService.IsEndUser()) return Forbid();

            var part = await _context.SpareParts
                .Include(p => p.CompatibleAssets)
                .FirstOrDefaultAsync(p => p.Id == model.Id);
            if (part == null || !part.IsActive) return NotFound();

            if (_currentUserService.IsMaintenance())
            {
                var myTeam = _currentUserService.GetMaintenanceTeam() ?? MaintenanceTeams.IT;
                if (!string.Equals(part.Department, myTeam, StringComparison.OrdinalIgnoreCase))
                {
                    return Forbid();
                }
            }

            part.Name = model.Name;
            part.NameAr = model.NameAr;
            part.PartNumber = model.PartNumber;
            if (_currentUserService.IsAdmin())
            {
                part.Department = model.Department;
            }
            part.Category = model.Category;
            part.MinReserveQuantity = model.MinReserveQuantity;
            part.Unit = model.Unit;
            // Task 9: Cost removed across application except adding asset
            part.UnitCost = null;
            part.StorageLocation = model.StorageLocation;
            part.CanFunctionAsAsset = model.CanFunctionAsAsset;
            part.CompatibleEquipment = model.CompatibleEquipment;
            part.Notes = model.Notes;
            part.FormId = (formId.HasValue && formId.Value > 0) ? formId.Value : null;

            // Extract dynamic specs (Task 1)
            var specsDict = new Dictionary<string, string>();
            if (part.FormId.HasValue)
            {
                var selectedForm = await _context.SparePartForms.FindAsync(part.FormId.Value);
                if (selectedForm != null)
                {
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
                }
            }
            part.Specs = JsonSerializer.Serialize(specsDict);

            // Synchronize compatible devices (Task 2)
            var existingLinks = await _context.AssetCompatibleSpareParts.Where(cp => cp.SparePartId == part.Id).ToListAsync();
            var newIds = (compatibleAssetIds ?? Array.Empty<int>()).Where(id => id > 0).ToHashSet();

            // Remove unselected
            var toRemove = existingLinks.Where(l => !newIds.Contains(l.AssetId)).ToList();
            _context.AssetCompatibleSpareParts.RemoveRange(toRemove);

            // Add new
            var existingAssetIds = existingLinks.Select(l => l.AssetId).ToHashSet();
            foreach (var aid in newIds.Where(id => !existingAssetIds.Contains(id)))
            {
                _context.AssetCompatibleSpareParts.Add(new AssetCompatibleSparePart
                {
                    AssetId = aid,
                    SparePartId = part.Id,
                    AssignedAt = DateTime.Now
                });
            }

            part.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Reserve item '{part.Name}' updated successfully.";
            return RedirectToAction(nameof(Index), new { department = part.Department });
        }

        // POST: /SpareParts/Restock
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Restock(int id, int addedQuantity, string? notes, int? ticketId = null)
        {
            if (_currentUserService.IsEndUser()) return Forbid();

            var part = await _context.SpareParts.FindAsync(id);
            if (part == null || !part.IsActive) return NotFound();

            if (_currentUserService.IsMaintenance())
            {
                var myTeam = _currentUserService.GetMaintenanceTeam() ?? MaintenanceTeams.IT;
                if (!string.Equals(part.Department, myTeam, StringComparison.OrdinalIgnoreCase))
                {
                    return Forbid();
                }
            }

            if (addedQuantity <= 0)
            {
                TempData["ErrorMessage"] = "Restock quantity must be greater than zero.";
                return RedirectToAction(nameof(Index), new { department = part.Department });
            }

            part.Quantity += addedQuantity;
            part.UpdatedAt = DateTime.Now;

            int currentUserId = _currentUserService.GetUserId();

            var transaction = new SparePartTransaction
            {
                SparePartId = part.Id,
                TicketId = ticketId,
                UserId = currentUserId,
                TransactionType = "Replenishment",
                QuantityChanged = addedQuantity,
                QuantityAfter = part.Quantity,
                Notes = string.IsNullOrWhiteSpace(notes) ? "Stock replenished / received." : notes.Trim(),
                Timestamp = DateTime.Now
            };
            _context.SparePartTransactions.Add(transaction);

            // If linked to a ticket, log in ticket messages
            if (ticketId.HasValue)
            {
                var ticket = await _context.Tickets.FindAsync(ticketId.Value);
                if (ticket != null)
                {
                    var msg = new TicketMsg
                    {
                        TicketId = ticket.Id,
                        UserId = currentUserId,
                        Msg = $"📦 [RESERVE STOCK REPLENISHED]: +{addedQuantity} {part.Unit ?? "Piece"} added to {part.Name} ({part.Department} Reserve). Stock is now {part.Quantity}.",
                        TimeStamp = DateTime.Now
                    };
                    _context.TicketMessages.Add(msg);
                }
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Restocked +{addedQuantity} units of '{part.Name}'. Total stock is now {part.Quantity} {part.Unit}.";
            return RedirectToAction(nameof(Index), new { department = part.Department });
        }

        // POST: /SpareParts/CommissionAsAsset
        // Converts a dual-nature spare part (e.g. SSD, Monitor, Tablet, Dehumidifier) into an independent registered hospital asset
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CommissionAsAsset(
            int id,
            string hospitalNo,
            string? brand,
            string? modelNo,
            string? serialNo,
            int? roomId,
            int? custodianUserId,
            string? notes,
            int? ticketId = null)
        {
            if (_currentUserService.IsEndUser()) return Forbid();

            var part = await _context.SpareParts.FindAsync(id);
            if (part == null || !part.IsActive) return NotFound();

            if (_currentUserService.IsMaintenance())
            {
                var myTeam = _currentUserService.GetMaintenanceTeam() ?? MaintenanceTeams.IT;
                if (!string.Equals(part.Department, myTeam, StringComparison.OrdinalIgnoreCase))
                {
                    return Forbid();
                }
            }

            if (!part.CanFunctionAsAsset)
            {
                TempData["ErrorMessage"] = $"Item '{part.Name}' is not marked as having independent asset functionality.";
                return RedirectToAction(nameof(Index), new { department = part.Department });
            }

            if (part.Quantity < 1)
            {
                TempData["ErrorMessage"] = $"Cannot commission asset: '{part.Name}' is out of stock in reserve.";
                return RedirectToAction(nameof(Index), new { department = part.Department });
            }

            if (string.IsNullOrWhiteSpace(hospitalNo))
            {
                TempData["ErrorMessage"] = "Hospital Tag / Barcode Number is required to commission an asset.";
                return RedirectToAction(nameof(Index), new { department = part.Department });
            }

            var cleanHospitalNo = hospitalNo.Trim().ToUpperInvariant();
            var existing = await _context.Assets.AnyAsync(a => a.HospitalNo == cleanHospitalNo);
            if (existing)
            {
                TempData["ErrorMessage"] = $"Asset Tag '{cleanHospitalNo}' is already registered in the system. Please use a unique barcode.";
                return RedirectToAction(nameof(Index), new { department = part.Department });
            }

            int currentUserId = _currentUserService.GetUserId();

            // 1. Decrement reserve stock
            part.Quantity -= 1;
            part.UpdatedAt = DateTime.Now;

            // 2. Create the new independent Asset record
            var newAsset = new Asset
            {
                HospitalNo = cleanHospitalNo,
                SerialNo = !string.IsNullOrWhiteSpace(serialNo) ? serialNo.Trim() : $"SN-{cleanHospitalNo}",
                Brand = !string.IsNullOrWhiteSpace(brand) ? brand.Trim() : (!string.IsNullOrWhiteSpace(part.Name) ? part.Name : "Standard"),
                Type = part.Name,
                Status = "In Service",
                Price = part.UnitCost ?? 0m,
                BuyingDate = DateTime.Now,
                RoomId = roomId,
                UserId = custodianUserId,
                CustodyTypeId = custodianUserId.HasValue ? 1 : 2, // 1: Personal, 2: Shared
                MaintenanceTeam = part.Department,
                Specs = JsonSerializer.Serialize(new Dictionary<string, string>
                {
                    ["Origin"] = "Commissioned from Reserve Stock",
                    ["SourcePartId"] = part.Id.ToString(),
                    ["PartNumber"] = part.PartNumber ?? "N/A",
                    ["StorageOrigin"] = part.StorageLocation ?? "N/A",
                    ["Model"] = modelNo ?? "Standard",
                    ["CommissionNotes"] = notes ?? "None"
                })
            };

            _context.Assets.Add(newAsset);
            await _context.SaveChangesAsync();

            // 3. Record the transaction linking the spare part and the new asset
            var transaction = new SparePartTransaction
            {
                SparePartId = part.Id,
                AssetId = newAsset.Id,
                TicketId = ticketId,
                UserId = currentUserId,
                TransactionType = "Commissioned as Asset",
                QuantityChanged = -1,
                QuantityAfter = part.Quantity,
                Notes = $"Commissioned as independent hospital asset '{newAsset.HospitalNo}' (Assigned to: {(custodianUserId.HasValue ? "Custodian User #" + custodianUserId : "Shared Inventory")}). Notes: {notes ?? "None"}",
                Timestamp = DateTime.Now
            };
            _context.SparePartTransactions.Add(transaction);

            // 4. If linked to a ticket, record in ticket timeline
            if (ticketId.HasValue)
            {
                var ticket = await _context.Tickets.FindAsync(ticketId.Value);
                if (ticket != null)
                {
                    var msg = new TicketMsg
                    {
                        TicketId = ticket.Id,
                        UserId = currentUserId,
                        Msg = $"⭐ [INDEPENDENT ASSET COMMISSIONED]: Spare part '{part.Name}' (SKU: {part.PartNumber ?? "N/A"}) commissioned into standalone asset #{newAsset.HospitalNo} ({newAsset.Brand}). Linked to this ticket.",
                        TimeStamp = DateTime.Now
                    };
                    _context.TicketMessages.Add(msg);
                }
            }

            await _context.SaveChangesAsync();

            // 5. Notify if reserve stock hit low threshold
            if (part.IsLowStock)
            {
                await _notificationService.NotifyLowReserveStockAsync(part);
            }

            TempData["SuccessMessage"] = $"🎉 Successfully commissioned '{part.Name}' as new independent asset '{newAsset.HospitalNo}'! Reserve stock updated ({part.Quantity} remaining).";
            return RedirectToAction("Details", "Assets", new { id = newAsset.Id });
        }

        // GET: /SpareParts/Transactions
        public async Task<IActionResult> Transactions(int? sparePartId, int? ticketId, string? department)
        {
            if (_currentUserService.IsEndUser()) return Forbid();

            var query = _context.SparePartTransactions
                .Include(t => t.SparePart)
                .Include(t => t.User)
                .Include(t => t.Asset)
                .Include(t => t.Ticket)
                .AsQueryable();

            string? activeDept = department;
            if (_currentUserService.IsMaintenance())
            {
                var myTeam = _currentUserService.GetMaintenanceTeam() ?? MaintenanceTeams.IT;
                activeDept = myTeam;
                query = query.Where(t => t.SparePart != null && t.SparePart.Department == myTeam);
            }
            else if (!string.IsNullOrWhiteSpace(activeDept) && !string.Equals(activeDept, "ALL", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(t => t.SparePart != null && t.SparePart.Department == activeDept);
            }

            if (sparePartId.HasValue)
            {
                query = query.Where(t => t.SparePartId == sparePartId.Value);
            }

            if (ticketId.HasValue)
            {
                query = query.Where(t => t.TicketId == ticketId.Value);
            }

            var transactions = await query
                .OrderByDescending(t => t.Timestamp)
                .Take(200)
                .ToListAsync();

            SparePart? filterPart = null;
            if (sparePartId.HasValue)
            {
                filterPart = await _context.SpareParts.FindAsync(sparePartId.Value);
            }

            var vm = new SparePartTransactionsViewModel
            {
                Transactions = transactions,
                FilterPart = filterPart,
                TicketId = ticketId,
                Department = activeDept
            };

            return View(vm);
        }
    }
}
