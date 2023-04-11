using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security;
using System.Diagnostics;

namespace UnitTests.TestSources
{
    class AvoidSecurityCriticalInformationExposure_Source
    {
        [SecurityCritical]
        static int x = 10;
        private string _password = "pass";
        public string Password
        {
            [SecurityCritical]
            get { return _password; }
        }

        [SecurityCritical]
        public string getAccountInfo()
        {
            return "account info";
        }

        public void func(string[] args)
        {
            Console.WriteLine(" Critical X " + x); // Violation
            Console.WriteLine(" Password " + Password); // Violation 
            Console.WriteLine(" AccountInfo " + getAccountInfo()); // Violation
            Debug.WriteLine(" Password " + Password); // Violation 
        }
    }
}
