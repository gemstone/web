//******************************************************************************************************
//  IAuthenticationWebBuilder.cs - Gbtc
//
//  Copyright © 2025, Grid Protection Alliance.  All Rights Reserved.
//
//  Licensed to the Grid Protection Alliance (GPA) under one or more contributor license agreements. See
//  the NOTICE file distributed with this work for additional information regarding copyright ownership.
//  The GPA licenses this file to you under the MIT License (MIT), the "License"; you may not use this
//  file except in compliance with the License. You may obtain a copy of the License at:
//
//      http://opensource.org/licenses/MIT
//
//  Unless agreed to in writing, the subject software distributed under the License is distributed on an
//  "AS-IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. Refer to the
//  License for the specific language governing permissions and limitations.
//
//  Code Modification History:
//  ----------------------------------------------------------------------------------------------------
//  07/25/2025 - Stephen C. Wills
//       Generated original version of source code.
//
//******************************************************************************************************

using System;
using System.Threading.Tasks;
using Gemstone.Security.AuthenticationProviders;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;

namespace Gemstone.Web.Security;

/// <summary>
/// Represents a builder for Gemstone authentication web components.
/// </summary>
public interface IAuthenticationWebBuilder
{
    /// <summary>
    /// Adds the default middleware type for the given provider identity to the application's request pipeline.
    /// </summary>
    /// <param name="providerIdentity">The identity of the authentication provider</param>
    /// <returns>The authentication web builder.</returns>
    IAuthenticationWebBuilder UseProvider(string providerIdentity);

    /// <summary>
    /// Adds the default middleware type for the given provider identity to the application's request pipeline.
    /// </summary>
    /// <param name="providerIdentity">The identity of the authentication provider</param>
    /// <param name="pathMatch">The path on which requests will require authentication using this provider</param>
    /// <returns>The authentication web builder.</returns>
    IAuthenticationWebBuilder UseProvider(string providerIdentity, string pathMatch);

    /// <summary>
    /// Adds the default middleware type for the given provider identity to the application's request pipeline.
    /// </summary>
    /// <param name="providerIdentity">The identity of the authentication provider</param>
    /// <param name="pathMatch">The path on which requests will require authentication using this provider</param>
    /// <param name="scheme">The authentication scheme to require on the endpoint</param>
    /// <returns>The authentication web builder.</returns>
    IAuthenticationWebBuilder UseProvider(string providerIdentity, string pathMatch, string scheme);
}

/// <summary>
/// Extensions for the authentication web builder.
/// </summary>
public static class AuthenticationWebBuilderExtensions
{
    private class AuthenticationWebBuilder(IApplicationBuilder app) : IAuthenticationWebBuilder
    {
        public IAuthenticationWebBuilder UseProvider(string providerIdentity)
        {
            string endpoint = $"/asi/auth/{providerIdentity}";
            return UseProvider(providerIdentity, endpoint);
        }

        public IAuthenticationWebBuilder UseProvider(string providerIdentity, string pathMatch)
        {
            return UseProvider(providerIdentity, pathMatch, providerIdentity);
        }

        public IAuthenticationWebBuilder UseProvider(string providerIdentity, string pathMatch, string scheme)
        {
            AuthenticationProviderInfo providerInfo = new(providerIdentity, scheme);

            app.Map(pathMatch, branch => branch
                .UseRouting()
                .UseAuthorization()
                .UseMiddleware<AuthenticationRuntimeMiddleware>(providerInfo)
                .UseEndpoints(endpoints =>
                {
                    IEndpointConventionBuilder conventions = endpoints.MapGet("/", async context =>
                    {
                        await context.SignInAsync(context.User);

                        string? returnURL = context.Request.Query["redir"];
                        context.Response.Redirect(returnURL ?? "/");
                    });

                    conventions.RequireAuthorization(policy => policy
                        .AddAuthenticationSchemes(scheme)
                        .RequireAuthenticatedUser());
                }));

            return this;
        }
    }

    private class LogoutMiddleware(RequestDelegate next, IOptionsMonitor<CookieAuthenticationOptions> cookieOptions)
    {
        private CookieAuthenticationOptions Options => cookieOptions
            .Get(CookieAuthenticationDefaults.AuthenticationScheme);

