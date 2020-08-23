using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace Client
{
    internal class Consumer
    {
        private readonly BlockingCollection<int> _cache;
        private readonly StatisticCalculator _calculator;

        public Consumer(BlockingCollection<int> cache, StatisticCalculator calculator)
        {
            _cache = cache;
            _calculator = calculator;
        }

        public async Task ConsumeMessagesAsync()
        {
            try
            {
                await Task.Run(ConsumeMessages);
            }
            catch (Exception ex)
            {
                ExceptionHandler.Handle(ex);
            }
        }

        private void ConsumeMessages()
        {
            foreach (int value in _cache.GetConsumingEnumerable())
            {
                try
                {
                    _calculator.Recalculate(value);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
        }
    }
}
