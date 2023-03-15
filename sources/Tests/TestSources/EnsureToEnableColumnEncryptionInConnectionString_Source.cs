using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;

namespace UnitTests.UnitTest.Sources
{
    public class EnsureToEnableColumnEncryptionInConnectionString_Source
    {
        public void Missing_column_encryption()
        {
            SqlConnectionStringBuilder strbldr = new SqlConnectionStringBuilder();
            strbldr.DataSource = "server63";
            strbldr.InitialCatalog = "Clinic";
            strbldr.IntegratedSecurity = true;
            SqlConnection connection = new SqlConnection(strbldr.ConnectionString);// VIOLATION
        }

        public void Missing_column_encryption2()
        {
            string connectionString = "Data Source=server63; Initial Catalog=Clinic; Integrated Security=true";    
            SqlConnection connection = new SqlConnection(connectionString); // VIOLATION
        }

        public void Enable_column_encryption()
        {
            SqlConnectionStringBuilder strbldr = new SqlConnectionStringBuilder();
            strbldr.DataSource = "server63";
            strbldr.InitialCatalog = "Clinic";
            strbldr.IntegratedSecurity = true;
            strbldr.ColumnEncryptionSetting = SqlConnectionColumnEncryptionSetting.Enabled; // Fixed
            SqlConnection connection = new SqlConnection(strbldr.ConnectionString);
        }

        public void Enable_column_encryption2()
        {
            string connectionString = "Data Source=server63; Initial Catalog=Clinic; Integrated Security=true; Column Encryption Setting=enabled";// Fixed
            SqlConnection connection = new SqlConnection(connectionString);
        }

        public void Enable_column_encryption3()
        {
            SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder();
            builder["Data Source"] = "(local)";
            builder["Initial Catalog"] = "AdventureWorks;NewValue=Bad";
            builder["Column Encryption Setting"] = true; // Fixed 
            SqlConnection connection = new SqlConnection(builder.ConnectionString);

            SqlConnectionStringBuilder builder2 = new SqlConnectionStringBuilder();
            builder2["Column Encryption Setting"] = 1; // Fixed
            SqlConnection connection2 = new SqlConnection(builder2.ConnectionString);

            SqlConnectionStringBuilder builder3 = new SqlConnectionStringBuilder();
            builder3["Column Encryption Setting"] = "enabled"; // Fixed
            SqlConnection connection3 = new SqlConnection(builder3.ConnectionString);
        }
    }
}
