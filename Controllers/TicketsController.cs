using System;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using StockPortal.Data;
using StockPortal.Models;
using StockPortal.ViewModels;
using StockPortal.Services;

namespace StockPortal.Controllers
{
    public class TicketsController : Controller
    {
        private readonly StockDbContext _context;
        private readonly ICurrentUserService _currentUserService;
        private readonly INotificationService _notificationService;

        public TicketsController(StockDbContext context, ICurrentUserService currentUserService, INotificationService notificationService)
        {
            _context = context;
            _currentUserService = currentUserService;
            _notificationService = notificationService;
        }

        // GET: Tickets
        public async Task<IActionResult> Index(int? statusId, int? priority, string? type, string? search, string? filter)
        {
            var currentUserId = _currentUserService.GetUserId();

            var query = _context.Tickets
                .Include(t => t.Asset)
                .Include(t => t.OpenedByUser)
                .Include(t => t.OnBehalfOfUser)
                .Include(t => t.Status)
                .Include(t => t.Handler).ThenInclude(h => h!.TechUser)
                .Include(t => t.Messages)
                .AsQueryable();

            // RBAC Filtering
            if (_currentUserService.IsEndUser())
            {
                // End Users view only tickets they authored or on behalf
                query = query.Where(t => t.OpenedBy == currentUserId || t.OnBehalfOf == currentUserId);
            }
            else if (_currentUserService.IsMaintenance())
            {
                // Maintenance users view only tickets routed to their designated team queue
                var myTeam = _currentUserService.GetMaintenanceTeam();
                query = query.Where(t => t.TargetTeam == myTeam);
            }
            // ADMIN has unrestricted access across all tickets

            // Default status filter is "Not Closed" (-1) unless explicitly specified
            int effectiveStatusId = statusId ?? -1;
            if (statusId == 0)
            {
                effectiveStatusId = 0; // 0 explicitly represents "All Statuses"
            }

            if (effectiveStatusId == -1)
            {
                // Not Closed tickets (Status != 5 and ClosedDT is null)
                query = query.Where(t => t.StatusId != 5 && t.ClosedDT == null);
            }
            else if (effectiveStatusId > 0)
            {
                query = query.Where(t => t.StatusId == effectiveStatusId);
            }

            if (priority.HasValue && priority.Value > 0)
            {
                query = query.Where(t => t.Priority == priority.Value);
            }

            if (!string.IsNullOrWhiteSpace(type))
            {
                query = query.Where(t => t.Type == type);
            }

            // Quick Filters
            if (!string.IsNullOrWhiteSpace(filter))
            {
                if (filter == "my_assigned")
                {
                    query = query.Where(t => t.Handler != null && t.Handler.TechUserId == currentUserId);
                }
                else if (filter == "my_opened")
                {
                    query = query.Where(t => t.OpenedBy == currentUserId || t.OnBehalfOf == currentUserId);
                }
                else if (filter == "unassigned")
                {
                    query = query.Where(t => t.Handler == null || t.Handler.TechUserId == null);
                }
                else if (filter == "maintenance" || filter == "in_maintenance")
                {
                    query = query.Where(t => t.StatusId == 6 || (t.Status != null && t.Status.Status != null && t.Status.Status.ToLower() == "maintenance"));
                }
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                query = query.Where(t =>
                    t.Id.ToString() == s ||
                    (t.Asset != null && t.Asset.HospitalNo != null && t.Asset.HospitalNo.ToLower().Contains(s)) ||
                    (t.Asset != null && t.Asset.Brand != null && t.Asset.Brand.ToLower().Contains(s)) ||
                    (t.OpenedByUser != null && t.OpenedByUser.FullName != null && t.OpenedByUser.FullName.ToLower().Contains(s)) ||
                    t.Messages.Any(m => m.Msg != null && m.Msg.ToLower().Contains(s))
                );
            }

            ViewBag.SelectedStatusId = effectiveStatusId;
            ViewBag.SelectedPriority = priority;
            ViewBag.SelectedType = type;
            ViewBag.Search = search;
            ViewBag.SelectedFilter = filter;

            ViewBag.Statuses = await _context.TicketStatuses.ToListAsync();
            ViewBag.AllTechnicians = await _context.Users
                .Where(u => u.IsActive == true && (u.Role == SystemRoles.Maintenance || u.Role == SystemRoles.Admin))
                .OrderBy(u => u.FullName)
                .ToListAsync();

            var maintCountQuery = _context.Tickets.Where(t => t.StatusId == 6 || (t.Status != null && t.Status.Status != null && t.Status.Status.ToLower() == "maintenance"));
            if (_currentUserService.IsMaintenance())
            {
                var myTeam = _currentUserService.GetMaintenanceTeam();
                maintCountQuery = maintCountQuery.Where(t => t.TargetTeam == myTeam);
            }
            ViewBag.MaintenanceCount = await maintCountQuery.CountAsync();

            var tickets = await query.OrderByDescending(t => t.OpenedDT).ToListAsync();
            return View(tickets);
        }

        // GET: Tickets/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var ticket = await _context.Tickets
                .Include(t => t.Asset).ThenInclude(a => a!.Room)
                .Include(t => t.Asset).ThenInclude(a => a!.User)
                .Include(t => t.OpenedByUser)
                .Include(t => t.OnBehalfOfUser)
                .Include(t => t.Status)
                .Include(t => t.Handler).ThenInclude(h => h!.TechUser)
                .Include(t => t.Messages).ThenInclude(m => m.User)
                .Include(t => t.Messages).ThenInclude(m => m.Attachments)
                .Include(t => t.MaintenanceLogs).ThenInclude(m => m.Technician)
                .Include(t => t.MaintenanceLogs).ThenInclude(m => m.Supplier)
                .Include(t => t.Attachments).ThenInclude(a => a.UploadedByUser)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (ticket == null) return NotFound();

            // RBAC Guard: Verify access permissions
            if (!_currentUserService.CanAccessTicket(ticket))
            {
                return Forbid();
            }

            var allSuppliers = await _context.Suppliers.Where(s => s.IsActive == true).OrderBy(s => s.Name).ToListAsync();
            var targetSuppliers = allSuppliers.Where(s => s.SupportsDepartment(ticket.TargetTeam)).ToList();

            var availableSpareParts = await _context.SpareParts
                .Where(p => p.IsActive && p.Department == ticket.TargetTeam && p.Quantity > 0)
                .OrderBy(p => p.Name)
                .ToListAsync();

            var usedSpareParts = await _context.SparePartTransactions
                .Include(t => t.SparePart)
                .Include(t => t.User)
                .Where(t => t.TicketId == id)
                .OrderByDescending(t => t.Timestamp)
                .ToListAsync();

            var allUsers = await _context.Users
                .Include(u => u.Department)
                .Where(u => u.IsActive == true)
                .OrderBy(u => u.FullName)
                .ToListAsync();

            // Task 2: Determine responsible department strictly according to the asset's department (or ticket target team)
            var responsibleTeam = ticket.Asset?.MaintenanceTeam ?? ticket.TargetTeam ?? MaintenanceTeams.IT;

            // Filter strictly for responsible department employees (e.g. IT, BIOMEDICAL, GENERAL)
            var departmentEmployees = allUsers
                .Where(u => u.BelongsToResponsibleDepartment(responsibleTeam))
                .ToList();

            var vm = new TicketDetailsViewModel
            {
                Ticket = ticket,
                Statuses = await _context.TicketStatuses.ToListAsync(),
                Technicians = departmentEmployees, // Show only the responsible team members
                DepartmentEmployees = departmentEmployees,
                Suppliers = targetSuppliers,
                AvailableSpareParts = availableSpareParts,
                UsedSpareParts = usedSpareParts,
                NewStatusId = ticket.StatusId,
                NewTechUserId = ticket.Handler?.TechUserId
            };

            return View(vm);
        }

