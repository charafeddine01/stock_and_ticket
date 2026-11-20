using System;
using System.Collections.Generic;
using System.Linq;
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
using StockPortal.ViewModels;
using Xunit;

namespace StockPortal.Tests
{
    public class MaintenancePrivilegeToggleTests
    {
        private StockDbContext GetInMemoryDbContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<StockDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;

            var context = new StockDbContext(options);

            if (!context.Suppliers.Any())
            {
                context.Suppliers.Add(new Supplier { Id = 1, Name = "MedTech Solutions", IsActive = true });
            }

            if (!context.Rooms.Any())
            {
                context.Rooms.Add(new Room { Id = 1, RoomNo = 101, Floor = 1 });
            }

            if (!context.Users.Any())
            {
                context.Users.AddRange(
                    new User { Id = 1, DomainUser = "admin", FullName = "System Admin", Role = SystemRoles.Admin, IsActive = true },
                    new User { Id = 10, DomainUser = "it.tech", FullName = "IT Specialist", Role = SystemRoles.Maintenance, MaintenanceTeam = "IT", IsActive = true },
                    new User { Id = 20, DomainUser = "bio.tech", FullName = "Bio Specialist", Role = SystemRoles.Maintenance, MaintenanceTeam = "BIOMEDICAL", IsActive = true }
                );
            }

            context.SaveChanges();
            return context;
        }

        [Fact]
        public async Task AssetsController_PrintTag_WhenPrintToggleDisabled_ReturnsForbid()
        {
            var db = GetInMemoryDbContext(nameof(AssetsController_PrintTag_WhenPrintToggleDisabled_ReturnsForbid));
            var itAsset = new Asset { Id = 501, HospitalNo = "IT-501", MaintenanceTeam = "IT", Status = "In Service" };
            db.Assets.Add(itAsset);
            await db.SaveChangesAsync();

            var mockUser = new Mock<ICurrentUserService>();
            mockUser.Setup(s => s.IsAdmin()).Returns(false);
            mockUser.Setup(s => s.IsMaintenance()).Returns(true);
            mockUser.Setup(s => s.GetMaintenanceTeam()).Returns("IT");
            mockUser.Setup(s => s.CanAccessAsset(It.IsAny<Asset>())).Returns(true);
            mockUser.Setup(s => s.CanPrintAssetTags()).Returns(false);
            mockUser.Setup(s => s.CanPrintAssetTag(It.IsAny<Asset>())).Returns(false);

            var controller = new AssetsController(db, mockUser.Object);
            var result = await controller.PrintTag(501);

            Assert.IsType<ForbidResult>(result);
        }

        [Fact]
        public async Task AssetsController_PrintBulkTags_WhenPrintToggleDisabled_ReturnsForbid()
        {
            var db = GetInMemoryDbContext(nameof(AssetsController_PrintBulkTags_WhenPrintToggleDisabled_ReturnsForbid));
            var itAsset = new Asset { Id = 502, HospitalNo = "IT-502", MaintenanceTeam = "IT", Status = "In Service" };
            db.Assets.Add(itAsset);
            await db.SaveChangesAsync();

            var mockUser = new Mock<ICurrentUserService>();
            mockUser.Setup(s => s.IsAdmin()).Returns(false);
            mockUser.Setup(s => s.IsMaintenance()).Returns(true);
            mockUser.Setup(s => s.CanPrintAssetTags()).Returns(false);

            var controller = new AssetsController(db, mockUser.Object);
            var result = await controller.PrintBulkTags();

            Assert.IsType<ForbidResult>(result);
        }

