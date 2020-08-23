using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Client.Models;

namespace Client
{
    internal class Program
    {
        static void Main()
        {
            Console.WriteLine("Client started.");

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

            var values = new BlockingCollection<int>();
            var calculator = new StatisticCalculator();
            using var producer = new Producer(values, options);
            var tasks = new List<Task>
            {
                producer.ProduceMessagesAsync(),
                Task.Run(() => WaitKey(calculator, producer))
            };

            for (var i = 0; i < 2; i++)
            {
                tasks.Add(new Consumer(values, calculator).ConsumeMessagesAsync());
            }

            Task.WaitAll(tasks.ToArray());
        }

        private static void WaitKey(StatisticCalculator calculator, Producer producer)
        {
            while (true)
            {
                if (Console.ReadKey().Key == ConsoleKey.Enter)
                {
                    ShowValues(calculator, producer.LossPackagesCount);
                }
            }
        }

        private static void ShowValues(StatisticCalculator calculator, int lossPackagesCount)
        {
            Console.WriteLine();
#if DEBUG
            Console.WriteLine($"Count: \t\t\t{calculator.Count}");
            Console.WriteLine($"Loss packages count: \t{lossPackagesCount}");
#endif
            Console.WriteLine($"Average: \t\t{calculator.Average}");
            Console.WriteLine($"Standard deviation: \t{calculator.Deviation}");

            if (calculator.TryCalculateModeAndMedian(out long mode, out long median))
            {
                Console.WriteLine($"Mode: \t\t\t{mode}");
                Console.WriteLine($"Median: \t\t{median}");
            }
        }
    }
}
