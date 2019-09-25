using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using crud.web.Data;
using System.Web.Mvc;
using System.Data;
using Newtonsoft.Json;

namespace crud.web.Controllers
{
    public class DataController : ApiController
    {
        public List<string> GetDatabases(string connectionString)
        {
            return SqlServerQuery.Connect(connectionString);
        }

        public List<string> GetTables(string connectionString)
        {
            return SqlServerQuery.GetTables(connectionString);
        }

        public JsonResult GetColumns(string connectionString, string tableName)
        {
            return new JsonResult { Data = JsonConvert.SerializeObject(SqlServerQuery.GetColumns(connectionString, tableName)) };
        }

        public JsonResult GetData(string connectionString, string tableName)
        {
            return new JsonResult { Data = JsonConvert.SerializeObject(SqlServerQuery.GetData(connectionString, tableName)) };
        }

        public HttpResponseMessage InsertData([FromUri] string connectionString, [FromUri] string tableName, [FromBody] List<crud.web.Data.Data> row)
        {
            return Request.CreateResponse(HttpStatusCode.OK, SqlServerQuery.InsertData(connectionString, tableName, row));
        }

        public HttpResponseMessage UpdateData([FromUri] string connectionString, [FromUri] string tableName, [FromBody] List<crud.web.Data.Data> row)
        {
            return Request.CreateResponse(HttpStatusCode.OK, SqlServerQuery.UpdateData(connectionString, tableName, row));
        }

        public HttpResponseMessage RemoveData([FromUri] string connectionString, [FromUri] string tableName, [FromBody] List<crud.web.Data.Data> row)
        {
            return Request.CreateResponse(HttpStatusCode.OK, SqlServerQuery.DeleteData(connectionString, tableName, row));
        }

    }
}
