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
using System.Runtime.CompilerServices;
using Gibraltar.Agent.Metrics;
using PostSharp.Aspects;
#if PostSharpLicensed
using PostSharp.Aspects.Dependencies;
#endif
using PostSharp.Extensibility;
using PostSharp.Reflection;

namespace Gibraltar.Agent.PostSharp
{
    /// <summary>
    /// A PostSharp aspect used to monitor the state of a field (member variable),
    /// recording a log message and metric when it changes.
    /// </summary>
    /// <remarks>
    /// 	<para>GField is a PostSharp aspect used to monitor changes to fields within
    ///     classes. It writes a message to the whenever an associated member variable is
    ///     changed. Additionally:</para>
    /// 	<list type="bullet">
    /// 		<item>The previous value is recorded in the log message</item>
    /// 		<item>The source code location that caused the change is recorded with the log
    ///         message if source code location information is available.</item>
    /// 		<item>A sampled metric is recorded for numeric or boolean fields to enable
    ///         graphing and quantitative analysis in the Gibraltar Analyst.</item>
    /// 	</list>
    /// 	<para><strong>Categorization</strong></para>
    /// 	<para>
    ///         Each log message is recorded in an automatically generated category in the form
    ///         {BaseCategory}.Trace Exit. See the <see cref="BaseCategory">BaseCategory</see>
    ///         property for more information on changing the log message category.
    ///     </para>
    /// 	<para>
    ///         Each metric is automatically recorded in a category in the form
    ///         {BaseCategory}.Field Monitoring.{Category}. See the <see cref="BaseCategory">BaseCategory</see> property for more information on changing the
    ///         category root and the <see cref="Category">Category</see> propety for more
    ///         information on how the full category name is determined.
    ///     </para>
    /// 	<para><strong>Enabling and Disabling at Runtime</strong></para>
    /// 	<para>
    ///         Because GField messages are voluminous and detailed it's often useful to enable
    ///         and disable them at runtime. To do this just set the <see cref="Enabled">Enabled</see> property which will affect all methods instrumented
    ///         with GField in the current process. When disabled GField has minimal
    ///         performance impact even in high volume scenarios.
    ///     </para>
    /// 	<para><strong>Differentiating Between Instances</strong></para>
    /// 	<para>
    ///         Since the GField attribute is applied to a field it will be tracked for all
    ///         instances of the related class. With the exception of static classes this may
    ///         be a potentially unbounded set of different objects. By default, every object
    ///         has a unique runtime identifier which is recorded with the metric value to
    ///         enable correlation during analysis but this value is not predictable or
    ///         consistent. Instead, if an object has a property that represents a more useful,
    ///         unique name for the object then that can be specified in the <see cref="InstanceNamer">InstanceNamer</see> property. Doing so will produce more useful
    ///         log messages and graphs.
    ///     </para>
    /// 	<para><strong>Refining Data Captured</strong></para>
    /// 	<para>
    ///         You can turn down the amount of information captured for a given method by
    ///         GField in log messages by setting the <see cref="EnableSourceLookup">EnableSourceLookup</see> property to false. This will
    ///         reduce the performance overhead of GField at the expense of losing the
    ///         information on what class &amp; method caused the value to change.
    ///     </para>
    /// 	<para>You can supply a Unit caption for fields that represent values in a common
    ///     unit you want to enable comparative graphing of. For example, you might specify
    ///     "ms" for times in milliseconds or "%" for percentages. This improves the overall
    ///     experience graphing the values in Analyst.</para>
    /// 	<para><strong>Advanced Usage</strong></para>
    /// 	<para>You can supply your own methods for formatting individual values, captions,
    ///     and descriptions by creating a new object that derives from GField and overriding
    ///     the appropriate methods:</para>
    /// 	<list type="bullet">
    /// 		<item>
    /// 			<see cref="FormatValue">FormatValue</see>: Overrides how objects are
    ///             translated to strings for insertion into messages.
    ///         </item>
    /// 		<item>
    /// 			<see cref="FormatCaption">FormatCaption</see>: Overrides how log message
    ///             captions are generated.
    ///         </item>
    /// 		<item>
    /// 			<see cref="FormatDescription">FormatDescription</see>: Overrides how log
    ///             message descriptions are generated.
    ///         </item>
    /// 	</list>
    /// </remarks>
    /// <summary>
    /// 	<para>Enables the logging of method entry and exit at runtime complete with
    ///     parameter information and results.</para>
    /// </summary>
    /// <example>
    ///     You can associate the GField attribute with a single field or an entire class to
    ///     trace all methods in that class. You can also use attribute multicasting to apply
    ///     it to all matching fiels or classes in your assembly (not generally recommended)
    ///     <code lang="CS" title="Track a Single Field" description="Track changes for one field within a class">
    /// public class SampleApplication
    /// {
    ///     [GField]
    ///     private int m_TrackedField;
    ///     private void InterestingMethod(int valueOne, String anotherValue)
    ///     {
    ///         //Do something interesting here
    ///         m_TrackedField = valueOne;
    ///     }
    /// }
    /// </code>
    /// </example>
    [DebuggerNonUserCode]
    [Serializable]
#if PostSharpLicensed
    [ProvideAspectRole(StandardRoles.Tracing)]
#endif
    [MulticastAttributeUsage(MulticastTargets.Field, TargetMemberAttributes = MulticastAttributes.NonLiteral)]
    public class GField : LocationInterceptionAspect, IMessageSourceProvider
    {
        private const string LogSystem = "Gibraltar Field Monitor"; //because it's possible it could generate collisions with GFeature
        private const string LogSubCategory = "Trace.Exit";
        private const string MetricSubCategory = "Field Monitoring";

