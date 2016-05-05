using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RESTService.Lib
{
    public class ResponseMessage
    {
        public int code { get; set; }
        public string message { get; set; }
        public ResponseMessage() {}
        public ResponseMessage(int code, string message) 
        { 
            this.code = code; 
            this.message = message; 
        }

    }
}
