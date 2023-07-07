﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Falu.Properties {
    using System;
    
    
    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option, or rebuild your VS project.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "17.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    internal class Resources {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Resources() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("Falu.Properties.Resources", typeof(Resources).Assembly);
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
        ///   Looks up a localized string similar to Authentication information is missing.
        ///Either pass an API key via --apikey option or perform login via &apos;falu login&apos; command..
        /// </summary>
        internal static string AuthenticationInformationMissing {
            get {
                return ResourceManager.GetString("AuthenticationInformationMissing", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Either you or the API Key provided does not have permissions to perform this operation.
        ///Consult your Falu dashboard to confirm the permissions of the API key or ask your administrator to grant you permissions..
        /// </summary>
        internal static string Forbidden403Message {
            get {
                return ResourceManager.GetString("Forbidden403Message", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Seems there was a problem reaching api.falu.io. This likely a transient internet connectivity issue on this computer or its internet provider.
        ///Try again after a short while.
        ///If it persists, check your internet connection.
        ///You can also check https://status.falu.io to see if there are any server related issues..
        /// </summary>
        internal static string HostNotFoundExceptionFormat {
            get {
                return ResourceManager.GetString("HostNotFoundExceptionFormat", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Server encountered an error. You can try again later or contact support at https://falu.io/support.
        /// </summary>
        internal static string InternalServerError500Message {
            get {
                return ResourceManager.GetString("InternalServerError500Message", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Value for &apos;{0}&apos; is not a valid E.164 phone number: &apos;{1}&apos;.
        /// </summary>
        internal static string InvalidE164PhoneNumber {
            get {
                return ResourceManager.GetString("InvalidE164PhoneNumber", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Value &apos;{0}&apos; is not a valid HTTP request path. It should start with &apos;/v1/&apos;..
        /// </summary>
        internal static string InvalidHttpRequestPath {
            get {
                return ResourceManager.GetString("InvalidHttpRequestPath", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Value &apos;{0}&apos; is not a valid HTTP status code. It should range between 200 and 599 (inclusive)..
        /// </summary>
        internal static string InvalidHttpStatusCode {
            get {
                return ResourceManager.GetString("InvalidHttpStatusCode", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Value for &apos;{0}&apos; has invalid format. The expected pattern is &apos;{1}&apos;.
        /// </summary>
        internal static string InvalidInputValue {
            get {
                return ResourceManager.GetString("InvalidInputValue", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Value for &apos;{0}&apos; is not a valid JSON representation. Ensure it is a valid JSON object..
        /// </summary>
        internal static string InvalidJsonInputValue {
            get {
                return ResourceManager.GetString("InvalidJsonInputValue", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Login request failed! {0}.
        /// </summary>
        internal static string LoginFailedFormat {
            get {
                return ResourceManager.GetString("LoginFailedFormat", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Login request failed with code: {0}.
        /// </summary>
        internal static string LoginFailedWithCodeFormat {
            get {
                return ResourceManager.GetString("LoginFailedWithCodeFormat", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Error Code: {0} (See docs at https://falu.io/docs/error-codes#{0}).
        /// </summary>
        internal static string ProblemDetailsErrorCodeFormat {
            get {
                return ResourceManager.GetString("ProblemDetailsErrorCodeFormat", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Error Detail: {0}.
        /// </summary>
        internal static string ProblemDetailsErrorDetailFormat {
            get {
                return ResourceManager.GetString("ProblemDetailsErrorDetailFormat", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Errors: {0}.
        /// </summary>
        internal static string ProblemDetailsErrorsFormat {
            get {
                return ResourceManager.GetString("ProblemDetailsErrorsFormat", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Refreshing access token failed. Most likely the refresh token is no longer valid.
        ///You can clear authentication information using the command:
        ///falu config clear auth.
        /// </summary>
        internal static string RefreshingAccessTokenFailed {
            get {
                return ResourceManager.GetString("RefreshingAccessTokenFailed", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Request to Falu servers failed..
        /// </summary>
        internal static string RequestFailedHeader {
            get {
                return ResourceManager.GetString("RequestFailedHeader", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Request Id: {0}.
        /// </summary>
        internal static string RequestIdFormat {
            get {
                return ResourceManager.GetString("RequestIdFormat", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Whether to skip update checks.
        ///This value can also be set globally using &apos;falu config set skip-update-check true&apos;
        ///This option overrides any value set in the configuration..
        /// </summary>
        internal static string SkipUpdateCheckOptionDescription {
            get {
                return ResourceManager.GetString("SkipUpdateCheckOptionDescription", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Message destinations (to) cannot exceed {0:n0} in one request..
        /// </summary>
        internal static string TooManyMessagesToBeSent {
            get {
                return ResourceManager.GetString("TooManyMessagesToBeSent", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Trace Identifier: {0}.
        /// </summary>
        internal static string TraceIdentifierFormat {
            get {
                return ResourceManager.GetString("TraceIdentifierFormat", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The API key provided cannot authenticate your request.
        ///Confirm you have provided the right value and that it matches any other related values.
        ///For example, do not use a key that belongs to another workspace or mix live and test mode keys.
        ///If you are not using an API key ensure you have logged correctly..
        /// </summary>
        internal static string Unauthorized401ErrorMessage {
            get {
                return ResourceManager.GetString("Unauthorized401ErrorMessage", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Unhandled exception: {0}.
        /// </summary>
        internal static string UnhandledExceptionFormat {
            get {
                return ResourceManager.GetString("UnhandledExceptionFormat", resourceCulture);
            }
        }
    }
}
