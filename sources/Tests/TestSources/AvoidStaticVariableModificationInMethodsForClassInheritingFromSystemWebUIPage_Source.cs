using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Web.UI;

namespace UnitTests.UnitTest.Sources
{
    public class AvoidStaticVariableModificationInMethodsForClassInheritingFromSystemWebUIPage_Source 
    {
        //CWE-366: Race Condition within a Thread
        public partial class _Default : System.Web.UI.Page
        {
            public static string secret = "None";
            static object locker = new object();
            protected void Page_Load(object sender, EventArgs e)
            {
                secret = Request.Params["secret"];// ANOMALY RULE : {366, Race Condition within a Thread}
            }

            protected void Page_Load2(object sender, EventArgs e)
            {
                lock (locker)
                {
                    secret = Request.Params["secret"];//NO VIOLATION
                }
            }

            protected void Page_Load3(object sender, EventArgs e)
            {
                Monitor.Enter(locker);
                secret = Request.Params["secret"];//NO VIOLATION  
                Monitor.Exit(locker);
            }

            private static Mutex mut = new Mutex();
            protected void Page_Load4(object sender, EventArgs e)
            {
                mut.WaitOne();
                secret = Request.Params["secret"];//NO VIOLATION  
                mut.ReleaseMutex();
            }
        }
       

        
    }
}
