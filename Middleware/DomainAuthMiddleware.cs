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

            // 1. Bypass authentication check for static assets and the AccessDenied rejection page & public ticket submission
            if (path.StartsWith("/css") ||
                path.StartsWith("/js") ||
                path.StartsWith("/lib") ||
                path.StartsWith("/images") ||
                path.StartsWith("/favicon.ico") ||
                path.StartsWith("/account/accessdenied") ||
                path.StartsWith("/account/submitaccessticket"))
            {
                await _next(context);
                return;
            }

            // 2. Resolve Physical Workstation / PC Username
            // In production, prioritize server-verified Windows identity (IIS / Kestrel Negotiate).
            string? rawUser = null;
            if (context.User?.Identity?.IsAuthenticated == true && !string.IsNullOrWhiteSpace(context.User.Identity.Name))
            {
                rawUser = context.User.Identity.Name;
            }

            // Reverse-proxy identity headers are honored ONLY if explicitly trusted in config and from loopback/trusted proxy
            bool trustProxyHeaders = configuration.GetValue<bool>("Authentication:TrustProxyHeaders");
            var remoteIp = context.Connection.RemoteIpAddress;
            bool isLoopbackOrLocal = remoteIp != null && (IPAddress.IsLoopback(remoteIp) || remoteIp.ToString() == "127.0.0.1" || remoteIp.ToString() == "::1");

            if (string.IsNullOrWhiteSpace(rawUser) && trustProxyHeaders && isLoopbackOrLocal)
            {
                rawUser = context.Request.Headers["LOGON_USER"].FirstOrDefault()
                    ?? context.Request.Headers["REMOTE_USER"].FirstOrDefault()
                    ?? context.Request.Headers["X-Remote-User"].FirstOrDefault();
            }

            // In local development environment ONLY, allow fallback to host process username for frictionless dev
            if (string.IsNullOrWhiteSpace(rawUser) && env.IsDevelopment())
            {
                rawUser = Environment.UserName;
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

            // If no username can be resolved at all, reject
            if (string.IsNullOrWhiteSpace(cleanUsername))
            {
                await SafeSignOutAsync(context);
                context.Response.Redirect("/Account/AccessDenied?reason=NotFound&username=Unknown");
                return;
            }

            // 3. Query database strictly for the physical PC user first
            var search = cleanUsername.ToLower();
            var rawSearch = rawUser?.ToLower();

            var pcUser = await dbContext.Users
                .Include(u => u.Department)
                .FirstOrDefaultAsync(u =>
                    (u.DomainUser != null && (u.DomainUser.ToLower() == search || (rawSearch != null && u.DomainUser.ToLower() == rawSearch))) ||
                    (u.FullName != null && u.FullName.ToLower() == search)
                );
            // 4. REJECTION CHECK 1: Physical PC user not registered in database
            if (pcUser == null)
            {
                await SafeSignOutAsync(context);
                context.Response.Redirect($"/Account/AccessDenied?reason=NotFound&username={Uri.EscapeDataString(cleanUsername)}");
                return;
            }

            // 5. REJECTION CHECK 2: Physical PC user is INACTIVE (isActive == 0 / false)
            if (pcUser.IsActive != true)
            {
                await SafeSignOutAsync(context);
                context.Response.Redirect($"/Account/AccessDenied?reason=Inactive&username={Uri.EscapeDataString(cleanUsername)}");
                return;
            }

            // 6. Check physical user admin privileges for optional testing profile switch
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

            // 7. AUTO-LOGIN: User found and ACTIVE!
            context.Items["CurrentUser"] = matchedUser;
            context.Items["PhysicalUser"] = pcUser;
            context.Items["IsPhysicalAdmin"] = isPhysicalAdmin;

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, matchedUser.Id.ToString()),
                new Claim(ClaimTypes.Name, matchedUser.FullName ?? matchedUser.DomainUser ?? cleanUsername),
                new Claim(ClaimTypes.Role, matchedUser.Role ?? SystemRoles.EndUser),
                new Claim("DomainUser", matchedUser.DomainUser ?? cleanUsername),
                new Claim("Department", matchedUser.Department?.Name ?? "General"),
                new Claim("PhysicalUserRole", pcUser.Role ?? SystemRoles.EndUser),
                new Claim("IsPhysicalAdmin", isPhysicalAdmin ? "true" : "false")
            };

            if (!string.IsNullOrWhiteSpace(matchedUser.MaintenanceTeam))
            {
                claims.Add(new Claim("MaintenanceTeam", matchedUser.MaintenanceTeam));
            }

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
