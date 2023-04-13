using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;

namespace UnitTests.UnitTest.Sources
{
    public class EnsureToSetBothEncryptAndTrustServerCertificateToTrueWhenConnectingToSqlServer_Source
    {
        public void Missing_encrypt_or_certificate()
        {
            SqlConnectionStringBuilder strbldr = new SqlConnectionStringBuilder();
            strbldr.DataSource = "server63";
            strbldr.InitialCatalog = "Clinic";
            strbldr.IntegratedSecurity = true;
            SqlConnection connection = new SqlConnection(strbldr.ConnectionString);// VIOLATION
        }

        public void Missing_encrypt_or_certificate2()
        {
            string connectionString = "Data Source=server63; Initial Catalog=Clinic; Integrated Security=true";
            SqlConnection connection = new SqlConnection(connectionString); // VIOLATION
        }

        public void Missing_encrypt_or_certificate3()
        {
            string connectionString = "Data Source=server63; Initial Catalog=Clinic; Integrated Security=true; Encrypt=true";
            SqlConnection connection = new SqlConnection(connectionString); // VIOLATION
        }

        public void Missing_encrypt_or_certificate4()
        {
            string connectionString = "Data Source=server63; Initial Catalog=Clinic; Integrated Security=true; TrustServerCertificate=true";
            SqlConnection connection = new SqlConnection(connectionString); // VIOLATION
        }

        public void Missing_encrypt_or_certificate5()
        {
            SqlConnectionStringBuilder strbldr = new SqlConnectionStringBuilder();
            strbldr.DataSource = "server63";
            strbldr.InitialCatalog = "Clinic";
            strbldr.IntegratedSecurity = true;
            strbldr.TrustServerCertificate = true;
            SqlConnection connection = new SqlConnection(strbldr.ConnectionString);// VIOLATION
        }

        public void Missing_encrypt_or_certificate6()
        {
            SqlConnectionStringBuilder strbldr = new SqlConnectionStringBuilder();
            strbldr.DataSource = "server63";
            strbldr.InitialCatalog = "Clinic";
            strbldr.IntegratedSecurity = true;
            strbldr.Encrypt = true;
            SqlConnection connection = new SqlConnection(strbldr.ConnectionString);// VIOLATION
        }

        public void Set_encrypt_or_certificate()
        {
            SqlConnectionStringBuilder strbldr = new SqlConnectionStringBuilder();
            strbldr.DataSource = "server63";
            strbldr.InitialCatalog = "Clinic";
            strbldr.IntegratedSecurity = true;
            strbldr.Encrypt = true; // Fixed
            strbldr.TrustServerCertificate = true; // Fixed
            SqlConnection connection = new SqlConnection(strbldr.ConnectionString);
        }

        public void Set_encrypt_or_certificate2()
        {
            string connectionString = "Data Source=server63; Initial Catalog=Clinic; Integrated Security=true; Column Encryption Setting=enabled; Encrypt=true; TrustServerCertificate=true";// Fixed
            SqlConnection connection = new SqlConnection(connectionString);
        }

        public void Set_encrypt_or_certificate3()
        {
            SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder();
            builder["Data Source"] = "(local)";
            builder["Initial Catalog"] = "AdventureWorks;NewValue=Bad";
            builder["Encrypt"] = true; // Fixed 
            builder["TrustServerCertificate"] = true; // Fixed 
            SqlConnection connection = new SqlConnection(builder.ConnectionString);

            SqlConnectionStringBuilder builder2 = new SqlConnectionStringBuilder();
            builder2["Column Encryption Setting"] = 1; // Fixed
            SqlConnection connection2 = new SqlConnection(builder2.ConnectionString);

            SqlConnectionStringBuilder builder3 = new SqlConnectionStringBuilder();
            builder3["Column Encryption Setting"] = "enabled"; // Fixed
            SqlConnection connection3 = new SqlConnection(builder3.ConnectionString);
        }

        public void NoConnectionString()
        {
            SqlConnection connection = new SqlConnection();
        }

        private string conString = "";
        public void NonLocalConnectionString()
        {
            SqlConnection connection = new SqlConnection(conString);
        }
    }
}
