using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Moq;
using StockPortal.Controllers;
using StockPortal.Data;
using StockPortal.Middleware;
using StockPortal.Models;
using StockPortal.Services;
using Xunit;

namespace StockPortal.Tests
{
    public class ProductionSecurityAndQualityTests
    {
        private StockDbContext CreateInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<StockDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            return new StockDbContext(options);
        }

        private Mock<ICurrentUserService> CreateMockUserService(string role = SystemRoles.Admin, string? team = null, int userId = 1)
        {
            var mock = new Mock<ICurrentUserService>();
            var user = new User
            {
                Id = userId,
                FullName = "Security Test User",
                DomainUser = "test.user",
                Role = role,
                MaintenanceTeam = team,
                IsActive = true
            };

            mock.Setup(s => s.GetCurrentUser()).Returns(user);
            mock.Setup(s => s.GetUserId()).Returns(userId);
            mock.Setup(s => s.GetFullName()).Returns(user.FullName);
            mock.Setup(s => s.GetDomainUser()).Returns(user.DomainUser);
            mock.Setup(s => s.GetRole()).Returns(role);
            mock.Setup(s => s.GetMaintenanceTeam()).Returns(team);
            mock.Setup(s => s.IsAdmin()).Returns(role == SystemRoles.Admin);
            mock.Setup(s => s.IsMaintenance()).Returns(role == SystemRoles.Maintenance);
            mock.Setup(s => s.IsEndUser()).Returns(role == SystemRoles.EndUser);
            mock.Setup(s => s.CanManageAssets()).Returns(role == SystemRoles.Admin || role == SystemRoles.Maintenance);
            mock.Setup(s => s.CanManageUsers()).Returns(role == SystemRoles.Admin);
            mock.Setup(s => s.CanAccessDashboard()).Returns(role == SystemRoles.Admin || role == SystemRoles.Maintenance);
            mock.Setup(s => s.CanSwitchProfile()).Returns(role == SystemRoles.Admin);
            mock.Setup(s => s.CanAccessTicket(It.IsAny<Ticket>())).Returns(true);
            mock.Setup(s => s.CanManageTicket(It.IsAny<Ticket>())).Returns(role == SystemRoles.Admin || role == SystemRoles.Maintenance);

            return mock;
        }

        // ========================================================
        // 1. FILE UPLOAD SECURITY TESTS
        // ========================================================

        [Theory]
        [InlineData("malicious.exe", "application/octet-stream")]
        [InlineData("exploit.svg", "image/svg+xml")]
        [InlineData("webshell.cshtml", "text/plain")]
        [InlineData("script.bat", "application/x-bat")]
        [InlineData("payload.dll", "application/x-msdownload")]
        public async Task FileUpload_RejectsDangerousOrNonWhitelistedExtensions(string fileName, string contentType)
        {
            using var context = CreateInMemoryDbContext();
            var userService = CreateMockUserService();
            var notifService = new Mock<INotificationService>();
            var controller = new TicketsController(context, userService.Object, notifService.Object);
            var httpContext = new DefaultHttpContext();
            controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
            controller.TempData = new Microsoft.AspNetCore.Mvc.ViewFeatures.TempDataDictionary(httpContext, Mock.Of<Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataProvider>());

            var ticket = new Ticket
            {
                Id = 1,
                OpenedBy = 1,
                Type = "Hardware",
                StatusId = 1
            };
            context.Tickets.Add(ticket);
            await context.SaveChangesAsync();

            var fileMock = new Mock<IFormFile>();
            var content = "dummy file content payload";
            var ms = new MemoryStream(Encoding.UTF8.GetBytes(content));
            fileMock.Setup(f => f.FileName).Returns(fileName);
            fileMock.Setup(f => f.ContentType).Returns(contentType);
            fileMock.Setup(f => f.Length).Returns(ms.Length);
            fileMock.Setup(f => f.OpenReadStream()).Returns(ms);

            var files = new List<IFormFile> { fileMock.Object };

            await controller.AddMessage(1, 1, "Testing dangerous upload", pastedScreenshotsJson: null, screenshotFiles: files);

            // Verify the attachment was NOT stored
            var attachments = await context.TicketAttachments.Where(a => a.TicketId == 1).ToListAsync();
            Assert.Empty(attachments);
        }

