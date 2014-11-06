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
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using Gibraltar.Agent;
using Gibraltar.Agent.PostSharp;

#endregion

namespace GSharp.Samples
{
    public partial class WorkerUI : UserControl
    {
        private static int m_ThreadCount;
        private bool m_KeepRunning;

        [GField(InstanceNamer = "ThreadCount")]
        private Thread m_WorkerThread;

        [GField(InstanceNamer = "ThreadCount")]
        private int m_WorkerThreadSpeed;

        private int m_WorkerWaitTime;
        private int m_cycleCount;

        [GField(InstanceNamer = "ThreadCount")]
        private bool m_NothingUseful;

        public int ThreadCount
        {
            get { return m_ThreadCount; }
        }

        private void ComputeWaitTime()
        {
            // workerSpeed comes from m_workerThreadSpeed, which comes from trackBarSpeed, which ranges from 0 to 20
            // This results in a sleep of 10..110ms per iteration resulting in max cylce time of about 2.5 seconds
            m_WorkerWaitTime = 10 + 5 *(20 - m_WorkerThreadSpeed);
        }

        public WorkerUI()
        {
            m_NothingUseful = true;
            m_ThreadCount++;
            InitializeComponent();
            trackbarSpeed.Minimum = 0;
            trackbarSpeed.Maximum = 20;
            trackbarSpeed.Value = 20;
            m_WorkerThreadSpeed = trackbarSpeed.Value;
            ComputeWaitTime();
        }

        private void trackbarSpeed_ValueChanged(object sender, EventArgs e)
        {
            m_WorkerThreadSpeed = trackbarSpeed.Value;
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
                lblCycleCount.Text = m_cycleCount.ToString();
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
            m_KeepRunning = true;
            m_WorkerThread = new Thread(WorkerThread) {IsBackground = true};
            m_WorkerThread.Start();
        }

        private void WorkerThread()
        {
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();

            int progress1 = 0;
            int progress2 = 0;
            int progress3 = 0;

            while (m_KeepRunning)
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
                m_NothingUseful = !m_NothingUseful;
            }

            if (progress3 > 100)
            {
                progress3 = 0;
                m_cycleCount++;
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

            Thread.Sleep(m_WorkerWaitTime);
            return progress1;
        }

        public void Stop()
        {
            m_KeepRunning = false; // Stop worker thread
            if (m_WorkerThread != null)
                m_WorkerThread.Join(500);
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
