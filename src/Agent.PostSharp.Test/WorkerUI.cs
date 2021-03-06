﻿// /*
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
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using Gibraltar.Agent;
using Gibraltar.Agent.PostSharp;

namespace GSharp.Samples
{
    public partial class WorkerUI : UserControl
    {
        private static int _threadCount;
        private bool _keepRunning;

        [GField(InstanceNamer = "ThreadCount")]
        private Thread _workerThread;

        [GField(InstanceNamer = "ThreadCount")]
        private int _workerThreadSpeed;

        private int _workerWaitTime;
        private int _cycleCount;

        [GField(InstanceNamer = "ThreadCount")]
        private bool _nothingUseful;

        public int ThreadCount
        {
            get { return _threadCount; }
        }

        private void ComputeWaitTime()
        {
            // workerSpeed comes from m_workerThreadSpeed, which comes from trackBarSpeed, which ranges from 0 to 20
            // This results in a sleep of 10..110ms per iteration resulting in max cycle time of about 2.5 seconds
            _workerWaitTime = 10 + 5 *(20 - _workerThreadSpeed);
        }

        public WorkerUI()
        {
            _nothingUseful = true;
            _threadCount++;
            InitializeComponent();
            trackbarSpeed.Minimum = 0;
            trackbarSpeed.Maximum = 20;
            trackbarSpeed.Value = 20;
            _workerThreadSpeed = trackbarSpeed.Value;
            ComputeWaitTime();
        }

        private void trackbarSpeed_ValueChanged(object sender, EventArgs e)
        {
            _workerThreadSpeed = trackbarSpeed.Value;
            ComputeWaitTime();
        }

        private void HighlightCycleCount()
        {
            if (InvokeRequired)
            {
                Invoke(new MethodInvoker(HighlightCycleCount));
            }
            else
            {
                lblCycleCount.Text = _cycleCount.ToString();
                verticalProgressBar1.ProgressInPercentage = 0;
                verticalProgressBar2.ProgressInPercentage = 0;
                verticalProgressBar3.ProgressInPercentage = 0;
                lblCycleCount.ForeColor = Color.Red;
                BackColor = Color.LightYellow;

            }
        }

        private void UnhighlightCycleCount()
        {
            if (InvokeRequired)
            {
                Invoke(new MethodInvoker(UnhighlightCycleCount));
            }
            else
            {
                lblCycleCount.ForeColor = SystemColors.ControlText;
                BackColor = Color.White;

            }
        }

        public void Start()
        {
            _keepRunning = true;
            _workerThread = new Thread(WorkerThread) {IsBackground = true};
            _workerThread.Start();
        }

        private void WorkerThread()
        {
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();

            int progress1 = 0;
            int progress2 = 0;
            int progress3 = 0;

            while (_keepRunning)
            {
                PerformLoopIteration(stopWatch, ref progress1, ref progress2, ref progress3);
            }
        }

        [GFeature]
        private int PerformLoopIteration(Stopwatch stopWatch, ref int progress1, ref int progress2, ref int progress3)
        {
            progress1 += 20;

            if (progress1 > 100)
            {
                progress1 = 0;
                progress2 += 20;
                Trace.WriteLine("First bar has spilled over.");
            }

            if (progress2 > 100)
            {
                progress2 = 0;
                progress3 += 20;
                Trace.TraceInformation("Second bar has spilled over.");
                _nothingUseful = !_nothingUseful;
            }

            if (progress3 > 100)
            {
                progress3 = 0;
                _cycleCount++;
                stopWatch.Stop();
                Trace.TraceWarning("Third bar has spilled over. Cycle-time: "
                                   + stopWatch.Elapsed.TotalSeconds.ToString("F2"));

                HighlightCycleCount();
                Thread.Sleep(500);
                UnhighlightCycleCount();

                Program.TimeSpans.Add(stopWatch.Elapsed);
                stopWatch.Reset();
                stopWatch.Start();
                DemonstrateExceptionRecording();
            }

            verticalProgressBar1.ProgressInPercentage = progress1;
            verticalProgressBar2.ProgressInPercentage = progress2;
            verticalProgressBar3.ProgressInPercentage = progress3;

            Thread.Sleep(_workerWaitTime);
            return progress1;
        }

        public void Stop()
        {
            _keepRunning = false; // Stop worker thread
            if (_workerThread != null)
                _workerThread.Join(500);
        }

        private void WorkerUI_Resize(object sender, EventArgs e)
        {
            Width = 140;
        }

        private void DemonstrateExceptionRecording()
        {
            try
            {
                InterveningMethod();
            }
            catch (Exception ex)
            {
                Log.RecordException(ex, "PostSharp.Explicit", true);
            }
        }

        [GFeature]
        private void InterveningMethod()
        {
            //our only purpose is to annoy the call stack and put one step
            //between us and the fatal method.  We should both report the failure.
            FatalMethod();
        }

        private void FatalMethod()
        {
            //do something that causes someone else to throw an exception.
            string tempFileNamePath = Path.GetTempFileName();
            using(new FileStream(tempFileNamePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
            {
                //now try to delete that bad boy while we have a lock.
                File.Delete(tempFileNamePath);
            }
        }
    }
}
