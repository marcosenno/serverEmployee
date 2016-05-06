using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RESTService.Lib;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.ServiceModel.Web;
namespace RESTService.Host
{
    class Program
    {
        static void Main(string[] args)
        {
            RestDemoServices DemoServices = new RestDemoServices();
            WebHttpBinding binding = new WebHttpBinding();
            WebHttpBehavior behavior = new WebHttpBehavior();

            WebServiceHost _serviceHost = new WebServiceHost(DemoServices, new Uri("http://localhost:8008/DEMOService"));
            _serviceHost.AddServiceEndpoint(typeof(IRESTDemoServices), binding, "");
            _serviceHost.Open();
            Console.ReadKey();
            _serviceHost.Close();
        }
    }
}
