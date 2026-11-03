using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
    public class CalibrationAndTagTests
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
                context.Users.Add(new User { Id = 1, DomainUser = "DOMAIN\\admin", FullName = "System Admin", Role = SystemRoles.Admin });
            }

            context.SaveChanges();
            return context;
        }

        private Mock<ICurrentUserService> CreateMockUserService(string role = SystemRoles.Admin, string? team = "BIOMEDICAL")
        {
            var mock = new Mock<ICurrentUserService>();
            mock.Setup(s => s.GetRole()).Returns(role);
            mock.Setup(s => s.GetMaintenanceTeam()).Returns(team);
            mock.Setup(s => s.GetUserId()).Returns(1);
            mock.Setup(s => s.IsAdmin()).Returns(role == SystemRoles.Admin);
            mock.Setup(s => s.IsMaintenance()).Returns(role == SystemRoles.Maintenance);
            mock.Setup(s => s.IsEndUser()).Returns(role == SystemRoles.EndUser);
            mock.Setup(s => s.CanManageAssets()).Returns(true);
            mock.Setup(s => s.CanManageAsset(It.IsAny<Asset>())).Returns(true);
            mock.Setup(s => s.CanAccessAsset(It.IsAny<Asset>())).Returns(true);
            return mock;
        }

        [Fact]
        public void Asset_Calibration_ComputedProperties_WorkCorrectly()
        {
            var today = DateTime.Now.Date;

            // 1. Asset without calibration needed
            var nonCalibAsset = new Asset
            {
                HospitalNo = "IT-001",
                NeedsCalibration = false
            };
            Assert.False(nonCalibAsset.IsCalibrationOverdue);
            Assert.False(nonCalibAsset.IsCalibrationDueSoon);
            Assert.Equal("Not Required", nonCalibAsset.CalibrationStatus);

            // 2. Asset with calibration overdue
            var overdueAsset = new Asset
            {
                HospitalNo = "MED-001",
                NeedsCalibration = true,
                CalibrationInterval = 180,
                LastCalibration = today.AddDays(-200),
                NextCalibration = today.AddDays(-20)
            };
            Assert.True(overdueAsset.IsCalibrationOverdue);
            Assert.False(overdueAsset.IsCalibrationDueSoon);
            Assert.Equal("Overdue", overdueAsset.CalibrationStatus);

            // 3. Asset due soon (within 30 days)
            var dueSoonAsset = new Asset
            {
                HospitalNo = "MED-002",
                NeedsCalibration = true,
                CalibrationInterval = 365,
                LastCalibration = today.AddDays(-350),
                NextCalibration = today.AddDays(15)
            };
            Assert.False(dueSoonAsset.IsCalibrationOverdue);
            Assert.True(dueSoonAsset.IsCalibrationDueSoon);
            Assert.Equal("Due Soon", dueSoonAsset.CalibrationStatus);

            // 4. Asset valid (due in > 30 days)
            var validAsset = new Asset
            {
                HospitalNo = "MED-003",
                NeedsCalibration = true,
                CalibrationInterval = 365,
                LastCalibration = today.AddDays(-30),
                NextCalibration = today.AddDays(335)
            };
            Assert.False(validAsset.IsCalibrationOverdue);
            Assert.False(validAsset.IsCalibrationDueSoon);
            Assert.Equal("Calibrated & Valid", validAsset.CalibrationStatus);
        }

        [Fact]
        public async Task AssetsController_LogCalibration_UpdatesAssetAndLogsMaintenance()
        {
            var db = GetInMemoryDbContext(nameof(AssetsController_LogCalibration_UpdatesAssetAndLogsMaintenance));
            var asset = new Asset
            {
                Id = 10,
                HospitalNo = "US-10",
                Brand = "GE Healthcare",
                NeedsCalibration = true,
                CalibrationInterval = 180,
                LastCalibration = new DateTime(2025, 1, 1),
                NextCalibration = new DateTime(2025, 6, 30),
                CalibrationCertNo = "OLD-CERT-001",
                CalibratedBy = "Old Tech"
            };
            db.Assets.Add(asset);
            await db.SaveChangesAsync();

            var userMock = CreateMockUserService();
            var controller = new AssetsController(db, userMock.Object)
            {
                TempData = new Mock<ITempDataDictionary>().Object
            };

            var newCalibDate = new DateTime(2026, 3, 1);
            var result = await controller.LogCalibration(
                assetId: 10,
                calibrationDate: newCalibDate,
                intervalDays: 365,
                certNo: "CERT-2026-999",
                calibratedBy: "National Metrology Lab",
                notes: "Calibrated and certified for 1 year"
            );

            // Verify redirect back to asset details
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Details", redirectResult.ActionName);
            Assert.Equal(10, redirectResult.RouteValues?["id"]);

            // Verify asset updated
            var updatedAsset = await db.Assets.FindAsync(10);
            Assert.NotNull(updatedAsset);
            Assert.Equal(newCalibDate, updatedAsset.LastCalibration);
            Assert.Equal(365, updatedAsset.CalibrationInterval);
            Assert.Equal(new DateTime(2027, 3, 1), updatedAsset.NextCalibration);
            Assert.Equal("CERT-2026-999", updatedAsset.CalibrationCertNo);
            Assert.Equal("National Metrology Lab", updatedAsset.CalibratedBy);

            // Verify maintenance audit log created
            var maintLog = await db.AssetMaintenances.FirstOrDefaultAsync(m => m.AssetId == 10 && m.MaintenanceType == 2);
            Assert.NotNull(maintLog);
            Assert.Contains("CERT-2026-999", maintLog.Description);
            Assert.Contains("National Metrology Lab", maintLog.Description);
        }

        [Fact]
        public async Task AssetsController_Index_FiltersByCalibrationStatus()
        {
            var db = GetInMemoryDbContext(nameof(AssetsController_Index_FiltersByCalibrationStatus));
            var today = DateTime.Now.Date;

            db.Assets.AddRange(
                new Asset { Id = 1, HospitalNo = "A-REQ-OVERDUE", NeedsCalibration = true, NextCalibration = today.AddDays(-10) },
                new Asset { Id = 2, HospitalNo = "A-REQ-DUESOON", NeedsCalibration = true, NextCalibration = today.AddDays(10) },
                new Asset { Id = 3, HospitalNo = "A-REQ-VALID", NeedsCalibration = true, NextCalibration = today.AddDays(100) },
                new Asset { Id = 4, HospitalNo = "A-NON-CALIB", NeedsCalibration = false }
            );
            await db.SaveChangesAsync();

            var userMock = CreateMockUserService();
            var controller = new AssetsController(db, userMock.Object);

            // Test 1: Filter "required"
            var resultRequired = await controller.Index(calibration: "required") as ViewResult;
            Assert.NotNull(resultRequired);
            var modelRequired = Assert.IsAssignableFrom<IEnumerable<Asset>>(resultRequired.Model);
            Assert.Equal(3, modelRequired.Count());

            // Test 2: Filter "overdue"
            var resultOverdue = await controller.Index(calibration: "overdue") as ViewResult;
            Assert.NotNull(resultOverdue);
            var modelOverdue = Assert.IsAssignableFrom<IEnumerable<Asset>>(resultOverdue.Model);
            Assert.Single(modelOverdue);
            Assert.Equal("A-REQ-OVERDUE", modelOverdue.First().HospitalNo);

            // Test 3: Filter "due30"
            var resultDue30 = await controller.Index(calibration: "due30") as ViewResult;
            Assert.NotNull(resultDue30);
            var modelDue30 = Assert.IsAssignableFrom<IEnumerable<Asset>>(resultDue30.Model);
            Assert.Single(modelDue30);
            Assert.Equal("A-REQ-DUESOON", modelDue30.First().HospitalNo);

            // Test 4: Filter "valid"
            var resultValid = await controller.Index(calibration: "valid") as ViewResult;
            Assert.NotNull(resultValid);
            var modelValid = Assert.IsAssignableFrom<IEnumerable<Asset>>(resultValid.Model);
            Assert.Single(modelValid);
            Assert.Equal("A-REQ-VALID", modelValid.First().HospitalNo);
        }

        [Fact]
        public async Task AssetsController_PrintTag_And_PrintBulkTags_ReturnView()
        {
            var db = GetInMemoryDbContext(nameof(AssetsController_PrintTag_And_PrintBulkTags_ReturnView));
            var asset = new Asset
            {
                Id = 55,
                HospitalNo = "TAG-55",
                Brand = "Philips Ultrasound",
                NeedsCalibration = true,
                LastCalibration = DateTime.Now.AddDays(-10),
                NextCalibration = DateTime.Now.AddDays(170)
            };
            db.Assets.Add(asset);
            await db.SaveChangesAsync();

            var userMock = CreateMockUserService();
            var controller = new AssetsController(db, userMock.Object);

            // 1. Single PrintTag
            var singleTagResult = await controller.PrintTag(55) as ViewResult;
            Assert.NotNull(singleTagResult);
            var singleModel = Assert.IsType<Asset>(singleTagResult.Model);
            Assert.Equal("TAG-55", singleModel.HospitalNo);

            // 2. Bulk PrintBulkTags
            var bulkTagResult = await controller.PrintBulkTags() as ViewResult;
            Assert.NotNull(bulkTagResult);
            var bulkModel = Assert.IsAssignableFrom<IEnumerable<Asset>>(bulkTagResult.Model);
            Assert.Contains(bulkModel, a => a.HospitalNo == "TAG-55");
        }

        [Fact]
        public async Task AssetsController_PrintBulkTags_WithNoFilters_ReturnsAllAssets()
        {
            var db = GetInMemoryDbContext(nameof(AssetsController_PrintBulkTags_WithNoFilters_ReturnsAllAssets));
            db.Assets.AddRange(
                new Asset { Id = 101, HospitalNo = "TAG-101", Status = "In Service", Brand = "Dell", Type = "Workstation" },
                new Asset { Id = 102, HospitalNo = "TAG-102", Status = "Out of Service", Brand = "HP", Type = "Printer" },
                new Asset { Id = 103, HospitalNo = "TAG-103", Status = "Maintenance", Brand = "GE", Type = "ECG" }
            );
            await db.SaveChangesAsync();

            var userMock = CreateMockUserService();
            var controller = new AssetsController(db, userMock.Object);

            var result = await controller.PrintBulkTags() as ViewResult;
            Assert.NotNull(result);
            var model = Assert.IsAssignableFrom<IEnumerable<Asset>>(result.Model).ToList();
            Assert.Equal(3, model.Count);
        }

        [Fact]
        public async Task AssetsController_PrintBulkTags_WithFilters_ReturnsOnlyFilteredAssets()
        {
            var db = GetInMemoryDbContext(nameof(AssetsController_PrintBulkTags_WithFilters_ReturnsOnlyFilteredAssets));
            var today = DateTime.Now.Date;
            db.Assets.AddRange(
                new Asset { Id = 201, HospitalNo = "TAG-201", Status = "In Service", Brand = "Dell OptiPlex", Type = "Workstation", NeedsCalibration = true, NextCalibration = today.AddDays(-5) },
                new Asset { Id = 202, HospitalNo = "TAG-202", Status = "Out of Service", Brand = "HP LaserJet", Type = "Printer", NeedsCalibration = false },
                new Asset { Id = 203, HospitalNo = "TAG-203", Status = "In Service", Brand = "Lenovo ThinkCentre", Type = "Workstation", NeedsCalibration = true, NextCalibration = today.AddDays(15) }
            );
            await db.SaveChangesAsync();

            var userMock = CreateMockUserService();
            var controller = new AssetsController(db, userMock.Object);

            // Test 1: Filter by status
            var statusResult = await controller.PrintBulkTags(status: "In Service") as ViewResult;
            Assert.NotNull(statusResult);
            var statusModel = Assert.IsAssignableFrom<IEnumerable<Asset>>(statusResult.Model).ToList();
            Assert.Equal(2, statusModel.Count);
            Assert.All(statusModel, a => Assert.Equal("In Service", a.Status));

            // Test 2: Filter by search keyword
            var searchResult = await controller.PrintBulkTags(search: "LaserJet") as ViewResult;
            Assert.NotNull(searchResult);
            var searchModel = Assert.IsAssignableFrom<IEnumerable<Asset>>(searchResult.Model).ToList();
            Assert.Single(searchModel);
            Assert.Equal("TAG-202", searchModel.First().HospitalNo);

            // Test 3: Filter by calibration overdue
            var calibResult = await controller.PrintBulkTags(calibration: "overdue") as ViewResult;
            Assert.NotNull(calibResult);
            var calibModel = Assert.IsAssignableFrom<IEnumerable<Asset>>(calibResult.Model).ToList();
            Assert.Single(calibModel);
            Assert.Equal("TAG-201", calibModel.First().HospitalNo);
        }

        [Fact]
        public async Task AssetsController_PrintBulkTags_WithIds_ReturnsOnlySpecifiedAssets()
        {
            var db = GetInMemoryDbContext(nameof(AssetsController_PrintBulkTags_WithIds_ReturnsOnlySpecifiedAssets));
            db.Assets.AddRange(
                new Asset { Id = 301, HospitalNo = "TAG-301", Status = "In Service" },
                new Asset { Id = 302, HospitalNo = "TAG-302", Status = "In Service" },
                new Asset { Id = 303, HospitalNo = "TAG-303", Status = "In Service" }
            );
            await db.SaveChangesAsync();

            var userMock = CreateMockUserService();
            var controller = new AssetsController(db, userMock.Object);

            // Passing specific IDs "301,303"
            var result = await controller.PrintBulkTags(ids: "301, 303") as ViewResult;
            Assert.NotNull(result);
            var model = Assert.IsAssignableFrom<IEnumerable<Asset>>(result.Model).ToList();
            Assert.Equal(2, model.Count);
            Assert.Contains(model, a => a.Id == 301);
            Assert.Contains(model, a => a.Id == 303);
            Assert.DoesNotContain(model, a => a.Id == 302);

            // Passing "none" when 0 items matched filter
            var emptyResult = await controller.PrintBulkTags(ids: "none") as ViewResult;
            Assert.NotNull(emptyResult);
            var emptyModel = Assert.IsAssignableFrom<IEnumerable<Asset>>(emptyResult.Model).ToList();
            Assert.Empty(emptyModel);
        }

        [Fact]
        public async Task AssetsController_ExportCsv_WithFiltersAndIds_ReturnsMatchingData()
        {
            var db = GetInMemoryDbContext(nameof(AssetsController_ExportCsv_WithFiltersAndIds_ReturnsMatchingData));
            db.Assets.AddRange(
                new Asset { Id = 401, HospitalNo = "CSV-401", Status = "In Service", Brand = "Dell" },
                new Asset { Id = 402, HospitalNo = "CSV-402", Status = "Out of Service", Brand = "HP" }
            );
            await db.SaveChangesAsync();

            var userMock = CreateMockUserService();
            var controller = new AssetsController(db, userMock.Object);

            // 1. Export with status filter
            var fileResult = await controller.ExportCsv(status: "In Service") as FileContentResult;
            Assert.NotNull(fileResult);
            Assert.Equal("text/csv", fileResult.ContentType);
            var csvText = System.Text.Encoding.UTF8.GetString(fileResult.FileContents);
            Assert.Contains("CSV-401", csvText);
            Assert.DoesNotContain("CSV-402", csvText);

            // 2. Export with ids filter
            var idsResult = await controller.ExportCsv(ids: "402") as FileContentResult;
            Assert.NotNull(idsResult);
            var idsText = System.Text.Encoding.UTF8.GetString(idsResult.FileContents);
            Assert.Contains("CSV-402", idsText);
            Assert.DoesNotContain("CSV-401", idsText);
        }
    }
}
