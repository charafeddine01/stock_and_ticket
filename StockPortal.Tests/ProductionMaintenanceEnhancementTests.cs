using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using StockPortal.Controllers;
using StockPortal.Data;
using StockPortal.Models;
using StockPortal.Services;
using StockPortal.ViewModels;
using Xunit;

namespace StockPortal.Tests
{
    public class ProductionMaintenanceEnhancementTests
    {
        private StockDbContext GetInMemoryDbContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<StockDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;
            return new StockDbContext(options);
        }

        // ==========================================
        // TASK 1: IP & MAC in Profile & Tickets
        // ==========================================

        [Fact]
        public void NetworkInfoHelper_GetClientIpAddress_HonorsForwardedFor()
        {
            var context = new DefaultHttpContext();
            context.Request.Headers["X-Forwarded-For"] = "192.168.10.45, 10.0.0.1";

            var ip = NetworkInfoHelper.GetClientIpAddress(context);
            Assert.Equal("192.168.10.45", ip);
        }

        [Fact]
        public void NetworkInfoHelper_GetLocalPrimaryMacAddress_ReturnsNonEmpty()
        {
            var mac = NetworkInfoHelper.GetLocalPrimaryMacAddress();
            Assert.NotNull(mac);
            Assert.NotEmpty(mac);
        }

        [Fact]
        public async Task AccountController_Profile_SetsClientIpAndMacInViewBag()
        {
            var db = GetInMemoryDbContext(Guid.NewGuid().ToString());
            var user = new User { Id = 1, FullName = "Admin User", Role = SystemRoles.Admin, IsActive = true };
            db.Users.Add(user);
            await db.SaveChangesAsync();

            var currentUserService = new Mock<ICurrentUserService>();
            currentUserService.Setup(s => s.GetCurrentUser()).Returns(user);
            currentUserService.Setup(s => s.GetUserId()).Returns(user.Id);
            currentUserService.Setup(s => s.IsAdmin()).Returns(true);
            currentUserService.Setup(s => s.CanSwitchProfile()).Returns(true);

            var controller = new AccountController(db, currentUserService.Object);
            var httpContext = new DefaultHttpContext();
            httpContext.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("10.20.30.40");
            controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

            var result = await controller.Profile();
            var viewResult = Assert.IsType<ViewResult>(result);

            Assert.Equal("10.20.30.40", viewResult.ViewData["ClientIp"]);
            Assert.NotNull(viewResult.ViewData["ClientMac"]);
        }

        [Fact]
        public async Task TicketsController_Create_CapturesClientIpAndMac()
        {
            var db = GetInMemoryDbContext(Guid.NewGuid().ToString());
            var user = new User { Id = 1, FullName = "Staff User", Role = SystemRoles.EndUser, IsActive = true };
            db.Users.Add(user);
            await db.SaveChangesAsync();

            var currentUserService = new Mock<ICurrentUserService>();
            currentUserService.Setup(s => s.GetUserId()).Returns(user.Id);
            currentUserService.Setup(s => s.GetCurrentUser()).Returns(user);
            currentUserService.Setup(s => s.IsEndUser()).Returns(true);

            var notifService = new Mock<INotificationService>();
            var controller = new TicketsController(db, currentUserService.Object, notifService.Object);
            var httpContext = new DefaultHttpContext();
            httpContext.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("172.16.5.99");
            controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
            controller.TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());

            var ticket = new Ticket
            {
                Type = "Hardware",
                Priority = 2,
                PcName = "PC-LAB-01"
            };