        private static bool s_Enabled = true;

        private string m_MethodName;
        private string m_ClassName;
        private string m_CaptionName;
        private string m_BaseCategory;
        private GAspectBase.MetricValueType m_MetricValueType;
        private bool m_IsStatic;

        [NonSerialized]
        private Dictionary<int, string> m_InstanceNameLookup;

        /// <summary>
        /// Default constructor
        /// </summary>
        public GField()
        {
            //set our defaults
            EnableSourceLookup = true;
            LogValue = true;
            LogValueDetails = true;
        }

        #region Public Properties and Methods

        /// <summary>
        /// The top level category for log messages and metrics.  Defaults to the name of the current application.
        /// </summary>
        public string BaseCategory
        {
            get
            {
                //we have to have a value, so if they set it to null or it got reserialized that way then return our default.
                return string.IsNullOrEmpty(m_BaseCategory) ? GAspectBase.DefaultBaseCategory : m_BaseCategory;
            }
            set
            {
                m_BaseCategory = value;
            }
        }

        /// <summary>
        /// Enables or disables field monitoring for the entire application.  Enabled by default.
        /// </summary>
        public static bool Enabled { get { return s_Enabled; } set { s_Enabled = value; } }

        /// <summary>
        /// Dot-delimited category for the metric related to this field within all fields for the current
        /// application.
        /// </summary>
        /// <remarks>
        /// 	<para>
        ///         By default the Category is the fully qualified class name of the field being
        ///         monitored. This is then added to the <see cref="BaseCategory">BaseCategory
        ///         Property</see> along with "Field Monitoring" to form the full category name.
        ///     </para>
        /// 	<para>
        ///         By setting the <see cref="Category">Category</see> property explicitly you can
        ///         put a specific field into a subcategory underneath "Field Monitoring".
        ///     </para>
        /// </remarks>
        public string Category { get; set;}

        /// <summary>
        /// Name of this specific metric in Gibraltar Analyst.
        /// Default value is the name of the monitored field. </summary>
        public string Name { get; set; }

        /// <summary>
        /// Name of the class property used to name instances of the monitored field.  Optional.
        /// </summary>
        /// <remarks>
        /// 	<para>The instance namer overrides how the object containing the field is
        ///     identified.</para>
        /// 	<para>Since the GField attribute is applied to a field it will be tracked for all
        ///     instances of the related class. With the exception of static classes this may be a
        ///     potentially unbounded set of different objects. By default, every object has a
        ///     unique runtime identifier which is recorded with the metric value to enable
        ///     correlation during analysis but this value is not predictable or consistent.
        ///     Instead, if an object has a property that represents a more useful, unique name for
        ///     the object then specifying its name in this property will produce more useful log
        ///     messages and graphs.</para>
        /// </remarks>
        public string InstanceNamer { get; set; }

