using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Primitives;
using Moq;
using StockPortal.Controllers;
using StockPortal.Data;
using StockPortal.Models;
using StockPortal.Services;
using Xunit;

namespace StockPortal.Tests
{
    public class DeviceFormSoftwareTests
    {
        private StockDbContext GetInMemoryDbContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<StockDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;

            var context = new StockDbContext(options);

            if (!context.Rooms.Any())
            {
                context.Rooms.Add(new Room { Id = 1, RoomNo = 101, Floor = 1 });
            }

            if (!context.Users.Any())
            {
                context.Users.Add(new User { Id = 1, DomainUser = "DOMAIN\\admin", FullName = "System Admin", Role = SystemRoles.Admin });
            }

            if (!context.Suppliers.Any())
            {
                context.Suppliers.Add(new Supplier { Id = 1, Name = "Tech Vendor", IsActive = true });
            }

            if (!context.CustodyTypes.Any())
            {
                context.CustodyTypes.Add(new CustodyType { Id = 1, Type = "Personal" });
            }

            context.SaveChanges();
            return context;
        }

        private Mock<ICurrentUserService> CreateMockUserService(string role = SystemRoles.Admin, string? team = MaintenanceTeams.IT)
        {
            var mock = new Mock<ICurrentUserService>();
            mock.Setup(s => s.GetRole()).Returns(role);
            mock.Setup(s => s.GetMaintenanceTeam()).Returns(team);
            mock.Setup(s => s.GetUserId()).Returns(1);
            mock.Setup(s => s.IsAdmin()).Returns(role == SystemRoles.Admin);
            mock.Setup(s => s.IsMaintenance()).Returns(role == SystemRoles.Maintenance);
            mock.Setup(s => s.IsEndUser()).Returns(role == SystemRoles.EndUser);
            mock.Setup(s => s.CanManageDeviceForms()).Returns(true);
            mock.Setup(s => s.CanManageDeviceForm(It.IsAny<DeviceForm>())).Returns(true);
            mock.Setup(s => s.CanManageAssets()).Returns(true);
            mock.Setup(s => s.CanManageAsset(It.IsAny<Asset>())).Returns(true);
            mock.Setup(s => s.CanAccessAsset(It.IsAny<Asset>())).Returns(true);
            mock.Setup(s => s.CanCreateAssetInDepartment(It.IsAny<string>())).Returns(true);
            return mock;
        }

        [Fact]
        public void DeviceForm_InstallableSoftwaresList_SerializesAndDeserializesCorrectly()
        {
            var form = new DeviceForm
            {
                Name = "Medical PC",
                Department = MaintenanceTeams.IT,
                HasSoftware = true,
                InstallableSoftwaresList = new List<string> { "Windows 11 Pro", "PACS Imaging Viewer", "Hospital HIS" }
            };

            Assert.True(form.HasSoftware);
            Assert.NotNull(form.InstallableSoftwares);
            Assert.Contains("PACS Imaging Viewer", form.InstallableSoftwares);

            // Re-read via list
            var list = form.InstallableSoftwaresList;
            Assert.Equal(3, list.Count);
            Assert.Equal("Windows 11 Pro", list[0]);
            Assert.Equal("PACS Imaging Viewer", list[1]);
            Assert.Equal("Hospital HIS", list[2]);
        }

        [Fact]
        public async Task DeviceFormsController_Create_WithInstallableSoftwares_PersistsInDb()
        {
            var db = GetInMemoryDbContext(nameof(DeviceFormsController_Create_WithInstallableSoftwares_PersistsInDb));
            var userMock = CreateMockUserService();
            var controller = new DeviceFormsController(db, userMock.Object);
            controller.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>());

            var model = new DeviceForm
            {
                Name = "Clinical Workstation",
                Department = MaintenanceTeams.IT,
                Form = "[{\"name\":\"CPU\",\"label\":\"Processor\",\"type\":\"text\",\"required\":true}]",
                HasSoftware = true,
                InstallableSoftwares = JsonSerializer.Serialize(new[] { "Windows 11 Pro", "Epic Hyperspace", "Philips IntelliSpace" })
            };

            var result = await controller.Create(model);
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal(nameof(DeviceFormsController.Index), redirect.ActionName);

