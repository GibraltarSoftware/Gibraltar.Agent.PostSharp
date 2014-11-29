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
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using PostSharp.Aspects;
#if PostSharpLicensed
using PostSharp.Aspects.Dependencies;
#endif

namespace Gibraltar.Agent.PostSharp
{
    /// <summary>
    /// A PostSharp aspect used to trace entry and successful exit from methods. It also
    /// measures method execution time.
    /// </summary>
    /// <remarks>
    /// 	<para>GTrace is a PostSharp aspect used to trace entry and exit from methods. It
    ///     writes a message to the log every time an associated method is entered and when it
    ///     returns. Additionally:</para>
    /// 	<list type="bullet">
    /// 		<item>The input parameters to the method are recorded for the entering
    ///         message</item>
    /// 		<item>The execution time of the method is recorded in the exiting
    ///         message.</item>
    /// 		<item>The return value (if any) is recorded in the exiting message</item>
    /// 		<item>If the method is exiting because an exception is being thrown out of the
    ///         method that is noted and the exception details are recorded.</item>
    /// 		<item>The log messages are attributed to the source code location that invoked
    ///         the method, if source code location information is available.</item>
    /// 	</list>
    /// 	<para><strong>Log Message Categorization</strong></para>
    /// 	<para>
    ///         Each log message is recorded in an automatically generated category in the form
    ///         {BaseCategory}.Trace.Enter or {BaseCategory}.Trace.Exit. See the <see cref="GAspectBase.BaseCategory">BaseCategory</see> property for more information on
    ///         changing the log message category.
    ///     </para>
    /// 	<para><strong>Enabling and Disabling at Runtime</strong></para>
    /// 	<para>
    ///         Because GTrace messages are voluminous and detailed it's often useful to enable
    ///         and disable them at runtime. To do this just set the <see cref="Enabled">Enabled</see> property which will affect all methods instrumented
    ///         with GTrace in the current process. When disabled GTrace has minimal
    ///         performance impact even in high volume scenarios.
    ///     </para>
    /// 	<para><strong>Refining Data Captured</strong></para>
    /// 	<para>
    ///         You can turn down the amount of information captured for a given method by
    ///         GTrace by setting the <see cref="LogParameters">LogParameters</see>, <see cref="LogReturnValue">LogReturnValue</see>, and <see cref="EnableSourceLookup">EnableSourceLookup</see> properties. These are
    ///         particularly handy if you know that the parameter information is very large and
    ///         not generally interesting.
    ///     </para>
    /// </remarks>
    /// <summary>
    /// 	<para>Enables the logging of method entry and exit at runtime complete with
    ///     parameter information and results.</para>
    /// </summary>
    /// <example>
    ///     You can associate the GTrace attribute with a single method, a property, or an
    ///     entire class to trace all methods in that class. You can also use attribute
    ///     multicasting to apply it to all matching methods in your assembly.
    ///     <code lang="CS" title="Trace a Single Method" description="Trace one method within a class">
    /// public class SampleApplication
    /// {
    ///     [GTrace]
    ///     private void InterestingMethod(int valueOne, String anotherValue)
    ///     {
    ///         //Do something interesting here
    ///     }
    /// }
    /// </code>
    /// 	<code lang="CS" title="Trace all methods within a class" description="Trace all methods within a class">
    /// [GTrace]
    /// public class SampleApplication
    /// {
    ///     private void InterestingMethod(int valueOne, String anotherValue)
    ///     {
    ///         //Do something interesting here
    ///     }
    ///  
    ///     private void AnotherInterestingMethod(int valueTwo, String anotherValue)
    ///     {
    ///         //Do something even more interesting here
    ///     }
    /// }
    /// </code>
    /// </example>
    [DebuggerNonUserCode]
    [Serializable]
#if PostSharpLicensed
    [ProvideAspectRole(StandardRoles.Tracing)]
    [AspectTypeDependency(AspectDependencyAction.Order, AspectDependencyPosition.After, typeof(GTimer))]
#endif
    public sealed class GTrace : GAspectBase
    {
        private const string LogSystem = "Gibraltar Trace Aspect";
        private const string EnterSubCategory = "Trace.Enter";
        private const string ExitSubCategory = "Trace.Exit";

        private static bool s_Enabled = true;

        // Internal variables calculated at compile-time for run-time efficiency
        private bool _logParameters;  // true if method has parameters AND LogParameters == true
        private bool _logParameterDetails;  // true if LogParameterDetails == true
        private bool _logReturnValue; // true if method has return value AND LogReturnValues == true
        private string _parameterListFormat;

