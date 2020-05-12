using Microsoft.Extensions.DependencyInjection;
using Sitecore.Abstractions;
using Sitecore.DependencyInjection;
using Sitecore.Pipelines.GetSignInUrlInfo;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace DGAIdentityLogin.Controllers
{
    public class IdentityLoginController : Controller
    {
        public ActionResult LoginProviderList(string returnUrl)
        {
            var corePipelineManager = ServiceLocator.ServiceProvider.GetService<BaseCorePipelineManager>();
            //var args = new GetSignInUrlInfoArgs("website", "/");
            var args = new GetSignInUrlInfoArgs("website", string.IsNullOrEmpty(returnUrl) ?"/": returnUrl);

            GetSignInUrlInfoPipeline.Run(corePipelineManager, args);

            return View("~/Views/Authentication/LoginProviderList.cshtml", args.Result);
        }
    }
}
