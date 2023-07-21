// See https://aka.ms/new-console-template for more information

var wcfReference = new ServiceReference1.Service1Client();

Console.WriteLine(wcfReference.SayHelloAsync().);

Console.ReadLine();
