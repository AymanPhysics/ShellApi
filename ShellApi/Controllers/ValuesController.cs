using FirebaseAdmin;
using FirebaseAdmin.Auth;
using FirebaseAdmin.Messaging;
using FireSharp.Interfaces;
using Google.Apis.Auth.OAuth2;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;
using System.Web.Http;

namespace ShellApi.Controllers
{
    public class ValuesController : ApiController
    { 

        string db_name;
        ValuesController()
        {
            db_name = GetDatabaseName();
        }

        public DataTable GetGroups()
        {
            return ExecuteAdapter("select Id,Name from " + db_name + ".dbo.Groups where IsOnline=1");
        }

        public DataTable GetTypes(String id)
        {
            return ExecuteAdapter("select Id,Name from " + db_name + ".dbo.Types where IsOnline=1 and GroupId=" + id);
        }

        public DataTable GetItems(String GroupId, String TypeId)
        {
            return ExecuteAdapter("select Id,Name,OnlinePrice from " + db_name + ".dbo.Items where Stopped=0 and IsOnline=1 and GroupId=" + GroupId + " and TypeId=" + TypeId);
        }


        /// <summary>
        /// //////////////////////////////////////////////////////Favorites
        /// </summary>
        public DataTable GetCustomersItemsFavorites(String CustomerId)
        {
            return ExecuteAdapter("select Id,Name,OnlinePrice from " + db_name + ".dbo.Items where IsOnline=1 and Id in(select ItemId from " + db_name + ".dbo.CustomersItemsFavorites where CustomerId=" + CustomerId + ")");
        }

        public int GetCustomersItemsFavorites(String CustomerId, String ItemId)
        {
            return val(ExecuteScalar("if exists(select ItemId from " + db_name + ".dbo.CustomersItemsFavorites where CustomerId=" + CustomerId + " and ItemId=" + ItemId + ") select 1 else select 0"));
        }

        [HttpGet]
        public int SetCustomersItemsFavorites(String CustomerId, String ItemId)
        {
            ExecuteScalar("if not exists(select ItemId from " + db_name + ".dbo.CustomersItemsFavorites where CustomerId=" + CustomerId + " and ItemId=" + ItemId + ") insert " + db_name + ".dbo.CustomersItemsFavorites(CustomerId,ItemId) select " + CustomerId + "," + ItemId );
            return GetCustomersItemsFavorites(CustomerId, ItemId);
        }

        [HttpGet]
        public int RemoveCustomersItemsFavorites(String CustomerId, String ItemId)
        {
            ExecuteNonQuery("delete " + db_name + ".dbo.CustomersItemsFavorites where CustomerId = " + CustomerId + " and ItemId = " + ItemId );
            return GetCustomersItemsFavorites(CustomerId, ItemId);
        }

        /// <summary>
        /// //////////////////////////////////////////////////////Cart
        /// </summary>
        public DataTable GetCustomersItemsCart(String CustomerId)
        {
            //return ExecuteAdapter("select Id,Name,OnlinePrice from " + db_name + ".dbo.Items where IsOnline=1 and Id in(select ItemId from " + db_name + ".dbo.CustomersItemsCart where CustomerId=" + CustomerId + ")");
            return ExecuteAdapter("select Id,Name,OnlinePrice,C.Qty from " + db_name + ".dbo.Items It left join " + db_name + ".dbo.CustomersItemsCart C on(It.Id=C.ItemId) where It.IsOnline=1 and C.CustomerId=" + CustomerId + "");
        }

        public int GetCustomersItemsCart(String CustomerId, String ItemId)
        {
            return val(ExecuteScalar("select Qty from " + db_name + ".dbo.CustomersItemsCart where CustomerId=" + CustomerId + " and ItemId=" + ItemId));
        }

        [HttpGet]
        public int SetCustomersItemsCart(String CustomerId, String ItemId,int Qty)
        {
            RemoveCustomersItemsCart(CustomerId, ItemId);
            if(Qty>0)
                ExecuteNonQuery("insert " + db_name + ".dbo.CustomersItemsCart(CustomerId,ItemId,Qty) select " + CustomerId + "," + ItemId + "," + Qty);
            return GetCustomersItemsCart(CustomerId, ItemId);
        }

        [HttpGet]
        public int RemoveCustomersItemsCart(String CustomerId, String ItemId)
        {
            ExecuteNonQuery("delete " + db_name + ".dbo.CustomersItemsCart where CustomerId = " + CustomerId + " and ItemId = " + ItemId);
            return GetCustomersItemsCart(CustomerId, ItemId);
        }