        // Internal variables only used at run-time
        [NonSerialized]
        private bool _tracingEnabled;

        #region Private Class TraceMethodState

        /// <summary>
        /// Carries information about a single method invocation
        /// </summary>
        private class TraceMethodState
        {
            public TraceMethodState(IMessageSourceProvider sourceProvider)
            {
                SourceProvider = sourceProvider;
                Timer = Stopwatch.StartNew();
            }

            public Stopwatch Timer { get; private set; }
            public IMessageSourceProvider SourceProvider { get; set; }
        }

        #endregion

        /// <summary>
        /// Default constructor
        /// </summary>
        public GTrace()
        {
            LogReturnValue = true;
            LogParameters = true;
            LogParameterDetails = true;
            EnableSourceLookup = true;
        }

        /// <summary>
        /// Enables or disables tracing for all methods tagged with GTrace in the current
        /// process. Enabled by default.
        /// </summary>
        /// <remarks>
        /// This property can be directly changed at runtime to affect all of the methods
        /// tagged with the GTrace attribute without recompilation.
        /// </remarks>
        public static bool Enabled { get { return s_Enabled; } set { s_Enabled = value; } }

        /// <summary>
        /// Enables or disables recording of parameters and their values. Enabled by default.
        /// </summary>
        /// <remarks>
        /// 	<para>When enabled GTrace will record the parameter values on entry to the method
        ///     being monitored.  Each parameter will be recorded as its ToString() representation if
        ///     overridden (and for struct and enum types).  If ToString() is not overridden in a class
        ///     type (or if <see cref="LogParameterDetails">LogParameterDetails</see> is disabled)
        ///     the base implementation will be replaced with the type's full name and the hash code
        ///     for that instance.  Some implementations of ToString() can produce very long strings
        ///     and can result in very large messages for certain objects if such details are also
        ///     enabled (which is the default).</para>
        /// </remarks>
        public bool LogParameters { get; set; }

        /// <summary>
        /// Enables or disables recording of object detail for parameters via ToString. Enabled by default.
        /// </summary>
        /// <remarks>
        /// 	<para>When enabled the parameter values being recorded upon entry to a method being monitored
        ///     (with <see cref="LogParameters">LogParameters</see> enabled) and return values being recorded
        ///     upon exit (with <see cref="LogReturnValue">LogReturnValue</see>) will include object detail
        ///     by converting class instances via their ToString method.  This can produce significant
        ///     overhead depending on the ToString implementation for those passed parameters.</para>
        ///     <para>When disabled the parameter values for most class instances will be recorded by their
        ///     type and hash code to avoid calling ToString() and thus keep overhead to a minimum.  Value types
        ///     (structs) will still be converted via ToString, and string objects will be recorded directly.</para>
        /// </remarks>
        public bool LogParameterDetails { get; set; }

        /// <summary>
        /// Enables or disables recording of the method return value (if declared). Enabled by default.
        /// </summary>
        /// <remarks>
        /// 	<para>When enabled GTrace will record the return value on method exit.
        ///     The return value  will be recorded as its ToString() representation if
        ///     overridden (and for struct and enum types).  If ToString() is not overridden in a class
        ///     type (or if <see cref="LogParameterDetails">LogParameterDetails</see> is disabled)
        ///     the base implementation will be replaced with the type's full name and the hash code
        ///     for that instance.  Some implementations of ToString() can produce very long strings
        ///     and can result in very large messages for certain objects if such details are also
        ///     enabled (which is the default).</para>
        /// </remarks>
        public bool LogReturnValue { get; set; }

        /// <summary>Enables lookup of source line and method. Enabled by default.</summary>
        /// <remarks>
        /// 	<para>If enabled GTrace will attempt to identify the class, method, file and line
        ///     number of the code that called the target method. Some or all of this information
        ///     may not be available depending on how the method was compiled and whether the
        ///     library symbol file is present on the local computer.</para>
        /// 	<para>In certain high performance scenarios disabling the source code lookup can
        ///     significantly improve performance. If you find that using GTrace is causing a
        ///     performance issue in your application you may experiment with selectively disabling
        ///     source code lookup on methods that are called extremely frequently.</para>
        /// </remarks>
        public bool EnableSourceLookup { get; set; }

