using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure.ActiveDirectory.GraphClient;
using Microsoft.Azure.ActiveDirectory.GraphClient.Extensions;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System.DirectoryServices;
using System.DirectoryServices.ActiveDirectory;
using ADCST.Configuration;
using ADCST.Utility;
using NLog;

namespace ADCST
{
    class ContactManagement
    {
        public void ContactSync(Logger Logger, IConfiguration config, IAuthenticationProvidor authProvidor, IAzureADFunctions azureAdFunctions, IOnPremADHelper onPremAdHelper, IOnPremAdFunctions onPremAdFunctions, ActiveDirectoryClient AzureClientSession)
        {

            //Get Entry into On-prem Active Directory Contacts OU.
            DirectoryEntry _OnPremContactsDirectoryEntry = onPremAdHelper.GetADDirectoryEntry(config.FQDomainName, config.ContactsDestinationOUDN, Logger);

            //Gather User Objects for the Work we intend to do later:
            Group _AzureUsersgroup = azureAdFunctions.GetADGroup(AzureClientSession, config.AzureADUserGroup, Logger);
            if (_AzureUsersgroup != null)
            {
                List<Tuple<string, IDirectoryObject>> _AzureGroupMembers = azureAdFunctions.GetAdGroupMembers(_AzureUsersgroup, config, Logger);
                
                if (_AzureGroupMembers.Any(members => members.Item1 == "user"))
                {
                    List<IUser> _AzureGroupUsers = _AzureGroupMembers.Where(member => member.Item1.Equals("user"))
                                                                     .Select(member => member.Item2)
                                                                     .Select(member => member as IUser)
                                                                     .ToList();

                    List<DirectoryEntry> _OnPremContactObjects = onPremAdFunctions.GetOUContactObjects(config.FQDomainName, config.ContactsDestinationOUDN, onPremAdHelper, Logger);


                    #region Add Contact Objects to AD Contacts OU
                    //foreach user in Cloud check if they reside onprem and add them if they dont.

                    if (config.AllowCreationOfADObjects)
                    {
                        Dictionary<string, IUser> azureUsers = _AzureGroupUsers.Where(x => x.Mail != null)
                                                                               .ToDictionary(x => x.Mail.ToLower(), x => x);

                        foreach (string OnPremUser in _OnPremContactObjects.Where(x => x.Properties["Mail"].Value != null)
                                                                           .Select(x => x.Properties["Mail"].Value.ToString()))
                        {
                            azureUsers.Remove(OnPremUser.ToLower());
                        }

                        int CreatedUsers = onPremAdFunctions.CreateADUserContacts(Logger, config, _OnPremContactsDirectoryEntry, onPremAdHelper, azureUsers);

                        Logger.Debug(String.Format("Created {0} user(s) in On-Prem Active Directory", CreatedUsers.ToString()));
                        Console.WriteLine("Created {0} user(s) in On-Prem Active Directory", CreatedUsers.ToString());

                    }
                    #endregion

                    #region Delete Contact Objects from AD OU
                    //foreach user onprem check if they reside in cloud - delete them from AD if they dont (Make this over-rideable with a key)
                    if (config.AllowDeletionOfADObjects)
                    {
                        Dictionary<string, DirectoryEntry> onpremUsers = _OnPremContactObjects.Where(y => y.Properties["Mail"].Value != null)
                                                                                              .ToDictionary(y => y.Properties["Mail"].Value.ToString().ToLower(), y => y);

                        foreach (string AzureUser in _AzureGroupUsers.Where(y => y.Mail != null)
                                                                     .Select(y => y.Mail.ToLower()))
                        {
                            onpremUsers.Remove(AzureUser.ToLower());
                        }

                        int DeletedUsers = onPremAdFunctions.DeleteADContacts(Logger, config, _OnPremContactsDirectoryEntry, onpremUsers);

                        Logger.Debug(String.Format("Deleted {0} user(s) in On-Prem Active Directory", DeletedUsers.ToString()));
                        Console.WriteLine("Deleted {0} user(s) in On-Prem Active Directory", DeletedUsers.ToString());
                    }
                }
                else
                {
                    Console.WriteLine("Could not find any USER objects in group {0}", config.AzureADUserGroup);
                    Logger.Error(String.Format("Could not find any USER objects in group {0}", config.AzureADUserGroup));
                }

            }
            else
            {
                Console.WriteLine("Could not find Group in Azure ({0} to enumerate users from", config.AzureADUserGroup);
                Logger.Error(String.Format("Could not find Group in Azure ({0} to enumerate users from", config.AzureADUserGroup));
            }

            //Close AD Directory Entry Handle
            onPremAdHelper.DisposeADDirectoryEntry(_OnPremContactsDirectoryEntry, Logger);

            Console.WriteLine("Contact Creation/Deletion complete - Changes will be reflected on Office365 Sync on Next Dir-Sync Cycle but may not appear in Address book until the following day.");
            Logger.Debug(@"Contact Creation/Deletion complete - Changes will be reflected on Office365 upon next DirSync.");

            #endregion
        }
    }
}