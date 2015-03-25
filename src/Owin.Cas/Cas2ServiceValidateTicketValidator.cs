using System;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.Owin;
using Microsoft.Owin.Security;

namespace Owin.Cas
{
    public class Cas2ServiceValidateTicketValidator : ICasTicketValidator
    {
        private readonly XNamespace _ns = "http://www.yale.edu/tp/cas";

        public async Task<AuthenticationTicket> ValidateTicket(CasAuthenticationOptions options, IOwinRequest request, IOwinContext context, HttpClient httpClient,
            string ticket, AuthenticationProperties properties, string service)
        {
            // Now, we need to get the ticket validated
            string validateUrl = options.CasServerUrlBase + "/serviceValidate" +
                                 "?service=" + service +
                                 "&ticket=" + Uri.EscapeDataString(ticket);

            HttpResponseMessage response = await httpClient.GetAsync(validateUrl, request.CallCancelled);

            response.EnsureSuccessStatusCode();
            string responseBody = await response.Content.ReadAsStringAsync();
            XDocument doc = XDocument.Parse(responseBody);

            var serviceResponse = doc.Element(_ns + "serviceResponse");
            var successNode = serviceResponse.Element(_ns + "authenticationSuccess");
            if (successNode != null)
            {
                var userNode = successNode.Element(_ns + "user");
                string validatedUserName = null;
                if (userNode != null)
                    validatedUserName = userNode.Value;

                if (!string.IsNullOrEmpty(validatedUserName))
                {
                    var identity = BuildIdentity(options, validatedUserName, successNode);

                    var authenticatedContext = new CasAuthenticatedContext(context, identity, properties);

                    await options.Provider.Authenticated(authenticatedContext);

                    return new AuthenticationTicket(authenticatedContext.Identity, authenticatedContext.Properties);
                }
            }

            return new AuthenticationTicket(null, properties);
        }

        private ClaimsIdentity BuildIdentity(CasAuthenticationOptions options, string username, XElement successNode)
        {
            var identity = new ClaimsIdentity(options.AuthenticationType, options.NameClaimType, ClaimTypes.Role);
            identity.AddClaim(new Claim(ClaimTypes.Name, username, "http://www.w3.org/2001/XMLSchema#string", options.AuthenticationType));

            var attributesNode = successNode.Element(_ns + "attributes");
            if (attributesNode != null)
            {
                foreach (var element in attributesNode.Elements())
                {
                    identity.AddClaim(new Claim(element.Name.LocalName, element.Value));
                }
            }

            string identityValue = username;
            if (options.NameIdentifierAttribute != null && attributesNode != null)
            {
                var identityAttribute = attributesNode.Elements().FirstOrDefault(x => x.Name.LocalName == options.NameIdentifierAttribute);
                if (identityAttribute == null)
                    throw new ApplicationException(string.Format("Identity attribute [{0}] not found for user: {1}", options.NameIdentifierAttribute, username));

                identityValue = identityAttribute.Value;
            }
            identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, identityValue, "http://www.w3.org/2001/XMLSchema#string", options.AuthenticationType));

            return identity;
        }
    }
}