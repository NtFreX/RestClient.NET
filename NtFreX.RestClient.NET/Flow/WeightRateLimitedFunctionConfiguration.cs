using System;
using System.Collections.Generic;
using NtFreX.RestClient.NET.Extensions;

namespace NtFreX.RestClient.NET.Flow
{
    public class WeightRateLimitedFunctionConfiguration
    {
        public int TotalWeightPerMinute { get; set; }

        private readonly Dictionary<long /* timeInSeconds */, int /* usedWeight*/> _usedWeight = new Dictionary<long, int>();

        public WeightRateLimitedFunctionConfiguration(int totalWeightPerMinute)
        {
            TotalWeightPerMinute = totalWeightPerMinute;
        }

        public int GetLeftWeight()
        {
            var now = DateTime.Now.ToUnixTimeSeconds();
            var currentTime = now;
            var usedRate = 0;
            while (currentTime > now - 60)
            {
                if (_usedWeight.ContainsKey(currentTime))
                    usedRate += _usedWeight[currentTime];

                currentTime -= 1;
            }
            var leftWeight = TotalWeightPerMinute - usedRate;
            return leftWeight < 0 ? 0 : leftWeight;
        }

        public void AddUsedWeight(int weight)
        {
            var timeInSeconds = DateTime.Now.ToUnixTimeSeconds();
            if(_usedWeight.ContainsKey(timeInSeconds))
                _usedWeight[timeInSeconds] += weight;
            else
                _usedWeight.Add(timeInSeconds, weight);
        }
    }
}