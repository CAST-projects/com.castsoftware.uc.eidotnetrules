using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;
using System.Threading;

namespace UnitTests.UnitTest.Sources
{
    public class AvoidUsingThreadSleepWithDynamicParameterInAControllerAction_Source : Controller
    {
        public ActionResult Index(int time)
        {
            Thread.Sleep(2000); // No Violation
            return View();
        }

        public ActionResult Details(int time)
        {
            Thread.Sleep(time); // Violation
            return View();
        }

        public ActionResult UpdateUser(User user) 
        {
            Thread.Sleep(user.Sleep); // Violation
            return View();
        }


        [HttpPost]
        public ActionResult Edit()
        {
            if (ModelState.IsValid)
            {
                var user = new User();
                TryUpdateModel(user);
                Thread.Sleep(user.Sleep); // Violation
                return RedirectToAction("Index");
            }
            return View();
        }


        public class User
        {
            public string UserName { get; set; }
            public string Password { get; set; }
            public int UserAge { get; set; }
            public int UserId { get; set; }
            public bool IsAdmin { get; set; }
            public int Sleep { get; set; }
        }
    }

}
