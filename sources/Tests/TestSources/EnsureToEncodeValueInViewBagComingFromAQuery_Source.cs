using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;
using System.Data.SqlClient;

namespace UnitTests.TestSources
{
    class EnsureToEncodeValueInViewBagComingFromAQuery_Source : Controller
    {
        //CWE-79 : Improper Neutralization of Input During Web Page Generation ('Cross-site Scripting')
        //Chmx name : Stored_XSS
        public void XssInjection(string currentUsername)
        {
            SqlConnection conn = new SqlConnection("Initial Catalog=appDb;User Id=sa;Pwd=mypass;");// ANOMALY RULE : {547, Use of Hard-coded, Security-relevant Constants}
            conn.Open();
            SqlCommand command = new SqlCommand("Select address from Users where User_Name=@user_name", conn);
            command.Parameters.AddWithValue("@user_name", currentUsername);

            using (SqlDataReader reader = command.ExecuteReader())
            {
                if (reader.Read())
                {
                    ViewBag.Address = reader["address"]; // ANOMALY RULE : {79, Improper Neutralization of Input During Web Page Generation ('Cross-site Scripting')}
                }
            }
        }

        public void XssInjection2(string currentUsername)
        {
            SqlConnection conn = new SqlConnection("Initial Catalog=appDb;User Id=sa;Pwd=mypass;");// ANOMALY RULE : {547, Use of Hard-coded, Security-relevant Constants}
            conn.Open();
            SqlCommand command = new SqlCommand("Select address from Users where User_Name=@user_name", conn);
            command.Parameters.AddWithValue("@user_name", currentUsername);
            
            using (SqlDataReader reader = command.ExecuteReader())
            {
                if (reader.Read())
                {
                    object val = reader["address"], val2 = reader["address"], val3 = reader.GetString(0); 
                    ViewBag.Address = val; // ANOMALY RULE : {79, Improper Neutralization of Input During Web Page Generation ('Cross-site Scripting')}
                }
            }
        }

        public void XssInjection3(string currentUsername)
        {
            SqlConnection conn = new SqlConnection("Initial Catalog=appDb;User Id=sa;Pwd=mypass;");// ANOMALY RULE : {547, Use of Hard-coded, Security-relevant Constants}
            conn.Open();
            SqlCommand command = new SqlCommand("Select address from Users where User_Name=@user_name", conn);
            command.Parameters.AddWithValue("@user_name", currentUsername);

            using (SqlDataReader reader = command.ExecuteReader())
            {
                if (reader.Read())
                {
                    ViewBag.Address = reader.GetString(0);// ANOMALY RULE : {79, Improper Neutralization of Input During Web Page Generation ('Cross-site Scripting')}
                }
            }
        }

        public void XssInjection_No_Violation(string currentUsername)
        {
            SqlConnection conn = new SqlConnection("Initial Catalog=appDb;User Id=sa;Pwd=mypass;");
            conn.Open();
            SqlCommand command = new SqlCommand("Select address from Users where User_Name=@user_name", conn);
            command.Parameters.AddWithValue("@user_name", currentUsername);

            using (SqlDataReader reader = command.ExecuteReader())
            {
                if (reader.Read())
                {
                    var val = reader["address"]; // ANOMALY RULE : {79, Improper Neutralization of Input During Web Page Generation ('Cross-site Scripting')}
                }
            }
        }
    }
}