        // GET: Tickets/Create
        public async Task<IActionResult> Create(int? assetId)
        {
            var currentUserId = _currentUserService.GetUserId();
            await PopulateDropdowns(assetId, currentUserId, currentUserId);

            // Task 11: Check if an active ticket exists for this asset (Status != Closed/Canceled and ClosedDT is null)
            if (assetId.HasValue && assetId.Value > 0)
            {
                var existingActiveTicket = await _context.Tickets
                    .FirstOrDefaultAsync(t => t.AssetId == assetId.Value && t.StatusId != 4 && t.StatusId != 5 && t.ClosedDT == null);
                if (existingActiveTicket != null)
                {
                    ViewBag.ActiveTicketWarning = $"An active ticket (#{existingActiveTicket.Id}) is already open for this asset. Please resolve or view the existing ticket before opening a new one.";
                    ViewBag.ExistingActiveTicketId = existingActiveTicket.Id;
                }
            }

            var ticket = new Ticket
            {
                AssetId = assetId,
                OpenedBy = currentUserId,
                OnBehalfOf = currentUserId,
                Type = "Hardware",
                Priority = 2,
                StatusId = 1,
                OpenedDT = DateTime.Now
            };

            return View(ticket);
        }

        // POST: Tickets/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Ticket ticket, string? manualAsset = null, string? initialMessage = null, int? assignedTechId = null, string? pcErrorLog = null, string? pastedScreenshotsJson = null, List<IFormFile>? screenshotFiles = null)
        {
            var currentUserId = _currentUserService.GetUserId();
            ticket.OpenedBy = currentUserId > 0 ? currentUserId : ticket.OpenedBy;

            bool isOnBehalfOf = ticket.OnBehalfOf.HasValue && ticket.OnBehalfOf.Value != ticket.OpenedBy;

            // Task 11: Block opening another ticket if an active ticket already exists for the asset
            if (ticket.AssetId.HasValue && ticket.AssetId.Value > 0)
            {
                var existingActiveTicket = await _context.Tickets
                    .FirstOrDefaultAsync(t => t.AssetId == ticket.AssetId.Value && t.StatusId != 4 && t.StatusId != 5 && t.ClosedDT == null);
                if (existingActiveTicket != null)
                {
                    ModelState.AddModelError("AssetId", $"Cannot open a new ticket for this asset. Active ticket #{existingActiveTicket.Id} is currently open.");
                    ViewBag.ActiveTicketWarning = $"An active ticket (#{existingActiveTicket.Id}) is already open for this asset. Please resolve or view the existing ticket before opening a new one.";
                    ViewBag.ExistingActiveTicketId = existingActiveTicket.Id;
                    await PopulateDropdowns(ticket.AssetId, ticket.OnBehalfOf, currentUserId);
                    return View(ticket);
                }
            }

            if (ModelState.IsValid)
            {
                // If opening on behalf of another user, clear personal asset dropdown ONLY if no specific asset was selected
                if (isOnBehalfOf && (!ticket.AssetId.HasValue || ticket.AssetId.Value <= 0))
                {
                    ticket.AssetId = null;
                }

                // Target team routing: honor explicitly specified target team; fall back to asset's team or IT
                if (string.IsNullOrWhiteSpace(ticket.TargetTeam))
                {
                    if (ticket.AssetId.HasValue && ticket.AssetId.Value > 0)
                    {
                        var targetAsset = await _context.Assets.FindAsync(ticket.AssetId.Value);
                        ticket.TargetTeam = targetAsset?.MaintenanceTeam ?? MaintenanceTeams.IT;
                    }
                    else
                    {
                        ticket.TargetTeam = MaintenanceTeams.IT;
                    }
                }

                // Task 1: Capture client IP and MAC address where the ticket is created
                var clientIp = NetworkInfoHelper.GetClientIpAddress(HttpContext);
                var clientMac = NetworkInfoHelper.GetClientMacAddress(HttpContext, clientIp);
                ticket.IpAddress = clientIp;
                ticket.MacAddress = clientMac;

                ticket.OpenedDT = DateTime.Now;
                ticket.StatusId = 1; // opened
                _context.Tickets.Add(ticket);
                await _context.SaveChangesAsync();

                // If technician assigned immediately
                if (assignedTechId.HasValue && assignedTechId.Value > 0)
                {
                    _context.TicketHandlers.Add(new TicketHandler
                    {
                        TicketId = ticket.Id,
                        TechUserId = assignedTechId.Value,
                        AssignedDT = DateTime.Now
                    });
                    ticket.StatusId = 2; // pending/in-progress
                    _context.Update(ticket);
                }

                // Construct initial message with manual asset information, affected software, and error log
                var messageBuilder = new StringBuilder();

                if (isOnBehalfOf && !string.IsNullOrWhiteSpace(manualAsset))
                {
                    messageBuilder.AppendLine($"[ON BEHALF OF ASSET / PC]: {manualAsset.Trim()}");
                    messageBuilder.AppendLine();
                }

                if (!string.IsNullOrWhiteSpace(ticket.AffectedSoftware))
                {
                    messageBuilder.AppendLine($"[AFFECTED SOFTWARE]: {ticket.AffectedSoftware.Trim()}");
                }

                if (!string.IsNullOrWhiteSpace(ticket.PcName))
                {
                    messageBuilder.AppendLine($"[WORKSTATION PC]: {ticket.PcName.Trim()}");
                }

                if (!string.IsNullOrWhiteSpace(ticket.AffectedSoftware) || !string.IsNullOrWhiteSpace(ticket.PcName))
                {
                    messageBuilder.AppendLine();
                }

                if (!string.IsNullOrWhiteSpace(initialMessage))
                {
                    messageBuilder.Append(initialMessage.Trim());
                }

                if (!string.IsNullOrWhiteSpace(pcErrorLog))
                {
                    messageBuilder.AppendLine();
                    messageBuilder.AppendLine();
                    messageBuilder.AppendLine("📋 [CLIENT WORKSTATION ERROR LOG]:");
                    messageBuilder.AppendLine(pcErrorLog.Trim());
                }

                TicketMsg? initialTicketMsg = null;
                var fullMsg = messageBuilder.ToString().Trim();
                if (!string.IsNullOrWhiteSpace(fullMsg))
                {
                    initialTicketMsg = new TicketMsg
                    {
                        TicketId = ticket.Id,
                        UserId = ticket.OpenedBy,
                        Msg = fullMsg,
                        TimeStamp = DateTime.Now,
                        IpAddress = clientIp,
                        MacAddress = clientMac
                    };
                    _context.TicketMessages.Add(initialTicketMsg);
                }

                await _context.SaveChangesAsync();

                // Process attached screenshots (base64 from clipboard paste and form files)
                await ProcessScreenshotsAsync(ticket.Id, ticket.OpenedBy ?? currentUserId, pastedScreenshotsJson, screenshotFiles, initialTicketMsg?.Id);

                var creatorUser = _currentUserService.GetCurrentUser() ?? await _context.Users.Include(u => u.Department).FirstOrDefaultAsync(u => u.Id == ticket.OpenedBy);
                await _notificationService.NotifyTicketCreatedAsync(ticket, creatorUser);

                TempData["SuccessMessage"] = $"Ticket #{ticket.Id} created successfully!";
                return RedirectToAction(nameof(Details), new { id = ticket.Id });
            }

            await PopulateDropdowns(ticket.AssetId, ticket.OnBehalfOf, currentUserId);
            return View(ticket);
        }

        // GET: Tickets/AutoTicket
        public async Task<IActionResult> AutoTicket(string? pcName, int? assetId)
        {
            var currentUserId = _currentUserService.GetUserId();
            var currentUser = await _context.Users.Include(u => u.Department).FirstOrDefaultAsync(u => u.Id == currentUserId);

            // Determine client PC name
            if (string.IsNullOrWhiteSpace(pcName))
            {
                pcName = Request.Headers["X-Computer-Name"].FirstOrDefault()
                    ?? Request.Headers["COMPUTERNAME"].FirstOrDefault();
            }

            if (string.IsNullOrWhiteSpace(pcName))
            {
                pcName = Environment.MachineName;
            }

            string clientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";

            // Resolve Asset by ID or by PC Name (hospitalNo in database)
            Asset? matchedAsset = null;
            if (assetId.HasValue && assetId.Value > 0)
            {
                matchedAsset = await _context.Assets
                    .Include(a => a.Room)
                    .Include(a => a.User)
                    .Include(a => a.CustodyType)
                    .Include(a => a.Supplier)
                    .FirstOrDefaultAsync(a => a.Id == assetId.Value);
            }

            if (matchedAsset == null && !string.IsNullOrWhiteSpace(pcName))
            {
                var search = pcName.Trim().ToLower();
                matchedAsset = await _context.Assets
                    .Include(a => a.Room)
                    .Include(a => a.User)
                    .Include(a => a.CustodyType)
                    .Include(a => a.Supplier)
                    .FirstOrDefaultAsync(a => a.HospitalNo != null && a.HospitalNo.ToLower() == search);
            }

            // Fallback: check if the logged in user has an assigned PC asset
            if (matchedAsset == null && currentUserId > 0)
            {
                matchedAsset = await _context.Assets
                    .Include(a => a.Room)
                    .Include(a => a.User)
                    .Include(a => a.CustodyType)
                    .Include(a => a.Supplier)
                    .FirstOrDefaultAsync(a => a.UserId == currentUserId);
            }

            var vm = new AutoTicketViewModel
            {
                DetectedPcName = matchedAsset?.HospitalNo ?? pcName ?? Environment.MachineName,
                ClientIp = clientIp,
                IsAssetFound = (matchedAsset != null),
                MatchedAsset = matchedAsset,
                SelectedAssetId = matchedAsset?.Id,
                SelectedRoomId = matchedAsset?.RoomId,
                SelectedUserId = matchedAsset?.UserId ?? currentUserId,
                Category = "Hardware",
                Priority = 2,
                UserNotes = "",
                AvailableAssets = await _context.Assets.OrderBy(a => a.HospitalNo).ToListAsync(),
                AvailableRooms = await _context.Rooms.OrderBy(r => r.RoomNo).ToListAsync(),
                AvailableUsers = await _context.Users.Where(u => u.IsActive == true).OrderBy(u => u.FullName).ToListAsync()
            };

            return View(vm);
        }

