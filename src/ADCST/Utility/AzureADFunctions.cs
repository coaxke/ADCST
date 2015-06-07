using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure.ActiveDirectory.GraphClient;
using Microsoft.Azure.ActiveDirectory.GraphClient.Extensions;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using ADCST.Configuration;
using ADCST.Utility;
using NLog;

namespace ADCST.Utility
{
    public interface IAzureADFunctions
    {
        ActiveDirectoryClient ADClient(IConfiguration Configuration, IAuthenticationProvidor authProvidor, Logger Logger);
        Group GetADGroup(ActiveDirectoryClient ADClient, string GroupNameSearchString, Logger Logger);
        List<Tuple<string, IDirectoryObject>> GetAdGroupMembers (Group RetrievedGroup, IConfiguration Configuration, Logger Logger);
        string TenantDetails(ActiveDirectoryClient ADClient, Logger Logger, IConfiguration Config);
        List<IUser> SearchADUser(ActiveDirectoryClient ADClient, Logger logger, string SearchString);
    }

    public sealed class AzureADFunctions : IAzureADFunctions
    {
        public ActiveDirectoryClient ADClient(IConfiguration Configuration, IAuthenticationProvidor authProvidor, Logger Logger)
        {
            ActiveDirectoryClient activeDirectoryClient;
            try
            {
                Logger.Debug(@"Connecting to Azure Active Directory GraphAPI to get ClientSession");
                activeDirectoryClient = authProvidor.GetActiveDirectoryClientAsApplication(Configuration);

                if (activeDirectoryClient != null)
                {
                    return activeDirectoryClient;
                }
                else
                {
                    return null;
                }
            }
            catch (AuthenticationException ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Acquiring a token failed with the following error: {0}", ex.Message);
                Logger.Error(String.Format(@"Could not aquire Azure active Directory Authentication Token {0}", ex.Message));

                if (ex.InnerException != null)
                {
                    //InnerException Message will contain the HTTP error status codes mentioned in the link above
                    Console.WriteLine("Error detail: {0}", ex.InnerException.Message);
                    Logger.Error(String.Format(@"Error detail {0}", ex.InnerException));

                }
                Console.ResetColor();
                return null;
            }
        }

        public Group GetADGroup (ActiveDirectoryClient ADClient, string GroupNameSearchString, Logger Logger)
        {
            List<IGroup> foundGroups = null;

            try
            {
                Logger.Debug(string.Format("Searching/Fetching AD Group {0}", GroupNameSearchString));
                foundGroups = ADClient.Groups
                    .Where(group => group.DisplayName.StartsWith(GroupNameSearchString))
                    .ExecuteAsync().Result.CurrentPage.ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error when fetching group from Azure Active Directory {0} {1}", ex.Message, ex.InnerException != null ? ex.InnerException.Message : "");
                Logger.Error(String.Format("Error when fetching group from Azure Active Directory {0} {1}", ex.Message, ex.InnerException != null ? ex.InnerException.Message : ""));
            }
            if (foundGroups != null && foundGroups.Count > 0)
            {
                return foundGroups.First() as Group;
            }
            else
            {
                return null;
            }
        }

        public List<Tuple<string, IDirectoryObject>> GetAdGroupMembers(Group RetrievedGroup, IConfiguration Configuration, Logger Logger)
        {
            List<Tuple<string, IDirectoryObject>> DirectoryObjects = new List<Tuple<string, IDirectoryObject>>();

            if (RetrievedGroup.ObjectId != null)
            {
                IGroupFetcher retrievedGroupFetcher = RetrievedGroup;

                try
                {
                    IPagedCollection<IDirectoryObject> GroupMembers = retrievedGroupFetcher.Members.ExecuteAsync().Result;
                    do
                    {
                        List<IDirectoryObject> directoryObjects = GroupMembers.CurrentPage.ToList();

                        foreach (IDirectoryObject directoryObject in directoryObjects)
                        {
                            if (directoryObject is User)
                            {
                                DirectoryObjects.Add(Tuple.Create("user", directoryObject));    
                            }
                            if (directoryObject is Contact)
                            {
                                DirectoryObjects.Add(Tuple.Create("contact", directoryObject));
                            }
                            if (directoryObject is Group)
                            {
                                DirectoryObjects.Add(Tuple.Create("group", directoryObject));
                            }
                        }
                        GroupMembers = GroupMembers.GetNextPageAsync().Result;
                    } while (GroupMembers != null);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error retrieving Group membership {0} {1}", ex.Message, ex.InnerException != null ? ex.InnerException.Message : "");
                    Logger.Error(String.Format("Error retrieving Group membership {0} {1}", ex.Message, ex.InnerException != null ? ex.InnerException.Message : ""));
                }

                return DirectoryObjects;
            }
            else
            {
                return DirectoryObjects;
            }
        }