            var saved = await db.DeviceForms.FirstOrDefaultAsync(f => f.Name == "Clinical Workstation");
            Assert.NotNull(saved);
            Assert.True(saved.HasSoftware);
            Assert.Equal(3, saved.InstallableSoftwaresList.Count);
            Assert.Contains("Epic Hyperspace", saved.InstallableSoftwaresList);
        }

        [Fact]
        public async Task DeviceFormsController_Create_WithoutSoftware_SetsEmptyJson()
        {
            var db = GetInMemoryDbContext(nameof(DeviceFormsController_Create_WithoutSoftware_SetsEmptyJson));
            var userMock = CreateMockUserService();
            var controller = new DeviceFormsController(db, userMock.Object);
            controller.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>());

            var model = new DeviceForm
            {
                Name = "Core Router",
                Department = MaintenanceTeams.IT,
                Form = "[{\"name\":\"Ports\",\"label\":\"Ports Count\",\"type\":\"number\",\"required\":true}]",
                HasSoftware = false,
                InstallableSoftwares = "[\"Some Stale Softwares\"]" // Should be wiped because HasSoftware is false
            };

            var result = await controller.Create(model);
            Assert.IsType<RedirectToActionResult>(result);

            var saved = await db.DeviceForms.FirstOrDefaultAsync(f => f.Name == "Core Router");
            Assert.NotNull(saved);
            Assert.False(saved.HasSoftware);
            Assert.Equal("[]", saved.InstallableSoftwares);
            Assert.Empty(saved.InstallableSoftwaresList);
        }

        [Fact]
        public async Task DeviceFormsController_Edit_UpdatesInstallableSoftwares()
        {
            var db = GetInMemoryDbContext(nameof(DeviceFormsController_Edit_UpdatesInstallableSoftwares));
            var existing = new DeviceForm
            {
                Id = 10,
                Name = "Diagnostic Workstation",
                Department = MaintenanceTeams.IT,
                Form = "[{\"name\":\"GPU\",\"label\":\"Graphics\",\"type\":\"text\",\"required\":true}]",
                HasSoftware = false,
                InstallableSoftwares = "[]"
            };
            db.DeviceForms.Add(existing);
            await db.SaveChangesAsync();

            var userMock = CreateMockUserService();
            var controller = new DeviceFormsController(db, userMock.Object);
            controller.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>());

            var updateModel = new DeviceForm
            {
                Id = 10,
                Name = "Diagnostic Workstation",
                Department = MaintenanceTeams.IT,
                Form = existing.Form,
                HasSoftware = true,
                InstallableSoftwares = JsonSerializer.Serialize(new[] { "Radiology Viewer", "3D Reconstruction Studio" })
            };

            var result = await controller.Edit(10, updateModel);
            Assert.IsType<RedirectToActionResult>(result);

            var updated = await db.DeviceForms.FindAsync(10);
            Assert.NotNull(updated);
            Assert.True(updated.HasSoftware);
            Assert.Equal(2, updated.InstallableSoftwaresList.Count);
            Assert.Contains("Radiology Viewer", updated.InstallableSoftwaresList);
        }

        [Fact]
        public async Task DeviceFormsController_GetSchema_ReturnsSoftwareMetadata()
        {
            var db = GetInMemoryDbContext(nameof(DeviceFormsController_GetSchema_ReturnsSoftwareMetadata));
            var form = new DeviceForm
            {
                Id = 5,
                Name = "Ultrasound Workstation",
                Department = MaintenanceTeams.Biomedical,
                Form = "[{\"name\":\"Probe\",\"label\":\"Probe Type\",\"type\":\"text\",\"required\":true}]",
                HasSoftware = true,
                InstallableSoftwares = JsonSerializer.Serialize(new[] { "Ultrasound Imaging Suite", "DICOM Exporter" })
            };
            db.DeviceForms.Add(form);
            await db.SaveChangesAsync();

            var userMock = CreateMockUserService(role: SystemRoles.Admin);
            var controller = new DeviceFormsController(db, userMock.Object);

            var result = await controller.GetSchema(5);
            var jsonResult = Assert.IsType<JsonResult>(result);
            Assert.NotNull(jsonResult.Value);

            // Serialize & deserialize anonymous object to check properties
            var jsonStr = JsonSerializer.Serialize(jsonResult.Value);
            using var doc = JsonDocument.Parse(jsonStr);
            var root = doc.RootElement;

            Assert.Equal(5, root.GetProperty("id").GetInt32());
            Assert.Equal("Ultrasound Workstation", root.GetProperty("name").GetString());
            Assert.True(root.GetProperty("hasSoftware").GetBoolean());

            var softwaresArray = root.GetProperty("installableSoftwares");
            Assert.Equal(2, softwaresArray.GetArrayLength());
            Assert.Equal("Ultrasound Imaging Suite", softwaresArray[0].GetString());
        }

        [Fact]
        public async Task AssetsController_Create_WithSelectedSoftwares_SavesAsJsonInAsset()
        {
            var db = GetInMemoryDbContext(nameof(AssetsController_Create_WithSelectedSoftwares_SavesAsJsonInAsset));
            var deviceForm = new DeviceForm
            {
                Id = 20,
                Name = "Nursing Cart Laptop",
                Department = MaintenanceTeams.IT,
                Form = "[{\"name\":\"CPU\",\"label\":\"Processor\",\"type\":\"text\",\"required\":false}]",
                HasSoftware = true,
                InstallableSoftwares = JsonSerializer.Serialize(new[] { "Windows 11", "Medication Admin System", "Barcode Scanner Client" })
            };
            db.DeviceForms.Add(deviceForm);
            await db.SaveChangesAsync();

            var userMock = CreateMockUserService();
            var controller = new AssetsController(db, userMock.Object);
            controller.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>());

            var asset = new Asset
            {
                HospitalNo = "HOSP-CART-001",
                SerialNo = "SN-CART-9912",
                Brand = "Dell",
                RoomId = 1,
                UserId = 1,
                CustodyTypeId = 1
            };

            var formCollection = new FormCollection(new Dictionary<string, StringValues>
            {
                { "selectedSoftwares", new StringValues(new[] { "Windows 11", "Medication Admin System" }) }
            });

            var result = await controller.Create(asset, deviceFormId: 20, formCollection: formCollection);
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal(nameof(AssetsController.Details), redirect.ActionName);

            var createdAsset = await db.Assets.FirstOrDefaultAsync(a => a.HospitalNo == "HOSP-CART-001");
            Assert.NotNull(createdAsset);
            Assert.NotNull(createdAsset.InstalledSoftwares);

            // Verify InstalledSoftwares is stored as JSON in DB
            var installedList = createdAsset.InstalledSoftwaresList;
            Assert.Equal(2, installedList.Count);
            Assert.Contains("Windows 11", installedList);
            Assert.Contains("Medication Admin System", installedList);
            Assert.DoesNotContain("Barcode Scanner Client", installedList);
        }

        [Fact]
        public async Task AssetsController_Edit_UpdatesSelectedSoftwaresAsJson()
        {
            var db = GetInMemoryDbContext(nameof(AssetsController_Edit_UpdatesSelectedSoftwaresAsJson));
            var deviceForm = new DeviceForm
            {
                Id = 30,
                Name = "Pharmacy Terminal",
                Department = MaintenanceTeams.IT,
                Form = "[{\"name\":\"OS\",\"label\":\"Operating System\",\"type\":\"text\",\"required\":false}]",
                HasSoftware = true,
                InstallableSoftwares = JsonSerializer.Serialize(new[] { "Windows 11 Pro", "Pharmacy Dispense 2.0", "Label Printer Suite" })
            };
            db.DeviceForms.Add(deviceForm);

            var asset = new Asset
            {
                Id = 100,
                HospitalNo = "HOSP-PHARM-001",
                SerialNo = "SN-PHARM-1122",
                Brand = "HP",
                Type = "Pharmacy Terminal",
                MaintenanceTeam = MaintenanceTeams.IT,
                RoomId = 1,
                UserId = 1,
                CustodyTypeId = 1,
                InstalledSoftwares = JsonSerializer.Serialize(new[] { "Windows 11 Pro" })
            };
            db.Assets.Add(asset);
            await db.SaveChangesAsync();
            db.Entry(asset).State = EntityState.Detached;

            var userMock = CreateMockUserService();
            var controller = new AssetsController(db, userMock.Object);
            controller.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>());

            var updatedAsset = new Asset
            {
                Id = 100,
                HospitalNo = "HOSP-PHARM-001",
                SerialNo = "SN-PHARM-1122",
                Brand = "HP",
                Type = "Pharmacy Terminal",
                MaintenanceTeam = MaintenanceTeams.IT,
                RoomId = 1,
                UserId = 1,
                CustodyTypeId = 1
            };

            var formCollection = new FormCollection(new Dictionary<string, StringValues>
            {
                { "selectedSoftwares", new StringValues(new[] { "Windows 11 Pro", "Pharmacy Dispense 2.0", "Label Printer Suite" }) }
            });

            var result = await controller.Edit(100, updatedAsset, deviceFormId: 30, formCollection: formCollection);
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal(nameof(AssetsController.Details), redirect.ActionName);

            var reloaded = await db.Assets.FindAsync(100);
            Assert.NotNull(reloaded);
            Assert.Equal(3, reloaded.InstalledSoftwaresList.Count);
            Assert.Contains("Pharmacy Dispense 2.0", reloaded.InstalledSoftwaresList);
            Assert.Contains("Label Printer Suite", reloaded.InstalledSoftwaresList);
        }
    }
}
