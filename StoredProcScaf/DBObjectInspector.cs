using System.Security.Cryptography.X509Certificates;
using System.Windows;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace StoredProcScaf
{
    public class DBObjectInspector
    {
        readonly TargetInfo _targetInfo;

        public DBObjectInspector(TargetInfo targetInfo)
        {
            _targetInfo = targetInfo;

            //MSSQL::/DANIEL_ASUS/Northwind/True/SqlProcedure/dbo.CustOrderHist.sql
            //dbo.CustOrderHist.sql
            //MSSQL::/DANIEL_ASUS/Northwind/True/SqlProcedure/
        }


        private static string GetConnectionString(PathTokens pathTokens)
        {
            const string defaultConnectionString = @"Data Source={0};Integrated Security={1};Connect Timeout=15;Encrypt=False;TrustServerCertificate=False";
            return string.Format(defaultConnectionString, pathTokens.Server.Replace('_','-'), pathTokens.Flag);
        }

        private DataSet FetchInfo(PathTokens pathTokens)
        {

            var connectionString = GetConnectionString(pathTokens);
            var cmdText = string.Format("{0}.dbo.sp_help", pathTokens.Database);
            var spName = string.Format("{0}.{1}", pathTokens.Database,_targetInfo.FileName.Remove(_targetInfo.FileName.Length - 4/*.sql*/).Replace("_"," "));

            var dataSet = new DataSet();

            var conn = new SqlConnection(connectionString);
            var cmd = new SqlCommand(cmdText, conn);
            cmd.Parameters.Add("@objname", SqlDbType.NVarChar, 776).Value = spName;
            cmd.CommandType = CommandType.StoredProcedure;
            var adapter = new SqlDataAdapter(cmd);
            adapter.Fill(dataSet);
            conn.Close();

            return dataSet;

        }


        private IEnumerable<ParameterInfo> GetDetails(DataSet dataSet)
        {
            foreach (DataRow row in dataSet.Tables[1].Rows)
            {
                // this is only true for the object type of stored procedure.
                var name = row.Field<string>("Parameter_name");
                var type = row.Field<string>("Type");
                var length = row.Field<short>("Length");
                var precision = row.Field<int>("Prec");
                var order = row.Field<int>("Param_order");
                yield return new ParameterInfo(name, type, length, precision, order);
            }
        }

        private ObjectInfo GetObjectInfo(DataSet dataSet)
        {
            // object info
            DataRow row = dataSet.Tables[0].Rows[0];

            var name = row.Field<string>("Name");
            var owner = row.Field<string>("Owner");
            var type = row.Field<string>("Type");
            var created = row.Field<DateTime>("Created_datetime");

            return new ObjectInfo(name, owner, type, created);
        }

        public bool CanScaffoldObject()
        {
            if (!_targetInfo.FullPath.StartsWith("MSSQL::")) return false;
            var pathTokens = GetPathTokens(_targetInfo);

            return  pathTokens.Folder.Equals("SqlProcedure");
        }

        private static PathTokens GetPathTokens(TargetInfo targetInfo)
        {

            //SqlConnection conn = new SqlConnection(@"Data Source=DANIEL-ASUS;Integrated Security=True;Connect Timeout=15;Encrypt=False;TrustServerCertificate=False");
            //MSSQL::/DANIEL_ASUS/Northwind/True/SqlProcedure/dbo.CustOrderHist.sql
            //dbo.CustOrderHist.sql
            //MSSQL::/DANIEL_ASUS/Northwind/True/SqlProcedure/

            var tokens = targetInfo.FullPath.Split(new[] { "/" }, StringSplitOptions.RemoveEmptyEntries);

            return new PathTokens
            {
                Header = tokens[0],
                Server = tokens[1],
                Database = tokens[2],
                Flag = tokens[3],
                Folder = tokens[4]
            };
        }



        public void Generate()
        {
            var pathTokens = GetPathTokens(_targetInfo);
            var ds = FetchInfo(pathTokens);
            var oi = GetObjectInfo(ds);
            var dtls = GetDetails(ds);

            //todo check if any parameter is of type user datatable,
            // if yes show the ui with parameters and let the user choose C# type,
            // if selection is not valid just show object.
            
            var ctx = TemplateTransformationContext.Create(oi, dtls);
            var txt = new ScaffTemplate(ctx).TransformText();
            Clipboard.SetText(txt);
           

        }
    }

   

    public class ParameterInfo
    {
        private string _name;

        public string Name
        {
            get { return _name; }
            set { _name = value.TrimStart('@'); }
        }

        public string Type { get; set; }
        public short Length { get; set; }
        public int Precision { get; set; }
        public int Order { get; set; }
        public ParameterInfo(string name, string type, short length, int precision, int order)
        {
            Name = name;
            Type = type;
            Length = length;
            Precision = precision;
            Order = order;
        }
    }

    public class PathTokens
    {
        public string Header { get; set; }
        public string Server { get; set; }
        public string Database { get; set; }
        public string Flag { get; set; }
        public string Folder { get; set; }
    }

    public class ObjectInfo
    {
        public string Name { get; set; }
        public string Owner { get; set; }
        private string Type { get; set; }
        public DateTime Created { get; set; }

        public ObjectInfo(string name, string owner, string type, DateTime created)
        {
            Name = name;
            Owner = owner;
            Type = type;
            Created = created;
        }


        public DbObjectType ObjectType
        {
            get
            {
                if (Type.Equals("Stored Procedure", StringComparison.OrdinalIgnoreCase))
                    return DbObjectType.StoredProcedure;
                if (Type.Equals("Table", StringComparison.OrdinalIgnoreCase))
                    return DbObjectType.Table;
                return DbObjectType.Unknown;
            }
        }

    }

    public enum DbObjectType
    {
        Unknown,
        StoredProcedure,
        Table
    }
    

    public static class MappingReader
    {
        static readonly string Sql2ClrMapFile = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "SQL2CLRTypeMap.json");
        static readonly string Sql2DbTypeMapFile = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "SQL2DbTypeMap.json");
        
        static Dictionary<string, string> _sql2ClrMap;
        static Dictionary<string, string> _sql2DbTypeMap;

        public static Dictionary<string, string> Sql2ClrMap
        {
            get {
                return _sql2ClrMap ??
                       (_sql2ClrMap =
                           JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(Sql2ClrMapFile)));
            }
        }
        public static Dictionary<string, string> Sql2DbTypeMap
        {
            get {
                return _sql2DbTypeMap ??
                       (_sql2DbTypeMap =
                           JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(Sql2DbTypeMapFile)));
            }
        }
    }
}
