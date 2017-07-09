using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using DocMd.Site.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using DocMd.Site;
using System.IO;

namespace Site.Controllers
{
    [Authorize]
    public class SecurityController : Controller
    {
        private readonly ContentOptions _contentOptions;

        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public SecurityController(
            IOptions<ContentOptions> contentOptions,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            _contentOptions = contentOptions.Value;

            _userManager = userManager;
            _roleManager = roleManager;
        }

        // GET: Security
        public ActionResult Index()
        {
            if (!string.IsNullOrWhiteSpace(_contentOptions.Layout) &&
                   System.IO.File.Exists(Path.Combine(_contentOptions.HtmlPath, _contentOptions.Layout)))
            {
                ViewBag.Layout = $"~/{_contentOptions.HtmlPath.Replace(_contentOptions.BasePath, "")}/{_contentOptions.Layout}".Replace("\\", "/");
            }
            else
            {
                ViewBag.Layout = "~/Views/Shared/_Layout.cshtml";
            }

            return View();
        }

        public ActionResult Roles()
        {
            if (!string.IsNullOrWhiteSpace(_contentOptions.Layout) &&
                      System.IO.File.Exists(Path.Combine(_contentOptions.HtmlPath, _contentOptions.Layout)))
            {
                ViewBag.Layout = $"~/{_contentOptions.HtmlPath.Replace(_contentOptions.BasePath, "")}/{_contentOptions.Layout}".Replace("\\", "/");
            }
            else
            {
                ViewBag.Layout = "~/Views/Shared/_Layout.cshtml";
            }

            var roles = _roleManager.Roles.ToList();

            return View(roles);
        }

        [HttpPost]
        public async Task<ActionResult> Roles(string RoleName)
        {
            if (_roleManager.Roles.Where(m => m.Name.ToLower().Equals(RoleName.ToLower())).Count() == 0)
            {
                var identityResult = await _roleManager.CreateAsync(new IdentityRole(RoleName));
            }

            return RedirectToAction(nameof(Roles));
        }

        public ActionResult Role(string id)
        {
            if (!string.IsNullOrWhiteSpace(_contentOptions.Layout) &&
                      System.IO.File.Exists(Path.Combine(_contentOptions.HtmlPath, _contentOptions.Layout)))
            {
                ViewBag.Layout = $"~/{_contentOptions.HtmlPath.Replace(_contentOptions.BasePath, "")}/{_contentOptions.Layout}".Replace("\\", "/");
            }
            else
            {
                ViewBag.Layout = "~/Views/Shared/_Layout.cshtml";
            }

            if (_roleManager.Roles.Where(m => m.Name.ToLower().Equals(id.ToLower())).Count() == 0)
            {
                return NotFound();
            }
            else
            {
                ViewBag.Users = _userManager.Users.ToList();
                ViewBag.RoleName = id;

                return View(_roleManager.Roles.Where(m => m.Name.ToLower().Equals(id.ToLower())).First().Users.ToList());
            }
        }

        [HttpPost]
        public async Task<ActionResult> RolePost(string id)
        {
            if (_roleManager.Roles.Where(m => m.Name.ToLower().Equals(id.ToLower())).Count() == 0)
            {
                return NotFound();
            }

            var existingUserRoles = _roleManager.Roles.Where(m => m.Name.ToLower().Equals(id.ToLower())).First().Users.ToList();
            var existingUsers = _userManager.Users.Where(m => existingUserRoles.Select(u => u.UserId).Contains(m.Id)).ToList();
            foreach (var key in Request.Form.Keys)
            {
                if (key.StartsWith("user__"))
                {
                    var userId = key.Replace("user__", "");

                    var user = existingUsers.Where(m => m.Id.ToLower().Equals(userId.ToLower())).First();

                    if (user != null)
                    {
                        await _userManager.AddToRoleAsync(user, id);

                        existingUsers.Remove(user);
                    }
                }
            }

            foreach (var existingUser in existingUsers)
            {
                await _userManager.RemoveFromRoleAsync(existingUser, id);
            }

            return RedirectToAction(nameof(Roles));
        }
    }
}