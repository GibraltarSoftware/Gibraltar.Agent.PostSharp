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
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using Gibraltar.Agent.Metrics;
using PostSharp.Aspects;
#if PostSharpLicensed
using PostSharp.Aspects.Dependencies;
#endif
using Gibraltar.Agent.Logging;

namespace Gibraltar.Agent.PostSharp
{
    /// <summary>
    /// A PostSharp aspect used to record how often a particular method in your
    /// application is used, how long it takes to run, and whether it was ultimately successful
    /// or not. It record both log messages and metrics to enable powerful analysis.
    /// </summary>
    /// <remarks>
    /// 	<para>GFeature is a PostSharp aspect used to record detailed information about how
    ///     a particular feature in your application is used. By associating the GFeature
    ///     attribute with a method it will automatically record:</para>
    /// 	<list type="bullet">
    /// 		<item>A log message on method invocation with the input parameters and the
    ///         feature caption.</item>
    /// 		<item>If the method is exiting because an exception is being thrown out of the
    ///         method that is noted and the exception details are recorded on an error log
    ///         message.</item>
    /// 		<item>The log messages are attributed to the source code location that invoked
    ///         the method, if source code location information is available.</item>
    /// 		<item>An event metric sample is recorded with information on how long the
    ///         method ran, what each of its input parameters were, whether it ended with an
    ///         exception, and its categorization. Using Loupe Desktop you can dissect this
    ///         information to understand what features are used most often (or least often),
    ///         how they're used, and how well they perform.</item>
    /// 	</list>
    /// 	<para><strong>Log Message Categorization</strong></para>
    /// 	<para>
    ///         Each log message is recorded in an automatically generated category in the form
    ///         {BaseCategory}.{Caption}. See the <see cref="GAspectBase.BaseCategory">BaseCategory</see> property for more information on
    ///         changing the log message category. The Caption will be Feature Usage by default
    ///         unless you set an alternate value using the <see cref="Caption">Caption</see>
    ///         Property.
    ///     </para>
    /// 	<para><strong>Log Message Severity</strong></para>
    /// 	<para>
    ///         By default GFeature will log messages as Informational unless the method is
    ///         exiting due to an exception. In these cases an Error will be recorded. These
    ///         defaults were optimized for the high level Feature use case, however if you are
    ///         using GFeature at multiple levels in your application (like to record each Data
    ///         Access Layer call) you may want to turn these severities down. You can adjust
    ///         them using the <see cref="MessageSeverity">MessageSeverity Property</see> and
    ///         <see cref="ExceptionMessageSeverity">ExceptionMessageSeverity Property</see>.
    ///     </para>
    /// 	<para><strong>Metric Categorization</strong></para>
    /// 	<para>
    ///         Each event metric is recorded in an automatically generated category in the
    ///         form {BaseCategory}.{Caption}.{Category}. See the <see cref="GAspectBase.BaseCategory">BaseCategory</see> property for more information on
    ///         how the BaseCategory can be set. You can set the <see cref="Caption">Caption</see> Property or leave it alone to use the default of
    ///         Feature. You can set the <see cref="Category">Category Property</see> to change
    ///         the subcategory used or leave it alone to use the default of the class name.
    ///     </para>
    /// 	<para><strong>Enabling and Disabling at Runtime</strong></para>
    /// 	<para>
    ///         You can globally enable and disable GFeature at runtime. To do this just set
    ///         the <see cref="Enabled">Enabled</see> property which will affect all methods
    ///         instrumented with GFeature in the current process. When disabled GFeature has
    ///         minimal performance impact even in high volume scenarios.
    ///     </para>
    /// 	<para><strong>Refining Data Captured</strong></para>
    /// 	<para>
    ///         You can manually set the category used to record this specific feature within
    ///         all of the features with the <see cref="Category">Category</see> property. By
    ///         default each feature is recorded in a generated category composed of the
    ///         <see cref="GAspectBase.BaseCategory">BaseCategory Property
    ///         (Gibraltar.Agent.PostSharp.GAspectBase)</see> along with "Feature Usage" and
    ///         the fully qualified class name. By setting the <see cref="Category">Category</see> property you can put a specific feature into a
    ///         subcategory underneath "Feature Usage".
    ///     </para>
    /// 	<para>
    ///         This aspect is also useful for recording each time your application moves
    ///         between tiers, such as for web service calls or calls to an external database.
    ///         In those cases you'll probably want to change "Feature Usage" to something
    ///         else, like "Web Server" or "Database". You can do this using the <see cref="Caption">Caption</see> property.
    ///     </para>
    /// 	<para>
    ///         You can override the default name for each feature by setting the <see cref="Name">Name</see> property. Within a given category features that share the
    ///         same name will be analyzed together in the Gibraltar Analyst. Therefore, they
    ///         must have the same method signatures or the metric may not be recorded
    ///         successfully. Alternately, you can disable parameter recording in these cases
    ///         by setting the <see cref="LogParameters">LogParameters Property</see> to false.
    ///         By default the Name is set to the name of the method being timed.
    ///     </para>
    /// </remarks>
    /// <example>
    ///     You can associate the GFeature attribute with a single method, a property, or an
    ///     entire class to time all methods in that class. You can also use attribute
    ///     multicasting to apply it to all matching methods in your assembly.
    ///     <code lang="CS" title="Monitoring a Single Method" description="Monitoring one method within a class">
    /// public class SampleApplication
    /// {
    ///     [GFeature]
    ///     private void InterestingMethod(int valueOne, String anotherValue)
    ///     {
    ///         //Do something interesting here
    ///     }
    /// }
    /// </code>
    /// 	<code lang="CS" title="Monitoring all methods within a class" description="Monitoring all methods within a class">
    /// [GFeature]
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
    [AspectTypeDependency(AspectDependencyAction.Order, AspectDependencyPosition.After, typeof(GTrace))]
    [AspectTypeDependency(AspectDependencyAction.Order, AspectDependencyPosition.After, typeof(GException))]
#endif
    public sealed class GFeature : GAspectBase
    {
        private const string LogSystem = "Gibraltar Feature Usage";
        private const string DefaultCaption = "Feature Usage";

