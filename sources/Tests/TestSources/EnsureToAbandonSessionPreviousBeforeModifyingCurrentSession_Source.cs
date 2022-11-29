using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace UnitTests.TestSources
{
    public class EnsureToAbandonSessionPreviousBeforeModifyingCurrentSession_Source
    {
        static void foo(string firstName)
        {
            HttpContext context = HttpContext.Current;
            context.Session["FirstName"] = firstName; // VIOLATION
        }

        static void foo(string firstName, HttpContext old_Context)
        {
            old_Context.Session.Abandon();               // FIXED
            HttpContext context = HttpContext.Current;
            context.Session["FirstName"] = firstName;
        }
    }
}
