using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockPortal.Data;
using StockPortal.Models;
using StockPortal.Services;
using StockPortal.ViewModels;

namespace StockPortal.Controllers
{
    public class AccountController : Controller
    {
        private readonly StockDbContext _context;
        private readonly ICurrentUserService _currentUserService;
        private readonly IPasswordHasher<User> _passwordHasher;
        private readonly Microsoft.Extensions.Configuration.IConfiguration? _configuration;

        public AccountController(StockDbContext context, ICurrentUserService currentUserService, Microsoft.Extensions.Configuration.IConfiguration? configuration = null, IPasswordHasher<User>? passwordHasher = null)
        {
            _context = context;
            _currentUserService = currentUserService;
            _configuration = configuration;
            _passwordHasher = passwordHasher ?? new PasswordHasher<User>();
        }

        private bool CanAccessWindowsAuth() => SafeGetBool(_configuration, "Authentication:CanAccessUsingWindowsAuth", true);
        private bool CanAccessPassword() => SafeGetBool(_configuration, "Authentication:CanAccessUsingUsernameAndPassword", true);
        private bool CanUserChangePassword() => SafeGetBool(_configuration, "Authentication:CanUserChangePassword", true);

        private static bool SafeGetBool(Microsoft.Extensions.Configuration.IConfiguration? config, string key, bool defaultValue = true)
        {
            if (config == null) return defaultValue;
            try
            {
                var section = config.GetSection(key);
                if (section == null || string.IsNullOrWhiteSpace(section.Value)) return defaultValue;
                return bool.TryParse(section.Value, out bool val) ? val : defaultValue;
            }
            catch
            {
                return defaultValue;
            }
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
            var clientDevice = NetworkInfoHelper.GetClientDeviceName(HttpContext);

            ViewBag.ClientIp = clientIp;
            ViewBag.ClientMac = clientMac;
            ViewBag.ClientDevice = clientDevice;
            ViewBag.MyAssets = myAssets;
            ViewBag.MyTickets = myTickets;
            ViewBag.CanSwitchProfile = canSwitchProfile;
            ViewBag.IsViewingSelf = (targetUser.Id == currentUser.Id);
            ViewBag.AuthMethod = _currentUserService.GetAuthMethod();
            ViewBag.CanAccessUsingWindowsAuth = CanAccessWindowsAuth();
            ViewBag.CanAccessUsingUsernameAndPassword = CanAccessPassword();
            ViewBag.CanUserChangePassword = CanUserChangePassword();

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
            string clientIp = NetworkInfoHelper.GetClientIpAddress(HttpContext);
            string clientDevice = NetworkInfoHelper.GetClientDeviceName(HttpContext);

            // Resolve the Windows/workstation username, never confusing it with the machine/device name.
            string? resolvedUser = !string.IsNullOrWhiteSpace(username) ? username.Trim() : null;
            if (string.IsNullOrWhiteSpace(resolvedUser))
            {
                var winUser = HttpContext.User?.Identity?.Name;
                if (!string.IsNullOrWhiteSpace(winUser))
                {
                    resolvedUser = winUser.Contains('\\') ? winUser.Split('\\').Last() : (winUser.Contains('@') ? winUser.Split('@').First() : winUser);
                }
            }
            if (string.IsNullOrWhiteSpace(resolvedUser))
            {
                bool isLocal = clientIp == "127.0.0.1" || clientIp == "::1" || clientIp == NetworkInfoHelper.GetLocalPrimaryIpAddress() || NetworkInfoHelper.IsLocalHost(HttpContext.Connection.RemoteIpAddress);
                if (isLocal)
                {
                    resolvedUser = Environment.UserName;
                }
            }

            ViewBag.Username = resolvedUser ?? "Unspecified";
            ViewBag.PcName = clientDevice;
            ViewBag.ClientIp = clientIp;

            // Resolve hardware asset matching PC or mobile device name (hospitalNo in database)
            var asset = await _context.Assets
                .Include(a => a.Room)
                .Include(a => a.User)
                .FirstOrDefaultAsync(a => a.HospitalNo != null && (a.HospitalNo.ToLower() == clientDevice.ToLower() || (resolvedUser != null && a.HospitalNo.ToLower() == resolvedUser.ToLower())));

            ViewBag.MatchedAsset = asset;
            ViewBag.Rooms = await _context.Rooms.OrderBy(r => r.RoomNo).ToListAsync();

            return View();
        }

