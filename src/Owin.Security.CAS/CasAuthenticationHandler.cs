using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.Owin;
using Microsoft.Owin.Infrastructure;
using Microsoft.Owin.Logging;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Infrastructure;

namespace Owin.Security.CAS
{
    internal class CasAuthenticationHandler : AuthenticationHandler<CasAuthenticationOptions>
    {
        private readonly ILogger _logger;
        private readonly HttpClient _httpClient;

        public CasAuthenticationHandler(HttpClient httpClient, ILogger logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public override async Task<bool> InvokeAsync()
        {
            if (Options.CallbackPath.HasValue && Options.CallbackPath == Request.Path)
                return await InvokeReturnPathAsync();

            return false;
        }

        protected override async Task<AuthenticationTicket> AuthenticateCoreAsync()
        {
            AuthenticationProperties properties = null;

            try
            {
                IReadableStringCollection query = Request.Query;

                properties = UnpackStateParameter(query);
                if (properties == null)
                {
                    _logger.WriteWarning("Invalid return state");
                    return null;
                }

                // Anti-CSRF
                if (!ValidateCorrelationId(properties, _logger))
                    return new AuthenticationTicket(null, properties);

                string ticket = GetTicketParameter(query);

                // No ticket
                if (String.IsNullOrEmpty(ticket))
                    return new AuthenticationTicket(null, properties);

                var validator = Options.TicketValidator;
                var service = Uri.EscapeDataString(BuildReturnTo(GetStateParameter(query)));
                return await validator.ValidateTicket(Request, Context, _httpClient, ticket, properties, service);
            }
            catch (Exception ex)
            {
                _logger.WriteError("Authentication failed", ex);
                return new AuthenticationTicket(null, properties);
            }
        }

        private static string GetStateParameter(IReadableStringCollection query)
        {
            IList<string> values = query.GetValues("state");
            if (values != null && values.Count == 1)
                return values[0];
            return null;
        }

        private AuthenticationProperties UnpackStateParameter(IReadableStringCollection query)
        {
            string state = GetStateParameter(query);
            if (state != null)
                return Options.StateDataFormat.Unprotect(state);
            return null;
        }

        private string BuildReturnTo(string state)
        {
            return Request.Scheme + "://" + Request.Host +
                RequestPathBase + Options.CallbackPath +
                "?state=" + Uri.EscapeDataString(state);
        }

        [SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times", Justification = "MemoryStream.Dispose is idempotent")]
        protected override Task ApplyResponseChallengeAsync()
        {
            if (Response.StatusCode != 401)
                return Task.FromResult<object>(null);

            AuthenticationResponseChallenge challenge = Helper.LookupChallenge(Options.AuthenticationType, Options.AuthenticationMode);

            if (challenge != null)
            {
                string requestPrefix = Request.Scheme + Uri.SchemeDelimiter + Request.Host;

                AuthenticationProperties properties = challenge.Properties;
                if (String.IsNullOrEmpty(properties.RedirectUri))
                    properties.RedirectUri = requestPrefix + Request.PathBase + Request.Path + Request.QueryString;

                // OAuth2 10.12 CSRF
                GenerateCorrelationId(properties);

                string returnTo = BuildReturnTo(Options.StateDataFormat.Protect(properties));

                string authorizationEndpoint =
                    Options.CasServerUrlBase + "/login" +
                    "?service=" + Uri.EscapeDataString(returnTo);

                if (properties.Dictionary.ContainsKey("renew") && properties.Dictionary["renew"] == "true")
                    authorizationEndpoint += "&renew=true";

                var redirectContext = new CasApplyRedirectContext(
                    Context, Options,
                    properties, authorizationEndpoint);
                Options.Provider.ApplyRedirect(redirectContext);
            }

            return Task.FromResult<object>(null);
        }

        public async Task<bool> InvokeReturnPathAsync()
        {
            AuthenticationTicket model = await AuthenticateAsync();
            if (model == null)
            {
                _logger.WriteWarning("Invalid return state, unable to redirect.");
                Response.StatusCode = 500;
                return true;
            }

            var context = new CasReturnEndpointContext(Context, model)
            {
                SignInAsAuthenticationType = Options.SignInAsAuthenticationType,
                RedirectUri = model.Properties.RedirectUri
            };
            model.Properties.RedirectUri = null;

            await Options.Provider.ReturnEndpoint(context);

            if (context.SignInAsAuthenticationType != null && context.Identity != null)
            {
                ClaimsIdentity signInIdentity = context.Identity;
                if (!string.Equals(signInIdentity.AuthenticationType, context.SignInAsAuthenticationType, StringComparison.OrdinalIgnoreCase))
                    signInIdentity = new ClaimsIdentity(signInIdentity.Claims, context.SignInAsAuthenticationType, signInIdentity.NameClaimType, signInIdentity.RoleClaimType);
                Context.Authentication.SignIn(context.Properties, signInIdentity);
            }

            if (!context.IsRequestCompleted && context.RedirectUri != null)
            {
                // add a redirect hint that sign-in failed in some way
                if (context.Identity == null)
                    context.RedirectUri = WebUtilities.AddQueryString(context.RedirectUri, "error", "access_denied");
                Response.Redirect(context.RedirectUri);
                context.RequestCompleted();
            }

            return context.IsRequestCompleted;
        }

        private static string GetTicketParameter(IReadableStringCollection query)
        {
            IList<string> values = query.GetValues("ticket");
            if (values != null && values.Count == 1)
                return values[0];
            return null;
        }
    }
}