//******************************************************************************************************
//  ClaimsControllerBase.cs - Gbtc
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
//  07/15/2025 - Stephen C. Wills
//       Generated original version of source code.
//
//******************************************************************************************************

using System;
using System.Collections.Generic;
using System.Linq;
using Gemstone.Security.AuthenticationProviders;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace Gemstone.Web.APIController;

/// <summary>
/// Base class for a controller that provides information about claims to client applications.
/// </summary>
public abstract class ClaimsControllerBase : ControllerBase
{
    /// <summary>
    /// Gets all the claims associated with the authenticated user.
    /// </summary>
    /// <returns>All of the authenticated user's claims.</returns>
    [HttpGet, Route("claims")]
    public IActionResult GetAllClaims()
    {
        var claims = HttpContext.User.Claims
            .Select(claim => new { claim.Type, claim.Value });

        return Ok(claims);
    }

    /// <summary>
    /// Gets the claims of a given type associated with the authenticated user.
    /// </summary>
    /// <param name="claimType">The type of the claims to be returned</param>
    /// <returns>The authenticated user's claims of the given type.</returns>
    [HttpGet, Route("claims/{**claimType}")]
    public IEnumerable<string> GetClaims(string claimType)
    {
        return HttpContext.User
            .FindAll(claim => claim.Type == claimType)
            .Select(claim => claim.Value);
    }

    /// <summary>
    /// Gets the types of claims that the authentication provider associates with its users.
    /// </summary>
    /// <param name="serviceProvider">Provides injected dependencies</param>
    /// <param name="providerIdentity">Identity of the authentication provider</param>
    /// <returns>The list of claim types used by the authentication provider.</returns>
    [HttpGet, Route("provider/{providerIdentity}/claimTypes")]
    public IActionResult GetClaimTypes(IServiceProvider serviceProvider, string providerIdentity)
    {
        IAuthenticationProvider? claimsProvider = serviceProvider
            .GetKeyedService<IAuthenticationProvider>(providerIdentity);

        return claimsProvider is not null
            ? Ok(claimsProvider.GetClaimTypes())
            : NotFound();
    }

    /// <summary>
    /// Gets a list of users that can be authorized via the authentication provider.
    /// </summary>
    /// <param name="serviceProvider">Provides injected dependencies</param>
    /// <param name="providerIdentity">Identity of the authentication provider</param>
    /// <param name="searchText">Text used to narrow the list of results</param>
    /// <returns>A list of users from the authentication provider.</returns>
    [HttpGet, Route("provider/{providerIdentity}/users")]
    public IActionResult FindUsers(IServiceProvider serviceProvider, string providerIdentity, string? searchText)
    {
        IAuthenticationProvider? claimsProvider = serviceProvider
            .GetKeyedService<IAuthenticationProvider>(providerIdentity);

        return claimsProvider is not null
            ? Ok(claimsProvider.FindUsers(searchText ?? "*"))
            : NotFound();
    }

    /// <summary>
    /// Gets a list of claims that can be assigned to users who authenticate with the provider.
    /// </summary>
    /// <param name="serviceProvider">Provides injected dependencies</param>
    /// <param name="providerIdentity">Identity of the authentication provider</param>
    /// <param name="claimType">The type of claims to search for</param>
    /// <param name="searchText">Text used to narrow the list of results</param>
    /// <returns>A list of claims from the authentication provider.</returns>
    [HttpGet, Route("provider/{providerIdentity}/claims/{**claimType}")]
    public IActionResult FindClaims(IServiceProvider serviceProvider, string providerIdentity, string claimType, string? searchText)
    {
        IAuthenticationProvider? claimsProvider = serviceProvider
            .GetKeyedService<IAuthenticationProvider>(providerIdentity);

        return claimsProvider is not null
            ? Ok(claimsProvider.FindClaims(claimType, searchText ?? "*"))
            : NotFound();
    }
}
