using System.Runtime.Serialization;

namespace WcfService1
{
    public class HelloObject
    {
        [DataMember]
        public bool happyHello { get; set; } = false;

        [DataMember]
        public string HelloMessage { get; set; } = "Hello";
    }
}