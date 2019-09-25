using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Web;
using System.Data;
using System.Data.SqlClient;
using System.Data.Common;
using System.Web.Configuration;
using System.Web.Mvc;
using Microsoft.Ajax.Utilities;
using Newtonsoft.Json;

namespace crud.web.Data
{

    public class Data
    {
        public string Key { get; set; }
        public string Value { get; set; }
    }

    public class SqlServerQuery
    {
        private static string SequenceFields = System.IO.File.ReadAllText(System.Web.HttpContext.Current.Server.MapPath(@"~/config/config.json"));
        private static List<string> SequenceColumns = new List<string>();

        public static string GetSequenceFromColumn(string column)
        {
            dynamic json = JsonConvert.DeserializeObject(SequenceFields);
            for (int i = 0; i < json.sequenceColumns.Count; i++)
            {
                if (column.Equals(json.sequenceColumns[i].columnName.Value))
                    return json.sequenceColumns[i].sequenceColumn.Value;
            }
            return "";
        }

        public static List<string> Connect(string connectionString)
        {
            dynamic json = JsonConvert.DeserializeObject(SequenceFields);
            for (int i = 0; i < json.sequenceColumns.Count; i++)
            {
                SequenceColumns.Add(json.sequenceColumns[i].columnName.Value);
            }
            var databases = new List<string>();
            var conn = new SqlConnection(connectionString);
            try
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandType = CommandType.Text;
                    cmd.CommandText = "SELECT name FROM master.sys.databases WHERE name NOT IN ('master','tempdb','model','msdb','distribution') AND HAS_DBACCESS(name) = 1";
                    using (var dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            databases.Add(dr.GetString(0));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                if (conn != null)
                    conn.Close();
            }

            return databases;
        }

        public static List<string> GetTables(string connectionString)
        {
            var tables = new List<string>();

            var conn = new SqlConnection(connectionString);
            try
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandType = CommandType.Text;
                    cmd.CommandText = @"SELECT TABLE_CATALOG + '.' + TABLE_SCHEMA + '.' + TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE' 
                                        AND TABLE_SCHEMA + '.' + TABLE_NAME IN (SELECT DISTINCT SCHEMA_NAME(schema_id) + '.' + name AS TableName   
                                        FROM sys.tables
                                        WHERE OBJECTPROPERTY(OBJECT_ID,'TableHasPrimaryKey') = 1 ) order by 1";
                    using (var dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            tables.Add(dr.GetString(0));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                if (conn != null)
                    conn.Close();
            }

            return tables;
        }

