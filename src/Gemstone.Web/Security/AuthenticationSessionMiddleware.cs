//******************************************************************************************************
//  AuthenticationSessionMiddleware.cs - Gbtc
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
//  04/18/2025 - Stephen C. Wills
//       Generated original version of source code.
//
//******************************************************************************************************

using System;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Gemstone.Web.Security;

/// <summary>
/// Represents configuration options for the <see cref="AuthenticationSessionMiddleware"/>.
/// </summary>
public class AuthenticationSessionOptions
{
    /// <summary>
    /// Gets or sets the base path of the application to which the cookie will be scoped.
    /// </summary>
    public string ApplicationBasePath { get; set; } = "/";

    /// <summary>
    /// Gets or sets the name of the cookie used for storing the authentication session token.
    /// </summary>
    public string AuthenticationSessionCookie { get; set; } = "x-gemstone-auth";

    /// <summary>
    /// Gets or sets the amount of time the server will continue to store
    /// the authentication session since the last request made by the user.
    /// </summary>
    public double IdleAuthenticationTokenExpirationMinutes { get; set; } = 15.0D;

    /// <summary>
    /// Gets or sets the amount of time after the authenticated
    /// session is established before the session cookie expires.
    /// </summary>
    public double AuthenticationSessionExpirationHours { get; set; } = 24.0D;

    /// <summary>
    /// Gets the amount of time the server will continue to store the
    /// authentication session since the last request made by the user.
    /// </summary>
    public TimeSpan AuthenticationTokenExpiration =>
        TimeSpan.FromMinutes(IdleAuthenticationTokenExpirationMinutes);

    /// <summary>
    /// Gets the amount of time after the authenticated session
    /// is established before the session cookie expires.
    /// </summary>
    public DateTimeOffset AuthenticationCookieExpiration =>
        DateTime.UtcNow.AddHours(AuthenticationSessionExpirationHours);
}

/// <summary>
/// Represents a middleware used to establish an authenticated session between the client and server.
/// </summary>
/// <param name="next">Delegate used to invoke the next middleware in the pipeline.</param>
/// <param name="memoryCache">Cache used to store session data.</param>
/// <param name="options">Options used to configure how the session is managed.</param>
public class AuthenticationSessionMiddleware(RequestDelegate next, IMemoryCache memoryCache, IOptionsMonitor<AuthenticationSessionOptions> options)
{
    #region [ Properties ]

    private RequestDelegate Next { get; } = next;
    private IMemoryCache MemoryCache { get; } = memoryCache;
    private AuthenticationSessionOptions Options => options.CurrentValue;

    #endregion

    #region [ Methods ]

    /// <summary>
    /// Handles an HTTP request.
    /// </summary>
    /// <param name="httpContext">The object providing context about the request.</param>
    /// <returns>Task that indicates when the middleware is finished processing the request.</returns>
    public async Task Invoke(HttpContext httpContext)
    {
        AuthenticationSessionOptions options = Options;

        if (httpContext.Request.Headers.ContainsKey("Authorization"))
            await HandleRequestWithCredentialsAsync(httpContext, options);
        else
            await HandleRequestAsync(httpContext, options);
    }

    // If a request with credentials is authenticated, create a new session and provide a session cookie.
    // If a request with credentials is not authenticated, flush session data and expire the session cookie.
    private async Task HandleRequestWithCredentialsAsync(HttpContext httpContext, AuthenticationSessionOptions options)
    {
        await Next(httpContext);

        string? oldAuthenticationToken = ReadAuthenticationCookie(httpContext, options);

        if (oldAuthenticationToken is not null)
            FlushSessionPrincipal(oldAuthenticationToken);

        if (httpContext.User.Identity?.IsAuthenticated == true)
            HandleAuthenticatedRequest(httpContext, options);
        else
            ExpireAuthenticationCookie(httpContext, options);
    }

    // Request without credentials will attempt to use the session to authenticate the request.
    private async Task HandleRequestAsync(HttpContext httpContext, AuthenticationSessionOptions options)
    {
        string? authenticationToken = ReadAuthenticationCookie(httpContext, options);

        if (authenticationToken is null)
        {
            await Next(httpContext);
            return;
        }

        ClaimsPrincipal? sessionPrincipal = FetchSessionPrincipal(authenticationToken);

        if (sessionPrincipal is null)
        {
            await Next(httpContext);
            return;
        }

        httpContext.User = sessionPrincipal;
        await Next(httpContext);
        UpdateSessionPrincipal(authenticationToken, sessionPrincipal, options);
    }

    // Establishes a new session with the client.
    private void HandleAuthenticatedRequest(HttpContext httpContext, AuthenticationSessionOptions options)
    {
        string newAuthenticationToken = GenerateNewAuthenticationToken();
        ClaimsPrincipal sessionPrincipal = BuildSessionPrincipal(httpContext.User);
        UpdateSessionPrincipal(newAuthenticationToken, sessionPrincipal, options);
        UpdateAuthenticationCookie(httpContext, newAuthenticationToken, options);
    }