        [Fact]
        public async Task FileUpload_RejectsOversizedAttachments()
        {
            using var context = CreateInMemoryDbContext();
            var userService = CreateMockUserService();
            var notifService = new Mock<INotificationService>();
            var controller = new TicketsController(context, userService.Object, notifService.Object);
            var httpContext = new DefaultHttpContext();
            controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
            controller.TempData = new Microsoft.AspNetCore.Mvc.ViewFeatures.TempDataDictionary(httpContext, Mock.Of<Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataProvider>());

            var ticket = new Ticket
            {
                Id = 2,
                OpenedBy = 1,
                Type = "Hardware",
                StatusId = 1
            };
            context.Tickets.Add(ticket);
            await context.SaveChangesAsync();

            var fileMock = new Mock<IFormFile>();
            fileMock.Setup(f => f.FileName).Returns("large_image.png");
            fileMock.Setup(f => f.ContentType).Returns("image/png");
            // Set length to 15 MB (over the 10 MB threshold)
            fileMock.Setup(f => f.Length).Returns(15 * 1024 * 1024);

            var files = new List<IFormFile> { fileMock.Object };

            await controller.AddMessage(2, 1, "Testing oversized upload", pastedScreenshotsJson: null, screenshotFiles: files);

            var attachments = await context.TicketAttachments.Where(a => a.TicketId == 2).ToListAsync();
            Assert.Empty(attachments);
        }

