using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using DocMd.Site.Models.SetupViewModels;
using DocMd.Site.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using DocMd.Site.Services;
using System.Text;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;
using System.IO;

namespace DocMd.Site.Controllers
{
    [Authorize]
    public class SetupController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IHostingEnvironment _hostingEnvironment;
        private readonly IEmailSender _emailSender;
        private readonly ISmsSender _smsSender;
        private readonly ILogger _logger;

        public SetupController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            RoleManager<IdentityRole> roleManager,
            IHostingEnvironment hostingEnvironment,
            IEmailSender emailSender,
            ISmsSender smsSender,
            ILoggerFactory loggerFactory)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _hostingEnvironment = hostingEnvironment;
            _emailSender = emailSender;
            _smsSender = smsSender;
            _logger = loggerFactory.CreateLogger<AccountController>();
        }

        //
        // GET: /Setup
        [AllowAnonymous]
        public async Task<IActionResult> Index()
        {
            var role = await _roleManager.FindByNameAsync("Administrators");

            if (role == null || role.Users.Count == 0)
            {
                var secret = ByteArrayToString(Guid.NewGuid().ToByteArray());
                var url = $"{Request.Scheme}://{Request.Host}/api/hook/";

                ViewBag.Secret = secret;
                ViewBag.Url = url;

                return View();
            }

            return RedirectToAction(nameof(ContentController.Index), "Content");
        }

        //
        // POST: /Setup/User
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> User(AccountViewModel model)
        {
            if (ModelState.IsValid)
            {
                var role = await _roleManager.FindByNameAsync("Administrators");

                if (role == null)
                {
                    var roleResult = await _roleManager.CreateAsync(new IdentityRole("Administrators"));

                    if (roleResult.Succeeded)
                    {
                        _logger.LogInformation(3, "Administrators role created.");
                        role = await _roleManager.FindByNameAsync("Administrators");
                    }
                }

                var user = new ApplicationUser { UserName = model.Email, Email = model.Email };
                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(user, "Administrators");

                    // For more information on how to enable account confirmation and password reset please visit https://go.microsoft.com/fwlink/?LinkID=532713
                    // Send an email with this link
                    //var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                    //var callbackUrl = Url.Action(nameof(ConfirmEmail), "Account", new { userId = user.Id, code = code }, protocol: HttpContext.Request.Scheme);
                    //await _emailSender.SendEmailAsync(model.Email, "Confirm your account",
                    //    $"Please confirm your account by clicking this link: <a href='{callbackUrl}'>link</a>");

                    await _signInManager.SignInAsync(user, isPersistent: true);
                    _logger.LogInformation(3, "Admin user created a new account with password.");

                    return RedirectToAction(nameof(Hook));
                }

                AddErrors(result);
            }

            // If we got this far, something failed, redisplay form
            return View("Index", model);
        }

        //
        // GET: /Setup/Hook
        public IActionResult Hook()
        {
            var secret = ByteArrayToString(Guid.NewGuid().ToByteArray());
            var url = $"{Request.Scheme}://{Request.Host}/api/hook/";

            ViewBag.Secret = secret;
            ViewBag.Url = url;

            return View();
        }

        //
        // POST: /Setup/Complete
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Complete(HookViewModel model)
        {
            if (ModelState.IsValid)
            {
                List<Shared.WebHook> webHooks = null;
                var webHooksFilePath = Path.Combine(_hostingEnvironment.ContentRootPath, "webhooks.json");

                if (System.IO.File.Exists(webHooksFilePath))
                {
                    webHooks = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Shared.WebHook>>(
                        System.IO.File.ReadAllText(webHooksFilePath));
                }
                else
                {
                    webHooks = new List<Shared.WebHook>();
                }

                webHooks.Add(new Shared.WebHook() {
                    PersonalAccessToken= model.PersonalAccessToken,
                    RemoteUrl = model.RepositoryUrl,
                    Secret = model.Secret
                });

                System.IO.File.WriteAllText(webHooksFilePath, Newtonsoft.Json.JsonConvert.SerializeObject(webHooks));

                return RedirectToAction(nameof(ContentController.Index), "Content");
            }

            return View("Hook");
        }

        public static string ByteArrayToString(byte[] ba)
        {
            StringBuilder hex = new StringBuilder(ba.Length * 2);
            foreach (byte b in ba)
                hex.AppendFormat("{0:x2}", b);
            return hex.ToString();
        }

        #region Helpers

        private void AddErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }

        #endregion
    }
}