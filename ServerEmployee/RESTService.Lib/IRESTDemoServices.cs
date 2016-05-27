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
        [WebInvoke(Method = "POST", ResponseFormat = WebMessageFormat.Json, RequestFormat = WebMessageFormat.Json, UriTemplate = "exit")]
        ResponseMessage exitBadge(EmployeeBadge e);

        [OperationContract]
        [WebInvoke(Method = "GET", ResponseFormat = WebMessageFormat.Json, UriTemplate = "test")]
        ResponseMessage test();

        [OperationContract]
        [WebInvoke(Method = "GET", ResponseFormat = WebMessageFormat.Json, UriTemplate = "employees")]
        Employee[] getEmployees();

        [OperationContract]
        [WebInvoke(Method = "GET", ResponseFormat = WebMessageFormat.Json, UriTemplate = "employees/{rfid}")]
        Employee getEmployee(string rfid);

        [OperationContract]
        [WebInvoke(Method = "POST", ResponseFormat = WebMessageFormat.Json, RequestFormat = WebMessageFormat.Json, UriTemplate = "employees/{rfid}")]
        ResponseMessage addEmployee(string rfid, EmployeePic employee);

        [OperationContract]
        [WebInvoke(Method = "PUT", ResponseFormat = WebMessageFormat.Json, RequestFormat = WebMessageFormat.Json, UriTemplate = "employees/{rfid}")]
        ResponseMessage editEmployee(string rfid, Employee employee);


         

        [OperationContract]
        [WebInvoke(Method = "OPTIONS", ResponseFormat = WebMessageFormat.Json, RequestFormat = WebMessageFormat.Json, UriTemplate = "employees/{rfid}")]
        ResponseMessage editEmployeeOptions(string rfid, Employee employee);

        [OperationContract]
        [WebInvoke(Method = "DELETE", ResponseFormat = WebMessageFormat.Json, RequestFormat = WebMessageFormat.Json, UriTemplate = "employees/{rfid}")]
        ResponseMessage deleteEmployee(string rfid);



    }
}
