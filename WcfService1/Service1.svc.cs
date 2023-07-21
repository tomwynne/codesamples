using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;

namespace WcfService1
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the class name "Service1" in code, svc and config file together.
    // NOTE: In order to launch WCF Test Client for testing this service, please select Service1.svc or Service1.svc.cs at the Solution Explorer and start debugging.
    public class Service1 : IService1
    {
        public string SayHello()
        {
            return "Hello there.";
        }

        public string GetData(string value)
        {
            // create response that incorporates value
            return $"GetData '{value}'";
        }

        public HelloObject GetModelObject(string id)
        {
            HelloObject helloObject = new HelloObject();

            if (Int32.Parse(id) > 0)
            {
                helloObject.happyHello = true;
                helloObject.HelloMessage = "Great day.";
            }
            else
            {
                helloObject.happyHello = false;
                helloObject.HelloMessage = "Sad day.";
            }
            return helloObject;
        }
    }
}
