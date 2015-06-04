using System.Configuration;
using System.Collections.Generic;
using System.Linq;

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
            string AzureADGroupsGroup { get; }
            string FQDomainName { get; }
            string ContactsDestinationOUDN { get; }
            string GroupsDestinationOUDN { get; }
            string PermittedSendersGroupDN { get; } 
            string ObjectPrefix { get; }
            bool AllowCreationOfADObjects { get; }
            bool AllowDeletionOfADObjects { get; }
            bool VerboseLogUserCreation { get; }
            bool VerboseLogUserDeletion { get; }
            //List<string> RemoteGroupsToSync { get; }
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
            public string AzureADGroupsGroup
            {
                get
                {
                    return ConfigurationManager.AppSettings["AzureADGroupsGroup"];
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
           public string ContactsDestinationOUDN
           {
                get
               {
                   return ConfigurationManager.AppSettings["ContactsDestinationOUDN"];
               }
           }
            public string GroupsDestinationOUDN
           {
                get
               {
                   return ConfigurationManager.AppSettings["GroupsDestinationOUDN"];
               }
           }
            public string PermittedSendersGroupDN
            {
                get
                {
                    return ConfigurationManager.AppSettings["PermittedSendersGroupDN"];
                }
            }
           public string ObjectPrefix
           {
                get
               {
                   return ConfigurationManager.AppSettings["ObjectPrefix"];
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

           // public List<string> RemoteGroupsToSync
           //{
           //     get
           //    {
           //        List<string> remoteGroupsToSync = new List<string>();

           //         if(!string.IsNullOrEmpty(ConfigurationManager.AppSettings["RemoteGroupsToSync"]))
           //         {
           //             string[] RemoteGroups = ConfigurationManager.AppSettings["RemoteGroupsToSync"].Split(',');

           //             remoteGroupsToSync = RemoteGroups.ToList();
           //         }

           //        return remoteGroupsToSync;
                    
           //    }
           //}
       }
   }

