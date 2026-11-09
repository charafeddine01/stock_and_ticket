using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
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
    public class HospitalMaintenanceEnhancementTests
    {
        private StockDbContext CreateInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<StockDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            return new StockDbContext(options);
        }

        private Mock<ICurrentUserService> CreateMockUserService(int userId = 1, string role = SystemRoles.Admin, string team = "IT")
        {
            var mock = new Mock<ICurrentUserService>();
            mock.Setup(s => s.GetUserId()).Returns(userId);
            mock.Setup(s => s.GetRole()).Returns(role);
            mock.Setup(s => s.GetMaintenanceTeam()).Returns(team);
            mock.Setup(s => s.IsAdmin()).Returns(role == SystemRoles.Admin);
            mock.Setup(s => s.IsMaintenance()).Returns(role == SystemRoles.Maintenance || role == SystemRoles.Admin);
            mock.Setup(s => s.IsEndUser()).Returns(role == SystemRoles.EndUser);
            mock.Setup(s => s.CanManageAssets()).Returns(role == SystemRoles.Admin || role == SystemRoles.Maintenance);
            mock.Setup(s => s.CanManageAsset(It.IsAny<Asset>())).Returns(true);
            mock.Setup(s => s.CanAccessAsset(It.IsAny<Asset>())).Returns(true);
            mock.Setup(s => s.CanManageTicket(It.IsAny<Ticket>())).Returns(true);
            mock.Setup(s => s.CanCreateAssetInDepartment(It.IsAny<string>())).Returns(true);
            mock.Setup(s => s.GetCurrentUser()).Returns(new User { Id = userId, Role = role, MaintenanceTeam = team, FullName = "Admin User" });
            return mock;
        }

        private T InitController<T>(T controller) where T : Controller
        {
            var httpContext = new DefaultHttpContext();
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };
            controller.TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());
            return controller;
        }

        // ==========================================
        // Task 1 & 2: Spare Part Forms and Compatibility
        // ==========================================
        [Fact]
        public async Task Task1_SparePartForm_CanBeCreated_AndUsedBySparePart()
        {
            using var db = CreateInMemoryDbContext();
            var userService = CreateMockUserService();
            var controller = InitController(new SparePartFormsController(db, userService.Object));

            var form = new SparePartForm
            {
                Name = "Toner Cartridge Template",
                Department = MaintenanceTeams.IT,
                Form = JsonSerializer.Serialize(new[]
                {
                    new FormFieldDefinition { Name = "color", Label = "Print Color", Type = "text", Required = true },
                    new FormFieldDefinition { Name = "yield_pages", Label = "Page Yield", Type = "number", Required = false }
                })
            };

            var result = await controller.Create(form);
            Assert.IsType<RedirectToActionResult>(result);

            var savedForm = await db.SparePartForms.FirstOrDefaultAsync(f => f.Name == "Toner Cartridge Template");
            Assert.NotNull(savedForm);
            Assert.Equal(2, savedForm.Fields.Count);

            // Add spare part with specs
            var part = new SparePart
            {
                Name = "HP 85A Black",
                PartNumber = "CE285A",
                Department = MaintenanceTeams.IT,
                FormId = savedForm.Id,
                Specs = JsonSerializer.Serialize(new Dictionary<string, string> { { "color", "Black" }, { "yield_pages", "1600" } }),
                Quantity = 10,
                MinReserveQuantity = 2
            };
            db.SpareParts.Add(part);
            await db.SaveChangesAsync();

            var loadedPart = await db.SpareParts.FindAsync(part.Id);
            Assert.NotNull(loadedPart);
            Assert.Equal("Black", loadedPart.SpecsDictionary["color"]);
        }

        [Fact]
        public async Task Task2_AssetAndSparePart_BidirectionalCompatibility_Maintained()
        {
            using var db = CreateInMemoryDbContext();

            var asset1 = new Asset { Id = 1, HospitalNo = "PRN-001", Brand = "HP LaserJet", Type = "Printer", MaintenanceTeam = MaintenanceTeams.IT };
            var asset2 = new Asset { Id = 2, HospitalNo = "PRN-002", Brand = "Canon ImageClass", Type = "Printer", MaintenanceTeam = MaintenanceTeams.IT };
            var part = new SparePart { Id = 10, Name = "Universal Roller Kit", Department = MaintenanceTeams.IT, Quantity = 5 };

            db.Assets.AddRange(asset1, asset2);
            db.SpareParts.Add(part);
            await db.SaveChangesAsync();

            // Link part to both assets
            db.AssetCompatibleSpareParts.AddRange(
                new AssetCompatibleSparePart { AssetId = asset1.Id, SparePartId = part.Id },
                new AssetCompatibleSparePart { AssetId = asset2.Id, SparePartId = part.Id }
            );
            await db.SaveChangesAsync();

            // Check from part side
            var compatibleAssets = await db.AssetCompatibleSpareParts
                .Where(c => c.SparePartId == part.Id)
                .Select(c => c.AssetId)
                .ToListAsync();
            Assert.Equal(2, compatibleAssets.Count);
            Assert.Contains(1, compatibleAssets);
            Assert.Contains(2, compatibleAssets);

            // Check from asset side
            var compatiblePartsForAsset1 = await db.AssetCompatibleSpareParts
                .Where(c => c.AssetId == asset1.Id)
                .Select(c => c.SparePartId)
                .ToListAsync();
            Assert.Single(compatiblePartsForAsset1);
            Assert.Equal(10, compatiblePartsForAsset1[0]);
        }

        // ==========================================
        // Task 3 & 4: Vendor Dispatch and Return
        // ==========================================
        [Fact]
        public async Task Task3_VendorReturn_LogsPartsChanged_WithoutCost()
        {
            using var db = CreateInMemoryDbContext();
            var userService = CreateMockUserService();
            var controller = InitController(new MaintenanceController(db, userService.Object));

            var asset = new Asset { Id = 5, HospitalNo = "BIO-DEFIB-01", Status = "Under Maintenance", CustodyTypeId = 3 };
            var supplier = new Supplier { Id = 10, Name = "MedTech Services", IsActive = true };
            var workOrder = new AssetMaintenance
            {
                Id = 1,
                AssetId = asset.Id,
                SupplierId = supplier.Id,
                MaintenanceType = 1,
                WorkOrderStatus = "Under Maintenance",
                SentDate = DateTime.Now.AddDays(-5)
            };

            db.Assets.Add(asset);
            db.Suppliers.Add(supplier);
            db.AssetMaintenances.Add(workOrder);
            await db.SaveChangesAsync();

            var replacedParts = JsonSerializer.Serialize(new[]
            {
                new { PartName = "Battery Pack", Quantity = 1, PartNumber = "BP-100", WarrantyMonths = 12 },
                new { PartName = "Paddle Cable", Quantity = 1, PartNumber = "PC-200", WarrantyMonths = 6 }
            });

            var result = await controller.ReturnFromSupplier(
                id: workOrder.Id,
                returnDate: DateTime.Now,
                actionTaken: "Replaced battery pack and internal defibrillator paddle cables.",
                partsJson: replacedParts,
                returnDestination: "central_stock",
                newUserId: null,
                newRoomId: null,
                cost: 999m // Even if passed, cost must not be assigned to workOrder per Task 9
            );

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal(nameof(MaintenanceController.WorkOrderDetails), redirect.ActionName);

            var updatedWorkOrder = await db.AssetMaintenances.FindAsync(workOrder.Id);
            Assert.NotNull(updatedWorkOrder);
            Assert.Equal("Completed", updatedWorkOrder.WorkOrderStatus);
            Assert.Contains("Battery Pack", updatedWorkOrder.PartsReplaced ?? "");
            Assert.Contains("Paddle Cable", updatedWorkOrder.PartsReplaced ?? "");
            Assert.Null(updatedWorkOrder.Cost); // Cost excluded per Task 9
        }

        [Fact]
        public async Task Task4_VendorDispatchForm_ReturnsValidViewWithAssetHistory()
        {
            using var db = CreateInMemoryDbContext();
            var userService = CreateMockUserService();
            var controller = InitController(new MaintenanceController(db, userService.Object));

            var asset = new Asset { Id = 7, HospitalNo = "IT-SVR-01", Brand = "Dell PowerEdge", SerialNo = "SRV12345" };
            var supplier = new Supplier { Id = 2, Name = "Dell Official Partner" };
            var wo = new AssetMaintenance
            {
                Id = 15,
                AssetId = asset.Id,
                SupplierId = supplier.Id,
                WorkOrderStatus = "Under Maintenance",
                SentDate = DateTime.Now,
                Description = "RAID Controller Failure"
            };

            db.Assets.Add(asset);
            db.Suppliers.Add(supplier);
            db.AssetMaintenances.Add(wo);
            await db.SaveChangesAsync();

            var result = await controller.VendorDispatchForm(wo.Id);
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.NotNull(viewResult.Model);
            Assert.Equal(wo.Id, ((AssetMaintenance)viewResult.Model).Id);
        }

        // ==========================================
        // Task 5: Vendor Free Maintenance Management
        // ==========================================
        [Fact]
        public async Task Task5_RecordVendorMaintenanceVisit_IncrementsCount_AndRecordsVisitLog()
        {
            using var db = CreateInMemoryDbContext();
            var userService = CreateMockUserService();
            var controller = InitController(new AssetsController(db, userService.Object));

            var asset = new Asset
            {
                Id = 20,
                HospitalNo = "XRAY-01",
                Brand = "Siemens Healthineers",
                HasVendorMaintenance = true,
                VendorMaintenanceCount = 4,
                VendorMaintenanceCompletedCount = 0,
                VendorMaintenanceStartDate = DateTime.Now.AddMonths(-6),
                VendorMaintenanceEndDate = DateTime.Now.AddMonths(6)
            };
            db.Assets.Add(asset);
            await db.SaveChangesAsync();

            var result = await controller.RecordVendorMaintenanceVisit(
                assetId: 20,
                visitDate: DateTime.Now,
                engineerName: "Siemens Engineer John",
                notes: "Replaced tube coolant and calibrated detector."
            );

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal(nameof(AssetsController.Details), redirect.ActionName);

            var updatedAsset = await db.Assets.FindAsync(20);
            Assert.NotNull(updatedAsset);
            Assert.Equal(1, updatedAsset.VendorMaintenanceCompletedCount);
            Assert.NotEmpty(updatedAsset.VendorVisitsList);
            Assert.Equal("Siemens Engineer John", updatedAsset.VendorVisitsList.Last().EngineerName);
            Assert.Equal("Replaced tube coolant and calibrated detector.", updatedAsset.VendorVisitsList.Last().Notes);
        }

        // ==========================================
        // Task 6: Predefined Asset Categories
        // ==========================================
        [Fact]
        public async Task Task6_AssetCategories_CanBeDefinedAndAssignedToAsset()
        {
            using var db = CreateInMemoryDbContext();
            var userService = CreateMockUserService();
            var catController = InitController(new AssetCategoriesController(db, userService.Object));

            var category = new AssetCategory
            {
                Name = "Printer",
                NameAr = "طابعات",
                Department = MaintenanceTeams.IT,
                Description = "Office network and desktop printers"
            };

            var catResult = await catController.Create(category);
            Assert.IsType<RedirectToActionResult>(catResult);

            var savedCat = await db.AssetCategories.FirstOrDefaultAsync(c => c.Name == "Printer");
            Assert.NotNull(savedCat);

            // Assign category to asset
            var asset = new Asset
            {
                HospitalNo = "HOSP-PRN-05",
                Brand = "Epson L3150",
                CategoryId = savedCat.Id,
                MaintenanceTeam = MaintenanceTeams.IT
            };
            db.Assets.Add(asset);
            await db.SaveChangesAsync();

            var loadedAsset = await db.Assets.Include(a => a.Category).FirstOrDefaultAsync(a => a.HospitalNo == "HOSP-PRN-05");
            Assert.NotNull(loadedAsset?.Category);
            Assert.Equal("Printer", loadedAsset.Category.Name);
        }

        // ==========================================
        // Task 7: Calibration Protocols
        // ==========================================
        [Fact]
        public async Task Task7_LogCalibrationProtocol_SavesJsonProtocolValuesOnAsset()
        {
            using var db = CreateInMemoryDbContext();
            var userService = CreateMockUserService();
            var controller = InitController(new AssetsController(db, userService.Object));

            var protocol = new CalibrationForm
            {
                Id = 1,
                Name = "Infusion Pump Rate Verification",
                Department = MaintenanceTeams.Biomedical,
                Form = JsonSerializer.Serialize(new[]
                {
                    new FormFieldDefinition { Name = "set_rate", Label = "Set Flow Rate (mL/h)", Type = "number", Required = true },
                    new FormFieldDefinition { Name = "measured_rate", Label = "Measured Rate (mL/h)", Type = "number", Required = true },
                    new FormFieldDefinition { Name = "deviation_pct", Label = "Deviation (%)", Type = "number", Required = false }
                })
            };

            var asset = new Asset
            {
                Id = 50,
                HospitalNo = "INF-PUMP-01",
                Brand = "B. Braun",
                MaintenanceTeam = MaintenanceTeams.Biomedical,
                NeedsCalibration = true
            };

            db.CalibrationForms.Add(protocol);
            db.Assets.Add(asset);
            await db.SaveChangesAsync();

            var formCollection = new FormCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
            {
                { "calib_set_rate", "100" },
                { "calib_measured_rate", "99.8" },
                { "calib_deviation_pct", "-0.2" }
            });

            var result = await controller.LogCalibrationProtocol(
                assetId: 50,
                calibrationFormId: protocol.Id,
                calibrationDate: DateTime.Now,
                intervalDays: 365,
                certNo: "CERT-001",
                calibratedBy: "Biomedical Specialist Ahmed",
                notes: "Calibrated",
                formCollection: formCollection
            );

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal(nameof(AssetsController.Details), redirect.ActionName);

            var updatedAsset = await db.Assets.FindAsync(50);
            Assert.NotNull(updatedAsset);
            Assert.NotNull(updatedAsset.LastCalibration);
            Assert.Contains("Infusion Pump Rate Verification", updatedAsset.LastCalibrationData ?? "");
            Assert.Contains("99.8", updatedAsset.LastCalibrationData ?? "");
        }

        // ==========================================
        // Task 8: Checklists Management & Printable Table
        // ==========================================
        [Fact]
        public async Task Task8_Checklist_Create_Print_AndMarkDone()
        {
            using var db = CreateInMemoryDbContext();
            var userService = CreateMockUserService();
            var checklistsController = InitController(new ChecklistsController(db, userService.Object));
            var assetsController = InitController(new AssetsController(db, userService.Object));

            var checklist = new ChecklistForm
            {
                Title = "Annual Electrical Safety Inspection",
                Department = MaintenanceTeams.Biomedical,
                ChecklistType = "Safety Inspection"
            };

            var items = new[]
            {
                "Chassis Ground Resistance < 0.2 Ohms",
                "Earth Leakage Current < 300 uA",
                "Visual Check of Power Cord & Strain Relief"
            };

            var createResult = await checklistsController.Create(checklist, items);
            Assert.IsType<RedirectToActionResult>(createResult);

            var savedChecklist = await db.ChecklistForms.FirstOrDefaultAsync(c => c.Title == "Annual Electrical Safety Inspection");
            Assert.NotNull(savedChecklist);
            Assert.Equal(3, savedChecklist.Items.Count);

            // Asset
            var asset = new Asset { Id = 70, HospitalNo = "MON-01", MaintenanceTeam = MaintenanceTeams.Biomedical };
            db.Assets.Add(asset);
            await db.SaveChangesAsync();

            // Print Checklist Action
            var printResult = await checklistsController.PrintChecklist(id: savedChecklist.Id, selectedAssetIds: new[] { 70 });
            var printView = Assert.IsType<ViewResult>(printResult);
            Assert.NotNull(printView.Model);

            // Mark checklist done on asset
            var doneResult = await assetsController.MarkChecklistDone(
                assetId: 70,
                checklistTitle: savedChecklist.Title,
                inspectorName: "Engineer Sarah",
                notes: "All safety tests within hospital electrical standards."
            );
            Assert.IsType<RedirectToActionResult>(doneResult);

            var updatedAsset = await db.Assets.FindAsync(70);
            Assert.NotNull(updatedAsset);
            Assert.NotNull(updatedAsset.LastChecklistDate);
            Assert.Equal("Engineer Sarah", updatedAsset.LastChecklistBy);
        }

        // ==========================================
        // Task 10: Automatic Internal Maintenance Record
        // ==========================================
        [Fact]
        public async Task Task10_CloseTicket_AutomaticallyRecordsAssetMaintenanceEntry()
        {
            using var db = CreateInMemoryDbContext();
            var userService = CreateMockUserService(userId: 1, role: SystemRoles.Admin, team: "IT");
            var notifMock = new Mock<INotificationService>();
            var controller = InitController(new TicketsController(db, userService.Object, notifMock.Object));

            var asset = new Asset { Id = 80, HospitalNo = "PC-LAB-01", Brand = "HP ProDesk", MaintenanceTeam = MaintenanceTeams.IT };
            var tech = new User { Id = 1, FullName = "IT Specialist", Role = SystemRoles.Maintenance, MaintenanceTeam = "IT", IsActive = true };
            var ticket = new Ticket
            {
                Id = 101,
                AssetId = asset.Id,
                Type = "Hardware Breakdown",
                TargetTeam = MaintenanceTeams.IT,
                StatusId = 2,
                OpenedDT = DateTime.Now.AddDays(-1)
            };

            db.Assets.Add(asset);
            db.Users.Add(tech);
            db.Tickets.Add(ticket);
            await db.SaveChangesAsync();

            var result = await controller.CloseTicket(
                ticketId: 101,
                resolutionNotes: "Replaced 400W PSU and tested power rails.",
                closedByUserIds: new[] { 1 }
            );

            Assert.IsType<RedirectToActionResult>(result);

            // AssetMaintenance must be automatically created
            var maintRecord = await db.AssetMaintenances.FirstOrDefaultAsync(m => m.TicketId == 101);
            Assert.NotNull(maintRecord);
            Assert.Equal(asset.Id, maintRecord.AssetId);
            Assert.Equal(1, maintRecord.MaintenanceType); // Corrective internal
            Assert.Equal("Completed", maintRecord.WorkOrderStatus);
            Assert.Contains("Replaced 400W PSU", maintRecord.ActionTaken ?? "");
            Assert.Null(maintRecord.SupplierId); // In-house / internal
        }

        // ==========================================
        // Task 11: Single Active Ticket Rule Per Asset
        // ==========================================
        [Fact]
        public async Task Task11_CreateTicket_BlocksWhenAssetAlreadyHasActiveTicket()
        {
            using var db = CreateInMemoryDbContext();
            var userService = CreateMockUserService(userId: 5, role: SystemRoles.EndUser, team: "IT");
            var notifMock = new Mock<INotificationService>();
            var controller = InitController(new TicketsController(db, userService.Object, notifMock.Object));

            var asset = new Asset { Id = 90, HospitalNo = "LAP-NURSE-01", MaintenanceTeam = MaintenanceTeams.IT };
            var activeTicket = new Ticket
            {
                Id = 201,
                AssetId = asset.Id,
                StatusId = 1, // Opened (active)
                TargetTeam = MaintenanceTeams.IT
            };

            db.Assets.Add(asset);
            db.Tickets.Add(activeTicket);
            await db.SaveChangesAsync();

            // Attempt to open another ticket for the same asset
            var newTicket = new Ticket
            {
                AssetId = asset.Id,
                Type = "Hardware Breakdown",
                TargetTeam = MaintenanceTeams.IT
            };

            var result = await controller.Create(newTicket);
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.False(controller.ModelState.IsValid);
            Assert.True(controller.ModelState.ContainsKey("AssetId"));
            Assert.NotNull(controller.ViewBag.ActiveTicketWarning);

            // Verify second ticket was NOT persisted
            var totalTicketsForAsset = await db.Tickets.CountAsync(t => t.AssetId == asset.Id);
            Assert.Equal(1, totalTicketsForAsset);
        }

        [Fact]
        public async Task Task11_CreateTicket_AllowsWhenPreviousTicketIsClosedOrCanceled()
        {
            using var db = CreateInMemoryDbContext();
            var userService = CreateMockUserService(userId: 5, role: SystemRoles.EndUser, team: "IT");
            var notifMock = new Mock<INotificationService>();
            var controller = InitController(new TicketsController(db, userService.Object, notifMock.Object));

            var asset = new Asset { Id = 95, HospitalNo = "LAP-NURSE-02", MaintenanceTeam = MaintenanceTeams.IT };
            var closedTicket = new Ticket
            {
                Id = 301,
                AssetId = asset.Id,
                StatusId = 5, // Closed
                TargetTeam = MaintenanceTeams.IT
            };

            db.Assets.Add(asset);
            db.Tickets.Add(closedTicket);
            await db.SaveChangesAsync();

            // Opening a new ticket when the previous one is closed should succeed
            var newTicket = new Ticket
            {
                AssetId = asset.Id,
                Type = "Printer & Peripherals",
                TargetTeam = MaintenanceTeams.IT
            };

            var result = await controller.Create(newTicket);
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Details", redirect.ActionName);

            var totalTicketsForAsset = await db.Tickets.CountAsync(t => t.AssetId == asset.Id);
            Assert.Equal(2, totalTicketsForAsset);
        }

        // ==========================================
        // Task 12: Shared Closing Restriction to Same Department
        // ==========================================
        [Fact]
        public async Task Task12_CloseTicket_ExcludesTechniciansFromOtherDepartments()
        {
            using var db = CreateInMemoryDbContext();
            var userService = CreateMockUserService(userId: 1, role: SystemRoles.Admin, team: "IT");
            var notifMock = new Mock<INotificationService>();
            var controller = InitController(new TicketsController(db, userService.Object, notifMock.Object));

            var itTech = new User { Id = 10, FullName = "IT Tech", Role = SystemRoles.Maintenance, MaintenanceTeam = "IT", IsActive = true };
            var bioTech = new User { Id = 20, FullName = "Biomedical Tech", Role = SystemRoles.Maintenance, MaintenanceTeam = "BIOMEDICAL", IsActive = true };

            var itTicket = new Ticket
            {
                Id = 401,
                TargetTeam = MaintenanceTeams.IT,
                StatusId = 2,
                OpenedDT = DateTime.Now
            };

            db.Users.AddRange(itTech, bioTech);
            db.Tickets.Add(itTicket);
            await db.SaveChangesAsync();

            // Attempt to close IT ticket with both IT tech (Id: 10) and Biomedical tech (Id: 20)
            var result = await controller.CloseTicket(
                ticketId: 401,
                resolutionNotes: "Fixed switch port.",
                closedByUserIds: new[] { 10, 20 }
            );

            Assert.IsType<RedirectToActionResult>(result);

            var closedTicket = await db.Tickets.FindAsync(401);
            Assert.NotNull(closedTicket);
            Assert.Equal(5, closedTicket.StatusId);

            // Biomedical tech (20) must be stripped out; only IT tech (10) allowed
            Assert.Contains(10, closedTicket.ClosedByUserIds);
            Assert.DoesNotContain(20, closedTicket.ClosedByUserIds);
        }
    }
}