        [Fact]
        public async Task AssetsController_PrintTag_WhenCrossTeam_ReturnsForbid()
        {
            var db = GetInMemoryDbContext(nameof(AssetsController_PrintTag_WhenCrossTeam_ReturnsForbid));
            // Asset belongs to BIOMEDICAL
            var bioAsset = new Asset { Id = 503, HospitalNo = "BIO-503", MaintenanceTeam = "BIOMEDICAL", Status = "In Service" };
            db.Assets.Add(bioAsset);
            await db.SaveChangesAsync();

            // Current user is IT
            var mockUser = new Mock<ICurrentUserService>();
            mockUser.Setup(s => s.IsAdmin()).Returns(false);
            mockUser.Setup(s => s.IsMaintenance()).Returns(true);
            mockUser.Setup(s => s.GetMaintenanceTeam()).Returns("IT");
            mockUser.Setup(s => s.CanPrintAssetTags()).Returns(true);
            mockUser.Setup(s => s.CanAccessAsset(bioAsset)).Returns(false);
            mockUser.Setup(s => s.CanPrintAssetTag(bioAsset)).Returns(false);

            var controller = new AssetsController(db, mockUser.Object);
            var result = await controller.PrintTag(503);

            Assert.IsType<ForbidResult>(result);
        }

        [Fact]
        public async Task AssetsController_Edit_WhenEditToggleDisabled_ReturnsForbid()
        {
            var db = GetInMemoryDbContext(nameof(AssetsController_Edit_WhenEditToggleDisabled_ReturnsForbid));
            var itAsset = new Asset { Id = 504, HospitalNo = "IT-504", MaintenanceTeam = "IT", Status = "In Service" };
            db.Assets.Add(itAsset);
            await db.SaveChangesAsync();

            var mockUser = new Mock<ICurrentUserService>();
            mockUser.Setup(s => s.IsAdmin()).Returns(false);
            mockUser.Setup(s => s.IsMaintenance()).Returns(true);
            mockUser.Setup(s => s.GetMaintenanceTeam()).Returns("IT");
            mockUser.Setup(s => s.CanAccessAsset(It.IsAny<Asset>())).Returns(true);
            mockUser.Setup(s => s.CanManageAsset(It.IsAny<Asset>())).Returns(true);
            mockUser.Setup(s => s.CanEditAssetTags()).Returns(false);
            mockUser.Setup(s => s.CanEditAsset(It.IsAny<Asset>())).Returns(false);

            var controller = new AssetsController(db, mockUser.Object);
            var getResult = await controller.Edit(504);
            Assert.IsType<ForbidResult>(getResult);

            var postResult = await controller.Edit(504, itAsset);
            Assert.IsType<ForbidResult>(postResult);
        }

        [Fact]
        public async Task HomeController_Index_WhenTeamAnalyticsDisabled_OmitsTeamStatsAndLocksPersonal()
        {
            var db = GetInMemoryDbContext(nameof(HomeController_Index_WhenTeamAnalyticsDisabled_OmitsTeamStatsAndLocksPersonal));
            
            var ticket = new Ticket
            {
                Id = 601,
                TargetTeam = "IT",
                Type = "Hardware",
                StatusId = 5,
                OpenedDT = DateTime.Today.AddHours(1),
                ClosedDT = DateTime.Today.AddHours(3),
                ClosedByUsers = "[10]"
            };
            db.Tickets.Add(ticket);
            await db.SaveChangesAsync();

            var mockUser = new Mock<ICurrentUserService>();
            mockUser.Setup(s => s.IsAdmin()).Returns(false);
            mockUser.Setup(s => s.IsMaintenance()).Returns(true);
            mockUser.Setup(s => s.GetMaintenanceTeam()).Returns("IT");
            mockUser.Setup(s => s.GetUserId()).Returns(10);
            mockUser.Setup(s => s.CanAccessTeamAnalytics()).Returns(false); // Team analytics DISABLED
            mockUser.Setup(s => s.CanViewOwnAnalytics()).Returns(true);   // Own analytics ENABLED

            var controller = new HomeController(db, mockUser.Object);
            var result = await controller.Index(datePreset: "today", tab: "analytics");

            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<DashboardViewModel>(viewResult.Model);

            // ActiveTab remains analytics because personal analytics is enabled
            Assert.Equal("analytics", model.ActiveTab);
            Assert.False(model.CanAccessTeamAnalytics);
            Assert.True(model.CanViewOwnAnalytics);
            // TeamStats must be empty because team-wide access is disabled
            Assert.Empty(model.TeamStats);
            // SelectedTechUserId is locked to current user (10)
            Assert.Equal(10, model.SelectedTechUserId);
            // TechnicianStats only has current user
            Assert.All(model.TechnicianStats, t => Assert.Equal(10, t.UserId));
        }

