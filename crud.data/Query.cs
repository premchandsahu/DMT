using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace crud.data
{

    public class Data
    {
        public string Key { get; set; }
        public string Value { get; set; }
    }

    public class Query
    {
        public static List<string> GetTables()
        {
            List<string> tables;

            using (var db = new sampleEntities())
            {
                tables = db.Database.SqlQuery<string>("SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES").ToList();
            }

            return tables;
        }

        public static DataTable GetColumns(string tableName)
        {
            DataTable columns;

            using (var db = new sampleEntities())
            {
                columns = db.Database.SqlQuery<DataTable>("SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME=@tableName",
                    new SqlParameter("@tableName",tableName)).FirstOrDefault();
            }

            return columns;
        }

        public static DataTable GetData(string tableName)
        {
            DataTable rows;
            using (var db = new sampleEntities())
            {
                rows = db.Database.SqlQuery<DataTable>(string.Format("SELECT * FROM {0}", tableName)).FirstOrDefault();
            }

            return rows;
        }

        public static int InsertData(string tableName, List<Data> row)
        {
            int returnValue;

            using (var db = new sampleEntities())
            {                
                var parameters = new List<SqlParameter>();
                var columns = new List<string>();
                var values = new List<string>();
                foreach (var column in row)
                {
                    columns.Add(column.Key);
                    values.Add("@" + column.Key);
                    parameters.Add(new SqlParameter("@" + column.Key, column.Value));
                }
                var sql = string.Format("INSERT INTO {0} ({1}) VALUES ({2})", tableName, string.Join(",", columns), string.Join(",", values));
                returnValue = db.Database.ExecuteSqlCommand(sql, parameters);
            }

            return returnValue;
        }
    }
}
