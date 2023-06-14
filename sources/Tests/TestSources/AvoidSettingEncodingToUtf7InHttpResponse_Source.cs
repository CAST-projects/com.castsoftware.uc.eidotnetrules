using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace UnitTests.TestSources
{
    public class AvoidSettingEncodingToUtf7InHttpResponse_Source : Controller
    {
        //CWE-79: Improper Neutralization of Input During Web Page Generation('Cross-site Scripting')
        //Chmx name : UTF7_XSS
        [HttpPost]
        public ActionResult UTF7_XSS()
        {
            Response.Charset = "UTF-7";// ANOMALY RULE : {79, Improper Neutralization of Input During Web Page Generation('Cross-site Scripting')}
            ViewBag.name = Request.Params["name"];

            return View();
        }

        public ActionResult UTF7_XSS_No_Violation()
        {
            Response.Charset = "UTF-8";// ANOMALY RULE : {79, Improper Neutralization of Input During Web Page Generation('Cross-site Scripting')}
            ViewBag.name = Request.Params["name"];
            var toto = "UTF-7";
            return View();
        }
    }
}
