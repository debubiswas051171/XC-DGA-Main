using Sitecore.Abstractions;
using Sitecore.DependencyInjection;
using Sitecore.Diagnostics;
using Sitecore.Pipelines.GetSignInUrlInfo;
using Sitecore.Pipelines.HttpRequest;
using Sitecore.Web;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

using Microsoft.Extensions.DependencyInjection;
using System.Net;

namespace DGAIdentityLogin.Pipelines
{
    public class SecuredPage : HttpRequestProcessor
    {
        public SecuredPage()
        {
        }

        private string GetRawUrl(string rawUrl)
        {
            string host = HttpContext.Current.Request.Url.Host;
            string protocol = HttpContext.Current.Request.IsSecureConnection ? "https" : "http";
            rawUrl = rawUrl.Replace(protocol + "//", "");
            rawUrl = rawUrl.Replace(host, "");
            return rawUrl;
        }

        private string LoginType
        {
            get
            {
                if (Sitecore.Context.Item.Paths.FullPath.ToLower().Contains("/identitylogin/"))
                {
                    return "identitylogin";
                }

                if (Sitecore.Context.Item.Paths.FullPath.ToLower().Contains("/login/"))
                {
                    return "login";
                }

                return string.Empty;
            }
        }

        public override void Process(HttpRequestArgs args)
        {
            try
            {
                Assert.ArgumentNotNull(args, "args");

                if (Sitecore.Context.GetSiteName() == "website")
                {
                    if (Sitecore.Context.Item.Fields["Is Secured"] != null)
                    {
                         if (LoginType.Equals("identitylogin") 
                            && ((Sitecore.Data.Fields.CheckboxField)Sitecore.Context.Item.Fields["Is Secured"]).Checked
                            && !Sitecore.Context.User.IsAuthenticated)
                        {
                            IdentityLoginRedirect();
                            //PerformPost();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error("Error while setting the context item " + ex.Message, ex, "Secured Page Item resolver");
                return;
            }
        }

        private void LoginRedirect()
        {
            Sitecore.Text.UrlString url = new Sitecore.Text.UrlString("/Login.aspx");
            url["returnUrl"] = GetRawUrl(Sitecore.Context.RawUrl);

            if (!Sitecore.Configuration.Settings.RequestErrors.UseServerSideRedirect)
            {
                WebUtil.Redirect(url.ToString(), false);
                return;
            }
            HttpContext.Current.Server.Transfer(url.ToString());

        }

        private void IdentityLoginRedirect()
        {
            //Sitecore.Text.UrlString url = new Sitecore.Text.UrlString(string.Format("/identitylogin?redirectUrl={0}",
            //    HttpUtility.UrlEncode(GetRawUrl(Sitecore.Context.RawUrl))));

            Sitecore.Text.UrlString url = new Sitecore.Text.UrlString(string.Format("/identitylogin?redirectUrl={0}",
                           "/identitylogin/securedpage"));

            if (!Sitecore.Configuration.Settings.RequestErrors.UseServerSideRedirect)
            {
                WebUtil.Redirect(url.ToString(), false);
                return;
            }
            HttpContext.Current.Server.Transfer(url.ToString());

            //HttpContext.Current.Response.Redirect(HttpUtility.UrlEncode(string.Format("/identity/externallogin?authenticationType=IS4&ReturnUrl=/identity/externallogincallback?ReturnUrl={0}&sc_site=website&authenticationSource=Default&sc_site=website",
            //    HttpUtility.UrlEncode(GetRawUrl(Sitecore.Context.RawUrl)))));


        }

        private void PerformPost()
        {
            string redirectUrl = "/identitylogin/securedpage";
            var corePipelineManager = ServiceLocator.ServiceProvider.GetService<BaseCorePipelineManager>();
            //var args = new GetSignInUrlInfoArgs("website", "/");
            var args = new GetSignInUrlInfoArgs("website", string.IsNullOrEmpty(redirectUrl) ? "/" : redirectUrl);

            GetSignInUrlInfoPipeline.Run(corePipelineManager, args);
            string postUrl = "https://dgaidentitysolutionqa.dga.org"+args.Result.FirstOrDefault().Href;

            WebRequest request = WebRequest.Create(postUrl);

            //Set the Method property of the request to POST.
            request.Method = "POST";

              //Get the response.
            WebResponse response = request.GetResponse();


        }

    }
}