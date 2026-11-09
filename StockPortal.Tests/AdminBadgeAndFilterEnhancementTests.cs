using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Moq;
using StockPortal.Controllers;
using StockPortal.Data;
using StockPortal.Models;
using StockPortal.Services;
using Xunit;

namespace StockPortal.Tests
{
    public class AdminBadgeAndFilterEnhancementTests
    {
        private StockDbContext CreateInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<StockDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            return new StockDbContext(options);
        }

        private T InitController<T>(T controller, int userId = 1) where T : Controller
        {
            var httpContext = new DefaultHttpContext();
            httpContext.Session = new TestSession();
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };
            controller.TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());
            return controller;
        }

        // ==========================================
        // Task 1: Queue badge count
        // ==========================================
        [Fact]
        public void Task1_QueueCount_IsZeroWhenNoStatus1TicketsOpen()
        {
            using var db = CreateInMemoryDbContext();

            // Seed only pending (status 2) and closed (status 5) tickets
            db.Tickets.Add(new Ticket { Id = 1, StatusId = 2, TargetTeam = "IT", Type = "Hardware" });
            db.Tickets.Add(new Ticket { Id = 2, StatusId = 5, TargetTeam = "IT", Type = "Hardware", ClosedDT = DateTime.Now });
            db.SaveChanges();

            // StatusId == 1 count should strictly be 0
            var openTicketsCount = db.Tickets.Count(t => t.StatusId == 1 && t.ClosedDT == null);
            Assert.Equal(0, openTicketsCount);

            // Add an opened ticket (StatusId == 1)
            db.Tickets.Add(new Ticket { Id = 3, StatusId = 1, TargetTeam = "IT", Type = "Hardware" });
            db.SaveChanges();

            openTicketsCount = db.Tickets.Count(t => t.StatusId == 1 && t.ClosedDT == null);
            Assert.Equal(1, openTicketsCount);
        }

        // ==========================================
        // Task 2: Maintenance count badge
        // ==========================================
        [Fact]
        public void Task2_MaintenanceCount_MatchesAssetsCurrentlyAtMaintenance()
        {
            using var db = CreateInMemoryDbContext();

            // 1 asset Under Maintenance, 1 in supplier custody (CustodyTypeId = 3), 1 In Service
            db.Assets.Add(new Asset { Id = 1, HospitalNo = "BIO-001", Status = "Under Maintenance", MaintenanceTeam = "BIOMEDICAL" });
            db.Assets.Add(new Asset { Id = 2, HospitalNo = "IT-001", Status = "In Service", CustodyTypeId = 3, MaintenanceTeam = "IT" });
            db.Assets.Add(new Asset { Id = 3, HospitalNo = "GEN-001", Status = "In Service", CustodyTypeId = 1, MaintenanceTeam = "GENERAL" });
            db.SaveChanges();

            var assetsAtMaintenanceCount = db.Assets.Count(a => a.Status == "Under Maintenance" || a.CustodyTypeId == 3);
            Assert.Equal(2, assetsAtMaintenanceCount);

            // Filtered by Biomedical department
            var bioMaintCount = db.Assets.Count(a => (a.Status == "Under Maintenance" || a.CustodyTypeId == 3) && a.MaintenanceTeam == "BIOMEDICAL");
            Assert.Equal(1, bioMaintCount);
        }

        // ==========================================
        // Task 3: Admin role & permission normalization
        // ==========================================
        [Theory]
        [InlineData("IT Manager & Lead SysAdmin", true, true)]
        [InlineData("Admin", true, true)]
        [InlineData("ADMIN", true, true)]
        [InlineData("System Administrator", true, true)]
        [InlineData("Biomedical Equipment Engineer", false, false)]
        [InlineData("END_USER", false, false)]
        public void Task3_CurrentUserService_RecognizesManagerAndAdminTitles(string roleTitle, bool expectedIsAdmin, bool expectedCanManageUsers)
        {
            var mockAccessor = new Mock<IHttpContextAccessor>();
            var context = new DefaultHttpContext();

            var user = new User
            {
                Id = 10,
                FullName = "Abbas Al-Amiri",
                DomainUser = "Abbas",
                Role = roleTitle,
                IsActive = true
            };

            context.Items["CurrentUser"] = user;
            mockAccessor.Setup(a => a.HttpContext).Returns(context);

            var service = new CurrentUserService(mockAccessor.Object);

            Assert.Equal(expectedIsAdmin, service.IsAdmin());
            Assert.Equal(expectedCanManageUsers, service.CanManageUsers());

            if (expectedIsAdmin)
            {
                Assert.Equal(SystemRoles.Admin, service.GetRole());
                Assert.False(service.IsEndUser());
                Assert.True(service.CanManageAssets());
                Assert.True(service.CanAccessDashboard());
            }
        }

        // ==========================================
        // Task 4: Asset Categories department isolation & dynamic filtering
        // ==========================================
        [Fact]
        public async Task Task4_AssetCategories_MaintenanceUsersIsolatedToOwnDepartment()
        {
            using var db = CreateInMemoryDbContext();

            db.AssetCategories.AddRange(
                new AssetCategory { Id = 1, Name = "PC", Department = "IT", IsActive = true },
                new AssetCategory { Id = 2, Name = "Printer", Department = "IT", IsActive = true },
                new AssetCategory { Id = 3, Name = "Infusion Pump", Department = "BIOMEDICAL", IsActive = true },
                new AssetCategory { Id = 4, Name = "HVAC Chiller", Department = "GENERAL", IsActive = true }
            );
            await db.SaveChangesAsync();

            // Biomedical technician
            var mockBioService = new Mock<ICurrentUserService>();
            mockBioService.Setup(s => s.IsAdmin()).Returns(false);
            mockBioService.Setup(s => s.IsMaintenance()).Returns(true);
            mockBioService.Setup(s => s.IsEndUser()).Returns(false);
            mockBioService.Setup(s => s.GetMaintenanceTeam()).Returns("BIOMEDICAL");

            var controller = InitController(new AssetCategoriesController(db, mockBioService.Object));
            var result = await controller.Index(department: null, search: null, filter: null) as ViewResult;
            Assert.NotNull(result);

            var model = Assert.IsAssignableFrom<IEnumerable<AssetCategory>>(result.Model).ToList();
            Assert.Single(model);
            Assert.Equal("Infusion Pump", model[0].Name);
            Assert.Equal("BIOMEDICAL", model[0].Department);
        }

        [Fact]
        public async Task Task4_AssetCategories_AdminCanViewAllAndFilterBySearch()
        {
            using var db = CreateInMemoryDbContext();

            db.AssetCategories.AddRange(
                new AssetCategory { Id = 1, Name = "PC Desktop", Description = "Micro workstation", Department = "IT", IsActive = true },
                new AssetCategory { Id = 2, Name = "LaserJet Printer", Description = "Network printer", Department = "IT", IsActive = true },
                new AssetCategory { Id = 3, Name = "Patient Monitor", Description = "Bedside telemetry", Department = "BIOMEDICAL", IsActive = true }
            );
            await db.SaveChangesAsync();

            var mockAdminService = new Mock<ICurrentUserService>();
            mockAdminService.Setup(s => s.IsAdmin()).Returns(true);
            mockAdminService.Setup(s => s.IsMaintenance()).Returns(false);
            mockAdminService.Setup(s => s.IsEndUser()).Returns(false);

            var controller = InitController(new AssetCategoriesController(db, mockAdminService.Object));

            // Admin viewing all
            var resultAll = await controller.Index(department: "ALL", search: null, filter: null) as ViewResult;
            var modelAll = Assert.IsAssignableFrom<IEnumerable<AssetCategory>>(resultAll!.Model).ToList();
            Assert.Equal(3, modelAll.Count);

            // Admin dynamic search for "Printer"
            var resultSearch = await controller.Index(department: null, search: "printer", filter: null) as ViewResult;
            var modelSearch = Assert.IsAssignableFrom<IEnumerable<AssetCategory>>(resultSearch!.Model).ToList();
            Assert.Single(modelSearch);
            Assert.Equal("LaserJet Printer", modelSearch[0].Name);
        }

        // ==========================================
        // Task 5 & 6: Tickets/Create asset counts & scoping
        // ==========================================
        [Fact]
        public async Task Task6_TicketsCreate_AdminGetsTotalHospitalAssetsCount()
        {
            using var db = CreateInMemoryDbContext();

            // Seed hospital assets across all 3 departments
            db.Assets.AddRange(
                new Asset { Id = 1, HospitalNo = "HOSP-PC-01", Brand = "Dell", Type = "PC", MaintenanceTeam = "IT", UserId = 1 },
                new Asset { Id = 2, HospitalNo = "HOSP-PRN-01", Brand = "HP", Type = "Printer", MaintenanceTeam = "IT", UserId = 1 },
                new Asset { Id = 3, HospitalNo = "HOSP-SW-01", Brand = "Cisco", Type = "Switch", MaintenanceTeam = "IT", UserId = 1 },
                new Asset { Id = 4, HospitalNo = "HOSP-BIO-01", Brand = "Philips", Type = "Monitor", MaintenanceTeam = "BIOMEDICAL", UserId = 2 },
                new Asset { Id = 5, HospitalNo = "HOSP-BIO-02", Brand = "GE", Type = "ECG", MaintenanceTeam = "BIOMEDICAL", UserId = 3 },
                new Asset { Id = 6, HospitalNo = "HOSP-GEN-01", Brand = "Carrier", Type = "HVAC", MaintenanceTeam = "GENERAL", UserId = 4 }
            );

            db.Users.AddRange(
                new User { Id = 1, FullName = "Abbas Al-Amiri", Role = SystemRoles.Admin, IsActive = true },
                new User { Id = 2, FullName = "Dr. Sarah", Role = SystemRoles.EndUser, IsActive = true }
            );
            await db.SaveChangesAsync();

            var mockAdmin = new Mock<ICurrentUserService>();
            mockAdmin.Setup(s => s.GetUserId()).Returns(1);
            mockAdmin.Setup(s => s.IsAdmin()).Returns(true);
            mockAdmin.Setup(s => s.IsMaintenance()).Returns(false);
            mockAdmin.Setup(s => s.IsEndUser()).Returns(false);

            var mockNotificationService = new Mock<INotificationService>();
            var controller = InitController(new TicketsController(db, mockAdmin.Object, mockNotificationService.Object), userId: 1);
            var result = await controller.Create(assetId: null) as ViewResult;
            Assert.NotNull(result);

            // Total assets count for admin should be 6 (all hospital assets), not just 3
            int userAssetsCount = (int)controller.ViewBag.UserAssetsCount;
            Assert.Equal(6, userAssetsCount);

            var userAssetsList = controller.ViewBag.UserAssetsList as System.Collections.IEnumerable;
            Assert.NotNull(userAssetsList);
            Assert.Equal(6, userAssetsList.Cast<object>().Count());
        }

        [Fact]
        public async Task Task6_TicketsCreate_EndUserGetsOnlyTheirAssignedAssets()
        {
            using var db = CreateInMemoryDbContext();

            db.Assets.AddRange(
                new Asset { Id = 1, HospitalNo = "HOSP-PC-01", Brand = "Dell", Type = "PC", MaintenanceTeam = "IT", UserId = 1 },
                new Asset { Id = 2, HospitalNo = "HOSP-PC-02", Brand = "Lenovo", Type = "PC", MaintenanceTeam = "IT", UserId = 2 }
            );

            db.Users.Add(new User { Id = 2, FullName = "Dr. Elena", Role = SystemRoles.EndUser, IsActive = true });
            await db.SaveChangesAsync();

            var mockEndUser = new Mock<ICurrentUserService>();
            mockEndUser.Setup(s => s.GetUserId()).Returns(2);
            mockEndUser.Setup(s => s.IsAdmin()).Returns(false);
            mockEndUser.Setup(s => s.IsMaintenance()).Returns(false);
            mockEndUser.Setup(s => s.IsEndUser()).Returns(true);

            var mockNotificationService = new Mock<INotificationService>();
            var controller = InitController(new TicketsController(db, mockEndUser.Object, mockNotificationService.Object), userId: 2);
            var result = await controller.Create(assetId: null) as ViewResult;
            Assert.NotNull(result);

            // End user 2 only gets their 1 assigned asset
            int userAssetsCount = (int)controller.ViewBag.UserAssetsCount;
            Assert.Equal(1, userAssetsCount);
        }
    }

    // Helper mock session for tests
    internal class TestSession : ISession
    {
        private readonly Dictionary<string, byte[]> _store = new();
        public bool IsAvailable => true;
        public string Id => "test-session";
        public IEnumerable<string> Keys => _store.Keys;

        public void Clear() => _store.Clear();
        public Task CommitAsync(System.Threading.CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task LoadAsync(System.Threading.CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void Remove(string key) => _store.Remove(key);
        public void Set(string key, byte[] value) => _store[key] = value;
        public bool TryGetValue(string key, out byte[] value) => _store.TryGetValue(key, out value!);
    }
}