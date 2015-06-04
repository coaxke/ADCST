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
        //Setup Objects for Syncing Classes
        ContactManagement _objContactManagement= new ContactManagement();
        GroupManagement _objGroupManagement = new GroupManagement();

        public ADCST(string arg, Logger Logger, IConfiguration config, IAuthenticationProvidor authProvidor, IAzureADFunctions azureAdFunctions, IOnPremADHelper onPremAdHelper, IOnPremAdFunctions onPremAdFunctions)
        {

            if (string.IsNullOrEmpty(arg))
            {
                StartSync(Logger, config, authProvidor, azureAdFunctions, onPremAdHelper, onPremAdFunctions, false);
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
                        StartSync(Logger, config, authProvidor, azureAdFunctions, onPremAdHelper, onPremAdFunctions, true);
                        break;

                    default:
                        StartSync(Logger, config, authProvidor, azureAdFunctions, onPremAdHelper, onPremAdFunctions, false);
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

        
        private void StartSync(Logger Logger, IConfiguration config, IAuthenticationProvidor authProvidor,  IAzureADFunctions azureAdFunctions, IOnPremADHelper onPremAdHelper, 
                                     IOnPremAdFunctions onPremAdFunctions, bool ShowDiagnostics)
        {
            ActiveDirectoryClient ClientSession = azureAdFunctions.ADClient(config, authProvidor, Logger);

            //Show Azure Tennant Diagnostics if requested.
            if(ShowDiagnostics)
            {
                Console.WriteLine(azureAdFunctions.TenantDetails(ClientSession, Logger, config));
            }            
            //TODO RE-ENABLE THE BELOW METHOD!
            //We're done outputting debug info - Call the applications main logic.
            _objContactManagement.ContactSync(Logger, config, authProvidor, azureAdFunctions, onPremAdHelper, onPremAdFunctions, ClientSession); 
            _objGroupManagement.GroupSync(Logger, config, authProvidor, azureAdFunctions, onPremAdHelper, onPremAdFunctions, ClientSession);
        }
    }
}
