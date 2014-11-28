// /*
//    Copyright 2013 Gibraltar Software, Inc.
//    
//    Licensed under the Apache License, Version 2.0 (the "License");
//    you may not use this file except in compliance with the License.
//    You may obtain a copy of the License at
// 
//        http://www.apache.org/licenses/LICENSE-2.0
// 
//    Unless required by applicable law or agreed to in writing, software
//    distributed under the License is distributed on an "AS IS" BASIS,
//    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//    See the License for the specific language governing permissions and
//    limitations under the License.
// */

using System;
using System.Collections.Generic;
using Gibraltar.Agent.Metrics;

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
