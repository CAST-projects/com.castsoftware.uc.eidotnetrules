using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnitTests.TestSources
{
    public class AvoidCopyingBufferWithoutCheckingTheSizeOfInput_Source
    {
        /*
         CWE 120 : Buffer_Overflow
         */
        public int GetString(ref byte[] buffer, int buflen)
        {
            string mystring = "hello world";

            // I have tried this:
            System.Text.UTF8Encoding encoding = new System.Text.UTF8Encoding();
            buffer = encoding.GetBytes(mystring);

            // and tried this:
            System.Buffer.BlockCopy(mystring.ToCharArray(), 0, buffer, 0, buflen); // ANOMALY RULE : {120, Buffer Copy without Checking Size of Input ('Classic Buffer Overflow')}
            return (buflen);
        }

        public int GetString2(ref byte[] buffer, int buflen)
        {
            string mystring = "hello world";

            // I have tried this:
            System.Text.UTF8Encoding encoding = new System.Text.UTF8Encoding();
            buffer = encoding.GetBytes(mystring);

            // and tried this:
            if(buflen <= mystring.Length)
                System.Buffer.BlockCopy(mystring.ToCharArray(), 0, buffer, 0, buflen); // ANOMALY RULE : {120, Buffer Copy without Checking Size of Input ('Classic Buffer Overflow')}
            return (buflen);
        }
    }
}
