using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CalculatorClient
{
    internal class Program
    {
        static void Main(string[] args)
        {
            ServiceReference1.Service1Client client = new ServiceReference1.Service1Client();

            Console.WriteLine("Add " + client.Add(1, 2));
            Console.WriteLine("Subtract " + client.Subtract(1983, 28));
            Console.WriteLine("Multiply " + client.Multiply(65, 40));
            Console.WriteLine("Divide " + client.Divide(365 * 24, 65 * 8));

            Console.ReadLine();

        }
    }
}