        private const string DurationValue = "~duration";
        private const string ThreadValue = "~thread";
        private const string ResultValue = "~result";
        private const string ErrorValue = "~error";
        private const string CategoryValue = "~category";
        private const string NameValue = "~name";
        private const string FullNameValue = "~fullname";

        private static bool s_Enabled = true;

        // Internal variables calculated at compile-time for run-time efficiency
        private bool _logParameters;  // true if method has parameters AND LogParameters == true
        private bool _logParameterDetails;  // true if LogParameterDetails == true
        private bool _logReturnValue; // true if method has return value AND LogReturnValues == true
        private string _parameterListFormat;
        private List<string> _parameterNames;
        private List<GAspectBase.MetricValueType> _parameterMetricValueTypes;
        private GAspectBase.MetricValueType _returnMetricValueType;

        // Internal variables only used at run-time
        [NonSerialized]
        private bool m_TracingEnabled;

        /// <summary>
        /// GFeaure is a GSharp attribute for monitoring feature usage.
        /// </summary>
        public GFeature()
        {
            LogReturnValue = true;
            LogParameters = true;
            LogParameterDetails = true;
            EnableSourceLookup = true;
            Caption = DefaultCaption;
            MessageSeverity = LogMessageSeverity.Information;
            ExceptionMessageSeverity = LogMessageSeverity.Error;
        }

        /// <summary>
        /// Enables or disables recording for all methods tagged with GFeature in the current
        /// process. Enabled by default.
        /// </summary>
        /// <remarks>
        /// This property can be directly changed at runtime to affect all of the methods
        /// tagged with the GFeature attribute without recompilation.
        /// </remarks>
        public static bool Enabled { get { return s_Enabled; } set { s_Enabled = value; } }

