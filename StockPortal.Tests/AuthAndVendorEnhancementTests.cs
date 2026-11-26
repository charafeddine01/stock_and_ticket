using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
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
    public class AuthAndVendorEnhancementTests
    {
        private StockDbContext GetInMemoryDbContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<StockDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;

            return new StockDbContext(options);
        }

        private IConfiguration CreateConfig(bool windowsAuth = true, bool passwordAuth = true, bool canChangePassword = true)
        {
            var inMemorySettings = new Dictionary<string, string?>
            {
                { "Authentication:CanAccessUsingWindowsAuth", windowsAuth.ToString() },
                { "Authentication:CanAccessUsingUsernameAndPassword", passwordAuth.ToString() },
                { "Authentication:CanUserChangePassword", canChangePassword.ToString() }
            };

            return new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();
        }

        [Fact]
        public async Task AccountController_Login_RejectsPost_WhenPasswordAuthIsDisabled()
        {
            // Arrange
            using var db = GetInMemoryDbContext(Guid.NewGuid().ToString());
            var user = new User { Id = 1, DomainUser = "testuser", FullName = "Test User", PasswordHash = "hash", IsActive = true };
            db.Users.Add(user);
            await db.SaveChangesAsync();

            var mockUserService = new Mock<ICurrentUserService>();
            var config = CreateConfig(windowsAuth: true, passwordAuth: false, canChangePassword: true);

            var controller = new AccountController(db, mockUserService.Object, config);
            var httpContext = new DefaultHttpContext();
            controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

            var model = new LoginViewModel { Username = "testuser", Password = "Password123!" };

            // Act
            var result = await controller.Login(model);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.False(controller.ModelState.IsValid);
            Assert.Contains(controller.ModelState[string.Empty]!.Errors, e => e.ErrorMessage.Contains("disabled"));
        }

        [Fact]
        public async Task AccountController_AutoLogin_RedirectsToAccessDenied_WhenWindowsAuthIsDisabled()
        {
            // Arrange
            using var db = GetInMemoryDbContext(Guid.NewGuid().ToString());
            var mockUserService = new Mock<ICurrentUserService>();
            var config = CreateConfig(windowsAuth: false, passwordAuth: true, canChangePassword: true);

            var controller = new AccountController(db, mockUserService.Object, config);
            var httpContext = new DefaultHttpContext();
            var tempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());
            controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
            controller.TempData = tempData;

            // Act
            var result = await controller.AutoLogin();

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("AccessDenied", redirectResult.ActionName);
            Assert.Equal("WindowsAuthDisabled", redirectResult.RouteValues?["reason"]?.ToString());
            Assert.Contains("Windows authentication is disabled", controller.TempData["ErrorMessage"]?.ToString() ?? "");
        }

        [Fact]
        public async Task AccountController_AutoLogin_RedirectsToAccessDenied_WhenUserNotFoundInDb()
        {
            // Arrange
            using var db = GetInMemoryDbContext(Guid.NewGuid().ToString());
            var mockUserService = new Mock<ICurrentUserService>();
            var config = CreateConfig(windowsAuth: true, passwordAuth: true, canChangePassword: true);

            var controller = new AccountController(db, mockUserService.Object, config);
            var httpContext = new DefaultHttpContext();
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "unregistered.pc.user") }, "Negotiate"));
            controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

            // Act
            var result = await controller.AutoLogin();

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("AccessDenied", redirectResult.ActionName);
            Assert.Equal("NotFound", redirectResult.RouteValues?["reason"]?.ToString());
            Assert.Equal("unregistered.pc.user", redirectResult.RouteValues?["username"]?.ToString());
        }

        [Fact]
        public async Task AccountController_AutoLogin_RedirectsToAccessDenied_WhenUserIsInactive()
        {
            // Arrange
            using var db = GetInMemoryDbContext(Guid.NewGuid().ToString());
            db.Users.Add(new User
            {
                Id = 88,
                DomainUser = "inactive.dev.user",
                FullName = "Inactive User",
                IsActive = false
            });
            await db.SaveChangesAsync();

            var mockUserService = new Mock<ICurrentUserService>();
            var config = CreateConfig(windowsAuth: true, passwordAuth: true);

            var controller = new AccountController(db, mockUserService.Object, config);
            var httpContext = new DefaultHttpContext();
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "inactive.dev.user") }, "Negotiate"));
            httpContext.Connection.RemoteIpAddress = System.Net.IPAddress.Loopback;
            controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

            // Act
            var result = await controller.AutoLogin();

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("AccessDenied", redirectResult.ActionName);
            Assert.Equal("Inactive", redirectResult.RouteValues?["reason"]?.ToString());
        }

        [Fact]
        public async Task AccountController_ChangePassword_FailsForNormalUser_WhenCanUserChangePasswordIsFalse()
        {
            // Arrange
            using var db = GetInMemoryDbContext(Guid.NewGuid().ToString());
            var user = new User { Id = 2, DomainUser = "normaluser", FullName = "Normal User", Role = SystemRoles.EndUser, PasswordHash = "oldhash", IsActive = true };
            db.Users.Add(user);
            await db.SaveChangesAsync();

            var mockUserService = new Mock<ICurrentUserService>();
            mockUserService.Setup(s => s.GetCurrentUser()).Returns(user);
            mockUserService.Setup(s => s.IsAdmin()).Returns(false);

            var config = CreateConfig(windowsAuth: true, passwordAuth: true, canChangePassword: false);

            var controller = new AccountController(db, mockUserService.Object, config);
            var httpContext = new DefaultHttpContext();
            var tempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());
            controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
            controller.TempData = tempData;

            var model = new ChangePasswordViewModel { CurrentPassword = "OldPass", NewPassword = "NewPass123!", ConfirmPassword = "NewPass123!" };

            // Act
            var result = await controller.ChangePassword(model);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal(nameof(AccountController.Profile), redirectResult.ActionName);
            Assert.Contains("disabled", controller.TempData["ErrorMessage"]?.ToString() ?? "");
            Assert.Contains("standard users", controller.TempData["ErrorMessage"]?.ToString() ?? "");
        }

        [Fact]
        public async Task DirectoryController_CreateAndEditSupplier_PersistsFocalPointDetails()
        {
            // Arrange
            using var db = GetInMemoryDbContext(Guid.NewGuid().ToString());
            var mockUserService = new Mock<ICurrentUserService>();
            mockUserService.Setup(s => s.CanManageUsers()).Returns(true);
            mockUserService.Setup(s => s.IsAdmin()).Returns(true);

            var controller = new DirectoryController(db, mockUserService.Object, new PasswordHasher<User>());
            var httpContext = new DefaultHttpContext();
            var tempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());
            controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
            controller.TempData = tempData;

            var supplier = new Supplier
            {
                Name = "MedTech Solutions",
                Type = 3,
                FocalPointName = "Fadi Haddad",
                FocalPointPosition = "Lead Field Engineer",
                FocalPointPhone1 = "+961 1 555666 Ext 201",
                FocalPointPhone2 = "+961 70 998877",
                FocalPointEmail = "fadi@medtech.com"
            };

            // Act 1: Create
            var createResult = await controller.CreateSupplier(supplier, new[] { "IT", "BIOMEDICAL" });
            Assert.IsType<RedirectToActionResult>(createResult);

            var saved = await db.Suppliers.FirstOrDefaultAsync(s => s.Name == "MedTech Solutions");
            Assert.NotNull(saved);
            Assert.Equal("Fadi Haddad", saved.FocalPointName);
            Assert.Equal("Lead Field Engineer", saved.FocalPointPosition);
            Assert.Equal("+961 1 555666 Ext 201", saved.FocalPointPhone1);
            Assert.Equal("+961 70 998877", saved.FocalPointPhone2);
            Assert.Equal("fadi@medtech.com", saved.FocalPointEmail);

            // Act 2: Edit
            var editResult = await controller.EditSupplier(
                saved.Id,
                "MedTech Solutions Intl",
                3,
                new[] { "BIOMEDICAL" },
                true,
                "Karim Nassar",
                "Operations Manager",
                "+961 1 555666 Ext 100",
                "+961 71 112233",
                "karim@medtech.com"
            );
            Assert.IsType<RedirectToActionResult>(editResult);

            var updated = await db.Suppliers.FindAsync(saved.Id);
            Assert.NotNull(updated);
            Assert.Equal("MedTech Solutions Intl", updated.Name);
            Assert.Equal("Karim Nassar", updated.FocalPointName);
            Assert.Equal("Operations Manager", updated.FocalPointPosition);
            Assert.Equal("+961 71 112233", updated.FocalPointPhone2);
            Assert.Equal("karim@medtech.com", updated.FocalPointEmail);
        }

        [Fact]
        public async Task AssetsController_RecordVendorMaintenanceVisit_CreatesVendorMaintenanceLogRow()
        {
            // Arrange
            using var db = GetInMemoryDbContext(Guid.NewGuid().ToString());
            var supplier = new Supplier { Id = 1, Name = "Siemens Healthineers", IsActive = true };
            var asset = new Asset
            {
                Id = 101,
                HospitalNo = "RAD-CT-01",
                Brand = "Siemens",
                MaintenanceTeam = "BIOMEDICAL",
                HasVendorMaintenance = true,
                VendorMaintenanceCount = 4,
                VendorMaintenanceCompletedCount = 0,
                SupplierId = 1
            };
            db.Suppliers.Add(supplier);
            db.Assets.Add(asset);
            await db.SaveChangesAsync();

            var mockUserService = new Mock<ICurrentUserService>();
            mockUserService.Setup(s => s.CanManageAsset(It.IsAny<Asset>())).Returns(true);
            mockUserService.Setup(s => s.GetUserId()).Returns(1);

            var controller = new AssetsController(db, mockUserService.Object);
            var httpContext = new DefaultHttpContext();
            var tempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());
            controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
            controller.TempData = tempData;

            // Act
            var result = await controller.RecordVendorMaintenanceVisit(
                asset.Id,
                new DateTime(2026, 11, 20),
                supplierId: null,
                engineerName: "Eng. Samir Kabbara",
                engineerPhone: "+961 3 445566",
                serviceReportNumber: "SR-2026-0089",
                notes: "Complete gantry and detector calibration completed successfully.",
                status: "Completed"
            );

            // Assert
            Assert.IsType<RedirectToActionResult>(result);

            var log = await db.VendorMaintenanceLogs.FirstOrDefaultAsync(l => l.AssetId == 101);
            Assert.NotNull(log);
            Assert.Equal(1, log.VisitNumber);
            Assert.Equal("Eng. Samir Kabbara", log.EngineerName);
            Assert.Equal("+961 3 445566", log.EngineerPhone);
            Assert.Equal("SR-2026-0089", log.ServiceReportNumber);
            Assert.Equal("Completed", log.Status);
            Assert.Equal(1, log.SupplierId);

            var updatedAsset = await db.Assets.FindAsync(101);
            Assert.NotNull(updatedAsset);
            Assert.Equal(1, updatedAsset.VendorMaintenanceCompletedCount);
            Assert.Single(updatedAsset.VendorVisitsList);
        }

        [Fact]
        public async Task AssetsController_VendorMaintenanceLogs_ReturnsFilteredResults()
        {
            // Arrange
            using var db = GetInMemoryDbContext(Guid.NewGuid().ToString());
            var s1 = new Supplier { Id = 1, Name = "Dell Technologies", IsActive = true };
            var s2 = new Supplier { Id = 2, Name = "Philips Healthcare", IsActive = true };
            db.Suppliers.AddRange(s1, s2);

            var a1 = new Asset { Id = 1, HospitalNo = "IT-PC-01", MaintenanceTeam = "IT", SupplierId = 1 };
            var a2 = new Asset { Id = 2, HospitalNo = "BIO-ECG-01", MaintenanceTeam = "BIOMEDICAL", SupplierId = 2 };
            db.Assets.AddRange(a1, a2);

            db.VendorMaintenanceLogs.AddRange(
                new VendorMaintenanceLog { Id = 1, AssetId = 1, SupplierId = 1, VisitNumber = 1, VisitDate = DateTime.Today, EngineerName = "Hadi IT", Status = "Completed" },
                new VendorMaintenanceLog { Id = 2, AssetId = 2, SupplierId = 2, VisitNumber = 1, VisitDate = DateTime.Today, EngineerName = "Omar Bio", Status = "Pending Parts" }
            );
            await db.SaveChangesAsync();

            var mockUserService = new Mock<ICurrentUserService>();
            mockUserService.Setup(s => s.IsAdmin()).Returns(true);
            mockUserService.Setup(s => s.GetMaintenanceTeam()).Returns("IT");

            var controller = new AssetsController(db, mockUserService.Object);
            var httpContext = new DefaultHttpContext();
            controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

            // Act 1: Filter by BIOMEDICAL team
            var resultBio = await controller.VendorMaintenanceLogs(team: "BIOMEDICAL");
            var viewBio = Assert.IsType<ViewResult>(resultBio);
            var modelBio = Assert.IsAssignableFrom<IEnumerable<VendorMaintenanceLog>>(viewBio.Model);
            Assert.Single(modelBio);
            Assert.Equal("Omar Bio", modelBio.First().EngineerName);

            // Act 2: Filter by Keyword Search
            var resultSearch = await controller.VendorMaintenanceLogs(search: "Hadi");
            var viewSearch = Assert.IsType<ViewResult>(resultSearch);
            var modelSearch = Assert.IsAssignableFrom<IEnumerable<VendorMaintenanceLog>>(viewSearch.Model);
            Assert.Single(modelSearch);
            Assert.Equal("Hadi IT", modelSearch.First().EngineerName);
        }

        [Fact]
        public async Task AssetsController_RecordVendorMaintenanceVisit_WithCustomServicingVendor_Succeeds()
        {
            // Arrange
            using var db = GetInMemoryDbContext(Guid.NewGuid().ToString());
            var sourceSupplier = new Supplier { Id = 1, Name = "Source Vendor Corp", IsActive = true };
            var servicingSupplier = new Supplier { Id = 2, Name = "Local Service Agent LLC", IsActive = true };
            db.Suppliers.AddRange(sourceSupplier, servicingSupplier);

            var asset = new Asset
            {
                Id = 202,
                HospitalNo = "RAD-MRI-02",
                MaintenanceTeam = "BIOMEDICAL",
                SupplierId = 1,
                HasVendorMaintenance = true,
                VendorMaintenanceCount = 4
            };
            db.Assets.Add(asset);
            await db.SaveChangesAsync();

            var mockUserService = new Mock<ICurrentUserService>();
            mockUserService.Setup(s => s.CanManageAsset(It.IsAny<Asset>())).Returns(true);
            mockUserService.Setup(s => s.GetUserId()).Returns(1);

            var controller = new AssetsController(db, mockUserService.Object);
            var httpContext = new DefaultHttpContext();
            var tempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());
            controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
            controller.TempData = tempData;

            // Act: select custom vendor (Id = 2) instead of source vendor (Id = 1)
            var result = await controller.RecordVendorMaintenanceVisit(
                asset.Id,
                new DateTime(2026, 11, 21),
                supplierId: 2,
                engineerName: "Eng. Rami",
                engineerPhone: "+961 71 889900",
                serviceReportNumber: "SR-CUSTOM-01",
                notes: "Scheduled after-sale visit performed by local maintenance contractor.",
                status: "Completed"
            );

            // Assert
            Assert.IsType<RedirectToActionResult>(result);
            var log = await db.VendorMaintenanceLogs.FirstOrDefaultAsync(l => l.AssetId == 202);
            Assert.NotNull(log);
            Assert.Equal(2, log.SupplierId);
            Assert.Equal("Eng. Rami", log.EngineerName);

            var updatedAsset = await db.Assets.FindAsync(202);
            Assert.NotNull(updatedAsset);
            Assert.Equal(2, updatedAsset.VendorMaintenanceSupplierId);
            Assert.Equal(1, updatedAsset.VendorMaintenanceCompletedCount);
        }

        [Fact]
        public void NtlmAuthHelper_CreateType2Challenge_ReturnsValidStructure()
        {
            var challenge = NtlmAuthHelper.CreateType2Challenge();
            Assert.NotNull(challenge);
            Assert.True(challenge.Length >= 48);
            Assert.Equal("NTLMSSP", System.Text.Encoding.ASCII.GetString(challenge, 0, 7));
            Assert.Equal(2, BitConverter.ToInt32(challenge, 8)); // MessageType = 2
        }

        [Fact]
        public void NtlmAuthHelper_ParseType3_ExtractsUsernameAndWorkstation()
        {
            // Build synthetic NTLM Type 3 message
            byte[] msg = new byte[120];
            System.Text.Encoding.ASCII.GetBytes("NTLMSSP\0").CopyTo(msg, 0);
            BitConverter.GetBytes(3).CopyTo(msg, 8); // MessageType = 3

            string domain = "WORKGROUP";
            string user = "abbas";
            string ws = "MYPC";

            byte[] domainBytes = System.Text.Encoding.Unicode.GetBytes(domain);
            byte[] userBytes = System.Text.Encoding.Unicode.GetBytes(user);
            byte[] wsBytes = System.Text.Encoding.Unicode.GetBytes(ws);

            int currentOffset = 64;

            // Domain
            BitConverter.GetBytes((ushort)domainBytes.Length).CopyTo(msg, 28);
            BitConverter.GetBytes((ushort)domainBytes.Length).CopyTo(msg, 30);
            BitConverter.GetBytes(currentOffset).CopyTo(msg, 32);
            domainBytes.CopyTo(msg, currentOffset);
            currentOffset += domainBytes.Length;

            // User
            BitConverter.GetBytes((ushort)userBytes.Length).CopyTo(msg, 36);
            BitConverter.GetBytes((ushort)userBytes.Length).CopyTo(msg, 38);
            BitConverter.GetBytes(currentOffset).CopyTo(msg, 40);
            userBytes.CopyTo(msg, currentOffset);
            currentOffset += userBytes.Length;

            // Workstation
            BitConverter.GetBytes((ushort)wsBytes.Length).CopyTo(msg, 44);
            BitConverter.GetBytes((ushort)wsBytes.Length).CopyTo(msg, 46);
            BitConverter.GetBytes(currentOffset).CopyTo(msg, 48);
            wsBytes.CopyTo(msg, currentOffset);

            // Flags (Unicode = 0x01)
            BitConverter.GetBytes((uint)0x00000001).CopyTo(msg, 60);

            var (parsedUser, parsedWs, parsedDomain) = NtlmAuthHelper.ParseType3(msg);
            Assert.Equal("abbas", parsedUser);
            Assert.Equal("MYPC", parsedWs);
            Assert.Equal("WORKGROUP", parsedDomain);
        }

        [Fact]
        public async Task AccountController_AutoLogin_Returns401Challenge_WhenFirstHitWithoutIdentity()
        {
            using var db = GetInMemoryDbContext(Guid.NewGuid().ToString());
            var mockUserService = new Mock<ICurrentUserService>();
            var config = CreateConfig(windowsAuth: true, passwordAuth: true);

            var controller = new AccountController(db, mockUserService.Object, config);
            var httpContext = new DefaultHttpContext();
            httpContext.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("192.168.100.50");
            controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

            var result = await controller.AutoLogin();

            var statusResult = Assert.IsType<StatusCodeResult>(result);
            Assert.Equal(401, statusResult.StatusCode);
            Assert.True(httpContext.Response.Headers.ContainsKey("WWW-Authenticate"));
        }

        [Fact]
        public async Task AccountController_AutoLogin_IgnoresUntrustedClientCookieAndIssues401Challenge()
        {
            using var db = GetInMemoryDbContext(Guid.NewGuid().ToString());
            var mockUserService = new Mock<ICurrentUserService>();
            var config = CreateConfig(windowsAuth: true, passwordAuth: true);

            var controller = new AccountController(db, mockUserService.Object, config);
            var httpContext = new DefaultHttpContext();
            httpContext.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("192.168.100.50");
            httpContext.Request.Headers["Cookie"] = "SOLVIS.WorkstationUser=fakeadmin";
            controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

            var result = await controller.AutoLogin();

            // Should challenge with 401 instead of trusting cookie
            var statusResult = Assert.IsType<StatusCodeResult>(result);
            Assert.Equal(401, statusResult.StatusCode);
            Assert.True(httpContext.Response.Headers.ContainsKey("WWW-Authenticate"));
        }

        [Fact]
        public async Task AccountController_AutoLogin_AuthenticatesViaNtlmType3Message()
        {
            using var db = GetInMemoryDbContext(Guid.NewGuid().ToString());
            var mockUserService = new Mock<ICurrentUserService>();
            var config = CreateConfig(windowsAuth: true, passwordAuth: true);

            var controller = new AccountController(db, mockUserService.Object, config);
            var httpContext = new DefaultHttpContext();
            httpContext.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("192.168.100.50");

            // Build minimal NTLM Type 3 message containing user "john.doe"
            byte[] msg = new byte[80];
            System.Text.Encoding.ASCII.GetBytes("NTLMSSP\0").CopyTo(msg, 0);
            BitConverter.GetBytes(3).CopyTo(msg, 8); // Type 3
            byte[] userBytes = System.Text.Encoding.Unicode.GetBytes("john.doe");
            ushort userLen = (ushort)userBytes.Length;
            BitConverter.GetBytes(userLen).CopyTo(msg, 36);
            BitConverter.GetBytes(userLen).CopyTo(msg, 38);
            BitConverter.GetBytes(52).CopyTo(msg, 40); // User offset
            userBytes.CopyTo(msg, 52);

            string token = Convert.ToBase64String(msg);
            httpContext.Request.Headers["Authorization"] = "NTLM " + token;
            controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

            var result = await controller.AutoLogin();

            // Extracts "john.doe" from NTLM token, finds not in DB -> NotFound AccessDenied
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("AccessDenied", redirectResult.ActionName);
            Assert.Equal("NotFound", redirectResult.RouteValues?["reason"]);
            Assert.Equal("john.doe", redirectResult.RouteValues?["username"]);
        }

        [Fact]
        public async Task AccountController_AutoLogin_RedirectsToNoWindowsIdentity_WhenNoIdentityDetected()
        {
            using var db = GetInMemoryDbContext(Guid.NewGuid().ToString());
            var mockUserService = new Mock<ICurrentUserService>();
            var config = CreateConfig(windowsAuth: true, passwordAuth: true);

            var controller = new AccountController(db, mockUserService.Object, config);
            var httpContext = new DefaultHttpContext();
            httpContext.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("192.168.100.50");
            // Client sent an unrecognized authorization header that yielded no user
            httpContext.Request.Headers["Authorization"] = "NTLM dW5yZWNvZ25pemVk";
            controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

            var result = await controller.AutoLogin();

            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("AccessDenied", redirectResult.ActionName);
            Assert.Equal("NoWindowsIdentity", redirectResult.RouteValues?["reason"]);
            Assert.Equal("Unspecified", redirectResult.RouteValues?["username"]);
        }
    }
}
