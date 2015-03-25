using System;
using System.Net.Http;
using System.Security.Claims;
using Microsoft.Owin;
using Microsoft.Owin.Security;

namespace Owin.Cas
{
    /// <summary>
    /// Configuration options for <see cref="CasAuthenticationMiddleware"/>
    /// </summary>
    public class CasAuthenticationOptions : AuthenticationOptions
    {
        /// <summary>
        /// Initializes a new <see cref="CasAuthenticationOptions"/>
        /// </summary>
        public CasAuthenticationOptions()
            : base("CAS")
        {
            Caption = "CAS";
            CallbackPath = new PathString("/signin-cas");
            AuthenticationMode = AuthenticationMode.Passive;
            BackchannelTimeout = TimeSpan.FromSeconds(60);
            TicketValidator = new Cas2ServiceValidateTicketValidator();
            NameClaimType = ClaimTypes.Name;
        }

        /// <summary>
        /// Gets or sets timeout value in milliseconds for back channel communications with Cas.
        /// </summary>
        /// <value>
        /// The back channel timeout.
        /// </value>
        public TimeSpan BackchannelTimeout { get; set; }

        /// <summary>
        /// The HttpMessageHandler used to communicate with Cas.
        /// This cannot be set at the same time as BackchannelCertificateValidator unless the value 
        /// can be downcast to a WebRequestHandler.
        /// </summary>
        public HttpMessageHandler BackchannelHttpHandler { get; set; }

        /// <summary>
        /// Get or sets the text that the user can display on a sign in user interface.
        /// </summary>
        public string Caption
        {
            get { return Description.Caption; }
            set { Description.Caption = value; }
        }

        /// <summary>
        /// The request path within the application's base path where the user-agent will be returned.
        /// The middleware will process this request when it arrives.
        /// Default value is "/signin-cas".
        /// </summary>
        public PathString CallbackPath { get; set; }

        /// <summary>
        /// Gets or sets the name of another authentication middleware which will be responsible for actually issuing a user <see cref="System.Security.Claims.ClaimsIdentity"/>.
        /// </summary>
        public string SignInAsAuthenticationType { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="ICasAuthenticationProvider"/> used to handle authentication events.
        /// </summary>
        public ICasAuthenticationProvider Provider { get; set; }

        /// <summary>
        /// Gets or sets the type used to secure data handled by the middleware.
        /// </summary>
        public ISecureDataFormat<AuthenticationProperties> StateDataFormat { get; set; }

        /// <summary>
        /// The base url of the CAS server
        /// </summary>
        /// <example>https://cas.example.com/cas</example>
        public string CasServerUrlBase { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="ICasTicketValidator"/> used to validate tickets from CAS 
        /// </summary>
        public ICasTicketValidator TicketValidator { get; set; }

        /// <summary>
        /// If set, and using the CAS 2 payload, the ticket validator will set the NameClaimType to
        /// the specified CAS attribute rather than using the default Name claim
        /// </summary>
        public string NameClaimType { get; set; }

        /// <summary>
        /// If set, and using the CAS 2 payload, the ticket validator use the specified CAS attribute as
        /// the NameIdentifier claim, which is used to associate external logins
        /// </summary>
        public string NameIdentifierAttribute { get; set; }
    }
}