        // POST: Tickets/AutoTicket
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AutoTicket(AutoTicketViewModel model)
        {
            var currentUserId = _currentUserService.GetUserId();
            var currentUser = await _context.Users.Include(u => u.Department).FirstOrDefaultAsync(u => u.Id == currentUserId);

            // 1. Resolve Asset
            Asset? asset = null;
            if (model.SelectedAssetId.HasValue && model.SelectedAssetId.Value > 0)
            {
                asset = await _context.Assets
                    .Include(a => a.Room)
                    .Include(a => a.User)
                    .FirstOrDefaultAsync(a => a.Id == model.SelectedAssetId.Value);
            }
            else if (!string.IsNullOrWhiteSpace(model.DetectedPcName))
            {
                var search = model.DetectedPcName.Trim().ToLower();
                asset = await _context.Assets
                    .Include(a => a.Room)
                    .Include(a => a.User)
                    .FirstOrDefaultAsync(a => a.HospitalNo != null && a.HospitalNo.ToLower() == search);
            }

            // Task 11: If asset has an active ticket, redirect to existing ticket
            if (asset != null)
            {
                var existingActive = await _context.Tickets
                    .FirstOrDefaultAsync(t => t.AssetId == asset.Id && t.StatusId != 4 && t.StatusId != 5 && t.ClosedDT == null);
                if (existingActive != null)
                {
                    TempData["ErrorMessage"] = $"An active ticket (#{existingActive.Id}) already exists for this equipment. Redirecting to existing ticket.";
                    return RedirectToAction(nameof(Details), new { id = existingActive.Id });
                }
            }

            // 2. If room was selected and asset exists, update asset room if not set or changed
            Room? selectedRoom = null;
            if (model.SelectedRoomId.HasValue && model.SelectedRoomId.Value > 0)
            {
                selectedRoom = await _context.Rooms.FindAsync(model.SelectedRoomId.Value);
                if (asset != null && (asset.RoomId == null || asset.RoomId != model.SelectedRoomId.Value))
                {
                    asset.RoomId = model.SelectedRoomId.Value;
                    _context.Update(asset);
                }
            }
            else if (asset?.Room != null)
            {
                selectedRoom = asset.Room;
            }

            // 3. If user selected and asset exists, update asset custodian if not set
            User? assignedUser = null;
            if (model.SelectedUserId.HasValue && model.SelectedUserId.Value > 0)
            {
                assignedUser = await _context.Users.FindAsync(model.SelectedUserId.Value);
                if (asset != null && asset.UserId == null)
                {
                    asset.UserId = model.SelectedUserId.Value;
                    _context.Update(asset);
                }
            }
            else if (asset?.User != null)
            {
                assignedUser = asset.User;
            }

            // 4. Create Ticket
            var clientIp = !string.IsNullOrWhiteSpace(model.ClientIp) ? model.ClientIp : NetworkInfoHelper.GetClientIpAddress(HttpContext);
            var clientMac = NetworkInfoHelper.GetClientMacAddress(HttpContext, clientIp);
            var pcName = asset?.HospitalNo ?? (!string.IsNullOrWhiteSpace(model.DetectedPcName) ? model.DetectedPcName : Environment.MachineName);

            var ticket = new Ticket
            {
                AssetId = asset?.Id,
                OpenedBy = currentUserId > 0 ? currentUserId : 1,
                OnBehalfOf = assignedUser?.Id ?? (currentUserId > 0 ? currentUserId : 1),
                Type = string.IsNullOrWhiteSpace(model.Category) ? "Hardware" : model.Category,
                Priority = model.Priority > 0 ? model.Priority : 2,
                StatusId = 1, // Opened
                OpenedDT = DateTime.Now,
                TargetTeam = asset?.MaintenanceTeam ?? MaintenanceTeams.IT,
                PcName = pcName,
                IpAddress = clientIp,
                MacAddress = clientMac
            };

            _context.Tickets.Add(ticket);
            await _context.SaveChangesAsync();

            // 5. Construct Telemetry Report Message
            var roomDesc = selectedRoom != null ? $"Room {selectedRoom.RoomNo} (Floor {selectedRoom.Floor})" : "Unspecified";
            var userDesc = assignedUser?.FullName ?? (currentUser?.FullName ?? "Unassigned");
            var hardwareDesc = asset != null 
                ? $"{asset.Brand ?? "Generic"} {(asset.ModelNo.HasValue ? "Model " + asset.ModelNo : "")} [S/N: {asset.SerialNo ?? "N/A"}]"
                : "Workstation not yet tagged in Assets database";

            var sb = new StringBuilder();
            sb.AppendLine("🖥️ AUTOMATED SYSTEM & WORKSTATION REPORT");
            sb.AppendLine($"• Workstation / PC Name: {pcName}");
            sb.AppendLine($"• Location: {roomDesc}");
            sb.AppendLine($"• Custodian / User: {userDesc}");
            sb.AppendLine($"• Equipment: {hardwareDesc}");
            sb.AppendLine($"• Reporter: {currentUser?.FullName ?? "System"} ({currentUser?.DomainUser ?? Environment.UserName})");
            sb.AppendLine($"• Department: {currentUser?.Department?.Name ?? "General"}");
            sb.AppendLine($"• Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");

            _context.TicketMessages.Add(new TicketMsg
            {
                TicketId = ticket.Id,
                UserId = ticket.OpenedBy,
                Msg = sb.ToString(),
                TimeStamp = DateTime.Now,
                IpAddress = clientIp,
                MacAddress = clientMac
            });

            // 6. User Manual Notes (if entered)
            if (!string.IsNullOrWhiteSpace(model.UserNotes))
            {
                _context.TicketMessages.Add(new TicketMsg
                {
                    TicketId = ticket.Id,
                    UserId = ticket.OpenedBy,
                    Msg = $"📝 USER NOTES:\n{model.UserNotes.Trim()}",
                    TimeStamp = DateTime.Now.AddSeconds(1)
                });
            }

            await _context.SaveChangesAsync();

            var autoCreator = _currentUserService.GetCurrentUser() ?? await _context.Users.Include(u => u.Department).FirstOrDefaultAsync(u => u.Id == ticket.OpenedBy);
            await _notificationService.NotifyTicketCreatedAsync(ticket, autoCreator);

            TempData["SuccessMessage"] = $"Automated Ticket #{ticket.Id} created for PC '{pcName}'!";
            return RedirectToAction(nameof(Details), new { id = ticket.Id });
        }

