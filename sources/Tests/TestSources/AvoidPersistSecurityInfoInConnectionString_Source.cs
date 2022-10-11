using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;
using System.Data.Common;
using System.Data;

namespace UnitTests.UnitTest.Sources
{
    class AvoidPersistSecurityInfoInConnectionString_Source
    {
        public void func()
        {

            SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder();
            builder["Data Source"] = "(local)";
            builder["Persist Security Info"] = true;     // VIOLATION
            builder["Initial Catalog"] = "AdventureWorks;NewValue=Bad";
            builder.PersistSecurityInfo = true;
            DbConnectionStringBuilder builder2 = new DbConnectionStringBuilder();
            builder2.ConnectionString = @"Data Source=c:\MyData\MyDb.mdb";
            builder2.Add("Initial Catalog", "TheDatabase");
            builder2.Add("Persist Security Info", true);
            builder2.Add("PersistSecurityInfo", true);
        }
    }
}

