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
        List<DirectoryEntry> GetOUContactObjects(IConfiguration Config, IOnPremADHelper OnPremADHelper, Logger Logger);
        int CreateADContacts (Logger Logger, IConfiguration Config, DirectoryEntry DirEntry, Dictionary<string, IUser>AzureUsers);
        int DeleteADContacts (Logger Logger, IConfiguration Config, DirectoryEntry DirEntry, Dictionary<string, DirectoryEntry> OnPremContacts);
    }

    public sealed class OnPremADFunctions : IOnPremAdFunctions
    {
        //Search Contact
        public List<DirectoryEntry> GetOUContactObjects (IConfiguration Config, IOnPremADHelper OnPremADHelper, Logger Logger)
        {
            SearchResultCollection ListADContactResults;
            List <DirectoryEntry> ADContacts = new List<DirectoryEntry>();

            //Will enter Directory at $DestinationOUDN and retrieve ALL properties (empty string array)
            using (DirectorySearcher ListADContacts = new DirectorySearcher(OnPremADHelper.GetADDirectoryEntry(Config.FQDomainName,Config.DestinationOUDN, Logger), 
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
                Logger.Debug(String.Format("No Contact objects found in {0}", Config.DestinationOUDN));
                Console.WriteLine("No Contact objects found in {0}", Config.DestinationOUDN);

                //Retrun an empty list.
                return ADContacts;
            }
        }
   
        //Create Contact 
        public int CreateADContacts(Logger Logger, IConfiguration Config, DirectoryEntry DirEntry, Dictionary<string, IUser> AzureUsers)
        {
            int CreationCount = 0;
            Logger.Debug("Begining Contact Creation...");
            foreach (User AzureUser in AzureUsers.Values)
            {
                try
                {
                    if (AzureUser.AccountEnabled.HasValue && AzureUser.AccountEnabled.Value)
                    {

                        DirectoryEntry newUser = DirEntry.Children.Add("CN=" + AzureUser.Mail, "contact");

                        //Add Each Property we care about for the respective User account (if not null):
                        if (!string.IsNullOrEmpty(Config.ContactPrefix))
                        {
                            newUser.Properties["description"].Value = Config.ContactPrefix;
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
            
        //Delete Contact
        public int DeleteADContacts(Logger Logger, IConfiguration Config, DirectoryEntry DirEntry, Dictionary<string, DirectoryEntry> OnPremContacts)
        {
            int DeletionCount = 0;
            Logger.Debug("Begining Contact Deletion...");

            foreach(DirectoryEntry OnPremContact in OnPremContacts.Values)
            {
                try
                {
                    DirectoryEntry ContactObjectToDelete = DirEntry.Children.Find("CN="+ OnPremContact.Properties["Mail"].Value.ToString(), "contact");

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
