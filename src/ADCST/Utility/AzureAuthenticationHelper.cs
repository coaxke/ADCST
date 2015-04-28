using System;
using System.Threading.Tasks;
using Microsoft.Azure.ActiveDirectory.GraphClient;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using ADCST.Configuration;

namespace ADCST.Utility
{
    public interface IAuthenticationProvidor
    {
        Task<string> AcquireTokenAsyncForApplication(IConfiguration Configuration);
        string GetTokenForApplication(IConfiguration Configuration);
        ActiveDirectoryClient GetActiveDirectoryClientAsApplication(IConfiguration Configuration);
        Task<string> AcquireTokenAsyncForUser(IConfiguration Configuration);
        string GetTokenForUser(IConfiguration Configuration);
        ActiveDirectoryClient GetActiveDirectoryClientAsUser(IConfiguration Configuration);
    }

    public sealed class AuthenticationHelper : IAuthenticationProvidor 
    {
        public string TokenForUser;

        /// <summary>
        /// Async task to acquire token for Application.
        /// </summary>
        /// <returns>Async Token for application.</returns>
        public async Task<string> AcquireTokenAsyncForApplication(IConfiguration Configuration)
        {
            return GetTokenForApplication(Configuration);
        }

        /// <summary>
        /// Get Token for Application.
        /// </summary>
        /// <returns>Token for application.</returns>
        public string GetTokenForApplication(IConfiguration Configuration)
        {
            AuthenticationContext authenticationContext = new AuthenticationContext(Configuration.AuthString, false);
            // Config for OAuth client credentials 
            ClientCredential clientCred = new ClientCredential(Configuration.ClientId, Configuration.ClientSecret);
            AuthenticationResult authenticationResult = authenticationContext.AcquireToken(Configuration.ResourceURL,
                clientCred);
            string token = authenticationResult.AccessToken;
            return token;
        }

        /// <summary>
        /// Get Active Directory Client for Application.
        /// </summary>
        /// <returns>ActiveDirectoryClient for Application.</returns>
        public ActiveDirectoryClient GetActiveDirectoryClientAsApplication(IConfiguration Configuration)
        {
            Uri servicePointUri = new Uri(Configuration.ResourceURL);
            Uri serviceRoot = new Uri(servicePointUri, Configuration.TenantID);
            ActiveDirectoryClient activeDirectoryClient = new ActiveDirectoryClient(serviceRoot,
                async () => await AcquireTokenAsyncForApplication(Configuration));
            return activeDirectoryClient;
        }

        /// <summary>
        /// Async task to acquire token for User.
        /// </summary>
        /// <returns>Token for user.</returns>
        public async Task<string> AcquireTokenAsyncForUser(IConfiguration Configuration)
        {
            return GetTokenForUser(Configuration);
        }

        /// <summary>
        /// Get Token for User.
        /// </summary>
        /// <returns>Token for user.</returns>
        public string GetTokenForUser(IConfiguration Configuration)
        {
            if (TokenForUser == null)
            {
                var redirectUri = new Uri("https://localhost"); //This can be a bougus URL
                AuthenticationContext authenticationContext = new AuthenticationContext(Configuration.AuthString, false);
                AuthenticationResult userAuthnResult = authenticationContext.AcquireToken(Configuration.ResourceURL,
                    Configuration.ClientIdForUserAuth, redirectUri, PromptBehavior.Always);
                TokenForUser = userAuthnResult.AccessToken;
               // Console.WriteLine("\n Welcome " + userAuthnResult.UserInfo.GivenName + " " + userAuthnResult.UserInfo.FamilyName);
            }
            return TokenForUser;
        }

        /// <summary>
        /// Get Active Directory Client for User.
        /// </summary>
        /// <returns>ActiveDirectoryClient for User.</returns>
        public ActiveDirectoryClient GetActiveDirectoryClientAsUser(IConfiguration Configuration)
        {
            Uri servicePointUri = new Uri(Configuration.ResourceURL);
            Uri serviceRoot = new Uri(servicePointUri, Configuration.TenantID);
            ActiveDirectoryClient activeDirectoryClient = new ActiveDirectoryClient(serviceRoot,
                async () => await AcquireTokenAsyncForUser(Configuration));
            return activeDirectoryClient;
        }
    }
}
