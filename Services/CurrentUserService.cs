using System;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using StockPortal.Models;

namespace StockPortal.Services
{
    public interface ICurrentUserService
    {
        User? GetCurrentUser();
        int GetUserId();
        string GetFullName();
        string GetDomainUser();
        string GetRole();
        string? GetMaintenanceTeam();
        string GetDepartmentName();
        bool IsAuthenticated();
        User? GetPhysicalUser();
        bool CanSwitchProfile();

        // RBAC Role Helpers
        bool IsAdmin();
        bool IsMaintenance();
        bool IsEndUser();

        // Resource Authorization Guards
        bool CanAccessTicket(Ticket ticket);
        bool CanManageTicket(Ticket ticket);
        bool CanAccessAsset(Asset asset);
        bool CanManageAssets();
        bool CanManageAsset(Asset asset);
        bool CanCreateAssetInDepartment(string department);
        bool CanManageDeviceForms();
        bool CanManageDeviceForm(DeviceForm form);
        bool CanManageUsers();
        bool CanAccessDashboard();
    }

    public class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CurrentUserService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public User? GetCurrentUser()
        {
            var context = _httpContextAccessor.HttpContext;
            if (context == null) return null;

            if (context.Items.TryGetValue("CurrentUser", out var userObj) && userObj is User user)
            {
                return user;
            }

            return null;
        }

        public int GetUserId()
        {
            var user = GetCurrentUser();
            if (user != null) return user.Id;

            var claim = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier);
            if (claim != null && int.TryParse(claim.Value, out int id))
            {
                return id;
            }

