using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Xml.Serialization;
using ProtoBuf;
using Server.Models;

namespace Server
{
    internal class Program
    {
        private static void Main()
        {
            Console.WriteLine("Server started.");

            const string fileName = "Settings.xml";
            Options options;
            try
            {
                using var fileStream = File.Open(fileName, FileMode.Open);
                var serializer = new XmlSerializer(typeof(Options));
                options = (Options)serializer.Deserialize(fileStream);

            }
            catch (FileNotFoundException)
            {
                Console.WriteLine($"{fileName} not found.");
                return;
            }

            var random = new Random();
            const int mode = 35000;
            const int maxValue = 100000;

            using var udpClient = new UdpClient(AddressFamily.InterNetwork);
            var address = IPAddress.Parse(options.IpAddress);
            var ipEndPoint = new IPEndPoint(address, options.Port);
            udpClient.JoinMulticastGroup(address);

            long i = 0;
            while (true)
            {
                i++;
                try
                {
                    int value = random.Next(random.Next(mode - random.Next(1, mode)), random.Next(mode, mode + random.Next(1, maxValue)));
                    var message = new Message(i, value);
                    using var stream = new MemoryStream();
                    Serializer.Serialize(stream, message);
                    udpClient.Send(stream.ToArray(), (int)stream.Length, ipEndPoint);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
        }
    }
}
