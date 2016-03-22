﻿namespace MetricsDefinition.Metrics
{
    [Metric("LO_PERIOD")]
    public sealed class LowestPeriod : SingleOutputRawInputSerialMetric
    {
        public LowestPeriod(int windowSize)
            : base(windowSize)
        {
        }

        public override void Update(double dataPoint)
        {
            Data.Add(dataPoint);

            int period = 1;
            for (int i = -1; i > -Data.Length; --i)
            {
                if (Data[i] < dataPoint)
                {
                    break;
                }

                ++period;
            }

            SetValue(period);
        }
    }
}