            return 0; // Unauthenticated / no active user
        }

        public string GetFullName()
        {
            var user = GetCurrentUser();
            return user?.FullName ?? _httpContextAccessor.HttpContext?.User.Identity?.Name ?? "Domain User";
        }

        public string GetDomainUser()
        {
            var user = GetCurrentUser();
            return user?.DomainUser ?? Environment.UserName;
        }

        public string GetRole()
        {
            var user = GetCurrentUser();
            if (user != null && !string.IsNullOrWhiteSpace(user.Role))
            {
                if (CheckIsAdminRole(user.Role)) return SystemRoles.Admin;
                if (CheckIsMaintenanceRole(user.Role)) return SystemRoles.Maintenance;
                return user.Role.Trim().ToUpperInvariant();
            }

            var claim = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.Role);
            var claimVal = claim?.Value?.Trim().ToUpperInvariant();
            if (!string.IsNullOrWhiteSpace(claimVal))
            {
                if (CheckIsAdminRole(claimVal)) return SystemRoles.Admin;
                if (CheckIsMaintenanceRole(claimVal)) return SystemRoles.Maintenance;
                return claimVal;
            }

            return SystemRoles.EndUser;
        }

        private static bool CheckIsAdminRole(string? role)
        {
            if (string.IsNullOrWhiteSpace(role)) return false;
            var r = role.Trim().ToUpperInvariant();
            return r == SystemRoles.Admin ||
                   r == "ADMINISTRATOR" ||
                   r.Contains("ADMIN") ||
                   r.Contains("SYSADMIN") ||
                   r.Contains("MANAGER");
        }

        private static bool CheckIsMaintenanceRole(string? role)
        {
            if (string.IsNullOrWhiteSpace(role)) return false;
            var r = role.Trim().ToUpperInvariant();
            return r == SystemRoles.Maintenance ||
                   r.Contains("MAINTENANCE") ||
                   r.Contains("TECHNICIAN") ||
                   r.Contains("ENGINEER");
        }

        public string? GetMaintenanceTeam()
        {
            var user = GetCurrentUser();
            if (user != null)
            {
                return user.MaintenanceTeam?.Trim().ToUpperInvariant();
            }

            var claim = _httpContextAccessor.HttpContext?.User.FindFirst("MaintenanceTeam");
            return claim?.Value?.Trim().ToUpperInvariant();
        }

        public string GetDepartmentName()
        {
            var user = GetCurrentUser();
            return user?.Department?.Name ?? "General";
        }

        public bool IsAuthenticated()
        {
            return GetCurrentUser() != null || (_httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated ?? false);
        }

        public User? GetPhysicalUser()
        {
            var context = _httpContextAccessor.HttpContext;
            if (context == null) return null;

            if (context.Items.TryGetValue("PhysicalUser", out var u) && u is User user)
            {
                return user;
            }

            return null;
        }

        public bool CanSwitchProfile()
        {
            var context = _httpContextAccessor.HttpContext;
            if (context != null)
            {
                if (context.Items.TryGetValue("IsPhysicalAdmin", out var obj) && obj is bool isPhysicalAdmin)
                {
                    return isPhysicalAdmin;
                }

                var claim = context.User.FindFirst("IsPhysicalAdmin");
                if (claim != null && bool.TryParse(claim.Value, out bool val))
                {
                    return val;
                }
            }

            var physicalUser = GetPhysicalUser();
            if (physicalUser != null)
            {
                return CheckIsAdminRole(physicalUser.Role);
            }

            return IsAdmin();
        }

        public bool IsAdmin()
        {
            var user = GetCurrentUser();
            if (user != null && CheckIsAdminRole(user.Role)) return true;
            return CheckIsAdminRole(GetRole());
        }

        public bool IsMaintenance()
        {
            if (IsAdmin()) return false;
            var user = GetCurrentUser();
            if (user != null && CheckIsMaintenanceRole(user.Role)) return true;
            return CheckIsMaintenanceRole(GetRole());
        }

        public bool IsEndUser()
        {
            return !IsAdmin() && !IsMaintenance();
        }

        public bool CanAccessTicket(Ticket ticket)
        {
            if (ticket == null) return false;
            if (IsAdmin()) return true;

            int currentUserId = GetUserId();

            if (IsMaintenance())
            {
                var myTeam = GetMaintenanceTeam();
                // Maintenance users access only tickets routed to their designated maintenance team
                return string.Equals(ticket.TargetTeam, myTeam, StringComparison.OrdinalIgnoreCase);
            }

            // End Users can view only tickets they authored
            return ticket.OpenedBy == currentUserId || ticket.OnBehalfOf == currentUserId;
        }

        public bool CanManageTicket(Ticket ticket)
        {
            if (ticket == null) return false;
            if (IsAdmin()) return true;

            if (IsMaintenance())
            {
                var myTeam = GetMaintenanceTeam();
                return string.Equals(ticket.TargetTeam, myTeam, StringComparison.OrdinalIgnoreCase);
            }

            return false;
        }

        public bool CanAccessAsset(Asset asset)
        {
            if (asset == null) return false;
            if (IsAdmin()) return true;

            if (IsMaintenance())
            {
                var myTeam = GetMaintenanceTeam();
                return string.Equals(asset.MaintenanceTeam, myTeam, StringComparison.OrdinalIgnoreCase);
            }

            // End Users view only assets currently in their custody
            return asset.UserId == GetUserId();
        }

        public bool CanManageAssets()
        {
            return IsAdmin() || IsMaintenance();
        }

        public bool CanManageAsset(Asset asset)
        {
            if (asset == null) return false;
            if (IsAdmin()) return true;

            if (IsMaintenance())
            {
                var myTeam = GetMaintenanceTeam();
                return string.Equals(asset.MaintenanceTeam, myTeam, StringComparison.OrdinalIgnoreCase);
            }

            return false;
        }

        public bool CanCreateAssetInDepartment(string department)
        {
            if (IsAdmin()) return true;
            if (IsMaintenance())
            {
                var myTeam = GetMaintenanceTeam();
                return string.Equals(department, myTeam, StringComparison.OrdinalIgnoreCase);
            }
            return false;
        }

        public bool CanManageDeviceForms()
        {
            return IsAdmin() || IsMaintenance();
        }

        public bool CanManageDeviceForm(DeviceForm form)
        {
            if (form == null) return false;
            if (IsAdmin()) return true;
            if (IsMaintenance())
            {
                var myTeam = GetMaintenanceTeam();
                return string.Equals(form.Department, myTeam, StringComparison.OrdinalIgnoreCase);
            }
            return false;
        }

        public bool CanManageUsers()
        {
            return IsAdmin();
        }

        public bool CanAccessDashboard()
        {
            return IsAdmin() || IsMaintenance();
        }
    }
}
