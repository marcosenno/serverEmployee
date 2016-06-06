using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RESTService.Lib
{

    public class EmployeeBadge
    {
        public string rfid { get; set; }
        public string session { get; set; }
        public Boolean faceRecognition { get; set; }
    }
}