        /// <summary>
        /// Enables or disables recording of values. Enabled by default.
        /// </summary>
        /// <remarks>
        /// 	<para>When enabled GField will record the field values before and after they are changed.
        ///     Each parameter will be recorded as its ToString() representation if
        ///     overridden (and for struct and enum types).  If ToString() is not overridden in a class
        ///     type (or if <see cref="LogValueDetails">LogValueDetails</see> is disabled)
        ///     the base implementation will be replaced with the type's full name and the hash code
        ///     for that instance.  Some implementations of ToString() can produce very long strings
        ///     and can result in very large messages for certain objects if such details are also
        ///     enabled (which is the default).</para>
        ///     <para>When disabled, only the sampled metric will be recorded, no log messages will be.</para>
        /// </remarks>
        public bool LogValue { get; set; }

        /// <summary>
        /// Enables or disables recording of object detail for parameters via ToString. Enabled by default.
        /// </summary>
        /// <remarks>
        /// 	<para>When enabled the field values being recorded 
        ///     (with <see cref="LogValue">LogValue</see> enabled) will include object detail
        ///     by converting class instances via their ToString method.  This can produce significant
        ///     overhead depending on the ToString implementation for those passed parameters.</para>
        ///     <para>When disabled the parameter values for most class instances will be recorded by their
        ///     type and hash code to avoid calling ToString() and thus keep overhead to a minimum.  Value types
        ///     (structs) will still be converted via ToString, and string objects will be recorded directly.</para>
        /// </remarks>
        public bool LogValueDetails { get; set; }

        /// <summary>
        /// Units to be displayed in the legend when graphing this field in Gibraltar Analyst.
        /// </summary>
        public string Units { get; set; }

        /// <summary>
        /// Enables or disables lookup of source line and method.  Enabled by default.
        /// </summary>
        public bool EnableSourceLookup { get; set; }

