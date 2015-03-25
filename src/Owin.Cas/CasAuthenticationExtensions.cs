using System;

namespace Owin.Cas
{
    public static class CasAuthenticationExtensions
    {
        /// <summary>
        /// Authenticate users using Cas
        /// </summary>
        /// <param name="app">The <see cref="IAppBuilder"/> passed to the configuration method</param>
        /// <param name="options">Middleware configuration options</param>
        /// <returns>The updated <see cref="IAppBuilder"/></returns>
        public static IAppBuilder UseCasAuthentication(this IAppBuilder app, CasAuthenticationOptions options)
        {
            if (app == null)
                throw new ArgumentNullException("app");
            if (options == null)
                throw new ArgumentNullException("options");

            app.Use(typeof(CasAuthenticationMiddleware), app, options);
            return app;
        }
    }
}