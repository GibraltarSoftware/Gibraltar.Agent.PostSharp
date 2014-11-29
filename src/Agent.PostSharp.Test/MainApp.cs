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

using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;

namespace GSharp.Samples
{
    public partial class MainApp : Form
    {
        private readonly List<WorkerUI> _workerUIs = new List<WorkerUI>();

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
            _workerUIs.Add(workerUI);
            tableLayoutPanel.Controls.Add(workerUI, _workerUIs.Count, 0);

            Trace.TraceInformation("Starting worker thread " + _workerUIs.Count);
            workerUI.Start();
        }

        private void MainApp_FormClosing(object sender, FormClosingEventArgs e)
        {
            Trace.TraceInformation("Closing MainApp form.");

            foreach (WorkerUI worker in _workerUIs)
                worker.Stop();

            //MessageBox.Show(Program.TimeSpans.ToString(), "Cycle Time Summary");
            Trace.TraceInformation("Cycle Time Summary: " + Program.TimeSpans);
        }
    }
}