        /// <summary>
        /// PostSharp Infrastructure. Method invoked at build time to ensure that the aspect has been applied to the right target.
        /// </summary>
        /// <param name="locationInfo"></param>
        /// <returns>true if the aspect was applied to an acceptable target, otherwise false.</returns>
        public override bool CompileTimeValidate(LocationInfo locationInfo)
        {
            //we are only for fields, not for properties.
            if (locationInfo.LocationKind == LocationKind.Field)
            {
                //we can't be applied to a constant.
                if ((locationInfo.FieldInfo.Attributes & FieldAttributes.Literal) == FieldAttributes.Literal)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
            else
            {
                return false;
            }
        }

        /// <summary>PostSharp Infrastructure. Initializes the current object.</summary>
        public override void CompileTimeInitialize(LocationInfo targetLocation, AspectInfo aspectInfo)
        {
//            if (CompileTimeValidate(targetLocation) == false)
//                return; //we'll catch it during validate, which is called after initialize (odd, that)

            //we only work on fields (we checked that in our validate)
            FieldInfo fieldInfo = targetLocation.FieldInfo;
            if (fieldInfo == null)
                throw new InvalidOperationException("There is no field information associated with the current location, so it can't be used. Location: " + targetLocation.Name);

            // This will be reported in the Class column of Gibraltar Analyst
            m_ClassName = fieldInfo.DeclaringType.Namespace + "." + fieldInfo.DeclaringType.Name;

            // This will be reported in the Method column of Gibraltar Analyst
            m_MethodName = fieldInfo.Name;

            // This name will be included in the Caption column of Gibraltar Analyst
            m_CaptionName = (Category ?? fieldInfo.DeclaringType.Name) + "." + (Name ?? fieldInfo.Name);

            // For non-static fields, we have some fancy logic that will make it easy
            // and efficient to assign a meaningful name to each field instance.
            // Because the logic uses reflection, we cache values for runtime efficiency
            m_IsStatic = fieldInfo.IsStatic;

            // Used to determine whether this field is suitable for graphing as a SampledMetric
            m_MetricValueType = GAspectBase.GetMetricValueType(fieldInfo.FieldType);
        }

        /// <summary>
        /// PostSharp Infrastructure. Method called instead of the <i>set</i> operation on
        /// the modified field.
        /// </summary>
        /// <param name="context">
        /// Event arguments specifying which field is being accessed and its current
        /// value.
        /// </param>
        [MethodImpl(MethodImplOptions.NoInlining)] //this should never be inlined by the compiler anyway, but we are making a point.
        [DebuggerHidden]
        public override void OnSetValue(LocationInterceptionArgs context)
        {
            // Get out fast if tracing is disabled
            if (!Enabled)
            {
                context.ProceedSetValue(); //or stuff just don't work!
                return;
            }

            object previousValue = context.GetCurrentValue();
            
            //now let the set pipeline continue down to the bottom...
            context.ProceedSetValue();

            //it's essential we don't let an exception alter the control flow
            try
            {
                //and now we're done with the set, so record what the value NOW is
                object newValue = context.Value;

                if (ValueChanged(previousValue, newValue))
                {
                    if (LogValue)
                        WriteLogMessage(context, previousValue, newValue, 2); //one to skip out of us, one to skip our caller which is an IL method that's setting the value.

                    if (m_MetricValueType != GAspectBase.MetricValueType.NotNumeric)
                        WriteSample(context, previousValue, newValue);
                }
            }
            catch { };
        }

        /// <summary>
        /// Initializes the current aspect.
        /// </summary>
        /// <param name="locationInfo">Location to which the current aspect is applied.</param>
        [DebuggerHidden]
        public override void RuntimeInitialize(LocationInfo locationInfo)
        {
            base.RuntimeInitialize(locationInfo);

            //if it isn't static we need to create our instance lookup dictionary
            if (!m_IsStatic)
                m_InstanceNameLookup = new Dictionary<int, string>();
        }

        #endregion

        #region IMessageSourceProvider

        // IMessageSourceProvider is a Gibrlatar interface.  We define it here to
        // suppress Gibraltar's normal source code attribution logic.

        /// <summary>
        /// MethodName is part of the IMessageSourceProvider interface.
        /// In the case that source lookup is being skipped for efficiency,
        /// we can still provide method and class name from information known
        /// at compilation.
        /// </summary>
        string IMessageSourceProvider.MethodName { get { return m_MethodName; } }

        /// <summary>
        /// ClassName is part of the IMessageSourceProvider interface.
        /// In the case that source lookup is being skipped for efficiency,
        /// we can still provide method and class name from information known
        /// at compilation.
        /// </summary>
        string IMessageSourceProvider.ClassName { get { return m_ClassName; } }

        /// <summary>
        /// FileName is part of the IMessageSourceProvider interface.
        /// In the case that source lookup is being skipped for efficiency,
        /// we return null for FileName.
        /// </summary>
        string IMessageSourceProvider.FileName { get { return null; } }

        /// <summary>
        /// LineNumber is part of the IMessageSourceProvider interface.
        /// In the case that source lookup is being skipped for efficiency,
        /// we return zero for LineNumber.
        /// </summary>
        int IMessageSourceProvider.LineNumber { get { return 0; } }

        #endregion

        #region Protected Properties and Methods

        /// <summary>
        /// Calculate the category from the root to the beginning of the autogenerated category.
        /// </summary>
        /// <remarks>
        /// 	<para>By default the base category will be a combination of the current application
        ///     name and a constant string (either "Trace.Exit" or "Profiled Method" depending on
        ///     whether this is a call to get the base category for a metric or a log message). By
        ///     overriding this method you can substitute your own method of calculating this value
        ///     and then set the category parameter to reflect your choice.</para>
        /// 	<para>If you set an invalid (generally null or empty) value the default will be
        ///     used.</para>
        /// </remarks>
        /// <param name="category">The default category for the current aspect, replace with your own non-empty value to override.</param>
        protected virtual void OnBaseCategoryCalculate(ref string category)
        {
            //by default we do nothing - we already decorated the category before it was passed in.
        }

        /// <summary>
        /// Infrastructure. Used to safely determine the base category path for the current
        /// aspect. Will always return a non-empty value.
        /// </summary>
        /// <returns>A non-empty category without the trailing period.</returns>
        /// <param name="subCategory">
        /// Optional. A sub category to use from the default base category for the current
        /// call.
        /// </param>
        protected string GetBaseCategory(string subCategory)
        {
            //set up our default value
            string category;
            if (string.IsNullOrEmpty(subCategory))
            {
                category = BaseCategory;
            }
            else
            {
                category = BaseCategory + "." + subCategory;
            }
            string defaultValue = category; //so we can reset it downstream if there's an issue.

            //now ask them to add to it
            try
            {
                OnBaseCategoryCalculate(ref category);
            }
            catch
            {
                //if they fail, we use our default.
                category = defaultValue;
            }

            //and clean it up if they changed it.
            if (string.IsNullOrEmpty(category))
                category = defaultValue;

            //if it starts or ends with a period fix that.
            if (category.StartsWith("."))
            {
                category = category.Remove(0, 1);
            }

            if (category.EndsWith("."))
            {
                category = category.Remove(category.Length - 1, 1);
            }

            return category;
        }

        #endregion

        #region Private Properties and Methods

        private bool IsStatic
        {
            get { return m_InstanceNameLookup == null; }
        }

        private static bool ValueChanged(object previousValue, object newValue)
        {
            // Check for reference equality first
            if (ReferenceEquals(previousValue, newValue))
                return false;

            //and then overloaded equals.
            if (previousValue == newValue)
                return false;

            // Next, check for null value, by the above test, new value must be non-null if previous value is null
            if (previousValue == null)
                return true;

            // Finally, check for value equality between two equivalent objects
            if (previousValue.Equals(newValue))
                return false;
            else
                return true;
        }

        /// <summary>
        /// Write the log message
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)] //it would never be inlined by the compiler because it's too involved, but we're making a point.
        private void WriteLogMessage(LocationInterceptionArgs context, object previousValue, object newValue, int skipFrames)
        {
            string caption = FormatCaption(context, previousValue, newValue);
            string description = FormatDescription(context, previousValue, newValue);

            string category = GetBaseCategory(LogSubCategory);

            // skipFrames = 0 would be here, 1 is our caller.
            // so we can safely start with skipFrames = 1 as the first possible
            // actual caller outside the framework, for efficiency.
            IMessageSourceProvider sourceProvider = EnableSourceLookup ? new MessageSourceProvider(skipFrames + 1) : this as IMessageSourceProvider;

            Log.Write(LogMessageSeverity.Information, LogSystem, sourceProvider,
                null, null, LogWriteMode.Queued, null,
                category, caption, description);
        }

