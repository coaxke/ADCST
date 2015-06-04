using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ADCST.Configuration;
using NLog;
using System.DirectoryServices;
using System.DirectoryServices.ActiveDirectory;

namespace ADCST.Utility
{
    public interface IOnPremADHelper
    {
        DirectoryEntry GetADDirectoryEntry(string FQDomainName, string LDAPPath, Logger Logger);
        void DisposeADDirectoryEntry(DirectoryEntry DirEntry, Logger Logger);
    }
    public sealed class OnPremADHelper : IOnPremADHelper
    {

       public DirectoryEntry GetADDirectoryEntry (string FQDomainName, string LDAPPath, Logger Logger)
        {
            Logger.Debug(string.Format(@"Getting Directory Entry for LDAP Path {0}", LDAPPath));

            //DirectoryEntry LDAPConnection = new DirectoryEntry(FQDomainName);
            DirectoryEntry LDAPConnection = new DirectoryEntry("LDAP://"+FQDomainName);

            LDAPConnection.Path = "LDAP://"+LDAPPath;
            LDAPConnection.AuthenticationType = AuthenticationTypes.Secure;
            
           try
           {
               //Try fetch to Native ADSI Object  - if this passes Then we know that we 
               //are authenticated. If an exception is thrown then we know we are using an Invalid Account.

               object DirEntryNativeObject = LDAPConnection.NativeObject;
               
           }
           catch (Exception ex)
           {
               Logger.Error(String.Format("Error authenticating to Active Directory with current running user. {0} {1}.",
                   ex.Message, ex.InnerException != null ? ex.InnerException.Message : ""));
               
               throw new Exception(String.Format("Error authenticating to Active Directory with current running user. {0} {1}.",
                   ex.Message, ex.InnerException != null ? ex.InnerException.Message : "" ));               
           }

           return LDAPConnection;
        }

        public void DisposeADDirectoryEntry(DirectoryEntry DirEntry, Logger Logger)
       {
            if (DirEntry != null)
            {
                DirEntry.Close();
            }

            Logger.Debug(@"Directory Entry Closed");
       }
    }
}
