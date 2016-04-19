using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;
using System.ServiceModel.Web;


namespace RESTService.Lib
{
    [ServiceContract(Name = "RESTDemoServices")]
    public interface IRESTDemoServices
    {
        [OperationContract]
        [WebGet(UriTemplate = Routing.GetClientRoute, BodyStyle = WebMessageBodyStyle.Bare)]
        string GetClientNameById(string Id);
        [OperationContract]
        [WebGet(UriTemplate = Routing.HelloRoute, BodyStyle = WebMessageBodyStyle.Bare)]
        string Hello();
        [OperationContract]
        [WebInvoke(Method="GET",ResponseFormat=WebMessageFormat.Json,BodyStyle=WebMessageBodyStyle.Bare,UriTemplate="getData/{value}")]
        string GetData(string value);
        



    }
}