        public async Task Invoke(HttpContext httpContext)
        {
            if (!IsLogoutRequest(httpContext.Request))
            {
                await next(httpContext);
                return;
            }

            // Log out of the cookie authentication scheme
            // to revoke the authentication ticket
            await httpContext.SignOutAsync();

            string? scheme = httpContext.User.Identity?.AuthenticationType;

            if (scheme is not null)
            {
                IAuthenticationHandlerProvider provider = httpContext.RequestServices.GetRequiredService<IAuthenticationHandlerProvider>();
                IAuthenticationHandler? handler = await provider.GetHandlerAsync(httpContext, scheme);

                // Check if the authentication provider that signed the
                // user in has a handler for signing the user out
                if (handler is not null && handler is IAuthenticationSignOutHandler)
                    await httpContext.SignOutAsync(scheme);
            }

            // If the sign out procedure did not trigger any errors or redirects,
            // this asks the cookie authentication scheme to redirect to the login page
            if (IsSuccess(httpContext.Response.StatusCode) && !IsAjaxRequest(httpContext.Request))
                await httpContext.ChallengeAsync(new AuthenticationProperties() { RedirectUri = "/" });
        }

        private bool IsLogoutRequest(HttpRequest request)
        {
            PathString logoutPath = Options.LogoutPath;
            return logoutPath.HasValue && request.Path.StartsWithSegments(logoutPath);
        }

        private static bool IsSuccess(int statusCode)
        {
            return statusCode >= 200 && statusCode <= 299;
        }

        // Taken from Microsoft's reference source for the Cookie authentication implementation
        // https://github.com/dotnet/aspnetcore/blob/v9.0.8/src/Security/Authentication/Cookies/src/CookieAuthenticationEvents.cs#L105
        private static bool IsAjaxRequest(HttpRequest request)
        {
            return string.Equals(request.Query[HeaderNames.XRequestedWith], "XMLHttpRequest", StringComparison.Ordinal) ||
                string.Equals(request.Headers.XRequestedWith, "XMLHttpRequest", StringComparison.Ordinal);
        }
    }

    /// <summary>
    /// Sets up the default configuration for services to support Gemstone authentication.
    /// </summary>
    /// <typeparam name="T">Provides setup information for the runtime.</typeparam>
    /// <param name="services">The collection of services</param>
    /// <returns>Builder for adding additional authentication schemes.</returns>
    public static AuthenticationBuilder ConfigureGemstoneWebAuthentication<T>(this IServiceCollection services) where T : class, IAuthenticationSetup
    {
        return services
            .AddGemstoneAuthentication<T>()
            .ConfigureGemstoneWebDefaults();
    }

    /// <summary>
    /// Sets up the default configuration for services to support Gemstone authentication.
    /// </summary>
    /// <param name="services">The collection of services</param>
    /// <param name="configure">Method to configure the runtime</param>
    /// <returns>Builder for adding additional authentication schemes.</returns>
    public static AuthenticationBuilder ConfigureGemstoneWebAuthentication(this IServiceCollection services, Action<IAuthenticationBuilder> configure)
    {
        return services
            .AddGemstoneAuthentication(configure)
            .ConfigureGemstoneWebDefaults();
    }

    /// <summary>
    /// Automatically configures the request pipeline to support well-known authentication providers.
    /// </summary>
    /// <param name="app">The application builder</param>
    /// <returns>The application builder.</returns>
    public static IApplicationBuilder UseGemstoneAuthentication(this IApplicationBuilder app)
    {
        return app.UseGemstoneAuthentication(configure);

        void configure(IAuthenticationWebBuilder builder)
        {
            IAuthenticationSetup setup = app.ApplicationServices.GetRequiredService<IAuthenticationSetup>();

            foreach (string providerIdentity in setup.GetProviderIdentities())
                builder.UseProvider(providerIdentity);
        }
    }

    /// <summary>
    /// Configures the request pipeline to support authentication providers using the given configuration method.
    /// </summary>
    /// <param name="app">The application builder</param>
    /// <param name="configure">The method to configure authentication provider middleware</param>
    /// <returns>The application builder.</returns>
    public static IApplicationBuilder UseGemstoneAuthentication(this IApplicationBuilder app, Action<IAuthenticationWebBuilder> configure)
    {
        AuthenticationWebBuilder builder = new(app.UseAuthentication());
        configure(builder);
        return app.UseMiddleware<LogoutMiddleware>();
    }

    private static AuthenticationBuilder ConfigureGemstoneWebDefaults(this IServiceCollection services)
    {
        services
            .AddMemoryCache()
            .TryAddSingleton<ITicketStore, DefaultTicketStore>();

        services
            .AddOptions<CookieAuthenticationOptions>(CookieAuthenticationDefaults.AuthenticationScheme)
            .Configure<ITicketStore>((options, sessionStore) =>
            {
                options.SessionStore = sessionStore;

                options.ExpireTimeSpan = TimeSpan.FromHours(24);
                options.SlidingExpiration = false;
                options.LoginPath = "/Login";
                options.LogoutPath = "/asi/logout";
                options.ReturnUrlParameter = "redir";

                options.Cookie.Name = "x-gemstone-auth";
                options.Cookie.Path = "/";
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
            });

        return services
            .AddWindowsAuthenticationProvider()
            .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddNegotiate("windows", _ => { })
            .AddCookie();
    }
}