        [HttpGet]
        public async Task<int> GenerateCustomersItemsCartInvoicesAsync(String CustomerId)
        {
            int InvoiceNo= val(ExecuteScalar("exec " + db_name + ".dbo.GenerateCustomersItemsCartInvoices " + CustomerId));
            
            /*
            var defaultApp = FirebaseApp.Create(new AppOptions()
            {
                Credential = GoogleCredential.GetApplicationDefault(),
                ProjectId = "elhariry-22e4b",
            });
            Console.WriteLine(defaultApp.Name);
            var defaultAuth = FirebaseAuth.GetAuth(defaultApp)
            var message = new Message
            {
                // payload data, some additional information for device
                Data = new Dictionary<string, string>
                {
                    { "additional_data", "a string" },
                    { "another_data", "another string" },
                },

                // the notification itself
                Notification = new Notification
                {
                    Title = "New Invoice",
                    Body = "please, check invoice no " + InvoiceNo.ToString(),
                    //ImageUrl = "https://a_image_url"
                },
                Topic = "a valid token",
            };
            var firebaseMessagingInstance = FirebaseMessaging.GetMessaging(defaultApp);
            var result = await firebaseMessagingInstance.SendAsync(message).ConfigureAwait(false);
            */
            
            return InvoiceNo;
        }


        [HttpGet]
        public DataTable GetCustomersItemsCartInvoices(String CustomerId)
        {
            return ExecuteAdapter("exec " + db_name + ".dbo.GetCustomersItemsCartInvoices " + CustomerId);
        }

        [HttpGet]
        public DataTable GetCustomersItemsCartInvoicesOne(String CustomerId, String InvoiceNo)
        {
            return ExecuteAdapter("exec " + db_name + ".dbo.GetCustomersItemsCartInvoicesOne " + CustomerId + "," + InvoiceNo);
        }


        [HttpGet]
        public DataTable GetItemsSearch(String CustomerId, string Search)
        {
            if (Search == null)
                Search = "";
            ExecuteNonQuery("insert " + db_name + ".dbo.CustomerSearch(CustomerId,Search) select " + CustomerId + ",'" + Search + "'");
            return ExecuteAdapter("select Id,Name,OnlinePrice from " + db_name + ".dbo.Items where IsOnline=1 and Name like '%" + Search.Replace("'", "''") + "%'");
        }

        [HttpGet]
        public DataTable GetItemsSearchHistory(String CustomerId)
        {
            return ExecuteAdapter("select Search,MyLine from " + db_name + ".dbo.CustomerSearch where CustomerId="+ CustomerId+ " order by MyLine desc");
        }

            
        public DataTable GetItems()
        {
            return ExecuteAdapter("select Id,Name,OnlinePrice from " + db_name + ".dbo.Items where Stopped=0 and IsOnline=1");
        }

        public string GetDatabaseName()
        {
            return ExecuteScalar("select top 1 name from sys.databases Where [name] like 'Shell%' order by name desc");  
        }

        public int val(string str) {
            try{
                return int.Parse(str);
            }
            catch (Exception){
                return 0;
            }
        }

        SqlConnection con = new SqlConnection("Data Source=.;Initial Catalog=master;User ID=Physics;Password=Phy123!@#;Persist Security Info=True;");
        SqlDataAdapter da = new SqlDataAdapter();

        bool ExecuteNonQuery(String sqlstatment)
        {
            SqlConnection c = new SqlConnection(con.ConnectionString);
            try
            {
                SqlCommand MyCmd = c.CreateCommand();
                if (MyCmd.Connection.State == ConnectionState.Closed)
                {
                    MyCmd.Connection.Open();
                }

                MyCmd.CommandTimeout = 72000000;
                MyCmd.Parameters.Clear();
                MyCmd.CommandType = CommandType.Text;
                MyCmd.CommandText = sqlstatment;

                MyCmd.ExecuteNonQuery();
                MyCmd.Connection.Close();
            }
            catch (Exception ex)
            {
                return false;
            }

            return true;
        }

