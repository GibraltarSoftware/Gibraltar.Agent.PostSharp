using System;
using System.Diagnostics;
using PostSharp.Aspects;
#if PostSharpLicensed
using PostSharp.Aspects.Dependencies;
#endif
using Gibraltar.Agent.Logging;

namespace Gibraltar.Agent.PostSharp
{
    /// <summary>
    /// A PostSharp aspect that logs exceptions after they are thrown when they cause a
    /// method to exit. This allows for logging of handled as well as unhandled
    /// exceptions.
    /// </summary>
    /// <remarks>
    /// 	<para>GException is a PostSharp aspect that watches for exceptions causing an
    ///     associated method to exit early. Even if these exceptions would ultimately be
    ///     handled by a method higher up the call stack they are recorded. Because they may be
    ///     handled they are logged as warnings. If they ultimately become unhandled exceptions
    ///     then Gibraltar's Error Manager will automatically record them as an error.</para>
    /// 	<para><strong>Recommended Usage</strong></para>
    /// 	<para>This aspect is very inexpensive at runtime and is highly recommended for use
    ///     throughout your assemblies. In general the only cases where it isn't highly useful
    ///     is when your code routinely uses exceptions to alter control flow or when you are
    ///     also using GTrace, which will record the same information recorded by GException in
    ///     a different form.</para>
    /// <para>This can be easily accomplished by using Attribute Multicasting.  See the general
    /// documentation on integrating with PostSharp for more information.</para>
    /// 	<para><strong>Log Message Categorization</strong></para>
    /// 	<para>
    ///         Each log message is recorded in an automatically generated category in the form
    ///         {BaseCategory}.Thrown Exceptions. See the <see cref="GAspectBase.BaseCategory">BaseCategory</see> property for more information on
    ///         changing the log message category.
    ///     </para>
    /// 	<para><strong>Enabling and Disabling at Runtime</strong></para>
    /// 	<para>
    ///         It is possible to enable and disable GException at runtime. To do this just set
    ///         the <see cref="Enabled">Enabled</see> property which will affect all methods
    ///         instrumented with GException in the current process. Because GException records
    ///         nothing in the nominal case where there is no exception causing the call stack
    ///         to unwind it is very inexpensive to apply broadly and rarely should be
    ///         disabled.
    ///     </para>
    /// </remarks>
    /// <example>
    ///     You can associate the GException attribute with a single method, a property, or an
    ///     entire class to monitor all methods in that class. You can also use attribute
    ///     multicasting to apply it to all matching methods in your assembly.
    ///     <code lang="CS" title="Monitor a Single Method" description="Monitor one method within a class">
    /// public class SampleApplication
    /// {
    ///     [GException]
    ///     private void InterestingMethod(int valueOne, String anotherValue)
    ///     {
    ///         //Do something interesting here
    ///     }
    /// }
    /// </code>
    /// 	<code lang="CS" title="Monitor all methods within a class" description="Monitor all methods within a class">
    /// [GException]
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
    [ProvideAspectRole(StandardRoles.ExceptionHandling)]
    [AspectTypeDependency(AspectDependencyAction.Order, AspectDependencyPosition.After, typeof(GTimer))]
    [AspectTypeDependency(AspectDependencyAction.Order, AspectDependencyPosition.After, typeof(GTrace))]
#endif
    public class GException : GAspectBase
    {
        private const string LogSystem = "Gibraltar Exception Monitor";
        private const string SubCategory = "Thrown Exceptions";

        private static bool s_Enabled = true;

        /// <summary>
        /// GException is a Gibraltar attribute for monitoring exceptions.
        /// </summary>
        public GException()
        {
            MessageSeverity = LogMessageSeverity.Warning;
        }

        /// <summary>
        /// Enables or disables exception recording for all methods tagged with GException in the current
        /// process. Enabled by default.
        /// </summary>
        /// <remarks>
        /// This property can be directly changed at runtime to affect all of the methods
        /// tagged with the GException attribute without recompilation.
        /// </remarks>
        public static bool Enabled { get { return s_Enabled; } set { s_Enabled = value; } }

        /// <summary>
        /// The severity to use for messages.  Defaults to Warning.
        /// </summary>
        public LogMessageSeverity MessageSeverity { get; set; }

        /// <summary>
        /// PostSharp Infrastructure.  Provides exception information to be recorded on method exit.
        /// </summary>
        /// <param name="eventArgs"></param>
        [DebuggerHidden]
        public override void OnException(MethodExecutionArgs eventArgs)
        {
            // Get out fast if tracing is disabled
            if (!Enabled)
                return;

            string caption = "Exception Exit " + CaptionName;
            string description = eventArgs.Exception.Message;

            // Use the stack trace in the exception to identify the relevant line
            IMessageSourceProvider sourceProvider = new ExceptionSourceProvider(eventArgs.Exception);

            Log.Write(MessageSeverity, LogSystem, sourceProvider, null,
                eventArgs.Exception, LogWriteMode.Queued,
                null, GetBaseCategory(SubCategory), Indent(caption), description);
        }
    }
}