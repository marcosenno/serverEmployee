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
            return new ResponseMessage(200, "Buona serata");
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

    }
}