        private void WriteSample(LocationInterceptionArgs context, object previousValue, object newValue)
        {
            SampledMetric metric = GetMetric(context.Instance);

            //in the extremely unlikely event we couldn't create a metric we'll get null
            if (metric == null)
                return;

            try
            {
                double sampleValue;
                if (m_MetricValueType == GAspectBase.MetricValueType.Boolean)
                    sampleValue = ((bool)newValue) ? 1 : 0;
                else
                    sampleValue = Convert.ToDouble(newValue);

                metric.WriteSample(sampleValue);
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
        /// Allows the formatting of the log message caption to be overridden.
        /// </summary>
        /// <param name="context">The PostSharp location interception arguments.</param>
        /// <param name="previousValue">The value that the field is being changed from.</param>
        /// <param name="newValue">The value that the field is being changed to.</param>
        /// <returns>A substitute caption to use instead of the default generated caption.</returns>
        /// <remarks>
        ///     By overriding this method you can implement your own custom caption formatting for
        ///     log messages in GField. Captions are generally short and often don't contain any
        ///     insertion strings. Captions show in the main body of the message viewer. For longer
        ///     values use <see cref="FormatDescription">FormatDescription</see> which is designed
        ///     for extended data.
        /// </remarks>
        protected virtual string FormatCaption(LocationInterceptionArgs context, object previousValue, object newValue)
        {
            string formattedValue = FormatValue(newValue);
            if (IsStatic)
                return string.Format("Set {0} = {1}", m_CaptionName, formattedValue);
            else
            {
                string instanceName = GetInstanceName(context.Instance);
                return string.Format("Set {0}[{2}] = {1}", m_CaptionName, formattedValue, instanceName);
            }
        }

        /// <summary>
        /// Allows the formatting of the log message description to be overridden.
        /// </summary>
        /// <remarks>
        /// By overriding this method you can implement your own custom description
        /// formatting for log messages in GField. Captions are generally short and often don't
        /// contain any insertion strings. Descriptions are designed for longer values and are
        /// shown in the Log Message Detail section of Gibraltar Analyst, along with the Caption
        /// (so it is not desirable to duplicate the caption in the descripton).
        /// </remarks>
        /// <param name="context">The PostSharp location interception arguments.</param>
        /// <param name="previousValue">The value that the field is being changed from.</param>
        /// <param name="newValue">The value that the field is being changed to.</param>
        /// <returns>
        /// A substitute description to use instead of the default generated
        /// description.
        /// </returns>
        protected virtual string FormatDescription(LocationInterceptionArgs context, object previousValue, object newValue)
        {
            return "was: " + FormatValue(previousValue);
        }

        /// <summary>
        /// Allows the formatting of the field value to be overridden.
        /// </summary>
        /// <returns>A string representation of the value.</returns>
        /// <remarks>
        /// When overridden this method is used instead of the default ToString method to
        /// convert old and new values to a string representation whenever they change.
        /// </remarks>
        /// <param name="value">The value to be formatted</param>
        protected virtual string FormatValue(object value)
        {
            string result = GAspectBase.ObjectToString(value, LogValueDetails);

            //if this was originally a string we want to do some escape around embedded quotes to escape them.  I guess.
            if (value is string)
            {
                result = result.Replace("\"", "\\\"");
                return "\"" + result + "\"";
            }
            else
            {
                return result;
            }
        }

        /// <summary>
        /// Helper function to retrieve the desired EventMetric
        /// </summary>
        private SampledMetric GetMetric(object instance)
        {
            string subCategory = (Category ?? m_ClassName);
            string category = GetBaseCategory(MetricSubCategory) + (string.IsNullOrEmpty(subCategory) ? string.Empty : "." + subCategory);
            string caption = Name ?? m_MethodName;
            string instanceName = IsStatic ? null : GetInstanceName(instance);

            try
            {
                SampledMetricDefinition metricDefinition;
                if (SampledMetricDefinition.TryGetValue(LogSystem, category, caption, out metricDefinition) == false)
                {
                    string units = Units ?? (m_MetricValueType == GAspectBase.MetricValueType.Boolean ? "Boolean" : "Value");
                    metricDefinition = SampledMetricDefinition.Register(LogSystem, category, caption, SamplingType.RawCount,
                        units, caption, "Monitor value changes for field " + caption);
                }

                SampledMetric metric = SampledMetric.Register(metricDefinition, instanceName);
                return metric;
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

        // This function assumes that instance and m_InstanceNameLookup have already been
        // determined to be non-null.
        private string GetInstanceName(object instance)
        {
            string instanceName = null;

            // We use hashcode as the key because we don't want to hold references
            // to object instances
            int hashCode = instance.GetHashCode();

            lock (m_InstanceNameLookup) //because there may be multiple threads running around in here at one time....
            {
                bool foundName = m_InstanceNameLookup.TryGetValue(hashCode, out instanceName);

                // If instance not found in dictionary, figure out the name and save it for next time
                if (!foundName)
                {
                    // First, check if there is an InstanceName property defined, if so, use it
                    instanceName = GetPropValue(instance, InstanceNamer);

                    //otherwise lets go with the hash code.
                    if (string.IsNullOrEmpty(instanceName))
                        instanceName = "0x" + hashCode.ToString("X8");

                    m_InstanceNameLookup.Add(hashCode, instanceName);
                }
            }

            return instanceName;
        }

        private static string GetPropValue(object src, string propName)
        {
            if (propName == null)
                return null;

            try
            {
                Type type = src.GetType();
                PropertyInfo property = type.GetProperty(propName);
                object value = property.GetValue(src, null);
                return value.ToString();
            }
            catch (Exception)
            {
                return null;
            }
        }

        #endregion
    }
}