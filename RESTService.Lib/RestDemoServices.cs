using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;
using System.ServiceModel.Activation;
using Newtonsoft.Json; 


namespace RESTService.Lib
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single, ConcurrencyMode = ConcurrencyMode.Single, IncludeExceptionDetailInFaults = true)]
    [AspNetCompatibilityRequirements(RequirementsMode = AspNetCompatibilityRequirementsMode.Allowed)]
    public class RestDemoServices:IRESTDemoServices
    {
        public string GetClientNameById(string Id)
        {
            Random r = new Random();
            string ReturnString="";
            for (int i = 0; i < Convert.ToUInt32(Id); i++)
                ReturnString += char.ConvertFromUtf32(r.Next(65, 85));

            return "ciao";

        }

        public string Hello() { return "Hellooooo"; }

        public string GetData(string value) {
            
            string final = JsonConvert.SerializeObject(new Data("ciao"));        
            return final; } 
    }
}
