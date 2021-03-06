# Spritely.Foundations.WebApi
Provides a default starting point for setting up an Owin WebApi service which includes Its.Configuration and Its.Log with a set of classes that preconfigure numerous defaults. This service can run with or without IIS, however the usage is slightly different as explained below. Please note that package author only runs services as console applications now so it is possible that newer features will not work correctly under IIS. If Web Api has sufficiently abstracted the host then this shouldn't be a concern.

## Usage
Add the NuGet package to your project which will provide it with all the necessary dependencies. Create a Startup.cs file with the following content:

```csharp
namespace MyNamespace
{
    using Owin;
    using Spritely.Foundations.WebApi;

    // Startup is the conventional/recommended class name for OWIN applications
    public class Startup
    {
        private static void InitializeContainer(Container container)
        {
            // Do any setup of your container here
            // container.Register<IMyType, MyType>();
        }

        public void Configuration(IAppBuilder app)
        {
            Start.Initialize();
            
            // Lets the application know how to register any custom types you have
            app.UseContainerInitializer(InitializeContainer) // optional
                // Sets up the application to pull values from Its.Configuration files
                .UseSettingsContainerInitializer()
                // If you have the configuration class present in your .config folder and the corresponding class cannot be found,
                // you can force assemblies to be loaded (and thus resolvable) by adding parameters as follows:
                //.UseSettingsContainerInitializer(typeof(MyDatabaseSettings).Assembly, typeof(MyAuthSettings).Assembly)
                
                // Have all http requests and http responses logged to Its.Log (which by default go to the .NET trace logger)
                .UseRequestAndResponseLogging()

                // Enable CORS for incoming requests
                // Options can be passed in or by default will be read from HostingSettings
                .UseCors()

                // This will wire up JwtBearerAuthentication from your Its.Configuration files (see example below)
                .UseJwtBearerAuthentication()

                // This will enable gzip and deflate compression for all responses over 4096 bytes in length
                .UseGzipDeflateCompression(compressResponsesOver: 4096)
                
                // Wire WebApi into OWIN pipeline and pass in any callback functions for setting up the InitializeHttpConfiguration
                // DefaultWebApiConfig.InitializeHttpConfiguration provides standard controller route mapping and sets Its.Log
                // messages to write to the .NET trace logger 
                .UseWebApiWithHttpConfigurationInitializers(DefaultWebApiConfig.InitializeHttpConfiguration);
        }

        // This is only necessary for a console application
        public static void Main(string[] args)
        {
            Start.Console<Startup>();
        }
    }
}
```

### App.config
Next, you will need to setup your App.config for a console hosted application.

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

Console application projects are setup correctly by default by Visual Studio. However, if you are modifying an existing project you should check your .csproj file to ensure the following property is set correctly or you will have to also add binding redirects to your app.config file (similar to web.config below):

```xml
    <AutoGenerateBindingRedirects>true</AutoGenerateBindingRedirects>
```

### Web.config
Or you will need Web.config for an IIS hosted application.
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
				<bindingRedirect oldVersion="0.0.0.0-9.0.0.0" newVersion="9.0.0.0"/>
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

