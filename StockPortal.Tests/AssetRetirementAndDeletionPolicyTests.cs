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
using Xunit;

namespace StockPortal.Tests
{
    public class AssetRetirementAndDeletionPolicyTests
    {
        private StockDbContext GetInMemoryDbContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<StockDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;

            var context = new StockDbContext(options);

            if (!context.Rooms.Any(r => r.Id == 1))
            {
                context.Rooms.Add(new Room { Id = 1, RoomNo = 101, Floor = 1, RoomNameAr = "عيادة 101" });
            }
            if (!context.Rooms.Any(r => r.RoomNo == 999))
            {
                context.Rooms.Add(new Room { Id = 999, RoomNo = 999, Floor = 0, RoomNameAr = "مستودع الكهنة والرجيع (Scrap & Warehouse)" });
            }

            if (!context.Users.Any(u => u.Id == 1))
            {
                context.Users.Add(new User { Id = 1, DomainUser = "admin", FullName = "System Admin", Role = SystemRoles.Admin, IsActive = true });
            }
            if (!context.Users.Any(u => u.Id == 2))
            {
                context.Users.Add(new User { Id = 2, DomainUser = "doctor1", FullName = "Dr. Omar", Role = SystemRoles.EndUser, IsActive = true });
            }

            if (!context.Suppliers.Any(s => s.Id == 1))
            {
                context.Suppliers.Add(new Supplier { Id = 1, Name = "Tech Vendor", IsActive = true });
            }

            if (!context.CustodyTypes.Any(c => c.Id == 1))
            {
                context.CustodyTypes.Add(new CustodyType { Id = 1, Type = "Personal" });
            }

            context.SaveChanges();
            return context;
        }

        private Mock<ICurrentUserService> CreateAdminUserMock()
        {
            var mock = new Mock<ICurrentUserService>();
            mock.Setup(s => s.IsAdmin()).Returns(true);
            mock.Setup(s => s.IsMaintenance()).Returns(false);
            mock.Setup(s => s.IsEndUser()).Returns(false);
            mock.Setup(s => s.GetUserId()).Returns(1);
            mock.Setup(s => s.GetFullName()).Returns("System Admin");
            mock.Setup(s => s.CanAccessAsset(It.IsAny<Asset>())).Returns(true);
            mock.Setup(s => s.CanManageAsset(It.IsAny<Asset>())).Returns(true);
            mock.Setup(s => s.CanEditAsset(It.IsAny<Asset>())).Returns(true);
            mock.Setup(s => s.CanManageAssets()).Returns(true);
            mock.Setup(s => s.CanAddAsset()).Returns(true);
            return mock;
        }

