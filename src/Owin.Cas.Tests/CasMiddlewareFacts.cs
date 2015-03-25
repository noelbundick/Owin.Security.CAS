using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using Microsoft.Owin;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Testing;
using Shouldly;
using Xunit;

namespace Owin.Cas.Tests
{
    public class CasMiddlewareFacts
    {
        private const string CookieAuthenticationType = "Cookie";

        [Fact]
        public async Task ChallengeWillTriggerRedirection()
        {
            var server = CreateServer(new CasAuthenticationOptions
            {
                CasServerUrlBase = "https://cas-dev.tamu.edu/cas"
            });
            var transaction = await SendAsync(server, "https://example.com/challenge");
            transaction.Response.StatusCode.ShouldBe(HttpStatusCode.Redirect);
            var location = transaction.Response.Headers.Location.ToString();
            location.ShouldContain("https://cas-dev.tamu.edu/cas/login?service=https://example.com/signin-cas");
            location.ShouldContain("?state=");
        }

        [Fact]
        public async Task ChallengeWillSetCorrelationCookie()
        {
            var server = CreateServer(new CasAuthenticationOptions
            {
                CasServerUrlBase = "https://cas-dev.tamu.edu/cas"
            });
            var transaction = await SendAsync(server, "https://example.com/challenge");
            Console.WriteLine(transaction.SetCookie);
            transaction.SetCookie.Single().ShouldContain(".AspNet.Correlation.CAS=");
        }

        [Fact]
        public async Task ChallengeWillTriggerApplyRedirectEvent()
        {
            var options = new CasAuthenticationOptions
            {
                CasServerUrlBase = "https://cas-dev.tamu.edu/cas",
                Provider = new CasAuthenticationProvider
                {
                    OnApplyRedirect = context => context.Response.Redirect(context.RedirectUri + "&custom=test")
                }
            };
            var server = CreateServer(options);
            var transaction = await SendAsync(server, "https://example.com/challenge");
            transaction.Response.StatusCode.ShouldBe(HttpStatusCode.Redirect);
            var query = transaction.Response.Headers.Location.Query;
            query.ShouldContain("custom=test");
        }

        [Fact]
        public async Task ReplyPathWithoutStateQueryStringWillBeRejected()
        {
            var options = new CasAuthenticationOptions()
            {
                CasServerUrlBase = "https://cas-dev.tamu.edu/cas"
            };
            var server = CreateServer(options);
            var transaction = await SendAsync(server, "https://example.com/signin-cas?code=TestCode");
            transaction.Response.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
        }



        [Fact]
        public async Task ReplyPathWillAuthenticateValidTicketAndState()
        {
            var options = new CasAuthenticationOptions
            {
                CasServerUrlBase = "https://cas-dev.tamu.edu/cas",
                BackchannelHttpHandler = new TestHttpMessageHandler
                {
                    Sender = req =>
                    {
                        if (req.RequestUri.AbsoluteUri.StartsWith("https://cas-dev.tamu.edu/cas/serviceValidate?service="))
                        {
                            var res = new HttpResponseMessage(HttpStatusCode.OK);
                            var text =
@"<cas:serviceResponse xmlns:cas='http://www.yale.edu/tp/cas'>
    <cas:authenticationSuccess>
        <cas:user>testuser</cas:user>
        <cas:attributes>
            
            <cas:tamuEduPersonNetID>testuser</cas:tamuEduPersonNetID>
            
            <cas:tamuEduPersonUIN>111001111</cas:tamuEduPersonUIN>
            
            <cas:uid>5c62dae8b85c9dfa1417a43ceffc5926</cas:uid>
            
        </cas:attributes>
        
    </cas:authenticationSuccess>
</cas:serviceResponse>
";
                            res.Content = new StringContent(text, Encoding.UTF8, "text/html");
                            return Task.FromResult(res);
                        }

                        return null;
                    }
                }
            };
            var server = CreateServer(options);
            var properties = new AuthenticationProperties();
            var correlationKey = ".AspNet.Correlation.CAS";
            var correlationValue = "TestCorrelationId";
            properties.Dictionary.Add(correlationKey, correlationValue);
            var state = options.StateDataFormat.Protect(properties);
            var transaction = await SendAsync(server,
                "https://example.com/signin-cas?&state=" + Uri.EscapeDataString(state) + "&ticket=12345",
                correlationKey + "=" + correlationValue);

            var authCookie = transaction.AuthenticationCookieValue;
            transaction = await SendAsync(server, "https://example.com/me", authCookie);
            transaction.Response.StatusCode.ShouldBe(HttpStatusCode.OK);
            transaction.FindClaimValue(ClaimTypes.Name).ShouldBe("testuser");
            transaction.FindClaimValue(ClaimTypes.NameIdentifier).ShouldBe("testuser");
            transaction.FindClaimValue("tamuEduPersonNetID").ShouldBe("testuser");
            transaction.FindClaimValue("tamuEduPersonUIN").ShouldBe("111001111");
            transaction.FindClaimValue("uid").ShouldBe("5c62dae8b85c9dfa1417a43ceffc5926");
        }

