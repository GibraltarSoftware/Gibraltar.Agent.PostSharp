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
using Gibraltar.Agent.Metrics;
using PostSharp.Aspects;
#if PostSharpLicensed
using PostSharp.Aspects.Dependencies;
#endif

namespace Gibraltar.Agent.PostSharp
{
    /// <summary>
    /// A PostSharp aspect that will log execution time for methods. Data is stored as a
    /// Gibraltar metric allowing charting and graphing in Gibraltar Analyst.
    /// </summary>
    /// <remarks>
    /// 	<para>GTimer is a PostSharp aspect used to record the duration of a method call. It
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
    ///         {BaseCategory}.Trace Enter or {BaseCategory}.Trace Exit. See the <see cref="GAspectBase.BaseCategory">BaseCategory</see> property for more information on
    ///         changing the log message category.
    ///     </para>
    /// 	<para><strong>Enabling and Disabling at Runtime</strong></para>
    /// 	<para>
    ///         You can globally enable and disable GTimer metrics at runtime. To do this just
    ///         set the <see cref="Enabled">Enabled</see> property which will affect all
    ///         methods instrumented with GTimer in the current process. When disabled GTimer
    ///         has minimal performance impact even in high volume scenarios.
    ///     </para>
    /// 	<para><strong>Refining Data Captured</strong></para>
    /// 	<para>
    ///         You can manually set the category used to record this specific timer within all
    ///         of the timers with the <see cref="Category">Category</see> property. By default
    ///         each timer is recorded in a generated category composed of the <see cref="GAspectBase.BaseCategory">BaseCategory Property
    ///         (Gibraltar.Agent.PostSharp.GAspectBase)</see> along with "Method Profiling" and
    ///         the fully qualified class name. By setting the <see cref="Category">Category</see> property you can put a specific timer into a
    ///         subcategory underneath "Method Profiling".
    ///     </para>
    /// 	<para>
    ///         You can override the default name for each timer by setting the <see cref="Name">Name</see> property. Within a given category timers that share the same
    ///         name will be analyzed together in the Gibraltar Analyst. By default the Name is
    ///         set to the name of the method being timed.
    ///     </para>
    /// </remarks>
    /// <example>
    ///     You can associate the GTimer attribute with a single method, a property, or an
    ///     entire class to time all methods in that class. You can also use attribute
    ///     multicasting to apply it to all matching methods in your assembly.
    ///     <code lang="CS" title="Timing a Single Method" description="Timing one method within a class">
    /// public class SampleApplication
    /// {
    ///     [GTimer]
    ///     private void InterestingMethod(int valueOne, String anotherValue)
    ///     {
    ///         //Do something interesting here
    ///     }
    /// }
    /// </code>
    /// 	<code lang="CS" title="Timing all methods within a class" description="Timing all methods within a class">
    /// [GTimer]
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
    [ProvideAspectRole(StandardRoles.PerformanceInstrumentation)]
#endif
    public sealed class GTimer : GAspectBase
    {
        private const string LogSystem = "Gibraltar Timer";
        private const string SubCategory = "Method Profiling";

        private const string DurationValue = "~duration";
        private const string NamespaceValue = "~namespace";
        private const string ClassValue = "~class";
        private const string MethodValue = "~method";
        private const string FullNameValue = "~fullname";

        private static bool s_Enabled = true;

        private string m_Namespace;

        // This local variable ensures that we log exit if we logged entry
        [NonSerialized]
        private bool m_TracingEnabled;

        /// <summary>
        /// Enables or disables timing for all methods tagged with GTimer in the current
        /// process. Enabled by default.
        /// </summary>
        /// <remarks>
        /// This property can be directly changed at runtime to affect all of the methods
        /// tagged with the GTrace attribute without recompilation.
        /// </remarks>
        public static bool Enabled { get { return s_Enabled; } set { s_Enabled = value; } }

        /// <summary>
        /// Dot-delimited category for this timer within all timers for the current
        /// application.
        /// </summary>
        /// <remarks>
        /// 	<para>
        ///         By default the Category is the fully qualified class name of the method being
        ///         timed. This is then added to the <see cref="GAspectBase.BaseCategory">BaseCategory Property
        ///         (Gibraltar.Agent.PostSharp.GAspectBase)</see> along with "Method Profiling" to
        ///         form the full category name.
        ///     </para>
        /// 	<para>
        ///         By setting the <see cref="Category">Category</see> property explicitly you can
        ///         put a specific timer into a subcategory underneath "Method Profiling".
        ///     </para>
        /// </remarks>
        public string Category { get; set; }

