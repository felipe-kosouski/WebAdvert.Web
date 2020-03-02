using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.AspNetCore.Identity.Cognito;
using Amazon.Extensions.CognitoAuthentication;
using Amazon.Runtime.Internal.Transform;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using WebAdvert.Web.Models.Accounts;

// For more information on enabling MVC for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace WebAdvert.Web.Controllers
{
    public class AccountsController : Controller
    {
        private readonly SignInManager<CognitoUser> _signInManager;
        private readonly UserManager<CognitoUser> _userManager;
        private readonly CognitoUserPool _pool;

        public AccountsController(SignInManager<CognitoUser> signInManager, UserManager<CognitoUser> userManager, CognitoUserPool cognitoUserPool)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _pool = cognitoUserPool;
        }

        public async Task<IActionResult> Signup()
        {
            var model = new Signup();
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Signup(Signup signupModel)
        {
            if (ModelState.IsValid)
            {
                var user = _pool.GetUser(signupModel.Email);
                if (user.Status != null)
                {
                    ModelState.AddModelError("User Exists", "A User with this email already exists");
                    return View(signupModel);
                }

                user.Attributes.Add(CognitoAttribute.Name.AttributeName, signupModel.Email);
                var createdUser = await _userManager.CreateAsync(user, signupModel.Password);

                if (createdUser.Succeeded)
                {
                    return RedirectToAction("Confirm");
                }
            }
            return View();
        }

        public async Task<IActionResult> Confirm()
        {
            var model = new Confirm();
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Confirm(Confirm confirmModel)
        {
            if (ModelState.IsValid)
            {
                var user =  await _userManager.FindByEmailAsync(confirmModel.Email);
                if (user == null)
                {
                    ModelState.AddModelError("Not Found", "A User with the given email was not found");
                    return View(confirmModel);
                }

                var result = await ((CognitoUserManager<CognitoUser>) _userManager).ConfirmSignUpAsync(user, confirmModel.Code, true);
                if (result.Succeeded)
                {
                    return RedirectToAction("Index", "Home");
                }
                else
                {
                    foreach (var item in result.Errors)
                    {
                        ModelState.AddModelError(item.Code, item.Description);
                    }
                    return View(confirmModel);
                }
            }
            return View(confirmModel);
            
        }

        [HttpGet]
        public async Task<IActionResult> Login(SignIn signInModel)
        {
            return View(signInModel);
        }

        [HttpPost]
        [ActionName("Login")]
        public async Task<IActionResult> LoginPost(SignIn signInModel)
        {
            if (ModelState.IsValid)
            {
                var result = await _signInManager.PasswordSignInAsync(signInModel.Email, signInModel.Password, signInModel.RememberMe, false);
                if (result.Succeeded)
                {
                    return RedirectToAction("Index", "Home");
                }
                else
                {
                    ModelState.AddModelError("Login Error", "Email and password do not match");
                }
            }

            return View(signInModel);
        }

        [HttpGet]
        public async Task<IActionResult> ResetPassword()
        {
            return View();
        }

        [HttpPost]
        [ActionName("ResetPassword")]
        public async Task<IActionResult> ResetPassword(ForgotPassword forgotPasswordModel)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByEmailAsync(forgotPasswordModel.Email);
                if (user == null)
                {
                    ModelState.AddModelError("Not Found", "A User with the given email was not found");
                    return View(forgotPasswordModel);
                }
                var result = await _userManager.ResetPasswordAsync(user, forgotPasswordModel.Token, forgotPasswordModel.NewPassword);
                if (result.Succeeded)
                {
                    return RedirectToAction("Login");
                }
                else
                {
                    foreach (var item in result.Errors)
                    {
                        ModelState.AddModelError(item.Code, item.Description);
                    }
                    return View(forgotPasswordModel);
                }
            }
            return View(forgotPasswordModel);
        }
    }
}
