//******************************************************************************************************
//  ControllerAccessHandler.cs - Gbtc
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
//  07/29/2025 - Stephen C. Wills
//       Generated original version of source code.
//
//******************************************************************************************************

using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Gemstone.Security.AccessControl;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Routing;

namespace Gemstone.Web.Security;

/// <summary>
/// Authorization handler for access to controller actions.
/// </summary>
public class ControllerAccessHandler : AuthorizationHandler<ControllerAccessRequirement>
{
    #region [ Members ]

    // Nested Types
    private enum Permission
    {
        Allow,
        Deny,
        Neither
    }

    private class ContextWrapper(AuthorizationHandlerContext context, ControllerAccessRequirement requirement, HttpContext httpContext, Endpoint endpoint, ControllerActionDescriptor descriptor)
    {
        private AuthorizationHandlerContext Context { get; } = context;
        private ControllerAccessRequirement Requirement { get; } = requirement;

        public ClaimsPrincipal User { get; } = context.User;
        public Endpoint Endpoint { get; } = endpoint;
        public ControllerActionDescriptor Descriptor { get; } = descriptor;
        public string HttpMethod => httpContext.Request.Method;

        public bool Succeed()
        {
            Context.Succeed(Requirement);
            return true;
        }

        public bool Fail(AuthorizationFailureReason reason)
        {
            Context.Fail(reason);
            return true;
        }
    }

    #endregion

    #region [ Methods ]

    /// <inheritdoc/>
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, ControllerAccessRequirement requirement)
    {
        if (context.Resource is not HttpContext httpContext)
            return Task.CompletedTask;

        IEndpointFeature? endpointFeature = httpContext.Features.Get<IEndpointFeature>();
        Endpoint? endpoint = endpointFeature?.Endpoint;

        if (endpoint is null)
            return Task.CompletedTask;

        ControllerActionDescriptor? descriptor = endpoint.Metadata
            .GetMetadata<ControllerActionDescriptor>();

        if (descriptor is null)
            return Task.CompletedTask;

        ContextWrapper wrapper = new(context, requirement, httpContext, endpoint, descriptor);

        if (HandleResourceActionPermission(wrapper))
            return Task.CompletedTask;

        HandleResourceAccessPermission(wrapper);
        return Task.CompletedTask;
    }

    private bool HandleResourceActionPermission(ContextWrapper wrapper)
    {
        IRouteNameMetadata? routeNameMetadata = wrapper.Endpoint.Metadata
            .GetMetadata<IRouteNameMetadata>();

        string? routeName = routeNameMetadata?.RouteName;

        string resource = wrapper.Descriptor.ControllerName;
        string action = routeName ?? wrapper.Descriptor.ActionName;
        string claimValue = $"Controller {resource} {action}";
        Permission permission = GetResourceActionPermission(wrapper.User, claimValue);

        return
            (permission == Permission.Deny && fail()) ||
            (permission == Permission.Allow && succeed());

        bool succeed() =>
            wrapper.Succeed();

        bool fail()
        {
            AuthorizationFailureReason reason = ToFailureReason(claimValue);
            return wrapper.Fail(reason);
        }
    }

    private AuthorizationFailureReason ToFailureReason(string claim)
    {
        return new AuthorizationFailureReason(this, $"{claim} permission denied");
    }

    #endregion

    #region [ Static ]

    // Static Methods

    private static Permission GetResourceActionPermission(ClaimsPrincipal user, string claimValue)
    {
        string allowClaim = $"Gemstone.ResourceAction.Allow";
        string denyClaim = $"Gemstone.ResourceAction.Deny";

        if (user.HasClaim(denyClaim, claimValue))
            return Permission.Deny;

        return user.HasClaim(allowClaim, claimValue)
            ? Permission.Allow
            : Permission.Neither;
    }

    private static void HandleResourceAccessPermission(ContextWrapper wrapper)
    {
        IReadOnlyList<ResourceAccessAttribute> accessAttributes = wrapper.Endpoint.Metadata
            .GetOrderedMetadata<ResourceAccessAttribute>();

        string resourceName = accessAttributes.GetResourceName(wrapper.Descriptor);
        ResourceAccessType access = accessAttributes.GetAccessType(wrapper.HttpMethod);

        if (wrapper.User.HasAccessTo("Controller", resourceName, access))
            wrapper.Succeed();
    }

    #endregion
}

/// <summary>
/// Requirement to be handled by the <see cref="ControllerAccessHandler"/>.
/// </summary>
public class ControllerAccessRequirement : IAuthorizationRequirement
{
}

/// <summary>
/// Defines extension methods for the controller access handler.
/// </summary>
public static class ControllerAccessHandlerExtensions
{
    private static ControllerAccessRequirement Requirement { get; } = new();

    /// <summary>
    /// Adds the <see cref="ControllerAccessRequirement"/> to the policy.
    /// </summary>
    /// <param name="builder">The policy builder</param>
    /// <returns>The policy builder.</returns>
    public static AuthorizationPolicyBuilder RequireControllerAccess(this AuthorizationPolicyBuilder builder)
    {
        return builder.AddRequirements(Requirement);
    }
}
