using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using StudentHub.Constants;
using StudentHub.Data;
using StudentHub.Models;
namespace StudentHub.Controllers;
public sealed class AccountController(UserManager<ApplicationUser> users, SignInManager<ApplicationUser> signIn) : Controller
{
 [HttpGet] public IActionResult Login() => View(new LoginViewModel());
 [HttpPost,ValidateAntiForgeryToken] public async Task<IActionResult> Login(LoginViewModel model,string? returnUrl=null){if(!ModelState.IsValid)return View(model);var r=await signIn.PasswordSignInAsync(model.Email,model.Password,model.RememberMe,true);if(r.Succeeded)return LocalRedirect(returnUrl??Url.Action("Index","Dashboard")!);ModelState.AddModelError(string.Empty,"Invalid email or password.");return View(model);}
 [HttpGet] public IActionResult Register()=>View(new RegisterViewModel());
 [HttpPost,ValidateAntiForgeryToken] public async Task<IActionResult> Register(RegisterViewModel model){if(!ModelState.IsValid)return View(model);var user=new ApplicationUser{UserName=model.Email,Email=model.Email,DisplayName=model.DisplayName};var r=await users.CreateAsync(user,model.Password);if(!r.Succeeded){foreach(var e in r.Errors)ModelState.AddModelError(string.Empty,e.Description);return View(model);}await users.AddToRoleAsync(user,StudentHubRoles.Student);await signIn.SignInAsync(user,false);return RedirectToAction("Index","Dashboard");}
 [HttpPost,ValidateAntiForgeryToken] public async Task<IActionResult> Logout(){await signIn.SignOutAsync();return RedirectToAction(nameof(Login));} public IActionResult AccessDenied()=>View();
}
