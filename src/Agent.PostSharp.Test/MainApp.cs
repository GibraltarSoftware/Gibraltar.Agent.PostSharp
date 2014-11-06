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

using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;

#endregion

namespace GSharp.Samples
{
    public partial class MainApp : Form
    {
        private readonly List<WorkerUI> workerUIs = new List<WorkerUI>();

        public MainApp()
        {
            Trace.WriteLine("Initializing MainApp form.");
            InitializeComponent();
            AddWorkers(4);
        }

        private void AddWorkers(int count)
        {
            for (int i = 0; i < count; i++)
                AddWorker();

            Width = tableLayoutPanel.Width + 16; // add a little margin
        }

        private void AddWorker()
        {
            Trace.WriteLine("Adding a new worker thread");

            WorkerUI workerUI = new WorkerUI();
            workerUIs.Add(workerUI);
            tableLayoutPanel.Controls.Add(workerUI, workerUIs.Count, 0);

            Trace.TraceInformation("Starting worker thread " + workerUIs.Count);
            workerUI.Start();
        }

        private void MainApp_FormClosing(object sender, FormClosingEventArgs e)
        {
            Trace.TraceInformation("Closing MainApp form.");

            foreach (WorkerUI worker in workerUIs)
                worker.Stop();

            //MessageBox.Show(Program.TimeSpans.ToString(), "Cycle Time Summary");
            Trace.TraceInformation("Cycle Time Summary: " + Program.TimeSpans);
        }
    }
}