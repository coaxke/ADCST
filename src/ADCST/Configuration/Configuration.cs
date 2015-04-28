using System.Configuration;

namespace ADCST.Configuration
{

        public interface IConfiguration
        {
            string TennantName { get; }
            string TenantID { get; }
            string ClientId { get; }
            string ClientSecret { get; }
            string ClientIdForUserAuth { get; }
            string AuthString { get; }
            string ResourceURL { get; }
            string AzureADUserGroup { get; }
            string FQDomainName { get; }
            string DestinationOUDN { get; }
            string ContactPrefix { get; }
            bool AllowCreationOfADObjects { get; }
            bool AllowDeletionOfADObjects { get; }
            bool VerboseLogUserCreation { get; }
            bool VerboseLogUserDeletion { get; }
        }

        public class ADCSTConfiguration : IConfiguration
        {
            //Azure Online Config Items

            public string TennantName
            {
                get
                {
                    return ConfigurationManager.AppSettings["AzureADTenantName"];
                }
            }

            public string TenantID
            {
                get
                {
                    return ConfigurationManager.AppSettings["AzureADTenantID"];
                }
            }

            public string ClientId
            {
                get
                {
                    return ConfigurationManager.AppSettings["AzureADClientId"];
                }
            }

            public string ClientSecret
            {
                get
                {
                    return ConfigurationManager.AppSettings["AzureADClientSecret"];
                }
            }

            public string ClientIdForUserAuth
            {
                get
                {
                    return ConfigurationManager.AppSettings["AzureADClientIdForUserAuth"];
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

            public string AzureADUserGroup
            {
                get
                {
                    return ConfigurationManager.AppSettings["AzureADUserGroup"];
                }
            }

            //Local Active Directory Config Items

           public string FQDomainName
            {
                get
                {
                    return ConfigurationManager.AppSettings["FQDomainName"];
                }
            }
           public string DestinationOUDN
           {
                get
               {
                   return ConfigurationManager.AppSettings["DestinationOUDN"];
               }
           }
           public string ContactPrefix
           {
                get
               {
                   return ConfigurationManager.AppSettings["ContactPrefix"];
               }
           }
           public bool AllowCreationOfADObjects
           {
               get
               {
                   bool allowCreationOfADObjects;

                   if(!bool.TryParse(ConfigurationManager.AppSettings["AllowCreationOfADObjects"], out allowCreationOfADObjects))
                   {
                       allowCreationOfADObjects = true;
                   }

                   return allowCreationOfADObjects;
               }
           }

            public bool AllowDeletionOfADObjects
           {
                get
               {
                   bool allowDeletionOfADObjects;

                    if(!bool.TryParse(ConfigurationManager.AppSettings["AllowDeletionOfADObjects"], out allowDeletionOfADObjects))
                    {
                        allowDeletionOfADObjects = true;
                    }

                    return allowDeletionOfADObjects;
               }
           }

           public bool VerboseLogUserCreation
            {
               get
                {
                    bool verboseLogUserCreation;

                       if(!bool.TryParse(ConfigurationManager.AppSettings["VerboseLogUserCreation"], out verboseLogUserCreation))
                       {
                           verboseLogUserCreation = false;
                       }

                   return verboseLogUserCreation;
                }
            }

           public bool VerboseLogUserDeletion
           {
               get
               {
                   bool verboseLogUserDeletion;

                   if(!bool.TryParse(ConfigurationManager.AppSettings["VerboseLogUserDeletion"], out verboseLogUserDeletion))
                   {
                       verboseLogUserDeletion = false;
                   }

                   return verboseLogUserDeletion;
               }
           }



       }
   }