### HostingSettings.json
Finally, create a .config folder in your application with a subfolder such as Local for your environment (must match a name in your configuration's appSettings Its.Configuration.Settings.Precendence value) and then add a file called HostingSettings.json with the following contents (change the urls as needed):

```json
{
    "urls": [ "https://localhost:443" ],
    "cors": {
        "supportsCredentials": true,
        "preflightMaxAge": 3600,
        "origins": [
            "https://your.website.com",
            "https://another.website.com"
        ],
        "headers": [ "*" ],
        "methods": [ "*" ]
    }
}
```

### JwtBearerAuthenticationSettings.json
If you are using JWT bearer authentication then you will also need a file called JwtBearerAuthenticationSettings.json with contents similar to the following:

```json
{
    "allowedClients": [
        "some.clientid.often.your.domain.name.com",
        "another.clientid.at.your.domain.com"
    ],
    "allowedServers": [
        {
            "issuer": "http://your.jwt.oauth.server.com",
            "secret": "SomeBase64UrlEncodedSecretTheServerUsesToSignTokens"
        }
    ]
}
```

JwtBearerAuthenticationSettings.json also permits the use of X509 certificates. If present the certificate's public key will be used to decrypt the JWE (Encrypted JWT) on this server. Certificates can be loaded via a file path (there is also an optional basePath property that defaults to the current directory of the console application if unset):

```json
{
    "allowedClients": [ /* ... */ ],
    "allowedServers": [ /* ... */ ],
    "relativeFileCertificate": {
        "relativeFilePath": "/Certificates/MyCertificate.pfx",
        "password": "my-password",
        "keyStorageFlags": "machineKeySet, exportable"
    }
}
```

or via the thumbprint of a certificate in the Windows Store:

```json
{
    "allowedClients": [ /* ... */ ],
    "allowedServers": [ /* ... */ ],
    "storeCertificate": {
        "certificateThumbprint": "aa1234...."
    }
}
```

Only a single certificate is permitted and it can only be loaded from a single source. Additional optional properties may be supplied to provide additional details for locating a certificate: storeName (defaults to "my"), storeLocation (defaults to "localMachine"), and certificateValidityRequired (defaults to true).

If your application is a console application you should be able to simply build and run it now, or launch it in the Visual Studio debugger. If your application is hosted on IIS, you should be able to point IIS at your development directory with appropriate permissions and build it, point your browser at it, and attach a debugger to it.

#### Dual Builds for IIS and Console
If you would like to have both, then the recommended approach is to first setup the console application. Then create another project for the website version, use a project reference to your console application, and add a NuGet reference to Microsoft.Owin.Host.SystemWeb (this is a runtime dependency only so MSBuild won't copy it to the output folder from the console project dependency without the explicit reference like this). We know you prefer a single project, but because the build outputs in .NET projects are different (/bin/Debug or /bin/Release for console applications/dlls and /bin for websites) they simply do not coexist well on disk unless you want to modify your build output directories. Another problem is that console applications use app.config files while websites use web.config files. While the content we care about is virtually identical they cause a code duplication problem. Again, something you could have your build solve but we don't try to solve that for you.

Despite the build annoyances with these two projects it is still a user friendly solution once you get it setup. With the two project configuration you can run, test, and debug the console and web projects side-by-side locally and just deploy the one you care about. Just do as little as possible in your web application and instead do all the work in your console application and you should get the best of both worlds. Even the .config file will copy from the console project and duplicate into your web project. If you need them to be different though, just add a .config folder in your web project and it will override any files copied from the console application because by default its files will be copied after the files of any dependent assemblies.

## Getting Started
When first initializing your project we recommend you start with very basic code and add on one aspect at a time until you get all the features working.  You can start as follows:

```csharp
// Startup.cs
namespace MyNamespace
{
    using Owin;
    using Spritely.Foundations.WebApi;

    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            Start.Initialize();
            
            // DefaultWebApiConfig.InitializeHttpConfiguration will not work until we get HostingSettings into the container...
            app.UseWebApiWithHttpConfigurationInitializers(DefaultWebApiConfig.InitializeHttpConfiguration);
        }

        // This is only necessary for a console application
        public static void Main(string[] args)
        {
            Start.Console<Startup>();
        }
    }
}

// TestController.cs
namespace MyNamespace
{
    using System.Web.Http;

    public class TestController : ApiController
    {
        public IHttpActionResult Get()
        {
            return this.Ok(
                new
                {
                    Success = true
                });
        }
    }
}
```

You also need your App.config/Web.config setup as above. UseWebApiWithHttpConfigurationInitializers will setup SimpleInjector dependency injection and call back any initializers passed as parameters to build up an HttpConfiguration for Web Api and then it will start Web Api with that configuration. DefaultWebApiConfig.InitializeHttpConfiguration will return a configuration with a default Web Api router and setup all controllers to return JSON responses. It will also attempt to retrieve HostingSettings from the container and use that to determine how to startup the service. So you need to add that file to your project (see above) and put it into the container. To do so modify your Startup.cs class as follows:

```csharp
// Startup.cs
public class Startup
{
    // Consider putting this method into its own class.
    private static void InitializeContainer(Container container)
    {
        container.RegisterSingleton(Settings.Get<HostingSettings>);
    }

    public void Configuration(IAppBuilder app)
    {
        Start.Initialize();
        
        app.UseContainerInitializer(InitializeContainer)
            .UseWebApiWithHttpConfigurationInitializers(DefaultWebApiConfig.InitializeHttpConfiguration);
    }

    // rest of code ....
}
```
That's it. Make sure HostingSettings.json is copied into your output directory and now when starting it should automatically load this .json into the HostingSettings class, place it into the container and start the web api where it will be extracted and used to determine the initialize the service. This minimal startup code and you should be able to execute a GET request to the /test url when running. All classes ending in 'Controller' are automatically discovered and made available.

### Console Appliations
To startup your console application outside of the debugger you will need to setup permissions to host on the url specified in HostingSettings.json for the user running the process or web api will be unable to start the host. You will also need to register a certificate if you are trying to host over SSL.

#### Setup Windows to Host Web Api from a Console app
To give user permissions to host on a url (trailing slash on url required):

```shell
D:\Source>netsh http add urlacl url=https://api.mydomain.com:443/ user=MACHINEORDOMAINNAME\user

URL reservation successfully added
```

To add certificate for a particular host:
```shell
D:\Source>netsh http add sslcert hostnameport=api.mydomain.com:443 appid={12345678-db90-4b66-8b01-88f7af2e36bf} certhash=<somehash> certstorename=MY
```

To see what is installed:
```shell
D:\Source>netsh http show sslcert hostnameport=api.mydomain.com:443

SSL Certificate bindings:
-------------------------

    Hostname:port                : api.mydomain.com:443
    Certificate Hash             : <certificate hash>
    Application ID               : {12345678-db90-4b66-8b01-88f7af2e36bf}
    Certificate Store Name       : MY
    Verify Client Certificate Revocation : Enabled
    Verify Revocation Using Cached Client Certificate Only : Disabled
    Usage Check                  : Enabled
    Revocation Freshness Time    : 0
    URL Retrieval Timeout        : 0
    Ctl Identifier               : (null)
    Ctl Store Name               : (null)
    DS Mapper Usage              : Disabled
    Negotiate Client Certificate : Disabled
    Reject Connections           : Disabled
```

To delete certificate:

```shell
D:\Source\Certificates>netsh http delete sslcert hostnameport=monitoring.api.local.cometrics.com:443

SSL Certificate successfully deleted
```

### Alternative Code - UseSettingsContainerInitializer
UseSettingsContainerInitializer can be used instead of a custom container initializer (or you can use both). By using this method the code will try to load every file in the config folder and find a matching class to load the .json into. If the assembly a settings.json file is not loaded yet then that assembly can be passed as a parameter to this method (typeof(MySettingsFile).Assembly) to force it to load early.

```csharp
// Startup.cs
public class Startup
{
    // InitializeContainer removed

    public void Configuration(IAppBuilder app)
    {
        Start.Initialize();
        
        app.UseSettingsContainerInitializer()
            .UseWebApiWithHttpConfigurationInitializers(DefaultWebApiConfig.InitializeHttpConfiguration);
    }

    // rest of code ....
}
```