        [Fact]
        public async Task HomeController_Index_WhenBothAnalyticsDisabled_ForcesOverviewTab()
        {
            var db = GetInMemoryDbContext(nameof(HomeController_Index_WhenBothAnalyticsDisabled_ForcesOverviewTab));

            var mockUser = new Mock<ICurrentUserService>();
            mockUser.Setup(s => s.IsAdmin()).Returns(false);
            mockUser.Setup(s => s.IsMaintenance()).Returns(true);
            mockUser.Setup(s => s.GetMaintenanceTeam()).Returns("IT");
            mockUser.Setup(s => s.GetUserId()).Returns(10);
            mockUser.Setup(s => s.CanAccessTeamAnalytics()).Returns(false); // DISABLED
            mockUser.Setup(s => s.CanViewOwnAnalytics()).Returns(false);   // DISABLED

            var controller = new HomeController(db, mockUser.Object);
            var result = await controller.Index(datePreset: "today", tab: "analytics");

            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<DashboardViewModel>(viewResult.Model);

            // ActiveTab is forced to overview because both analytics toggles are off
            Assert.Equal("overview", model.ActiveTab);
            Assert.False(model.CanAccessTeamAnalytics);
            Assert.False(model.CanViewOwnAnalytics);
        }

        [Fact]
        public async Task HomeController_PrintMemberReport_PersonalToggleEnforcement()
        {
            var db = GetInMemoryDbContext(nameof(HomeController_PrintMemberReport_PersonalToggleEnforcement));

            var mockUser = new Mock<ICurrentUserService>();
            mockUser.Setup(s => s.CanAccessDashboard()).Returns(true);
            mockUser.Setup(s => s.IsAdmin()).Returns(false);
            mockUser.Setup(s => s.IsMaintenance()).Returns(true);
            mockUser.Setup(s => s.GetMaintenanceTeam()).Returns("IT");
            mockUser.Setup(s => s.GetUserId()).Returns(10);
            mockUser.Setup(s => s.CanViewOwnAnalytics()).Returns(false); // Own analytics disabled

            var controller = new HomeController(db, mockUser.Object);

            // Attempting to print own report when CanViewOwnAnalytics is false returns Forbid
            var ownResult = await controller.PrintMemberReport(10, datePreset: "today");
            Assert.IsType<ForbidResult>(ownResult);

            // Now enable own analytics, but leave team analytics false: can print own report
            mockUser.Setup(s => s.CanViewOwnAnalytics()).Returns(true);
            mockUser.Setup(s => s.CanAccessTeamAnalytics()).Returns(false);

            var ownAllowedResult = await controller.PrintMemberReport(10, datePreset: "today");
            Assert.IsType<ViewResult>(ownAllowedResult);

            // Attempting to print ANOTHER member's report while CanAccessTeamAnalytics is false returns Forbid
            var otherMemberResult = await controller.PrintMemberReport(20, datePreset: "today");
            Assert.IsType<ForbidResult>(otherMemberResult);
        }

