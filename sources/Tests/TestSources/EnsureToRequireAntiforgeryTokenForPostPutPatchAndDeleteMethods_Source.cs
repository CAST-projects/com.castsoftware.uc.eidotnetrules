using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Data.SqlClient;

namespace UnitTests.TestSources
{
    public class EnsureToRequireAntiforgeryTokenForPostPutPatchAndDeleteMethods_SourceController: Controller
    {
        // GET: XSRF
        public ActionResult Index()
        {
            return View();
        }

        /* CWE 352 : XSRF */
        public void func1_KO(SqlConnection connection, HttpRequest Request) // Violation
        {
            string input = Request.QueryString["user"];
            string sql = "insert into Comments(comment) values (@user)";
            SqlCommand cmd = new SqlCommand(sql, connection);
            cmd.Parameters.AddWithValue(input, User);
            connection.Open();
            SqlDataReader reader = cmd.ExecuteReader();
        }

        [HttpPost]
        public void func2_KO(SqlConnection connection, HttpRequest Request) // Violation
        {
            string input = Request.QueryString["user"];
            string sql = "insert into Comments(comment) values (@user)";
            SqlCommand cmd = new SqlCommand(sql, connection);
            cmd.Parameters.AddWithValue(input, User);
            connection.Open();
            SqlDataReader reader = cmd.ExecuteReader();
        }

        [ValidateAntiForgeryToken]
        public void func1_OK(SqlConnection connection, HttpRequest Request) // No violation
        {
            string input = Request.QueryString["user"];
            string sql = "insert into Comments(comment) values (@user)";
            SqlCommand cmd = new SqlCommand(sql, connection);
            cmd.Parameters.AddWithValue(input, User);
            connection.Open();
            SqlDataReader reader = cmd.ExecuteReader();
        }
    }

    [ValidateAntiForgeryToken]
    public class XSRFController : Controller
    {
        // GET: XSRF
        public ActionResult Index()
        {
            return View();
        }

        public void foo(SqlConnection connection, HttpRequest Request) // No violation
        {
            string input = Request.QueryString["user"];
            string sql = "insert into Comments(comment) values (@user)";
            SqlCommand cmd = new SqlCommand(sql, connection);
            cmd.Parameters.AddWithValue(input, User);
            connection.Open();
            SqlDataReader reader = cmd.ExecuteReader();
        }
    }

    public class Klass
    {
        public void Func() { }
    }
}
