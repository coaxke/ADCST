using System;
using NLog;
using ADCST.Configuration;
using ADCST.Utility;

namespace ADCST
{
   public static class Program
    {
        public static int Main(string[] args)
        {
            IConfiguration config = new ADCSTConfiguration();
            IAuthenticationProvidor authProvidor = new AuthenticationHelper();
            IAzureADFunctions azureAdFunctions = new AzureADFunctions();
            IOnPremADHelper onPremAdHelper = new OnPremADHelper();
            IOnPremAdFunctions onPremAdFunctions = new OnPremADFunctions();
            Logger Logger = LogManager.GetCurrentClassLogger();
            
            string arg = args.Length == 0 ? string.Empty : args[0];

            ADCST ADCST = new ADCST(arg, Logger, config, authProvidor, azureAdFunctions, onPremAdHelper, onPremAdFunctions);

            return Environment.ExitCode;

        }
    }
}
