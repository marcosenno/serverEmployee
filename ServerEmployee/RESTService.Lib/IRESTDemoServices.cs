using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.IO;



namespace RESTService.Lib
{
    [ServiceContract(Name = "RESTDemoServices")]
    public interface IRESTDemoServices
    {

        [OperationContract]
        [WebInvoke(Method = "POST", ResponseFormat = WebMessageFormat.Json, RequestFormat = WebMessageFormat.Json, UriTemplate = "enter")]
        ResponseMessage enterBadge(EmployeeBadge e); 
        
        [OperationContract]
        [WebInvoke(Method = "GET", ResponseFormat = WebMessageFormat.Json, UriTemplate = "prova")]
        string prova();
        
        [OperationContract]
        [WebInvoke(Method = "POST", ResponseFormat = WebMessageFormat.Json, RequestFormat = WebMessageFormat.Json, UriTemplate = "exit")]
        ResponseMessage exitBadge(EmployeeBadge e);

        [OperationContract]
        [WebInvoke(Method = "POST", ResponseFormat = WebMessageFormat.Json, RequestFormat = WebMessageFormat.Json, UriTemplate = "blabla")]
        ResponseMessage blabla(string e);
        




    }
}
