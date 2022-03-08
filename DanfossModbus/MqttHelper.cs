using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Options;
using MQTTnet.Client.Subscribing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DanfossModbus
{
    public class MqttHelper
    {
        private IMqttClient mqttClient;
        public MqttHelper()
        {
        }
        public async Task Initialize()
        {
            var options = new MqttClientOptionsBuilder()
                .WithTcpServer("192.168.1.2", 1883) // Port is optional
            .Build();
            var factory = new MqttFactory();
            mqttClient = factory.CreateMqttClient();
            mqttClient.UseConnectedHandler(async e =>
            {
                Console.WriteLine("### CONNECTED WITH SERVER ###");

                // Subscribe to a topic
             //   await mqttClient.SubscribeAsync(new MqttClientSubscribeOptionsBuilder().WithTopicFilter("#").Build());

                Console.WriteLine("### SUBSCRIBED ###");
            });
            await mqttClient.ConnectAsync(options, CancellationToken.None);

            mqttClient.UseApplicationMessageReceivedHandler(e =>
            {
                if (e.ApplicationMessage.Retain == false)
                {
                    Console.WriteLine("### RECEIVED APPLICATION MESSAGE ###");
                    Console.WriteLine($"+ Topic = {e.ApplicationMessage.Topic}");
                    Console.WriteLine($"+ Payload = {Encoding.UTF8.GetString(e.ApplicationMessage.Payload)}");
                    Console.WriteLine($"+ QoS = {e.ApplicationMessage.QualityOfServiceLevel}");
                    Console.WriteLine($"+ Retain = {e.ApplicationMessage.Retain}");
                    Console.WriteLine();
                }

//                Task.Run(() => mqttClient.PublishAsync("hello/world"));
            });
        }
        public async Task PublishMessage(string topic, string payload)
        {
            await mqttClient.PublishAsync(topic, payload);

        }
    }
}
