Loupe Agent for PostSharp
===================

Easily inject logging, profiling, and feature usage monitoring into your .NET application. 
Records all information using the Loupe Agent which is designed for use in production.

If you don't need to modify the source code just download the latest 
[Loupe Agent for PostSharp 3](https://www.nuget.org/packages/Gibraltar.Agent.PostSharp.3.0/).

PostSharp is an aspect-oriented programming tool that helps make your code easier to understand 
and maintain by encapsulating repeating code patterns as .NET custom attributes. 
These repeating patterns, called aspects, can then be applied wherever you want in your software 
by just tagging an appropriate line of code with the attribute.  Loupe includes a set of aspects 
that can quickly create log messages and metrics.  To get started:

1. Add the Loupe Agent for PostSharp (either from NuGet or by compiling the project in this repository)
2. Add attributes to properties, methods, classes, etc. to apply the various aspects in this library - 
   * GTrace: Verbose trace-style logging of entrance and exit of a property or method
   * GException: Warning log messages for exception exit of properties and methods
   * GFeature: (Our favorite!) Log entrance of a method and record timing data about the method invocation.
   * GTimer: Record performance timing of a property or method
   * GField: Record the value of a variable every time it changes
3. Compile your assembly and view the log messages & metrics in Loupe

How PostSharp and Loupe Work Together
-------------------------------------

When you tag methods with aspects PostSharp inserts itself in the build process to post-process 
compiled assemblies adding code at the MSIL level incorporating the aspect with your code. 
Each time you build your project the aspect code is inserted in the right places. 
With this approach you don't have to worry about autogenerated code getting in the way of your ability 
to maintain and work with your application. 

For example, in the code snippet below, the SearchGoogle method is tagged with the GTrace aspect. 
This would result in Loupe logging every call to this method including arguments, return value and 
execution time. Alternately, if [GTrace] were applied to the SearchWrapper class, calls to all 
methods in the class would be logged.

```C#
public class SearchWrapper
{
    [GTrace]
    public Results SearchGoogle(string search)
    {
        // ...
    }
    public Results SearchBing(string search)
    {
        // ...
    }
}
```

Aspects can also be assigned to whole assemblies with flexible filtering so that you can instrument 
whole programs with just a few attribute tags.  For more details, see [PostSharp Tips & Tricks](http://www.gibraltarsoftware.com/Support/Loupe/Documentation/WebFrame.html#ThirdParty_PostSharp_Tips.html).

For ideas on how to effectively use Loupe and PostSharp together see 
[Using PostSharp with Loupe - Best Practices](http://www.gibraltarsoftware.com/Support/Loupe/Documentation/WebFrame.html#ThirdParty_PostSharp_BestPractices.html).

Building the Agent
------------------

This project is designed for use with Visual Studio 2012 with NuGet package restore enabled.
When you build it the first time it will retrieve dependencies from NuGet.

PostSharp 3.0 or later are required to build (or use) this library.  PostSharp should be
automatically installed via NuGet if it isn't already present.

Contributing
------------

Feel free to branch this project and contribute a pull request to the development branch. 
If your changes are incorporated into the master version they'll be published out to NuGet for
everyone to use!