        public string TenantDetails (ActiveDirectoryClient ADClient, Logger Logger, IConfiguration Config)
        {
            List<ITenantDetail> tenantsList = new List<ITenantDetail>();
            List<string> VerifiedDomainNames = new List<string>();
            List<string> DefaultDomains = new List<string>();
            List<string> TechContacts = new List<string>();            
            string LastDirSyncTime;
            string returnvalue;
            List<string> ReturnTenantDetails = new List<string>();

            Logger.Debug(@"Retrieving Azure Tenant Details");
            
            try
            {
               tenantsList = ADClient.TenantDetails
                .Where(tenantDetail => tenantDetail.ObjectId.Equals(Config.TenantID))
                .ExecuteAsync().Result.CurrentPage.ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error getting Tenant Details {0} {1}", ex.Message, ex.InnerException != null ? ex.InnerException.Message : "");
                Logger.Debug(string.Format("Error getting Tennnt Details {0} {1}", ex.Message, ex.InnerException != null ? ex.InnerException.Message : ""));
            }

            if (tenantsList.Count < 0)
            {
                Console.WriteLine("Tenant not found");
                Logger.Error("Tenant not found");
                return "Tennant not found";
            }
            else
            {
                foreach (ITenantDetail tenant in tenantsList)
                {
                    VerifiedDomainNames = tenant.VerifiedDomains
                        .Where(x => x.Initial.HasValue && x.Initial.Value)
                        .Select(n => n.Name).ToList();

                    DefaultDomains = tenant.VerifiedDomains
                        .Where(x => x.@default.HasValue && x.@default.Value)
                        .Select(n => n.Name).ToList();

                    TechContacts = tenant.TechnicalNotificationMails.ToList();

                    LastDirSyncTime = tenant.CompanyLastDirSyncTime.ToString();


                    ReturnTenantDetails.Add(string.Format(" Tennant Display Name(s): {0}\n Initial Domain name(s): {1}\n Default Domain Name(s): {2}\n Tennant Tech Contact(s): {3}\n Last Dir Sync Time {4}. \n\n",
                                            string.Join(" ", tenant.DisplayName), string.Join(" ", VerifiedDomainNames.ToList()), string.Join(" ", DefaultDomains.ToList()),
                                            string.Join(" ", TechContacts.ToList()), LastDirSyncTime));                    
                }
                
                returnvalue = String.Join("\n\n", ReturnTenantDetails);
                return returnvalue;
            }            
        }

        public List<IUser> SearchADUser(ActiveDirectoryClient ADClient, Logger Logger, string SearchString)
        {
            List<IUser> RetrievedUsers = new List<IUser>();

            try
            {
                RetrievedUsers = ADClient.Users
                    .Where(user => user.UserPrincipalName.StartsWith(SearchString))
                    .ExecuteAsync().Result.CurrentPage.ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error Searching for user {0} {1}", ex.Message, ex.InnerException != null ? ex.InnerException.Message : "");
                Logger.Error(string.Format("Error Searching for user {0} {1}", ex.Message, ex.InnerException != null ? ex.InnerException.Message : ""));
            }

            if (RetrievedUsers != null && RetrievedUsers.Count > 1)
            {
                Logger.Debug(String.Format("Found {0} user(s) for the query {1}", RetrievedUsers.Count(), SearchString));
                return RetrievedUsers;

            }
            else
            {
                Logger.Debug(String.Format("No Users found for the query {0}", SearchString));
                return null;
            }
        }
    }
}