        private static TestServer CreateServer(CasAuthenticationOptions options, Func<IOwinContext, Task> testpath = null)
        {
            return TestServer.Create(app =>
            {
                app.Properties["host.AppName"] = "Microsoft.Owin.Security.Tests";
                app.UseCookieAuthentication(new CookieAuthenticationOptions
                {
                    AuthenticationType = CookieAuthenticationType
                });
                options.SignInAsAuthenticationType = CookieAuthenticationType;
                app.UseCasAuthentication(options);
                app.Use(async (context, next) =>
                {
                    IOwinRequest req = context.Request;
                    IOwinResponse res = context.Response;
                    if (req.Path == new PathString("/challenge"))
                    {
                        context.Authentication.Challenge("CAS");
                        res.StatusCode = 401;
                    }
                    else if (req.Path == new PathString("/me"))
                    {
                        Describe(res, new AuthenticateResult(req.User.Identity, new AuthenticationProperties(), new AuthenticationDescription()));
                    }
                    else if (testpath != null)
                    {
                        await testpath(context);
                    }
                    else
                    {
                        await next();
                    }
                });
            });
        }

        private static async Task<Transaction> SendAsync(TestServer server, string uri, string cookieHeader = null)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, uri);
            if (!string.IsNullOrEmpty(cookieHeader))
            {
                request.Headers.Add("Cookie", cookieHeader);
            }
            var transaction = new Transaction
            {
                Request = request,
                Response = await server.HttpClient.SendAsync(request),
            };
            if (transaction.Response.Headers.Contains("Set-Cookie"))
            {
                transaction.SetCookie = transaction.Response.Headers.GetValues("Set-Cookie").ToList();
            }
            transaction.ResponseText = await transaction.Response.Content.ReadAsStringAsync();

            if (transaction.Response.Content != null &&
                transaction.Response.Content.Headers.ContentType != null &&
                transaction.Response.Content.Headers.ContentType.MediaType == "text/xml")
            {
                transaction.ResponseElement = XElement.Parse(transaction.ResponseText);
            }
            return transaction;
        }

        private static void Describe(IOwinResponse res, AuthenticateResult result)
        {
            res.StatusCode = 200;
            res.ContentType = "text/xml";
            var xml = new XElement("xml");
            if (result != null && result.Identity != null)
            {
                xml.Add(result.Identity.Claims.Select(claim => new XElement("claim", new XAttribute("type", claim.Type), new XAttribute("value", claim.Value))));
            }
            if (result != null && result.Properties != null)
            {
                xml.Add(result.Properties.Dictionary.Select(extra => new XElement("extra", new XAttribute("type", extra.Key), new XAttribute("value", extra.Value))));
            }
            using (var memory = new MemoryStream())
            {
                using (var writer = new XmlTextWriter(memory, Encoding.UTF8))
                {
                    xml.WriteTo(writer);
                }
                res.Body.Write(memory.ToArray(), 0, memory.ToArray().Length);
            }
        }

        private class TestHttpMessageHandler : HttpMessageHandler
        {
            public Func<HttpRequestMessage, Task<HttpResponseMessage>> Sender { get; set; }

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, System.Threading.CancellationToken cancellationToken)
            {
                if (Sender != null)
                {
                    return await Sender(request);
                }

                return null;
            }
        }

        private class Transaction
        {
            public HttpRequestMessage Request { get; set; }
            public HttpResponseMessage Response { get; set; }

            public IList<string> SetCookie { get; set; }

            public string ResponseText { get; set; }
            public XElement ResponseElement { get; set; }

            public string AuthenticationCookieValue
            {
                get
                {
                    if (SetCookie != null && SetCookie.Count > 0)
                    {
                        var authCookie = SetCookie.SingleOrDefault(c => c.Contains(".AspNet.Cookie="));
                        if (authCookie != null)
                        {
                            return authCookie.Substring(0, authCookie.IndexOf(';'));
                        }
                    }

                    return null;
                }
            }

            public string FindClaimValue(string claimType)
            {
                XElement claim = ResponseElement.Elements("claim").SingleOrDefault(elt => elt.Attribute("type").Value == claimType);
                if (claim == null)
                {
                    return null;
                }
                return claim.Attribute("value").Value;
            }
        }
    }
}
