using IdentityManager;
using LicenseManager.Data;
using LicenseManager.Models;
using LicenseManager.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Security.Claims;
using System.Text.Encodings.Web;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MimeKit;
using MimeKit.Text;
using LicenseManager.Services.EmailServices;


namespace LicenseManager.Controllers
{
    [Authorize]
    public class AccountController : Controller
    {

        private readonly UserManager<ApplicationUser> _userManager;       
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IEmailSender _emailSender;
        private readonly ApplicationDbContext _db;
        private readonly UrlEncoder _urlEncoder;
        private readonly IEmailService _emailService;



        public AccountController(UserManager<ApplicationUser> userManager, UrlEncoder urlEncoder, SignInManager<ApplicationUser> signInManager, RoleManager<IdentityRole> roleManager, IEmailSender emailSender, ApplicationDbContext db, IEmailService emailService)
        {
            
            _signInManager = signInManager;
            _userManager = userManager;
            _roleManager = roleManager;
            _emailSender = emailSender;
            _db = db;
            _urlEncoder = urlEncoder;
            _emailService = emailService;

        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> SendEmail(EmailDto request)
        {
            var expiredLicenses = await _db.License
               .Where(l => l.EndDate <= DateTime.Now)
               .ToListAsync();

            if (!expiredLicenses.Any())
            {
                return Ok("No expired licenses found.");
            }

            var users = await _db.Users.ToListAsync();

            foreach (var user in users)
            {
                foreach (var license in expiredLicenses)
                {
                    var message = $"License '{license.Title}' expired on {license.EndDate}. Comments: {license.Comments}";
                    // await _emailSender.SendEmailAsync(user.Email, "Expired License Notification", message);
                    request.To = user.Email;
                    request.Subject = "Expired License Notification";
                    request.Body = message;

                    _emailService.SendEmail(request);
                }
            }

            return Ok("Emails sent successfully.");
            
           // return Ok();
        }

        //[AllowAnonymous]
        [Authorize(Roles = SD.Admin)]
        public async Task<IActionResult> Register(string returnurl = null)
        {
            if (!_roleManager.RoleExistsAsync(SD.Admin).GetAwaiter().GetResult())
            {
                await _roleManager.CreateAsync(new IdentityRole(SD.Admin));
                await _roleManager.CreateAsync(new IdentityRole(SD.User));
            }

        

            ViewData["ReturnUrl"] = returnurl;

            RegisterViewModel registerViewModel = new()
            {
                RoleList = _roleManager.Roles.Select(x => x.Name).Select(i => new SelectListItem
                {
                    Text = i,
                    Value = i
                })
            };
            return View(registerViewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AllowAnonymous]
        public async Task<IActionResult> Register(RegisterViewModel model , string returnurl = null)
        {
            ViewData["ReturnUrl"] = returnurl;
            returnurl = returnurl ?? Url.Content("~/");
            if (ModelState.IsValid)
            {
                var user = new ApplicationUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    Name = model.Name,
                };

                var result = await _userManager.CreateAsync(user, model.Password);
                if (result.Succeeded)
                {
                    if (model.RoleSelected != null)
                    {
                        await _userManager.AddToRoleAsync(user, model.RoleSelected);
                    }
                    else
                    {
                        await _userManager.AddToRoleAsync(user, SD.User);
                    }

                    await _signInManager.SignInAsync(user, isPersistent: false);
                    return LocalRedirect(returnurl);
                }

                AddErrors(result);

            }

        //    model.RoleList = _roleManager.Roles.Select(x => x.Name).Select(i => new SelectListItem
         //   {
         //       Text = i,
        //        Value = i
         //   });

            return View(model);
        }

        private void AddErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LogOff()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        [AllowAnonymous]
        public IActionResult Login(string returnurl = null)
        {
            ViewData["ReturnUrl"] = returnurl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AllowAnonymous]
        public async Task<IActionResult> Login(LoginViewModel model, string returnurl = null)
        {
            ViewData["ReturnUrl"] = returnurl;
            returnurl = returnurl ?? Url.Content("~/Home/Display");
            if (ModelState.IsValid)
            {
                var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe,
                    lockoutOnFailure: true);
                if (result.Succeeded)
                {
                    return LocalRedirect(returnurl);

                }
                if (result.RequiresTwoFactor)
                {
                    return RedirectToAction(nameof(VerifyAuthenticatorCode), new { returnurl, model.RememberMe });
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Invaild login attempt");
                    return View(model);
                }
 


            }
            return View(model);
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult NoAccess()
        {
            return View();
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> VerifyAuthenticatorCode(bool rememberMe, string returnUrl = null)
        {
            var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
            if (user == null)
            {
                return View("Error");
            }
            ViewData["ReturnUrl"] = returnUrl;

            return View(new VerifyAuthenticatorViewModel { ReturnUrl = returnUrl, RememberMe = rememberMe });

        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyAuthenticatorCode(VerifyAuthenticatorViewModel model)
        {

            model.ReturnUrl = model.ReturnUrl ?? Url.Content("~/Home/Display");
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            var result = await _signInManager.TwoFactorAuthenticatorSignInAsync(model.Code, model.RememberMe,
                rememberClient: false);
            if (result.Succeeded)
            {
                return LocalRedirect(model.ReturnUrl);
            }

        
            else
            {
                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                return View(model);
            }
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> EnableAuthenticator()
        {
            string AuthenticatorUriFormat = "otpauth://totp/{0}:{1}?secret={2}&issuer={0}&digits=6";
            var user = await _userManager.GetUserAsync(User);
            await _userManager.ResetAuthenticatorKeyAsync(user);
            var token = await _userManager.GetAuthenticatorKeyAsync(user);
            string AuthUri = string.Format(AuthenticatorUriFormat, _urlEncoder.Encode("LicenseManager"),
                _urlEncoder.Encode(user.Email), token);

            var model = new TwoFactorAuthenticationViewModel() { Token = token, QRCodeUrl = AuthUri };
            return View(model);
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult AuthenticatorConfirmation()
        {
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> NotifyExpiredLicenses()
        {
            var expiredLicenses = await _db.License
                .Where(l => l.EndDate <= DateTime.Now)
                .ToListAsync();

            if (!expiredLicenses.Any())
            {
                return Ok("No expired licenses found.");
            }

            var users = await _db.Users.ToListAsync();

            foreach (var user in users)
            {
                foreach (var license in expiredLicenses)
                {
                    var message = $"License '{license.Title}' expired on {license.EndDate}. Comments: {license.Comments}";
                    await _emailSender.SendEmailAsync(user.Email, "Expired License Notification", message);
                }
            }

            return Ok("Emails sent successfully.");
        }

        [HttpPost]
        
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EnableAuthenticator(TwoFactorAuthenticationViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.GetUserAsync(User);
                var succeeded = await _userManager.VerifyTwoFactorTokenAsync(user, _userManager.Options.Tokens.AuthenticatorTokenProvider, model.Code);
                if (succeeded)
                {
                    await _userManager.SetTwoFactorEnabledAsync(user, true);
                }
                else
                {
                    ModelState.AddModelError("Verify", "Your two factor auth code could not be validated.");
                    return View(model);
                }
                return RedirectToAction(nameof(AuthenticatorConfirmation));
            }

            return View("Error");
        }

    }
}
