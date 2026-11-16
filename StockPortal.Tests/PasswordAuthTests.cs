using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
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
    public class PasswordAuthTests
    {
        private StockDbContext GetInMemoryDbContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<StockDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;

            var context = new StockDbContext(options);

            if (!context.Users.Any())
            {
                var hasher = new PasswordHasher<User>();
                var admin = new User { Id = 1, DomainUser = "admin", FullName = "System Admin", Role = SystemRoles.Admin, IsActive = true };
                admin.PasswordHash = hasher.HashPassword(admin, "Admin@123");

                var tech = new User { Id = 2, DomainUser = "hOuta", FullName = "Hasan Outa", Role = SystemRoles.Maintenance, MaintenanceTeam = "IT", IsActive = true };
                tech.PasswordHash = hasher.HashPassword(tech, "Solvis@2026");

                var inactive = new User { Id = 3, DomainUser = "inactive.user", FullName = "Inactive User", Role = SystemRoles.EndUser, IsActive = false };
                inactive.PasswordHash = hasher.HashPassword(inactive, "Pass@123");

                var noPassUser = new User { Id = 4, DomainUser = "nopass.user", FullName = "No Password User", Role = SystemRoles.EndUser, IsActive = true };

                context.Users.AddRange(admin, tech, inactive, noPassUser);
                context.SaveChanges();
            }

            return context;
        }

        private AccountController CreateAccountController(StockDbContext db, ICurrentUserService userService, User? currentUser = null)
        {
            var httpContext = new DefaultHttpContext();
            var authServiceMock = new Mock<IAuthenticationService>();
            authServiceMock
                .Setup(a => a.SignInAsync(It.IsAny<HttpContext>(), It.IsAny<string>(), It.IsAny<ClaimsPrincipal>(), It.IsAny<AuthenticationProperties>()))
                .Returns(Task.CompletedTask);
            authServiceMock
                .Setup(a => a.SignOutAsync(It.IsAny<HttpContext>(), It.IsAny<string>(), It.IsAny<AuthenticationProperties>()))
                .Returns(Task.CompletedTask);

            var serviceProviderMock = new Mock<IServiceProvider>();
            serviceProviderMock.Setup(sp => sp.GetService(typeof(IAuthenticationService))).Returns(authServiceMock.Object);
            httpContext.RequestServices = serviceProviderMock.Object;

            if (currentUser != null)
            {
                httpContext.Items["CurrentUser"] = currentUser;
                httpContext.Items["PhysicalUser"] = currentUser;
                httpContext.Items["IsPhysicalAdmin"] = currentUser.Role == SystemRoles.Admin;
            }

            var mockUrl = new Mock<IUrlHelper>();
            mockUrl.Setup(u => u.IsLocalUrl(It.IsAny<string>())).Returns(false);

            var controller = new AccountController(db, userService)
            {
                ControllerContext = new ControllerContext { HttpContext = httpContext },
                TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>()),
                Url = mockUrl.Object
            };

            return controller;
        }

        private DirectoryController CreateDirectoryController(StockDbContext db, ICurrentUserService userService, User? currentUser = null)
        {
            var httpContext = new DefaultHttpContext();
            if (currentUser != null)
            {
                httpContext.Items["CurrentUser"] = currentUser;
                httpContext.Items["PhysicalUser"] = currentUser;
                httpContext.Items["IsPhysicalAdmin"] = currentUser.Role == SystemRoles.Admin;
            }

            var mockUrl = new Mock<IUrlHelper>();
            mockUrl.Setup(u => u.IsLocalUrl(It.IsAny<string>())).Returns(false);

            var controller = new DirectoryController(db, userService)
            {
                ControllerContext = new ControllerContext { HttpContext = httpContext },
                TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>()),
                Url = mockUrl.Object
            };

            return controller;
        }

        [Fact]
        public void PasswordHasher_HashesAndVerifiesPasswordCorrectly()
        {
            var hasher = new PasswordHasher<User>();
            var user = new User { Id = 10, DomainUser = "testuser" };

            var hash = hasher.HashPassword(user, "SecureP@ssw0rd");

            Assert.NotNull(hash);
            Assert.NotEmpty(hash);
            Assert.NotEqual("SecureP@ssw0rd", hash);

            // Correct password succeeds
            var success = hasher.VerifyHashedPassword(user, hash, "SecureP@ssw0rd");
            Assert.True(success == PasswordVerificationResult.Success || success == PasswordVerificationResult.SuccessRehashNeeded);

            // Incorrect password fails
            var failure = hasher.VerifyHashedPassword(user, hash, "WrongPassword123");
            Assert.Equal(PasswordVerificationResult.Failed, failure);
        }

        [Fact]
        public void Login_GET_ReturnsViewWithLoginViewModel()
        {
            using var db = GetInMemoryDbContext("Login_Get_Test");
            var mockUserService = new Mock<ICurrentUserService>();
            mockUserService.Setup(s => s.GetCurrentUser()).Returns((User?)null);

            var controller = CreateAccountController(db, mockUserService.Object);

            var result = controller.Login("/Tickets") as ViewResult;

            Assert.NotNull(result);
            var model = Assert.IsType<LoginViewModel>(result.Model);
            Assert.Equal("/Tickets", model.ReturnUrl);
        }

        [Fact]
        public async Task Login_POST_ValidCredentials_SignsInAndRedirects()
        {
            using var db = GetInMemoryDbContext("Login_Post_Valid_Test");
            var mockUserService = new Mock<ICurrentUserService>();

            var controller = CreateAccountController(db, mockUserService.Object);

            var model = new LoginViewModel
            {
                Username = "hOuta",
                Password = "Solvis@2026",
                RememberMe = true
            };

            var result = await controller.Login(model) as RedirectToActionResult;

            Assert.NotNull(result);
            Assert.Equal("Index", result.ActionName);
            Assert.Equal("Home", result.ControllerName);
        }

        [Fact]
        public async Task Login_POST_InvalidPassword_ReturnsViewWithError()
        {
            using var db = GetInMemoryDbContext("Login_Post_InvalidPass_Test");
            var mockUserService = new Mock<ICurrentUserService>();

            var controller = CreateAccountController(db, mockUserService.Object);

            var model = new LoginViewModel
            {
                Username = "hOuta",
                Password = "WrongPassword999",
                RememberMe = true
            };

            var result = await controller.Login(model) as ViewResult;

            Assert.NotNull(result);
            Assert.False(controller.ModelState.IsValid);
        }

        [Fact]
        public async Task Login_POST_InactiveUser_ReturnsViewWithError()
        {
            using var db = GetInMemoryDbContext("Login_Post_Inactive_Test");
            var mockUserService = new Mock<ICurrentUserService>();

            var controller = CreateAccountController(db, mockUserService.Object);

            var model = new LoginViewModel
            {
                Username = "inactive.user",
                Password = "Pass@123",
                RememberMe = true
            };

            var result = await controller.Login(model) as ViewResult;

            Assert.NotNull(result);
            Assert.False(controller.ModelState.IsValid);
        }

        [Fact]
        public async Task Login_POST_UserWithNoPassword_ReturnsViewWithError()
        {
            using var db = GetInMemoryDbContext("Login_Post_NoPass_Test");
            var mockUserService = new Mock<ICurrentUserService>();

            var controller = CreateAccountController(db, mockUserService.Object);

            var model = new LoginViewModel
            {
                Username = "nopass.user",
                Password = "AnyPassword",
                RememberMe = true
            };

            var result = await controller.Login(model) as ViewResult;

            Assert.NotNull(result);
            Assert.False(controller.ModelState.IsValid);
        }

        [Fact]
        public async Task ChangePassword_ValidCurrentAndNewPassword_UpdatesPasswordHashInDb()
        {
            using var db = GetInMemoryDbContext("ChangePassword_Success_Test");
            var techUser = db.Users.First(u => u.Id == 2);

            var mockUserService = new Mock<ICurrentUserService>();
            mockUserService.Setup(s => s.GetCurrentUser()).Returns(techUser);

            var controller = CreateAccountController(db, mockUserService.Object, techUser);

            var model = new ChangePasswordViewModel
            {
                CurrentPassword = "Solvis@2026",
                NewPassword = "NewSecretPassword@123",
                ConfirmPassword = "NewSecretPassword@123"
            };

            var result = await controller.ChangePassword(model) as RedirectToActionResult;

            Assert.NotNull(result);
            Assert.Equal("Profile", result.ActionName);
            Assert.Contains("successfully changed", controller.TempData["SuccessMessage"]?.ToString());

            // Verify the new password verifies against updated DB hash
            var hasher = new PasswordHasher<User>();
            var updatedUser = db.Users.First(u => u.Id == 2);
            var verifyResult = hasher.VerifyHashedPassword(updatedUser, updatedUser.PasswordHash!, "NewSecretPassword@123");
            Assert.True(verifyResult == PasswordVerificationResult.Success || verifyResult == PasswordVerificationResult.SuccessRehashNeeded);
        }

        [Fact]
        public async Task ChangePassword_WrongCurrentPassword_FailsWithoutUpdating()
        {
            using var db = GetInMemoryDbContext("ChangePassword_WrongCurrent_Test");
            var techUser = db.Users.First(u => u.Id == 2);
            var originalHash = techUser.PasswordHash;

            var mockUserService = new Mock<ICurrentUserService>();
            mockUserService.Setup(s => s.GetCurrentUser()).Returns(techUser);

            var controller = CreateAccountController(db, mockUserService.Object, techUser);

            var model = new ChangePasswordViewModel
            {
                CurrentPassword = "IncorrectCurrentPassword",
                NewPassword = "NewSecretPassword@123",
                ConfirmPassword = "NewSecretPassword@123"
            };

            var result = await controller.ChangePassword(model) as RedirectToActionResult;

            Assert.NotNull(result);
            Assert.Equal("Profile", result.ActionName);
            Assert.Contains("Current password is incorrect", controller.TempData["ErrorMessage"]?.ToString());

            // Ensure DB hash remains untouched
            var userInDb = db.Users.First(u => u.Id == 2);
            Assert.Equal(originalHash, userInDb.PasswordHash);
        }

        [Fact]
        public async Task ResetPassword_AdminUser_SuccessfullyResetsUserPassword()
        {
            using var db = GetInMemoryDbContext("ResetPassword_Admin_Success_Test");
            var adminUser = db.Users.First(u => u.Id == 1);

            var mockUserService = new Mock<ICurrentUserService>();
            mockUserService.Setup(s => s.GetUserId()).Returns(adminUser.Id);
            mockUserService.Setup(s => s.GetDomainUser()).Returns(adminUser.DomainUser!);
            mockUserService.Setup(s => s.IsAdmin()).Returns(true);
            mockUserService.Setup(s => s.CanManageUsers()).Returns(true);

            var controller = CreateDirectoryController(db, mockUserService.Object, adminUser);

            var result = await controller.ResetPassword(2, "ResetPass@999", "ResetPass@999") as RedirectToActionResult;

            Assert.NotNull(result);
            Assert.Equal("Index", result.ActionName);
            Assert.Contains("successfully reset", controller.TempData["SuccessMessage"]?.ToString());

            // Verify user 2 can now verify with ResetPass@999
            var hasher = new PasswordHasher<User>();
            var targetUser = db.Users.First(u => u.Id == 2);
            var verifyResult = hasher.VerifyHashedPassword(targetUser, targetUser.PasswordHash!, "ResetPass@999");
            Assert.True(verifyResult == PasswordVerificationResult.Success || verifyResult == PasswordVerificationResult.SuccessRehashNeeded);
        }

        [Fact]
        public async Task ResetPassword_NonAdminUser_IsForbidden()
        {
            using var db = GetInMemoryDbContext("ResetPassword_NonAdmin_Test");
            var techUser = db.Users.First(u => u.Id == 2);

            var mockUserService = new Mock<ICurrentUserService>();
            mockUserService.Setup(s => s.GetUserId()).Returns(techUser.Id);
            mockUserService.Setup(s => s.GetDomainUser()).Returns(techUser.DomainUser!);
            mockUserService.Setup(s => s.IsAdmin()).Returns(false);
            mockUserService.Setup(s => s.CanManageUsers()).Returns(false);

            var controller = CreateDirectoryController(db, mockUserService.Object, techUser);

            var result = await controller.ResetPassword(1, "HackedPassword@123", "HackedPassword@123");

            Assert.IsType<ForbidResult>(result);
        }

        [Fact]
        public async Task CreateUser_WithInitialPassword_HashesAndSavesProperly()
        {
            using var db = GetInMemoryDbContext("CreateUser_WithPassword_Test");
            var adminUser = db.Users.First(u => u.Id == 1);

            var mockUserService = new Mock<ICurrentUserService>();
            mockUserService.Setup(s => s.GetUserId()).Returns(adminUser.Id);
            mockUserService.Setup(s => s.GetDomainUser()).Returns(adminUser.DomainUser!);
            mockUserService.Setup(s => s.IsAdmin()).Returns(true);
            mockUserService.Setup(s => s.CanManageUsers()).Returns(true);

            var controller = CreateDirectoryController(db, mockUserService.Object, adminUser);

            var newUser = new User
            {
                FullName = "Dr. Layla Noor",
                DomainUser = "l.noor",
                Role = SystemRoles.EndUser
            };

            var result = await controller.CreateUser(newUser, "LaylaSecure@2026") as RedirectToActionResult;

            Assert.NotNull(result);
            Assert.Equal("Index", result.ActionName);

            var savedUser = db.Users.FirstOrDefault(u => u.DomainUser == "l.noor");
            Assert.NotNull(savedUser);
            Assert.NotNull(savedUser.PasswordHash);
            Assert.NotEqual("LaylaSecure@2026", savedUser.PasswordHash);

            var hasher = new PasswordHasher<User>();
            var verifyResult = hasher.VerifyHashedPassword(savedUser, savedUser.PasswordHash, "LaylaSecure@2026");
            Assert.True(verifyResult == PasswordVerificationResult.Success || verifyResult == PasswordVerificationResult.SuccessRehashNeeded);
        }

        [Fact]
        public async Task Logout_SignsOutAndSetsSignedOutCookie()
        {
            using var db = GetInMemoryDbContext("Logout_Test");
            var mockUserService = new Mock<ICurrentUserService>();

            var controller = CreateAccountController(db, mockUserService.Object);

            var result = await controller.Logout() as RedirectToActionResult;

            Assert.NotNull(result);
            Assert.Equal("Login", result.ActionName);

            // Verify SOLVIS.SignedOut cookie was appended to response
            var cookies = controller.Response.Headers["Set-Cookie"].ToString();
            Assert.Contains("SOLVIS.SignedOut", cookies);
        }
    }
}
