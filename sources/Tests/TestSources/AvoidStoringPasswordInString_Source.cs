using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnitTests.TestSources
{
    public class AvoidStoringPasswordInString_Source
    {
        private string Password { get; set; } // violation

        private string _passwd; // violation

        public void SetPassword(string newPassword) // violation
        {
            var passwd = newPassword;
            var authorization = newPassword;
            Password = newPassword;
        }

        public string GetPassword()
        {
            var auth = Password;
            return auth;
        }
        public DateTime DateOfBirth { get; set; }
    }
}
