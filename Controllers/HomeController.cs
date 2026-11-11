using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockPortal.Data;
using StockPortal.Models;
using StockPortal.Services;
using StockPortal.ViewModels;

namespace StockPortal.Controllers
{
    public class HomeController : Controller
    {
        private readonly StockDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public HomeController(StockDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<IActionResult> Index(
            DateTime? fromDate = null,
            DateTime? toDate = null,
            string? teamFilter = null,
            int? techId = null,
            string? datePreset = "30days",
            string? tab = "overview")
        {
            var now = DateTime.Now;
            var today = DateTime.Today;
            var thirtyDaysFromNow = today.AddDays(30);

            bool isAdmin = _currentUserService.IsAdmin();
            bool isMaint = _currentUserService.IsMaintenance();
            string? userMaintTeam = _currentUserService.GetMaintenanceTeam();

            // End Users land directly on their tickets queue / workplace
            if (_currentUserService.IsEndUser())
            {
                return RedirectToAction("Index", "Tickets");
            }

            // RBAC Team Isolation Guard:
            // Admin can view all teams or filter by any team.
            // Maintenance teams (IT, BIOMEDICAL, GENERAL) MUST only see their own team's private analysis!
            string normalizedTeam;
            if (isMaint && !string.IsNullOrWhiteSpace(userMaintTeam))
            {
                normalizedTeam = userMaintTeam; // strictly lock to their own team
            }
            else if (isAdmin)
            {
                normalizedTeam = string.IsNullOrWhiteSpace(teamFilter) ? "ALL" : teamFilter.Trim().ToUpperInvariant();
            }
            else
            {
                normalizedTeam = "ALL";
            }

            // Query scoped datasets based on RBAC & department filter:
            // Admin can see ALL departments (or filter by one).
            // Maintenance users are strictly isolated to their own department's assets, tickets, and maintenance logs!
            var assetsQuery = _context.Assets
                .Include(a => a.User)
                .Include(a => a.Room)
                .Include(a => a.Supplier)
                .AsQueryable();

            var ticketsQuery = _context.Tickets
                .Include(t => t.Asset)
                .Include(t => t.OpenedByUser)
                .Include(t => t.Status)
                .Include(t => t.Handler).ThenInclude(h => h!.TechUser)
                .AsQueryable();

            var recentMaintQuery = _context.AssetMaintenances
                .Include(m => m.Asset)
                .Include(m => m.Technician)
                .Include(m => m.Supplier)
                .AsQueryable();

            if (normalizedTeam != "ALL")
            {
                assetsQuery = assetsQuery.Where(a => a.MaintenanceTeam == normalizedTeam);
                ticketsQuery = ticketsQuery.Where(t =>
                    t.TargetTeam == normalizedTeam ||
                    (t.Asset != null && t.Asset.MaintenanceTeam == normalizedTeam)
                );
                recentMaintQuery = recentMaintQuery.Where(m =>
                    (m.Asset != null && m.Asset.MaintenanceTeam == normalizedTeam) ||
                    (m.Technician != null && m.Technician.MaintenanceTeam == normalizedTeam)
                );
            }

            var assets = await assetsQuery.ToListAsync();
            var tickets = await ticketsQuery.OrderByDescending(t => t.OpenedDT).ToListAsync();
            var recentMaintenance = await recentMaintQuery.OrderByDescending(m => m.TimStamp).Take(5).ToListAsync();

            // ----------------------------------------------------
            // Date Filter Computation for Maintenance Analytics Tab
            // ----------------------------------------------------
            DateTime rangeStart;
            DateTime rangeEnd;

            string selectedPreset = datePreset?.ToLowerInvariant() ?? "30days";

            if (fromDate.HasValue || toDate.HasValue)
            {
                selectedPreset = "custom";
                rangeStart = fromDate ?? today.AddDays(-30);
                rangeEnd = toDate.HasValue ? toDate.Value.Date.AddDays(1).AddTicks(-1) : now;
            }
            else
            {
                switch (selectedPreset)
                {
                    case "today":
                        rangeStart = today;
                        rangeEnd = today.AddDays(1).AddTicks(-1);
                        break;
                    case "7days":
                        rangeStart = today.AddDays(-7);
                        rangeEnd = now;
                        break;
                    case "month":
                        rangeStart = new DateTime(today.Year, today.Month, 1);
                        rangeEnd = now;
                        break;
                    case "quarter":
                        int currentQuarter = (today.Month - 1) / 3 + 1;
                        rangeStart = new DateTime(today.Year, (currentQuarter - 1) * 3 + 1, 1);
                        rangeEnd = now;
                        break;
                    case "year":
                        rangeStart = new DateTime(today.Year, 1, 1);
                        rangeEnd = now;
                        break;
                    case "all":
                        rangeStart = new DateTime(2000, 1, 1);
                        rangeEnd = now.AddDays(1);
                        break;
                    case "30days":
                    default:
                        selectedPreset = "30days";
                        rangeStart = today.AddDays(-30);
                        rangeEnd = now;
                        break;
                }
            }

            // Target Team Scope Filtering
            var scopedTickets = tickets.Where(t =>
                normalizedTeam == "ALL" ||
                string.Equals(t.TargetTeam, normalizedTeam, StringComparison.OrdinalIgnoreCase) ||
                (t.Asset != null && string.Equals(t.Asset.MaintenanceTeam, normalizedTeam, StringComparison.OrdinalIgnoreCase))
            ).ToList();

            // Tickets closed within date range
            var closedTickets = scopedTickets.Where(t =>
                t.StatusId == 5 &&
                (
                    (t.ClosedDT.HasValue && t.ClosedDT.Value >= rangeStart && t.ClosedDT.Value <= rangeEnd) ||
                    (!t.ClosedDT.HasValue && t.OpenedDT.HasValue && t.OpenedDT.Value >= rangeStart && t.OpenedDT.Value <= rangeEnd)
                )
            ).ToList();

            // Active (Open/Pending/Delayed) tickets in scope
            var activeTickets = scopedTickets.Where(t => t.StatusId != 5 && t.StatusId != 4).ToList();

            // Maintenance logs in the period
            var allMaintenanceLogs = await _context.AssetMaintenances
                .Include(m => m.Asset)
                .Include(m => m.Technician)
                .ToListAsync();

            var periodMaintenanceLogs = allMaintenanceLogs.Where(m =>
                m.TimStamp.HasValue &&
                m.TimStamp.Value >= rangeStart &&
                m.TimStamp.Value <= rangeEnd &&
                (normalizedTeam == "ALL" || (m.Asset != null && string.Equals(m.Asset.MaintenanceTeam, normalizedTeam, StringComparison.OrdinalIgnoreCase)))
            ).ToList();

            // ----------------------------------------------------
            // Technicians available in the scoped team
            // ----------------------------------------------------
            var allUsers = await _context.Users.ToListAsync();
            var techUserLookup = allUsers.ToDictionary(u => u.Id);

            var availableTechnicians = allUsers.Where(u =>
                u.IsActive == true && (
                    (normalizedTeam == "ALL" && (u.Role == SystemRoles.Maintenance || u.Role == SystemRoles.Admin || string.Equals(u.Role, "Technician", StringComparison.OrdinalIgnoreCase))) ||
                    (normalizedTeam != "ALL" && string.Equals(u.MaintenanceTeam, normalizedTeam, StringComparison.OrdinalIgnoreCase))
                )
            ).OrderBy(u => u.FullName).ToList();

            // Prevent unauthorized cross-team technician filter
            if (techId.HasValue && techId.Value > 0)
            {
                if (techUserLookup.TryGetValue(techId.Value, out var selectedTechUser))
                {
                    if (isMaint && !string.Equals(selectedTechUser.MaintenanceTeam, userMaintTeam, StringComparison.OrdinalIgnoreCase))
                    {
                        techId = null; // reset if from another team!
                    }
                }
                else
                {
                    techId = null;
                }
            }

            // If an individual technician is selected, filter closed tickets, active tickets, and maintenance logs for this person
            var displayClosedTickets = closedTickets;
            var displayActiveTickets = activeTickets;
            var displayMaintenanceLogs = periodMaintenanceLogs;

            if (techId.HasValue && techId.Value > 0)
            {
                int tid = techId.Value;
                displayClosedTickets = closedTickets.Where(t =>
                    t.ClosedByUserIds.Contains(tid) || (t.Handler?.TechUserId == tid)
                ).ToList();

                displayActiveTickets = activeTickets.Where(t =>
                    t.Handler?.TechUserId == tid
                ).ToList();

                displayMaintenanceLogs = periodMaintenanceLogs.Where(m =>
                    m.UserId == tid
                ).ToList();
            }

            int filteredTotal = displayClosedTickets.Count + displayActiveTickets.Count;
            double filteredResRate = filteredTotal > 0
                ? Math.Round(((double)displayClosedTickets.Count / filteredTotal) * 100.0, 1)
                : 100.0;

            var resolutionDurations = displayClosedTickets
                .Where(t => t.OpenedDT.HasValue && t.ClosedDT.HasValue && t.ClosedDT.Value >= t.OpenedDT.Value)
                .Select(t => (t.ClosedDT!.Value - t.OpenedDT!.Value).TotalHours)
                .ToList();

            double avgResHours = resolutionDurations.Any()
                ? Math.Round(resolutionDurations.Average(), 1)
                : 0.0;

            // ----------------------------------------------------
            // Technician & Specialist Workload Scorecard ("Who managed what")
            // ----------------------------------------------------
            var candidateTechIds = new HashSet<int>(availableTechnicians.Select(u => u.Id));

            foreach (var t in closedTickets)
            {
                foreach (var uid in t.ClosedByUserIds)
                {
                    if (techUserLookup.TryGetValue(uid, out var u) &&
                        (normalizedTeam == "ALL" || string.Equals(u.MaintenanceTeam, normalizedTeam, StringComparison.OrdinalIgnoreCase)))
                    {
                        candidateTechIds.Add(uid);
                    }
                }
                if (t.Handler?.TechUserId.HasValue == true)
                {
                    int hid = t.Handler.TechUserId.Value;
                    if (techUserLookup.TryGetValue(hid, out var u) &&
                        (normalizedTeam == "ALL" || string.Equals(u.MaintenanceTeam, normalizedTeam, StringComparison.OrdinalIgnoreCase)))
                    {
                        candidateTechIds.Add(hid);
                    }
                }
            }

            var techStats = new List<TechnicianWorkloadStat>();

            foreach (var tId in candidateTechIds)
            {
                if (!techUserLookup.TryGetValue(tId, out var user)) continue;

                // Closed tickets where user was either assigned tech or included in ClosedByUserIds
                var techClosed = closedTickets.Where(t =>
                    t.ClosedByUserIds.Contains(tId) ||
                    (t.Handler?.TechUserId == tId)
                ).ToList();

                // Active tickets currently assigned to this user
                var techActive = scopedTickets.Where(t =>
                    t.StatusId != 5 && t.StatusId != 4 &&
                    t.Handler?.TechUserId == tId
                ).ToList();

                var techMaintCount = periodMaintenanceLogs.Count(m => m.UserId == tId);

                var techDurations = techClosed
                    .Where(t => t.OpenedDT.HasValue && t.ClosedDT.HasValue && t.ClosedDT.Value >= t.OpenedDT.Value)
                    .Select(t => (t.ClosedDT!.Value - t.OpenedDT!.Value).TotalHours)
                    .ToList();

                double techAvgHours = techDurations.Any() ? Math.Round(techDurations.Average(), 1) : 0.0;

                techStats.Add(new TechnicianWorkloadStat
                {
                    UserId = user.Id,
                    FullName = user.FullName ?? user.DomainUser ?? $"User #{user.Id}",
                    FullNameAr = user.FullNameAr,
                    DomainUser = user.DomainUser,
                    MaintenanceTeam = user.MaintenanceTeam ?? (user.Role == SystemRoles.Admin ? "ADMIN" : "GENERAL"),
                    RoleDisplay = user.RoleDisplay,
                    ClosedTicketsCount = techClosed.Count,
                    ActiveTicketsCount = techActive.Count,
                    TotalHandledTickets = techClosed.Count + techActive.Count,
                    AverageResolutionHours = techAvgHours,
                    MaintenanceWorkOrdersCount = techMaintCount
                });
            }

            // Order technicians by most closed tickets, then active tickets
            techStats = techStats
                .OrderByDescending(s => s.ClosedTicketsCount)
                .ThenByDescending(s => s.ActiveTicketsCount)
                .ThenBy(s => s.FullName)
                .ToList();

            // ----------------------------------------------------
            // Team Performance Comparison (IT vs BIOMEDICAL vs GENERAL)
            // Admin sees all 3 teams; Maintenance user sees ONLY their team
            // ----------------------------------------------------
            var teamCodes = isAdmin && normalizedTeam == "ALL"
                ? new[] { MaintenanceTeams.IT, MaintenanceTeams.Biomedical, MaintenanceTeams.General }
                : new[] { normalizedTeam };

            var teamStats = new List<TeamAnalyticsStat>();

            foreach (var code in teamCodes)
            {
                var teamTickets = tickets.Where(t =>
                    string.Equals(t.TargetTeam, code, StringComparison.OrdinalIgnoreCase) ||
                    (t.Asset != null && string.Equals(t.Asset.MaintenanceTeam, code, StringComparison.OrdinalIgnoreCase))
                ).ToList();

                var teamClosed = teamTickets.Where(t =>
                    t.StatusId == 5 &&
                    (
                        (t.ClosedDT.HasValue && t.ClosedDT.Value >= rangeStart && t.ClosedDT.Value <= rangeEnd) ||
                        (!t.ClosedDT.HasValue && t.OpenedDT.HasValue && t.OpenedDT.Value >= rangeStart && t.OpenedDT.Value <= rangeEnd)
                    )
                ).ToList();

                var teamActive = teamTickets.Where(t => t.StatusId != 5 && t.StatusId != 4).ToList();
                var teamUrgent = teamActive.Count(t => t.Priority >= 3);

                int teamTotalHandled = teamClosed.Count + teamActive.Count;
                double teamRate = teamTotalHandled > 0
                    ? Math.Round(((double)teamClosed.Count / teamTotalHandled) * 100.0, 1)
                    : 100.0;

                var teamDurations = teamClosed
                    .Where(t => t.OpenedDT.HasValue && t.ClosedDT.HasValue && t.ClosedDT.Value >= t.OpenedDT.Value)
                    .Select(t => (t.ClosedDT!.Value - t.OpenedDT!.Value).TotalHours)
                    .ToList();

                double teamAvgHours = teamDurations.Any() ? Math.Round(teamDurations.Average(), 1) : 0.0;

                teamStats.Add(new TeamAnalyticsStat
                {
                    TeamCode = code,
                    TeamName = LocHelper.TranslateTargetTeam(code),
                    TotalTickets = teamTickets.Count,
                    ClosedTickets = teamClosed.Count,
                    ActiveTickets = teamActive.Count,
                    UrgentTickets = teamUrgent,
                    ResolutionRate = teamRate,
                    AverageResolutionHours = teamAvgHours
                });
            }

            // ----------------------------------------------------
            // Issue Category Breakdown
            // ----------------------------------------------------
            var categoryStats = displayClosedTickets
                .GroupBy(t => string.IsNullOrWhiteSpace(t.Type) ? "General" : t.Type)
                .Select(g => new CategoryAnalyticsStat
                {
                    Category = g.Key,
                    DisplayCategory = LocHelper.TranslateTicketType(g.Key),
                    Count = g.Count(),
                    Percentage = displayClosedTickets.Count > 0 ? Math.Round(((double)g.Count() / displayClosedTickets.Count) * 100.0, 1) : 0.0
                })
                .OrderByDescending(c => c.Count)
                .ToList();

            // ----------------------------------------------------
            // Closed Tickets Management & Audit Log
            // ----------------------------------------------------
            var closedTicketsLog = displayClosedTickets
                .OrderByDescending(t => t.ClosedDT ?? t.OpenedDT)
                .Select(t =>
                {
                    var resolvedUsers = new List<User>();
                    if (t.ClosedByUserIds.Any())
                    {
                        foreach (var uid in t.ClosedByUserIds)
                        {
                            if (techUserLookup.TryGetValue(uid, out var u)) resolvedUsers.Add(u);
                        }
                    }
                    else if (t.Handler?.TechUser != null)
                    {
                        resolvedUsers.Add(t.Handler.TechUser);
                    }

                    double turnaround = (t.ClosedDT.HasValue && t.OpenedDT.HasValue && t.ClosedDT >= t.OpenedDT)
                        ? Math.Round((t.ClosedDT.Value - t.OpenedDT.Value).TotalHours, 1)
                        : 0.0;

                    return new ClosedTicketRecordItem
                    {
                        TicketId = t.Id,
                        Title = t.ResolutionDetails,
                        TargetTeam = t.TargetTeam ?? "IT",
                        Type = t.Type,
                        Priority = t.Priority,
                        OpenedDT = t.OpenedDT,
                        ClosedDT = t.ClosedDT,
                        TurnaroundHours = turnaround,
                        RequesterName = t.OpenedByUser?.FullName ?? t.OpenedByUser?.DomainUser ?? "Unknown",
                        EquipmentTag = t.Asset?.HospitalNo ?? (t.PcName ?? "N/A"),
                        EquipmentBrand = t.Asset?.Brand,
                        AssetId = t.AssetId,
                        ResolutionNotes = t.ResolutionDetails,
                        ResolvedByUsers = resolvedUsers
                    };
                })
                .ToList();

            var vm = new DashboardViewModel
            {
                TotalAssets = assets.Count,
                InServiceAssets = assets.Count(a => a.Status == "In Service"),
                InStockAssets = assets.Count(a => a.Status == "In Stock"),
                UnderMaintenanceAssets = assets.Count(a => a.Status == "Under Maintenance"),
                TotalInventoryValue = assets.Sum(a => a.Price ?? 0),

                TotalTickets = tickets.Count,
                OpenTickets = tickets.Count(t => t.StatusId == 1),
                PendingTickets = tickets.Count(t => t.StatusId == 2),
                DelayedTickets = tickets.Count(t => t.StatusId == 3),
                ClosedTickets = tickets.Count(t => t.StatusId == 5),

                UrgentTickets = tickets.Where(t => t.Priority >= 3 && t.StatusId != 5 && t.StatusId != 4)
                    .OrderByDescending(t => t.Priority)
                    .Take(5)
                    .ToList(),

                RecentTickets = tickets.Take(6).ToList(),

                OverduePeriodicAssets = assets
                    .Where(a => a.NeedsPeriodic == true && a.NextPeriodic.HasValue && a.NextPeriodic.Value < today)
                    .OrderBy(a => a.NextPeriodic)
                    .ToList(),

                UpcomingPeriodicAssets = assets
                    .Where(a => a.NeedsPeriodic == true && a.NextPeriodic.HasValue && a.NextPeriodic.Value >= today && a.NextPeriodic.Value <= thirtyDaysFromNow)
                    .OrderBy(a => a.NextPeriodic)
                    .ToList(),

                OverdueCalibrationAssets = assets
                    .Where(a => a.NeedsCalibration == true && a.NextCalibration.HasValue && a.NextCalibration.Value < today)
                    .OrderBy(a => a.NextCalibration)
                    .ToList(),

                UpcomingCalibrationAssets = assets
                    .Where(a => a.NeedsCalibration == true && a.NextCalibration.HasValue && a.NextCalibration.Value >= today && a.NextCalibration.Value <= thirtyDaysFromNow)
                    .OrderBy(a => a.NextCalibration)
                    .ToList(),

                RecentMaintenance = recentMaintenance,

                // Analytics Tab Data
                ActiveTab = string.Equals(tab, "analytics", StringComparison.OrdinalIgnoreCase) ? "analytics" : "overview",
                FromDate = rangeStart,
                ToDate = rangeEnd,
                SelectedTeam = normalizedTeam,
                SelectedTechUserId = techId,
                CanFilterTeam = isAdmin,
                UserMaintenanceTeam = userMaintTeam,
                AvailableTechnicians = availableTechnicians,
                DatePreset = selectedPreset,
                FilteredTotalTickets = filteredTotal,
                FilteredClosedTickets = displayClosedTickets.Count,
                FilteredActiveTickets = displayActiveTickets.Count,
                FilteredResolutionRate = filteredResRate,
                FilteredAverageResolutionHours = avgResHours,
                FilteredMaintenanceLogsCount = displayMaintenanceLogs.Count,
                TechnicianStats = techStats,
                TeamStats = teamStats,
                CategoryStats = categoryStats,
                FilteredClosedTicketsLog = closedTicketsLog
            };

            return View(vm);
        }

        // GET: Home/PrintMemberReport
        [HttpGet]
        public async Task<IActionResult> PrintMemberReport(
            int userId,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            string? datePreset = "30days")
        {
            var targetUser = await _context.Users.Include(u => u.Department).FirstOrDefaultAsync(u => u.Id == userId);
            if (targetUser == null) return NotFound();

            // RBAC Isolation Guard:
            // Admin can view any member's report.
            // Maintenance team members can ONLY view reports for members within their own maintenance team.
            if (!_currentUserService.CanAccessDashboard())
            {
                return Forbid();
            }

            if (_currentUserService.IsMaintenance())
            {
                var myTeam = _currentUserService.GetMaintenanceTeam();
                if (!string.Equals(targetUser.MaintenanceTeam, myTeam, StringComparison.OrdinalIgnoreCase) &&
                    targetUser.Id != _currentUserService.GetUserId())
                {
                    return Forbid();
                }
            }

            var now = DateTime.Now;
            var today = DateTime.Today;
            DateTime rangeStart;
            DateTime rangeEnd;

            string selectedPreset = datePreset?.ToLowerInvariant() ?? "30days";
            if (fromDate.HasValue || toDate.HasValue)
            {
                selectedPreset = "custom";
                rangeStart = fromDate ?? today.AddDays(-30);
                rangeEnd = toDate.HasValue ? toDate.Value.Date.AddDays(1).AddTicks(-1) : now;
            }
            else
            {
                switch (selectedPreset)
                {
                    case "today":
                        rangeStart = today;
                        rangeEnd = today.AddDays(1).AddTicks(-1);
                        break;
                    case "7days":
                        rangeStart = today.AddDays(-7);
                        rangeEnd = now;
                        break;
                    case "month":
                        rangeStart = new DateTime(today.Year, today.Month, 1);
                        rangeEnd = now;
                        break;
                    case "quarter":
                        int currentQuarter = (today.Month - 1) / 3 + 1;
                        rangeStart = new DateTime(today.Year, (currentQuarter - 1) * 3 + 1, 1);
                        rangeEnd = now;
                        break;
                    case "year":
                        rangeStart = new DateTime(today.Year, 1, 1);
                        rangeEnd = now;
                        break;
                    case "all":
                        rangeStart = new DateTime(2000, 1, 1);
                        rangeEnd = now.AddDays(1);
                        break;
                    case "30days":
                    default:
                        selectedPreset = "30days";
                        rangeStart = today.AddDays(-30);
                        rangeEnd = now;
                        break;
                }
            }

            // Fetch closed tickets where targetUser was assigned or participated
            var allClosedInPeriod = await _context.Tickets
                .Include(t => t.Asset)
                .Include(t => t.OpenedByUser)
                .Include(t => t.Handler).ThenInclude(h => h!.TechUser)
                .Where(t => t.StatusId == 5 &&
                            (
                                (t.ClosedDT.HasValue && t.ClosedDT.Value >= rangeStart && t.ClosedDT.Value <= rangeEnd) ||
                                (!t.ClosedDT.HasValue && t.OpenedDT.HasValue && t.OpenedDT.Value >= rangeStart && t.OpenedDT.Value <= rangeEnd)
                            ))
                .ToListAsync();

            var userClosedTickets = allClosedInPeriod.Where(t =>
                t.ClosedByUserIds.Contains(userId) || (t.Handler?.TechUserId == userId)
            ).ToList();

            var allUsers = await _context.Users.ToDictionaryAsync(u => u.Id);

            var closedTicketRecords = userClosedTickets
                .OrderByDescending(t => t.ClosedDT ?? t.OpenedDT)
                .Select(t =>
                {
                    var collaborators = new List<User>();
                    foreach (var cid in t.ClosedByUserIds)
                    {
                        if (allUsers.TryGetValue(cid, out var u)) collaborators.Add(u);
                    }
                    if (!collaborators.Any() && t.Handler?.TechUser != null)
                    {
                        collaborators.Add(t.Handler.TechUser);
                    }

                    double turnaround = (t.ClosedDT.HasValue && t.OpenedDT.HasValue && t.ClosedDT >= t.OpenedDT)
                        ? Math.Round((t.ClosedDT.Value - t.OpenedDT.Value).TotalHours, 1)
                        : 0.0;

                    return new ClosedTicketRecordItem
                    {
                        TicketId = t.Id,
                        Title = t.ResolutionDetails,
                        TargetTeam = t.TargetTeam ?? "IT",
                        Type = t.Type,
                        Priority = t.Priority,
                        OpenedDT = t.OpenedDT,
                        ClosedDT = t.ClosedDT,
                        TurnaroundHours = turnaround,
                        RequesterName = t.OpenedByUser?.FullName ?? t.OpenedByUser?.DomainUser ?? "Unknown",
                        EquipmentTag = t.Asset?.HospitalNo ?? (t.PcName ?? "N/A"),
                        EquipmentBrand = t.Asset?.Brand,
                        AssetId = t.AssetId,
                        ResolutionNotes = t.ResolutionDetails,
                        ResolvedByUsers = collaborators
                    };
                }).ToList();

            // Active tickets assigned to targetUser
            var activeTickets = await _context.Tickets
                .Include(t => t.Asset)
                .Include(t => t.OpenedByUser)
                .Include(t => t.Handler)
                .Where(t => t.StatusId != 5 && t.StatusId != 4 && t.Handler != null && t.Handler.TechUserId == userId)
                .OrderByDescending(t => t.Priority)
                .ToListAsync();

            // Maintenance work orders performed by targetUser in date range
            var maintenanceLogs = await _context.AssetMaintenances
                .Include(m => m.Asset)
                .Where(m => m.UserId == userId &&
                            m.TimStamp.HasValue &&
                            m.TimStamp.Value >= rangeStart &&
                            m.TimStamp.Value <= rangeEnd)
                .OrderByDescending(m => m.TimStamp)
                .ToListAsync();

            var durations = userClosedTickets
                .Where(t => t.OpenedDT.HasValue && t.ClosedDT.HasValue && t.ClosedDT.Value >= t.OpenedDT.Value)
                .Select(t => (t.ClosedDT!.Value - t.OpenedDT!.Value).TotalHours)
                .ToList();

            double avgResHours = durations.Any() ? Math.Round(durations.Average(), 1) : 0.0;

            var categoryBreakdown = userClosedTickets
                .GroupBy(t => string.IsNullOrWhiteSpace(t.Type) ? "General" : t.Type)
                .Select(g => new CategoryAnalyticsStat
                {
                    Category = g.Key,
                    DisplayCategory = LocHelper.TranslateTicketType(g.Key),
                    Count = g.Count(),
                    Percentage = userClosedTickets.Count > 0 ? Math.Round(((double)g.Count() / userClosedTickets.Count) * 100.0, 1) : 0.0
                })
                .OrderByDescending(c => c.Count)
                .ToList();

            var reportVm = new TechnicianReportViewModel
            {
                Technician = targetUser,
                FromDate = rangeStart,
                ToDate = rangeEnd,
                DatePreset = selectedPreset,
                ClosedTicketsCount = userClosedTickets.Count,
                ActiveTicketsCount = activeTickets.Count,
                TotalHandledTickets = userClosedTickets.Count + activeTickets.Count,
                AverageResolutionHours = avgResHours,
                WorkOrdersCount = maintenanceLogs.Count,
                ResolvedTickets = closedTicketRecords,
                ActiveTickets = activeTickets,
                MaintenanceLogs = maintenanceLogs,
                CategoryBreakdown = categoryBreakdown
            };

            return View(reportVm);
        }

        // GET: Home/Privacy
        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View();
        }
    }
}