        /// <summary>
        /// PostSharp Infrastructure. This method pre-computes as much as possible at compile
        /// time to minimize runtime processing requirements.
        /// </summary>
        public override void CompileTimeInitialize(MethodBase method, AspectInfo info)
        {
            base.CompileTimeInitialize(method, info);

            // This block of code is all about pre-calculating a format string containing the names
            // of each method argument.
            ParameterInfo[] parameters = method.GetParameters();
            bool hasParameters = parameters != null && parameters.Length > 0;
            if (hasParameters)
            {
                _parameterListFormat = string.Empty;
                int paramOrdinal = 0;
                foreach (ParameterInfo parameterInfo in parameters)
                {
                    _parameterListFormat += string.Format("{0} = {{{1}}}\r\n", parameterInfo.Name, paramOrdinal++);
                }
            }
            _logParameters = hasParameters && LogParameters;
            _logParameterDetails = LogParameterDetails;

            // Display return value if the method is not void and the option to include return value is set
            MethodInfo methodInfo = method as MethodInfo;
            bool hasReturnValue = ((methodInfo != null) && (methodInfo.ReturnType.Equals(typeof(void)) == false));
            _logReturnValue = (hasReturnValue && LogReturnValue);
        }

        /// <summary>
        /// PostSharp Infrastructure. This method is called each time a tagged method is called.
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)] //This method would never be inlined by the compiler in practice but it's best to be explicit.
        public override void OnEntry(MethodExecutionArgs eventArgs)
        {
            // Get out fast if tracing is disabled
            _tracingEnabled = Enabled;
            if (!_tracingEnabled)
                return;

            // If the method has arguments, log the value of each parameter
            string description = null;
            if (_logParameters)
            {
                string[] stringArray = ObjectArrayToStrings(eventArgs.Arguments.ToArray(), _logParameterDetails);
                description = string.Format(_parameterListFormat, stringArray);
            }

            string category = GetBaseCategory(EnterSubCategory);

            // Write the log message.  skip frames to report back to the customer code caller, not PostSharp or us.
            IMessageSourceProvider sourceProvider = EnableSourceLookup ? new MessageSourceProvider(2) : this as IMessageSourceProvider; ;
            Log.Write(LogMessageSeverity.Verbose, LogSystem, sourceProvider, null, null, LogWriteMode.Queued, null,
                category, Indent("==> " + CaptionName), description);
            Trace.Indent();

            // Last thing we do is get the timer so that we measure just the guts of this method
            eventArgs.MethodExecutionTag = new TraceMethodState(sourceProvider);
        }

        /// <summary>PostSharp Infrastructure. This method is called when the method returns.</summary>
        [MethodImpl(MethodImplOptions.NoInlining)] //This method would never be inlined by the compiler in practice but it's best to be explicit.
        public override void OnExit(MethodExecutionArgs eventArgs)
        {
            // Get out fast if tracing is disabled
            if (!_tracingEnabled)
                return;

            // We need to unindent before calling Indent() below
            Trace.Unindent();

            // Get duration value and format nicely
            TraceMethodState methodState = eventArgs.MethodExecutionTag as TraceMethodState;
            if (methodState != null)
            {
                methodState.Timer.Stop();
                TimeSpan duration = methodState.Timer.Elapsed;

                //fix for issue with .Net < 4 where short durations can be negative on some computers.
                if (duration.Ticks < 0)
                    duration = new TimeSpan(0);

                string durationText = "    ";
                if (duration.TotalMilliseconds < 10000)
                    durationText += duration.TotalMilliseconds + " ms";
                else
                    durationText += duration;

                // Caption and severity vary in case of an exception exit
                string caption;
                LogMessageSeverity severity;
                if (eventArgs.Exception == null)
                {
                    caption = Indent("<== " + CaptionName);
                    severity = LogMessageSeverity.Verbose;
                }
                else
                {
                    caption = Indent("<== (EXCEPTION) " + CaptionName);
                    severity = LogMessageSeverity.Warning;
                }

                // Format description text
                string returnText = _logReturnValue ? "\r\nReturns: " + ObjectToString(eventArgs.ReturnValue, _logParameterDetails) : string.Empty;
                string description = string.Format("Duration: {0}{1}", durationText, returnText);
                string category = GetBaseCategory(ExitSubCategory);

                // Write the log message.  skip frames to report back to the customer code caller, not PostSharp or us.
                Log.Write(severity, LogSystem, methodState.SourceProvider, null, eventArgs.Exception, LogWriteMode.Queued, null,
                          category, caption, description);
            }
        }
    }
}