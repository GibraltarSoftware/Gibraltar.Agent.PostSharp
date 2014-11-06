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
using System.Windows.Forms;
using Gibraltar.Agent;

#endregion

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
    /// This example shows a typical winforms Program.Main() with a few recommended calls to make sure the
    /// Gibraltar Agent can perform at its best.
    /// </para>
    /// <para>
    /// Also see the static binding sample application as an alternative approach showing some of the features of the
    /// Gibraltar API.</para></remarks>
    internal static class Program
    {
        public static TimeSpanCollection TimeSpans = new TimeSpanCollection();

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