        bool ExecuteNonQuery(String StoredName, string[] ParaName,string[] ParaValue)
        {
            SqlConnection c = new SqlConnection(con.ConnectionString);
            try
            {
                SqlCommand MyCmd = c.CreateCommand();
                if (MyCmd.Connection.State == ConnectionState.Closed)
                {
                    MyCmd.Connection.Open();
                }

                MyCmd.CommandTimeout = 72000000;
                MyCmd.Parameters.Clear();
                MyCmd.CommandType = CommandType.StoredProcedure;
                MyCmd.CommandText = StoredName;

                for (int i = 0; i < ParaName.Length; i++)
                {
                    MyCmd.Parameters.Add("@" + ParaName[i], SqlDbType.VarChar);
                    MyCmd.Parameters["@" + ParaName[i]].Value = ParaValue[i];
                }

                MyCmd.ExecuteNonQuery();
                MyCmd.Connection.Close();
            }
            catch (Exception ex)
            {
                return false;
            }

            return true;
        }

        string ExecuteScalar(String sqlstatment)
        {
            SqlConnection c = new SqlConnection(con.ConnectionString);
            try
            {
                SqlCommand MyCmd = c.CreateCommand();
                if (MyCmd.Connection.State == ConnectionState.Closed)
                {
                    MyCmd.Connection.Open();
                }

                MyCmd.CommandTimeout = 72000000;
                MyCmd.Parameters.Clear();
                MyCmd.CommandType = CommandType.Text;
                MyCmd.CommandText = sqlstatment;

                string s = MyCmd.ExecuteScalar().ToString().Trim();
                MyCmd.Connection.Close();
                return s;
            }
            catch (Exception ex)
            {
                return "";
            }

        }

        string ExecuteScalar(String StoredName, string[] ParaName, string[] ParaValue)
        {
            SqlConnection c = new SqlConnection(con.ConnectionString);
            try
            {
                SqlCommand MyCmd = c.CreateCommand();
                if (MyCmd.Connection.State == ConnectionState.Closed)
                {
                    MyCmd.Connection.Open();
                }

                MyCmd.CommandTimeout = 72000000;
                MyCmd.Parameters.Clear();
                MyCmd.CommandType = CommandType.StoredProcedure;
                MyCmd.CommandText = StoredName;

                for (int i = 0; i < ParaName.Length; i++)
                {
                    MyCmd.Parameters.Add("@" + ParaName[i], SqlDbType.VarChar);
                    MyCmd.Parameters["@" + ParaName[i]].Value = ParaValue[i];
                }

                string s = MyCmd.ExecuteScalar().ToString().Trim();
                MyCmd.Connection.Close();
                return s;
            }
            catch (Exception ex)
            {
                return "";
            }

        }

        DataTable ExecuteAdapter(String sqlstatment)
        {
            SqlConnection c = new SqlConnection(con.ConnectionString);
            try
            {
                SqlCommand MyCmd = c.CreateCommand();
                if (MyCmd.Connection.State == ConnectionState.Closed)
                {
                    MyCmd.Connection.Open();
                }

                DataTable dt = new DataTable("Tbl");
                MyCmd.CommandTimeout = 72000000;
                MyCmd.Parameters.Clear();
                MyCmd.CommandType = CommandType.Text;
                MyCmd.CommandText = sqlstatment;
                da.SelectCommand = MyCmd;
                da.Fill(dt);
                MyCmd.Connection.Close();
                return dt;
            }
            catch (Exception ex)
            {
                return new DataTable();
            }

        }

        DataTable ExecuteAdapter(String StoredName, string[] ParaName, string[] ParaValue)
        {
            SqlConnection c = new SqlConnection(con.ConnectionString);
            try
            {
                SqlCommand MyCmd = c.CreateCommand();
                if (MyCmd.Connection.State == ConnectionState.Closed)
                {
                    MyCmd.Connection.Open();
                }

                DataTable dt = new DataTable("Tbl");
                MyCmd.CommandTimeout = 72000000;
                MyCmd.Parameters.Clear();
                MyCmd.CommandType = CommandType.StoredProcedure;
                MyCmd.CommandText = StoredName;

                for (int i = 0; i < ParaName.Length; i++)
                {
                    MyCmd.Parameters.Add("@" + ParaName[i], SqlDbType.VarChar);
                    MyCmd.Parameters["@" + ParaName[i]].Value = ParaValue[i];
                }

                da.SelectCommand = MyCmd;
                da.Fill(dt);
                MyCmd.Connection.Close();
                return dt;
            }
            catch (Exception ex)
            {
                return new DataTable();
            }

        }

    }
}
