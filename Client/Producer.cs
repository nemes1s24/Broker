using System;
using System.Collections.Concurrent;
using System.Configuration;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Client.Models;
using ProtoBuf;

namespace Client
{
    internal class Producer : IDisposable
    {
        private readonly BlockingCollection<int> _cache;
        private readonly Options _options;
        private readonly Timer _timer;
        private readonly ManualResetEvent _resetEvent;
        private int _lossPackagesCount;
        private int _hold;

        public Producer(BlockingCollection<int> cache, Options options)
        {
            _cache = cache;
            _options = options;
            _lossPackagesCount = -1;
            _resetEvent = new ManualResetEvent(true);
            _timer = new Timer(_ => Hold(), null, 1000, 1000);
        }

        public int LossPackagesCount => _lossPackagesCount;

        public async Task ProduceMessagesAsync()
        {
            try
            {
                await Task.Run(ProduceMessages);
            }
            catch (Exception ex)
            {
                ExceptionHandler.Handle(ex);
            }
        }
        
        private void ProduceMessages()
        {
            using var udpClient = new UdpClient(_options.Port, AddressFamily.InterNetwork);
#if DEBUG
            Task.Run(() =>
            {
                while (true)
                {
                    Console.WriteLine($"Available packages on {DateTime.Now:T}: {udpClient.Available}");
                    Thread.Sleep(1000);
                }
            });
#endif

            var address = IPAddress.Parse(_options.IpAddress);
            udpClient.JoinMulticastGroup(address);
            var previousMessageNumber = long.MinValue;

            while (true)
            {
                if (_hold > 0)
                {
                    _resetEvent.WaitOne();
                }

                try
                {
                    IPEndPoint remoteIp = null;
                    byte[] data = udpClient.Receive(ref remoteIp);
                    using var stream = new MemoryStream(data);
                    var message = Serializer.Deserialize<Message>(stream);
                    _cache.Add(message.Value);

                    if (previousMessageNumber + 1 != message.SequenceNumber)
                    {
                        Interlocked.Increment(ref _lossPackagesCount);
                    }
                    previousMessageNumber = message.SequenceNumber;
                }
                catch (Exception e)
                {
                    ExceptionHandler.Handle(e);
                }
            }
        }

        private void Hold()
        {
            Interlocked.Increment(ref _hold);
            _resetEvent.Reset();
            Thread.Sleep(_options.HoldingDuration);
            Interlocked.Decrement(ref _hold);
            _resetEvent.Set();
        }

        void IDisposable.Dispose()
        {
            _timer.Dispose();
        }
    }
}