        [Fact]
        public async Task DirectoryController_UpdateUserRole_PersistsAllFourToggles()
        {
            var db = GetInMemoryDbContext(nameof(DirectoryController_UpdateUserRole_PersistsAllFourToggles));

            var mockUser = new Mock<ICurrentUserService>();
            mockUser.Setup(s => s.GetUserId()).Returns(1);
            mockUser.Setup(s => s.GetDomainUser()).Returns("admin");
            mockUser.Setup(s => s.IsAdmin()).Returns(true);
            mockUser.Setup(s => s.CanManageUsers()).Returns(true);

            var controller = new DirectoryController(db, mockUser.Object);
            var httpContext = new DefaultHttpContext();
            controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
            controller.TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());

            // Update user 10 to MAINTENANCE with custom toggles
            var result = await controller.UpdateUserRole(
                id: 10,
                role: "MAINTENANCE",
                maintenanceTeam: "IT",
                canPrintAssetTags: true,
                canEditAssetTags: false,
                canAccessTeamAnalytics: false,
                canViewOwnAnalytics: true
            );

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal(nameof(DirectoryController.Index), redirect.ActionName);

            var updatedUser = await db.Users.FindAsync(10);
            Assert.NotNull(updatedUser);
            Assert.Equal("MAINTENANCE", updatedUser.Role);
            Assert.Equal("IT", updatedUser.MaintenanceTeam);
            Assert.True(updatedUser.CanPrintAssetTags);
            Assert.False(updatedUser.CanEditAssetTags);
            Assert.False(updatedUser.CanAccessTeamAnalytics);
            Assert.True(updatedUser.CanViewOwnAnalytics);
        }

        [Fact]
        public void CurrentUserService_ImplementsPrivilegeChecksCorrectly()
        {
            var mockAccessor = new Mock<IHttpContextAccessor>();
            var httpContext = new DefaultHttpContext();

            var itAsset = new Asset { Id = 1, MaintenanceTeam = "IT" };
            var bioAsset = new Asset { Id = 2, MaintenanceTeam = "BIOMEDICAL" };

            // Case A: Admin user
            var adminUser = new User
            {
                Id = 1,
                Role = SystemRoles.Admin,
                IsActive = true
            };
            httpContext.Items["CurrentUser"] = adminUser;
            mockAccessor.Setup(a => a.HttpContext).Returns(httpContext);

            var adminService = new CurrentUserService(mockAccessor.Object);
            Assert.True(adminService.IsAdmin());
            Assert.True(adminService.CanPrintAssetTags());
            Assert.True(adminService.CanPrintAssetTag(itAsset));
            Assert.True(adminService.CanPrintAssetTag(bioAsset)); // Admin has cross-team access
            Assert.True(adminService.CanEditAssetTags());
            Assert.True(adminService.CanEditAsset(itAsset));
            Assert.True(adminService.CanEditAsset(bioAsset));
            Assert.True(adminService.CanAccessTeamAnalytics());
            Assert.True(adminService.CanViewOwnAnalytics());

            // Case B: Maintenance user with restricted toggles
            var maintUser = new User
            {
                Id = 10,
                Role = SystemRoles.Maintenance,
                MaintenanceTeam = "IT",
                CanPrintAssetTags = false,
                CanEditAssetTags = true,
                CanAccessTeamAnalytics = false,
                CanViewOwnAnalytics = true,
                IsActive = true
            };
            httpContext.Items["CurrentUser"] = maintUser;

            var maintService = new CurrentUserService(mockAccessor.Object);
            Assert.False(maintService.IsAdmin());
            Assert.True(maintService.IsMaintenance());
            Assert.True(maintService.CanManageAssets());
            Assert.True(maintService.CanCreateAssetInDepartment("IT"));
            Assert.False(maintService.CanCreateAssetInDepartment("BIOMEDICAL"));
            Assert.True(maintService.CanManageDeviceForms());
            Assert.False(maintService.CanPrintAssetTags());
            Assert.False(maintService.CanPrintAssetTag(itAsset)); // Print tags toggle is false
            Assert.True(maintService.CanEditAssetTags());
            Assert.True(maintService.CanEditAsset(itAsset)); // Edit is true and matches IT team
            Assert.False(maintService.CanEditAsset(bioAsset)); // Cross-team blocked even if edit toggle is true!
            Assert.False(maintService.CanAccessTeamAnalytics());
            Assert.True(maintService.CanViewOwnAnalytics());

            // Case C: Maintenance user with CanEditAssetTags = false
            var restrictedMaintUser = new User
            {
                Id = 11,
                Role = SystemRoles.Maintenance,
                MaintenanceTeam = "IT",
                CanPrintAssetTags = true,
                CanEditAssetTags = false, // Disabled
                CanAccessTeamAnalytics = true,
                CanViewOwnAnalytics = true,
                IsActive = true
            };
            httpContext.Items["CurrentUser"] = restrictedMaintUser;

            var restrictedMaintService = new CurrentUserService(mockAccessor.Object);
            Assert.False(restrictedMaintService.CanEditAssetTags());
            Assert.False(restrictedMaintService.CanManageAssets()); // Cannot manage/create assets
            Assert.False(restrictedMaintService.CanCreateAssetInDepartment("IT"));
            Assert.False(restrictedMaintService.CanManageDeviceForms());
            Assert.False(restrictedMaintService.CanEditAsset(itAsset)); // Cannot edit asset tags
            Assert.True(restrictedMaintService.CanManageAsset(itAsset)); // Can still perform maintenance logs for their team
            Assert.True(restrictedMaintService.CanPrintAssetTags());
            Assert.True(restrictedMaintService.CanPrintAssetTag(itAsset));
        }

        [Fact]
        public async Task AssetsController_Create_WhenEditAssetTagsDisabled_ReturnsForbid()
        {
            var db = GetInMemoryDbContext(nameof(AssetsController_Create_WhenEditAssetTagsDisabled_ReturnsForbid));

            var mockUser = new Mock<ICurrentUserService>();
            mockUser.Setup(s => s.IsAdmin()).Returns(false);
            mockUser.Setup(s => s.IsMaintenance()).Returns(true);
            mockUser.Setup(s => s.GetMaintenanceTeam()).Returns("IT");
            mockUser.Setup(s => s.CanManageAssets()).Returns(false);
            mockUser.Setup(s => s.CanEditAssetTags()).Returns(false);

            var controller = new AssetsController(db, mockUser.Object);

            var getResult = await controller.Create();
            Assert.IsType<ForbidResult>(getResult);

            var newAsset = new Asset { HospitalNo = "IT-NEW", MaintenanceTeam = "IT" };
            var postResult = await controller.Create(newAsset, deviceFormId: 1);
            Assert.IsType<ForbidResult>(postResult);
        }

        [Fact]
        public async Task AssetsController_TransferCustodyAndRetire_WhenEditAssetDisabled_ReturnsForbid()
        {
            var db = GetInMemoryDbContext(nameof(AssetsController_TransferCustodyAndRetire_WhenEditAssetDisabled_ReturnsForbid));
            var asset = new Asset { Id = 701, HospitalNo = "IT-701", MaintenanceTeam = "IT", Status = "In Service" };
            db.Assets.Add(asset);
            await db.SaveChangesAsync();

            var mockUser = new Mock<ICurrentUserService>();
            mockUser.Setup(s => s.IsAdmin()).Returns(false);
            mockUser.Setup(s => s.IsMaintenance()).Returns(true);
            mockUser.Setup(s => s.GetMaintenanceTeam()).Returns("IT");
            mockUser.Setup(s => s.CanEditAsset(It.IsAny<Asset>())).Returns(false);

            var controller = new AssetsController(db, mockUser.Object);

            var transferResult = await controller.TransferCustody(701, newUserId: 1, newRoomId: 1, newCustodyTypeId: 1, notes: "Transfer test");
            Assert.IsType<ForbidResult>(transferResult);

            var retireResult = await controller.Retire(701, reason: "Retire test");
            Assert.IsType<ForbidResult>(retireResult);
        }
    }
}
