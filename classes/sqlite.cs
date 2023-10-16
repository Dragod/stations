using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using System.Data.SQLite;
using System.Windows;

namespace pfcode_stations.classes
{
    
    public static class DbConnector
    {
        public static SQLiteConnection Connect()
        {
            string connectionString = ConfigurationManager.ConnectionStrings["SQLiteConnectionString"].ConnectionString;
            SQLiteConnection connection = new SQLiteConnection(connectionString);
            connection.Open();
            return connection;
        }

        public static void Disconnect(SQLiteConnection connection)
        {
            connection.Close();
        }
    }
    
 }


