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
using System.Diagnostics;

namespace RESTService.Lib
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single, ConcurrencyMode = ConcurrencyMode.Single, IncludeExceptionDetailInFaults = true)]
    [AspNetCompatibilityRequirements(RequirementsMode = AspNetCompatibilityRequirementsMode.Allowed)]
    public class RestDemoServices : IRESTDemoServices
    {
        enum BadgeType { ENTER, EXIT };
        enum ColorType { YELLOW, RED, GREEN };
        private string PATHDIR = "C:\\EmployeePhoto\\openSession";
        private string ROOT = "C:\\EmployeePhoto";
        private CascadeClassifier classifier = new CascadeClassifier("haarcascade_frontalface_default.xml");
        private int greenValue = 2600;
        private int yellowValue = 1500;
        
        public  ResponseMessage enterBadge(EmployeeBadge e)
        {
            Debug.WriteLine("enterBadge called");
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
                Debug.WriteLine("RFID belongs to " + name + " " + surname);
            }
            else
            {
                conn.Close();
                Debug.WriteLine("Person with that RFID not found");
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
                Debug.WriteLine("Session '" + e.session + "' not found");
                return new ResponseMessage(201, "Not found session.");
            }
            conn.Close();

            db = new DBConnect();
            conn = db.getConnection();
            query = "SELECT COUNT(*) FROM access where rfid='" + e.rfid + "'";
            cmd = new MySqlCommand(query, conn);
            int count = Convert.ToInt32(cmd.ExecuteScalar());
            if (count % 2 != 0)
            {
                conn.Close();
                File.Delete(PATHDIR + "\\" + e.session + ".bmp");
                return new ResponseMessage(201, "You cannot enter again.");
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
            try
            {
                cmd2.ExecuteNonQuery();
                Debug.WriteLine("INSERT for RFID "+e.rfid+", date "+ d.Date.ToShortDateString()+", time "+(d - DateTime.Today).TotalSeconds.ToString()+" session_id "+e.session+" type ENTER into access executed");
            }
            catch(MySqlException ex)
            {
                Debug.WriteLine("Insertion execution failed, error code: " +ex.Number);
            }
            conn.Close();

            db = new DBConnect();
            conn = db.getConnection();
            MySqlCommand cmd3 = new MySqlCommand();
            cmd3.Connection = conn;
            cmd3.CommandText = "delete from sessions_user where session_id =?a ";
            cmd3.Parameters.Add("?a", MySqlDbType.VarChar).Value = e.session;
            cmd3.ExecuteNonQuery();
            conn.Close();

            //concludeSession(e.rfid, e.session);
            /*db = new DBConnect();
            conn = db.getConnection();
            cmd2 = new MySqlCommand();
            cmd2.Connection = conn;
            cmd2.CommandText = "INSERT INTO colors(session_id,rfid,color) VALUES(?a,?b,?c)";
            cmd2.Parameters.Add("?a", MySqlDbType.VarChar).Value = e.session;
            cmd2.Parameters.Add("?b", MySqlDbType.VarChar).Value = e.rfid;
            cmd2.Parameters.Add("?c", MySqlDbType.Enum).Value = ColorType.YELLOW; 
            try
            {
                cmd2.ExecuteNonQuery();
            }
            catch (MySqlException ex)
            {
                Debug.WriteLine("Insertion execution failed, error code: " + ex.Number);
            }
            conn.Close();*/

            return faceDetectionAndConcludeSession(e, name, surname);

            /*if (faceDetectionAndConcludeSession(e))
            {
                Debug.WriteLine("Face succesfully detected");
                return new ResponseMessage(200, "Welcome " + name + " " + surname);
            }
            else
            {
                Debug.WriteLine("Face not detected");
                return new ResponseMessage(404, "Face Not found.");
            }*/


        }

        private void createDirectories(string rfid)
        {
            if (!Directory.Exists(ROOT + "\\" + rfid))
            {
                Directory.CreateDirectory(ROOT + "\\" + rfid);
                Directory.CreateDirectory(ROOT + "\\" + rfid + "\\" + ColorType.YELLOW.ToString());
                Directory.CreateDirectory(ROOT + "\\" + rfid + "\\" + ColorType.RED.ToString());
                Directory.CreateDirectory(ROOT + "\\" + rfid + "\\" + ColorType.GREEN.ToString());
            }
        }

        /*private  void concludeSession(string rfid, string session)
        {
            createDirectories(rfid); 
            if (File.Exists(PATHDIR + "\\" + session + ".bmp"))
                File.Move(PATHDIR + "\\" + session + ".bmp", 
                    ROOT + "\\" + rfid + "\\" + ColorType.YELLOW.ToString() + "\\" +session + ".bmp");

        }*/

        private  string getPictureUri(string rfid, string session)
        {
            DBConnect db = new DBConnect();
            MySqlConnection conn = db.getConnection();
            string query = "SELECT * FROM colors where session_id='" + session + "' and rfid='"+rfid+"'";
            MySqlCommand cmd = new MySqlCommand(query, conn);
            MySqlDataReader dataReader = cmd.ExecuteReader();
            ColorType currentColor; 
            if (dataReader.Read())
            {
                 currentColor = (ColorType) dataReader["color"];
            }
            else
            {
                conn.Close();
                return null; 
            }

            conn.Close();
            return ROOT + "\\" + rfid + "\\" +currentColor.ToString() + "\\" + session + ".bmp";
        }

        public  ResponseMessage exitBadge(EmployeeBadge e)
        {
            Debug.WriteLine("exitBadge called");
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
                Debug.WriteLine("RFID belongs to " + name + " " + surname);
            }
            else
            {
                conn.Close();
                Debug.WriteLine("Person with that RFID not found");
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
                Debug.WriteLine("Session '" + e.session + "' not found");
                conn.Close();
                return new ResponseMessage(201, "Not found session.");
            }
            conn.Close();

            db = new DBConnect();
            conn = db.getConnection();
            query = "SELECT COUNT(*) FROM access where rfid='" + e.rfid + "'";
            cmd = new MySqlCommand(query, conn);
            int count = Convert.ToInt32(cmd.ExecuteScalar());
            if (count % 2 == 0)
            {
                conn.Close();
                File.Delete(PATHDIR + "\\" + e.session + ".bmp");
                return new ResponseMessage(201, "You cannot exit again.");
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
            cmd2.Parameters.Add("?d", MySqlDbType.Enum).Value = BadgeType.EXIT;
            cmd2.Parameters.Add("?e", MySqlDbType.LongBlob).Value = e.session;
            try
            {
                cmd2.ExecuteNonQuery();
                Debug.WriteLine("INSERT for RFID " + e.rfid + ", date " + d.Date.ToShortDateString() + ", time " + (d - DateTime.Today).TotalSeconds.ToString() + " session_id " + e.session + " type EXIT into access executed");
            }
            catch (MySqlException ex)
            {
                Debug.WriteLine("Insertion execution failed, error code: " + ex.Number);
            }
            conn.Close();

            db = new DBConnect();
            conn = db.getConnection();
            MySqlCommand cmd3 = new MySqlCommand();
            cmd3.Connection = conn;
            cmd3.CommandText = "delete from sessions_user where session_id =?a ";
            cmd3.Parameters.Add("?a", MySqlDbType.VarChar).Value = e.session;
            try
            {
                cmd3.ExecuteNonQuery();
            }
            catch (MySqlException ex)
            {
                Debug.WriteLine("Failed to delete from sessions_user, error code: " + ex.Number);
            }
            conn.Close();

            return faceDetectionAndConcludeSession(e, name, surname);

            //concludeSession(e.rfid, e.session);
            /*if (facialDetection(getPictureUri(e.rfid, e.session)))
            {
                try
                {
                    int sec = getSeconds(e);
                    return new ResponseMessage(200, "seconds:" + sec );
                }
                catch (Exception ex) 
                {
                    return new ResponseMessage(201, ex.Message);
                } 
            }
            else
                return new ResponseMessage(404, "Face Not found.");*/
        }

        private int getSeconds(EmployeeBadge e)
        {
            Debug.WriteLine("getSeconds called");
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
                Debug.WriteLine("Entry for {0} {1} {2} not found in 'access' table", e.rfid, e.session, BadgeType.EXIT);
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
            Debug.WriteLine("calculated seconds: " + secondOfWork.ToString());
            return secondOfWork; 
        }

        public  ResponseMessage test(string s, Stream fileStream)
        {
            return new ResponseMessage(200, "stringa ricevuta:" + s);
        }

        public ResponseMessage faceDetectionAndConcludeSession(EmployeeBadge employee, string name, string surname)
        {
            string pathpicture = getPictureUri(employee.rfid, employee.session);

            Image<Bgr, byte> receivedImg = new Image<Bgr, byte>(pathpicture);
            Image<Gray, byte> normalizedImg = receivedImg.Convert<Gray, Byte>();

            Rectangle[] rectangles = classifier.DetectMultiScale(normalizedImg, 1.4, 1, new Size(100, 100), new Size(800, 800));

            foreach (Rectangle r in rectangles)
                receivedImg.Draw(r, new Bgr(Color.Red), 2);

            receivedImg.Save("photoFaced.jpg");

            if (rectangles.Length <= 0)
            {
                //put into red folder
                putSessionPhotoIntoRightFolder(employee, ColorType.RED);
                return new ResponseMessage(404, "Face Not found.");
            }

            if (!employee.faceRecognition)
            {
                //put into yellow folder
                putSessionPhotoIntoRightFolder(employee, ColorType.YELLOW);
                return new ResponseMessage(200, "Welcome " + name + " " + surname);
            }
                
            //face recognition
            normalizedImg = receivedImg.Copy(rectangles[0]).Convert<Gray, byte>().Resize(64, 64, Emgu.CV.CvEnum.Inter.Cubic);
            normalizedImg._EqualizeHist();

            Classifier_Train eigenRecog = new Classifier_Train(ROOT + "\\" + employee.rfid + "\\" + ColorType.GREEN.ToString());
            
            string labelName = eigenRecog.Recognize(normalizedImg, greenValue);

            //eigenRecog.AddTrainingImage(normalizedImg, "Unknown");

            if (labelName == null || labelName == "" || labelName == "Unknown")
            {
                labelName = eigenRecog.Recognize(normalizedImg, yellowValue);

                if (labelName == null || labelName == "" || labelName == "Unknown")
                {
                    //put into red folder
                    putSessionPhotoIntoRightFolder(employee, ColorType.RED);
                    return new ResponseMessage(404, "Face Not Recognized.");
                }
                else
                {
                    //put into yellow folder
                    putSessionPhotoIntoRightFolder(employee, ColorType.YELLOW);
                    return new ResponseMessage(200, "Welcome " + name + " " + surname);
                }
            }
            else
            {
                //put into green folder
                eigenRecog.AddTrainingImage(normalizedImg, labelName);
                return new ResponseMessage(200, "Welcome " + name + " " + surname);
            }
        }

        private void putSessionPhotoIntoRightFolder(EmployeeBadge employee, ColorType colorType)
        {
            if (File.Exists(PATHDIR + "\\" + employee.session + ".bmp"))
                File.Move(PATHDIR + "\\" + employee.session + ".bmp",
                    ROOT + "\\" + employee.rfid + "\\" + colorType.ToString() + "\\" + employee.session + ".bmp");

            DBConnect db = new DBConnect();
            MySqlConnection conn = db.getConnection();
            MySqlCommand cmd = new MySqlCommand();
            cmd.Connection = conn;
            cmd.CommandText = "INSERT INTO colors(session_id,rfid,color) VALUES(?a,?b,?c)";
            cmd.Parameters.Add("?a", MySqlDbType.VarChar).Value = employee.session;
            cmd.Parameters.Add("?b", MySqlDbType.VarChar).Value = employee.rfid;
            cmd.Parameters.Add("?c", MySqlDbType.Enum).Value = colorType;

            try
            {
                cmd.ExecuteNonQuery();
            }
            catch (MySqlException ex)
            {
                Debug.WriteLine("Insertion execution failed, error code: " + ex.Number);
            }
            conn.Close();
        }

        public  ResponseMessage test()
        {
            return new ResponseMessage(200, "it works.");
        }

        public  Employee[] getEmployees()
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

        public  EmployeePic getInfo(string rfid)
        {
            DBConnect db = new DBConnect();
            MySqlConnection conn = db.getConnection();
            string query = "SELECT * FROM user where rfid='" + rfid + "'";
            MySqlCommand cmd = new MySqlCommand(query, conn);
            MySqlDataReader dataReader = cmd.ExecuteReader();
            EmployeePic employee = new EmployeePic();
            employee.employee = new Employee();
            if (dataReader.Read())
            {
                employee.employee.rfid = dataReader["rfid"].ToString();
                employee.employee.name = dataReader["name"].ToString();
                employee.employee.surname = dataReader["surname"].ToString();
                string path = ROOT + "\\" + rfid + "\\" + rfid + ".bmp";
                string base64picture = Convert.ToBase64String(File.ReadAllBytes(path));
                employee.picture = base64picture;
            }

            conn.Close();
            return employee;
        }
        
        public  ResponseMessage changeColor(ChangePicture emp)
        {

            DBConnect db = new DBConnect();
            MySqlConnection conn = db.getConnection();
            string query = "SELECT * FROM colors where rfid='" + emp.rfid + "' and session_id='" + emp.session_id + "';";
            MySqlCommand cmd = new MySqlCommand(query, conn);
            MySqlDataReader dataReader = cmd.ExecuteReader();
            string oldcolor = ""; 
            while (dataReader.Read())
            {
                oldcolor = dataReader["color"].ToString(); 

            }
            conn.Close();

            if (oldcolor != "")
            {
                db = new DBConnect();
                conn = db.getConnection();
                query = "UPDATE `colors` SET `color`='" + emp.color + "' WHERE `session_id`='" + emp.session_id + "' and `rfid`='" + emp.rfid + "';";
                cmd = new MySqlCommand(query, conn);
                int row = cmd.ExecuteNonQuery();
                if (row == 1)
                {
                    File.Move(ROOT + "\\" + emp.rfid + "\\" + oldcolor + "\\" + emp.session_id + ".bmp",
                        ROOT + "\\" + emp.rfid + "\\" + emp.color + "\\" + emp.session_id + ".bmp");
                    conn.Close();
                    return new ResponseMessage(200, "Color updated for " + emp.session_id);

                }
                else
                {
                    conn.Close();
                    return new ResponseMessage(201, "Unable to change color.");
                }


            }
            return new ResponseMessage(201, "Unable to change color.");

        }
        
        public  ColoredPicture[] getPictureColored(string rfid, string color)
        {
            List<ColoredPicture> collection = new List<ColoredPicture>(); 
            DBConnect db = new DBConnect();
            MySqlConnection conn = db.getConnection();
            string query = "SELECT * FROM colors where rfid='" + rfid + "' and color='"+color+"';";
            MySqlCommand cmd = new MySqlCommand(query, conn);
            MySqlDataReader dataReader = cmd.ExecuteReader();
            while(dataReader.Read())
            {
                string gotrfid = dataReader["rfid"].ToString();
                string gotsession = dataReader["session_id"].ToString();
                string gotcolor = dataReader["color"].ToString();
                string pathpicture = ROOT + "\\" + gotrfid + "\\" + gotcolor + "\\" + gotsession + ".bmp";
                string base64picture = Convert.ToBase64String(File.ReadAllBytes(pathpicture));
                ColoredPicture toret = new ColoredPicture();
                toret.session_id = gotsession;
                toret.rfid = gotrfid;
                toret.picture = base64picture;
                collection.Add(toret); 
                
            }
            conn.Close();

            return collection.ToArray(); 

        }
        
        public  Employee getEmployee(string rfid)
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
        
        public  ResponseMessage addEmployee(string rfid, EmployeePic employee)
        {
            WebOperationContext.Current.OutgoingResponse.Headers.Add("Access-Control-Allow-Origin", "*");
            WebOperationContext.Current.OutgoingResponse.Headers.Add("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE");
            WebOperationContext.Current.OutgoingResponse.Headers.Add("Access-Control-Allow-Headers", "Content-Type, Accept");
            WebOperationContext.Current.OutgoingResponse.Headers.Add("Access-Control-Max-Age", "1728000");

            try{
            createDirectories(rfid); 
            byte[] pict = Convert.FromBase64String(employee.picture);
            string pictName = employee.employee.rfid;
            string path = ROOT + "\\" + rfid + "\\" + ColorType.GREEN.ToString() + "\\" + pictName + ".bmp";
            File.WriteAllBytes(path, pict);
            DBConnect db = new DBConnect(); 
            MySqlConnection conn = db.getConnection();
            MySqlCommand cmd2 = new MySqlCommand();
            cmd2.Connection = conn;
            cmd2.CommandText = "INSERT INTO `user`(`rfid`, `name`, `surname`) VALUES (?a,?b,?c);";
            cmd2.Parameters.Add("?a", MySqlDbType.VarChar).Value = employee.employee.rfid;
            cmd2.Parameters.Add("?b", MySqlDbType.VarChar).Value = employee.employee.name;
            cmd2.Parameters.Add("?c", MySqlDbType.VarChar).Value = employee.employee.surname;
            cmd2.ExecuteNonQuery();
            conn.Close(); 
            }catch(Exception e){
                return new ResponseMessage(201,"Impossible to insert a new Employee."); 
            }
            return new ResponseMessage(200, employee.employee.name + " " + employee.employee.surname + " added correctly."); 
            
        }
        
        public  ResponseMessage editEmployee(string rfid, Employee employee)
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
        
        public  ResponseMessage deleteEmployee(string rfid)
        {
            WebOperationContext.Current.OutgoingResponse.Headers.Add("Access-Control-Allow-Origin", "*");
            WebOperationContext.Current.OutgoingResponse.Headers.Add("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE");
            WebOperationContext.Current.OutgoingResponse.Headers.Add("Access-Control-Allow-Headers", "Content-Type, Accept");
            WebOperationContext.Current.OutgoingResponse.Headers.Add("Access-Control-Max-Age", "1728000");

            DBConnect db = new DBConnect();
            MySqlConnection conn = db.getConnection();
            string query = "DELETE FROM `user` WHERE `rfid` = '" + rfid + "'";
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
