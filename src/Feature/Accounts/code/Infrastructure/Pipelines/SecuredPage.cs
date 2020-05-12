using Sitecore.Diagnostics;
using Sitecore.Pipelines.HttpRequest;
using Sitecore.Web;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Sitecore.Feature.Accounts.Infrastructure.Pipelines
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
                        if (LoginType.Equals("login"))
                        {
                            if (args.HttpContext.Session["authtoken"] == null)
                            {
                                if (((Sitecore.Data.Fields.CheckboxField)Sitecore.Context.Item.Fields["Is Secured"]).Checked)
                                {
                                    LoginRedirect();
                                }
                            }
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


    }
}