        /// <summary>
        /// Dot-delimited category for the metric related to this feature within all features for the current
        /// application.
        /// </summary>
        /// <remarks>
        /// 	<para>
        ///         By default the Category is the fully qualified class name of the method being
        ///         monitored. This is then added to the <see cref="GAspectBase.BaseCategory">BaseCategory
        ///         Property</see> along with "Feature Usage" to form the full category name.
        ///     </para>
        /// 	<para>
        ///         By setting the <see cref="Category">Category</see> property explicitly you can
        ///         put a specific field into a subcategory underneath "Feature Usage".
        ///     </para>
        /// 	<para>
        ///         Two features with the same <see cref="Category">Category</see> and <see cref="Name">Name</see> must have a common set of parameters or the event metrics
        ///         necessary for analysis will not record correctly. If you need to associate the
        ///         same feature Category and Name with methods that have different signatures
        ///         disable parameter recording by setting the <see cref="LogParameters">LogParameters Property</see> to false.
        ///     </para>
        /// </remarks>
        public string Category { get; set; }

        /// <summary>
        /// Name of this specific metric in Gibraltar Analyst.
        /// Default value is the of the method tagged with the GFeature attribute.
        /// </summary>
        /// <remarks>
        ///     Two features with the same <see cref="Category">Category</see> and Name must have a
        ///     common set of parameters or the event metrics necessary for analysis will not
        ///     record correctly. If you need to associate the same feature Category and Name with
        ///     methods that have different signatures disable parameter recording by setting the
        ///     <see cref="LogParameters">LogParameters Property</see> to false.
        /// </remarks>
        public string Name { get; set; }

        /// <summary>
        /// The display caption for this type of functionality. Defaults to "Feature Usage"
        /// </summary>
        /// <remarks>This is used in several places to generate log messages, categorization, etc.</remarks>
        public string Caption { get; set; }

        /// <summary>
        /// Enables or disables recording of parameters and their values. Enabled by default.
        /// </summary>
        /// <remarks>
        /// 	<para>When enabled GFeature will record the parameter values on entry to the method
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
        /// 	<para>When enabled GFeature will record the return value on method exit.
        ///     The return value  will be recorded as its ToString() representation if
        ///     overridden (and for struct and enum types).  If ToString() is not overridden in a class
        ///     type (or if <see cref="LogParameterDetails">LogParameterDetails</see> is disabled)
        ///     the base implementation will be replaced with the type's full name and the hash code
        ///     for that instance.  Some implementations of ToString() can produce very long strings
        ///     and can result in very large messages for certain objects if such details are also
        ///     enabled (which is the default).</para>
        /// </remarks>
        public bool LogReturnValue { get; set; }

        /// <summary>
        /// Enables lookup of source line and method. Enabled by default.</summary>
        /// <remarks>
        /// 	<para>If enabled GFeature will attempt to identify the class, method, file and line
        ///     number of the code that called the target method. Some or all of this information
        ///     may not be available depending on how the method was compiled and whether the
        ///     library symbol file is present on the local computer.</para>
        /// 	<para>In certain high performance scenarios disabling the source code lookup can
        ///     significantly improve performance. If you find that using GFeature is causing a
        ///     performance issue in your application you may experiment with selectively disabling
        ///     source code lookup on methods that are called extremely frequently.</para>
        /// </remarks>
        public bool EnableSourceLookup { get; set; }

        /// <summary>
        /// The severity to use for routine messages.  Defaults to Information.
        /// </summary>
        public LogMessageSeverity MessageSeverity { get; set; }

        /// <summary>
        /// The severity to use for reporting exception exits.  Defaults to Error.
        /// </summary>
        public LogMessageSeverity ExceptionMessageSeverity { get; set; }