        [Theory]
        [InlineData("valid_screenshot.png", "image/png")]
        [InlineData("valid_photo.jpg", "image/jpeg")]
        [InlineData("valid_diag.webp", "image/webp")]
        public async Task FileUpload_AcceptsWhitelistedImageExtensions(string fileName, string contentType)
        {
            using var context = CreateInMemoryDbContext();
            var userService = CreateMockUserService();
            var notifService = new Mock<INotificationService>();
            var controller = new TicketsController(context, userService.Object, notifService.Object);
            var httpContext = new DefaultHttpContext();
            controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
            controller.TempData = new Microsoft.AspNetCore.Mvc.ViewFeatures.TempDataDictionary(httpContext, Mock.Of<Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataProvider>());

            var ticket = new Ticket
            {
                Id = 3,
                OpenedBy = 1,
                Type = "Hardware",
                StatusId = 1
            };
            context.Tickets.Add(ticket);
            await context.SaveChangesAsync();

            var fileMock = new Mock<IFormFile>();
            var bytes = new byte[] { 0x89, 0x50, 0x4E, 0x47 }; // PNG magic bytes
            var ms = new MemoryStream(bytes);
            fileMock.Setup(f => f.FileName).Returns(fileName);
            fileMock.Setup(f => f.ContentType).Returns(contentType);
            fileMock.Setup(f => f.Length).Returns(ms.Length);
            fileMock.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), It.IsAny<System.Threading.CancellationToken>()))
                .Returns(Task.CompletedTask);

            var files = new List<IFormFile> { fileMock.Object };

            await controller.AddMessage(3, 1, "Testing valid upload", pastedScreenshotsJson: null, screenshotFiles: files);

            var attachments = await context.TicketAttachments.Where(a => a.TicketId == 3).ToListAsync();
            Assert.Single(attachments);
            Assert.Equal(fileName, attachments[0].FileName);
        }

        // ========================================================
        // 2. CSRF ANTI-FORGERY TOKEN ATTRIBUTES
        // ========================================================

        [Fact]
        public void QuickCreate_HasHttpPostAndValidateAntiForgeryToken()
        {
            var method = typeof(AssetCategoriesController).GetMethod("QuickCreate");
            Assert.NotNull(method);
            Assert.NotNull(method.GetCustomAttribute<HttpPostAttribute>());
            Assert.NotNull(method.GetCustomAttribute<ValidateAntiForgeryTokenAttribute>());
        }

        [Fact]
        public void DismissNotification_HasHttpPostAndValidateAntiForgeryToken()
        {
            var method = typeof(NotificationsController).GetMethod("Dismiss");
            Assert.NotNull(method);
            Assert.NotNull(method.GetCustomAttribute<HttpPostAttribute>());
            Assert.NotNull(method.GetCustomAttribute<ValidateAntiForgeryTokenAttribute>());
        }

        [Fact]
        public void EditSupplier_HasHttpPostAndValidateAntiForgeryToken()
        {
            var method = typeof(DirectoryController).GetMethod("EditSupplier");
            Assert.NotNull(method);
            Assert.NotNull(method.GetCustomAttribute<HttpPostAttribute>());
            Assert.NotNull(method.GetCustomAttribute<ValidateAntiForgeryTokenAttribute>());
        }

        // ========================================================
        // 3. HOME CONTROLLER PRIVACY VIEW ACTION
        // ========================================================

        [Fact]
        public void HomeController_Privacy_ReturnsViewResult()
        {
            using var context = CreateInMemoryDbContext();
            var userService = CreateMockUserService();
            var controller = new HomeController(context, userService.Object);

            var result = controller.Privacy();

            Assert.IsType<ViewResult>(result);
        }

        // ========================================================
        // 4. DIRECTORY EDIT SUPPLIER TAB RETENTION
        // ========================================================

        [Fact]
        public async Task DirectoryController_EditSupplier_RetainsSuppliersTab()
        {
            using var context = CreateInMemoryDbContext();
            var supplier = new Supplier
            {
                Id = 1,
                Name = "Initial Supplier",
                Type = 1,
                IsActive = true
            };
            context.Suppliers.Add(supplier);

            // Add Admin user for live DB handshake
            context.Users.Add(new User
            {
                Id = 1,
                DomainUser = "admin",
                FullName = "Admin",
                Role = SystemRoles.Admin,
                IsActive = true
            });
            await context.SaveChangesAsync();

            var userService = CreateMockUserService(SystemRoles.Admin, null, 1);
            var controller = new DirectoryController(context, userService.Object);

            var httpContext = new DefaultHttpContext();
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "1"),
                new Claim(ClaimTypes.Name, "Admin"),
                new Claim(ClaimTypes.Role, SystemRoles.Admin)
            };
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
            controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
            controller.TempData = new Microsoft.AspNetCore.Mvc.ViewFeatures.TempDataDictionary(httpContext, Mock.Of<Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataProvider>());

            var result = await controller.EditSupplier(1, "Updated Supplier", 2, new[] { "IT" }, true);

            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
            Assert.NotNull(redirectResult.RouteValues);
            Assert.Equal("suppliers", redirectResult.RouteValues["tab"]);
        }

        // ========================================================
        // 5. DOMAIN AUTH MIDDLEWARE HEADER SPOOFING GUARDS
        // ========================================================

        [Fact]
        public async Task DomainAuthMiddleware_IgnoresSpoofedProxyHeaders_WhenTrustProxyHeadersIsFalse()
        {
            using var context = CreateInMemoryDbContext();
            context.Users.Add(new User
            {
                Id = 10,
                DomainUser = "target.user",
                FullName = "Target User",
                Role = SystemRoles.Admin,
                IsActive = true
            });
            await context.SaveChangesAsync();

            var nextCalled = false;
            RequestDelegate next = (ctx) =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            };

            var middleware = new DomainAuthMiddleware(next);
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Path = "/Tickets";
            // Client attempts to spoof header from an external IP
            httpContext.Request.Headers["X-Remote-User"] = "target.user";
            httpContext.Connection.RemoteIpAddress = IPAddress.Parse("192.168.1.50");

            var configMock = new Mock<IConfiguration>();
            var configSectionMock = new Mock<IConfigurationSection>();
            configSectionMock.Setup(s => s.Value).Returns("false");
            configMock.Setup(c => c.GetSection("Authentication:TrustProxyHeaders")).Returns(configSectionMock.Object);

            var envMock = new Mock<IHostEnvironment>();
            envMock.Setup(e => e.EnvironmentName).Returns("Production");

            await middleware.InvokeAsync(httpContext, context, configMock.Object, envMock.Object);

            // In production with untrusted headers and no Windows Auth, spoofed header is ignored and access is denied
            Assert.False(nextCalled);
            Assert.Equal(302, httpContext.Response.StatusCode); // Redirects to AccessDenied
        }

        [Fact]
        public async Task DomainAuthMiddleware_HonorsProxyHeaders_WhenExplicitlyTrustedAndLoopback()
        {
            using var context = CreateInMemoryDbContext();
            context.Users.Add(new User
            {
                Id = 11,
                DomainUser = "trusted.user",
                FullName = "Trusted User",
                Role = SystemRoles.Admin,
                IsActive = true
            });
            await context.SaveChangesAsync();

            var nextCalled = false;
            RequestDelegate next = (ctx) =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            };

            var middleware = new DomainAuthMiddleware(next);
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Path = "/Tickets";
            httpContext.Request.Headers["X-Remote-User"] = "trusted.user";
            httpContext.Connection.RemoteIpAddress = IPAddress.Loopback;

            var inMemoryConfig = new Dictionary<string, string?>
            {
                { "Authentication:TrustProxyHeaders", "true" }
            };
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(inMemoryConfig).Build();

            var envMock = new Mock<IHostEnvironment>();
            envMock.Setup(e => e.EnvironmentName).Returns("Production");

            await middleware.InvokeAsync(httpContext, context, configuration, envMock.Object);

            Assert.True(nextCalled);
            Assert.NotNull(httpContext.Items["CurrentUser"]);
            var matched = (User)httpContext.Items["CurrentUser"]!;
            Assert.Equal("trusted.user", matched.DomainUser);
        }

        [Fact]
        public async Task SubmitAccessTicket_CreatesTicketWithValidTargetTeam_NeverNull()
        {
            using var context = CreateInMemoryDbContext();
            var user = new User { Id = 1, FullName = "Access Applicant", DomainUser = "access.user", IsActive = true };
            var asset = new Asset { Id = 10, HospitalNo = "PC-LAB-01", MaintenanceTeam = MaintenanceTeams.Biomedical };
            context.Users.Add(user);
            context.Assets.Add(asset);
            await context.SaveChangesAsync();

            var mockUserService = CreateMockUserService();
            var controller = new AccountController(context, mockUserService.Object);
            controller.TempData = new Microsoft.AspNetCore.Mvc.ViewFeatures.TempDataDictionary(
                new DefaultHttpContext(),
                Mock.Of<Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataProvider>()
            );
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };

            var result = await controller.SubmitAccessTicket(pcName: "PC-LAB-01", assetId: 10, roomId: null, notes: "Cannot login to station", username: "access.user");

            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("AccessDenied", redirectResult.ActionName);

            var createdTicket = await context.Tickets.OrderByDescending(t => t.Id).FirstOrDefaultAsync();
            Assert.NotNull(createdTicket);
            Assert.False(string.IsNullOrWhiteSpace(createdTicket.TargetTeam));
            Assert.Equal(MaintenanceTeams.Biomedical, createdTicket.TargetTeam);
            Assert.Equal("PC-LAB-01", createdTicket.PcName);
        }

        [Fact]
        public async Task StockDbContext_EnforcesDefaultTargetTeam_WhenTicketCreatedWithoutTargetTeam()
        {
            using var context = CreateInMemoryDbContext();
            var ticket = new Ticket
            {
                Type = "Hardware",
                StatusId = 1,
                OpenedBy = 1,
                TargetTeam = null! // Explicitly test null assignment guard
            };

            context.Tickets.Add(ticket);
            await context.SaveChangesAsync();

            var savedTicket = await context.Tickets.FindAsync(ticket.Id);
            Assert.NotNull(savedTicket);
            Assert.False(string.IsNullOrWhiteSpace(savedTicket.TargetTeam));
            Assert.Equal(MaintenanceTeams.IT, savedTicket.TargetTeam);
        }
    }
}