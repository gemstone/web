//******************************************************************************************************
//  AuthorizationInfoControllerBase.cs - Gbtc
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
using System.Security.Claims;
using System.Threading.Tasks;
using Gemstone.Collections.CollectionExtensions;
using Gemstone.Security.AccessControl;
using Gemstone.Security.AuthenticationProviders;
using Gemstone.Web.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Gemstone.Web.APIController;

/// <summary>
/// Base class for a controller that provides information about claims to client applications.
/// </summary>
public abstract class AuthorizationInfoControllerBase : ControllerBase
{
    /// <summary>
    /// Gets all the claims associated with the authenticated user.
    /// </summary>
    /// <returns>All of the authenticated user's claims.</returns>
    [HttpGet, Route("user/claims")]
    public virtual IActionResult GetAllClaims()
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
    [HttpGet, Route("user/claims/{**claimType}")]
    public virtual IEnumerable<string> GetClaims(string claimType)
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
    public virtual IActionResult GetClaimTypes(IServiceProvider serviceProvider, string providerIdentity)
    {
        IAuthenticationProvider? claimsProvider = serviceProvider
            .GetKeyedService<IAuthenticationProvider>(providerIdentity);

        if (claimsProvider is null)
            return NotFound();

        var claimTypes = claimsProvider
            .GetClaimTypes()
            .Select(type => new { Type = type, Alias = GetClaimTypeAlias(type) });

        return Ok(claimTypes);
    }

    /// <summary>
    /// Gets a list of users that can be authorized via the authentication provider.
    /// </summary>
    /// <param name="serviceProvider">Provides injected dependencies</param>
    /// <param name="providerIdentity">Identity of the authentication provider</param>
    /// <param name="searchText">Text used to narrow the list of results</param>
    /// <returns>A list of users from the authentication provider.</returns>
    [HttpGet, Route("provider/{providerIdentity}/users")]
    public virtual IActionResult FindUsers(IServiceProvider serviceProvider, string providerIdentity, string? searchText)
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
    public virtual IActionResult FindClaims(IServiceProvider serviceProvider, string providerIdentity, string claimType, string? searchText)
    {
        IAuthenticationProvider? claimsProvider = serviceProvider
            .GetKeyedService<IAuthenticationProvider>(providerIdentity);

        return claimsProvider is not null
            ? Ok(claimsProvider.FindClaims(claimType, searchText ?? "*"))
            : NotFound();
    }