            var result = await controller.Create(ticket, initialMessage: "Screen is flickering");
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);

            var savedTicket = await db.Tickets.Include(t => t.Messages).FirstOrDefaultAsync();
            Assert.NotNull(savedTicket);
            Assert.Equal("172.16.5.99", savedTicket.IpAddress);
            Assert.NotNull(savedTicket.MacAddress);

            var initialMsg = savedTicket.Messages.FirstOrDefault();
            Assert.NotNull(initialMsg);
            Assert.Equal("172.16.5.99", initialMsg.IpAddress);
            Assert.NotNull(initialMsg.MacAddress);
        }

        // ==========================================
        // TASK 2: Specialist Dropdown by Asset Department
        // ==========================================

        [Fact]
        public void User_BelongsToResponsibleDepartment_RespectsSpecializedMaintenanceTeam()
        {
            var itDept = new Department { Id = 1, Name = "Information Technology" };

            // User has maintenance_team = BIOMEDICAL, but their Department happens to be Information Technology
            var bioTech = new User
            {
                Id = 1,
                FullName = "Abbass chd",
                Role = SystemRoles.Maintenance,
                MaintenanceTeam = MaintenanceTeams.Biomedical,
                DepartmentId = 1,
                Department = itDept
            };

            // User belongs strictly to BIOMEDICAL
            Assert.True(bioTech.BelongsToResponsibleDepartment(MaintenanceTeams.Biomedical));
            // User should NEVER be matched to IT even though DepartmentId is IT
            Assert.False(bioTech.BelongsToResponsibleDepartment(MaintenanceTeams.IT));
            Assert.False(bioTech.BelongsToResponsibleDepartment(MaintenanceTeams.General));
        }

        [Fact]
        public async Task TicketsController_Details_FiltersSpecialistsByAssetDepartment()
        {
            var db = GetInMemoryDbContext(Guid.NewGuid().ToString());

            // 1. Setup departments and users
            var itTech = new User { Id = 1, FullName = "Hadi IT", Role = SystemRoles.Maintenance, MaintenanceTeam = MaintenanceTeams.IT, IsActive = true };
            var bioTech = new User { Id = 2, FullName = "Tariq Bio", Role = SystemRoles.Maintenance, MaintenanceTeam = MaintenanceTeams.Biomedical, IsActive = true };
            var genTech = new User { Id = 3, FullName = "Ahmed Gen", Role = SystemRoles.Maintenance, MaintenanceTeam = MaintenanceTeams.General, IsActive = true };
            db.Users.AddRange(itTech, bioTech, genTech);

            // 2. Asset belongs to BIOMEDICAL
            var bioAsset = new Asset { Id = 10, HospitalNo = "BIO-ECG-01", MaintenanceTeam = MaintenanceTeams.Biomedical };
            db.Assets.Add(bioAsset);

            var ticket = new Ticket
            {
                Id = 100,
                AssetId = bioAsset.Id,
                Asset = bioAsset,
                OpenedBy = 1,
                TargetTeam = MaintenanceTeams.Biomedical,
                StatusId = 1
            };
            db.Tickets.Add(ticket);
            await db.SaveChangesAsync();

            var currentUserService = new Mock<ICurrentUserService>();
            currentUserService.Setup(s => s.CanAccessTicket(It.IsAny<Ticket>())).Returns(true);
            currentUserService.Setup(s => s.GetUserId()).Returns(1);

            var controller = new TicketsController(db, currentUserService.Object, Mock.Of<INotificationService>());
            controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };

            var result = await controller.Details(ticket.Id);
            var viewResult = Assert.IsType<ViewResult>(result);
            var vm = Assert.IsType<TicketDetailsViewModel>(viewResult.Model);

            // Only Biomedical technician should be present!
            Assert.Contains(vm.DepartmentEmployees, u => u.Id == bioTech.Id);
            Assert.DoesNotContain(vm.DepartmentEmployees, u => u.Id == itTech.Id);
            Assert.DoesNotContain(vm.DepartmentEmployees, u => u.Id == genTech.Id);
        }

        [Fact]
        public async Task TicketsController_AssignTechnician_RejectsCrossTeamTechnician()
        {
            var db = GetInMemoryDbContext(Guid.NewGuid().ToString());
            var bioAsset = new Asset { Id = 1, HospitalNo = "BIO-01", MaintenanceTeam = MaintenanceTeams.Biomedical };
            db.Assets.Add(bioAsset);

            var ticket = new Ticket { Id = 50, AssetId = bioAsset.Id, Asset = bioAsset, TargetTeam = MaintenanceTeams.Biomedical, StatusId = 1 };
            var itTech = new User { Id = 7, FullName = "IT Specialist", Role = SystemRoles.Maintenance, MaintenanceTeam = MaintenanceTeams.IT, IsActive = true };
            db.Tickets.Add(ticket);
            db.Users.Add(itTech);
            await db.SaveChangesAsync();

            var currentUserService = new Mock<ICurrentUserService>();
            currentUserService.Setup(s => s.CanManageTicket(It.IsAny<Ticket>())).Returns(true);
            currentUserService.Setup(s => s.GetUserId()).Returns(1);

            var controller = new TicketsController(db, currentUserService.Object, Mock.Of<INotificationService>());
            var httpContext = new DefaultHttpContext();
            controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
            controller.TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());

            var result = await controller.AssignTechnician(ticket.Id, itTech.Id);
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);

            Assert.True(controller.TempData.ContainsKey("ErrorMessage"));
            Assert.Contains("Selected technician does not belong to the responsible BIOMEDICAL department", controller.TempData["ErrorMessage"]!.ToString());
        }

        // ==========================================
        // TASK 3: Maintenance Log Sidebar Badge Count
        // ==========================================

        [Fact]
        public async Task MaintenanceBadge_CountsOnlyDevicesCurrentlyUnderMaintenance()
        {
            var db = GetInMemoryDbContext(Guid.NewGuid().ToString());

            var itAsset1 = new Asset { Id = 1, HospitalNo = "IT-01", MaintenanceTeam = MaintenanceTeams.IT };
            var itAsset2 = new Asset { Id = 2, HospitalNo = "IT-02", MaintenanceTeam = MaintenanceTeams.IT };
            var itAsset3 = new Asset { Id = 3, HospitalNo = "IT-03", MaintenanceTeam = MaintenanceTeams.IT };
            db.Assets.AddRange(itAsset1, itAsset2, itAsset3);

            // Asset 1 has an active work order under maintenance
            db.AssetMaintenances.Add(new AssetMaintenance { Id = 1, AssetId = itAsset1.Id, Asset = itAsset1, WorkOrderStatus = "Under Maintenance" });
            // Asset 2 has an active work order under maintenance
            db.AssetMaintenances.Add(new AssetMaintenance { Id = 2, AssetId = itAsset2.Id, Asset = itAsset2, WorkOrderStatus = "Under Maintenance" });
            // Asset 3 has a COMPLETED maintenance log (not currently in maintenance)
            db.AssetMaintenances.Add(new AssetMaintenance { Id = 3, AssetId = itAsset3.Id, Asset = itAsset3, WorkOrderStatus = "Completed" });

            await db.SaveChangesAsync();

            // Query exactly matching the _Layout logic:
            var activeMaintOrdersQuery = db.AssetMaintenances.Where(m => m.WorkOrderStatus == "Under Maintenance" && m.Asset != null && m.Asset.MaintenanceTeam == MaintenanceTeams.IT);
            int badgeCount = await activeMaintOrdersQuery.Select(m => m.AssetId).Where(id => id.HasValue).Distinct().CountAsync();

            // Should be 2, NOT 3!
            Assert.Equal(2, badgeCount);
        }

        // ==========================================
        // TASK 4: Checklist Category Filter & Enhanced Toolbar
        // ==========================================

        [Fact]
        public async Task ChecklistsController_Index_CategoryDropdownShowsOnlySameDepartment()
        {
            var db = GetInMemoryDbContext(Guid.NewGuid().ToString());

            var itCat1 = new AssetCategory { Id = 1, Name = "Laptops", Department = MaintenanceTeams.IT, IsActive = true };
            var itCat2 = new AssetCategory { Id = 2, Name = "Network Switches", Department = MaintenanceTeams.IT, IsActive = true };
            var bioCat = new AssetCategory { Id = 3, Name = "Defibrillators", Department = MaintenanceTeams.Biomedical, IsActive = true };
            var genCat = new AssetCategory { Id = 4, Name = "Generators", Department = MaintenanceTeams.General, IsActive = true };

            db.AssetCategories.AddRange(itCat1, itCat2, bioCat, genCat);

            db.ChecklistForms.Add(new ChecklistForm { Id = 1, Title = "IT Laptop Checklist", Department = MaintenanceTeams.IT, CategoryId = 1, ChecklistType = "Periodic Maintenance", IsActive = true });
            db.ChecklistForms.Add(new ChecklistForm { Id = 2, Title = "Biomedical Checklist", Department = MaintenanceTeams.Biomedical, CategoryId = 3, ChecklistType = "Periodic Maintenance", IsActive = true });
            await db.SaveChangesAsync();

            var currentUserService = new Mock<ICurrentUserService>();
            currentUserService.Setup(s => s.IsMaintenance()).Returns(true);
            currentUserService.Setup(s => s.GetMaintenanceTeam()).Returns(MaintenanceTeams.IT);

            var controller = new ChecklistsController(db, currentUserService.Object);
            var result = await controller.Index();
            var viewResult = Assert.IsType<ViewResult>(result);

            var categories = Assert.IsAssignableFrom<List<AssetCategory>>(viewResult.ViewData["Categories"]);
            // Only IT categories should be returned for IT maintenance user!
            Assert.All(categories, c => Assert.Equal(MaintenanceTeams.IT, c.Department));
            Assert.Contains(categories, c => c.Id == itCat1.Id);
            Assert.Contains(categories, c => c.Id == itCat2.Id);
            Assert.DoesNotContain(categories, c => c.Id == bioCat.Id);
            Assert.DoesNotContain(categories, c => c.Id == genCat.Id);
        }

        [Fact]
        public async Task ChecklistsController_Index_FiltersBySearchKeyword()
        {
            var db = GetInMemoryDbContext(Guid.NewGuid().ToString());

            db.ChecklistForms.Add(new ChecklistForm { Id = 1, Title = "Biomedical Ultrasound Calibration", Department = MaintenanceTeams.Biomedical, ChecklistType = "Calibration", IsActive = true });
            db.ChecklistForms.Add(new ChecklistForm { Id = 2, Title = "General AC Unit Periodic Check", Department = MaintenanceTeams.General, ChecklistType = "Periodic Maintenance", IsActive = true });
            await db.SaveChangesAsync();

            var currentUserService = new Mock<ICurrentUserService>();
            currentUserService.Setup(s => s.IsAdmin()).Returns(true);

            var controller = new ChecklistsController(db, currentUserService.Object);
            var result = await controller.Index(search: "Ultrasound");
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<List<ChecklistForm>>(viewResult.Model);

            Assert.Single(model);
            Assert.Equal("Biomedical Ultrasound Calibration", model[0].Title);
        }

        [Fact]
        public async Task TicketsController_Create_DoesNotBlockTicketIfExistingTicketHasClosedDT()
        {
            var db = GetInMemoryDbContext(Guid.NewGuid().ToString());
            var user = new User { Id = 1, FullName = "Staff User", Role = SystemRoles.EndUser, IsActive = true };
            var asset = new Asset { Id = 23, HospitalNo = "HOSP-IT-E2E-99", Brand = "Dell", Type = "Laptop", UserId = user.Id };
            db.Users.Add(user);
            db.Assets.Add(asset);

            // Ticket 20 had ClosedDT set, even if StatusId was set to 2 by supplier return
            var ticket20 = new Ticket
            {
                Id = 20,
                AssetId = asset.Id,
                OpenedBy = user.Id,
                StatusId = 2,
                ClosedDT = DateTime.Now.AddDays(-5) // Closed in the past!
            };
            db.Tickets.Add(ticket20);
            await db.SaveChangesAsync();

            var currentUserService = new Mock<ICurrentUserService>();
            currentUserService.Setup(s => s.GetUserId()).Returns(user.Id);
            currentUserService.Setup(s => s.GetCurrentUser()).Returns(user);
            currentUserService.Setup(s => s.IsEndUser()).Returns(true);

            var controller = new TicketsController(db, currentUserService.Object, Mock.Of<INotificationService>());
            var httpContext = new DefaultHttpContext();
            controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
            controller.TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());

            // Attempt to open a new ticket for asset 23
            var newTicket = new Ticket
            {
                AssetId = asset.Id,
                Type = "Hardware",
                Priority = 2
            };

            var result = await controller.Create(newTicket, initialMessage: "New issue");
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Details", redirectResult.ActionName);

            // New ticket must have been created successfully, not blocked!
            var createdTicket = await db.Tickets.FirstOrDefaultAsync(t => t.Id != 20);
            Assert.NotNull(createdTicket);
            Assert.Equal(asset.Id, createdTicket.AssetId);
        }

        // ==========================================
        // DANGER ZONE: Automatic Domain & Live DB Handshake
        // ==========================================

        [Fact]
        public async Task DirectoryController_NonAdminUser_BlockedWithForbidResult()
        {
            var db = GetInMemoryDbContext(Guid.NewGuid().ToString());
            var currentUserService = new Mock<ICurrentUserService>();
            currentUserService.Setup(s => s.CanManageUsers()).Returns(false);
            currentUserService.Setup(s => s.IsMaintenance()).Returns(true);

            var controller = new DirectoryController(db, currentUserService.Object);
            var result = await controller.Index(tab: "users");

            Assert.IsType<ForbidResult>(result);
        }

        [Fact]
        public async Task DirectoryController_AdminWithNonAdminDatabaseRole_BlockedWithForbidResult()
        {
            var db = GetInMemoryDbContext(Guid.NewGuid().ToString());
            // User modified their role in database to EndUser!
            var user = new User { Id = 1, FullName = "Abbas", DomainUser = "Abbas", Role = SystemRoles.EndUser, IsActive = true };
            db.Users.Add(user);
            await db.SaveChangesAsync();

            var currentUserService = new Mock<ICurrentUserService>();
            currentUserService.Setup(s => s.CanManageUsers()).Returns(true); // Claims might still say admin
            currentUserService.Setup(s => s.GetUserId()).Returns(user.Id);
            currentUserService.Setup(s => s.GetDomainUser()).Returns("Abbas");

            var controller = new DirectoryController(db, currentUserService.Object);
            var httpContext = new DefaultHttpContext();
            controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

            // Handshake must query live DB, detect role is EndUser, and block access!
            var result = await controller.Index(tab: "users");
            Assert.IsType<ForbidResult>(result);
        }

        [Fact]
        public async Task DirectoryController_ActiveAdminInDatabase_HandshakePassesAndRendersIndex()
        {
            var db = GetInMemoryDbContext(Guid.NewGuid().ToString());
            var user = new User { Id = 1, FullName = "Abbas", DomainUser = "Abbas", Role = SystemRoles.Admin, IsActive = true };
            db.Users.Add(user);
            await db.SaveChangesAsync();

            var currentUserService = new Mock<ICurrentUserService>();
            currentUserService.Setup(s => s.CanManageUsers()).Returns(true);
            currentUserService.Setup(s => s.GetUserId()).Returns(user.Id);
            currentUserService.Setup(s => s.GetDomainUser()).Returns("Abbas");

            var controller = new DirectoryController(db, currentUserService.Object);
            var httpContext = new DefaultHttpContext();
            controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

            var result = await controller.Index(tab: "users");
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.True(string.IsNullOrEmpty(viewResult.ViewName) || viewResult.ViewName == "Index");
        }

        [Fact]
        public async Task DirectoryController_InactiveAdminInDatabase_BlockedWithForbidResult()
        {
            var db = GetInMemoryDbContext(Guid.NewGuid().ToString());
            var user = new User { Id = 1, FullName = "Abbas", DomainUser = "Abbas", Role = SystemRoles.Admin, IsActive = false }; // Deactivated!
            db.Users.Add(user);
            await db.SaveChangesAsync();

            var currentUserService = new Mock<ICurrentUserService>();
            currentUserService.Setup(s => s.CanManageUsers()).Returns(true);
            currentUserService.Setup(s => s.GetUserId()).Returns(user.Id);
            currentUserService.Setup(s => s.GetDomainUser()).Returns("Abbas");

            var controller = new DirectoryController(db, currentUserService.Object);
            var httpContext = new DefaultHttpContext();
            controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

            var result = await controller.Index(tab: "users");
            Assert.IsType<ForbidResult>(result);
        }

        [Fact]
        public async Task DirectoryController_CreateUser_WhenDatabaseRoleIsNotAdmin_BlockedWithForbidResult()
        {
            var db = GetInMemoryDbContext(Guid.NewGuid().ToString());
            var user = new User { Id = 1, FullName = "Abbas", DomainUser = "Abbas", Role = SystemRoles.EndUser, IsActive = true };
            db.Users.Add(user);
            await db.SaveChangesAsync();

            var currentUserService = new Mock<ICurrentUserService>();
            currentUserService.Setup(s => s.CanManageUsers()).Returns(true);
            currentUserService.Setup(s => s.GetUserId()).Returns(user.Id);
            currentUserService.Setup(s => s.GetDomainUser()).Returns("Abbas");

            var controller = new DirectoryController(db, currentUserService.Object);
            var httpContext = new DefaultHttpContext();
            controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

            var newUser = new User { FullName = "Hacked User", Role = SystemRoles.Admin };
            var result = await controller.CreateUser(newUser);

            Assert.IsType<ForbidResult>(result);
            Assert.Single(db.Users); // Only original user, newUser not added
        }

        [Fact]
        public async Task DirectoryController_CreateUser_WhenDatabaseRoleIsAdmin_Succeeds()
        {
            var db = GetInMemoryDbContext(Guid.NewGuid().ToString());
            var user = new User { Id = 1, FullName = "Abbas", DomainUser = "Abbas", Role = SystemRoles.Admin, IsActive = true };
            db.Users.Add(user);
            await db.SaveChangesAsync();

            var currentUserService = new Mock<ICurrentUserService>();
            currentUserService.Setup(s => s.CanManageUsers()).Returns(true);
            currentUserService.Setup(s => s.GetUserId()).Returns(user.Id);
            currentUserService.Setup(s => s.GetDomainUser()).Returns("Abbas");

            var controller = new DirectoryController(db, currentUserService.Object);
            var httpContext = new DefaultHttpContext();
            controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
            controller.TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());

            var newUser = new User { FullName = "New Staff Member", Role = SystemRoles.EndUser, DomainUser = "HOSP\\newstaff" };
            var result = await controller.CreateUser(newUser);

            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
            Assert.Equal(2, db.Users.Count());
            Assert.Contains(db.Users, u => u.FullName == "New Staff Member");
        }

        // ==========================================
        // MAINTENANCE LOG: Application-Managed JSON Parts
        // ==========================================

        [Fact]
        public void NormalizePartsJson_WithValidJsonArray_PreservesCleanJson()
        {
            var json = "[{\"partName\":\"1TB NVMe SSD\",\"partNumber\":\"SSD-970\",\"quantity\":2,\"notes\":\"Replaced bad drive\"}]";
            var result = TicketsController.NormalizePartsJson(json);

            Assert.NotNull(result);
            Assert.Equal(json, result);

            var items = JsonSerializer.Deserialize<List<ReplacedPartItem>>(result, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            Assert.NotNull(items);
            Assert.Single(items);
            Assert.Equal("1TB NVMe SSD", items[0].PartName);
            Assert.Equal(2, items[0].Quantity);
        }

        [Fact]
        public void NormalizePartsJson_WithSingleJsonObject_WrapsInArray()
        {
            var json = "{\"partName\":\"Battery Pack\",\"quantity\":1}";
            var result = TicketsController.NormalizePartsJson(json);

            Assert.NotNull(result);
            Assert.Equal($"[{json}]", result);

            var items = JsonSerializer.Deserialize<List<ReplacedPartItem>>(result, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            Assert.NotNull(items);
            Assert.Single(items);
            Assert.Equal("Battery Pack", items[0].PartName);
        }

        [Fact]
        public void NormalizePartsJson_WithPlainText_TranslatesToReplacedPartItemJson()
        {
            var plainText = "Replaced CMOS Battery CR2032 and thermal paste";
            var result = TicketsController.NormalizePartsJson(plainText);

            Assert.NotNull(result);
            Assert.StartsWith("[{", result);

            var items = JsonSerializer.Deserialize<List<ReplacedPartItem>>(result, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            Assert.NotNull(items);
            Assert.Single(items);
            Assert.Equal(plainText, items[0].PartName);
            Assert.Equal(1, items[0].Quantity);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("[]")]
        [InlineData("{}")]
        public void NormalizePartsJson_WithEmptyOrNull_ReturnsNull(string? input)
        {
            var result = TicketsController.NormalizePartsJson(input);
            Assert.Null(result);
        }

        [Fact]
        public async Task TicketsController_LogMaintenance_SavesReplacedPartsAsValidJson()
        {
            var db = GetInMemoryDbContext(Guid.NewGuid().ToString());
            var user = new User { Id = 10, FullName = "Tech Specialist", Role = SystemRoles.Maintenance };
            var asset = new Asset { Id = 50, HospitalNo = "HOSP-IT-LOG1" };
            var ticket = new Ticket { Id = 100, AssetId = asset.Id, OpenedBy = 1, TargetTeam = MaintenanceTeams.IT, StatusId = 1 };

            db.Users.Add(user);
            db.Assets.Add(asset);
            db.Tickets.Add(ticket);
            await db.SaveChangesAsync();

            var currentUserService = new Mock<ICurrentUserService>();
            currentUserService.Setup(s => s.CanManageTicket(It.IsAny<Ticket>())).Returns(true);
            currentUserService.Setup(s => s.GetUserId()).Returns(user.Id);

            var controller = new TicketsController(db, currentUserService.Object, Mock.Of<INotificationService>());
            var httpContext = new DefaultHttpContext();
            controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
            controller.TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());

            // Submit with dynamic parts JSON from text boxes
            var partsJson = "[{\"partName\":\"16GB DDR4 RAM\",\"partNumber\":\"KVR26S19S8/16\",\"quantity\":2,\"notes\":\"Upgraded memory\"}]";
            var result = await controller.LogMaintenance(
                ticketId: ticket.Id,
                assetId: asset.Id,
                technicianId: user.Id,
                supplierId: null,
                maintenanceType: 1,
                description: "Replaced faulty RAM sticks",
                partsReplaced: partsJson
            );

            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Details", redirectResult.ActionName);

            var log = await db.AssetMaintenances.FirstOrDefaultAsync(m => m.TicketId == ticket.Id);
            Assert.NotNull(log);
            Assert.Equal(partsJson, log.PartsReplaced);
            Assert.Single(log.ParsedParts);
            Assert.Equal("16GB DDR4 RAM", log.ParsedParts[0].PartName);
            Assert.Equal(2, log.ParsedParts[0].Quantity);
        }

        [Fact]
        public async Task MaintenanceController_Log_Post_SavesReplacedPartsAsValidJson()
        {
            var db = GetInMemoryDbContext(Guid.NewGuid().ToString());
            var tech = new User { Id = 20, FullName = "Bio Tech", Role = SystemRoles.Maintenance, MaintenanceTeam = MaintenanceTeams.Biomedical };
            var asset = new Asset { Id = 70, HospitalNo = "HOSP-BIO-LOG1", MaintenanceTeam = MaintenanceTeams.Biomedical, PeriodicInterval = 90 };

            db.Users.Add(tech);
            db.Assets.Add(asset);
            await db.SaveChangesAsync();

            var currentUserService = new Mock<ICurrentUserService>();
            currentUserService.Setup(s => s.GetUserId()).Returns(tech.Id);
            currentUserService.Setup(s => s.IsMaintenance()).Returns(true);
            currentUserService.Setup(s => s.GetMaintenanceTeam()).Returns(MaintenanceTeams.Biomedical);

            var controller = new MaintenanceController(db, currentUserService.Object);
            var httpContext = new DefaultHttpContext();
            controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
            controller.TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());

            var workOrder = new AssetMaintenance
            {
                AssetId = asset.Id,
                UserId = tech.Id,
                MaintenanceType = 2, // Periodic Check
                Description = "Completed 3-month periodic inspection and filter replacement",
                PartsReplaced = "[{\"partName\":\"HEPA Filter Pack\",\"partNumber\":\"HF-440\",\"quantity\":1,\"notes\":\"New filter installed\"}]"
            };

            var result = await controller.Log(workOrder);
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);

            var saved = await db.AssetMaintenances.FirstOrDefaultAsync(m => m.AssetId == asset.Id);
            Assert.NotNull(saved);
            Assert.Equal("Completed", saved.WorkOrderStatus);
            Assert.Single(saved.ParsedParts);
            Assert.Equal("HEPA Filter Pack", saved.ParsedParts[0].PartName);
            Assert.Equal(1, saved.ParsedParts[0].Quantity);

            // Asset periodic date should have been updated
            var updatedAsset = await db.Assets.FindAsync(asset.Id);
            Assert.NotNull(updatedAsset);
            Assert.NotNull(updatedAsset.LastPeriodic);
            Assert.NotNull(updatedAsset.NextPeriodic);
        }
    }
}
