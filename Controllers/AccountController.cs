using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockPortal.Data;
using StockPortal.Models;
using StockPortal.Services;

namespace StockPortal.Controllers
{
    public class AccountController : Controller
    {
        private readonly StockDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public AccountController(StockDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        // POST: Account/SwitchUser (Only allowed for Admin users via POST with Anti-Forgery Token, URL GET tampering blocked)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SwitchUser(int id, string? returnUrl)
        {
            // Production Phase Security Guard: Only Admins can switch profiles
            if (!_currentUserService.CanSwitchProfile())
            {
                return Forbid();
            }

            if (id > 0)
            {
                var user = await _context.Users.FindAsync(id);
                if (user != null && user.IsActive == true)
                {
                    HttpContext.Session.SetInt32("ActiveUserId", user.Id);
                    TempData["SuccessMessage"] = $"Switched profile to: {user.FullName} ({user.Role})";
                }
                else
                {
                    TempData["ErrorMessage"] = "Cannot switch to an inactive or non-existent user.";
                }
            }
            else
            {
                // Revert to detected domain user
                HttpContext.Session.Remove("ActiveUserId");
                TempData["SuccessMessage"] = $"Reverted to Windows PC identity ({Environment.UserName}).";
            }

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction("Index", "Home");
        }

        // GET: Account/Profile (or Account/Profile?id=123 for Admin inspection)
        public async Task<IActionResult> Profile(int? id = null)
        {
            var currentUser = _currentUserService.GetCurrentUser();
            if (currentUser == null)
            {
                return RedirectToAction("Index", "Home");
            }

            // Production Phase Security Guard:
            // Non-admin users can ONLY access their own profile. Only Admins can view/inspect other users' profiles.
            User targetUser = currentUser;
            bool canSwitchProfile = _currentUserService.CanSwitchProfile();

            if (id.HasValue && id.Value > 0 && id.Value != currentUser.Id)
            {
                if (!canSwitchProfile && !_currentUserService.IsAdmin())
                {
                    return Forbid();
                }

                var specifiedUser = await _context.Users
                    .Include(u => u.Department)
                    .FirstOrDefaultAsync(u => u.Id == id.Value);

                if (specifiedUser == null)
                {
                    return NotFound();
                }

                targetUser = specifiedUser;
            }

            var myAssets = await _context.Assets
                .Where(a => a.UserId == targetUser.Id)
                .Include(a => a.Room)
                .Include(a => a.CustodyType)
                .ToListAsync();

            var myTickets = await _context.Tickets
                .Where(t => t.OpenedBy == targetUser.Id || t.OnBehalfOf == targetUser.Id)
                .Include(t => t.Asset)
                .Include(t => t.Status)
                .OrderByDescending(t => t.OpenedDT)
                .ToListAsync();

            var clientIp = NetworkInfoHelper.GetClientIpAddress(HttpContext);
            var clientMac = NetworkInfoHelper.GetClientMacAddress(HttpContext, clientIp);

            ViewBag.ClientIp = clientIp;
            ViewBag.ClientMac = clientMac;
            ViewBag.MyAssets = myAssets;
            ViewBag.MyTickets = myTickets;
            ViewBag.CanSwitchProfile = canSwitchProfile;
            ViewBag.IsViewingSelf = (targetUser.Id == currentUser.Id);

            if (canSwitchProfile)
            {
                ViewBag.AllUsers = await _context.Users
                    .Where(u => u.IsActive == true)
                    .OrderBy(u => u.FullName)
                    .ToListAsync();
            }
            else
            {
                ViewBag.AllUsers = new List<User>();
            }

            return View(targetUser);
        }

        // GET: Account/AccessDenied
        [HttpGet]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public async Task<IActionResult> AccessDenied(string? reason, string? username, string? returnUrl)
        {
            Response.StatusCode = StatusCodes.Status403Forbidden;
            var currentUser = _currentUserService.GetCurrentUser();

            // If user is authenticated and active, but got forbidden due to RBAC route guard
            if (currentUser != null && currentUser.IsActive == true)
            {
                ViewBag.Reason = "ForbiddenResource";
                ViewBag.ReturnUrl = returnUrl;
                ViewBag.CurrentUser = currentUser;
                return View();
            }

            ViewBag.Reason = reason ?? "NotFound";
            string resolvedUser = username ?? Environment.UserName;
            ViewBag.Username = resolvedUser;

            string pcName = Environment.MachineName;
            ViewBag.PcName = pcName;
            ViewBag.ClientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";

            // Resolve hardware asset matching PC name (hospitalNo in database)
            var asset = await _context.Assets
                .Include(a => a.Room)
                .Include(a => a.User)
                .FirstOrDefaultAsync(a => a.HospitalNo != null && a.HospitalNo.ToLower() == pcName.ToLower());

            ViewBag.MatchedAsset = asset;
            ViewBag.Rooms = await _context.Rooms.OrderBy(r => r.RoomNo).ToListAsync();

            return View();
        }

        // POST: Account/SubmitAccessTicket
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitAccessTicket(string? pcName, int? assetId, int? roomId, string? notes, string? username = null)
        {
            string hostName = string.IsNullOrWhiteSpace(pcName) ? Environment.MachineName : pcName.Trim();
            string cleanUsername = !string.IsNullOrWhiteSpace(username) ? username.Trim() : Environment.UserName;

            // Resolve target asset
            Asset? asset = null;
            if (assetId.HasValue && assetId.Value > 0)
            {
                asset = await _context.Assets.Include(a => a.Room).Include(a => a.User).FirstOrDefaultAsync(a => a.Id == assetId.Value);
            }
            if (asset == null && !string.IsNullOrWhiteSpace(hostName))
            {
                asset = await _context.Assets.Include(a => a.Room).Include(a => a.User)
                    .FirstOrDefaultAsync(a => a.HospitalNo != null && a.HospitalNo.ToLower() == hostName.ToLower());
            }

            // Update room if provided and missing
            Room? selectedRoom = null;
            if (roomId.HasValue && roomId.Value > 0)
            {
                selectedRoom = await _context.Rooms.FindAsync(roomId.Value);
                if (asset != null && (asset.RoomId == null || asset.RoomId != roomId.Value))
                {
                    asset.RoomId = roomId.Value;
                    _context.Update(asset);
                }
            }
            else if (asset?.Room != null)
            {
                selectedRoom = asset.Room;
            }

            // Resolve user to attribute ticket
            var pcUser = await _context.Users.FirstOrDefaultAsync(u =>
                (u.DomainUser != null && (u.DomainUser.ToLower() == cleanUsername.ToLower() || u.DomainUser.ToLower().StartsWith(cleanUsername.ToLower()) || cleanUsername.ToLower().StartsWith(u.DomainUser.ToLower()))) ||
                (u.FullName != null && u.FullName.ToLower().StartsWith(cleanUsername.ToLower()))
            );

            int ticketUserId = pcUser?.Id ?? (asset?.UserId ?? 1);

            var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
            var clientMac = NetworkInfoHelper.GetClientMacAddress(HttpContext, clientIp);
            string routingTeam = !string.IsNullOrWhiteSpace(asset?.MaintenanceTeam) ? asset.MaintenanceTeam : MaintenanceTeams.IT;

            // Create technical access ticket
            var ticket = new Ticket
            {
                AssetId = asset?.Id,
                OpenedBy = ticketUserId,
                OnBehalfOf = ticketUserId,
                Type = "Access",
                Priority = 3, // High
                StatusId = 1, // Opened
                OpenedDT = DateTime.Now,
                TargetTeam = routingTeam,
                PcName = hostName,
                IpAddress = clientIp,
                MacAddress = clientMac
            };

            _context.Tickets.Add(ticket);
            await _context.SaveChangesAsync();

            // Construct telemetry message
            var roomInfo = selectedRoom != null ? $"Room {selectedRoom.RoomNo} (Floor {selectedRoom.Floor})" : "Unspecified";
            var hardwareInfo = asset != null 
                ? $"{asset.Brand ?? "Generic"} {(asset.ModelNo.HasValue ? "Model " + asset.ModelNo : "")} [S/N: {asset.SerialNo ?? "N/A"}]" 
                : "Workstation not yet tagged in Assets database";

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("🖥️ AUTOMATED SYSTEM & WORKSTATION REPORT (LOGIN FAILURE)");
            sb.AppendLine($"• Workstation / PC Name : {hostName}");
            sb.AppendLine($"• Location              : {roomInfo}");
            sb.AppendLine($"• Hardware Details      : {hardwareInfo}");
            sb.AppendLine($"• Locked-out User       : {cleanUsername} (Registered ID: {(pcUser != null ? pcUser.Id.ToString() : "Unregistered")})");
            sb.AppendLine($"• Client Host / IP      : {HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1"}");
            sb.AppendLine($"• Timestamp             : {DateTime.Now:yyyy-MM-dd HH:mm:ss}");

            _context.TicketMessages.Add(new TicketMsg
            {
                TicketId = ticket.Id,
                UserId = ticketUserId,
                Msg = sb.ToString(),
                TimeStamp = DateTime.Now
            });

            // Add user manual notes if provided
            if (!string.IsNullOrWhiteSpace(notes))
            {
                _context.TicketMessages.Add(new TicketMsg
                {
                    TicketId = ticket.Id,
                    UserId = ticketUserId,
                    Msg = $"📝 USER NOTES:\n{notes.Trim()}",
                    TimeStamp = DateTime.Now.AddSeconds(1)
                });
            }

            await _context.SaveChangesAsync();

            TempData["SubmittedTicketId"] = ticket.Id;
            return RedirectToAction(nameof(AccessDenied), new { reason = "TicketSubmitted", username = cleanUsername });
        }
    }
}