    // Retrieve session data from the server-side cache.
    private ClaimsPrincipal? FetchSessionPrincipal(string authenticationToken)
    {
        return MemoryCache.TryGetValue(authenticationToken, out ClaimsPrincipal? principal)
            ? principal : null;
    }

    // Updates the server-side cache entry and resets the expiration window.
    private void UpdateSessionPrincipal(string authenticationToken, ClaimsPrincipal sessionPrincipal, AuthenticationSessionOptions options)
    {
        MemoryCache
            .CreateEntry(authenticationToken)
            .SetValue(sessionPrincipal)
            .SetSlidingExpiration(options.AuthenticationTokenExpiration)
            .Dispose();
    }

    // Removes session data from the server-side cache.
    private void FlushSessionPrincipal(string authenticationToken)
    {
        MemoryCache.Remove(authenticationToken);
    }

    // Prepares the cookie with the session token in the response to send it to the client.
    private void UpdateAuthenticationCookie(HttpContext httpContext, string authenticationToken, AuthenticationSessionOptions options)
    {
        CookieOptions cookieOptions = new()
        {
            Path = Options.ApplicationBasePath,
            HttpOnly = true,
            Secure = true,
            IsEssential = true,
            Expires = options.AuthenticationCookieExpiration
        };

        httpContext.Response.Cookies.Append(options.AuthenticationSessionCookie, authenticationToken, cookieOptions);
    }

    #endregion

    #region [ Static ]

    // Static Methods

    // Reads the session token from the cookie provided by the client.
    private static string? ReadAuthenticationCookie(HttpContext httpContext, AuthenticationSessionOptions options)
    {
        return httpContext.Request.Cookies.TryGetValue(options.AuthenticationSessionCookie, out string? token)
            ? token : null;
    }

    // Generates a token the server can use to look up the session principal.
    private static string GenerateNewAuthenticationToken()
    {
        byte[] tokenData = new byte[128];
        RandomNumberGenerator.Fill(tokenData);
        return Convert.ToBase64String(tokenData);
    }

    // Copies the claims from the authenticated request's principal into a new
    // principal that will persist across requests within the same session.
    private static ClaimsPrincipal BuildSessionPrincipal(ClaimsPrincipal httpPrincipal)
    {
        ClaimsIdentity sessionIdentity = new(httpPrincipal.Identity, httpPrincipal.Claims);
        ClaimsPrincipal sessionPrincipal = new(sessionIdentity);
        sessionIdentity.AddClaim(new(ClaimTypes.Role, "Session"));
        return sessionPrincipal;
    }

    // If the client provided a cookie, indicate to the client that the
    // cookie is no longer valid by sending an expired cookie to replace it.
    private static void ExpireAuthenticationCookie(HttpContext httpContext, AuthenticationSessionOptions options)
    {
        string authenticationCookie = options.AuthenticationSessionCookie;

        if (httpContext.Request.Cookies.ContainsKey(authenticationCookie))
            httpContext.Response.Cookies.Delete(authenticationCookie);
    }

    #endregion
}

/// <summary>
/// Extension methods for setting up the <see cref="AuthenticationSessionMiddleware"/>.
/// </summary>
public static class AuthenticationSessionMiddlewareExtensions
{
    /// <summary>
    /// Registers the services required by the <see cref="AuthenticationSessionMiddleware"/>.
    /// </summary>
    /// <param name="services">Collection of services used by the app.</param>
    /// <returns>Collection of services used by the app.</returns>
    public static IServiceCollection AddAuthenticationCache(this IServiceCollection services)
    {
        return services.AddMemoryCache();
    }

    /// <summary>
    /// Registers the services required by the <see cref="AuthenticationSessionMiddleware"/>.
    /// </summary>
    /// <param name="services">Collection of services used by the app.</param>
    /// <param name="configureOptions">The action used to configure the options.</param>
    /// <returns>Collection of services used by the app.</returns>
    public static IServiceCollection AddAuthenticationCache(this IServiceCollection services, Action<AuthenticationSessionOptions> configureOptions)
    {
        return services
            .Configure(configureOptions)
            .AddAuthenticationCache();
    }

    /// <summary>
    /// Registers the <see cref="AuthenticationSessionMiddleware"/> in the app pipeline.
    /// </summary>
    /// <param name="app">The app builder with which to register the middleware.</param>
    /// <returns>The app builder with middleware registered.</returns>
    public static IApplicationBuilder UseAuthenticationCache(this IApplicationBuilder app)
    {
        return app.UseMiddleware<AuthenticationSessionMiddleware>();
    }
}
