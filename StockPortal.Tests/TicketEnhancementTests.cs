using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
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
    public class TicketEnhancementTests
    {
        private StockDbContext CreateInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<StockDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            return new StockDbContext(options);
        }

        // ==========================================
        // Task 4: Maintenance Sub-Type Identification Tests
        // ==========================================
        [Theory]
        [InlineData("BIOMEDICAL", "MAINTENANCE (BIOMEDICAL)")]
        [InlineData("IT", "MAINTENANCE (IT)")]
        [InlineData("GENERAL", "MAINTENANCE (GENERAL)")]
        [InlineData(null, "MAINTENANCE (GENERAL)")]
        [InlineData("", "MAINTENANCE (GENERAL)")]
        public void User_RoleDisplay_ShowsSpecificMaintenanceDomain_WhenMaintenance(string? team, string expectedDisplay)
        {
            var user = new User
            {
                FullName = "Test Maintenance Tech",
                Role = SystemRoles.Maintenance,
                MaintenanceTeam = team
            };

            Assert.Equal(expectedDisplay, user.RoleDisplay);
        }

        [Fact]
        public void User_RoleDisplay_ShowsOriginalRole_WhenNotMaintenance()
        {
            var admin = new User { Role = SystemRoles.Admin };
            var endUser = new User { Role = SystemRoles.EndUser };

            Assert.Equal(SystemRoles.Admin, admin.RoleDisplay);
            Assert.Equal(SystemRoles.EndUser, endUser.RoleDisplay);
        }

        // ==========================================
        // Task 2: Department-Scoped Ticket Counter Tests
        // ==========================================
        [Fact]
        public void DepartmentTicketCounter_FiltersByBiomedicalTeam_ForBiomedicalUser()
        {
            using var context = CreateInMemoryDbContext();

            // Seed tickets: 2 Biomedical, 3 IT, 1 General
            context.Tickets.AddRange(
                new Ticket { Id = 1, TargetTeam = MaintenanceTeams.Biomedical, StatusId = 1 }, // Opened
                new Ticket { Id = 2, TargetTeam = MaintenanceTeams.Biomedical, StatusId = 2 }, // Pending
                new Ticket { Id = 3, TargetTeam = MaintenanceTeams.Biomedical, StatusId = 5 }, // Closed (should not count)
                new Ticket { Id = 4, TargetTeam = MaintenanceTeams.IT, StatusId = 1 },
                new Ticket { Id = 5, TargetTeam = MaintenanceTeams.IT, StatusId = 2 },
                new Ticket { Id = 6, TargetTeam = MaintenanceTeams.General, StatusId = 1 }
            );
            context.SaveChanges();

            // Biomedical maintenance user query
            string biomedicalTeam = MaintenanceTeams.Biomedical;
            var openTicketsCount = context.Tickets
                .Where(t => t.StatusId == 1 || t.StatusId == 2)
                .Where(t => t.TargetTeam == biomedicalTeam)
                .Count();

            // Should be exactly 2 biomedical tickets (Tickets 1 & 2), NOT 5 open tickets across hospital
            Assert.Equal(2, openTicketsCount);
        }

        // ==========================================
        // Task 3: Notifications Service Tests
        // ==========================================
        [Fact]
        public async Task NotificationService_BroadcastsToAdminsAndDepartmentUsers_WhenTicketCreated()
        {
            using var context = CreateInMemoryDbContext();

            // Seed Users: 1 Admin, 2 Biomedical Techs, 1 Cardiology EndUser (requester), 1 IT Tech
            var admin = new User { Id = 1, FullName = "Admin User", Role = SystemRoles.Admin, IsActive = true };
            var bioTech1 = new User { Id = 2, FullName = "Bio Tech 1", Role = SystemRoles.Maintenance, MaintenanceTeam = MaintenanceTeams.Biomedical, DepartmentId = 10, IsActive = true };
            var bioTech2 = new User { Id = 3, FullName = "Bio Tech 2", Role = SystemRoles.Maintenance, MaintenanceTeam = MaintenanceTeams.Biomedical, DepartmentId = 10, IsActive = true };
            var requester = new User { Id = 4, FullName = "Dr. Cardio", Role = SystemRoles.EndUser, DepartmentId = 20, IsActive = true };
            var itTech = new User { Id = 5, FullName = "IT Tech", Role = SystemRoles.Maintenance, MaintenanceTeam = MaintenanceTeams.IT, DepartmentId = 30, IsActive = true };

            context.Users.AddRange(admin, bioTech1, bioTech2, requester, itTech);
            await context.SaveChangesAsync();

            var ticket = new Ticket
            {
                Id = 101,
                OpenedBy = requester.Id,
                TargetTeam = MaintenanceTeams.Biomedical,
                Type = "Hardware",
                Priority = 3
            };
            context.Tickets.Add(ticket);
            await context.SaveChangesAsync();

            var notificationService = new NotificationService(context);
            await notificationService.NotifyTicketCreatedAsync(ticket, requester);

            // Verify notifications: Admin and Biomedical Techs should receive notifications, but not the requester or unrelated IT tech
            var adminNotifs = await notificationService.GetUserNotificationsAsync(admin.Id);
            var bio1Notifs = await notificationService.GetUserNotificationsAsync(bioTech1.Id);
            var bio2Notifs = await notificationService.GetUserNotificationsAsync(bioTech2.Id);
            var requesterNotifs = await notificationService.GetUserNotificationsAsync(requester.Id);
            var itNotifs = await notificationService.GetUserNotificationsAsync(itTech.Id);

            Assert.Single(adminNotifs);
            Assert.Contains("Ticket #101 Opened", adminNotifs.First().Title);

            Assert.Single(bio1Notifs);
            Assert.Single(bio2Notifs);

            Assert.Empty(requesterNotifs); // Initiator excluded
            Assert.Empty(itNotifs); // Unrelated team excluded
        }

        [Fact]
        public async Task NotificationService_BroadcastsToDepartmentAndAdmins_WhenMessagePosted()
        {
            using var context = CreateInMemoryDbContext();

            var admin = new User { Id = 1, FullName = "Admin User", Role = SystemRoles.Admin, IsActive = true };
            var bioTech = new User { Id = 2, FullName = "Bio Tech", Role = SystemRoles.Maintenance, MaintenanceTeam = MaintenanceTeams.Biomedical, IsActive = true };
            var requester = new User { Id = 4, FullName = "Requester", Role = SystemRoles.EndUser, DepartmentId = 20, IsActive = true };

            context.Users.AddRange(admin, bioTech, requester);
            await context.SaveChangesAsync();

            var ticket = new Ticket
            {
                Id = 202,
                OpenedBy = requester.Id,
                TargetTeam = MaintenanceTeams.Biomedical
            };
            context.Tickets.Add(ticket);
            await context.SaveChangesAsync();

            var msg = new TicketMsg
            {
                Id = 501,
                TicketId = ticket.Id,
                UserId = requester.Id,
                Msg = "Probe cable is defective, please inspect immediately."
            };
            context.TicketMessages.Add(msg);
            await context.SaveChangesAsync();

            var notificationService = new NotificationService(context);
            await notificationService.NotifyTicketMessageAsync(ticket, msg, requester);

            var adminNotifs = await notificationService.GetUserNotificationsAsync(admin.Id);
            var bioNotifs = await notificationService.GetUserNotificationsAsync(bioTech.Id);
            var requesterNotifs = await notificationService.GetUserNotificationsAsync(requester.Id);

            Assert.Single(adminNotifs);
            Assert.Contains("Probe cable is defective", adminNotifs.First().Message);

            Assert.Single(bioNotifs);
            Assert.Empty(requesterNotifs); // Sender excluded
        }

        [Fact]
        public async Task NotificationService_MarkAsRead_UpdatesReadStatus()
        {
            using var context = CreateInMemoryDbContext();
            var user = new User { Id = 10, FullName = "Test User" };
            context.Users.Add(user);

            var notif = new Notification
            {
                Id = 1,
                UserId = user.Id,
                Title = "Test Update",
                Message = "Important maintenance update",
                IsRead = false
            };
            context.Notifications.Add(notif);
            await context.SaveChangesAsync();

            var notificationService = new NotificationService(context);
            int unreadBefore = await notificationService.GetUnreadCountAsync(user.Id);
            Assert.Equal(1, unreadBefore);

            await notificationService.MarkAsReadAsync(notif.Id, user.Id);

            int unreadAfter = await notificationService.GetUnreadCountAsync(user.Id);
            Assert.Equal(0, unreadAfter);
        }

        // ==========================================
        // Task 1 (Phase 2): Not Closed Status Tab Test
        // ==========================================
        [Fact]
        public async Task TicketsController_Index_StatusIdMinusOne_FiltersNotClosedTickets()
        {
            using var context = CreateInMemoryDbContext();

            context.Tickets.AddRange(
                new Ticket { Id = 101, StatusId = 1, OpenedDT = DateTime.Now }, // Opened
                new Ticket { Id = 102, StatusId = 2, OpenedDT = DateTime.Now }, // Pending
                new Ticket { Id = 103, StatusId = 5, ClosedDT = DateTime.Now, OpenedDT = DateTime.Now.AddDays(-1) }, // Closed
                new Ticket { Id = 104, StatusId = 3, OpenedDT = DateTime.Now } // Delayed
            );
            await context.SaveChangesAsync();

            var mockUserService = new Mock<ICurrentUserService>();
            mockUserService.Setup(s => s.GetUserId()).Returns(1);
            mockUserService.Setup(s => s.IsAdmin()).Returns(true);

            var controller = new TicketsController(context, mockUserService.Object, new NotificationService(context));
            var result = await controller.Index(statusId: -1, priority: null, type: null, search: null, filter: null);

            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<List<Ticket>>(viewResult.Model);

            // Should contain tickets 101, 102, 104, but NOT closed ticket 103
            Assert.Equal(3, model.Count);
            Assert.DoesNotContain(model, t => t.Id == 103);
            Assert.All(model, t => Assert.True(t.StatusId != 5 && t.ClosedDT == null));
        }

        // ==========================================
        // Task 3 (Phase 2): End-User Team Assignment Tests
        // ==========================================
        [Fact]
        public async Task EndUser_CanAssignTargetTeam_OnTicketCreation()
        {
            using var context = CreateInMemoryDbContext();

            // IT asset
            var itAsset = new Asset { Id = 50, HospitalNo = "PC-50", MaintenanceTeam = MaintenanceTeams.IT };
            context.Assets.Add(itAsset);
            await context.SaveChangesAsync();

            var mockUserService = new Mock<ICurrentUserService>();
            mockUserService.Setup(s => s.GetUserId()).Returns(20);
            mockUserService.Setup(s => s.IsEndUser()).Returns(true);

            var controller = new TicketsController(context, mockUserService.Object, new NotificationService(context));
            controller.TempData = new Microsoft.AspNetCore.Mvc.ViewFeatures.TempDataDictionary(
                new Microsoft.AspNetCore.Http.DefaultHttpContext(),
                Mock.Of<Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataProvider>());

            var ticket = new Ticket
            {
                AssetId = itAsset.Id,
                OpenedBy = 20,
                TargetTeam = MaintenanceTeams.Biomedical, // User explicitly assigned to Biomedical
                Type = "Hardware Breakdown",
                Priority = 2
            };

            await controller.Create(ticket, manualAsset: null, initialMessage: "Medical sensor plugged into PC failed", assignedTechId: null);

            var created = await context.Tickets.FirstOrDefaultAsync(t => t.AssetId == itAsset.Id);
            Assert.NotNull(created);
            // Must preserve user's chosen team (Biomedical), NOT overwrite with asset's team (IT)
            Assert.Equal(MaintenanceTeams.Biomedical, created.TargetTeam);
        }

        [Fact]
        public async Task EndUser_CanReassignTargetTeam_ForOwnTicket()
        {
            using var context = CreateInMemoryDbContext();

            var ticket = new Ticket
            {
                Id = 200,
                OpenedBy = 25,
                TargetTeam = MaintenanceTeams.IT,
                StatusId = 1
            };
            context.Tickets.Add(ticket);
            await context.SaveChangesAsync();

            var mockUserService = new Mock<ICurrentUserService>();
            mockUserService.Setup(s => s.GetUserId()).Returns(25);
            mockUserService.Setup(s => s.IsEndUser()).Returns(true);

            var controller = new TicketsController(context, mockUserService.Object, new NotificationService(context));
            controller.TempData = new Microsoft.AspNetCore.Mvc.ViewFeatures.TempDataDictionary(
                new Microsoft.AspNetCore.Http.DefaultHttpContext(),
                Mock.Of<Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataProvider>());

            var result = await controller.AssignTeam(ticket.Id, MaintenanceTeams.General, returnUrl: null);

            var updatedTicket = await context.Tickets.FindAsync(ticket.Id);
            Assert.NotNull(updatedTicket);
            Assert.Equal(MaintenanceTeams.General, updatedTicket.TargetTeam);

            // Verify timeline message was added
            var message = await context.TicketMessages.FirstOrDefaultAsync(m => m.TicketId == ticket.Id);
            Assert.NotNull(message);
            Assert.Contains("Transferred from IT to GENERAL", message.Msg);
        }

        // ==========================================
        // Task 4 (Phase 2): Logged-in Reply Author Test
        // ==========================================
        [Fact]
        public async Task AddMessage_UsesLoggedInUserId_ForMaintenanceUser()
        {
            using var context = CreateInMemoryDbContext();

            var techUser = new User { Id = 55, FullName = "Hasan Outa", Role = SystemRoles.Maintenance, MaintenanceTeam = MaintenanceTeams.General };
            context.Users.Add(techUser);

            var ticket = new Ticket
            {
                Id = 300,
                TargetTeam = MaintenanceTeams.General,
                StatusId = 1
            };
            context.Tickets.Add(ticket);
            await context.SaveChangesAsync();

            var mockUserService = new Mock<ICurrentUserService>();
            mockUserService.Setup(s => s.GetUserId()).Returns(55);
            mockUserService.Setup(s => s.CanAccessTicket(It.IsAny<Ticket>())).Returns(true);
            mockUserService.Setup(s => s.GetCurrentUser()).Returns(techUser);

            var controller = new TicketsController(context, mockUserService.Object, new NotificationService(context));
            controller.TempData = new Microsoft.AspNetCore.Mvc.ViewFeatures.TempDataDictionary(
                new Microsoft.AspNetCore.Http.DefaultHttpContext(),
                Mock.Of<Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataProvider>());

            // Even if an external or forged userId is submitted in the request, author must be logged in user (55)
            await controller.AddMessage(ticket.Id, userId: 999, msg: "Inspected wiring and repaired power socket.");

            var message = await context.TicketMessages.FirstOrDefaultAsync(m => m.TicketId == ticket.Id);
            Assert.NotNull(message);
            Assert.Equal(55, message.UserId); // Must match logged in user 55
        }

        // ==========================================
        // Task 2 (Phase 2): Notifications Popup Endpoint Test
        // ==========================================
        // ==========================================
        // Task: Custody Items Search & Maintenance Department Filter Tests
        // ==========================================
        [Fact]
        public async Task TicketsController_GetUserAssets_ReturnsCustodyAssetsWithMaintenanceDepartment()
        {
            using var context = CreateInMemoryDbContext();

            var user = new User { Id = 88, FullName = "Dr. Custodian", Role = SystemRoles.EndUser, IsActive = true };
            context.Users.Add(user);

            var room101 = new Room { Id = 1, RoomNo = 101, Floor = 1 };
            context.Rooms.Add(room101);

            // Add multiple custody assets belonging to different maintenance departments
            context.Assets.AddRange(
                new Asset { Id = 10, HospitalNo = "HOSP-IT-01", Brand = "Dell", Type = "Laptop", MaintenanceTeam = MaintenanceTeams.IT, UserId = 88, RoomId = 1 },
                new Asset { Id = 11, HospitalNo = "HOSP-BIO-01", Brand = "Philips", Type = "Patient Monitor", MaintenanceTeam = MaintenanceTeams.Biomedical, UserId = 88, RoomId = 1 },
                new Asset { Id = 12, HospitalNo = "HOSP-GEN-01", Brand = "Carrier", Type = "HVAC Unit", MaintenanceTeam = MaintenanceTeams.General, UserId = 88 },
                new Asset { Id = 13, HospitalNo = "HOSP-OTHER-01", Brand = "HP", Type = "PC", MaintenanceTeam = MaintenanceTeams.IT, UserId = 99 } // Different user
            );
            await context.SaveChangesAsync();

            var mockUserService = new Mock<ICurrentUserService>();
            mockUserService.Setup(s => s.GetUserId()).Returns(88);

            var controller = new TicketsController(context, mockUserService.Object, new NotificationService(context));
            var result = await controller.GetUserAssets(userId: 88);

            var jsonResult = Assert.IsType<JsonResult>(result);
            Assert.NotNull(jsonResult.Value);

            var countProp = jsonResult.Value.GetType().GetProperty("count");
            var successProp = jsonResult.Value.GetType().GetProperty("success");
            var assetsProp = jsonResult.Value.GetType().GetProperty("assets");

            Assert.NotNull(countProp);
            Assert.NotNull(assetsProp);

            var countVal = (int)countProp.GetValue(jsonResult.Value)!;
            var successVal = (bool)successProp!.GetValue(jsonResult.Value)!;

            Assert.True(successVal);
            Assert.Equal(3, countVal); // 3 assets for user 88, excluding user 99's asset
        }

        [Fact]
        public async Task TicketsController_Create_PreservesAssetIdAndTeam_WhenSelectedFromColleagueCustody()
        {
            using var context = CreateInMemoryDbContext();

            var userA = new User { Id = 91, FullName = "Submitter Secretary", Role = SystemRoles.EndUser, IsActive = true };
            var userB = new User { Id = 92, FullName = "Dr. Colleague", Role = SystemRoles.EndUser, IsActive = true };
            context.Users.AddRange(userA, userB);

            var bioAsset = new Asset
            {
                Id = 75,
                HospitalNo = "BIO-PUMP-75",
                Brand = "B. Braun",
                Type = "Infusion Pump",
                MaintenanceTeam = MaintenanceTeams.Biomedical,
                UserId = 92
            };
            context.Assets.Add(bioAsset);
            await context.SaveChangesAsync();

            var mockUserService = new Mock<ICurrentUserService>();
            mockUserService.Setup(s => s.GetUserId()).Returns(91);
            mockUserService.Setup(s => s.IsEndUser()).Returns(true);

            var controller = new TicketsController(context, mockUserService.Object, new NotificationService(context));
            controller.TempData = new Microsoft.AspNetCore.Mvc.ViewFeatures.TempDataDictionary(
                new Microsoft.AspNetCore.Http.DefaultHttpContext(),
                Mock.Of<Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataProvider>());

            var ticket = new Ticket
            {
                AssetId = bioAsset.Id,
                OpenedBy = 91,
                OnBehalfOf = 92,
                TargetTeam = MaintenanceTeams.Biomedical,
                Type = "Infusion & Syringe Pump",
                Priority = 3
            };

            var actionResult = await controller.Create(ticket, manualAsset: null, initialMessage: "Pump occlusion alarm won't clear", assignedTechId: null);

            var createdTicket = await context.Tickets.FirstOrDefaultAsync(t => t.AssetId == bioAsset.Id);
            Assert.NotNull(createdTicket);
            Assert.Equal(91, createdTicket.OpenedBy);
            Assert.Equal(92, createdTicket.OnBehalfOf);
            Assert.Equal(75, createdTicket.AssetId);
            Assert.Equal(MaintenanceTeams.Biomedical, createdTicket.TargetTeam);
        }

        // ==========================================
        // Task: PC Error Log & Screenshot Paste & Installed Software Tests
        // ==========================================
        [Fact]
        public void TicketsController_GetPcErrorLog_ReturnsTelemetryAndFormattedEvents()
        {
            using var context = CreateInMemoryDbContext();
            var mockUserService = new Mock<ICurrentUserService>();
            mockUserService.Setup(s => s.GetUserId()).Returns(1);

            var controller = new TicketsController(context, mockUserService.Object, new NotificationService(context));
            var result = controller.GetPcErrorLog("TEST-WORKSTATION-01");

            var jsonResult = Assert.IsType<JsonResult>(result);
            Assert.NotNull(jsonResult.Value);

            var successProp = jsonResult.Value.GetType().GetProperty("success");
            var machineNameProp = jsonResult.Value.GetType().GetProperty("machineName");
            var countProp = jsonResult.Value.GetType().GetProperty("count");
            var formattedProp = jsonResult.Value.GetType().GetProperty("formatted");

            Assert.NotNull(successProp);
            Assert.NotNull(machineNameProp);
            Assert.NotNull(countProp);
            Assert.NotNull(formattedProp);

            Assert.True((bool)successProp.GetValue(jsonResult.Value)!);
            Assert.Equal("TEST-WORKSTATION-01", (string)machineNameProp.GetValue(jsonResult.Value)!);
            Assert.True((int)countProp.GetValue(jsonResult.Value)! > 0);
            Assert.False(string.IsNullOrWhiteSpace((string)formattedProp.GetValue(jsonResult.Value)!));
        }

        [Fact]
        public async Task TicketsController_Create_SavesAffectedSoftwarePcNameErrorLogAndScreenshots()
        {
            using var context = CreateInMemoryDbContext();

            var user = new User { Id = 33, FullName = "Dr. Clinic", Role = SystemRoles.EndUser, IsActive = true };
            context.Users.Add(user);
            await context.SaveChangesAsync();

            var mockUserService = new Mock<ICurrentUserService>();
            mockUserService.Setup(s => s.GetUserId()).Returns(33);
            mockUserService.Setup(s => s.IsEndUser()).Returns(true);

            var controller = new TicketsController(context, mockUserService.Object, new NotificationService(context));
            controller.TempData = new Microsoft.AspNetCore.Mvc.ViewFeatures.TempDataDictionary(
                new Microsoft.AspNetCore.Http.DefaultHttpContext(),
                Mock.Of<Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataProvider>());

            var ticket = new Ticket
            {
                OpenedBy = 33,
                OnBehalfOf = 33,
                TargetTeam = MaintenanceTeams.IT,
                Type = "OS & Software Crash",
                AffectedSoftware = "HIS - Hospital Information System (EHR)",
                PcName = "MED-WORKSTATION-07",
                Priority = 3
            };

            string errorLogText = "[2026-11-03 09:30:11] [ERROR] [Application Error] (EventID: 1000)\n  Faulting application: his_client.exe version 4.5.1";
            // Valid 1x1 PNG base64 DataURL
            string singlePngBase64 = "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==";
            string screenshotsJson = System.Text.Json.JsonSerializer.Serialize(new[] { singlePngBase64 });

            var result = await controller.Create(
                ticket: ticket,
                manualAsset: null,
                initialMessage: "HIS system freeze upon patient admitting",
                assignedTechId: null,
                pcErrorLog: errorLogText,
                pastedScreenshotsJson: screenshotsJson,
                screenshotFiles: null
            );

            // Assert Redirect to Details
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Details", redirectResult.ActionName);

            // Verify ticket fields
            var savedTicket = await context.Tickets.FirstOrDefaultAsync(t => t.OpenedBy == 33);
            Assert.NotNull(savedTicket);
            Assert.Equal("HIS - Hospital Information System (EHR)", savedTicket.AffectedSoftware);
            Assert.Equal("MED-WORKSTATION-07", savedTicket.PcName);

            // Verify message content includes the error log
            var savedMsg = await context.TicketMessages.FirstOrDefaultAsync(m => m.TicketId == savedTicket.Id);
            Assert.NotNull(savedMsg);
            Assert.Contains("[AFFECTED SOFTWARE]: HIS - Hospital Information System (EHR)", savedMsg.Msg);
            Assert.Contains("[WORKSTATION PC]: MED-WORKSTATION-07", savedMsg.Msg);
            Assert.Contains("[CLIENT WORKSTATION ERROR LOG]:", savedMsg.Msg);
            Assert.Contains("Faulting application: his_client.exe", savedMsg.Msg);

            // Verify screenshot attachment was created
            var savedAttachment = await context.TicketAttachments.FirstOrDefaultAsync(a => a.TicketId == savedTicket.Id);
            Assert.NotNull(savedAttachment);
            Assert.Equal("image/png", savedAttachment.FileType);
            Assert.Contains(".png", savedAttachment.FilePath);
        }

        [Fact]
        public async Task TicketsController_AddMessage_SavesPastedScreenshots()
        {
            using var context = CreateInMemoryDbContext();

            var user = new User { Id = 40, FullName = "Nurse Sarah", Role = SystemRoles.EndUser, IsActive = true };
            context.Users.Add(user);

            var ticket = new Ticket
            {
                Id = 205,
                OpenedBy = 40,
                TargetTeam = MaintenanceTeams.IT,
                Type = "OS & Software Crash",
                StatusId = 1
            };
            context.Tickets.Add(ticket);
            await context.SaveChangesAsync();

            var mockUserService = new Mock<ICurrentUserService>();
            mockUserService.Setup(s => s.GetUserId()).Returns(40);
            mockUserService.Setup(s => s.CanAccessTicket(It.IsAny<Ticket>())).Returns(true);

            var controller = new TicketsController(context, mockUserService.Object, new NotificationService(context));
            controller.TempData = new Microsoft.AspNetCore.Mvc.ViewFeatures.TempDataDictionary(
                new Microsoft.AspNetCore.Http.DefaultHttpContext(),
                Mock.Of<Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataProvider>());

            string singlePngBase64 = "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==";
            string screenshotsJson = System.Text.Json.JsonSerializer.Serialize(new[] { singlePngBase64 });

            var result = await controller.AddMessage(
                ticketId: 205,
                userId: 40,
                msg: "Pasted screenshot of error dialog box",
                pastedScreenshotsJson: screenshotsJson,
                screenshotFiles: null
            );

            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Details", redirectResult.ActionName);

            var attachment = await context.TicketAttachments.FirstOrDefaultAsync(a => a.TicketId == 205);
            Assert.NotNull(attachment);
            Assert.Equal(40, attachment.UploadedBy);
            Assert.NotNull(attachment.MessageId);
            Assert.Contains("/uploads/tickets/", attachment.FilePath);

            var savedMsg = await context.TicketMessages.Include(m => m.Attachments).FirstOrDefaultAsync(m => m.TicketId == 205);
            Assert.NotNull(savedMsg);
            Assert.Single(savedMsg.Attachments);
        }

        [Fact]
        public async Task AssetsController_UpdateInstalledSoftwares_UpdatesAndPersistsSoftwareCatalog()
        {
            using var context = CreateInMemoryDbContext();

            var pcAsset = new Asset
            {
                Id = 15,
                HospitalNo = "HOSP-PC-015",
                Brand = "Dell",
                Type = "Desktop Workstation",
                MaintenanceTeam = MaintenanceTeams.IT,
                InstalledSoftwares = "[]"
            };
            context.Assets.Add(pcAsset);
            await context.SaveChangesAsync();

            var mockUserService = new Mock<ICurrentUserService>();
            mockUserService.Setup(s => s.GetUserId()).Returns(1);
            mockUserService.Setup(s => s.CanManageAsset(It.IsAny<Asset>())).Returns(true);

            var controller = new AssetsController(context, mockUserService.Object);
            controller.TempData = new Microsoft.AspNetCore.Mvc.ViewFeatures.TempDataDictionary(
                new Microsoft.AspNetCore.Http.DefaultHttpContext(),
                Mock.Of<Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataProvider>());

            var selectedSoftwares = new[]
            {
                "HIS - Hospital Information System (EHR)",
                "PACS - Radiology DICOM Viewer",
                "Microsoft Office 365"
            };

            var result = await controller.UpdateInstalledSoftwares(15, selectedSoftwares);
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Details", redirectResult.ActionName);

            var updatedAsset = await context.Assets.FindAsync(15);
            Assert.NotNull(updatedAsset);
            Assert.Equal(3, updatedAsset.InstalledSoftwaresList.Count);
            Assert.Contains("HIS - Hospital Information System (EHR)", updatedAsset.InstalledSoftwaresList);
            Assert.Contains("PACS - Radiology DICOM Viewer", updatedAsset.InstalledSoftwaresList);
        }

        // ==========================================================
        // Task: Closed Tickets Management, Analytics & Multi-Tech Closure
        // ==========================================================
        [Fact]
        public async Task TicketsController_CloseTicket_MultiTechnicians_PersistsAllParticipatingTechs()
        {
            using var context = CreateInMemoryDbContext();

            var tech1 = new User { Id = 101, FullName = "Tech Alice", Role = SystemRoles.Maintenance, MaintenanceTeam = "IT", IsActive = true };
            var tech2 = new User { Id = 102, FullName = "Tech Bob", Role = SystemRoles.Maintenance, MaintenanceTeam = "IT", IsActive = true };
            var tech3 = new User { Id = 103, FullName = "Tech Charlie", Role = SystemRoles.Maintenance, MaintenanceTeam = "IT", IsActive = true };
            context.Users.AddRange(tech1, tech2, tech3);

            var ticket = new Ticket
            {
                Id = 301,
                TargetTeam = "IT",
                Type = "Hardware",
                StatusId = 1,
                OpenedDT = DateTime.Now.AddHours(-3)
            };
            context.Tickets.Add(ticket);
            await context.SaveChangesAsync();

            var mockUserService = new Mock<ICurrentUserService>();
            mockUserService.Setup(s => s.GetUserId()).Returns(101);
            mockUserService.Setup(s => s.GetCurrentUser()).Returns(tech1);
            mockUserService.Setup(s => s.CanManageTicket(It.IsAny<Ticket>())).Returns(true);

            var mockNotifService = new Mock<INotificationService>();

            var controller = new TicketsController(
                context,
                mockUserService.Object,
                mockNotifService.Object
            );
            controller.TempData = new Microsoft.AspNetCore.Mvc.ViewFeatures.TempDataDictionary(
                new Microsoft.AspNetCore.Http.DefaultHttpContext(),
                Mock.Of<Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataProvider>());

            var participatingTechIds = new[] { 101, 102, 103 };
            var result = await controller.CloseTicket(301, "Replaced motherboard and updated BIOS firmware", participatingTechIds);

            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Details", redirectResult.ActionName);

            var updatedTicket = await context.Tickets.FindAsync(301);
            Assert.NotNull(updatedTicket);
            Assert.Equal(5, updatedTicket.StatusId);
            Assert.NotNull(updatedTicket.ClosedDT);
            Assert.Equal("Replaced motherboard and updated BIOS firmware", updatedTicket.ResolutionDetails);
            Assert.Equal(3, updatedTicket.ClosedByUserIds.Count);
            Assert.Contains(101, updatedTicket.ClosedByUserIds);
            Assert.Contains(102, updatedTicket.ClosedByUserIds);
            Assert.Contains(103, updatedTicket.ClosedByUserIds);

            var timelineMsg = await context.TicketMessages.FirstOrDefaultAsync(m => m.TicketId == 301);
            Assert.NotNull(timelineMsg);
            Assert.Contains("Replaced motherboard and updated BIOS firmware", timelineMsg.Msg);
            Assert.Contains("Tech Alice", timelineMsg.Msg);
            Assert.Contains("Tech Bob", timelineMsg.Msg);
            Assert.Contains("Tech Charlie", timelineMsg.Msg);
        }

        [Fact]
        public void CurrentUserService_CanAccessDashboard_GrantsAccessToAdminAndAllMaintenanceTeams()
        {
            var httpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext();
            var mockAccessor = new Mock<Microsoft.AspNetCore.Http.IHttpContextAccessor>();
            mockAccessor.Setup(a => a.HttpContext).Returns(httpContext);

            var service = new CurrentUserService(mockAccessor.Object);

            // EndUser
            var endUser = new User { Role = SystemRoles.EndUser };
            httpContext.Items["CurrentUser"] = endUser;
            Assert.False(service.CanAccessDashboard());

            // Admin
            var adminUser = new User { Role = SystemRoles.Admin };
            httpContext.Items["CurrentUser"] = adminUser;
            Assert.True(service.CanAccessDashboard());

            // Maintenance (IT)
            var maintIt = new User { Role = SystemRoles.Maintenance, MaintenanceTeam = "IT" };
            httpContext.Items["CurrentUser"] = maintIt;
            Assert.True(service.CanAccessDashboard());

            // Maintenance (Biomedical)
            var maintBio = new User { Role = SystemRoles.Maintenance, MaintenanceTeam = "BIOMEDICAL" };
            httpContext.Items["CurrentUser"] = maintBio;
            Assert.True(service.CanAccessDashboard());

            // Maintenance (General)
            var maintGen = new User { Role = SystemRoles.Maintenance, MaintenanceTeam = "GENERAL" };
            httpContext.Items["CurrentUser"] = maintGen;
            Assert.True(service.CanAccessDashboard());
        }

        [Fact]
        public async Task HomeController_Index_MaintenanceAnalytics_CreditsAllCollaboratingTechnicians()
        {
            using var context = CreateInMemoryDbContext();

            var tech1 = new User { Id = 201, FullName = "Lead Engineer Dave", Role = SystemRoles.Maintenance, MaintenanceTeam = "IT", IsActive = true };
            var tech2 = new User { Id = 202, FullName = "Technician Eve", Role = SystemRoles.Maintenance, MaintenanceTeam = "IT", IsActive = true };
            context.Users.AddRange(tech1, tech2);

            var closedTicket = new Ticket
            {
                Id = 401,
                TargetTeam = "IT",
                Type = "Hardware",
                StatusId = 5,
                OpenedDT = DateTime.Today.AddHours(2),
                ClosedDT = DateTime.Today.AddHours(10),
                ResolutionDetails = "Fixed PSU and replaced wiring",
                ClosedByUsers = "[201,202]"
            };
            context.Tickets.Add(closedTicket);
            await context.SaveChangesAsync();

            var mockUserService = new Mock<ICurrentUserService>();
            mockUserService.Setup(s => s.GetUserId()).Returns(201);
            mockUserService.Setup(s => s.CanAccessDashboard()).Returns(true);

            var controller = new HomeController(context, mockUserService.Object);

            var result = await controller.Index(datePreset: "today", tab: "analytics");
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<DashboardViewModel>(viewResult.Model);

            Assert.Equal("analytics", model.ActiveTab);
            Assert.Equal(1, model.FilteredClosedTickets);

            var daveStat = model.TechnicianStats.FirstOrDefault(s => s.UserId == 201);
            var eveStat = model.TechnicianStats.FirstOrDefault(s => s.UserId == 202);

            Assert.NotNull(daveStat);
            Assert.NotNull(eveStat);
            Assert.Equal(1, daveStat.ClosedTicketsCount);
            Assert.Equal(1, eveStat.ClosedTicketsCount);
            Assert.Equal(8.0, daveStat.AverageResolutionHours);
            Assert.Equal(8.0, eveStat.AverageResolutionHours);

            Assert.Single(model.FilteredClosedTicketsLog);
            var logItem = model.FilteredClosedTicketsLog[0];
            Assert.Equal(401, logItem.TicketId);
            Assert.Equal(2, logItem.ResolvedByUsers.Count);
        }

        [Fact]
        public async Task HomeController_Index_DateAndTeamFiltering_FiltersMetricsCorrectly()
        {
            using var context = CreateInMemoryDbContext();

            var tech = new User { Id = 501, FullName = "Bio Engineer", Role = SystemRoles.Maintenance, MaintenanceTeam = "BIOMEDICAL", IsActive = true };
            context.Users.Add(tech);

            // Ticket 1: IT ticket closed 45 days ago (outside 30-day window)
            context.Tickets.Add(new Ticket
            {
                Id = 501,
                TargetTeam = "IT",
                Type = "Hardware",
                StatusId = 5,
                OpenedDT = DateTime.Today.AddDays(-50),
                ClosedDT = DateTime.Today.AddDays(-45),
                ClosedByUsers = "[501]"
            });

            // Ticket 2: Biomedical ticket closed 5 days ago (inside 30-day window)
            context.Tickets.Add(new Ticket
            {
                Id = 502,
                TargetTeam = "BIOMEDICAL",
                Type = "Medical Device",
                StatusId = 5,
                OpenedDT = DateTime.Today.AddDays(-6),
                ClosedDT = DateTime.Today.AddDays(-5),
                ClosedByUsers = "[501]"
            });

            await context.SaveChangesAsync();

            var mockUserService = new Mock<ICurrentUserService>();
            var controller = new HomeController(context, mockUserService.Object);

            // Query with datePreset = "30days" and teamFilter = "BIOMEDICAL"
            var result = await controller.Index(teamFilter: "BIOMEDICAL", datePreset: "30days", tab: "analytics");
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<DashboardViewModel>(viewResult.Model);

            Assert.Equal(1, model.FilteredClosedTickets);
            Assert.Single(model.FilteredClosedTicketsLog);
            Assert.Equal(502, model.FilteredClosedTicketsLog[0].TicketId);
            Assert.Equal("BIOMEDICAL", model.FilteredClosedTicketsLog[0].TargetTeam);
        }
    }
}
