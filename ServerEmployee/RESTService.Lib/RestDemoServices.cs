using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;
using System.ServiceModel.Activation;
using Newtonsoft.Json;
using MySql.Data.MySqlClient;
using System.Drawing.Imaging;
using System.IO;
using System.Drawing;
using Newtonsoft.Json.Linq;
using Emgu.CV;
using Emgu.CV.Structure;
using System.Web;
using System.ServiceModel.Web;

namespace RESTService.Lib
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single, ConcurrencyMode = ConcurrencyMode.Single, IncludeExceptionDetailInFaults = true)]
    [AspNetCompatibilityRequirements(RequirementsMode = AspNetCompatibilityRequirementsMode.Allowed)]
    public class RestDemoServices : IRESTDemoServices
    {
        enum BadgeType { ENTER, EXIT };
        private string PATHDIR = "C:\\EmployeePhoto\\openSession";
        private string ROOT = "C:\\EmployeePhoto";
        private CascadeClassifier classifier = new CascadeClassifier("haarcascade_frontalface_default.xml");
        private Classifier_Train eigenRecog = new Classifier_Train();

        public ResponseMessage enterBadge(EmployeeBadge e)
        {

            DBConnect db = new DBConnect();
            MySqlConnection conn = db.getConnection();
            string query = "SELECT * FROM user where rfid='" + e.rfid + "'";
            MySqlCommand cmd = new MySqlCommand(query, conn);
            MySqlDataReader dataReader = cmd.ExecuteReader();
            string name = "none", surname = "none";
            if (dataReader.Read())
            {
                name = dataReader["name"].ToString();
                surname = dataReader["surname"].ToString();
            }
            else
            {
                conn.Close();
                return new ResponseMessage(201, "Not found RFID.");
            }

            conn.Close();


            db = new DBConnect();
            conn = db.getConnection();
            query = "SELECT * FROM sessions_user where session_id='" + e.session + "'";
            cmd = new MySqlCommand(query, conn);
            dataReader = cmd.ExecuteReader();
            if (!dataReader.Read())
            {
                conn.Close();
                return new ResponseMessage(201, "Not found session.");
            }
            conn.Close();

            db = new DBConnect();
            conn = db.getConnection();
            MySqlCommand cmd2 = new MySqlCommand();
            cmd2.Connection = conn;
            cmd2.CommandText = "INSERT INTO access(rfid,date,time,type,session_id) VALUES(?a,?b,?c,?d,?e)";
            cmd2.Parameters.Add("?a", MySqlDbType.VarChar).Value = e.rfid;
            DateTime d = DateTime.Now;
            cmd2.Parameters.Add("?b", MySqlDbType.Date).Value = d.Date;
            cmd2.Parameters.Add("?c", MySqlDbType.VarChar).Value = (d - DateTime.Today).TotalSeconds;
            cmd2.Parameters.Add("?d", MySqlDbType.Enum).Value = BadgeType.ENTER;
            cmd2.Parameters.Add("?e", MySqlDbType.LongBlob).Value = e.session;
            cmd2.ExecuteNonQuery();
            conn.Close();


            db = new DBConnect();
            conn = db.getConnection();
            MySqlCommand cmd3 = new MySqlCommand();
            cmd3.Connection = conn;
            cmd3.CommandText = "delete from sessions_user where session_id =?a ";
            cmd3.Parameters.Add("?a", MySqlDbType.VarChar).Value = e.session;
            cmd3.ExecuteNonQuery();
            conn.Close();

            concludeSession(e.rfid, e.session);
            if (facialDetection(getPictureUri(e.rfid, e.session)))
            {
                return new ResponseMessage(200, "Welcome " + name + " " + surname);
            }
            else
                return new ResponseMessage(404, "Face Not found.");


        }

        // sposta la foto nella cartella dell rfid corretto
        public void concludeSession(string rfid, string session)
        {
            if (!Directory.Exists(ROOT + "\\" + rfid))
                Directory.CreateDirectory(ROOT + "\\" + rfid);
            if (File.Exists(PATHDIR + "\\" + session + ".bmp"))
                File.Move(PATHDIR + "\\" + session + ".bmp", ROOT + "\\" + rfid + "\\" + session + ".bmp");

        }

        public string getPictureUri(string rfid, string session)
        {
            return ROOT + "\\" + rfid + "\\" + session + ".bmp";
        }



        public ResponseMessage exitBadge(EmployeeBadge e)
        {
            
            DBConnect db = new DBConnect();
            MySqlConnection conn = db.getConnection();
            string query = "SELECT * FROM user where rfid='" + e.rfid + "'";
            MySqlCommand cmd = new MySqlCommand(query, conn);
            MySqlDataReader dataReader = cmd.ExecuteReader();
            string name = "none", surname = "none";
            if (dataReader.Read())
            {
                name = dataReader["name"].ToString();
                surname = dataReader["surname"].ToString();
            }
            else
            {
                conn.Close();
                return new ResponseMessage(201, "Not found RFID.");
            }

            conn.Close();


            db = new DBConnect();
            conn = db.getConnection();
            query = "SELECT * FROM sessions_user where session_id='" + e.session + "'";
            cmd = new MySqlCommand(query, conn);
            dataReader = cmd.ExecuteReader();
            if (!dataReader.Read())
            {
                conn.Close();
                return new ResponseMessage(201, "Not found session.");
            }
            conn.Close();

            db = new DBConnect();
            conn = db.getConnection();
            MySqlCommand cmd2 = new MySqlCommand();
            cmd2.Connection = conn;
            cmd2.CommandText = "INSERT INTO access(rfid,date,time,type,session_id) VALUES(?a,?b,?c,?d,?e)";
            cmd2.Parameters.Add("?a", MySqlDbType.VarChar).Value = e.rfid;
            DateTime d = DateTime.Now;
            cmd2.Parameters.Add("?b", MySqlDbType.Date).Value = d.Date;
            cmd2.Parameters.Add("?c", MySqlDbType.VarChar).Value = (d - DateTime.Today).TotalSeconds;
            cmd2.Parameters.Add("?d", MySqlDbType.Enum).Value = BadgeType.ENTER;
            cmd2.Parameters.Add("?e", MySqlDbType.LongBlob).Value = e.session;
            cmd2.ExecuteNonQuery();
            conn.Close();


            db = new DBConnect();
            conn = db.getConnection();
            MySqlCommand cmd3 = new MySqlCommand();
            cmd3.Connection = conn;
            cmd3.CommandText = "delete from sessions_user where session_id =?a ";
            cmd3.Parameters.Add("?a", MySqlDbType.VarChar).Value = e.session;
            cmd3.ExecuteNonQuery();
            conn.Close();
            
            concludeSession(e.rfid, e.session);
            if (facialDetection(getPictureUri(e.rfid, e.session)))
            {
                try
                {
                    int sec = getSeconds(e);
                    return new ResponseMessage(200, "seconds:" + sec );
                }
                catch (Exception ex) { return new ResponseMessage(201, ex.Message); } 
            }
            else
                return new ResponseMessage(404, "Face Not found.");
        }

        private int getSeconds(EmployeeBadge e)
        {
            DBConnect db = new DBConnect();
            MySqlConnection conn = db.getConnection();
            string query = "SELECT * FROM `access` where rfid='" + e.rfid + "'" + " and session_id='" + e.session + "'" + " and type='" + BadgeType.EXIT + "'";
            MySqlCommand cmd = new MySqlCommand(query, conn);
            MySqlDataReader dataReader = cmd.ExecuteReader();
            DateTime d;
            string secondExit = "";
            string secondEnter = ""; 

            if (!dataReader.Read())
            {
                conn.Close();
                throw new Exception("Not found session or rfid or type wrong.");

            }
            else
            {
                d = (DateTime) dataReader["date"];
                secondExit = dataReader["time"].ToString(); 
            }
            conn.Close();

            string datesql = d.Year + "-" + d.Month + "-" + d.Day;
            query = "SELECT * FROM `access` where rfid='" + e.rfid + "'" + " and type='" + BadgeType.ENTER + "'" + " and date='" + datesql + "'";
            db = new DBConnect();
            conn = db.getConnection();
            cmd = new MySqlCommand(query, conn);
            dataReader = cmd.ExecuteReader();
            Double maxNow = 0.0;
            int control = 0; 
            while(dataReader.Read())
            {
                if (control == 0)
                    control++; 
                if (Double.Parse(dataReader["time"].ToString()) > maxNow)
                {
                    secondEnter = dataReader["time"].ToString();
                    maxNow = Double.Parse(dataReader["time"].ToString());
                }

            }
            if (control == 0)
            {
                conn.Close();
                throw new Exception("You are trying to cheat the system.");
            }
            conn.Close();
           //compute difference 
            Double fexit = 0.0,fenter = 0.0;
            fexit = Double.Parse(secondExit);
            fenter = Double.Parse(secondEnter);
            Double doublesecondsOfWork = fexit - fenter;
            int secondOfWork = (int)doublesecondsOfWork; 

            return secondOfWork; 
        }

        public ResponseMessage test(string s, Stream fileStream)
        {

            return new ResponseMessage(200, "stringa ricevuta:" + s);
        }

        public bool facialDetection(string pathpicture)
        {
            
            JObject o = new JObject();
            o["I_say"] = "Hello";

            Image<Bgr, byte> receivedImg = new Image<Bgr, byte>(pathpicture);
            Image<Gray, Byte> normalizedImg = receivedImg.Convert<Gray, Byte>();

            Rectangle[] rectangles = classifier.DetectMultiScale(normalizedImg, 1.4, 1, new Size(100, 100), new Size(800, 800));

            foreach (Rectangle r in rectangles)
                receivedImg.Draw(r, new Bgr(Color.Red), 2);

            receivedImg.Save("fotoFaced.jpg");
            
            o["rectangels"] = rectangles.Length;
            /*
            normalizedImg = receivedImg.Copy(rectangles[0]).Convert<Gray, byte>().Resize(100, 100, Emgu.CV.CvEnum.Inter.Cubic);
            normalizedImg._EqualizeHist();

            eigenRecog.AddTrainingImage(normalizedImg, "lenaRfidTest");

            eigenRecog.Retrain();

            o["rfid"] = eigenRecog.Recognise(normalizedImg, 5);
            */
            return rectangles.Length > 0;
        }

        public ResponseMessage test()
        {
            return new ResponseMessage(200, "it works.");
        }

        public Employee[] getEmployees()
        {
            WebOperationContext.Current.OutgoingResponse.Headers.Add("Access-Control-Allow-Origin", "*");
            WebOperationContext.Current.OutgoingResponse.Headers.Add("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE");
            WebOperationContext.Current.OutgoingResponse.Headers.Add("Access-Control-Allow-Headers", "Content-Type, Accept");
            WebOperationContext.Current.OutgoingResponse.Headers.Add("Access-Control-Max-Age", "1728000");

            DBConnect db = new DBConnect();
            MySqlConnection conn = db.getConnection();
            string query = "SELECT * FROM user";
            MySqlCommand cmd = new MySqlCommand(query, conn);

            MySqlDataReader dataReader = cmd.ExecuteReader();

            List<Employee> employeeArray = new List<Employee>();

            while (dataReader.Read())
            {
                Employee employee = new Employee();
                employee.rfid = dataReader["rfid"].ToString();
                employee.name = dataReader["name"].ToString();
                employee.surname = dataReader["surname"].ToString();

                employeeArray.Add(employee);
            }
            
            dataReader.Close();

            conn.Close();

            return employeeArray.ToArray();
        }

        public Employee getEmployee(string rfid)
        {
            WebOperationContext.Current.OutgoingResponse.Headers.Add("Access-Control-Allow-Origin", "*");
            WebOperationContext.Current.OutgoingResponse.Headers.Add("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE");
            WebOperationContext.Current.OutgoingResponse.Headers.Add("Access-Control-Allow-Headers", "Content-Type, Accept");
            WebOperationContext.Current.OutgoingResponse.Headers.Add("Access-Control-Max-Age", "1728000");

            Employee e = new Employee();
            e.rfid = rfid;

            DBConnect db = new DBConnect();
            MySqlConnection conn = db.getConnection();
            string query = "SELECT * FROM user where rfid='" + rfid + "'";
            MySqlCommand cmd = new MySqlCommand(query, conn);
            MySqlDataReader dataReader = cmd.ExecuteReader();
            if (dataReader.Read())
            {
                e.name = dataReader["name"].ToString();
                e.surname = dataReader["surname"].ToString();
            }

            conn.Close();

            return e;
        }

        public ResponseMessage addEmployee(string rfid, Employee employee)
        {
            WebOperationContext.Current.OutgoingResponse.Headers.Add("Access-Control-Allow-Origin", "*");
            WebOperationContext.Current.OutgoingResponse.Headers.Add("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE");
            WebOperationContext.Current.OutgoingResponse.Headers.Add("Access-Control-Allow-Headers", "Content-Type, Accept");
            WebOperationContext.Current.OutgoingResponse.Headers.Add("Access-Control-Max-Age", "1728000");

            DBConnect db = new DBConnect();
            MySqlConnection conn = db.getConnection();
            string query = "INSERT INTO `user`(`rfid`, `name`, `surname`, `photo`) VALUES ('" + employee.rfid + "','" + employee.name + "','" + employee.surname + "','null')";
            MySqlCommand cmd = new MySqlCommand(query, conn);
            int row = cmd.ExecuteNonQuery();

            conn.Close();

            if (row == 1)
                return new ResponseMessage(200, "OK");
            else
                return new ResponseMessage(500, "Server Error");
        }

        public ResponseMessage editEmployee(string rfid, Employee employee)
        {
            WebOperationContext.Current.OutgoingResponse.Headers.Add("Access-Control-Allow-Origin", "*");
            WebOperationContext.Current.OutgoingResponse.Headers.Add("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE");
            WebOperationContext.Current.OutgoingResponse.Headers.Add("Access-Control-Allow-Headers", "Content-Type, Accept");
            WebOperationContext.Current.OutgoingResponse.Headers.Add("Access-Control-Max-Age", "1728000");

            DBConnect db = new DBConnect();
            MySqlConnection conn = db.getConnection();
            string query = "UPDATE `user` SET `name` = '" + employee.name + "', `surname` = '" + employee.surname + "' WHERE `user`.`rfid` = '" + employee.rfid + "'";
            MySqlCommand cmd = new MySqlCommand(query, conn);
            int row = cmd.ExecuteNonQuery();

            conn.Close();

            if(row == 1)
                return new ResponseMessage(200, "OK");
            else
                return new ResponseMessage(500, "Server Error");
        }

        public ResponseMessage editEmployeeOptions(string rfid, Employee employee)
        {
            WebOperationContext.Current.OutgoingResponse.Headers.Add("Access-Control-Allow-Origin", "*");
            WebOperationContext.Current.OutgoingResponse.Headers.Add("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE");
            WebOperationContext.Current.OutgoingResponse.Headers.Add("Access-Control-Allow-Headers", "Content-Type, Accept");
            WebOperationContext.Current.OutgoingResponse.Headers.Add("Access-Control-Max-Age", "1728000");
            return new ResponseMessage(200, "OK");
        }

        public ResponseMessage deleteEmployee(string rfid)
        {
            WebOperationContext.Current.OutgoingResponse.Headers.Add("Access-Control-Allow-Origin", "*");
            WebOperationContext.Current.OutgoingResponse.Headers.Add("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE");
            WebOperationContext.Current.OutgoingResponse.Headers.Add("Access-Control-Allow-Headers", "Content-Type, Accept");
            WebOperationContext.Current.OutgoingResponse.Headers.Add("Access-Control-Max-Age", "1728000");

            DBConnect db = new DBConnect();
            MySqlConnection conn = db.getConnection();
            string query = "DELETE FROM `user` WHERE `user`.`rfid` = '" + rfid + "'";
            MySqlCommand cmd = new MySqlCommand(query, conn);
            int row = cmd.ExecuteNonQuery();

            conn.Close();

            if (row == 1)
                return new ResponseMessage(200, "OK");
            else
                return new ResponseMessage(500, "Server Error");
        }
    }
}
