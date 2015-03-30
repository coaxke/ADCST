using System.Configuration;

namespace ADCST.Configuration
{

        public interface IConfiguration
        {
            string TenantName { get; }
            string TenantID { get; }
            string ClientId { get; }
            string ClientSecret { get; }
            string ClientIdForUserAuth { get; }
            string AuthString { get; }
            string ResourceURL { get; }
        }

        public class ADCSTConfiguration : IConfiguration
        {
            public string TennantName
            {
                get
                {
                    return ConfigurationManager.AppSettings["TenantName"];
                }
            }

            public string TenantID
            {
                get
                {
                    return ConfigurationManager.AppSettings["TenantID"];
                }
            }

            public string ClientId
            {
                get
                {
                    return ConfigurationManager.AppSettings["ClientId"];
                }
            }

            public string ClientSecret
            {
                get
                {
                    return ConfigurationManager.AppSettings["ClientSecret"];
                }
            }

            public string ClientIdForUserAuth
            {
                get
                {
                    return ConfigurationManager.AppSettings["ClientIdForUserAuth"];
                }
            }

            public string AuthString
            {
                get
                {
                    string FullAuthString = string.Concat("https://login.windows.net/", TennantName);

                    return FullAuthString;
                }
            }

            public string ResourceURL
            {
                get
                {
                    return "https://graph.windows.net";
                }
           }        
       }
   }