    /// <summary>
    /// Gets a list of resources available for which permissions can be granted within the application.
    /// </summary>
    /// <param name="policyProvider">Provides authorization policies defined within the application</param>
    /// <param name="endpointDataSource">Source for endpoint data used to look up controller and action metadata</param>
    /// <returns>A list of resources within the application.</returns>
    [HttpGet, Route("resources")]
    public virtual async Task<IActionResult> GetResources(IAuthorizationPolicyProvider policyProvider, EndpointDataSource endpointDataSource)
    {
        Dictionary<string, HashSet<ResourceAccessLevel>> resourceAccessLookup = [];

        foreach (Endpoint endpoint in endpointDataSource.Endpoints)
        {
            ControllerActionDescriptor? descriptor = endpoint.Metadata
                .GetMetadata<ControllerActionDescriptor>();

            if (descriptor is null)
                continue;

            IReadOnlyList<IAuthorizeData> authorizeData = endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>() ?? [];
            IReadOnlyList<AuthorizationPolicy> policies = endpoint.Metadata.GetOrderedMetadata<AuthorizationPolicy>() ?? [];
            IReadOnlyList<IAuthorizationRequirementData> requirementData = endpoint.Metadata.GetOrderedMetadata<IAuthorizationRequirementData>() ?? [];
            AuthorizationPolicy? policy = await AuthorizationPolicy.CombineAsync(policyProvider, authorizeData, policies);

            bool hasControllerAccessRequirement = requirementData
                .SelectMany(datum => datum.GetRequirements())
                .Concat(policy?.Requirements ?? [])
                .Any(requirement => requirement is ControllerAccessRequirement);

            if (!hasControllerAccessRequirement)
                continue;

            ResourceAccessAttribute? accessAttribute = endpoint.Metadata
                .GetMetadata<ResourceAccessAttribute>();

            string resourceName = accessAttribute.GetResourceName(descriptor);
            IEnumerable<ResourceAccessLevel> accessLevels = ToAccessLevels(endpoint, accessAttribute);
            HashSet<ResourceAccessLevel> access = resourceAccessLookup.GetOrAdd(resourceName, _ => []);
            access.UnionWith(accessLevels);
        }

        var resources = resourceAccessLookup
            .OrderBy(kvp => kvp.Key)
            .Select(kvp => new
            {
                Type = "Controller",
                Name = kvp.Key,
                AccessLevels = kvp.Value
                    .OrderBy(level => level)
                    .Select(level => $"{level}")
            });

        return Ok(resources);

        static IEnumerable<ResourceAccessLevel> ToAccessLevels(Endpoint endpoint, ResourceAccessAttribute? accessAttribute)
        {
            if (accessAttribute is not null)
                return accessAttribute.Access;

            HttpMethodMetadata? httpMethodMetadata = endpoint.Metadata
                .GetMetadata<HttpMethodMetadata>();

            IReadOnlyList<string> httpMethods = httpMethodMetadata?.HttpMethods
                ?? [HttpMethods.Get, HttpMethods.Post];

            return httpMethods
                .SelectMany(accessAttribute.GetAccessLevels);
        }
    }

    /// <summary>
    /// Checks whether the user has permission to access each of the resources listed in the access list.
    /// </summary>
    /// <param name="accessList">List of resources the user is attempting to access</param>
    /// <returns>List of boolean values indicating whether the user has access to each requested resource.</returns>
    [HttpPost, Route("access")]
    public virtual IEnumerable<bool> CheckAccess([FromBody] ResourceAccessEntry[] accessList)
    {
        return accessList.Select(entry => User.HasAccessTo(entry.ResourceType, entry.ResourceName, entry.Access));
    }

    /// <summary>
    /// Represents an entry in a resource access list.
    /// </summary>
    public class ResourceAccessEntry
    {
        /// <summary>
        /// Gets or sets the type of the resource.
        /// </summary>
        public string ResourceType { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the name of the resource.
        /// </summary>
        public string ResourceName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the level of access needed.
        /// </summary>
        public ResourceAccessLevel[] Access { get; set; } = [];
    }

    private static string GetClaimTypeAlias(string claimType)
    {
        return ClaimTypeAliases.TryGetValue(claimType, out string? alias)
            ? alias : claimType;
    }

