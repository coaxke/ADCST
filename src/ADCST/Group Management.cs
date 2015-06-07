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
    class GroupManagement
    {
        public void GroupSync(Logger Logger, IConfiguration config, IAuthenticationProvidor authProvidor, IAzureADFunctions azureAdFunctions, IOnPremADHelper onPremAdHelper, IOnPremAdFunctions onPremAdFunctions, ActiveDirectoryClient AzureClientSession)
        {
            //Get Entry into On-prem Active Directory Groups OU.
            DirectoryEntry _OnPremGroupsDirectoryEntry = onPremAdHelper.GetADDirectoryEntry(config.FQDomainName, config.GroupsDestinationOUDN, Logger);

            //Gather User Objects for the Work we intend to do later:
            Group _AzureUsersgroup = azureAdFunctions.GetADGroup(AzureClientSession, config.AzureADGroupsGroup, Logger);
            if (_AzureUsersgroup != null)
            {
                List<Tuple<string, IDirectoryObject>> _AzureGroupMembers = azureAdFunctions.GetAdGroupMembers(_AzureUsersgroup, config, Logger);
                
                if (_AzureGroupMembers.Any(members => members.Item1 == "group"))
                {
                    List<IGroup> _AzureGroupGroups = _AzureGroupMembers.Where(member => member.Item1.Equals("group"))
                                                 .Select(member => member.Item2)
                                                 .Select(member => member as IGroup)
                                                 .ToList();

                    List<DirectoryEntry> _OnPremContactObjects = onPremAdFunctions.GetOUContactObjects(config.FQDomainName, config.GroupsDestinationOUDN, onPremAdHelper, Logger);

                    #region Add Contact Objects to AD Contacts OU
                    //foreach group in Cloud check if they reside onprem and add them if they dont.

                    if (config.AllowCreationOfADObjects)
                    {
                        Dictionary<string, IGroup> azureGroups = _AzureGroupGroups.Where(x => x.Mail != null)
                                                                               .ToDictionary(x => x.Mail.ToLower(), x => x);

                        foreach (string OnPremUser in _OnPremContactObjects.Where(x => x.Properties["Mail"].Value != null)
                                                                           .Select(x => x.Properties["Mail"].Value.ToString()))
                        {
                            azureGroups.Remove(OnPremUser.ToLower());
                        }

                        int CreatedUsers = onPremAdFunctions.CreateADGroupContacts(Logger, config, _OnPremGroupsDirectoryEntry, onPremAdHelper, azureGroups);

                        Logger.Debug(String.Format("Created {0} group(s) in On-Prem Active Directory", CreatedUsers.ToString()));
                        Console.WriteLine("Created {0} group(s) in On-Prem Active Directory", CreatedUsers.ToString());

                    }
                    #endregion

                    #region Delete Group Objects from AD OU

                     //foreach group onprem check if they reside in cloud - delete them from AD if they dont (Make this over-rideable with a key)
                    if (config.AllowDeletionOfADObjects)
                    {
                        Dictionary<string, DirectoryEntry> onpremGroups = _OnPremContactObjects.Where(y => y.Properties["Mail"].Value != null)
                                                                                              .ToDictionary(y => y.Properties["Mail"].Value.ToString().ToLower(), y => y);

                        foreach (string AzureUser in _AzureGroupGroups.Where(y => y.Mail != null)
                                                                     .Select(y => y.Mail.ToLower()))
                        {
                            onpremGroups.Remove(AzureUser.ToLower());
                        }

                        int DeletedGroups = onPremAdFunctions.DeleteADContacts(Logger, config, _OnPremGroupsDirectoryEntry, onpremGroups);

                        Logger.Debug(String.Format("Deleted {0} group(s) in On-Prem Active Directory", DeletedGroups.ToString()));
                        Console.WriteLine("Deleted {0} group(s) in On-Prem Active Directory", DeletedGroups.ToString());
                    }
                }
                else
                {
                    Console.WriteLine("Could not find any GROUP objects in group {0}", config.AzureADUserGroup);
                    Logger.Error(String.Format("Could not find any GROUP objects in group {0}", config.AzureADUserGroup));
                }

            }
            else
            {
                Console.WriteLine("Could not find Group in Azure ({0} to enumerate users from", config.AzureADUserGroup);
                Logger.Error(String.Format("Could not find Group in Azure ({0} to enumerate users from", config.AzureADUserGroup));
            }

            //Close AD Directory Entry Handle
            onPremAdHelper.DisposeADDirectoryEntry(_OnPremGroupsDirectoryEntry, Logger);

            Console.WriteLine("Group Creation/Deletion complete - Changes will be reflected on Office365 Sync on Next Dir-Sync Cycle but may not appear in Address book until the following day.");
            Logger.Debug(@"Group Creation/Deletion complete - Changes will be reflected on Office365 upon next DirSync.");

            #endregion   

        }
    }
}