        public static DataTable GetColumns(string connectionString, string tableName)
        {
            var columns = new DataTable();
            var refColumns = new DataTable();
            var databaseName = tableName.Split('.')[0];
            var schemaName = tableName.Split('.')[1];
            var tablenm = tableName.Split('.')[2];
            var referenceData = new Dictionary<string, IEnumerable<object>>();
            var conn = new SqlConnection(connectionString);
            try
            {

                conn.Open();
                // Fetch all columns for the selected table
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"SELECT DISTINCT *, CASE COALESCE( B.Column_Name,'') WHEN '' THEN 0 ELSE 1 END IS_PRIMARY,
					                            COLUMNPROPERTY(object_id(A.TABLE_CATALOG + '.' + A.TABLE_SCHEMA + '.' + A.TABLE_NAME), A.COLUMN_NAME, 'IsIdentity') IS_IDENTITY
                                        FROM INFORMATION_SCHEMA.COLUMNS A 
                                        LEFT JOIN (SELECT Col.Column_Name from 
                                            INFORMATION_SCHEMA.TABLE_CONSTRAINTS Tab, 
                                            INFORMATION_SCHEMA.CONSTRAINT_COLUMN_USAGE Col 
                                        WHERE 
                                            Col.Constraint_Name = Tab.Constraint_Name
                                            AND Col.Table_Name = Tab.Table_Name
                                            AND Constraint_Type = 'PRIMARY KEY'
	                                        AND col.CONSTRAINT_CATALOG = @databaseName
	                                        AND col.CONSTRAINT_SCHEMA = @schemaName
                                            AND Col.Table_Name = @tablenm) B
                                        ON A.Column_Name = B.Column_Name
                                        WHERE   A.TABLE_CATALOG = @databaseName
	                                        AND A.TABLE_SCHEMA = @schemaName
                                            AND A.TABLE_NAME=@tablenm
                                        ORDER BY ORDINAL_POSITION ";
                        //"SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_CATALOG=@databaseName AND TABLE_SCHEMA=@schemaName AND TABLE_NAME=@tablenm ";
                    cmd.CommandType = CommandType.Text;
                    cmd.Parameters.Add(new SqlParameter("databaseName", databaseName));
                    cmd.Parameters.Add(new SqlParameter("schemaName", schemaName));
                    cmd.Parameters.Add(new SqlParameter("tablenm", tablenm));
                    using (var reader = cmd.ExecuteReader())
                    {
                        columns.Load(reader);
                    }
                }
                var dcRef = new DataColumn("IS_REF") {DataType = typeof (bool), ReadOnly = false};
                var dcSequence = new DataColumn("IS_SEQUENCE") { DataType = typeof(bool), ReadOnly = false };
                var dcPossibleValues = new DataColumn("POSSIBLE_VALUES") {DataType = typeof (String), ReadOnly = false};
                columns.Columns.Add(dcRef);
                columns.Columns.Add(dcSequence);
                columns.Columns.Add(dcPossibleValues);

                foreach (var sequenceColumn in SequenceColumns)
                {
                    var dbName = sequenceColumn.Split('.')[0];
                    var scName = sequenceColumn.Split('.')[1];
                    var tbName = sequenceColumn.Split('.')[2];
                    var clName = sequenceColumn.Split('.')[3];
                    var row = columns.Select(String.Format("TABLE_CATALOG = '{0}' AND TABLE_SCHEMA = '{1}' AND TABLE_NAME = '{2}' AND COLUMN_NAME = '{3}'", dbName, scName, tbName, clName));
                    if (row != null && row.Length == 1)
                    {
                        row[0]["IS_SEQUENCE"] = true; 
                    }
                }

                // Fetch all reference columns for the selected table
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"SELECT
                                            FK_Table = FK.TABLE_NAME,
                                            FK_Column = CU.COLUMN_NAME,
                                            PK_Table = PK.TABLE_NAME,
                                            PK_Column = PT.COLUMN_NAME,
                                            Constraint_Name = C.CONSTRAINT_NAME,
                                            PK_Database = PK.TABLE_CATALOG,
                                            PK_Schema = PK.TABLE_SCHEMA
                                        FROM
                                            INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS C
                                        INNER JOIN INFORMATION_SCHEMA.TABLE_CONSTRAINTS FK
                                            ON C.CONSTRAINT_NAME = FK.CONSTRAINT_NAME
                                        INNER JOIN INFORMATION_SCHEMA.TABLE_CONSTRAINTS PK
                                            ON C.UNIQUE_CONSTRAINT_NAME = PK.CONSTRAINT_NAME
                                        INNER JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE CU
                                            ON C.CONSTRAINT_NAME = CU.CONSTRAINT_NAME
                                        INNER JOIN (
                                                    SELECT
                                                        i1.TABLE_NAME,
                                                        i2.COLUMN_NAME
                                                    FROM
                                                        INFORMATION_SCHEMA.TABLE_CONSTRAINTS i1
                                                    INNER JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE i2
                                                        ON i1.CONSTRAINT_NAME = i2.CONSTRAINT_NAME
                                                    WHERE
                                                        i1.CONSTRAINT_TYPE = 'PRIMARY KEY'
                                                   ) PT
                                            ON PT.TABLE_NAME = PK.TABLE_NAME
                                        WHERE FK.TABLE_CATALOG=@databaseName AND FK.TABLE_SCHEMA=@schemaName AND FK.TABLE_NAME=@tablenm ";
                    cmd.CommandType = CommandType.Text;
                    cmd.Parameters.Add(new SqlParameter("databaseName", databaseName));
                    cmd.Parameters.Add(new SqlParameter("schemaName", schemaName));
                    cmd.Parameters.Add(new SqlParameter("tablenm", tablenm));
                    using (var reader = cmd.ExecuteReader())
                    {
                        refColumns.Load(reader);
                    }
                }
                // Load all reference column data
                if (refColumns != null)
                {
                    string descriptionColumnName = null;
                    string descriptionColumnSql = null;
                    foreach (DataRow dr in refColumns.Rows)
                    {
                        using (var cmd = conn.CreateCommand())
                        {
                            cmd.CommandType = CommandType.Text;
                            cmd.CommandText = string.Format(@"SELECT TOP 1 CASE TABLE_NAME WHEN 'market_ref' THEN 'display_name' 
							                                                                WHEN 'cable_channel_ref' THEN 'cable_channel_name'
							                                                                WHEN 'city_ref' THEN 'city_name'
							                                                                WHEN 'contact_type_ref' THEN 'contact_type_value'
							                                                                WHEN 'country_ref' THEN 'country_name'
							                                                                WHEN 'county_ref' THEN 'county_name'
							                                                                WHEN 'franchise_ref' THEN 'franchise_name'
							                                                                WHEN 'internet_speed_offering_ref' THEN '''Up: '' + up_speed + '', Down: '' + down_speed'
							                                                                WHEN 'location_ref' THEN '''Lat: '' + latitude + '', Lon: '' + longitude'
							                                                                WHEN 'macaddress_ref' THEN 'bit_size'
							                                                                WHEN 'state_ref' THEN 'state_name'
				                                                            ELSE COLUMN_NAME END COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_CATALOG='{0}' AND TABLE_SCHEMA='{1}' AND TABLE_NAME='{2}'
	                                                                        AND (COLUMN_NAME LIKE '%description%' OR COLUMN_NAME = 'display_name'  or
		                                                                            COLUMN_NAME = 'cable_channel_name' or COLUMN_NAME = 'city_name' or
		                                                                            COLUMN_NAME = 'contact_type_value' or COLUMN_NAME = 'country_name' or
		                                                                            COLUMN_NAME = 'county_name' or COLUMN_NAME = 'franchise_name' or
		                                                                            COLUMN_NAME = 'up_speed' or COLUMN_NAME = 'down_speed' or
		                                                                            COLUMN_NAME = 'latitude' or COLUMN_NAME = 'longitude' or
		                                                                            COLUMN_NAME = 'bit_size' or COLUMN_NAME = 'state_name')", dr["PK_Database"], dr["PK_Schema"], dr["PK_Table"]); 
                            using (var dr1 = cmd.ExecuteReader())
                            {
                                while (dr1.Read())
                                {
                                    descriptionColumnName = dr1.GetString(0);
                                }
                            }
                        }
                        if (string.IsNullOrWhiteSpace(descriptionColumnName))
                            descriptionColumnSql = "";
                        else
                        {
                            descriptionColumnSql = "+ ' ==> ' +" + descriptionColumnName;
                        }
                        using (var cmd = conn.CreateCommand())
                        {
                            cmd.CommandText = String.Format(@"SELECT DISTINCT cast({0} as varchar(100) ) {4} FROM {1}.{2}.{3} ", dr["PK_Column"], dr["PK_Database"], dr["PK_Schema"], dr["PK_Table"], descriptionColumnSql);
                            //cmd.CommandText = String.Format(@"SELECT DISTINCT {0} FROM {1}.{2}.{3} ", dr["PK_Column"], dr["PK_Database"], dr["PK_Schema"], dr["PK_Table"]);
                            cmd.CommandType = CommandType.Text;
                            using (var reader = cmd.ExecuteReader())
                            {
                                if (reader.HasRows)
                                {
                                    var refRecords = new List<string>();
                                    while (reader.Read())
                                    {
                                        refRecords.Add(reader[0].ToString());
                                    }
                                    referenceData.Add(dr["FK_Column"].ToString(), refRecords);
                                    var row = columns.Select(String.Format("COLUMN_NAME = '{0}'", dr["FK_COLUMN"].ToString()));
                                    if (row != null && row.Length == 1)
                                    {
                                        row[0]["IS_REF"] = true;
                                        row[0]["POSSIBLE_VALUES"] = String.Join(",", refRecords);
                                    }
                                }
                            }
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                if (conn != null)
                    conn.Close();
            }

            return columns;
        }

        public static DataTable GetData(string connectionString, string tableName)
        {
            var rows = new DataTable();
            var conn = new SqlConnection(connectionString);
            try
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = string.Format("SELECT * FROM {0}", tableName);
                    cmd.CommandType = CommandType.Text;
                    using (var reader = cmd.ExecuteReader())
                    {
                        rows.Load(reader);
                    }
                }

            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                if (conn != null)
                    conn.Close();
            }

            return rows;
        }

