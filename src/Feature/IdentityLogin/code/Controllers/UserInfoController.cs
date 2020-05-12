using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Sitecore.Feature.Accounts.Controllers
{
    public class UserInfoController : Controller
    {
        public ActionResult UserInfo()
        {
            return View("~/Views/Authentication/UserInfo.cshtml");
        }

        [HttpPost]
        public ActionResult Logout()
        {
            Sitecore.Security.Authentication.AuthenticationManager.Logout();
            return Redirect("/"); //Mostly irrelevant since redirection is handled from the HandlePostLogoutUrl pipeline.
        }
    }
}