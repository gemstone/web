//******************************************************************************************************
//  AuthenticationRuntimeMiddleware.cs - Gbtc
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
//  07/23/2025 - Stephen C. Wills
//       Generated original version of source code.
//
//******************************************************************************************************

using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Gemstone.Security.AuthenticationProviders;
using Microsoft.AspNetCore.Http;

namespace Gemstone.Web.Security;

/// <summary>
/// Provides information about an authentication provider.
/// </summary>
/// <param name="identity">The identity of the provider</param>
/// <param name="scheme">The scheme used by the provider to authenticate users</param>
public class AuthenticationProviderInfo(string identity, string scheme)
{
    /// <summary>
    /// Gets the identity of the provider.
    /// </summary>
    public string Identity { get; } = identity;

    /// <summary>
    /// Gets the scheme used by the provider to authenticate users.
    /// </summary>
    public string Scheme { get; } = scheme;
}

/// <summary>
/// Represents a middleware used to assign claims to authenticated users.
/// </summary>
/// <param name="next">Delegate used to invoke the next middleware in the pipeline</param>
/// <param name="runtime">The authentication runtime used to query assigned claims</param>
/// <param name="providerInfo">Information about the provider that authenticates the user</param>
public class AuthenticationRuntimeMiddleware(RequestDelegate next, IAuthenticationRuntime runtime, AuthenticationProviderInfo providerInfo)
{
    private RequestDelegate Next { get; } = next;
    private IAuthenticationRuntime Runtime { get; } = runtime;
    private string ProviderIdentity { get; } = providerInfo.Identity;
    private string ProviderScheme { get; } = providerInfo.Scheme;

    /// <summary>
    /// Assigns claims from the authentication runtime to the authenticated user.
    /// </summary>
    /// <param name="httpContext">The context of the HTTP request</param>
    public async Task Invoke(HttpContext httpContext)
    {
        ClaimsPrincipal user = httpContext.User;
        IEnumerable<Claim> nameClaim = ReadNameClaim(user);
        IEnumerable<Claim> assignedClaims = Runtime.GetAssignedClaims(ProviderIdentity, user);
        IEnumerable<Claim> allClaims = nameClaim.Concat(assignedClaims);
        ClaimsIdentity newIdentity = new(allClaims, ProviderScheme);
        newIdentity.AddClaims(assignedClaims);
        httpContext.User = new(newIdentity);
        await Next(httpContext);
    }

    private static IEnumerable<Claim> ReadNameClaim(params ClaimsPrincipal[] users)
    {
        return users
            .Select(user => user.Identity?.Name)
            .Where(name => name is not null)
            .Select(name => new Claim(ClaimTypes.Name, name!));
    }
}
