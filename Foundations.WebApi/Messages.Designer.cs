﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Spritely.Foundations.WebApi {
    using System;
    
    
    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option, or rebuild your VS project.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "4.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    internal class Messages {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Messages() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("Spritely.Foundations.WebApi.Messages", typeof(Messages).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///   Overrides the current thread's CurrentUICulture property for all
        ///   resource lookups using this strongly typed resource class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Application started..
        /// </summary>
        internal static string Application_Started {
            get {
                return ResourceManager.GetString("Application_Started", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Could not load JwtBearerAuthenticationSettings from the container; please ensure that the file exists in your configuration and that UseSettingsContainerInitializer has been called or explicitly supply the value when calling UseJwtBearerAuthentication..
        /// </summary>
        internal static string Exception_UseJwtBearerAuthentication_NoSettingsProvided {
            get {
                return ResourceManager.GetString("Exception_UseJwtBearerAuthentication_NoSettingsProvided", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Could not locate assembly for {0} when attempting to load settings from configuration; to fix make sure you have a type named {0} and add typeof({0}).Assembly to the list of parameters supplied to UseSettingsContainerInitializer if the assembly is not already loaded into memory already by the main process..
        /// </summary>
        internal static string Exception_UseSettingsContainerInitializer_NoType {
            get {
                return ResourceManager.GetString("Exception_UseSettingsContainerInitializer_NoType", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Press any key to quit..
        /// </summary>
        internal static string Quit {
            get {
                return ResourceManager.GetString("Quit", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Web server is running on {0}.
        /// </summary>
        internal static string Web_Server_Running {
            get {
                return ResourceManager.GetString("Web_Server_Running", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Web server on {0} was terminated by a user..
        /// </summary>
        internal static string Web_Server_Terminated {
            get {
                return ResourceManager.GetString("Web_Server_Terminated", resourceCulture);
            }
        }
    }
}
