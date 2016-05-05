using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using MySql.Data.MySqlClient;

namespace SocketPhoto
{
    class ServerProcessor
    {
        private System.Net.Sockets.Socket handler;
        private int TIMECLOSE = 10000;
        private string PATHDIR ="C:\\EmployeePhoto\\openSession";
        private string ROOT = "C:\\EmployeePhoto"; 
        public ServerProcessor(System.Net.Sockets.Socket handler)
        {
            // TODO: Complete member initialization
            this.handler = handler;
            this.handler.ReceiveTimeout = TIMECLOSE;
            this.handler.SendTimeout = TIMECLOSE;
        }


        public static string RandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, length)
              .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        internal void SERVICE()
        {
            if (!Directory.Exists(ROOT))
                Directory.CreateDirectory(ROOT);
            if (!Directory.Exists(PATHDIR))
                Directory.CreateDirectory(PATHDIR); 
                
            string filenameTemp = PATHDIR + "\\" +RandomString(20)+".bmp";
            MySocketFunctions.socketReceiveFile(handler, filenameTemp);
            string hash = calcChecksum(filenameTemp);
            MySocketFunctions.socketWriteLine(handler, hash);

            string filenameFinal = PATHDIR + "\\" + hash + ".bmp";
            File.Copy(filenameTemp, filenameFinal);
            File.Delete(filenameTemp);

            DBConnect db = new DBConnect();
            MySqlConnection conn = db.getConnection();
            MySqlCommand cmd2 = new MySqlCommand();
            cmd2.Connection = conn;
            cmd2.CommandText = "INSERT INTO sessions_user(session_id) VALUES(?a)";
            cmd2.Parameters.Add("?a", MySqlDbType.VarChar).Value = hash;
            cmd2.ExecuteNonQuery();
            conn.Close();

        }

        private static string calcChecksum(string fileName)
        {
            using (MD5 md5Hash = MD5.Create())
            {
                string hash = GetMd5Hash(md5Hash, fileName);
                return hash;

            }

        }
        private static string GetMd5Hash(MD5 md5Hash, string reference)
        {
            FileInfo f1 = new FileInfo(reference);
            int total_byte = (int)f1.Length;
            byte[] data = new byte[total_byte];
            FileStream file = File.Open(reference, FileMode.Open);
            int read = file.Read(data, 0, total_byte);
            file.Close();
            byte[] data_out = md5Hash.ComputeHash(data);
            StringBuilder sBuilder = new StringBuilder();
            for (int i = 0; i < data_out.Length; i++)
            {
                sBuilder.Append(data_out[i].ToString("x2"));
            }
            return sBuilder.ToString();
        }

        
    }
}