        /// <summary>
        /// PostSharp Infrastructure.  This method pre-computes as much as possible at compile time to minimize runtime processing requirements.
        /// </summary>
        public override void CompileTimeInitialize(MethodBase method, AspectInfo info)
        {
            base.CompileTimeInitialize(method, info);

            // This name will be included in the Caption column of Gibraltar Analyst
            // We override the value defined in GAspectBase to consider Category & Name
            // values passed from the caller
            CaptionName = (Category ?? method.DeclaringType.Name) + "." + (Name ?? method.Name);

            // This block of code is all about pre-calculating data structures needed to
            // efficiently record parameters passed to the monitored method.

            ParameterInfo[] parameters = method.GetParameters();
            bool hasParameters = parameters != null && parameters.Length > 0;
            _logParameters = hasParameters && LogParameters;
            _logParameterDetails = LogParameterDetails;

            if ((_logParameters) && (parameters != null))
            {
                _parameterNames = new List<string>();
                _parameterMetricValueTypes = new List<MetricValueType>();
                _parameterListFormat = string.Empty;
                for (int index = 0; index < parameters.Length; index++)
                {
                    ParameterInfo parameterInfo = parameters[index];
                    _parameterListFormat += string.Format("{0} = {{{1}}}\r\n", parameterInfo.Name, index);
                    _parameterNames.Add(parameterInfo.Name);
                    _parameterMetricValueTypes.Add(GetMetricValueType(parameterInfo.ParameterType));
                }
            }

            // Display return value if the method is not void and the option to include return value is set
            MethodInfo methodInfo = method as MethodInfo;
            bool hasReturnValue = ((methodInfo != null) && (methodInfo.ReturnType != typeof (void)));
            _logReturnValue = hasReturnValue && LogReturnValue;
            if (_logReturnValue)
                _returnMetricValueType = GetMetricValueType(methodInfo.ReturnType);
        }

        /// <summary>
        /// PostSharp Infrastructure. This method is called each time a tagged method is called.
        /// </summary>
        [DebuggerHidden]
        public override void OnEntry(MethodExecutionArgs eventArgs)
        {
            // Get out fast if tracing is disabled
            m_TracingEnabled = Enabled;
            if (!m_TracingEnabled)
                return;

            // If the method has arguments, log the value of each parameter
            string description = null;
            if (_logParameters)
            {

                string[] stringArray = ObjectArrayToStrings(eventArgs.Arguments.ToArray(), _logParameterDetails);
                description = string.Format(_parameterListFormat, stringArray);
            }

            string caption = string.Format("{0} {1} Called", Caption, CaptionName);

            // skipFrames = 0 would be here, 1 is our caller
            // but we've found that 2 is the right minimum skip to not get our method.
            IMessageSourceProvider sourceProvider = EnableSourceLookup ? new MessageSourceProvider(2) : this as IMessageSourceProvider;

            Log.Write(MessageSeverity, LogSystem, sourceProvider, null, null, LogWriteMode.Queued, null,
                      LogCategory, caption, description);

            // Last thing we do is get the timer so that we measure just the guts of this method
            Stopwatch timer = Stopwatch.StartNew();
            eventArgs.MethodExecutionTag = timer;
        }

        /// <summary>
        /// PostSharp Infrastructure. This method is called when the method returns.
        /// </summary>
        [DebuggerHidden]
        public override void OnExit(MethodExecutionArgs eventArgs)
        {
            // Get out fast if tracing is disabled
            if (!m_TracingEnabled)
                return;

            // Get duration value and format nicely
            Stopwatch timer = eventArgs.MethodExecutionTag as Stopwatch;
            if (timer != null)
            {
                timer.Stop();
                TimeSpan duration = timer.Elapsed;

                //fix for issue with .Net < 4 where short durations can be negative on some computers.
                if (duration.Ticks < 0)
                    duration = new TimeSpan(0);

                string subCategory = (Category ?? ((IMessageSourceProvider)this).ClassName);

                string category = GetBaseCategory(Caption) + (string.IsNullOrEmpty(subCategory) ? string.Empty : "." + subCategory);
                string name = Name ?? ((IMessageSourceProvider)this).MethodName;

                EventMetric metric = GetMetric(category, name);
                CreateSample(metric, eventArgs, duration);

                if (eventArgs.Exception != null)
                {
                    string caption = string.Format("{0} {1} Failed due to " + eventArgs.Exception.GetType(), Caption, CaptionName);

                    // Use the stack trace in the exception to identify the relevant line
                    IMessageSourceProvider sourceProvider = new ExceptionSourceProvider(eventArgs.Exception);

                    Log.Write(ExceptionMessageSeverity, LogSystem, sourceProvider, null, eventArgs.Exception, LogWriteMode.Queued, null,
                              LogCategory, caption, eventArgs.Exception.Message);
                }
            }
        }

