using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;

namespace UnitTests.TestSources
{
    public class AvoidExposedDangerousMethod_Source
    {
        public void RemoveKO()
        {
            string databaseName = "";
            string tableName = "";
            string viewName = "";
            string columnName = "";
            string connectionString = "";
            using (SqlConnection connexion = new SqlConnection(connectionString))
            {
                string sql = "DROP DATABASE " + databaseName;
                using (SqlCommand readDetailsCommand = new SqlCommand(sql, connexion))
                {
                    using (SqlDataReader reader = readDetailsCommand.ExecuteReader())// ANOMALY RULE : {749, Exposed Dangerous Method or Function}
                    {
                        reader.Read();
                    }
                }
                Int32 newProdID = 0;
                using (SqlCommand readDetailsCommand = new SqlCommand("DROP DATABASE " + databaseName, connexion))
                {
                    newProdID = (Int32)readDetailsCommand.ExecuteScalar();// ANOMALY RULE : {749, Exposed Dangerous Method or Function}  
                }

                string sqlTable = "DROP TABLE " + tableName;
                using (SqlCommand readDetailsCommand = new SqlCommand(sqlTable, connexion))
                {
                    System.Xml.XmlReader reader = readDetailsCommand.ExecuteXmlReader();// ANOMALY RULE : {749, Exposed Dangerous Method or Function}
                }

                string sqlView = "DROP VIEW " + viewName;
                using (SqlCommand readDetailsCommand = new SqlCommand(sqlView, connexion))
                {
                    using (SqlDataReader reader = readDetailsCommand.ExecuteReader())// ANOMALY RULE : {749, Exposed Dangerous Method or Function}
                    {
                        reader.Read();
                    }
                }

                string sqlColumn = "ALTER TABLE Customers DROP COLUMN " + columnName;
                using (SqlCommand readDetailsCommand = new SqlCommand(sqlColumn, connexion))
                {
                    using (SqlDataReader reader = readDetailsCommand.ExecuteReader())// ANOMALY RULE : {749, Exposed Dangerous Method or Function}
                    {
                        reader.Read();
                    }
                }
                string sqlDelete;
                sqlDelete = "DELETE FROM Customers WHERE CustomerName='Alfreds Futterkiste';";
                SqlCommand readDetailsCommand2;
                using (readDetailsCommand2 = new SqlCommand(sqlDelete, connexion))
                {
                    using (SqlDataReader reader = readDetailsCommand2.ExecuteReader())// ANOMALY RULE : {749, Exposed Dangerous Method or Function}
                    {
                        reader.Read();
                    }
                }

                using (SqlDataReader reader = (new SqlCommand("DROP DATABASE " + databaseName, connexion)).ExecuteReader())// ANOMALY RULE : {749, Exposed Dangerous Method or Function}
                {
                    reader.Read();
                }
            }
        }




        private void RemoveOK()
        {
            string databaseName = "";
            string tableName = "";
            string viewName = "";
            string columnName = "";
            string connectionString = "";
            using (SqlConnection connexion = new SqlConnection(connectionString))
            {
                string sql = "DROP DATABASE " + databaseName;
                using (SqlCommand readDetailsCommand = new SqlCommand(sql, connexion))
                {
                    using (SqlDataReader reader = readDetailsCommand.ExecuteReader())// ANOMALY RULE : {749, Exposed Dangerous Method or Function}
                    {
                        reader.Read();
                    }
                }
                Int32 newProdID = 0;
                using (SqlCommand readDetailsCommand = new SqlCommand("DROP DATABASE " + databaseName, connexion))
                {
                    newProdID = (Int32)readDetailsCommand.ExecuteScalar();// ANOMALY RULE : {749, Exposed Dangerous Method or Function}  
                }

                string sqlTable = "DROP TABLE " + tableName;
                using (SqlCommand readDetailsCommand = new SqlCommand(sqlTable, connexion))
                {
                    System.Xml.XmlReader reader = readDetailsCommand.ExecuteXmlReader();// ANOMALY RULE : {749, Exposed Dangerous Method or Function}
                }

                string sqlView = "DROP VIEW " + viewName;
                using (SqlCommand readDetailsCommand = new SqlCommand(sqlView, connexion))
                {
                    using (SqlDataReader reader = readDetailsCommand.ExecuteReader())// ANOMALY RULE : {749, Exposed Dangerous Method or Function}
                    {
                        reader.Read();
                    }
                }

                string sqlColumn = "ALTER TABLE Customers DROP COLUMN " + columnName;
                using (SqlCommand readDetailsCommand = new SqlCommand(sqlColumn, connexion))
                {
                    using (SqlDataReader reader = readDetailsCommand.ExecuteReader())// ANOMALY RULE : {749, Exposed Dangerous Method or Function}
                    {
                        reader.Read();
                    }
                }

                string sqlDelete = "DELETE FROM Customers WHERE CustomerName='Alfreds Futterkiste';";
                using (SqlCommand readDetailsCommand = new SqlCommand(sqlDelete, connexion))
                {
                    using (SqlDataReader reader = readDetailsCommand.ExecuteReader())// ANOMALY RULE : {749, Exposed Dangerous Method or Function}
                    {
                        reader.Read();
                    }
                }

                using (SqlDataReader reader = (new SqlCommand("DROP DATABASE " + databaseName, connexion)).ExecuteReader())// ANOMALY RULE : {749, Exposed Dangerous Method or Function}
                {
                    reader.Read();
                }

            }
        }

    }
}
