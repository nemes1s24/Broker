using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Client.Models;

namespace Client
{
    internal class StatisticCalculator
    {
        private const int MinimumStatisticCount = 1000;

        private static float _average;
        private static long _sum;
        private static float _squareAverage;
        private static long _squareSum;
        private static float _deviation;
        private static long _count;
        private static bool _intervalsInitialized;
        private static List<int> _initializationStatistic = new List<int>(MinimumStatisticCount);
        private static Interval[] _intervals;

        private int _modeAndMedianCalculating;
        private readonly object _lockedObj = new object();
        private readonly ManualResetEvent _resetEvent = new ManualResetEvent(true);

        public float Average => _average;
        public float Deviation => _deviation;

#if DEBUG
        public float Count => _count;
#endif

        public void Recalculate(int value)
        {
            if (_modeAndMedianCalculating > 0)
            {
                _resetEvent.WaitOne();
            }

            Interlocked.Add(ref _sum, value);
            long square = value * value;
            Interlocked.Add(ref _squareSum, square);

            lock (_lockedObj)
            {
                _count++;
                RecalculateAverage(value, square);
                RecalculateDeviation();
            }

            RecalculateIntervals(value);
        }

        public bool TryCalculateModeAndMedian(out long mode, out long median)
        {
            if (!_intervalsInitialized)
            {
                mode = -1;
                median = -1;

                return false;
            }

            Interlocked.Increment(ref _modeAndMedianCalculating);
            _resetEvent.Reset();

            var mostFrequentInterval = _intervals.First(interval => interval.Count == _intervals.Max(comparedInterval => comparedInterval.Count));
            int index = Array.IndexOf(_intervals, mostFrequentInterval);

            if (index == 0 || index == 9)
            {
                throw new InvalidOperationException("Modal intervals are invalid.");
            }

            mode = mostFrequentInterval.Min + (mostFrequentInterval.Max - mostFrequentInterval.Min) * (mostFrequentInterval.Count - _intervals[index - 1].Count)
                / (mostFrequentInterval.Count * 2 - _intervals[index - 1].Count - _intervals[index + 1].Count);
            long previousIntervalsCount = 0;
            for (var i = 0; i < index; i++)
            {
                previousIntervalsCount += _intervals[i].Count;
            }

            median = mostFrequentInterval.Min + (mostFrequentInterval.Max - mostFrequentInterval.Min) * (_count / 2 - previousIntervalsCount) / mostFrequentInterval.Count;

            Interlocked.Decrement(ref _modeAndMedianCalculating);
            _resetEvent.Set();
            return true;
        }

        private void RecalculateAverage(int value, long square)
        {
            if (Math.Abs(_average - value) > 0)
            {
                _average += (value - _average) / _count;
            }

            if (Math.Abs(_squareAverage - square) > 0)
            {
                _squareAverage += (square - _squareAverage) / _count;
            }
        }

        private void RecalculateDeviation()
        {
            if (_count < 2)
            {
                return;
            }

            _deviation = (_squareSum - _sum * _sum / _count) / (_count - (float) 1);
        }

        private void RecalculateIntervals(int newValue)
        {
            void AddValue(int value)
            {
                _intervals.Single(x => value >= x.Min && value <= x.Max).IncrementCount();
            }
            
            if (!_intervalsInitialized)
            {
                lock (_lockedObj)
                {
                    if (!_intervalsInitialized)
                    {
                        _initializationStatistic.Add(newValue);

                        if (_count == MinimumStatisticCount)
                        {
                            int minValue = _initializationStatistic.Min();
                            var intervalLength = (int)Math.Round((_initializationStatistic.Max() - minValue) / 8m);

                            _intervals = new Interval[10];
                            _intervals[0] = new Interval(0, minValue - 1);
                            for (var i = 1; i < 9; i++)
                            {
                                _intervals[i] = new Interval(minValue + intervalLength * (i - 1), minValue + intervalLength * i - 1);
                            }
                            _intervals[9] = new Interval(_intervals[8].Max + 1, int.MaxValue);

                            foreach (int statisticValue in _initializationStatistic)
                            {
                                AddValue(statisticValue);
                            }

                            _initializationStatistic = null;
                            _intervalsInitialized = true;
                        }

                        return;
                    }
                }
            }

            AddValue(newValue);
        }
    }
}