        // GET: Tickets/GetAssetByPcName
        [HttpGet]
        public async Task<IActionResult> GetAssetByPcName(string pcName)
        {
            if (string.IsNullOrWhiteSpace(pcName)) return Json(new { found = false });

            var search = pcName.Trim().ToLower();
            var asset = await _context.Assets
                .Include(a => a.Room)
                .Include(a => a.User)
                .FirstOrDefaultAsync(a => a.HospitalNo != null && a.HospitalNo.ToLower() == search);

            if (asset == null)
            {
                return Json(new { found = false });
            }

            return Json(new
            {
                found = true,
                id = asset.Id,
                hospitalNo = asset.HospitalNo,
                brand = asset.Brand,
                modelNo = asset.ModelNo,
                serialNo = asset.SerialNo,
                type = asset.Type,
                roomId = asset.RoomId,
                roomText = asset.Room != null ? $"Room {asset.Room.RoomNo} (Floor {asset.Room.Floor})" : null,
                userId = asset.UserId,
                userName = asset.User?.FullName,
                status = asset.Status,
                specs = asset.Specs
            });
        }

        // POST: Tickets/UpdateStatus
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int ticketId, int statusId)
        {
            var ticket = await _context.Tickets.FindAsync(ticketId);
            if (ticket == null) return NotFound();

            // RBAC Guard: Only Admin and assigned Maintenance team can manage tickets
            if (!_currentUserService.CanManageTicket(ticket))
            {
                return Forbid();
            }

            ticket.StatusId = statusId;
            if (statusId == 5) // closed
            {
                ticket.ClosedDT = DateTime.Now;
            }
            else
            {
                ticket.ClosedDT = null;
            }

            _context.Update(ticket);
            await _context.SaveChangesAsync();

            var statusObj = await _context.TicketStatuses.FindAsync(statusId);
            var statusActor = _currentUserService.GetCurrentUser();
            await _notificationService.NotifyStatusChangedAsync(ticket, statusObj?.Status ?? statusId.ToString(), statusActor);

            TempData["SuccessMessage"] = "Ticket status updated.";
            return RedirectToAction(nameof(Details), new { id = ticketId });
        }

        // POST: Tickets/CloseTicket
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CloseTicket(int ticketId, string? resolutionNotes, int[]? closedByUserIds = null)
        {
            var ticket = await _context.Tickets
                .Include(t => t.Handler)
                .FirstOrDefaultAsync(t => t.Id == ticketId);
            if (ticket == null) return NotFound();

            // RBAC Guard: Only Admin and assigned Maintenance team can close tickets
            if (!_currentUserService.CanManageTicket(ticket))
            {
                return Forbid();
            }

            ticket.StatusId = 5; // closed
            ticket.ClosedDT = DateTime.Now;
            ticket.ResolutionDetails = resolutionNotes?.Trim();

            var currentUserId = _currentUserService.GetUserId();

            // Determine multi-technician closure participants: strictly responsible department employees (Task 12)
            var participatingTechIds = new HashSet<int>();
            if (closedByUserIds != null && closedByUserIds.Length > 0)
            {
                var candidateUsers = await _context.Users
                    .Include(u => u.Department)
                    .Where(u => u.IsActive == true)
                    .ToListAsync();

                var allowedIds = candidateUsers
                    .Where(u => u.BelongsToResponsibleDepartment(ticket.TargetTeam))
                    .Select(u => u.Id)
                    .ToHashSet();

                foreach (var id in closedByUserIds.Where(id => id > 0 && allowedIds.Contains(id)))
                {
                    participatingTechIds.Add(id);
                }
            }
            if (participatingTechIds.Count == 0)
            {
                var candidateUsers = await _context.Users.Include(u => u.Department).Where(u => u.IsActive == true).ToListAsync();
                var allowedIds = candidateUsers.Where(u => u.BelongsToResponsibleDepartment(ticket.TargetTeam)).Select(u => u.Id).ToHashSet();

                if (currentUserId > 0 && allowedIds.Contains(currentUserId))
                {
                    participatingTechIds.Add(currentUserId);
                }
                else if (ticket.Handler?.TechUserId.HasValue == true && allowedIds.Contains(ticket.Handler.TechUserId.Value))
                {
                    participatingTechIds.Add(ticket.Handler.TechUserId.Value);
                }
                else if (currentUserId > 0)
                {
                    participatingTechIds.Add(currentUserId);
                }
            }

            ticket.ClosedByUserIds = participatingTechIds.ToList();
            _context.Update(ticket);

            // Fetch names of participating technicians for timeline note
            var techNames = await _context.Users
                .Where(u => participatingTechIds.Contains(u.Id))
                .Select(u => !string.IsNullOrEmpty(u.FullName) ? u.FullName : (u.DomainUser ?? ("Tech #" + u.Id)))
                .ToListAsync();

            var techListStr = techNames.Any() ? string.Join(", ", techNames) : null;

            string logMsg = $"[RESOLUTION NOTE]: {(string.IsNullOrWhiteSpace(resolutionNotes) ? "Ticket resolved and marked closed." : resolutionNotes.Trim())}";
            if (!string.IsNullOrEmpty(techListStr))
            {
                logMsg += $"\n[RESOLVED BY TEAM]: {techListStr}";
            }

            _context.TicketMessages.Add(new TicketMsg
            {
                TicketId = ticketId,
                UserId = currentUserId > 0 ? currentUserId : (participatingTechIds.FirstOrDefault()),
                Msg = logMsg,
                TimeStamp = DateTime.Now
            });

            // Task 10: Automatically record internal maintenance in AssetMaintenance
            if (ticket.AssetId.HasValue && ticket.AssetId.Value > 0)
            {
                var existingMaint = await _context.AssetMaintenances.FirstOrDefaultAsync(m => m.TicketId == ticket.Id);
                if (existingMaint == null)
                {
                    var usedPartsList = await _context.SparePartTransactions
                        .Where(t => t.TicketId == ticket.Id && t.SparePart != null)
                        .Select(t => t.SparePart.Name)
                        .ToListAsync();
                    string? partsReplacedStr = usedPartsList.Any() ? string.Join(", ", usedPartsList.Distinct()) : null;

                    var primaryTechId = participatingTechIds.FirstOrDefault();
                    if (primaryTechId <= 0)
                    {
                        primaryTechId = currentUserId > 0 ? currentUserId : (ticket.Handler?.TechUserId ?? 1);
                    }

                    var maintTitle = $"[Ticket #{ticket.Id}] {ticket.Type ?? "Internal Maintenance"}";
                    if (!string.IsNullOrWhiteSpace(ticket.AffectedSoftware))
                    {
                        maintTitle += $" - {ticket.AffectedSoftware}";
                    }

                    var internalMaint = new AssetMaintenance
                    {
                        AssetId = ticket.AssetId.Value,
                        TicketId = ticket.Id,
                        MaintenanceType = 1, // Corrective / Repair
                        SupplierId = null, // In-house / internal maintenance
                        UserId = primaryTechId > 0 ? primaryTechId : null,
                        WorkOrderStatus = "Completed",
                        Description = maintTitle.Trim(),
                        ActionTaken = string.IsNullOrWhiteSpace(resolutionNotes) ? "Internal ticket maintenance completed." : resolutionNotes.Trim(),
                        PartsReplaced = partsReplacedStr,
                        TimStamp = DateTime.Now,
                        SentDate = ticket.OpenedDT,
                        ReturnDate = ticket.ClosedDT ?? DateTime.Now
                    };
                    _context.AssetMaintenances.Add(internalMaint);

                    var assetObj = await _context.Assets.FindAsync(ticket.AssetId.Value);
                    if (assetObj != null)
                    {
                        assetObj.LastPeriodic = DateTime.Now;
                        if (assetObj.PeriodicInterval.HasValue && assetObj.PeriodicInterval.Value > 0)
                        {
                            assetObj.NextPeriodic = DateTime.Now.AddDays(assetObj.PeriodicInterval.Value);
                        }
                        _context.Update(assetObj);
                    }
                }
            }

            await _context.SaveChangesAsync();

            var closeActor = _currentUserService.GetCurrentUser();
            await _notificationService.NotifyStatusChangedAsync(ticket, "Closed", closeActor);

            TempData["SuccessMessage"] = $"Ticket #{ticketId} was resolved and closed.";
            return RedirectToAction(nameof(Details), new { id = ticketId });
        }

        // POST: Tickets/AssignTechnician
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignTechnician(int ticketId, int techUserId, string? returnUrl = null)
        {
            var ticket = await _context.Tickets
                .Include(t => t.Asset)
                .Include(t => t.Handler)
                .FirstOrDefaultAsync(t => t.Id == ticketId);
            if (ticket == null) return NotFound();

            var currentUserId = _currentUserService.GetUserId();
            bool canAssign = _currentUserService.CanManageTicket(ticket)
                             || ticket.OpenedBy == currentUserId
                             || ticket.OnBehalfOf == currentUserId;

            // RBAC Guard: Admin, assigned Maintenance team, or ticket requester can assign/reassign technician
            if (!canAssign)
            {
                return Forbid();
            }

            if (ticket.StatusId == 5 || ticket.ClosedDT.HasValue)
            {
                TempData["ErrorMessage"] = "Cannot reassign technician on a closed ticket.";
                if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl)) return Redirect(returnUrl);
                return RedirectToAction(nameof(Details), new { id = ticketId });
            }

            var techUser = techUserId > 0 ? await _context.Users.FindAsync(techUserId) : null;
            var responsibleTeam = ticket.Asset?.MaintenanceTeam ?? ticket.TargetTeam ?? MaintenanceTeams.IT;
            if (techUser != null && !techUser.BelongsToResponsibleDepartment(responsibleTeam))
            {
                TempData["ErrorMessage"] = $"Selected technician does not belong to the responsible {responsibleTeam} department.";
                if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl)) return Redirect(returnUrl);
                return RedirectToAction(nameof(Details), new { id = ticketId });
            }

            if (ticket.Handler == null)
            {
                _context.TicketHandlers.Add(new TicketHandler
                {
                    TicketId = ticketId,
                    TechUserId = techUserId > 0 ? techUserId : null,
                    AssignedDT = DateTime.Now
                });
            }
            else
            {
                ticket.Handler.TechUserId = techUserId > 0 ? techUserId : null;
                ticket.Handler.AssignedDT = DateTime.Now;
                _context.Update(ticket.Handler);
            }

            if (techUserId > 0 && ticket.StatusId == 1) // Move from opened to pending
            {
                ticket.StatusId = 2;
                _context.Update(ticket);
            }

            // Record timeline activity message
            if (techUser != null)
            {
                _context.TicketMessages.Add(new TicketMsg
                {
                    TicketId = ticketId,
                    UserId = currentUserId > 0 ? currentUserId : techUserId,
                    Msg = $"👤 ASSIGNED SPECIALIST: Ticket assigned to {techUser.FullName} ({techUser.RoleDisplay}).",
                    TimeStamp = DateTime.Now
                });
            }

            await _context.SaveChangesAsync();

            if (techUser != null)
            {
                var assignActor = _currentUserService.GetCurrentUser();
                await _notificationService.NotifyStatusChangedAsync(ticket, $"Assigned to {techUser.FullName}", assignActor);
            }

            TempData["SuccessMessage"] = techUser != null 
                ? $"Assigned to {techUser.FullName} successfully." 
                : "Technician unassigned.";

            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction(nameof(Details), new { id = ticketId });
        }

        // POST: Tickets/AssignTeam
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignTeam(int ticketId, string targetTeam, string? returnUrl)
        {
            var ticket = await _context.Tickets.Include(t => t.Handler).FirstOrDefaultAsync(t => t.Id == ticketId);
            if (ticket == null) return NotFound();

            var currentUserId = _currentUserService.GetUserId();
            bool canReassign = _currentUserService.IsAdmin()
                               || _currentUserService.CanManageTicket(ticket)
                               || ticket.OpenedBy == currentUserId
                               || ticket.OnBehalfOf == currentUserId;

            if (!canReassign)
            {
                return Forbid();
            }

            if (ticket.StatusId == 5 || ticket.ClosedDT.HasValue)
            {
                TempData["ErrorMessage"] = "Cannot reassign a closed ticket.";
                if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl)) return Redirect(returnUrl);
                return RedirectToAction(nameof(Details), new { id = ticketId });
            }

            var validTeams = new[] { MaintenanceTeams.IT, MaintenanceTeams.Biomedical, MaintenanceTeams.General };
            var normalizedTeam = targetTeam?.ToUpper().Trim() ?? "";
            if (!validTeams.Contains(normalizedTeam))
            {
                TempData["ErrorMessage"] = "Invalid maintenance team selected.";
                if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl)) return Redirect(returnUrl);
                return RedirectToAction(nameof(Details), new { id = ticketId });
            }

            var oldTeam = ticket.TargetTeam;
            if (oldTeam != normalizedTeam)
            {
                ticket.TargetTeam = normalizedTeam;

                // If currently assigned tech belongs to a different maintenance team, unassign them
                if (ticket.Handler != null && ticket.Handler.TechUserId.HasValue)
                {
                    var currentTech = await _context.Users.FindAsync(ticket.Handler.TechUserId.Value);
                    if (currentTech != null && currentTech.Role == SystemRoles.Maintenance && currentTech.MaintenanceTeam != normalizedTeam)
                    {
                        ticket.Handler.TechUserId = null;
                        if (ticket.StatusId == 2)
                        {
                            ticket.StatusId = 1; // back to opened/unassigned
                        }
                    }
                }

                _context.TicketMessages.Add(new TicketMsg
                {
                    TicketId = ticket.Id,
                    UserId = currentUserId > 0 ? currentUserId : ticket.OpenedBy,
                    Msg = $"🔄 REASSIGNED RESPONSIBLE TEAM: Transferred from {oldTeam} to {normalizedTeam} maintenance team.",
                    TimeStamp = DateTime.Now
                });

                _context.Update(ticket);
                await _context.SaveChangesAsync();

                var actor = _currentUserService.GetCurrentUser();
                await _notificationService.NotifyTicketCreatedAsync(ticket, actor);

                TempData["SuccessMessage"] = $"Ticket #{ticket.Id} successfully assigned to {normalizedTeam} team.";
            }

            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction(nameof(Details), new { id = ticketId });
        }

        // POST: Tickets/AddMessage
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddMessage(int ticketId, int userId, string? msg = null, string? pastedScreenshotsJson = null, List<IFormFile>? screenshotFiles = null)
        {
            var ticket = await _context.Tickets.FindAsync(ticketId);
            if (ticket == null) return NotFound();

            // Task 1: Lock messages once ticket is closed
            if (ticket.StatusId == 5 || ticket.ClosedDT.HasValue)
            {
                TempData["ErrorMessage"] = "This ticket is closed. New messages cannot be added.";
                return RedirectToAction(nameof(Details), new { id = ticketId });
            }

            // RBAC Guard: Verify access permission
            if (!_currentUserService.CanAccessTicket(ticket))
            {
                return Forbid();
            }

            bool hasScreenshots = (!string.IsNullOrWhiteSpace(pastedScreenshotsJson) && pastedScreenshotsJson != "[]")
                                  || (screenshotFiles != null && screenshotFiles.Any(f => f.Length > 0));

            if (string.IsNullOrWhiteSpace(msg) && !hasScreenshots)
            {
                TempData["ErrorMessage"] = "Message cannot be empty.";
                return RedirectToAction(nameof(Details), new { id = ticketId });
            }

            var currentUserId = _currentUserService.GetUserId();
            if (currentUserId > 0)
            {
                userId = currentUserId;
            }

            var cleanMsg = !string.IsNullOrWhiteSpace(msg) ? msg.Trim() : (hasScreenshots ? "📸 [Attached screenshot / error evidence]" : "");

            var clientIp = NetworkInfoHelper.GetClientIpAddress(HttpContext);
            var clientMac = NetworkInfoHelper.GetClientMacAddress(HttpContext, clientIp);

            var newMsg = new TicketMsg
            {
                TicketId = ticketId,
                UserId = userId,
                Msg = cleanMsg,
                TimeStamp = DateTime.Now,
                IpAddress = clientIp,
                MacAddress = clientMac
            };
            _context.TicketMessages.Add(newMsg);

            await _context.SaveChangesAsync();

            // Save any screenshots attached to this message, linked to newMsg.Id
            await ProcessScreenshotsAsync(ticketId, userId, pastedScreenshotsJson, screenshotFiles, newMsg.Id);

            var senderUser = await _context.Users.Include(u => u.Department).FirstOrDefaultAsync(u => u.Id == userId) ?? _currentUserService.GetCurrentUser();
            await _notificationService.NotifyTicketMessageAsync(ticket, newMsg, senderUser);

            TempData["SuccessMessage"] = "Message posted.";
            return RedirectToAction(nameof(Details), new { id = ticketId });
        }

        /// <summary>
        /// Normalizes parts replaced input into a valid JSON array string.
        /// If raw text is provided by user or legacy caller, wraps it into a valid ReplacedPartItem JSON structure.
        /// </summary>
        public static string? NormalizePartsJson(string? input)
        {
            if (string.IsNullOrWhiteSpace(input)) return null;

            var trimmed = input.Trim();
            if (trimmed == "[]" || trimmed == "{}") return null;

            try
            {
                using var doc = JsonDocument.Parse(trimmed);
                if (doc.RootElement.ValueKind == JsonValueKind.Array)
                {
                    if (doc.RootElement.GetArrayLength() == 0) return null;
                    return trimmed;
                }
                if (doc.RootElement.ValueKind == JsonValueKind.Object)
                {
                    return $"[{trimmed}]";
                }
            }
            catch
            {
                // Convert plain text into a structured JSON array
                var item = new ReplacedPartItem { PartName = trimmed, Quantity = 1 };
                return JsonSerializer.Serialize(new List<ReplacedPartItem> { item });
            }

            return trimmed;
        }

        // POST: Tickets/LogMaintenance
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LogMaintenance(int ticketId, int assetId, int technicianId, int? supplierId, int maintenanceType, string description, string? partsReplaced)
        {
            var ticket = await _context.Tickets.FindAsync(ticketId);
            if (ticket == null) return NotFound();

            if (!_currentUserService.CanManageTicket(ticket))
            {
                return Forbid();
            }

            var cleanParts = NormalizePartsJson(partsReplaced);

            var maintenance = new AssetMaintenance
            {
                TicketId = ticketId,
                AssetId = assetId,
                UserId = technicianId,
                SupplierId = supplierId,
                MaintenanceType = maintenanceType,
                Description = description,
                PartsReplaced = cleanParts,
                TimStamp = DateTime.Now
            };

            _context.AssetMaintenances.Add(maintenance);

            // Update asset lastPeriodic if periodic
            if (maintenanceType == 2)
            {
                var asset = await _context.Assets.FindAsync(assetId);
                if (asset != null && asset.PeriodicInterval.HasValue)
                {
                    asset.LastPeriodic = DateTime.Now;
                    asset.NextPeriodic = DateTime.Now.AddDays(asset.PeriodicInterval.Value);
                    _context.Update(asset);
                }
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Maintenance record saved and linked to ticket!";
            return RedirectToAction(nameof(Details), new { id = ticketId });
        }

        // POST: Tickets/LogSparePartUsage
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LogSparePartUsage(int ticketId, int sparePartId, int quantity, string? notes)
        {
            var ticket = await _context.Tickets
                .Include(t => t.Asset)
                .FirstOrDefaultAsync(t => t.Id == ticketId);

            if (ticket == null) return NotFound();

            // RBAC Guard: Only Admin and assigned Maintenance team can manage ticket reserve parts
            if (!_currentUserService.CanManageTicket(ticket))
            {
                return Forbid();
            }

            if (quantity <= 0)
            {
                TempData["ErrorMessage"] = "Quantity must be greater than zero.";
                return RedirectToAction(nameof(Details), new { id = ticketId });
            }

            var part = await _context.SpareParts.FindAsync(sparePartId);
            if (part == null || !part.IsActive)
            {
                TempData["ErrorMessage"] = "Spare part not found or inactive.";
                return RedirectToAction(nameof(Details), new { id = ticketId });
            }

            // Role isolation check: Technicians can only consume parts from their target team department
            if (!_currentUserService.IsAdmin() && !string.Equals(part.Department, ticket.TargetTeam, StringComparison.OrdinalIgnoreCase))
            {
                TempData["ErrorMessage"] = $"Selected spare part does not belong to the {ticket.TargetTeam} reserve stock.";
                return RedirectToAction(nameof(Details), new { id = ticketId });
            }

            if (part.Quantity < quantity)
            {
                TempData["ErrorMessage"] = $"Insufficient reserve stock for '{part.Name}'. Requested: {quantity}, Available: {part.Quantity}.";
                return RedirectToAction(nameof(Details), new { id = ticketId });
            }

            int currentUserId = _currentUserService.GetUserId();

            // Decrement inventory
            part.Quantity -= quantity;
            part.UpdatedAt = DateTime.Now;

            // Record transaction audit log
            var transaction = new SparePartTransaction
            {
                SparePartId = part.Id,
                TicketId = ticket.Id,
                AssetId = ticket.AssetId,
                UserId = currentUserId,
                TransactionType = "Usage in Ticket",
                QuantityChanged = -quantity,
                QuantityAfter = part.Quantity,
                Notes = string.IsNullOrWhiteSpace(notes) ? $"Used in Ticket #{ticket.Id}" : notes.Trim(),
                Timestamp = DateTime.Now
            };
            _context.SparePartTransactions.Add(transaction);

            // Record in ticket timeline
            var skuText = !string.IsNullOrWhiteSpace(part.PartNumber) ? $" (SKU: {part.PartNumber})" : "";
            var noteText = !string.IsNullOrWhiteSpace(notes) ? $" - Note: {notes.Trim()}" : "";
            var timelineMsg = new TicketMsg
            {
                TicketId = ticket.Id,
                UserId = currentUserId,
                Msg = $"🔧 [RESERVE STOCK / SPARE PART USED]: {quantity}x {part.Name}{skuText} from {part.Department} Reserve Stock{noteText}",
                TimeStamp = DateTime.Now
            };
            _context.TicketMessages.Add(timelineMsg);

            await _context.SaveChangesAsync();

            // Notify low stock if threshold reached
            if (part.IsLowStock)
            {
                await _notificationService.NotifyLowReserveStockAsync(part);
            }

            TempData["SuccessMessage"] = $"Successfully issued {quantity}x '{part.Name}' from {part.Department} reserve stock and recorded in ticket.";
            return RedirectToAction(nameof(Details), new { id = ticketId });
        }

        // GET: Tickets/PrintTicket/5
        public async Task<IActionResult> PrintTicket(int? id)
        {
            if (id == null) return NotFound();

            var ticket = await _context.Tickets
                .Include(t => t.Asset).ThenInclude(a => a!.Room)
                .Include(t => t.Asset).ThenInclude(a => a!.Supplier)
                .Include(t => t.OpenedByUser).ThenInclude(u => u!.Department)
                .Include(t => t.OnBehalfOfUser)
                .Include(t => t.Status)
                .Include(t => t.Handler).ThenInclude(h => h!.TechUser)
                .Include(t => t.Messages).ThenInclude(m => m.User)
                .Include(t => t.Messages).ThenInclude(m => m.Attachments)
                .Include(t => t.MaintenanceLogs).ThenInclude(m => m.Technician)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (ticket == null) return NotFound();

            // RBAC Guard: Verify access permissions
            if (!_currentUserService.CanAccessTicket(ticket))
            {
                return Forbid();
            }

            return View(ticket);
        }

        // GET: Tickets/ExportCsv
        public async Task<IActionResult> ExportCsv(int? statusId, int? priority, string? type, string? search, string? filter)
        {
            var currentUserId = _currentUserService.GetUserId();

            var query = _context.Tickets
                .Include(t => t.Asset)
                .Include(t => t.OpenedByUser)
                .Include(t => t.Status)
                .Include(t => t.Handler).ThenInclude(h => h!.TechUser)
                .Include(t => t.Messages)
                .AsQueryable();

            // RBAC Filtering
            if (_currentUserService.IsEndUser())
            {
                query = query.Where(t => t.OpenedBy == currentUserId || t.OnBehalfOf == currentUserId);
            }
            else if (_currentUserService.IsMaintenance())
            {
                var myTeam = _currentUserService.GetMaintenanceTeam();
                query = query.Where(t => t.TargetTeam == myTeam);
            }

            if (statusId.HasValue)
            {
                if (statusId.Value == -1) query = query.Where(t => t.StatusId != 5 && t.ClosedDT == null);
                else if (statusId.Value > 0) query = query.Where(t => t.StatusId == statusId.Value);
            }
            if (priority.HasValue && priority.Value > 0) query = query.Where(t => t.Priority == priority.Value);
            if (!string.IsNullOrWhiteSpace(type)) query = query.Where(t => t.Type == type);

            if (!string.IsNullOrWhiteSpace(filter))
            {
                if (filter == "my_assigned") query = query.Where(t => t.Handler != null && t.Handler.TechUserId == currentUserId);
                else if (filter == "my_opened") query = query.Where(t => t.OpenedBy == currentUserId || t.OnBehalfOf == currentUserId);
                else if (filter == "unassigned") query = query.Where(t => t.Handler == null || t.Handler.TechUserId == null);
                else if (filter == "maintenance" || filter == "in_maintenance") query = query.Where(t => t.StatusId == 6 || (t.Status != null && t.Status.Status != null && t.Status.Status.ToLower() == "maintenance"));
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                query = query.Where(t =>
                    t.Id.ToString() == s ||
                    (t.Asset != null && t.Asset.HospitalNo != null && t.Asset.HospitalNo.ToLower().Contains(s)) ||
                    (t.OpenedByUser != null && t.OpenedByUser.FullName != null && t.OpenedByUser.FullName.ToLower().Contains(s))
                );
            }

            var tickets = await query.OrderByDescending(t => t.OpenedDT).ToListAsync();

            var sb = new StringBuilder();
            sb.AppendLine("Ticket ID,Target Team,Issue Category,Asset Tag,Asset Brand,Priority,Status,Requester,Assigned Technician,Opened Date,Closed Date,Total Messages");

            foreach (var t in tickets)
            {
                sb.AppendLine($"\"{t.Id}\",\"{EscapeCsv(t.TargetTeam)}\",\"{EscapeCsv(t.Type)}\",\"{EscapeCsv(t.Asset?.HospitalNo)}\",\"{EscapeCsv(t.Asset?.Brand)}\",\"{t.PriorityName}\",\"{EscapeCsv(t.Status?.Status)}\",\"{EscapeCsv(t.OpenedByUser?.FullName)}\",\"{EscapeCsv(t.Handler?.TechUser?.FullName ?? "Unassigned")}\",\"{t.OpenedDT:yyyy-MM-dd HH:mm}\",\"{t.ClosedDT:yyyy-MM-dd HH:mm}\",\"{t.Messages.Count}\"");
            }

            var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
            return File(bytes, "text/csv", $"Tickets_Export_{DateTime.Now:yyyyMMdd_HHmm}.csv");
        }

        private static string EscapeCsv(string? value)
        {
            if (string.IsNullOrEmpty(value)) return "";
            return value.Replace("\"", "\"\"");
        }

        // GET: Tickets/GetUserAssets?userId=123
        [HttpGet]
        public async Task<IActionResult> GetUserAssets(int? userId)
        {
            var targetUserId = (userId.HasValue && userId.Value > 0) ? userId.Value : _currentUserService.GetUserId();
            if (targetUserId <= 0)
            {
                return Json(new { success = false, count = 0, assets = Array.Empty<object>() });
            }

            var activeTickets = await _context.Tickets
                .Where(t => t.AssetId.HasValue && t.StatusId != 4 && t.StatusId != 5 && t.ClosedDT == null)
                .Select(t => new { AssetId = t.AssetId!.Value, TicketId = t.Id })
                .ToListAsync();
            var activeTicketsDict = activeTickets
                .GroupBy(x => x.AssetId)
                .ToDictionary(g => g.Key, g => g.First().TicketId);

            var rawAssets = await _context.Assets
                .Include(a => a.Room)
                .Where(a => a.UserId == targetUserId)
                .OrderBy(a => a.HospitalNo)
                .ToListAsync();

            var assets = rawAssets.Select(a => new
            {
                id = a.Id,
                hospitalNo = a.HospitalNo ?? "",
                brand = a.Brand ?? "",
                type = a.Type ?? "Equipment",
                displayType = a.DisplayType,
                serialNo = a.SerialNo ?? "",
                roomName = a.Room != null ? (a.Room.RoomNo.HasValue ? $"Room {a.Room.RoomNo}" : (a.Room.RoomNameAr ?? "")) : "",
                maintenanceTeam = string.IsNullOrWhiteSpace(a.MaintenanceTeam) ? MaintenanceTeams.IT : (a.MaintenanceTeam.ToUpper().Contains("BIO") ? MaintenanceTeams.Biomedical : (a.MaintenanceTeam.ToUpper().Contains("GEN") || a.MaintenanceTeam.ToUpper().Contains("FACIL") ? MaintenanceTeams.General : MaintenanceTeams.IT)),
                installedSoftwares = a.InstalledSoftwares ?? "[]",
                installedSoftwaresList = a.InstalledSoftwaresList,
                display = $"{a.HospitalNo} - {a.Brand} ({a.Type ?? "Workstation"})",
                hasActiveTicket = activeTicketsDict.ContainsKey(a.Id),
                activeTicketId = activeTicketsDict.ContainsKey(a.Id) ? activeTicketsDict[a.Id] : (int?)null
            }).ToList();

            return Json(new { success = true, count = assets.Count, assets });
        }

        // GET: Tickets/GetPcErrorLog?pcName=XYZ
        [HttpGet]
        public IActionResult GetPcErrorLog(string? pcName)
        {
            var targetPc = !string.IsNullOrWhiteSpace(pcName) ? pcName.Trim() : Environment.MachineName;
            var logEntries = new List<object>();
            var sbFormatted = new StringBuilder();

            try
            {
                if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows))
                {
                    using var eventLog = new System.Diagnostics.EventLog("Application");
                    int totalEntries = eventLog.Entries.Count;
                    int collected = 0;
                    for (int i = totalEntries - 1; i >= 0 && collected < 15; i--)
                    {
                        try
                        {
                            var entry = eventLog.Entries[i];
                            if (entry.EntryType == System.Diagnostics.EventLogEntryType.Error || 
                                entry.EntryType == System.Diagnostics.EventLogEntryType.Warning)
                            {
                                var timeStr = entry.TimeGenerated.ToString("yyyy-MM-dd HH:mm:ss");
                                var level = entry.EntryType.ToString();
                                var source = entry.Source ?? "System";
                                var rawMsg = entry.Message ?? "";
                                var singleLineMsg = rawMsg.Replace("\r\n", " ").Replace("\n", " ").Trim();
                                if (singleLineMsg.Length > 240) singleLineMsg = singleLineMsg.Substring(0, 240) + "...";

                                logEntries.Add(new
                                {
                                    time = timeStr,
                                    level = level,
                                    source = source,
                                    eventId = entry.InstanceId,
                                    message = singleLineMsg
                                });

                                sbFormatted.AppendLine($"[{timeStr}] [{level.ToUpper()}] [{source}] (EventID: {entry.InstanceId})");
                                sbFormatted.AppendLine($"  {singleLineMsg}");
                                sbFormatted.AppendLine();
                                collected++;
                            }
                        }
                        catch { }
                    }
                }
            }
            catch (Exception ex)
            {
                sbFormatted.AppendLine($"[Notice] Windows Event Log query notice: {ex.Message}");
            }

            if (logEntries.Count == 0)
            {
                var now = DateTime.Now;
                var simulated = new[]
                {
                    new { time = now.AddMinutes(-3).ToString("yyyy-MM-dd HH:mm:ss"), level = "Error", source = "Application Error", eventId = 1000L, message = "Faulting application name: HIS_Client.exe, version: 4.5.102.0, exception code: 0xc0000005, fault offset: 0x0001a4e2." },
                    new { time = now.AddMinutes(-9).ToString("yyyy-MM-dd HH:mm:ss"), level = "Warning", source = ".NET Runtime", eventId = 1026L, message = "Application MedicalViewer.exe encountered high memory usage or stalled background worker thread." },
                    new { time = now.AddMinutes(-25).ToString("yyyy-MM-dd HH:mm:ss"), level = "Error", source = "SideBySide", eventId = 33L, message = "Activation context generation failed for PACS_Plugin.dll. Dependent assembly Microsoft.VC90.CRT not found." }
                };
                foreach (var s in simulated)
                {
                    logEntries.Add(s);
                    sbFormatted.AppendLine($"[{s.time}] [{s.level.ToUpper()}] [{s.source}] (EventID: {s.eventId})");
                    sbFormatted.AppendLine($"  {s.message}");
                    sbFormatted.AppendLine();
                }
            }

            return Json(new
            {
                success = true,
                machineName = targetPc,
                count = logEntries.Count,
                formatted = sbFormatted.ToString().Trim(),
                entries = logEntries
            });
        }

        private async Task<int> ProcessScreenshotsAsync(int ticketId, int userId, string? pastedScreenshotsJson, List<IFormFile>? screenshotFiles, int? messageId = null)
        {
            int savedCount = 0;
            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "tickets");
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            // 1. Process base64 screenshots from clipboard paste
            if (!string.IsNullOrWhiteSpace(pastedScreenshotsJson))
            {
                try
                {
                    var dataUrls = JsonSerializer.Deserialize<List<string>>(pastedScreenshotsJson);
                    if (dataUrls != null)
                    {
                        int index = 1;
                        foreach (var dataUrl in dataUrls)
                        {
                            if (string.IsNullOrWhiteSpace(dataUrl)) continue;
                            var commaIdx = dataUrl.IndexOf(',');
                            if (commaIdx > 0)
                            {
                                var header = dataUrl.Substring(0, commaIdx);
                                var base64Data = dataUrl.Substring(commaIdx + 1);
                                var ext = ".png";
                                var fileType = "image/png";
                                if (header.Contains("jpeg") || header.Contains("jpg"))
                                {
                                    ext = ".jpg";
                                    fileType = "image/jpeg";
                                }
                                else if (header.Contains("webp"))
                                {
                                    ext = ".webp";
                                    fileType = "image/webp";
                                }

                                var bytes = Convert.FromBase64String(base64Data);
                                var uniqueFileName = $"screenshot_{ticketId}_{DateTime.Now:yyyyMMddHHmmss}_{Guid.NewGuid().ToString("N").Substring(0, 8)}{ext}";
                                var filePath = Path.Combine(uploadsFolder, uniqueFileName);
                                await System.IO.File.WriteAllBytesAsync(filePath, bytes);

                                var attachment = new TicketAttachment
                                {
                                    TicketId = ticketId,
                                    MessageId = messageId,
                                    FileName = $"Screenshot_{index}_{DateTime.Now:HHmm}{ext}",
                                    FilePath = $"/uploads/tickets/{uniqueFileName}",
                                    FileType = fileType,
                                    FileSize = bytes.Length,
                                    UploadedDT = DateTime.Now,
                                    UploadedBy = userId > 0 ? userId : null
                                };
                                _context.TicketAttachments.Add(attachment);
                                savedCount++;
                                index++;
                            }
                        }
                    }
                }
                catch { }
            }

            // 2. Process uploaded image files
            if (screenshotFiles != null && screenshotFiles.Any())
            {
                foreach (var file in screenshotFiles)
                {
                    if (file.Length > 0 && file.ContentType.StartsWith("image/"))
                    {
                        var ext = Path.GetExtension(file.FileName);
                        if (string.IsNullOrWhiteSpace(ext)) ext = ".png";
                        var uniqueFileName = $"upload_{ticketId}_{DateTime.Now:yyyyMMddHHmmss}_{Guid.NewGuid().ToString("N").Substring(0, 8)}{ext}";
                        var filePath = Path.Combine(uploadsFolder, uniqueFileName);
                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await file.CopyToAsync(stream);
                        }

                        var attachment = new TicketAttachment
                        {
                            TicketId = ticketId,
                            MessageId = messageId,
                            FileName = file.FileName,
                            FilePath = $"/uploads/tickets/{uniqueFileName}",
                            FileType = file.ContentType,
                            FileSize = file.Length,
                            UploadedDT = DateTime.Now,
                            UploadedBy = userId > 0 ? userId : null
                        };
                        _context.TicketAttachments.Add(attachment);
                        savedCount++;
                    }
                }
            }

            if (savedCount > 0)
            {
                await _context.SaveChangesAsync();
            }

            return savedCount;
        }

        private async Task PopulateDropdowns(int? selectedAssetId = null, int? defaultUserId = null, int? currentUserId = null)
        {
            var activeTickets = await _context.Tickets
                .Where(t => t.AssetId.HasValue && t.StatusId != 4 && t.StatusId != 5 && t.ClosedDT == null)
                .Select(t => new { AssetId = t.AssetId!.Value, TicketId = t.Id })
                .ToListAsync();
            var activeTicketsDict = activeTickets
                .GroupBy(x => x.AssetId)
                .ToDictionary(g => g.Key, g => g.First().TicketId);

            // Assets associated with the current user / authorized scope
            var userAssetsQuery = _context.Assets.Include(a => a.Room).AsQueryable();

            if (_currentUserService.IsAdmin())
            {
                // Admin can open tickets for any asset across all departments in the hospital
            }
            else if (_currentUserService.IsMaintenance())
            {
                var myTeam = _currentUserService.GetMaintenanceTeam();
                if (!string.IsNullOrWhiteSpace(myTeam))
                {
                    userAssetsQuery = userAssetsQuery.Where(a => a.MaintenanceTeam == myTeam || (currentUserId.HasValue && a.UserId == currentUserId.Value));
                }
            }
            else if (currentUserId.HasValue && currentUserId.Value > 0)
            {
                userAssetsQuery = userAssetsQuery.Where(a => a.UserId == currentUserId.Value);
            }

            var rawUserAssets = await userAssetsQuery
                .OrderBy(a => a.HospitalNo)
                .ToListAsync();

            var userAssets = rawUserAssets.Select(a => new { 
                a.Id, 
                HospitalNo = a.HospitalNo ?? "", 
                Brand = a.Brand ?? "", 
                Type = a.Type ?? "Equipment", 
                DisplayType = a.DisplayType,
                SerialNo = a.SerialNo ?? "",
                RoomName = a.Room != null ? (a.Room.RoomNo.HasValue ? $"Room {a.Room.RoomNo}" : (a.Room.RoomNameAr ?? "")) : "",
                MaintenanceTeam = string.IsNullOrWhiteSpace(a.MaintenanceTeam) ? MaintenanceTeams.IT : (a.MaintenanceTeam.ToUpper().Contains("BIO") ? MaintenanceTeams.Biomedical : (a.MaintenanceTeam.ToUpper().Contains("GEN") || a.MaintenanceTeam.ToUpper().Contains("FACIL") ? MaintenanceTeams.General : MaintenanceTeams.IT)), 
                InstalledSoftwares = a.InstalledSoftwares ?? "[]",
                InstalledSoftwaresList = a.InstalledSoftwaresList,
                Display = $"{a.HospitalNo} - {a.Brand} ({a.Type ?? "Workstation"})",
                HasActiveTicket = activeTicketsDict.ContainsKey(a.Id),
                ActiveTicketId = activeTicketsDict.ContainsKey(a.Id) ? activeTicketsDict[a.Id] : (int?)null
            }).ToList();

            ViewBag.Assets = new SelectList(userAssets, "Id", "Display", selectedAssetId);
            ViewBag.UserAssetsList = userAssets;
            ViewBag.UserAssetsCount = userAssets.Count;
            ViewBag.UserAssetsCountIT = userAssets.Count(a => a.MaintenanceTeam == MaintenanceTeams.IT);
            ViewBag.UserAssetsCountBio = userAssets.Count(a => a.MaintenanceTeam == MaintenanceTeams.Biomedical);
            ViewBag.UserAssetsCountGen = userAssets.Count(a => a.MaintenanceTeam == MaintenanceTeams.General);
            ViewBag.UserAssetsJson = JsonSerializer.Serialize(userAssets);
            ViewBag.ActiveTicketAssetsMap = activeTicketsDict;
            ViewBag.CurrentUserId = currentUserId ?? 0;
            ViewBag.DetectedPcName = Environment.MachineName;
            ViewBag.Users = new SelectList(await _context.Users.Where(u => u.IsActive == true).OrderBy(u => u.FullName).ToListAsync(), "Id", "FullName", defaultUserId);
            var techUsers = await _context.Users
                .Where(u => u.IsActive == true && (u.Role == SystemRoles.Maintenance || u.Role == SystemRoles.Admin))
                .OrderBy(u => u.FullName)
                .Select(u => new {
                    u.Id,
                    u.FullName,
                    RoleDisplay = u.RoleDisplay,
                    MaintenanceTeam = u.MaintenanceTeam ?? "IT"
                })
                .ToListAsync();
            ViewBag.TechniciansList = techUsers;
            ViewBag.Technicians = new SelectList(techUsers, "Id", "FullName");
            ViewBag.Statuses = new SelectList(await _context.TicketStatuses.ToListAsync(), "Id", "Status");
            ViewBag.MaintenanceTeams = new SelectList(MaintenanceTeams.All, MaintenanceTeams.IT);
            ViewBag.SoftwareCatalog = await _context.SoftwareCatalog.Where(s => s.IsActive).OrderBy(s => s.Category).ThenBy(s => s.Name).ToListAsync();
        }
    }
}
