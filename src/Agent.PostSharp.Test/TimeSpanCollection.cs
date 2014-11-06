#region Released to Public Domain by eSymmetrix, Inc.

/********************************************************************************
 *   This file is sample code demonstrating Gibraltar integration with PostSharp
 *   
 *   This sample is free software: you have an unlimited rights to
 *   redistribute it and/or modify it.
 *   
 *   This sample is distributed in the hope that it will be useful,
 *   but WITHOUT ANY WARRANTY; without even the implied warranty of
 *   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
 *   
 *******************************************************************************/

using System;
using System.Collections.Generic;
using Gibraltar.Agent.Metrics;

#endregion

namespace GSharp.Samples
{
    public class TimeSpanCollection : List<TimeSpan>
    {
        public TimeSpan Min()
        {
            if (Count <= 0)
                return TimeSpan.Zero;

            TimeSpan min = TimeSpan.MaxValue;
            foreach (TimeSpan timeSpan in this)
                if (timeSpan < min)
                    min = timeSpan;

            return min;
        }

        public TimeSpan Max()
        {
            if (Count <= 0)
                return TimeSpan.Zero;

            TimeSpan max = TimeSpan.MinValue;
            foreach (TimeSpan timeSpan in this)
                if (timeSpan > max)
                    max = timeSpan;

            return max;
        }

        public TimeSpan Average()
        {
            if (Count <= 0)
                return TimeSpan.Zero;

            TimeSpan total = TimeSpan.Zero;
            foreach (TimeSpan timeSpan in this)
                total += timeSpan;

            var average = new TimeSpan(total.Ticks / Count);
            return average;
        }

        public override string ToString()
        {
            return string.Format("{0} Cycles\nAverage Cycle Time     =\t{1:f2} seconds\nMinimum Cycle Time  =\t{2:f2} seconds\nMaximum Cycle Time  =\t{3:f2} seconds",
                Count, Average().TotalSeconds, Min().TotalSeconds, Max().TotalSeconds);
        }

        public new void Add(TimeSpan timeSpan)
        {
            // Let's record a custom metric to track cycle-time per thread
            EventMetric.Write(new TimeSpanMetric(timeSpan));
            base.Add(timeSpan);
        }

        /// <summary>
        /// This helper class demonstrates how easy it is to collect custom metrics with Gibraltar
        /// </summary>
        [EventMetric("PostSharpSample", "PostSharp", "Cycle Time")]
        private class TimeSpanMetric
        {
            private readonly TimeSpan _timeSpan;
            private readonly int _thread;

            public TimeSpanMetric(TimeSpan timeSpan)
            {
                _timeSpan = timeSpan;
                _thread = System.Threading.Thread.CurrentThread.ManagedThreadId;
            }

            [EventMetricValue("Duration", SummaryFunction.Average, "seconds")]
            public double Duration
            {
                get { return _timeSpan.TotalSeconds; }
            }

            [EventMetricValue("Thread", SummaryFunction.Count, null)]
            public string Thread
            {
                get { return "Thread " + _thread; }
            }
        }
    }
}
