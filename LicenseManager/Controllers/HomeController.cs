using IdentityManager;
using LicenseManager.Data;
using LicenseManager.Models;
using LicenseManager.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Diagnostics;

namespace LicenseManager.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _db;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext db)
        {
            _logger = logger;
            _db = db;
        }

        public IActionResult Index()
        {
            return View();
        }

        [Authorize(Roles = SD.Admin)]
        public IActionResult Privacy()
        {
            return View();
        }

        [Authorize(Roles = SD.Admin)]
        public IActionResult Update()
        {
            License license = new License();
            return View(license);
        }

        [Authorize]
        public IActionResult Display()
        {
            

            var licenses = _db.License.ToList();
            var totalLicenses = licenses.Count;
            var activeLicenses = licenses.Count(l => l.IsActive);
            var expiredLicenses = totalLicenses - activeLicenses;

            var viewModel = new LicenseViewModel
            {
                TotalLicenses = totalLicenses,
                ActiveLicenses = activeLicenses,
                ExpiredLicenses = expiredLicenses,
                Licenses = licenses
            };

            return View(viewModel);

        }


        [Authorize(Roles = SD.Admin)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Update(License model)
        {
         
            if (ModelState.IsValid)
            {
                _db.License.Add(model);
                _db.SaveChanges();
                TempData["success"] = "Category created successfully";
                return RedirectToAction("Index");
            }
            return View(model);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
