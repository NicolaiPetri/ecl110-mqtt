using System;
using System.Threading;
using Newtonsoft.Json;


namespace DanfossModbus
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");

//            if (false) {
                var mqtt = new MqttHelper();
                mqtt.Initialize().Wait();

//                mqtt.DoStuff().Wait();
//                Console.ReadKey();
//                mqtt.SendDummy().Wait();
//            }

            var app = new DanfossManager();
            string mqttTopic = "heating/danfoss/status";

            while (true) {
                var state = app.ReadState();
                string output = JsonConvert.SerializeObject(state);
                mqtt.PublishMessage(mqttTopic, output).Wait();
                Console.WriteLine(output);
                Thread.Sleep(30000);
            }
        }
    }
}