        /// <summary>
        /// Name of this specific metric in Gibraltar Analyst. Defaults to the name of the
        /// method being monitored.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// This method pre-computes as much as possible at compile time to minimize runtime processing requirements.
        /// </summary>
        public override void CompileTimeInitialize(MethodBase method, AspectInfo info)
        {
            base.CompileTimeInitialize(method, info);
            m_Namespace = method.DeclaringType.Namespace;
        }

        /// <summary>
        /// This method is called each time a tagged method is called.
        /// </summary>
        [DebuggerHidden]
        public override void OnEntry(MethodExecutionArgs eventArgs)
        {
            // Get out fast if tracing is disabled
            m_TracingEnabled = Enabled;
            if (!m_TracingEnabled)
                return;

            eventArgs.MethodExecutionTag = Stopwatch.StartNew();
        }

        /// <summary>
        /// For convenience in Analyst, each duration is stored in two metrics.
        /// An overall metric is useful for charting hotspots grouping by method.
        /// Individual metrics per method are useful for graphing duration over time.
        /// </summary>
        [DebuggerHidden]
        public override void OnSuccess(MethodExecutionArgs eventArgs)
        {
            // Get out fast if tracing is disabled
            if (!m_TracingEnabled)
                return;

            Stopwatch timer = eventArgs.MethodExecutionTag as Stopwatch;
            if (timer != null)
            {
                timer.Stop();
                TimeSpan duration = timer.Elapsed;
                
                //fix for issue with .Net < 4 where short durations can be negative on some computers.
                if (duration.Ticks < 0)
                    duration = new TimeSpan(0);

                string categoryBase = GetBaseCategory(SubCategory); //we use this for our overall metric

                string subCategory = (Category ?? ((IMessageSourceProvider)this).ClassName);
                string category = categoryBase + (string.IsNullOrEmpty(subCategory) ? string.Empty : "." + subCategory);
                string caption = Name ?? ((IMessageSourceProvider)this).MethodName;

                CreateSample(GetMetric(categoryBase, null), duration); //the summary value for all timers
                CreateSample(GetMetric(category, caption), duration); //the value for just this timer
            }
        }

        /// <summary>
        /// Helper method to store the metric data
        /// </summary>
        private void CreateSample(EventMetric metric, TimeSpan duration)
        {
            //in the extremely unlikely event we couldn't create a metric we'll get null
            if (metric == null)
                return;

            try
            {
                EventMetricSample sample = metric.CreateSample();
                sample.SetValue(DurationValue, duration);
                sample.SetValue(NamespaceValue, m_Namespace);
                sample.SetValue(ClassValue, ((IMessageSourceProvider)this).ClassName);
                sample.SetValue(MethodValue, ((IMessageSourceProvider)this).MethodName);
                sample.SetValue(FullNameValue, m_Namespace + "." + CaptionName);
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

        /// <summary>
        /// Helper function to retrieve the desired EventMetric
        /// </summary>
        private static EventMetric GetMetric(string category, string caption)
        {
            EventMetricDefinition ourMetricDefinition;

            string description;
            if (caption == null)
            {
                caption = "_Overview";
                description = "Overview of method execution time for all monitored methods";
            }
            else
            {
                description = "Method execution time of " + caption;
            }

            try
            {
                //so we can be called multiple times we want to see if the definition already exists.
                if (EventMetricDefinition.TryGetValue(LogSystem, category, caption, out ourMetricDefinition) == false)
                {
                    ourMetricDefinition = new EventMetricDefinition(LogSystem, category, caption);
                    ourMetricDefinition.Description = description;

                    //add the values (that are part of the definition)
                    ourMetricDefinition.DefaultValue = ourMetricDefinition.AddValue(DurationValue, typeof(TimeSpan), SummaryFunction.Average,
                                                                                    "ms", "Duration", "Average execution duration");
                    ourMetricDefinition.AddValue(NamespaceValue, typeof(string), SummaryFunction.Count, string.Empty, "Namespace", "Namespace");
                    ourMetricDefinition.AddValue(ClassValue, typeof(string), SummaryFunction.Count, string.Empty, "Class",
                                                 "Class name ignoring namespace");
                    ourMetricDefinition.AddValue(MethodValue, typeof(string), SummaryFunction.Count, string.Empty, "Method",
                                                 "Method name ignoring class and namespace");
                    ourMetricDefinition.AddValue(FullNameValue, typeof(string), SummaryFunction.Count, string.Empty, "Full Name",
                                                 "Combines Class and Method name");


                    EventMetricDefinition.Register(ref ourMetricDefinition);
                }

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