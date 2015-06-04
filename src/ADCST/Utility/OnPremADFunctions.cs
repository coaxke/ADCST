using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ADCST.Configuration;
using ADCST.Utility;
using NLog;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System.DirectoryServices;
using System.DirectoryServices.ActiveDirectory;
using Microsoft.Azure.ActiveDirectory.GraphClient;

namespace ADCST.Utility
{
    public interface IOnPremAdFunctions
    {
        List<DirectoryEntry> GetOUContactObjects(string FQDomainName, string DestinationOUDN, IOnPremADHelper OnPremADHelper, Logger Logger);
        int CreateADUserContacts(Logger Logger, IConfiguration Config, DirectoryEntry DirEntry, IOnPremADHelper OnPremADHelper, Dictionary<string, IUser> AzureUsers);
        int CreateADGroupContacts(Logger Logger, IConfiguration Config, DirectoryEntry DirEntry, IOnPremADHelper OnPremADHelper, Dictionary<string, IGroup> AzureGroups);
        int DeleteADContacts (Logger Logger, IConfiguration Config, DirectoryEntry DirEntry, Dictionary<string, DirectoryEntry> OnPremContacts);
        void AddADContactToGroup (Logger Logger, IConfiguration Config, IOnPremADHelper OnPremADHelper, string ContactDNPath);

    }

    public sealed class OnPremADFunctions : IOnPremAdFunctions
    {
        //Search Contact
        public List<DirectoryEntry> GetOUContactObjects (string FQDomainName, string DestinationOUDN, IOnPremADHelper OnPremADHelper, Logger Logger)
        {
            SearchResultCollection ListADContactResults;
            List <DirectoryEntry> ADContacts = new List<DirectoryEntry>();

            //Will enter Directory at $ContactsDestinationOUDN and retrieve ALL properties (empty string array)
            using (DirectorySearcher ListADContacts = new DirectorySearcher(OnPremADHelper.GetADDirectoryEntry(FQDomainName,DestinationOUDN, Logger), 
                                                                           "(objectClass=contact)", new string[] {}))            
            {
                ListADContacts.PropertiesToLoad.Add("mail");
                ListADContactResults = ListADContacts.FindAll();

            }

            if (ListADContactResults != null)
            {
                //Get the corrosponding Directory Entry for each SearchResult object.
                foreach (SearchResult DirectoryEntry in ListADContactResults)
                {
                    ADContacts.Add(DirectoryEntry.GetDirectoryEntry());
                }

                return ADContacts;
            }
            else
            {
                Logger.Debug(String.Format("No Contact objects found in {0}", DestinationOUDN));
                Console.WriteLine("No Contact objects found in {0}", DestinationOUDN);

                //Retrun an empty list.
                return ADContacts;
            }
        }
   
