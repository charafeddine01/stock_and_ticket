using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using StockPortal.Data;
using StockPortal.Models;
using StockPortal.Services;

namespace StockPortal.Middleware
{
    public class DomainAuthMiddleware
    {
        private readonly RequestDelegate _next;

        public DomainAuthMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, StockDbContext dbContext, IConfiguration configuration, IHostEnvironment env)
        {
            var path = context.Request.Path.Value?.ToLower() ?? "";

            // 1. Bypass authentication check for static assets, Login, Logout, AutoLogin, AccessDenied rejection page & public ticket submission
            if (path.StartsWith("/css") ||
                path.StartsWith("/js") ||
                path.StartsWith("/lib") ||
                path.StartsWith("/images") ||
                path.StartsWith("/favicon.ico") ||
                path.StartsWith("/account/login") ||
                path.StartsWith("/account/logout") ||
                path.StartsWith("/account/autologin") ||
                path.StartsWith("/account/accessdenied") ||
                path.StartsWith("/account/submitaccessticket"))
            {
                await _next(context);
                return;
            }

            // 2. Check if user already has an active, authenticated Cookie session
            var idClaim = context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (context.User?.Identity?.IsAuthenticated == true && int.TryParse(idClaim, out int authUserId) && authUserId > 0)
            {
                var cookieUser = await dbContext.Users
                    .Include(u => u.Department)
                    .FirstOrDefaultAsync(u => u.Id == authUserId);

                if (cookieUser != null && cookieUser.IsActive == true)
                {
                    bool isAuthAdmin = string.Equals(cookieUser.Role, SystemRoles.Admin, StringComparison.OrdinalIgnoreCase) ||
                                       (cookieUser.Role != null && (cookieUser.Role.Contains("ADMIN", StringComparison.OrdinalIgnoreCase) || cookieUser.Role.Contains("SYSADMIN", StringComparison.OrdinalIgnoreCase)));

                    User effectiveUser = cookieUser;
                    var cookieSessionUserId = HasSession(context) ? context.Session.GetInt32("ActiveUserId") : null;

                    if (cookieSessionUserId.HasValue)
                    {
                        if (isAuthAdmin)
                        {
                            var switchedUser = await dbContext.Users
                                .Include(u => u.Department)
                                .FirstOrDefaultAsync(u => u.Id == cookieSessionUserId.Value);

                            if (switchedUser != null && switchedUser.IsActive == true)
                            {
                                effectiveUser = switchedUser;
                            }
                            else if (HasSession(context))
                            {
                                context.Session.Remove("ActiveUserId");
                            }
                        }
                        else if (HasSession(context))
                        {
                            context.Session.Remove("ActiveUserId");
                        }
                    }

                    context.Items["CurrentUser"] = effectiveUser;
                    context.Items["PhysicalUser"] = cookieUser;
                    context.Items["IsPhysicalAdmin"] = isAuthAdmin;
                    context.Items["AuthMethod"] = context.User.FindFirst("AuthMethod")?.Value ?? "Password";

                    await _next(context);
                    return;
                }
                else
                {
                    await SafeSignOutAsync(context);
                    context.Response.Redirect("/Account/Login");
                    return;
                }
            }

            // 3. Check if user explicitly logged out on this browser
            if (context.Request.Cookies.ContainsKey("SOLVIS.SignedOut"))
            {
                context.Response.Redirect($"/Account/Login?returnUrl={Uri.EscapeDataString(context.Request.Path + context.Request.QueryString)}");
                return;
            }

            // 4. Resolve Physical Workstation / PC Username for Windows SSO Auto-Login
            // In production, prioritize server-verified Windows identity (IIS / Kestrel Negotiate).
            string? rawUser = null;
            if (context.User?.Identity?.IsAuthenticated == true && !string.IsNullOrWhiteSpace(context.User.Identity.Name))
            {
                rawUser = context.User.Identity.Name;
            }

            // Reverse-proxy identity headers are honored ONLY if explicitly trusted in config and from loopback/trusted proxy
            bool trustProxyHeaders = configuration.GetValue<bool>("Authentication:TrustProxyHeaders");
            var remoteIp = context.Connection.RemoteIpAddress;
            bool isLoopbackOrLocal = remoteIp != null && NetworkInfoHelper.IsLocalHost(remoteIp);

            if (string.IsNullOrWhiteSpace(rawUser) && trustProxyHeaders && isLoopbackOrLocal)
            {
                rawUser = context.Request.Headers["LOGON_USER"].FirstOrDefault()
                    ?? context.Request.Headers["REMOTE_USER"].FirstOrDefault()
                    ?? context.Request.Headers["X-Remote-User"].FirstOrDefault();
            }

            // In local development environment, allow fallback to host developer username ONLY for loopback (PC itself).
            // Remote devices / phones connecting over LAN must never automatically inherit the developer/admin account!
            if (string.IsNullOrWhiteSpace(rawUser))
            {
                if (env.IsDevelopment() && isLoopbackOrLocal)
                {
                    rawUser = Environment.UserName;
                }
                else
                {
                    // Phones and non-local network devices: resolve client device name
                    rawUser = NetworkInfoHelper.GetClientDeviceName(context);
                }
            }

            // Normalize PC username: strip DOMAIN\ or user@domain
            string cleanUsername = rawUser ?? "";
            if (cleanUsername.Contains('\\'))
            {
                cleanUsername = cleanUsername.Split('\\').Last();
            }
            if (cleanUsername.Contains('@'))
            {
                cleanUsername = cleanUsername.Split('@').First();
            }
            cleanUsername = cleanUsername.Trim();

            // If no username can be resolved at all, redirect to Login
            if (string.IsNullOrWhiteSpace(cleanUsername))
            {
                await SafeSignOutAsync(context);
                context.Response.Redirect($"/Account/Login?returnUrl={Uri.EscapeDataString(context.Request.Path + context.Request.QueryString)}");
                return;
            }

            // 5. Query database strictly for the physical PC user first
            var search = cleanUsername.ToLower();
            var rawSearch = rawUser?.ToLower();

            var pcUser = await dbContext.Users
                .Include(u => u.Department)
                .FirstOrDefaultAsync(u =>
                    (u.DomainUser != null && (u.DomainUser.ToLower() == search || (rawSearch != null && u.DomainUser.ToLower() == rawSearch))) ||
                    (u.FullName != null && u.FullName.ToLower() == search)
                );

            // 6. REJECTION CHECK 1: Physical PC user not registered in database -> Redirect to Login
            if (pcUser == null)
            {
                await SafeSignOutAsync(context);
                context.Response.Redirect($"/Account/Login?returnUrl={Uri.EscapeDataString(context.Request.Path + context.Request.QueryString)}");
                return;
            }

            // 7. REJECTION CHECK 2: Physical PC user is INACTIVE (isActive == 0 / false)
            if (pcUser.IsActive != true)
            {
                await SafeSignOutAsync(context);
                context.Response.Redirect($"/Account/AccessDenied?reason=Inactive&username={Uri.EscapeDataString(cleanUsername)}");
                return;
            }

            // 8. Check physical user admin privileges for optional testing profile switch
            bool isPhysicalAdmin = string.Equals(pcUser.Role, SystemRoles.Admin, StringComparison.OrdinalIgnoreCase) ||
                                   (pcUser.Role != null && (pcUser.Role.Contains("ADMIN", StringComparison.OrdinalIgnoreCase) || pcUser.Role.Contains("SYSADMIN", StringComparison.OrdinalIgnoreCase)));

            User matchedUser = pcUser;
            var sessionUserId = HasSession(context) ? context.Session.GetInt32("ActiveUserId") : null;

            if (sessionUserId.HasValue)
            {
                // Production Phase Security Guard:
                // Only physical Administrators are permitted to switch profiles for testing.
                if (isPhysicalAdmin)
                {
                    var switchedUser = await dbContext.Users
                        .Include(u => u.Department)
                        .FirstOrDefaultAsync(u => u.Id == sessionUserId.Value);

                    if (switchedUser != null && switchedUser.IsActive == true)
                    {
                        matchedUser = switchedUser;
                    }
                    else if (HasSession(context))
                    {
                        context.Session.Remove("ActiveUserId");
                    }
                }
                else if (HasSession(context))
                {
                    // Non-admin physical user attempting to use ActiveUserId: clear session immediately
                    context.Session.Remove("ActiveUserId");
                }
            }

            // 9. AUTO-LOGIN: User found and ACTIVE!
            context.Items["CurrentUser"] = matchedUser;
            context.Items["PhysicalUser"] = pcUser;
            context.Items["IsPhysicalAdmin"] = isPhysicalAdmin;
            context.Items["AuthMethod"] = "WindowsSSO";

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, matchedUser.Id.ToString()),
                new Claim(ClaimTypes.Name, matchedUser.FullName ?? matchedUser.DomainUser ?? cleanUsername),
                new Claim(ClaimTypes.Role, matchedUser.Role ?? SystemRoles.EndUser),
                new Claim("DomainUser", matchedUser.DomainUser ?? cleanUsername),
                new Claim("Department", matchedUser.Department?.Name ?? "General"),
                new Claim("PhysicalUserRole", pcUser.Role ?? SystemRoles.EndUser),
                new Claim("IsPhysicalAdmin", isPhysicalAdmin ? "true" : "false"),
                new Claim("AuthMethod", "WindowsSSO")
            };

            if (!string.IsNullOrWhiteSpace(matchedUser.MaintenanceTeam))
            {
                claims.Add(new Claim("MaintenanceTeam", matchedUser.MaintenanceTeam));
            }

            claims.Add(new Claim("CanPrintAssetTags", matchedUser.CanPrintAssetTags ? "true" : "false"));
            claims.Add(new Claim("CanEditAssetTags", matchedUser.CanEditAssetTags ? "true" : "false"));
            claims.Add(new Claim("CanAccessTeamAnalytics", matchedUser.CanAccessTeamAnalytics ? "true" : "false"));
            claims.Add(new Claim("CanViewOwnAnalytics", matchedUser.CanViewOwnAnalytics ? "true" : "false"));

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            context.User = new ClaimsPrincipal(identity);

            await _next(context);
        }

        private static bool HasSession(HttpContext context)
        {
            return context.Features.Get<Microsoft.AspNetCore.Http.Features.ISessionFeature>()?.Session != null;
        }

        private static async Task SafeSignOutAsync(HttpContext context)
        {
            if (HasSession(context))
            {
                context.Session.Clear();
            }
            if (context.RequestServices?.GetService<IAuthenticationService>() != null)
            {
                await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            }
            context.Response.Cookies.Delete("SOLVIS.DomainAuth");
        }
    }

    public static class DomainAuthMiddlewareExtensions
    {
        public static IApplicationBuilder UseDomainAuthentication(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<DomainAuthMiddleware>();
        }
    }
}
