using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using DocMd.Site.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace Site.Controllers
{
    public class SecurityController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public SecurityController()
        {
        }

        public SecurityController(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        // GET: Security
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult Roles()
        {
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