        private AssetsController CreateAssetsController(StockDbContext db, Mock<ICurrentUserService> userMock)
        {
            var controller = new AssetsController(db, userMock.Object);
            var httpContext = new DefaultHttpContext();
            controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
            controller.TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());
            return controller;
        }

        [Fact]
        public async Task Retire_DischargesUserCustody_AndMovesToWarehouse()
        {
            var db = GetInMemoryDbContext(nameof(Retire_DischargesUserCustody_AndMovesToWarehouse));
            var asset = new Asset
            {
                Id = 101,
                HospitalNo = "PC-101",
                Status = "In Service",
                UserId = 2, // Dr. Omar
                RoomId = 1,
                MaintenanceTeam = MaintenanceTeams.IT
            };
            db.Assets.Add(asset);
            await db.SaveChangesAsync();

            var adminMock = CreateAdminUserMock();
            var controller = CreateAssetsController(db, adminMock);

            var result = await controller.Retire(101, "Motherboard burnt");
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Details", redirect.ActionName);

            var updated = await db.Assets.FindAsync(101);
            Assert.NotNull(updated);
            Assert.Equal("Retired", updated.Status);
            Assert.Null(updated.UserId); // Must NOT be in user custody
            Assert.Equal(999, updated.RoomId); // Moved to Scrap & Warehouse room

            var history = await db.AssetHistories.Where(h => h.AssetId == 101).ToListAsync();
            Assert.NotEmpty(history);
            Assert.Contains("retired", history.Last().Notes, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task TransferCustody_WhenAssetIsRetired_IsBlocked()
        {
            var db = GetInMemoryDbContext(nameof(TransferCustody_WhenAssetIsRetired_IsBlocked));
            var asset = new Asset
            {
                Id = 102,
                HospitalNo = "PC-102",
                Status = "Retired",
                UserId = null,
                RoomId = 999,
                MaintenanceTeam = MaintenanceTeams.IT
            };
            db.Assets.Add(asset);
            await db.SaveChangesAsync();

            var adminMock = CreateAdminUserMock();
            var controller = CreateAssetsController(db, adminMock);

            var result = await controller.TransferCustody(102, newUserId: 2, newRoomId: 1, newCustodyTypeId: 1, notes: "Try assign");
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Details", redirect.ActionName);

            Assert.True(controller.TempData.ContainsKey("ErrorMessage"));

            var fresh = await db.Assets.FindAsync(102);
            Assert.Null(fresh!.UserId); // Remains null
        }

        [Fact]
        public async Task Edit_WhenStatusChangedToRetired_DischargesUserCustody()
        {
            var db = GetInMemoryDbContext(nameof(Edit_WhenStatusChangedToRetired_DischargesUserCustody));
            var asset = new Asset
            {
                Id = 103,
                HospitalNo = "PC-103",
                Status = "In Service",
                UserId = 2,
                RoomId = 1,
                MaintenanceTeam = MaintenanceTeams.IT
            };
            db.Assets.Add(asset);
            await db.SaveChangesAsync();

            var adminMock = CreateAdminUserMock();
            var controller = CreateAssetsController(db, adminMock);

            var postedAsset = new Asset
            {
                Id = 103,
                HospitalNo = "PC-103",
                Status = "Retired",
                UserId = 2, // Form might still post the user ID
                RoomId = 1,
                MaintenanceTeam = MaintenanceTeams.IT
            };

            var result = await controller.Edit(103, postedAsset);
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Details", redirect.ActionName);

            var updated = await db.Assets.FindAsync(103);
            Assert.NotNull(updated);
            Assert.Equal("Retired", updated.Status);
            Assert.Null(updated.UserId); // System forced null!
            Assert.Equal(999, updated.RoomId); // Defaulted to warehouse room
        }

        [Fact]
        public async Task Delete_WhenAssetHasTickets_IsBlocked()
        {
            var db = GetInMemoryDbContext(nameof(Delete_WhenAssetHasTickets_IsBlocked));
            var asset = new Asset
            {
                Id = 104,
                HospitalNo = "PC-104",
                Status = "In Service",
                UserId = 2,
                MaintenanceTeam = MaintenanceTeams.IT
            };
            db.Assets.Add(asset);
            db.Tickets.Add(new Ticket
            {
                Id = 201,
                AssetId = 104,
                Type = "Hardware",
                StatusId = 5, // Closed ticket
                OpenedDT = DateTime.Now.AddDays(-10),
                ClosedDT = DateTime.Now.AddDays(-8)
            });
            await db.SaveChangesAsync();

            var adminMock = CreateAdminUserMock();
            var controller = CreateAssetsController(db, adminMock);

            // GET Delete should show block reason
            var getView = await controller.Delete(104);
            var viewResult = Assert.IsType<ViewResult>(getView);
            Assert.False((bool)controller.ViewBag.CanDelete);
            Assert.NotEmpty((string)controller.ViewBag.BlockReason);

            // POST Delete should refuse deletion
            var postResult = await controller.DeleteConfirmed(104);
            var redirect = Assert.IsType<RedirectToActionResult>(postResult);
            Assert.Equal("Details", redirect.ActionName);
            Assert.True(controller.TempData.ContainsKey("ErrorMessage"));

            // Asset still exists in DB!
            Assert.NotNull(await db.Assets.FindAsync(104));
        }

        [Fact]
        public async Task Delete_WhenAssetHasMaintenanceRecords_IsBlocked()
        {
            var db = GetInMemoryDbContext(nameof(Delete_WhenAssetHasMaintenanceRecords_IsBlocked));
            var asset = new Asset
            {
                Id = 105,
                HospitalNo = "PC-105",
                Status = "In Service",
                MaintenanceTeam = MaintenanceTeams.IT
            };
            db.Assets.Add(asset);
            db.AssetMaintenances.Add(new AssetMaintenance
            {
                Id = 301,
                AssetId = 105,
                TimStamp = DateTime.Now.AddDays(-5),
                WorkOrderStatus = "Completed",
                Description = "Replaced thermal paste"
            });
            await db.SaveChangesAsync();

            var adminMock = CreateAdminUserMock();
            var controller = CreateAssetsController(db, adminMock);

            var postResult = await controller.DeleteConfirmed(105);
            var redirect = Assert.IsType<RedirectToActionResult>(postResult);
            Assert.Equal("Details", redirect.ActionName);
            Assert.True(controller.TempData.ContainsKey("ErrorMessage"));

            Assert.NotNull(await db.Assets.FindAsync(105));
        }

        [Fact]
        public async Task Delete_WhenAssetHasSparePartTransactions_IsBlocked()
        {
            var db = GetInMemoryDbContext(nameof(Delete_WhenAssetHasSparePartTransactions_IsBlocked));
            var asset = new Asset
            {
                Id = 106,
                HospitalNo = "PC-106",
                Status = "In Service",
                MaintenanceTeam = MaintenanceTeams.IT
            };
            var part = new SparePart { Id = 401, Name = "DDR4 16GB", Quantity = 10, Department = "IT" };
            db.Assets.Add(asset);
            db.SpareParts.Add(part);
            db.SparePartTransactions.Add(new SparePartTransaction
            {
                Id = 501,
                AssetId = 106,
                SparePartId = 401,
                QuantityChanged = -1,
                QuantityAfter = 9,
                TransactionType = "Usage in Ticket",
                Timestamp = DateTime.Now
            });
            await db.SaveChangesAsync();

            var adminMock = CreateAdminUserMock();
            var controller = CreateAssetsController(db, adminMock);

            var postResult = await controller.DeleteConfirmed(106);
            Assert.IsType<RedirectToActionResult>(postResult);
            Assert.True(controller.TempData.ContainsKey("ErrorMessage"));

            Assert.NotNull(await db.Assets.FindAsync(106));
        }

        [Fact]
        public async Task Delete_WhenAssetIsNewlyAddedWithoutLogs_Succeeds()
        {
            var db = GetInMemoryDbContext(nameof(Delete_WhenAssetIsNewlyAddedWithoutLogs_Succeeds));
            // Newly added by mistake - zero tickets, zero maintenance, zero spare parts, zero past custody
            var asset = new Asset
            {
                Id = 107,
                HospitalNo = "PC-MISTAKE-107",
                Status = "In Service",
                UserId = null,
                MaintenanceTeam = MaintenanceTeams.IT
            };
            db.Assets.Add(asset);
            await db.SaveChangesAsync();

            var adminMock = CreateAdminUserMock();
            var controller = CreateAssetsController(db, adminMock);

            // GET Delete indicates can delete
            var getView = await controller.Delete(107);
            Assert.IsType<ViewResult>(getView);
            Assert.True((bool)controller.ViewBag.CanDelete);

            // POST Delete succeeds
            var postResult = await controller.DeleteConfirmed(107);
            var redirect = Assert.IsType<RedirectToActionResult>(postResult);
            Assert.Equal("Index", redirect.ActionName);
            Assert.True(controller.TempData.ContainsKey("SuccessMessage"));

            Assert.Null(await db.Assets.FindAsync(107));
        }

        [Fact]
        public async Task Tickets_GetUserAssets_ExcludesRetiredAssets()
        {
            var db = GetInMemoryDbContext(nameof(Tickets_GetUserAssets_ExcludesRetiredAssets));
            db.Assets.Add(new Asset
            {
                Id = 108,
                HospitalNo = "PC-ACTIVE-108",
                Status = "In Service",
                UserId = 2,
                MaintenanceTeam = MaintenanceTeams.IT
            });
            db.Assets.Add(new Asset
            {
                Id = 109,
                HospitalNo = "PC-RETIRED-109",
                Status = "Retired",
                UserId = 2, // Even if somehow flagged
                MaintenanceTeam = MaintenanceTeams.IT
            });
            await db.SaveChangesAsync();

            var userMock = new Mock<ICurrentUserService>();
            userMock.Setup(s => s.GetUserId()).Returns(2);
            userMock.Setup(s => s.IsAdmin()).Returns(false);
            userMock.Setup(s => s.IsMaintenance()).Returns(false);
            userMock.Setup(s => s.IsEndUser()).Returns(true);

            var notifyMock = new Mock<INotificationService>();
            var ticketsController = new TicketsController(db, userMock.Object, notifyMock.Object);
            var result = await ticketsController.GetUserAssets(2);
            var json = Assert.IsType<JsonResult>(result);

            // Dynamic reflection check on JSON return value
            var val = json.Value;
            var assetsProp = val.GetType().GetProperty("assets");
            var assetsList = assetsProp!.GetValue(val) as System.Collections.IEnumerable;
            Assert.NotNull(assetsList);

            var list = assetsList.Cast<object>().ToList();
            Assert.Single(list); // Only the active PC-ACTIVE-108
        }
    }
}
