using System;
using NLog;
using ADCST.Configuration;

namespace ADCST
{
   public static class Program
    {
        public static int Main(string[] args)
        {
            IConfiguration config = new ADCSTConfiguration();
            Logger Logger = LogManager.GetCurrentClassLogger();


            string arg = args.Length == 0 ? string.Empty : args[0];

            return Environment.ExitCode;

        }
    }
}