        /// <summary>
        /// The category we should use for logging which is not feature specific.
        /// </summary>
        private string LogCategory
        {
            get
            {
                return GetBaseCategory(Caption);
            }
        }

        /// <summary>
        /// Helper method to store the metric data
        /// </summary>
        private void CreateSample(EventMetric metric, MethodExecutionArgs eventArgs, TimeSpan duration)
        {
            //if we failed to create a metric for some crazy reason just return.
            if (metric == null)
                return;

            try
            {
                EventMetricSample sample = metric.CreateSample();

                sample.SetValue(DurationValue, duration);
                sample.SetValue(ThreadValue, "Thread " + Thread.CurrentThread.ManagedThreadId);

                // Write parameter values, if applicable
                if (_logParameters)
                {
                    object[] array = eventArgs.Arguments.ToArray();
                    for (int index = 0; index < _parameterMetricValueTypes.Count; index++)
                    {
                        switch (_parameterMetricValueTypes[index])
                        {
                            case GAspectBase.MetricValueType.Numeric:
                                sample.SetValue(_parameterNames[index], Convert.ToDouble(array[index]));
                                break;
                            case GAspectBase.MetricValueType.Boolean:
                                sample.SetValue(_parameterNames[index], ((bool)array[index]) ? 1 : 0);
                                break;
                            default:
                                sample.SetValue(_parameterNames[index], ObjectToString(array[index], _logParameterDetails));
                                break;
                        }
                    }
                }

                // Write return value, if applicable
                if (_logReturnValue)
                {
                    switch (_returnMetricValueType)
                    {
                        case GAspectBase.MetricValueType.Numeric:
                            sample.SetValue(ResultValue, Convert.ToDouble(eventArgs.ReturnValue));
                            break;
                        case GAspectBase.MetricValueType.Boolean:
                            sample.SetValue(ResultValue, ((bool)eventArgs.ReturnValue) ? 1 : 0);
                            break;
                        default:
                            sample.SetValue(ResultValue, ObjectToString(eventArgs.ReturnValue, _logParameterDetails));
                            break;
                    }
                }

                sample.SetValue(ErrorValue, eventArgs.Exception == null ? null : eventArgs.Exception.GetType().Name);
                string category = Category ?? ((IMessageSourceProvider)this).ClassName;
                string name = Name ?? ((IMessageSourceProvider)this).MethodName;

                sample.SetValue(CategoryValue, category);
                sample.SetValue(NameValue, name);
                sample.SetValue(FullNameValue, category + "." + name);
                sample.Write();
            }
            catch (Exception ex)
            {
                GC.KeepAlive(ex);

#if DEBUG
                Log.RecordException(ex, "Gibraltar.PostSharp", true);
#endif
            }
        }

        private static string GetStringValue(object obj)
        {
            if (obj == null)
                return "null";

            string value = obj.ToString();
            return obj.GetType().FullName == value ? null : value;
        }

