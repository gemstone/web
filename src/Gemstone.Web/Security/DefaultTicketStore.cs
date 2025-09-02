//******************************************************************************************************
//  DefaultTicketStore.cs - Gbtc
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
//  08/05/2025 - Stephen C. Wills
//       Generated original version of source code.
//
//******************************************************************************************************

using System;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Gemstone.Web.Security;

/// <summary>
/// Options to configure the memory cache used by the authentication ticket store.
/// </summary>
public class SessionCacheOptions
{
    /// <summary>
    /// Gets or sets the amount of time a ticket should
    /// remain valid since the last time it was accessed.
    /// </summary>
    public TimeSpan SlidingExpiration { get; set; } = TimeSpan.FromMinutes(15);
}

/// <summary>
/// Represents an in-memory storage mechanism for authentication tickets.
/// </summary>
/// <param name="memoryCache">The cache in which tickets will be stored</param>
public class DefaultTicketStore(IMemoryCache memoryCache, IOptionsMonitor<SessionCacheOptions> optionsMonitor) : ITicketStore
{
    private IMemoryCache MemoryCache { get; } = memoryCache;
    private SessionCacheOptions Options { get; } = optionsMonitor.CurrentValue;

    /// <inheritdoc/>
    public Task<string> StoreAsync(AuthenticationTicket ticket)
    {
        string key = GenerateNewSessionToken();
        UpdateEntry(key, ticket);
        return Task.FromResult(key);
    }

    /// <inheritdoc/>
    public Task RenewAsync(string key, AuthenticationTicket ticket)
    {
        UpdateEntry(key, ticket);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<AuthenticationTicket?> RetrieveAsync(string key)
    {
        if (!MemoryCache.TryGetValue(key, out AuthenticationTicket? ticket))
            ticket = null;

        return Task.FromResult(ticket);
    }

    /// <inheritdoc/>
    public Task RemoveAsync(string key)
    {
        MemoryCache.Remove(key);
        return Task.CompletedTask;
    }

    private void UpdateEntry(string key, AuthenticationTicket ticket)
    {
        DateTimeOffset? expiration = ticket.Properties.ExpiresUtc;
        MemoryCacheEntryOptions options = new() { SlidingExpiration = Options.SlidingExpiration };

        if (expiration is not null)
            options.AbsoluteExpiration = ticket.Properties.ExpiresUtc;

        MemoryCache
            .CreateEntry(key)
            .SetOptions(options)
            .SetValue(ticket)
            .Dispose();
    }

    private static string GenerateNewSessionToken()
    {
        byte[] tokenData = new byte[128];
        RandomNumberGenerator.Fill(tokenData);
        return Convert.ToBase64String(tokenData);
    }
}
