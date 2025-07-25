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
using System.Collections.Generic;
using Gemstone.Security.AuthenticationProviders;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Gemstone.Web.Hosting;

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
    IAuthenticationWebBuilder UseProviderMiddleware(string providerIdentity);

    /// <summary>
    /// Adds the default middleware type for the given provider identity to the application's request pipeline.
    /// </summary>
    /// <param name="providerIdentity">The identity of the authentication provider</param>
    /// <param name="pathMatch">The path on which requests will require authentication using this provider</param>
    /// <returns>The authentication web builder.</returns>
    IAuthenticationWebBuilder UseProviderMiddleware(string providerIdentity, string pathMatch);

    /// <summary>
    /// Adds the given middleware type for the given provider identity to the application's request pipeline.
    /// </summary>
    /// <param name="providerIdentity">The identity of the authentication provider</param>
    /// <param name="middlewareType">The type of middleware to be used for authenticating the request</param>
    /// <returns>The authentication web builder.</returns>
    IAuthenticationWebBuilder UseProviderMiddleware(string providerIdentity, Type middlewareType);

    /// <summary>
    /// Adds the given middleware type for the given provider identity to the application's request pipeline.
    /// </summary>
    /// <param name="providerIdentity">The identity of the authentication provider</param>
    /// <param name="pathMatch">The path on which requests will require authentication using this provider</param>
    /// <param name="middlewareType">The type of middleware to be used for authenticating the request</param>
    /// <returns>The authentication web builder.</returns>
    IAuthenticationWebBuilder UseProviderMiddleware(string providerIdentity, string pathMatch, Type middlewareType);

    /// <summary>
    /// Adds the given middleware type for the given provider identity to the application's request pipeline.
    /// </summary>
    /// <typeparam name="TMiddleware">The type of middleware to be used for authenticating the request</typeparam>
    /// <param name="providerIdentity">The identity of the authentication provider</param>
    /// <returns>The authentication web builder.</returns>
    IAuthenticationWebBuilder UseProviderMiddleware<TMiddleware>(string providerIdentity);

    /// <summary>
    /// Adds the given middleware type for the given provider identity to the application's request pipeline.
    /// </summary>
    /// <typeparam name="TMiddleware">The type of middleware to be used for authenticating the request</typeparam>
    /// <param name="providerIdentity">The identity of the authentication provider</param>
    /// <param name="pathMatch">The path on which requests will require authentication using this provider</param>
    /// <returns>The authentication web builder.</returns>
    IAuthenticationWebBuilder UseProviderMiddleware<TMiddleware>(string providerIdentity, string pathMatch);
}

/// <summary>
/// Extensions for the authentication web builder.
/// </summary>
public static class AuthenticationWebBuilderExtensions
{
    private class AuthenticationWebBuilder(IApplicationBuilder app) : IAuthenticationWebBuilder
    {
        private static Dictionary<string, (string, Type)> MiddlewareRegistry { get; } = new Dictionary<string, (string, Type)>() {
            { "windows", ("/asi/auth/windows", typeof(WindowsAuthenticationProviderMiddleware)) }
        };

        public IAuthenticationWebBuilder UseProviderMiddleware(string providerIdentity)
        {
            (string endpoint, Type middlewareType) = FindMiddlewareDescriptor(providerIdentity);
            return UseProviderMiddleware(providerIdentity, endpoint, middlewareType);
        }

        public IAuthenticationWebBuilder UseProviderMiddleware(string providerIdentity, string endpoint)
        {
            (_, Type middlewareType) = FindMiddlewareDescriptor(providerIdentity);
            return UseProviderMiddleware(providerIdentity, endpoint, middlewareType);
        }

        public IAuthenticationWebBuilder UseProviderMiddleware(string providerIdentity, Type middlewareType)
        {
            (string endpoint, _) = FindMiddlewareDescriptor(providerIdentity);
            return UseProviderMiddleware(providerIdentity, endpoint, middlewareType);
        }

        public IAuthenticationWebBuilder UseProviderMiddleware(string providerIdentity, string endpoint, Type middlewareType)
        {
            app.Map(endpoint, branch => branch
                .UseMiddleware(middlewareType)
                .UseMiddleware<AuthenticationRuntimeMiddleware>(providerIdentity));

            return this;
        }

        public IAuthenticationWebBuilder UseProviderMiddleware<TMiddleware>(string providerIdentity)
        {
            (string endpoint, _) = FindMiddlewareDescriptor(providerIdentity);
            return UseProviderMiddleware<TMiddleware>(providerIdentity, endpoint);
        }

        public IAuthenticationWebBuilder UseProviderMiddleware<TMiddleware>(string providerIdentity, string endpoint)
        {
            app.Map(endpoint, branch => branch
                .UseMiddleware<TMiddleware>()
                .UseMiddleware<AuthenticationRuntimeMiddleware>(providerIdentity));

            return this;
        }

        private static (string, Type) FindMiddlewareDescriptor(string providerIdentity)
        {
            if (!MiddlewareRegistry.TryGetValue(providerIdentity, out (string, Type) descriptor))
                throw new KeyNotFoundException($"Provider \"{providerIdentity}\" is not recognized");

            return descriptor;
        }
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
            IAuthenticationRuntime runtime = app.ApplicationServices.GetRequiredService<IAuthenticationRuntime>();

            foreach (string providerIdentity in runtime.GetProviderIdentities())
                builder.UseProviderMiddleware(providerIdentity);
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
        AuthenticationWebBuilder builder = new(app);
        configure(builder);
        app.UseMiddleware<AuthenticationSessionMiddleware>();
        return app;
    }
}
