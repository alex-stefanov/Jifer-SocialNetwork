﻿using Jifer.Data;
using Jifer.Data.Models;
using Jifer.Helpers;
using Jifer.Models.SendEmail;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Jifer.Controllers
{
    public class InviteController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly IHelper _inviteHelper;
        private readonly UserManager<JUser> userManager;

        public InviteController(IConfiguration configuration,
            ApplicationDbContext context,
            UserManager<JUser> userManager)
        {
            _configuration = configuration;
            _inviteHelper = new IHelper(context);
            this.userManager = userManager;
        }

        [HttpGet]
        public IActionResult InviteFriend()
        {
            var viewModel = new SendEmailViewModel(); 
            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> InviteFriend(SendEmailViewModel model)
        {
            var user = await userManager.GetUserAsync(User);

            var invite = _inviteHelper.GenerateInviteCode(user, model.EmailToBeSent).Result;

            var emailHelper = new EHelper(_configuration);

            string subject = "Congrats! You were invited";
            string link = Url.Action("Register", "User", new { code = invite.InvitationCode.ToString() }, Request.Scheme);
            string body = $"<p>You have been invited to join our site. Please click <a href='{link}'>here</a> to register. This link is valid for 48 hours.</p>";

            emailHelper.SendEmail(model.EmailToBeSent, subject, body);

            return RedirectToAction("Welcome", "Home");
        }
    }
}
