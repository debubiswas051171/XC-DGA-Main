namespace Sitecore.Feature.Accounts.Repositories
{
    using System;
    using System.Threading.Tasks;
    using System.Web.Security;
    using IdentityModel.Client;
    using Sitecore.Diagnostics;
    using Sitecore.Feature.Accounts.Services;
    using Sitecore.Foundation.Accounts.Pipelines;
    using Sitecore.Foundation.DependencyInjection;
    using Sitecore.Pipelines;
    using Sitecore.Security.Accounts;
    using Sitecore.Security.Authentication;
    using DGA.Take2Rest;
    using DGA.Take2Rest.IdentityModelExtensions;

    [Service(typeof(IAccountRepository))]
    public class AccountRepository : IAccountRepository
    {
        public IAccountTrackerService AccountTrackerService { get; }
        private readonly PipelineService pipelineService;

        public AccountRepository(PipelineService pipelineService, IAccountTrackerService accountTrackerService)
        {
            this.AccountTrackerService = accountTrackerService;
            this.pipelineService = pipelineService;
        }

        public bool Exists(string userName)
        {
            var fullName = Context.Domain.GetFullName(userName);

            return User.Exists(fullName);
        }

        public User Login(string userName, string password)
        {
            var accountName = string.Empty;
            var domain = Context.Domain;
            if (domain != null)
            {
                accountName = domain.GetFullName(userName);
            }

            var result = AuthenticationManager.Login(accountName, password);
            if (!result)
            {
                AccountTrackerService.TrackLoginFailed(accountName);
                return null;
            }

            var user = AuthenticationManager.GetActiveUser();
            this.pipelineService.RunLoggedIn(user);
            return user;
        }

        #region DGA Login

        private IdentityModel.Client.IdentityModelExtensions.TokenResponse Take2RetrieveToken(string login, string password)
        {
            IdentityModel.Client.IdentityModelExtensions.TokenResponse token = null;
            token = Client.RetrieveAccessToken(login, password);
            return token;
        }

        private DGA.Take2Rest.IdentityModelExtensions.DGATokenResponse Take2RetrieveDGAToken(string tokenString)
        {
            DGA.Take2Rest.IdentityModelExtensions.DGATokenResponse token = null;
            token = Client.RetrieveDGAAccessToken(tokenString);
            return token;
        }


        private User CreateVirtualUser(DGATokenResponse token, string userName)
        {
            var virtualUser = AuthenticationManager.BuildVirtualUser(string.Format("{0}\\{1}", Take2Domain, userName), true);
            virtualUser.Profile.FullName = string.Format("{0} {1}",token.given_name,token.family_name);
            virtualUser.Profile.Email = token.email;
            virtualUser.Profile.Save();
            if (token.role.Equals("member", StringComparison.InvariantCultureIgnoreCase))
            {
                virtualUser.Roles.Add(Sitecore.Security.Accounts.Role.FromName(@"take2\member"));
            }
            return virtualUser;
        }

        private string Take2Domain
        {
            get
            {
                return "take2";
            }
        }

        public User DGALogin(string userName, string password)
        {
            var accountName = string.Empty;
            accountName = string.Format("{0}\\{1}", Take2Domain, userName);

            IdentityModel.Client.IdentityModelExtensions.TokenResponse token = Take2RetrieveToken(accountName, password);

            DGATokenResponse dgaToken = null;
            if (token != null)
            {
                dgaToken = Take2RetrieveDGAToken(token.AccessToken);
            }

            if (dgaToken != null)
            {
                var identifiedUser = CreateVirtualUser(dgaToken, userName);
                var result = AuthenticationManager.LoginVirtualUser(identifiedUser);

            }

            var user = AuthenticationManager.GetActiveUser();
            //this.pipelineService.RunLoggedIn(user);
            return user;
        }

        #endregion

        public void Logout()
        {
            var user = AuthenticationManager.GetActiveUser();
            AuthenticationManager.Logout();
            //if (user != null)
            //    this.pipelineService.RunLoggedOut(user);

            System.Web.HttpContext.Current.Session.Clear();
            System.Web.HttpContext.Current.Session.Abandon();
        }

        public string RestorePassword(string userName)
        {
            var domainName = Context.Domain.GetFullName(userName);
            var user = Membership.GetUser(domainName);
            if (user == null)
                throw new ArgumentException($"Could not reset password for user '{userName}'", nameof(userName));
            return user.ResetPassword();
        }

        public void RegisterUser(string email, string password, string profileId)
        {
            Assert.ArgumentNotNullOrEmpty(email, nameof(email));
            Assert.ArgumentNotNullOrEmpty(password, nameof(password));

            var fullName = Context.Domain.GetFullName(email);
            try
            {

                Assert.IsNotNullOrEmpty(fullName, "Can't retrieve full userName");

                var user = User.Create(fullName, password);
                user.Profile.Email = email;
                if (!string.IsNullOrEmpty(profileId))
                {
                    user.Profile.ProfileItemId = profileId;
                }

                user.Profile.Save();
                this.pipelineService.RunRegistered(user);
            }
            catch
            {
                AccountTrackerService.TrackRegistrationFailed(email);
                throw;
            }

            this.Login(email, password);
        }
    }
}