    private static Dictionary<string, string> ClaimTypeAliases { get; } = new()
    {
        { ClaimTypes.Actor, nameof(ClaimTypes.Actor) },
        { ClaimTypes.Anonymous, nameof(ClaimTypes.Anonymous) },
        { ClaimTypes.Authentication, nameof(ClaimTypes.Authentication) },
        { ClaimTypes.AuthenticationInstant, nameof(ClaimTypes.AuthenticationInstant) },
        { ClaimTypes.AuthenticationMethod, nameof(ClaimTypes.AuthenticationMethod) },
        { ClaimTypes.AuthorizationDecision, nameof(ClaimTypes.AuthorizationDecision) },
        { ClaimTypes.CookiePath, nameof(ClaimTypes.CookiePath) },
        { ClaimTypes.Country, nameof(ClaimTypes.Country) },
        { ClaimTypes.DateOfBirth, nameof(ClaimTypes.DateOfBirth) },
        { ClaimTypes.DenyOnlyPrimaryGroupSid, nameof(ClaimTypes.DenyOnlyPrimaryGroupSid) },
        { ClaimTypes.DenyOnlyPrimarySid, nameof(ClaimTypes.DenyOnlyPrimarySid) },
        { ClaimTypes.DenyOnlySid, nameof(ClaimTypes.DenyOnlySid) },
        { ClaimTypes.DenyOnlyWindowsDeviceGroup, nameof(ClaimTypes.DenyOnlyWindowsDeviceGroup) },
        { ClaimTypes.Dns, nameof(ClaimTypes.Dns) },
        { ClaimTypes.Dsa, nameof(ClaimTypes.Dsa) },
        { ClaimTypes.Email, nameof(ClaimTypes.Email) },
        { ClaimTypes.Expiration, nameof(ClaimTypes.Expiration) },
        { ClaimTypes.Expired, nameof(ClaimTypes.Expired) },
        { ClaimTypes.Gender, nameof(ClaimTypes.Gender) },
        { ClaimTypes.GivenName, nameof(ClaimTypes.GivenName) },
        { ClaimTypes.GroupSid, nameof(ClaimTypes.GroupSid) },
        { ClaimTypes.Hash, nameof(ClaimTypes.Hash) },
        { ClaimTypes.HomePhone, nameof(ClaimTypes.HomePhone) },
        { ClaimTypes.IsPersistent, nameof(ClaimTypes.IsPersistent) },
        { ClaimTypes.Locality, nameof(ClaimTypes.Locality) },
        { ClaimTypes.MobilePhone, nameof(ClaimTypes.MobilePhone) },
        { ClaimTypes.Name, nameof(ClaimTypes.Name) },
        { ClaimTypes.NameIdentifier, nameof(ClaimTypes.NameIdentifier) },
        { ClaimTypes.OtherPhone, nameof(ClaimTypes.OtherPhone) },
        { ClaimTypes.PostalCode, nameof(ClaimTypes.PostalCode) },
        { ClaimTypes.PrimaryGroupSid, nameof(ClaimTypes.PrimaryGroupSid) },
        { ClaimTypes.PrimarySid, nameof(ClaimTypes.PrimarySid) },
        { ClaimTypes.Role, nameof(ClaimTypes.Role) },
        { ClaimTypes.Rsa, nameof(ClaimTypes.Rsa) },
        { ClaimTypes.SerialNumber, nameof(ClaimTypes.SerialNumber) },
        { ClaimTypes.Sid, nameof(ClaimTypes.Sid) },
        { ClaimTypes.Spn, nameof(ClaimTypes.Spn) },
        { ClaimTypes.StateOrProvince, nameof(ClaimTypes.StateOrProvince) },
        { ClaimTypes.StreetAddress, nameof(ClaimTypes.StreetAddress) },
        { ClaimTypes.Surname, nameof(ClaimTypes.Surname) },
        { ClaimTypes.System, nameof(ClaimTypes.System) },
        { ClaimTypes.Thumbprint, nameof(ClaimTypes.Thumbprint) },
        { ClaimTypes.Upn, nameof(ClaimTypes.Upn) },
        { ClaimTypes.Uri, nameof(ClaimTypes.Uri) },
        { ClaimTypes.UserData, nameof(ClaimTypes.UserData) },
        { ClaimTypes.Version, nameof(ClaimTypes.Version) },
        { ClaimTypes.Webpage, nameof(ClaimTypes.Webpage) },
        { ClaimTypes.WindowsAccountName, nameof(ClaimTypes.WindowsAccountName) },
        { ClaimTypes.WindowsDeviceClaim, nameof(ClaimTypes.WindowsDeviceClaim) },
        { ClaimTypes.WindowsDeviceGroup, nameof(ClaimTypes.WindowsDeviceGroup) },
        { ClaimTypes.WindowsFqbnVersion, nameof(ClaimTypes.WindowsFqbnVersion) },
        { ClaimTypes.WindowsSubAuthority, nameof(ClaimTypes.WindowsSubAuthority) },
        { ClaimTypes.WindowsUserClaim, nameof(ClaimTypes.WindowsUserClaim) },
        { ClaimTypes.X500DistinguishedName, nameof(ClaimTypes.X500DistinguishedName) }
    };
}