        public static int InsertData(string connectionString, string tableName, List<Data> row)
        {
            int returnValue;
            var databaseName = tableName.Split('.')[0];
            var schemaName = tableName.Split('.')[1];
            var tablenm = tableName.Split('.')[2];
            var conn = new SqlConnection(connectionString);
            var parameters = new List<SqlParameter>();
            var columns = new List<string>();
            var values = new List<string>();
            foreach (var column in row)
            {
                int isIdentityColumn = 0;
                try
                {
                    conn.Open();
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = string.Format("SELECT COLUMNPROPERTY(object_id('{0}' + '.' + '{1}' + '.' + '{2}'), '{3}', 'IsIdentity') ", databaseName, schemaName, tablenm, column.Key);
                        cmd.CommandType = CommandType.Text;
                        var reader = cmd.ExecuteScalar();
                        if (reader != null)
                            isIdentityColumn = Int32.Parse(reader.ToString());
                    }
                    if (isIdentityColumn == 1)
                    {
                        continue;
                    }
                }
                catch (Exception ex)
                {
                    throw ex;
                }
                finally
                {
                    if (conn != null)
                        conn.Close();
                }

                columns.Add(column.Key);
                values.Add("@" + column.Key);
                if (!string.IsNullOrWhiteSpace(column.Value) && column.Value.Contains(" ==> "))
                    parameters.Add(new SqlParameter(column.Key,
                        column.Value.Substring(0,column.Value.IndexOf(" ==> "))));
                else
                {
                    int sequenceNextval = 0;
                    if (SequenceColumns.Contains(string.Concat(tableName, ".", column.Key)))
                    {
                        try
                        {
                            conn.Open();
                            using (var cmd = conn.CreateCommand())
                            {
                                cmd.CommandText = string.Format("SELECT NEXT VALUE FOR {0}", GetSequenceFromColumn(string.Concat(tableName, ".", column.Key)));
                                cmd.CommandType = CommandType.Text;
                                var reader = cmd.ExecuteScalar();
                                if (reader != null)
                                    sequenceNextval = Int32.Parse(reader.ToString());
                            }

                        }
                        catch (Exception ex)
                        {
                            throw ex;
                        }
                        finally
                        {
                            if (conn != null)
                                conn.Close();
                        }
                    }
                    //parameters.Add(column.Value.IsNullOrWhiteSpace()
                    //    ? new SqlParameter(column.Key, DBNull.Value)
                    //    : sequenceNextval == 0 ? new SqlParameter(column.Key, column.Value) : new SqlParameter(column.Key, sequenceNextval));
                    parameters.Add(sequenceNextval != 0
                        ? new SqlParameter(column.Key, sequenceNextval)
                        : column.Value.IsNullOrWhiteSpace()
                            ? new SqlParameter(column.Key, DBNull.Value)
                            : new SqlParameter(column.Key, column.Value));
                }

            }
            var sql = string.Format("INSERT INTO {0} ({1}) VALUES ({2})", tableName, string.Join(",", columns), string.Join(",", values));
            
