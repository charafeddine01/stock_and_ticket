using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using StockPortal.Data;
using StockPortal.Models;
using StockPortal.Services;

namespace StockPortal.Controllers
{
    public class DirectoryController : Controller
    {
        private readonly StockDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public DirectoryController(StockDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        // GET: Directory
        public async Task<IActionResult> Index(string? departmentFilter = null, string? tab = null)
        {
            if (!_currentUserService.CanManageUsers() && !_currentUserService.IsMaintenance()) return Forbid();

            ViewBag.Users = await _context.Users.Include(u => u.Department).OrderBy(u => u.FullName).ToListAsync();
            ViewBag.Rooms = await _context.Rooms.OrderBy(r => r.Floor).ThenBy(r => r.RoomNo).ToListAsync();

            var suppliersQuery = await _context.Suppliers.OrderBy(s => s.Name).ToListAsync();
            if (!string.IsNullOrWhiteSpace(departmentFilter) && departmentFilter != "All")
            {
                suppliersQuery = suppliersQuery.Where(s => s.SupportsDepartment(departmentFilter)).ToList();
            }
            ViewBag.Suppliers = suppliersQuery;
            ViewBag.DepartmentFilter = departmentFilter ?? "All";
            ViewBag.ActiveTab = string.IsNullOrWhiteSpace(tab) ? "users" : tab.Trim().ToLowerInvariant();
            ViewBag.Departments = await _context.Departments.Include(d => d.HeadUser).OrderBy(d => d.Name).ToListAsync();
            ViewBag.AvailableRoles = SystemRoles.All;
            ViewBag.AvailableTeams = MaintenanceTeams.All;
            ViewBag.CanManageUsers = _currentUserService.CanManageUsers();
            ViewBag.UserTeam = _currentUserService.GetMaintenanceTeam();

            return View();
        }

        // POST: Directory/CreateUser
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateUser(User user)
        {
            if (!_currentUserService.CanManageUsers()) return Forbid();

            if (!string.IsNullOrWhiteSpace(user.FullName))
            {
                user.IsActive = true;
                if (!string.IsNullOrWhiteSpace(user.DomainUser))
                {
                    user.DomainUser = user.DomainUser.Trim();
                }
                user.Role = string.IsNullOrWhiteSpace(user.Role) ? SystemRoles.EndUser : user.Role.Trim().ToUpperInvariant();
                if (user.Role != SystemRoles.Maintenance)
                {
                    user.MaintenanceTeam = null;
                }
                _context.Users.Add(user);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"User {user.FullName} (PC Username: {user.DomainUser}, Role: {user.Role}) registered successfully with Auto-Login enabled.";
            }
            return RedirectToAction(nameof(Index), new { tab = "users" });
        }

        // POST: Directory/UpdateUserRole
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateUserRole(int id, string role, string? maintenanceTeam)
        {
            if (!_currentUserService.CanManageUsers()) return Forbid();

            var user = await _context.Users.FindAsync(id);
            if (user != null)
            {
                user.Role = string.IsNullOrWhiteSpace(role) ? SystemRoles.EndUser : role.Trim().ToUpperInvariant();
                user.MaintenanceTeam = (user.Role == SystemRoles.Maintenance) ? maintenanceTeam : null;
                _context.Update(user);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"User {user.FullName} role updated to {user.Role} {(user.MaintenanceTeam != null ? $"({user.MaintenanceTeam})" : "")}.";
            }
            return RedirectToAction(nameof(Index), new { tab = "users" });
        }

        // POST: Directory/ToggleUserStatus
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleUserStatus(int id)
        {
            if (!_currentUserService.CanManageUsers()) return Forbid();

            var user = await _context.Users.FindAsync(id);
            if (user != null)
            {
                user.IsActive = !(user.IsActive ?? true);
                _context.Update(user);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"User {user.FullName} status set to {(user.IsActive == true ? "Active" : "Inactive")}.";
            }
            return RedirectToAction(nameof(Index), new { tab = "users" });
        }

        // POST: Directory/CreateRoom
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateRoom(Room room)
        {
            if (!_currentUserService.CanManageUsers()) return Forbid();

            if (room.RoomNo.HasValue && room.Floor.HasValue)
            {
                _context.Rooms.Add(room);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Room {room.RoomNo} (Floor {room.Floor}) created.";
            }
            return RedirectToAction(nameof(Index), new { tab = "rooms" });
        }

        // POST: Directory/DeleteRoom
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteRoom(int id)
        {
            if (!_currentUserService.CanManageUsers()) return Forbid();

            var room = await _context.Rooms.Include(r => r.Assets).FirstOrDefaultAsync(r => r.Id == id);
            if (room != null)
            {
                if (room.Assets.Any())
                {
                    TempData["ErrorMessage"] = $"Cannot delete Room {room.RoomNo} because it has {room.Assets.Count} assets assigned to it.";
                }
                else
                {
                    _context.Rooms.Remove(room);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = $"Room {room.RoomNo} deleted.";
                }
            }
            return RedirectToAction(nameof(Index), new { tab = "rooms" });
        }

        // POST: Directory/CreateSupplier
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateSupplier(Supplier supplier, string[]? selectedDepartments = null)
        {
            if (!_currentUserService.CanManageUsers()) return Forbid();

            if (!string.IsNullOrWhiteSpace(supplier.Name))
            {
                supplier.IsActive = true;
                supplier.DepartmentList = (selectedDepartments != null && selectedDepartments.Length > 0)
                    ? selectedDepartments.Distinct().ToList()
                    : new List<string>();

                _context.Suppliers.Add(supplier);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Supplier {supplier.Name} created successfully with departments: {(supplier.DepartmentList.Any() ? string.Join(", ", supplier.DepartmentList) : "Common to All")}.";
            }
            return RedirectToAction(nameof(Index), new { tab = "suppliers" });
        }

        // POST: Directory/EditSupplier
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditSupplier(int id, string name, int? type, string[]? selectedDepartments, bool? isActive)
        {
            if (!_currentUserService.CanManageUsers()) return Forbid();

            var supplier = await _context.Suppliers.FindAsync(id);
            if (supplier != null)
            {
                if (!string.IsNullOrWhiteSpace(name))
                {
                    supplier.Name = name.Trim();
                }
                if (type.HasValue)
                {
                    supplier.Type = type.Value;
                }
                if (isActive.HasValue)
                {
                    supplier.IsActive = isActive.Value;
                }
                supplier.DepartmentList = (selectedDepartments != null && selectedDepartments.Length > 0)
                    ? selectedDepartments.Distinct().ToList()
                    : new List<string>();

                _context.Update(supplier);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Supplier {supplier.Name} updated successfully with departments: {(supplier.DepartmentList.Any() ? string.Join(", ", supplier.DepartmentList) : "Common to All")}.";
            }
            return RedirectToAction(nameof(Index));
        }

        // POST: Directory/ToggleSupplierStatus
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleSupplierStatus(int id)
        {
            if (!_currentUserService.CanManageUsers()) return Forbid();

            var supplier = await _context.Suppliers.FindAsync(id);
            if (supplier != null)
            {
                supplier.IsActive = !(supplier.IsActive ?? true);
                _context.Update(supplier);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Supplier {supplier.Name} set to {(supplier.IsActive == true ? "Active" : "Inactive")}.";
            }
            return RedirectToAction(nameof(Index), new { tab = "suppliers" });
        }

        // GET: Directory/GetSuppliersByDepartment
        [HttpGet]
        public async Task<IActionResult> GetSuppliersByDepartment(string? department)
        {
            var list = await _context.Suppliers.Where(s => s.IsActive == true).OrderBy(s => s.Name).ToListAsync();
            if (!string.IsNullOrWhiteSpace(department))
            {
                list = list.Where(s => s.SupportsDepartment(department)).ToList();
            }
            return Json(list.Select(s => new {
                id = s.Id,
                name = s.Name,
                type = s.Type,
                typeName = s.TypeName,
                departments = s.DepartmentList
            }));
        }

        // POST: Directory/CreateDepartment
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateDepartment(Department department)
        {
            if (!_currentUserService.CanManageUsers()) return Forbid();

            if (!string.IsNullOrWhiteSpace(department.Name))
            {
                _context.Departments.Add(department);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Department {department.Name} created.";
            }
            return RedirectToAction(nameof(Index), new { tab = "depts" });
        }

        // POST: Directory/DeleteDepartment
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteDepartment(int id)
        {
            if (!_currentUserService.CanManageUsers()) return Forbid();

            var dept = await _context.Departments.Include(d => d.Users).FirstOrDefaultAsync(d => d.Id == id);
            if (dept != null)
            {
                if (dept.Users.Any())
                {
                    TempData["ErrorMessage"] = $"Cannot delete Department {dept.Name} because it has {dept.Users.Count} active users.";
                }
                else
                {
                    _context.Departments.Remove(dept);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = $"Department {dept.Name} removed.";
                }
            }
            return RedirectToAction(nameof(Index), new { tab = "depts" });
        }
    }
}
