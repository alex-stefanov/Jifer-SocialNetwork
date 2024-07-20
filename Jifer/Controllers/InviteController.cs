namespace Jifer.Controllers
{
    using Jifer.Services.Models.SendEmail;
    using Jifer.Services.Interfaces;
    using Microsoft.AspNetCore.Mvc;
    using Jifer.Services.Implementations;
    using System.ComponentModel;

    public class InviteController : Controller
    {
        private readonly IInviteService inviteService;

        private readonly IHomeService homeService;

        public InviteController(
            IInviteService _inviteService,
            IHomeService _homeService)
        { 
            inviteService = _inviteService;
            homeService = _homeService;
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
            if (ModelState.IsValid)
            {
                var user = await homeService.GetCurrentUserAsync(User);

                if (!await inviteService.IsInvitationValidAsync(model.EmailToBeSent))
                {
                    model.DuplicateEmail = "Yes";

                    return View(model);
                }

                var inviteCode = await inviteService.GenerateInviteCodeAsync(user, model.EmailToBeSent);

                string subject = "Congrats! You were invited";
                string link = Url.Action("Register", "User", new { code = inviteCode }, Request.Scheme);
                string body = $"<p>You have been invited to join our site. Please click <a href='{link}'>here</a> to register. This link is valid for 48 hours.</p>";

                await inviteService.SendInvitationEmailAsync(model.EmailToBeSent, subject, body);

                model.DuplicateEmail = "No";

                return View(model);
            }

            return View(model);
        }
    }
}
