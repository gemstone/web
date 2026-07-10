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

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Security.Claims;
using System.Threading.Tasks;
using Gemstone.Reflection.MemberInfoExtensions;
using Gemstone.Security;
using Gemstone.Security.AccessControl;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Routing;

namespace Gemstone.Web.Security;

/// <summary>
/// Authorization handler for access to rest api actions.
/// </summary>
public class APIAccessHandler: GemstoneAccessHandler<APIAccessRequirement>
{
    /// <inheritdoc/>
    protected override string ResourceType => "API";

    /// #ToDo - Remove Support for Gemstone.ResourceAccess.Default
}

/// <summary>
/// Requirement to be handled by the <see cref="APIAccessHandler"/>.
/// </summary>
public class APIAccessRequirement : IAuthorizationRequirement
{
}

/// <summary>
/// Defines extension methods for the <see cref="APIAccessHandler"/>.
/// </summary>
public static class APIAccessHandlerExtensions
{
    private static APIAccessRequirement Requirement { get; } = new();

    /// <summary>
    /// Adds the <see cref="APIAccessRequirement"/> to the policy.
    /// </summary>
    /// <param name="builder">The policy builder</param>
    /// <returns>The policy builder.</returns>
    public static AuthorizationPolicyBuilder RequireAPIAccess(this AuthorizationPolicyBuilder builder)
    {
        return builder.AddRequirements(Requirement);
    }
}
