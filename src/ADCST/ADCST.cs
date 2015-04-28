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
    class ADCST
    {

        public ADCST(string arg, Logger Logger, IConfiguration config, IAuthenticationProvidor authProvidor, IAzureADFunctions azureAdFunctions, IOnPremADHelper onPremAdHelper, IOnPremAdFunctions onPremAdFunctions)
        {
            if (string.IsNullOrEmpty(arg))
            {
                ShowHelp();
            }
            else
            {
                switch (arg.ToLower())
                {
                    case @"/h":
                    case @"--h":
                    case @"-h":
                    case @"h":
                        ShowHelp();
                        break;

                    case @"/d":
                    case @"--d":
                    case @"-d":
                    case @"d":
                        ShowDiagnostics(Logger, config, authProvidor, azureAdFunctions, onPremAdHelper, onPremAdFunctions);
                        break;

                    default:
                        ADCSTMain(Logger, config, authProvidor, azureAdFunctions, onPremAdHelper, onPremAdFunctions);
                        break;
                }
            }

        }

        private void ShowHelp()
        {
            Console.WriteLine();
            Console.WriteLine(@"========================================================");
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine(@"A.D.C.S.T (Active Directory Contact Sync Tool)");
            Console.ResetColor();
            Console.WriteLine(@"========================================================");

            Console.WriteLine();
            Console.WriteLine(@"Usage: ADCST.exe [-d] [-h]");
            Console.WriteLine();

            Console.WriteLine(@"Options:");
            Console.WriteLine(@"  -d  Shows Azure Active Directory verbose Tennant/Diagnostic Information");
            Console.WriteLine(@"  -h  Shows this Help Message");

        }

        private void ShowDiagnostics(Logger Logger, IConfiguration config, IAuthenticationProvidor authProvidor,  IAzureADFunctions azureAdFunctions, IOnPremADHelper onPremAdHelper, 
                                     IOnPremAdFunctions onPremAdFunctions)
        {
            ActiveDirectoryClient ClientSession = azureAdFunctions.ADClient(config, authProvidor, Logger);
            ActiveDirectoryClient[] ADDirectoryClientContainer = new ActiveDirectoryClient[] { ClientSession };

            Console.WriteLine(azureAdFunctions.TenantDetails(ClientSession, Logger, config));

            //We're done outputting debug info - Call the applications main logic.
            ADCSTMain(Logger, config, authProvidor, azureAdFunctions, onPremAdHelper, onPremAdFunctions, ADDirectoryClientContainer);
        }

        private void ADCSTMain(Logger Logger, IConfiguration config, IAuthenticationProvidor authProvidor,  IAzureADFunctions azureAdFunctions, IOnPremADHelper onPremAdHelper, IOnPremAdFunctions onPremAdFunctions, ActiveDirectoryClient[] AdClient = null)
        {
            ActiveDirectoryClient _ClientSession;

            if (AdClient[0] == null)
            {
                _ClientSession = azureAdFunctions.ADClient(config, authProvidor, Logger);
            }
            else
            {
                _ClientSession = AdClient[0];
            }
            

           //Get Entry into On-prem Active Directory.
            DirectoryEntry _OnPremDirectoryEntry = onPremAdHelper.GetADDirectoryEntry(config.FQDomainName, config.DestinationOUDN, Logger);
            
            //Gather User Objects for the Work we intend to do later:
            Group _AzureUsersgroup = azureAdFunctions.GetADGroup(_ClientSession, config.AzureADUserGroup, Logger);
            if(_AzureUsersgroup !=null)
            {
                List<IDirectoryObject> _AzureGroupMembers = azureAdFunctions.GetAdGroupMembers(_AzureUsersgroup, config, Logger);
                List<IUser> _AzureGroupUsers = _AzureGroupMembers.ConvertAll(members => members as IUser);

                List<DirectoryEntry> _OnPremContactObjects = onPremAdFunctions.GetOUContactObjects(config, onPremAdHelper, Logger);


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

                  int CreatedUsers = onPremAdFunctions.CreateADContacts(Logger, config, _OnPremDirectoryEntry, azureUsers);

                  Logger.Debug(String.Format("Created {0} users in On-Prem Active Directory", CreatedUsers.ToString()));
                  Console.WriteLine("Created {0} users in On-Prem Active Directory", CreatedUsers.ToString());
                   
                }

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

                    int DeletedUsers = onPremAdFunctions.DeleteADContacts(Logger, config, _OnPremDirectoryEntry, onpremUsers);

                    Logger.Debug(String.Format("Deleted {0} users in On-Prem Active Directory", DeletedUsers.ToString()));
                    Console.WriteLine("Deleted {0} users in On-Prem Active Directory", DeletedUsers.ToString());
                }
            }
            else
            {
                Console.WriteLine("Could not find Group in Azure ({0} to enumerate users from", config.AzureADUserGroup);
                Logger.Error(String.Format("Could not find Group in Azure ({0} to enumerate users from", config.AzureADUserGroup));
            }

            Console.WriteLine("Contact Creation/Deletion complete - Changes will be reflected on Office365 Sync on Next Dir-Sync Cycle but may not appear in Address book until the following day.");
            Logger.Debug(@"Contact Creation/Deletion complete - Changes will be reflected on Office365 upon next DirSync.");

            //Close AD Directory Entry Handle
            onPremAdHelper.DisposeADDirectoryEntry(_OnPremDirectoryEntry, Logger);        
        }
    }
}
