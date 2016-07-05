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
using System.Xml;
using System.Threading;

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
        private int greenValue = 2100;
        private int yellowValue = 2600;
        
        public  ResponseMessageColor enterBadge(EmployeeBadge e)
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
                return new ResponseMessageColor(201, "Not found RFID.");
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
                return new ResponseMessageColor(201, "Not found session.");
            }
            conn.Close();


            if (!checkFace(PATHDIR + "\\" + e.session + ".bmp"))
            {
                File.Delete(PATHDIR + "\\" + e.session + ".bmp");

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


                return new ResponseMessageColor(404, "Face Not found.");


            }


            db = new DBConnect();
            conn = db.getConnection();
            query = "SELECT COUNT(*) FROM access where rfid='" + e.rfid + "'";
            cmd = new MySqlCommand(query, conn);
            int count = Convert.ToInt32(cmd.ExecuteScalar());
            if (count % 2 != 0)
            {
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


                File.Delete(PATHDIR + "\\" + e.session + ".bmp");
                return new ResponseMessageColor(201, "You cannot enter again.");
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
            MySqlCommand cmd6 = new MySqlCommand();
            cmd6.Connection = conn;
            cmd6.CommandText = "delete from sessions_user where session_id =?a ";
            cmd6.Parameters.Add("?a", MySqlDbType.VarChar).Value = e.session;
            cmd6.ExecuteNonQuery();
            conn.Close();

            concludeSession(e.rfid, e.session);
            db = new DBConnect();
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
            conn.Close();

            return faceDetectionAndConcludeSession(e, name, surname, true);




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

        private  void concludeSession(string rfid, string session)
        {
            createDirectories(rfid); 
            if (File.Exists(PATHDIR + "\\" + session + ".bmp"))
                File.Move(PATHDIR + "\\" + session + ".bmp", 
                    ROOT + "\\" + rfid + "\\" + ColorType.YELLOW.ToString() + "\\" +session + ".bmp");

        }

        private  string getPictureUri(string rfid, string session)
        {
            DBConnect db = new DBConnect();
            MySqlConnection conn = db.getConnection();
            string query = "SELECT * FROM colors where session_id='" + session + "' and rfid='"+rfid+"'";
            MySqlCommand cmd = new MySqlCommand(query, conn);
            MySqlDataReader dataReader = cmd.ExecuteReader();
            String currentColor; 
            if (dataReader.Read())
            {
                 currentColor = dataReader["color"].ToString();
            }
            else
            {
                conn.Close();
                return null; 
            }

            conn.Close();
            return ROOT + "\\" + rfid + "\\" +currentColor + "\\" + session + ".bmp";
        }

        public  ResponseMessageColor exitBadge(EmployeeBadge e)
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
                return new ResponseMessageColor(201, "Not found RFID.");
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
                return new ResponseMessageColor(201, "Not found session.");
            }
            conn.Close();

            if (!checkFace(PATHDIR + "\\" + e.session + ".bmp"))
            {
                File.Delete(PATHDIR + "\\" + e.session + ".bmp");

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


                return new ResponseMessageColor(404, "Face Not found.");


            }


            db = new DBConnect();
            conn = db.getConnection();
            query = "SELECT COUNT(*) FROM access where rfid='" + e.rfid + "'";
            cmd = new MySqlCommand(query, conn);
            int count = Convert.ToInt32(cmd.ExecuteScalar());
            if (count % 2 == 0)
            {
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

                File.Delete(PATHDIR + "\\" + e.session + ".bmp");
                return new ResponseMessageColor(201, "You cannot exit again.");
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
            MySqlCommand cmd5 = new MySqlCommand();
            cmd5.Connection = conn;
            cmd5.CommandText = "delete from sessions_user where session_id =?a ";
            cmd5.Parameters.Add("?a", MySqlDbType.VarChar).Value = e.session;
            try
            {
                cmd5.ExecuteNonQuery();
            }
            catch (MySqlException ex)
            {
                Debug.WriteLine("Failed to delete from sessions_user, error code: " + ex.Number);
            }
            conn.Close();


            concludeSession(e.rfid, e.session);
            db = new DBConnect();
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
            conn.Close();


            

                return faceDetectionAndConcludeSession(e, name, surname,false);
           

            
        }


        private bool checkFace(string path)
        { 
            
           

            Image<Bgr, byte> receivedImg = new Image<Bgr, byte>(path);
            Image<Gray, byte> normalizedImg = receivedImg.Convert<Gray, Byte>();

            Rectangle[] rectangles = classifier.DetectMultiScale(normalizedImg, 1.4, 1, new Size(100, 100), new Size(800, 800));

            foreach (Rectangle r in rectangles)
                receivedImg.Draw(r, new Bgr(Color.Red), 2);

            receivedImg.Save("photoFaced.jpg");

            if (rectangles.Length <= 0)
            {
                return false;
            }
            return true; 

        }

        private String getSeconds(EmployeeBadge e)
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

            TimeSpan span = TimeSpan.FromSeconds(secondOfWork);
            return span.ToString(@"hh\:mm\:ss");
        }

        public  ResponseMessage test(string s, Stream fileStream)
        {
            return new ResponseMessage(200, "stringa ricevuta:" + s);
        }

        public ResponseMessageColor faceDetectionAndConcludeSession(EmployeeBadge employee, string name, string surname,bool isentering)
        {

            DBConnect db = new DBConnect();
            MySqlConnection conn = db.getConnection();
            string query = "SELECT * FROM colors where color='GREEN' and rfid='" + employee.rfid + "'";
            MySqlCommand cmd = new MySqlCommand(query, conn);
            MySqlDataReader dataReader = cmd.ExecuteReader();
            
            int count=0;
            while(dataReader.Read())
            {
                count++; 
            }

            conn.Close();

            if (count < 5)
            {
                if (isentering)
                { new ResponseMessageColor(200, "Welcome " + name + " " + surname, "YELLOW");  }
                else
                    return new ResponseMessageColor(200, "You worked "+getSeconds(employee) +" seconds" , "YELLOW");

            }
           






            string pathpicture = getPictureUri(employee.rfid, employee.session);

            Image<Bgr, byte> receivedImg = new Image<Bgr, byte>(pathpicture);
            Image<Gray, byte> normalizedImg = receivedImg.Convert<Gray, Byte>();

            Rectangle[] rectangles = classifier.DetectMultiScale(normalizedImg, 1.4, 1, new Size(100, 100), new Size(800, 800));

            foreach (Rectangle r in rectangles)
                receivedImg.Draw(r, new Bgr(Color.Red), 2);

            receivedImg.Save("photoFaced.jpg");

            if (rectangles.Length <= 0)
            {

                // NEVER REACHED THIS CODE ! ! ! ! ! 


                //put into red folder
               /* ChangePicture c = new ChangePicture();
                c.rfid = employee.rfid;
                c.session_id = employee.session;
                c.color = ColorType.RED.ToString();
                changeColor(c);*/
               // putSessionPhotoIntoRightFolder(employee, ColorType.RED);
                return new ResponseMessageColor(404, "Never reached this code.");
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
                    ChangePicture c = new ChangePicture();
                    c.rfid = employee.rfid; 
                    c.session_id = employee.session; 
                    c.color = ColorType.RED.ToString();
                    changeColor(c); 
                    //putSessionPhotoIntoRightFolder(employee, ColorType.RED);
                    if(isentering)
                    return new ResponseMessageColor(200, "Welcome " + name + " " + surname);
                    else
                        return new ResponseMessageColor(200, "You worked " + getSeconds(employee).ToString() + " seconds");
                }
                else
                {
                    //put into yellow folder
                    ChangePicture c = new ChangePicture();
                    c.rfid = employee.rfid;
                    c.session_id = employee.session;
                    c.color = ColorType.YELLOW.ToString();
                    changeColor(c); 
                    //putSessionPhotoIntoRightFolder(employee, ColorType.YELLOW);
                    if(isentering)
                    return new ResponseMessageColor(200, "Welcome " + name + " " + surname,"YELLOW");
                    else
                        return new ResponseMessageColor(200, "You worked " + getSeconds(employee) + " seconds", "YELLOW");
                }
            }
            else
            {
                //put into green folder
                ChangePicture c = new ChangePicture();
                c.rfid = employee.rfid;
                c.session_id = employee.session;
                c.color = ColorType.GREEN.ToString();
                changeColor(c); 
                eigenRecog.AddTrainingImage(normalizedImg, employee.rfid, employee.rfid+".bmp");
                if(isentering)
                return new ResponseMessageColor(200, "Welcome " + name + " " + surname,"GREEN");
                else return new ResponseMessageColor(200, "You worked " + getSeconds(employee) + " seconds", "GREEN");
            }
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

            WebOperationContext.Current.OutgoingResponse.Headers.Add("Access-Control-Allow-Origin", "*");
            WebOperationContext.Current.OutgoingResponse.Headers.Add("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE");
            WebOperationContext.Current.OutgoingResponse.Headers.Add("Access-Control-Allow-Headers", "Content-Type, Accept");
            WebOperationContext.Current.OutgoingResponse.Headers.Add("Access-Control-Max-Age", "1728000");

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
                string path = ROOT + "\\" + rfid + "\\" +"GREEN"+"\\"+ rfid + ".bmp";
                string base64picture = Convert.ToBase64String(File.ReadAllBytes(path));
                employee.picture = base64picture;
            }

            conn.Close();
            return employee;
        }
        
        public  ResponseMessage changeColor(ChangePicture emp)
        {

            WebOperationContext.Current.OutgoingResponse.Headers.Add("Access-Control-Allow-Origin", "*");
            WebOperationContext.Current.OutgoingResponse.Headers.Add("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE");
            WebOperationContext.Current.OutgoingResponse.Headers.Add("Access-Control-Allow-Headers", "Content-Type, Accept");
            WebOperationContext.Current.OutgoingResponse.Headers.Add("Access-Control-Max-Age", "1728000");


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

            if (oldcolor != "" && oldcolor!=emp.color)
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
                    if (emp.color.Equals(ColorType.GREEN.ToString()))
                    {
                        string trainedFacesFolder = ROOT + "\\" + emp.rfid + "\\" + ColorType.GREEN.ToString();
                        string fileName = emp.session_id + ".bmp"; 


                        if (File.Exists(trainedFacesFolder + "/TrainedLabels.xml"))
                        {
                            XmlDocument docu = new XmlDocument();
                            bool loading = true;
                            while (loading)
                            {
                                try
                                {
                                    docu.Load(trainedFacesFolder + "/TrainedLabels.xml");
                                    loading = false;
                                }
                                catch
                                {
                                    docu = null;
                                    docu = new XmlDocument();
                                    Thread.Sleep(10);
                                }
                            }

                            //Get the root element
                            XmlElement root = docu.DocumentElement;

                            XmlElement employee_D = docu.CreateElement("EMPLOYEE");
                            XmlElement rfid_D = docu.CreateElement("RFID");
                            XmlElement file_D = docu.CreateElement("FILE");

                            //Add the values for each nodes
                            rfid_D.InnerText = emp.rfid;
                            file_D.InnerText = fileName;

                            //Construct the employee element
                            employee_D.AppendChild(rfid_D);
                            employee_D.AppendChild(file_D);

                            //Add the New employee element to the end of the root element
                            root.AppendChild(employee_D);

                            //Save the document
                            docu.Save(trainedFacesFolder + "/TrainedLabels.xml");
                        }


                    }
                    else if (oldcolor.Equals(ColorType.GREEN.ToString()))
                    {

                        //loadfile
                        string trainedFacesFolder = ROOT + "\\" + emp.rfid + "\\" + ColorType.GREEN.ToString();
                        string fileName = emp.session_id + ".bmp"; 


                        if (File.Exists(trainedFacesFolder + "/TrainedLabels.xml"))
                        {
                            XmlDocument docu = new XmlDocument();
                            bool loading = true;
                            while (loading)
                            {
                                try
                                {
                                    docu.Load(trainedFacesFolder + "/TrainedLabels.xml");
                                    loading = false;
                                }
                                catch
                                {
                                    docu = null;
                                    docu = new XmlDocument();
                                    Thread.Sleep(10);
                                }
                            }


                            XmlNodeList nodeList = docu.SelectNodes("//EMPLOYEE[contains(FILE,'"+fileName+"')]");
                           
                            foreach(XmlNode n in nodeList)
                                n.ParentNode.RemoveChild(n);
                            //Save the document
                            docu.Save(trainedFacesFolder + "/TrainedLabels.xml");
                        }


                       

                    }
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


            WebOperationContext.Current.OutgoingResponse.Headers.Add("Access-Control-Allow-Origin", "*");
            WebOperationContext.Current.OutgoingResponse.Headers.Add("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE");
            WebOperationContext.Current.OutgoingResponse.Headers.Add("Access-Control-Allow-Headers", "Content-Type, Accept");
            WebOperationContext.Current.OutgoingResponse.Headers.Add("Access-Control-Max-Age", "1728000");


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




        public ResponseMessage addEmployee(string rfid, EmployeePic employee)
        {
            WebOperationContext.Current.OutgoingResponse.Headers.Add("Access-Control-Allow-Origin", "*");
            WebOperationContext.Current.OutgoingResponse.Headers.Add("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE");
            WebOperationContext.Current.OutgoingResponse.Headers.Add("Access-Control-Allow-Headers", "Content-Type, Accept");
            WebOperationContext.Current.OutgoingResponse.Headers.Add("Access-Control-Max-Age", "1728000");

            try
            {
                createDirectories(rfid);
                byte[] pict = Convert.FromBase64String(employee.picture);
                string pictName = employee.employee.rfid;
                string path = ROOT + "\\" + rfid + "\\" + ColorType.GREEN.ToString() + "\\" + pictName + ".bmp";
                File.WriteAllBytes(path, pict);

                Image<Bgr, byte> receivedImg = new Image<Bgr, byte>(path);
                Image<Gray, byte> normalizedImg = receivedImg.Convert<Gray, Byte>();

                Rectangle[] rectangles = classifier.DetectMultiScale(normalizedImg, 1.4, 1, new Size(100, 100), new Size(800, 800));

                foreach (Rectangle r in rectangles)
                    receivedImg.Draw(r, new Bgr(Color.Red), 2);

                if (rectangles.Length <= 0)
                {
                    //put into red folder
                    return new ResponseMessage(201, "Impossible to insert a new Employee.Face Not found.");
                }

                normalizedImg = receivedImg.Copy(rectangles[0]).Convert<Gray, byte>().Resize(64, 64, Emgu.CV.CvEnum.Inter.Cubic);
                normalizedImg._EqualizeHist();

                Classifier_Train eigenRecog = new Classifier_Train(ROOT + "\\" + rfid + "\\" + ColorType.GREEN.ToString());


                eigenRecog.AddTrainingImage(normalizedImg, rfid,rfid+".bmp");

                /* Image<Gray, byte> dummyImg = new Image<Gray, byte>("dummy.jpg");
                 File.Copy("dummy.jpg", ROOT + "\\" + rfid + "\\" + ColorType.GREEN.ToString()+"\\"+"dummy.jpg"); 
                eigenRecog.AddTrainingImage(dummyImg, "Unknown","dummy.jpg");*/

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
            }
            catch (Exception e)
            {
                return new ResponseMessage(201, "Impossible to insert a new Employee.");
            }
            return new ResponseMessage(200, employee.employee.name + " " + employee.employee.surname + " added correctly.");

        }





        public  ResponseMessage addEmployee2(string rfid, EmployeePic employee)
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

        public ResponseMessage editEmployeeOptions(string rfid, Employee employee)
        {
            WebOperationContext.Current.OutgoingResponse.Headers.Add("Access-Control-Allow-Origin", "*");
            WebOperationContext.Current.OutgoingResponse.Headers.Add("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE");
            WebOperationContext.Current.OutgoingResponse.Headers.Add("Access-Control-Allow-Headers", "Content-Type, Accept");
            WebOperationContext.Current.OutgoingResponse.Headers.Add("Access-Control-Max-Age", "1728000");
            return new ResponseMessage(200, "OK");
        }

        public ResponseMessage changeColorOptions(ChangePicture emp)
        {
            WebOperationContext.Current.OutgoingResponse.Headers.Add("Access-Control-Allow-Origin", "*");
            WebOperationContext.Current.OutgoingResponse.Headers.Add("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE");
            WebOperationContext.Current.OutgoingResponse.Headers.Add("Access-Control-Allow-Headers", "Content-Type, Accept");
            WebOperationContext.Current.OutgoingResponse.Headers.Add("Access-Control-Max-Age", "1728000");
            return new ResponseMessage(200, "OK");
        }


        public  ResponseMessage editEmployee(string rfid, Employee employee)
        {

            WebOperationContext.Current.OutgoingResponse.Headers.Add("Access-Control-Allow-Origin", "*");
            WebOperationContext.Current.OutgoingResponse.Headers.Add("Access-Control-Allow-Methods", "POST, PUT, DELETE");
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

            Directory.Delete(ROOT + "\\" + rfid, true); 


            conn.Close();

            if (row == 1)
                return new ResponseMessage(200, "OK");
            else
                return new ResponseMessage(500, "Server Error");
        }
    }
}
