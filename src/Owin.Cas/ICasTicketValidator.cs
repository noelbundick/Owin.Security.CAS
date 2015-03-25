using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Owin;
using Microsoft.Owin.Security;

namespace Owin.Cas
{
    public interface ICasTicketValidator
    {
        Task<AuthenticationTicket> ValidateTicket(CasAuthenticationOptions options, IOwinRequest request, IOwinContext context, HttpClient httpClient, 
            string ticket, AuthenticationProperties properties, string service);
    }
}