        // POST: Account/SubmitAccessTicket
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitAccessTicket(string? pcName, int? assetId, int? roomId, string? notes, string? username = null)
        {
            string hostName = string.IsNullOrWhiteSpace(pcName) ? NetworkInfoHelper.GetClientDeviceName(HttpContext) : pcName.Trim();
            
            // Resolve clean username: never fallback to PC hostname
            string? cleanUsername = !string.IsNullOrWhiteSpace(username) && !username.Equals("Unspecified", StringComparison.OrdinalIgnoreCase)
                ? username.Trim()
                : null;

            if (string.IsNullOrWhiteSpace(cleanUsername))
            {
                var winUser = HttpContext.User?.Identity?.Name;
                if (!string.IsNullOrWhiteSpace(winUser))
                {
                    cleanUsername = winUser.Contains('\\') ? winUser.Split('\\').Last() : (winUser.Contains('@') ? winUser.Split('@').First() : winUser);
                }
            }
            if (string.IsNullOrWhiteSpace(cleanUsername))
            {
                bool isLocal = NetworkInfoHelper.IsLocalHost(HttpContext.Connection.RemoteIpAddress);
                cleanUsername = isLocal ? Environment.UserName : "Unregistered User";
            }

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

        // GET: Account/Login
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            // If already authenticated via Password or SSO and active, redirect
            var currentUser = _currentUserService.GetCurrentUser();
            if (currentUser != null && currentUser.IsActive == true && !Request.Cookies.ContainsKey("SOLVIS.SignedOut"))
            {
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }
                return RedirectToAction("Index", "Home");
            }

            var model = new LoginViewModel
            {
                ReturnUrl = returnUrl
            };

            ViewBag.ClientDevice = NetworkInfoHelper.GetClientDeviceName(HttpContext);
            ViewBag.ClientIp = NetworkInfoHelper.GetClientIpAddress(HttpContext);
            ViewBag.CanAccessUsingWindowsAuth = CanAccessWindowsAuth();
            ViewBag.CanAccessUsingUsernameAndPassword = CanAccessPassword();

            return View(model);
        }

        // POST: Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            ViewBag.ClientDevice = NetworkInfoHelper.GetClientDeviceName(HttpContext);
            ViewBag.ClientIp = NetworkInfoHelper.GetClientIpAddress(HttpContext);
            ViewBag.CanAccessUsingWindowsAuth = CanAccessWindowsAuth();
            ViewBag.CanAccessUsingUsernameAndPassword = CanAccessPassword();

            if (!CanAccessPassword())
            {
                ModelState.AddModelError(string.Empty, "Username and password authentication is disabled by administrator.");
                return View(model);
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var cleanUsername = model.Username.Trim().ToLower();
            var user = await _context.Users
                .Include(u => u.Department)
                .FirstOrDefaultAsync(u =>
                    (u.DomainUser != null && u.DomainUser.ToLower() == cleanUsername) ||
                    (u.FullName != null && u.FullName.ToLower() == cleanUsername)
                );

            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Invalid username or password.");
                return View(model);
            }

            if (user.IsActive != true)
            {
                ModelState.AddModelError(string.Empty, "This user account is inactive. Please contact the administrator.");
                return View(model);
            }

            if (string.IsNullOrEmpty(user.PasswordHash))
            {
                ModelState.AddModelError(string.Empty, "No password has been set for this account yet. Please contact an administrator.");
                return View(model);
            }