        /// <summary>
        /// Helper function to retrieve the desired EventMetric
        /// </summary>
        private EventMetric GetMetric(string category, string caption)
        {
            EventMetricDefinition ourMetricDefinition;

            //we never want to allow an exception to stop us here..
            try
            {
                //so we can be called multiple times we want to see if the definition already exists.
                if (EventMetricDefinition.TryGetValue(LogSystem, category, caption, out ourMetricDefinition) == false)
                {
                    //it does not exist - create it fresh.
                    ourMetricDefinition = new EventMetricDefinition(LogSystem, category, caption);
                    ourMetricDefinition.Description = "Track feature usage of " + caption;

                    ourMetricDefinition.DefaultValue = ourMetricDefinition.AddValue(DurationValue, typeof(TimeSpan), SummaryFunction.Average, "ms", "Duration", "Average execution duration");
                    ourMetricDefinition.AddValue(ThreadValue, typeof(string), SummaryFunction.Average, string.Empty, "Thread", "Managed Thread ID of call");

                    if (_logParameters)
                    {
                        for (int index = 0; index < _parameterMetricValueTypes.Count; index++)
                        {
                            string name = _parameterNames[index];
                            string description = string.Format("{0} Parameter Value", _parameterNames[index]);
                            Type metricType;
                            SummaryFunction summaryFunction;
                            string units;
                            switch (_parameterMetricValueTypes[index])
                            {
                                case GAspectBase.MetricValueType.Numeric:
                                    metricType = typeof(double);
                                    summaryFunction = SummaryFunction.Average;
                                    units = "Value";
                                    break;
                                case GAspectBase.MetricValueType.Boolean:
                                    metricType = typeof(double);
                                    summaryFunction = SummaryFunction.Average;
                                    units = "Boolean";
                                    break;
                                default:
                                    metricType = typeof(string);
                                    summaryFunction = SummaryFunction.Count;
                                    units = string.Empty;
                                    break;
                            }

                            ourMetricDefinition.AddValue(name, metricType, summaryFunction, units, name, description);
                        }
                    }

                    if (_logReturnValue)
                    {
                        switch (_returnMetricValueType)
                        {
                            case GAspectBase.MetricValueType.Numeric:
                                ourMetricDefinition.AddValue(ResultValue, typeof(double), SummaryFunction.Average, "Value", "Result", "Return value of the method implementing this feature");
                                break;
                            case GAspectBase.MetricValueType.Boolean:
                                ourMetricDefinition.AddValue(ResultValue, typeof(double), SummaryFunction.Average, "Boolean", "Result", "Return value of the method implementing this feature");
                                break;
                            default:
                                ourMetricDefinition.AddValue(ResultValue, typeof(string), SummaryFunction.Count, string.Empty, "Result", "Return value of the method implementing this feature");
                                break;
                        }
                    }

                    ourMetricDefinition.AddValue(ErrorValue, typeof(bool), SummaryFunction.Average, string.Empty, "Error", "Null unless an exception causes feature to fail");
                    ourMetricDefinition.AddValue(CategoryValue, typeof(string), SummaryFunction.Count, string.Empty, "Category", "Category including this feature");
                    ourMetricDefinition.AddValue(NameValue, typeof(string), SummaryFunction.Count, string.Empty, "Name", "Name of this specific feature");
                    ourMetricDefinition.AddValue(FullNameValue, typeof(string), SummaryFunction.Count, string.Empty, "Full Name", "Fully qualified name of this feature");

                    //now that we've fully defined the metric we register it.  Notice that we pass by ref- if another thread was attempting to 
                    //register the same metric at the same time we'd get back whoever won that race, guaranteeing thread safe behavior.
                    EventMetricDefinition.Register(ref ourMetricDefinition);
                }

                //now that we know we have the metric definition, get the specific metric we want to record.
                //we're going to record everything under the default instance in this case.
                EventMetric ourEventMetric = EventMetric.Register(ourMetricDefinition, null);
                return ourEventMetric;
            }
            catch (Exception ex)
            {
                GC.KeepAlive(ex);

#if DEBUG
                Log.RecordException(ex, "Gibraltar.PostSharp", true);
#endif
                return null;
            }
        }
    }
}
