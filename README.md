# Foundations.WebApi
Provides a default starting point for setting up an Owin WebApi service which includes Its.Configuration and Its.Log with a set of classes that preconfigure numerous defaults. This service can run with or without IIS, however the usage is slightly different as explained below.

## Usage
Add the NuGet package to your project which will provide it with all the necessary dependencies. Create a Startup.cs file with the following content:

```csharp
namespace MyNamespace
{
    using Owin;
    using Spritely.Foundations.WebApi;

    // Startup is the conventional/recommended class name for Owin applications
    public class Startup
    {
        // Common logic
        public static StartupConfiguration GetConfiguration()
        {
            var configuration = new StartupConfiguration
            {
                HttpConfigurationInitializers =
                {
                    DefaultWebApiConfig.InitializeHttpConfiguration
                }
            };

            return configuration;
        }

        // This is for an IIS hosted application
        public void Configuration(IAppBuilder appBuilder)
        {
            Start.Configuration(GetConfiguration(), appBuilder);
        }

        // This is only necessary for a console application
        public static void Main(string[] args)
        {
            Start.Console<Startup>(GetConfiguration());
        }
    }
}
```

Next, you will need to setup your App.config for a console hosted application or Web.config for an IIS hosted application.

### App.config
```xml
<configuration>
    <appSettings>
        <!-- Enable Its.Configuration and tell it where to source its settings files -->
        <add key="Its.Configuration.Settings.Precedence" value="Local|Common"/>
    </appSettings>
    <!-- By default Its.Log is configured to write to the Trace log so you can enable log
         output by using standard .NET tracing -->
    <system.diagnostics>
        <trace autoflush="true" indentsize="4">
            <listeners>
                <remove name="Default"/>
                <add name="TraceListener" type="System.Diagnostics.TextWriterTraceListener" initializeData="Application.log"/>
            </listeners>
        </trace>
    </system.diagnostics>
</configuration>
```

### Web.config
```xml
<?xml version="1.0"?>
<configuration>
    <appSettings>
        <!-- Enable Its.Configuration and tell it where to source its settings files -->
        <add key="Its.Configuration.Settings.Precedence" value="Local|Common" />
        <!-- Tell Owin where to find the Startup class -->
        <add key="owin:AppStartup" value="MyNamespace.Startup, MyAssemblyName"/>
    </appSettings>
    <!-- By default Its.Log is configured to write to the Trace log so you can enable log
        output by using standard .NET tracing -->
    <system.diagnostics>
        <trace autoflush="true" indentsize="4">
            <listeners>
                <remove name="Default" />
                <add name="TraceListener" type="System.Diagnostics.TextWriterTraceListener" initializeData="Web.log" />
            </listeners>
        </trace>
    </system.diagnostics>
    <!-- You will probably also need to do some .NET binding redirects because not all assemblies reference
         the same set of dependent dlls. Spritely is setup to use the latest versions of assemblies as much
         as possible. You can let Visual Studio fix these for you. At the time of writing this was the
         minimal set of binding redirects required to get an application functioning correctly -->
	<runtime>
		<assemblyBinding xmlns="urn:schemas-microsoft-com:asm.v1">
			<dependentAssembly>
				<assemblyIdentity name="Newtonsoft.Json" publicKeyToken="30AD4FE6B2A6AEED" culture="neutral"/>
				<bindingRedirect oldVersion="0.0.0.0-8.0.0.0" newVersion="8.0.0.0"/>
			</dependentAssembly>
			<dependentAssembly>
				<assemblyIdentity name="System.Web.Http" publicKeyToken="31BF3856AD364E35" culture="neutral"/>
				<bindingRedirect oldVersion="0.0.0.0-5.2.3.0" newVersion="5.2.3.0"/>
			</dependentAssembly>
			<dependentAssembly>
				<assemblyIdentity name="Microsoft.Owin" publicKeyToken="31BF3856AD364E35" culture="neutral"/>
				<bindingRedirect oldVersion="0.0.0.0-3.0.1.0" newVersion="3.0.1.0"/>
			</dependentAssembly>
		</assemblyBinding>
	</runtime>
</configuration>
```

Finally, create a .config folder in your application with a subfolder such as Local for your environment (must match a name in your configuration's appSettings Its.Configuration.Settings.Precendence value) and then add a file called HostingSettings.json with the following contents (change the Url value as needed):

```json
{
    "url": "https://localhost:443"
}
```

If your application is a console application you should be able to simply build and run it now, or launch it in the Visual Studio debugger. If your application is hosted on IIS, you should be able to point IIS at your development directory with appropriate permissions and build it, point your browser at it, and attach a debugger to it.

If you would like to have both, then the recommended approach is to first setup the console application. Then create another project for the website version, use a project reference to your console application, and add a NuGet reference to Microsoft.Owin.Host.SystemWeb (this is a runtime dependency only so MSBuild won't copy it to the output folder from the console project dependency without the explicit reference like this). We know you prefer a single project, but because the build outputs in .NET projects are different (/bin/Debug or /bin/Release for console applications/dlls and /bin for websites) they simply do not coexist well on disk unless you want to modify your build output directories. Another problem is that console applications use app.config files while websites use web.config files. While the content we care about is virtually identical they cause a code duplication problem. Again, something you could have your build solve but we don't try to solve that for you.

Despite the litany of build annoyances with these two projects it is still a user friendly solution once you get it setup. With the two project configuration you can run, test, and debug the console and web projects side-by-side locally and just deploy the one you care about. Just do as little as possible in your web application and instead do all the work in your console application and you should get the best of both worlds. Even the .config file will copy from the console project and duplicate into your web project. If you need them to be different though, just add a .config folder in your web project and it will override any files copied from the console application because by default its files will be copied after the files of any dependent assemblies.