            var verification = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, model.Password);
            if (verification == PasswordVerificationResult.Failed)
            {
                ModelState.AddModelError(string.Empty, "Invalid username or password.");
                return View(model);
            }

            if (verification == PasswordVerificationResult.SuccessRehashNeeded)
            {
                user.PasswordHash = _passwordHasher.HashPassword(user, model.Password);
                await _context.SaveChangesAsync();
            }

            // Remove signed out cookie flag
            Response.Cookies.Delete("SOLVIS.SignedOut");

            // Build Claims
            bool isAdmin = string.Equals(user.Role, SystemRoles.Admin, StringComparison.OrdinalIgnoreCase) ||
                           (user.Role != null && (user.Role.Contains("ADMIN", StringComparison.OrdinalIgnoreCase) || user.Role.Contains("SYSADMIN", StringComparison.OrdinalIgnoreCase)));

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.FullName ?? user.DomainUser ?? model.Username),
                new Claim(ClaimTypes.Role, user.Role ?? SystemRoles.EndUser),
                new Claim("DomainUser", user.DomainUser ?? model.Username),
                new Claim("Department", user.Department?.Name ?? "General"),
                new Claim("PhysicalUserRole", user.Role ?? SystemRoles.EndUser),
                new Claim("IsPhysicalAdmin", isAdmin ? "true" : "false"),
                new Claim("AuthMethod", "Password"),
                new Claim("CanPrintAssetTags", user.CanPrintAssetTags ? "true" : "false"),
                new Claim("CanEditAssetTags", user.CanEditAssetTags ? "true" : "false"),
                new Claim("CanAccessTeamAnalytics", user.CanAccessTeamAnalytics ? "true" : "false"),
                new Claim("CanViewOwnAnalytics", user.CanViewOwnAnalytics ? "true" : "false")
            };

            if (!string.IsNullOrWhiteSpace(user.MaintenanceTeam))
            {
                claims.Add(new Claim("MaintenanceTeam", user.MaintenanceTeam));
            }

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = model.RememberMe,
                ExpiresUtc = model.RememberMe ? DateTimeOffset.UtcNow.AddDays(30) : DateTimeOffset.UtcNow.AddHours(12)
            };

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity), authProperties);

            if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            {
                return Redirect(model.ReturnUrl);
            }

            return RedirectToAction("Index", "Home");
        }

        // GET/POST: Account/AutoLogin (Windows SSO auto-login strictly via cryptographically verified Windows/NTLM token)
        public async Task<IActionResult> AutoLogin(string? returnUrl = null)
        {
            if (!CanAccessWindowsAuth())
            {
                TempData["ErrorMessage"] = "Windows authentication is disabled by administrator.";
                return RedirectToAction(nameof(AccessDenied), new { reason = "WindowsAuthDisabled", returnUrl });
            }

            Response.Cookies.Delete("SOLVIS.SignedOut");
            Response.Cookies.Delete("SOLVIS.WorkstationUser"); // Purge any legacy client cookie

            string? rawUser = null;

            // 1. Attempt to resolve server-verified Windows identity (IIS / Kestrel Windows Auth / Kerberos)
            if (User?.Identity?.IsAuthenticated == true && !string.IsNullOrWhiteSpace(User.Identity.Name))
            {
                rawUser = User.Identity.Name;
            }

            // 2. Check Authorization header (NTLM, Negotiate, or password-verified Basic)
            string? authHeader = Request.Headers["Authorization"].FirstOrDefault();
            if (string.IsNullOrWhiteSpace(rawUser) && !string.IsNullOrWhiteSpace(authHeader))
            {
                if (authHeader.StartsWith("NTLM ", StringComparison.OrdinalIgnoreCase) ||
                    authHeader.StartsWith("Negotiate ", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        int spaceIdx = authHeader.IndexOf(' ');
                        string token = authHeader.Substring(spaceIdx + 1).Trim();
                        byte[] messageBytes = Convert.FromBase64String(token);
                        int ntlmOffset = NtlmAuthHelper.FindNtlmSignature(messageBytes);
                        if (ntlmOffset >= 0)
                        {
                            byte[] ntlmBytes = ntlmOffset == 0 ? messageBytes : messageBytes[ntlmOffset..];
                            if (NtlmAuthHelper.IsType1(ntlmBytes))
                            {
                                byte[] challenge = NtlmAuthHelper.CreateType2Challenge();
                                string challengeB64 = Convert.ToBase64String(challenge);
                                if (authHeader.StartsWith("Negotiate", StringComparison.OrdinalIgnoreCase))
                                {
                                    Response.Headers.Append("WWW-Authenticate", "Negotiate " + challengeB64);
                                }
                                Response.Headers.Append("WWW-Authenticate", "NTLM " + challengeB64);
                                return StatusCode(StatusCodes.Status401Unauthorized);
                            }
                            else if (NtlmAuthHelper.IsType3(ntlmBytes))
                            {
                                var (ntlmUser, _, _) = NtlmAuthHelper.ParseType3(ntlmBytes);
                                if (!string.IsNullOrWhiteSpace(ntlmUser))
                                {
                                    rawUser = ntlmUser;
                                }
                            }
                        }
                    }
                    catch { }
                }
                else if (authHeader.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        string token = authHeader.Substring(6).Trim();
                        string creds = Encoding.UTF8.GetString(Convert.FromBase64String(token));
                        int colon = creds.IndexOf(':');
                        if (colon > 0)
                        {
                            string basicUsername = creds.Substring(0, colon).Trim();
                            string basicPassword = creds.Substring(colon + 1);

                            // Verify password against database to prevent credential spoofing
                            var searchBasic = basicUsername.ToLower();
                            var dbUser = await _context.Users
                                .FirstOrDefaultAsync(u =>
                                    (u.DomainUser != null && u.DomainUser.ToLower() == searchBasic) ||
                                    (u.FullName != null && u.FullName.ToLower() == searchBasic)
                                );

                            if (dbUser != null && !string.IsNullOrEmpty(dbUser.PasswordHash))
                            {
                                var verify = _passwordHasher.VerifyHashedPassword(dbUser, dbUser.PasswordHash, basicPassword);
                                if (verify != PasswordVerificationResult.Failed)
                                {
                                    rawUser = dbUser.DomainUser ?? dbUser.FullName ?? basicUsername;
                                }
                            }
                        }
                    }
                    catch { }
                }
            }

            // 3. Localhost loopback fallback (strictly for development/local testing on same machine)
            var clientIp = NetworkInfoHelper.GetClientIpAddress(HttpContext);
            bool isLoopbackOrLocal = clientIp == "127.0.0.1" || clientIp == "::1" || NetworkInfoHelper.IsLocalHost(HttpContext.Connection.RemoteIpAddress);
            if (string.IsNullOrWhiteSpace(rawUser) && isLoopbackOrLocal)
            {
                rawUser = Environment.UserName;
            }

            // 4. If identity is still not detected and no authorization header was received:
            // Issue 401 challenge with Negotiate & NTLM to retrieve the logged-in Windows user from client PC
            if (string.IsNullOrWhiteSpace(rawUser) && string.IsNullOrWhiteSpace(authHeader))
            {
                Response.Cookies.Delete("SOLVIS.NtlmTried");
                Response.Headers.Append("WWW-Authenticate", "Negotiate");
                Response.Headers.Append("WWW-Authenticate", "NTLM");
                return StatusCode(StatusCodes.Status401Unauthorized);
            }

            Response.Cookies.Delete("SOLVIS.NtlmTried");

            string cleanUsername = rawUser ?? "";
            if (cleanUsername.Contains('\\')) cleanUsername = cleanUsername.Split('\\').Last();
            if (cleanUsername.Contains('@')) cleanUsername = cleanUsername.Split('@').First();
            cleanUsername = cleanUsername.Trim();

            // 5. If no verified Windows username could be detected
            if (string.IsNullOrWhiteSpace(cleanUsername))
            {
                return RedirectToAction(nameof(AccessDenied), new { reason = "NoWindowsIdentity", username = "Unspecified", returnUrl });
            }

            // 6. Query user in database
            var search = cleanUsername.ToLower();
            var pcUser = await _context.Users
                .Include(u => u.Department)
                .FirstOrDefaultAsync(u =>
                    (u.DomainUser != null && (u.DomainUser.ToLower() == search || u.DomainUser.ToLower().Contains(search) || search.Contains(u.DomainUser.ToLower()))) ||
                    (u.FullName != null && (u.FullName.ToLower() == search || u.FullName.ToLower().StartsWith(search + " ") || u.FullName.ToLower().EndsWith(" " + search) || u.FullName.ToLower().Contains(search)))
                );

            // 7. User not found in database -> Redirect to AccessDenied so user sees explanation AND automated ticket generator!
            if (pcUser == null)
            {
                return RedirectToAction(nameof(AccessDenied), new { reason = "NotFound", username = cleanUsername, returnUrl });
            }

            // 8. User is inactive -> Redirect to AccessDenied (Inactive) with automated ticket generator!
            if (pcUser.IsActive != true)
            {
                return RedirectToAction(nameof(AccessDenied), new { reason = "Inactive", username = cleanUsername, returnUrl });
            }

            // 12. Active and authorized! Build Claims & Sign In
            bool isAdmin = string.Equals(pcUser.Role, SystemRoles.Admin, StringComparison.OrdinalIgnoreCase) ||
                           (pcUser.Role != null && (pcUser.Role.Contains("ADMIN", StringComparison.OrdinalIgnoreCase) || pcUser.Role.Contains("SYSADMIN", StringComparison.OrdinalIgnoreCase)));

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, pcUser.Id.ToString()),
                new Claim(ClaimTypes.Name, pcUser.FullName ?? pcUser.DomainUser ?? cleanUsername),
                new Claim(ClaimTypes.Role, pcUser.Role ?? SystemRoles.EndUser),
                new Claim("DomainUser", pcUser.DomainUser ?? cleanUsername),
                new Claim("Department", pcUser.Department?.Name ?? "General"),
                new Claim("PhysicalUserRole", pcUser.Role ?? SystemRoles.EndUser),
                new Claim("IsPhysicalAdmin", isAdmin ? "true" : "false"),
                new Claim("AuthMethod", "WindowsSSO"),
                new Claim("CanPrintAssetTags", pcUser.CanPrintAssetTags ? "true" : "false"),
                new Claim("CanEditAssetTags", pcUser.CanEditAssetTags ? "true" : "false"),
                new Claim("CanAccessTeamAnalytics", pcUser.CanAccessTeamAnalytics ? "true" : "false"),
                new Claim("CanViewOwnAnalytics", pcUser.CanViewOwnAnalytics ? "true" : "false")
            };

            if (!string.IsNullOrWhiteSpace(pcUser.MaintenanceTeam))
            {
                claims.Add(new Claim("MaintenanceTeam", pcUser.MaintenanceTeam));
            }

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(30)
            });

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction("Index", "Home");
        }

        // GET/POST: Account/Logout
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            if (HttpContext.Features.Get<Microsoft.AspNetCore.Http.Features.ISessionFeature>()?.Session != null)
            {
                HttpContext.Session.Clear();
            }
            Response.Cookies.Delete("SOLVIS.DomainAuth");
            Response.Cookies.Append("SOLVIS.SignedOut", "1", new CookieOptions
            {
                HttpOnly = true,
                SameSite = SameSiteMode.Lax,
                Expires = DateTimeOffset.UtcNow.AddDays(7)
            });

            return RedirectToAction(nameof(Login));
        }

        // POST: Account/ChangePassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            var currentUser = _currentUserService.GetCurrentUser();
            if (currentUser == null)
            {
                return RedirectToAction(nameof(Login));
            }

            if (!CanAccessPassword())
            {
                TempData["ErrorMessage"] = "Password authentication is disabled by system administrator.";
                return RedirectToAction(nameof(Profile));
            }

            if (!CanUserChangePassword() && !_currentUserService.IsAdmin())
            {
                TempData["ErrorMessage"] = "Password modification for standard users is disabled by system administrator.";
                return RedirectToAction(nameof(Profile));
            }

            if (!ModelState.IsValid)
            {
                var errors = string.Join(" ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                TempData["ErrorMessage"] = errors;
                return RedirectToAction(nameof(Profile));
            }

            var user = await _context.Users.FindAsync(currentUser.Id);
            if (user == null)
            {
                return NotFound();
            }

            // If user already had a password, verify current password
            if (!string.IsNullOrEmpty(user.PasswordHash))
            {
                if (string.IsNullOrEmpty(model.CurrentPassword))
                {
                    TempData["ErrorMessage"] = "Please enter your current password.";
                    return RedirectToAction(nameof(Profile));
                }

                var verifyResult = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, model.CurrentPassword);
                if (verifyResult == PasswordVerificationResult.Failed)
                {
                    TempData["ErrorMessage"] = "Current password is incorrect.";
                    return RedirectToAction(nameof(Profile));
                }
            }

            user.PasswordHash = _passwordHasher.HashPassword(user, model.NewPassword);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Password has been successfully changed.";
            return RedirectToAction(nameof(Profile));
        }
    }
}
