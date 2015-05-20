using System;
using System.Net.Http;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.Owin;
using Microsoft.Owin.Security;

namespace Owin.Security.CAS
{
    public class Cas1ValidateTicketValidator : ICasTicketValidator
    {
        private readonly CasAuthenticationOptions _options;

        public Cas1ValidateTicketValidator(CasAuthenticationOptions options)
        {
            _options = options;
        }

        public async Task<AuthenticationTicket> ValidateTicket(IOwinRequest request, IOwinContext context, HttpClient httpClient,
            string ticket, AuthenticationProperties properties, string service)
        {
            // Now, we need to get the ticket validated
            string validateUrl = _options.CasServerUrlBase + "/validate" +
                                 "?service=" + service +
                                 "&ticket=" + Uri.EscapeDataString(ticket);

            HttpResponseMessage response = await httpClient.GetAsync(validateUrl, request.CallCancelled);

            response.EnsureSuccessStatusCode();
            string responseBody = await response.Content.ReadAsStringAsync();


            String validatedUserName = null;
            var responseParts = responseBody.Split('\n');
            if (responseParts.Length >= 2 && responseParts[0] == "yes")
                validatedUserName = responseParts[1];

            if (!String.IsNullOrEmpty(validatedUserName))
            {
                var identity = new ClaimsIdentity(_options.AuthenticationType);
                identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, validatedUserName, "http://www.w3.org/2001/XMLSchema#string", _options.AuthenticationType));
                identity.AddClaim(new Claim(ClaimTypes.Name, validatedUserName, "http://www.w3.org/2001/XMLSchema#string", _options.AuthenticationType));

                var authenticatedContext = new CasAuthenticatedContext(context, identity, properties);

                await _options.Provider.Authenticated(authenticatedContext);

                return new AuthenticationTicket(authenticatedContext.Identity, authenticatedContext.Properties);
            }

            return new AuthenticationTicket(null, properties);
        }
    }
}