            try
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = sql;
                    cmd.CommandType = CommandType.Text;
                    foreach (var parameter in parameters)
                    {
                        cmd.Parameters.Add(parameter);
                    }
                    returnValue = cmd.ExecuteNonQuery();
                }

            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                if (conn != null)
                    conn.Close();
            }

            return returnValue;
        }

        public static int UpdateData(string connectionString, string tableName, List<Data> row)
        {
            int returnValue;
            var parameters = new List<SqlParameter>();
            var columns = new List<string>();
            var values = new List<string>(); 
            var schemaName = tableName.Split('.')[1];
            var tablenm = tableName.Split('.')[2];

            var primaryKeyColumnList = new List<string>();
            var reminderColumnList = new List<string>();

            var conn = new SqlConnection(connectionString);

            if (row == null)
            {
                throw new Exception("Please select a row before deleting");
            }
            foreach (var column in row)
            {
                columns.Add(column.Key);
                values.Add("@" + column.Key);
                //parameters.Add(new SqlParameter(column.Key, column.Value));
                if (!string.IsNullOrWhiteSpace(column.Value) && column.Value.Contains(" ==> "))
                    parameters.Add(new SqlParameter(column.Key,
                        column.Value.Substring(0, column.Value.IndexOf(" ==> "))));
                else
                {
                    parameters.Add(column.Value.IsNullOrWhiteSpace()
                        ? new SqlParameter(column.Key, DBNull.Value)
                        : new SqlParameter(column.Key, column.Value));
                }
            }

            try
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandType = CommandType.Text;
                    cmd.CommandText = "SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE WHERE OBJECTPROPERTY(OBJECT_ID(CONSTRAINT_SCHEMA + '.' + CONSTRAINT_NAME), 'IsPrimaryKey') = 1 AND TABLE_NAME = @tablenm AND TABLE_SCHEMA = @schemaName";

                    cmd.Parameters.Add(new SqlParameter("schemaName", schemaName));
                    cmd.Parameters.Add(new SqlParameter("tablenm", tablenm));
                    using (var dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            primaryKeyColumnList.Add(dr.GetString(0));
                        }
                    }
                }

                reminderColumnList.AddRange(from sqlParameter in parameters where !primaryKeyColumnList.Contains(sqlParameter.ParameterName) select sqlParameter.ParameterName);

                var filter = new StringBuilder(" WHERE ");
                foreach (var primaryKeyColumn in primaryKeyColumnList)
                {
                    string value = null;
                    foreach (
                        var sqlParameter in
                            parameters.Where(sqlParameter => sqlParameter.ParameterName.Equals(primaryKeyColumn)))
                        value = sqlParameter.Value.ToString();
                    filter.Append(string.Concat(primaryKeyColumn, " = '", value, "' AND "));
                }

                var assignment = new StringBuilder(" SET ");
                foreach (var reminderColumn in reminderColumnList)
                {
                    string value = null;
                    foreach (
                        var sqlParameter in
                            parameters.Where(sqlParameter => sqlParameter.ParameterName.Equals(reminderColumn)))
                        value = sqlParameter.Value.ToString();
                    assignment.Append(string.Concat(reminderColumn, " = '", value, "' , "));
                }

                var sql = string.Format(string.Concat("UPDATE {0} ", assignment.ToString().Substring(0,assignment.Length -2), filter.ToString().Substring(0, filter.Length - 4), "; "), tableName);

                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = sql;
                    cmd.CommandType = CommandType.Text;
                    returnValue = cmd.ExecuteNonQuery();
                }

            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                conn.Close();
            }

            return returnValue;
        }
        public static int DeleteData(string connectionString, string tableName, List<Data> row)
        {
            int returnValue;
            var parameters = new List<SqlParameter>();
            var columns = new List<string>();
            var values = new List<string>(); 
            var schemaName = tableName.Split('.')[1];
            var tablenm = tableName.Split('.')[2];

            var primaryKeyColumnList = new List<string>(); 

            var conn = new SqlConnection(connectionString);

            if (row == null)
            {
                throw new Exception("Please select a row before deleting");
            }
            foreach (var column in row)
            {
                columns.Add(column.Key);
                values.Add("@" + column.Key);
                parameters.Add(new SqlParameter(column.Key, column.Value));
            } 
            
            try
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandType = CommandType.Text;
                    cmd.CommandText = "SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE WHERE OBJECTPROPERTY(OBJECT_ID(CONSTRAINT_SCHEMA + '.' + CONSTRAINT_NAME), 'IsPrimaryKey') = 1 AND TABLE_NAME = @tablenm AND TABLE_SCHEMA = @schemaName";

                    cmd.Parameters.Add(new SqlParameter("schemaName", schemaName));
                    cmd.Parameters.Add(new SqlParameter("tablenm", tablenm));
                    using (var dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            primaryKeyColumnList.Add(dr.GetString(0));
                        }
                    }
                }
                var filter = new StringBuilder(" WHERE ");
                foreach (var primaryKeyColumn in primaryKeyColumnList)
                {
                    string value = null;
                    foreach (
                        var sqlParameter in
                            parameters.Where(sqlParameter => sqlParameter.ParameterName.Equals(primaryKeyColumn)))
                        value = sqlParameter.Value.ToString();
                    filter.Append(string.Concat(primaryKeyColumn , " = '" , value , "' AND "));
                }

                var sql = string.Format(string.Concat("DELETE FROM {0} ", filter.ToString().Substring(0, filter.Length - 4), "; "), tableName);

                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = sql;
                    cmd.CommandType = CommandType.Text;
                    returnValue = cmd.ExecuteNonQuery();
                }

            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                conn.Close();
            }

            return returnValue;
        }
    }
}