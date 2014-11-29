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
using System.Windows.Forms;
using Gibraltar.Agent;

namespace GSharp.Samples
{
    /// <summary>
    /// A sample application using dynamic binding to Gibraltar.Agent.dll via app.config.
    /// </summary>
    /// <remarks><para>
    /// This example is compiled without direct reference to Gibraltar, only adding Gibraltar's TraceListener
    /// (Gibraltar.Agent.LogListener) in the trace section of the system.diagnostics group in the app.config file to
    /// connect the Agent through the use of built-in Trace logging.  This allows the Gibraltar Agent to be attached to
    /// an application which is already using Trace or other logging systems without recompiling your application.
    /// </para>
    /// <para>
    /// This example shows a typical WinForms Program.Main() with a few recommended calls to make sure the
    /// Gibraltar Agent can perform at its best.
    /// </para>
    /// <para>
    /// Also see the static binding sample application as an alternative approach showing some of the features of the
    /// Gibraltar API.</para></remarks>
    internal static class Program
    {
        public static readonly TimeSpanCollection TimeSpans = new TimeSpanCollection();

        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            Log.StartSession(); //lets fire up logging.  This ensures Gibraltar fully initializes all of its features before we continue.
            Application.Run(new MainApp());
            Log.EndSession(); //tell Gibraltar we're ending normally.  It will now make sure we can quickly exit.
        }
    }
}