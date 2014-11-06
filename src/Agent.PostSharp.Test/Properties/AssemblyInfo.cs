#region Using Statements

using System.Reflection;
using System.Runtime.InteropServices;
using Gibraltar.Agent.PostSharp;

#endregion

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.

[assembly: AssemblyTitle("PostSharp Sample")]
[assembly: AssemblyDescription("Demonstrates Gibralar integration with PostSharp")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("eSymmetrix, Inc")]
[assembly: AssemblyProduct("Demo")]
[assembly: AssemblyCopyright("Copyright © 2008-2010 eSymmetrix Inc.")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// Setting ComVisible to false makes the types in this assembly not visible 
// to COM components.  If you need to access a type in this assembly from 
// COM, set the ComVisible attribute to true on that type.

[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM

[assembly: Guid("f1c11317-fb77-48ff-82dc-ab4f69828f49")]

// Version information for an assembly consists of the following four values:
//
//      Major Version
//      Minor Version 
//      Build Number
//      Revision
//
// You can specify all the values or you can default the Build and Revision Numbers 
// by using the '*' as shown below:
// [assembly: AssemblyVersion("1.0.*")]

[assembly: AssemblyVersion("3.0.0.0")]
[assembly: AssemblyFileVersion("3.0.0.0")]

// Log all exceptions thrown for every method in the current assembly
[assembly: GException]
#if DEBUG // Only log entry/exit in DEBUG builds
// Log entry and exit of every method with exceptions listed below
[assembly: GTrace(AttributePriority = -1)]
// Exclude constructors and a few excessively noisy classes & methods
[assembly: GTrace(AttributeTargetTypes = "*VerticalProgressBar",
    AttributeExclude = true)]
[assembly: GTrace(AttributeTargetMembers = ".ctor",
    AttributeExclude = true)]
[assembly: GTrace(AttributeTargetMembers = "ToString",
    AttributeExclude = true)]
[assembly: GTrace(AttributeTargetMembers = "WorkerUI_Resize",
    AttributeExclude = true)]
#endif

// Measure timing of all methods with a few exceptions listed below
[assembly: GTimer(AttributePriority = -1, Category = "Key Methods")]
// Exclude these long-running methods of no interest
[assembly: GTimer(AttributeTargetTypes = "*MainApp", AttributeTargetMembers = ".ctor",
    AttributeExclude = true)]
[assembly: GTimer(AttributeTargetMembers = "regex:(Main|WorkerThread)",
    AttributeExclude = true)]
// Exclude everything in this low-level class that's fast and called all the time
[assembly: GTimer(AttributeTargetTypes = "*VerticalProgressBar",
    AttributeExclude = true)]