        //Create User Contact objects
        public int CreateADUserContacts(Logger Logger, IConfiguration Config, DirectoryEntry DirEntry, IOnPremADHelper OnPremADHelper, Dictionary<string, IUser> AzureUsers)
        {
            int CreationCount = 0;
            Logger.Debug("Begining User Contact Creation...");
            foreach (User AzureUser in AzureUsers.Values)
            {
                try
                {
                    if (AzureUser.AccountEnabled.HasValue && AzureUser.AccountEnabled.Value)
                    {

                        DirectoryEntry newUser = DirEntry.Children.Add("CN=" + AzureUser.DisplayName, "contact");

                        //Add Each Property we care about for the respective User account (if not null):
                        if (!string.IsNullOrEmpty(Config.ObjectPrefix))
                        {
                            newUser.Properties["description"].Value = Config.ObjectPrefix;
                        }
                        if (!string.IsNullOrEmpty(AzureUser.GivenName))
                        {
                            newUser.Properties["givenName"].Value = AzureUser.GivenName;
                        }
                        if (!string.IsNullOrEmpty(AzureUser.Mail))
                        {
                            newUser.Properties["Mail"].Value = AzureUser.Mail;
                            newUser.Properties["proxyAddresses"].AddRange(new object[] { "SMTP:" + AzureUser.Mail, "SIP:" + AzureUser.Mail });
                        }
                        if (!string.IsNullOrEmpty(AzureUser.Surname))
                        {
                            newUser.Properties["sn"].Value = AzureUser.Surname;
                        }
                        if (!string.IsNullOrEmpty(AzureUser.DisplayName))
                        {
                            newUser.Properties["displayName"].Value = AzureUser.DisplayName;
                        }
                        if(!string.IsNullOrEmpty(AzureUser.JobTitle))
                        {
                            newUser.Properties["title"].Value = AzureUser.JobTitle;
                        }
                        if(!string.IsNullOrEmpty(AzureUser.City))
                        {
                            newUser.Properties["l"].Value = AzureUser.City;
                        }
                        if(!string.IsNullOrEmpty(AzureUser.Country))
                        {
                            newUser.Properties["co"].Value = AzureUser.Country;
                            newUser.Properties["c"].Value = AzureUser.UsageLocation;
                        }
                        if(!string.IsNullOrEmpty(AzureUser.State))
                        {
                            newUser.Properties["st"].Value = AzureUser.State;
                        }
                        if(!string.IsNullOrEmpty(AzureUser.StreetAddress))
                        {
                            newUser.Properties["streetAddress"].Value = AzureUser.StreetAddress;
                        }
                        if(!string.IsNullOrEmpty(AzureUser.Department))
                        {
                            newUser.Properties["department"].Value = AzureUser.Department;
                        }

                        newUser.CommitChanges();
                        
                        //Call Add Contact to Group if Key exists
                        if (!string.IsNullOrEmpty(Config.PermittedSendersGroupDN))
                        {
                            string UserDN = newUser.Path.Substring(newUser.Path.LastIndexOf('/') + 1);
                            AddADContactToGroup(Logger, Config, OnPremADHelper, UserDN);
                        }
                        
                        CreationCount++;

                        if (Config.VerboseLogUserCreation)
                        {
                            Logger.Debug(String.Format("Created User Contact {0}", AzureUser.Mail));
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error(String.Format("Error when Creating user contact {0} {1}", ex.Message, ex.InnerException != null ? ex.InnerException.Message : ""));
                }                
            }

            return CreationCount;

        }

        //Create Group Contact objects
       public int CreateADGroupContacts(Logger Logger, IConfiguration Config, DirectoryEntry DirEntry, IOnPremADHelper OnPremADHelper, Dictionary<string, IGroup> AzureGroups)
        {
            int CreationCount = 0;
            Logger.Debug("Begining Group Contact Creation...");

            foreach (Group AzureGroup in AzureGroups.Values)
            {
                try
                {
                    if (AzureGroup.MailEnabled == true)
                    {
                        DirectoryEntry newGroup = DirEntry.Children.Add("CN=" + AzureGroup.DisplayName, "contact");

                        //Add Each Property we care about for the respective User account (if not null):
                        if (!string.IsNullOrEmpty(Config.ObjectPrefix))
                        {
                            if(!string.IsNullOrEmpty(AzureGroup.Description))
                            {
                                newGroup.Properties["description"].Value = String.Format("{0} - {1}",Config.ObjectPrefix, AzureGroup.Description);
                            }
                            else
                            {
                                newGroup.Properties["description"].Value = Config.ObjectPrefix;
                            }                            
                        }
                        if (!string.IsNullOrEmpty(AzureGroup.Mail))
                        {
                            newGroup.Properties["Mail"].Value = AzureGroup.Mail;
                            newGroup.Properties["proxyAddresses"].AddRange(new object[] { "SMTP:" + AzureGroup.Mail}); //todo: ADD Any extra Proxy Addresses (if they exist as smtp:<somemail>
                        }
                        if (!string.IsNullOrEmpty(AzureGroup.DisplayName))
                        {
                            newGroup.Properties["displayName"].Value = AzureGroup.DisplayName;
                            //newGroup.Properties["name"].Value = AzureGroup.DisplayName;
                        }

                        newGroup.CommitChanges();
                        
                        CreationCount++;

                        if (Config.VerboseLogUserCreation)
                        {
                            Logger.Debug(String.Format("Created Group Contact {0}", AzureGroup.Mail));
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error(String.Format("Error when Creating user contact {0} {1}", ex.Message, ex.InnerException != null ? ex.InnerException.Message : ""));
                }  
            }

            return CreationCount;
        }

        public void AddADContactToGroup (Logger Logger, IConfiguration Config, IOnPremADHelper OnPremADHelper, string ContactDNPath)
       {
            try
            {
               DirectoryEntry GroupDirectoryEntry = OnPremADHelper.GetADDirectoryEntry(Config.FQDomainName, Config.PermittedSendersGroupDN, Logger);
               GroupDirectoryEntry.Properties["member"].Add(ContactDNPath);
               GroupDirectoryEntry.CommitChanges();
            }
            catch (Exception ex)
            {
                Logger.Error(string.Format("Error when adding Contact to Group {0} , {1} {2}", Config.PermittedSendersGroupDN, ex.Message, ex.InnerException !=null ? ex.InnerException.Message : ""));
            }
       }
            
        //Delete Contact
        public int DeleteADContacts(Logger Logger, IConfiguration Config, DirectoryEntry DirEntry, Dictionary<string, DirectoryEntry> OnPremContacts)
        {
            int DeletionCount = 0;
            Logger.Debug("Begining Contact Deletion...");

            foreach(DirectoryEntry OnPremContact in OnPremContacts.Values)
            {
                try
                {
                    DirectoryEntry ContactObjectToDelete = DirEntry.Children.Find(OnPremContact.Name, "contact");

                    DirEntry.Children.Remove(ContactObjectToDelete);

                    if (Config.VerboseLogUserDeletion)
                    {
                        Logger.Debug(String.Format("Deleted User Contact {0}", OnPremContact.Properties["Mail"].Value.ToString()));
                    }

                    DeletionCount++;
                }
                catch (Exception ex)
                {
                    Logger.Error(String.Format("Error when Deleting user contact {0} {1}", ex.Message, ex.InnerException != null ? ex.InnerException.Message : ""));
                }
            }

            return DeletionCount;
        }
    }
}
