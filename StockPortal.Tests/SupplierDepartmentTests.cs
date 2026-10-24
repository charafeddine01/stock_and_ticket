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
    public class SupplierDepartmentTests
    {
        private StockDbContext GetInMemoryDbContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<StockDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;

            var context = new StockDbContext(options);

            if (!context.Suppliers.Any())
            {
                context.Suppliers.AddRange(
                    new Supplier { Id = 1, Name = "Dell Technologies", IsActive = true, Type = 3, Departments = "[\"IT\"]" },
                    new Supplier { Id = 2, Name = "Philips Healthcare", IsActive = true, Type = 3, Departments = "[\"BIOMEDICAL\"]" },
                    new Supplier { Id = 3, Name = "Carrier Facilities", IsActive = true, Type = 3, Departments = "[\"GENERAL\"]" },
                    new Supplier { Id = 4, Name = "Multi-Care Medical & IT Solutions", IsActive = true, Type = 3, Departments = "[\"IT\",\"BIOMEDICAL\"]" },
                    new Supplier { Id = 5, Name = "Global General Contracting", IsActive = true, Type = 3, Departments = "[]" } // Common to all
                );
            }

            if (!context.TicketStatuses.Any())
            {
                context.TicketStatuses.AddRange(
                    new TicketStatus { Id = 1, Status = "opened" },
                    new TicketStatus { Id = 2, Status = "pending" },
                    new TicketStatus { Id = 5, Status = "closed" },
                    new TicketStatus { Id = 6, Status = "maintenance" }
                );
            }

            if (!context.CustodyTypes.Any())
            {
                context.CustodyTypes.AddRange(
                    new CustodyType { Id = 1, Type = "Personal" },
                    new CustodyType { Id = 2, Type = "Shared" },
                    new CustodyType { Id = 3, Type = "Supplier / External Maintenance" }
                );
            }

            if (!context.Rooms.Any())
            {
                context.Rooms.Add(new Room { Id = 1, RoomNo = 101, Floor = 1 });
            }

            context.SaveChanges();
            return context;
        }

        private Mock<ICurrentUserService> CreateMockUserService(string role, string? team = null, int userId = 1)
        {
            var mock = new Mock<ICurrentUserService>();
            mock.Setup(s => s.GetRole()).Returns(role);
            mock.Setup(s => s.GetMaintenanceTeam()).Returns(team);
            mock.Setup(s => s.GetUserId()).Returns(userId);
            mock.Setup(s => s.IsAdmin()).Returns(role == SystemRoles.Admin);
            mock.Setup(s => s.IsMaintenance()).Returns(role == SystemRoles.Maintenance);
            mock.Setup(s => s.IsEndUser()).Returns(role == SystemRoles.EndUser);
            mock.Setup(s => s.CanManageUsers()).Returns(role == SystemRoles.Admin);
            mock.Setup(s => s.CanManageAssets()).Returns(role == SystemRoles.Admin || role == SystemRoles.Maintenance);
            mock.Setup(s => s.CanManageAsset(It.IsAny<Asset>())).Returns(true);
            mock.Setup(s => s.CanAccessAsset(It.IsAny<Asset>())).Returns(true);
            return mock;
        }

        [Fact]
        public void Supplier_DepartmentList_GetterAndSetter_WorksWithJson()
        {
            var supplier = new Supplier();
            Assert.Empty(supplier.DepartmentList);

            supplier.DepartmentList = new List<string> { "IT", "BIOMEDICAL" };
            Assert.Contains("\"IT\"", supplier.Departments);
            Assert.Contains("\"BIOMEDICAL\"", supplier.Departments);

            // Re-read via getter
            var parsed = supplier.DepartmentList;
            Assert.Equal(2, parsed.Count);
            Assert.Contains("IT", parsed);
            Assert.Contains("BIOMEDICAL", parsed);
        }

        [Theory]
        [InlineData("[\"IT\"]", "IT", true)]
        [InlineData("[\"IT\"]", "BIOMEDICAL", false)]
        [InlineData("[\"IT\"]", "GENERAL", false)]
        [InlineData("[\"BIOMEDICAL\"]", "BIOMEDICAL", true)]
        [InlineData("[\"BIOMEDICAL\"]", "IT", false)]
        [InlineData("[\"IT\", \"BIOMEDICAL\"]", "IT", true)]
        [InlineData("[\"IT\", \"BIOMEDICAL\"]", "BIOMEDICAL", true)]
        [InlineData("[\"IT\", \"BIOMEDICAL\"]", "GENERAL", false)]
        [InlineData("[]", "IT", true)] // Empty means common/unrestricted
        [InlineData("[]", "BIOMEDICAL", true)]
        [InlineData("[]", "GENERAL", true)]
        public void Supplier_SupportsDepartment_EvaluatesCorrectly(string deptsJson, string department, bool expected)
        {
            var supplier = new Supplier { Departments = deptsJson };
            bool result = supplier.SupportsDepartment(department);
            Assert.Equal(expected, result);
        }

        [Fact]
        public async Task DirectoryController_Index_FiltersByDepartment()
        {
            var context = GetInMemoryDbContext(Guid.NewGuid().ToString());
            var mockUser = CreateMockUserService(SystemRoles.Admin);
            var controller = new DirectoryController(context, mockUser.Object);

            // Filter for IT
            var itResult = await controller.Index("IT") as ViewResult;
            Assert.NotNull(itResult);
            var itSuppliers = controller.ViewBag.Suppliers as List<Supplier>;
            Assert.NotNull(itSuppliers);
            // IT suppliers: Dell (IT), Multi-Care (IT, BIOMEDICAL), Global (Common)
            Assert.Contains(itSuppliers, s => s.Name == "Dell Technologies");
            Assert.Contains(itSuppliers, s => s.Name == "Multi-Care Medical & IT Solutions");
            Assert.Contains(itSuppliers, s => s.Name == "Global General Contracting");
            Assert.DoesNotContain(itSuppliers, s => s.Name == "Philips Healthcare");
            Assert.DoesNotContain(itSuppliers, s => s.Name == "Carrier Facilities");

            // Filter for BIOMEDICAL
            var bioResult = await controller.Index("BIOMEDICAL") as ViewResult;
            Assert.NotNull(bioResult);
            var bioSuppliers = controller.ViewBag.Suppliers as List<Supplier>;
            Assert.NotNull(bioSuppliers);
            Assert.Contains(bioSuppliers, s => s.Name == "Philips Healthcare");
            Assert.Contains(bioSuppliers, s => s.Name == "Multi-Care Medical & IT Solutions");
            Assert.DoesNotContain(bioSuppliers, s => s.Name == "Dell Technologies");
        }

        [Fact]
        public async Task DirectoryController_CreateSupplier_StoresDepartmentsAsJson()
        {
            var context = GetInMemoryDbContext(Guid.NewGuid().ToString());
            var mockUser = CreateMockUserService(SystemRoles.Admin);
            var controller = new DirectoryController(context, mockUser.Object);
            controller.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>());

            var newSupplier = new Supplier { Name = "Siemens Healthineers", Type = 3 };
            var selectedDepts = new[] { "BIOMEDICAL", "GENERAL" };

            var result = await controller.CreateSupplier(newSupplier, selectedDepts) as RedirectToActionResult;
            Assert.NotNull(result);

            var created = await context.Suppliers.FirstOrDefaultAsync(s => s.Name == "Siemens Healthineers");
            Assert.NotNull(created);
            Assert.True(created.IsActive);
            Assert.Contains("BIOMEDICAL", created.DepartmentList);
            Assert.Contains("GENERAL", created.DepartmentList);
            Assert.False(created.SupportsDepartment("IT"));
            Assert.True(created.SupportsDepartment("BIOMEDICAL"));
            Assert.True(created.SupportsDepartment("GENERAL"));
        }

        [Fact]
        public async Task DirectoryController_EditSupplier_UpdatesDepartmentsAndMetadata()
        {
            var context = GetInMemoryDbContext(Guid.NewGuid().ToString());
            var mockUser = CreateMockUserService(SystemRoles.Admin);
            var controller = new DirectoryController(context, mockUser.Object);
            controller.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>());

            // Edit supplier #1 (Dell) to also support BIOMEDICAL
            var result = await controller.EditSupplier(
                id: 1,
                name: "Dell Technologies Healthcare Division",
                type: 3,
                selectedDepartments: new[] { "IT", "BIOMEDICAL" },
                isActive: true
            ) as RedirectToActionResult;

            Assert.NotNull(result);

            var updated = await context.Suppliers.FindAsync(1);
            Assert.NotNull(updated);
            Assert.Equal("Dell Technologies Healthcare Division", updated.Name);
            Assert.True(updated.SupportsDepartment("IT"));
            Assert.True(updated.SupportsDepartment("BIOMEDICAL"));
            Assert.False(updated.SupportsDepartment("GENERAL"));
        }

        [Fact]
        public async Task MaintenanceController_SendToSupplier_RejectsMismatchedDepartmentSupplier()
        {
            var context = GetInMemoryDbContext(Guid.NewGuid().ToString());
            // Create a Biomedical asset
            var bioAsset = new Asset
            {
                Id = 10,
                HospitalNo = "HOSP-BIO-001",
                Brand = "Philips",
                Type = "Patient Monitor",
                MaintenanceTeam = "BIOMEDICAL",
                Status = "In Use"
            };
            context.Assets.Add(bioAsset);
            await context.SaveChangesAsync();

            var mockUser = CreateMockUserService(SystemRoles.Maintenance, "BIOMEDICAL", userId: 2);
            var controller = new MaintenanceController(context, mockUser.Object);
            controller.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>());

            // Attempt to send Biomedical asset to Supplier #1 (Dell Technologies - IT only)
            var result = await controller.SendToSupplier(
                assetId: 10,
                supplierId: 1, // Dell (IT only)
                description: "Display screen flicker",
                ticketId: null,
                technicianId: 2,
                maintenanceType: 1,
                sentDate: DateTime.Now
            ) as ViewResult;

            // Must reject and stay on view with ModelState error
            Assert.NotNull(result);
            Assert.False(controller.ModelState.IsValid);
            Assert.True(controller.ModelState.ContainsKey("SupplierId"));
            var error = controller.ModelState["SupplierId"]!.Errors.First().ErrorMessage;
            Assert.Contains("not authorized", error);

            // Asset status must remain unchanged
            var unchangedAsset = await context.Assets.FindAsync(10);
            Assert.Equal("In Use", unchangedAsset!.Status);
        }

        [Fact]
        public async Task MaintenanceController_SendToSupplier_AcceptsMatchingDepartmentSupplier()
        {
            var context = GetInMemoryDbContext(Guid.NewGuid().ToString());
            // Create a Biomedical asset
            var bioAsset = new Asset
            {
                Id = 20,
                HospitalNo = "HOSP-BIO-002",
                Brand = "Philips",
                Type = "ECG Machine",
                MaintenanceTeam = "BIOMEDICAL",
                Status = "In Use"
            };
            context.Assets.Add(bioAsset);
            await context.SaveChangesAsync();

            var mockUser = CreateMockUserService(SystemRoles.Maintenance, "BIOMEDICAL", userId: 2);
            var controller = new MaintenanceController(context, mockUser.Object);
            controller.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>());

            // Send to Supplier #2 (Philips Healthcare - BIOMEDICAL)
            var result = await controller.SendToSupplier(
                assetId: 20,
                supplierId: 2, // Philips (BIOMEDICAL)
                description: "Lead cable failure and sensor calibration required",
                ticketId: null,
                technicianId: 2,
                maintenanceType: 1,
                sentDate: DateTime.Now
            ) as RedirectToActionResult;

            Assert.NotNull(result);
            Assert.Equal("WorkOrderDetails", result.ActionName);

            // Asset must now be Under Maintenance with Supplier Custody
            var updatedAsset = await context.Assets.FindAsync(20);
            Assert.Equal("Under Maintenance", updatedAsset!.Status);
            Assert.Equal(3, updatedAsset.CustodyTypeId); // Supplier custody

            var workOrder = await context.AssetMaintenances.FirstOrDefaultAsync(m => m.AssetId == 20);
            Assert.NotNull(workOrder);
            Assert.Equal(2, workOrder.SupplierId);
        }

        [Fact]
        public async Task MaintenanceController_SendToSupplier_AcceptsCommonSupplierForAnyDepartment()
        {
            var context = GetInMemoryDbContext(Guid.NewGuid().ToString());
            var genAsset = new Asset
            {
                Id = 30,
                HospitalNo = "HOSP-GEN-001",
                Brand = "Carrier",
                Type = "HVAC Unit",
                MaintenanceTeam = "GENERAL",
                Status = "In Stock"
            };
            context.Assets.Add(genAsset);
            await context.SaveChangesAsync();

            var mockUser = CreateMockUserService(SystemRoles.Maintenance, "GENERAL", userId: 3);
            var controller = new MaintenanceController(context, mockUser.Object);
            controller.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>());

            // Send to Supplier #5 (Global General Contracting - common to all departments [])
            var result = await controller.SendToSupplier(
                assetId: 30,
                supplierId: 5, // Common vendor
                description: "Compressor valve inspection",
                ticketId: null,
                technicianId: 3,
                maintenanceType: 1,
                sentDate: DateTime.Now
            ) as RedirectToActionResult;

            Assert.NotNull(result);
            Assert.Equal("WorkOrderDetails", result.ActionName);

            var updatedAsset = await context.Assets.FindAsync(30);
            Assert.Equal("Under Maintenance", updatedAsset!.Status);
            Assert.Equal(3, updatedAsset.CustodyTypeId);
        }
    }
}
