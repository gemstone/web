//******************************************************************************************************
//  WebHostExtensions.cs - Gbtc
//
//  Copyright © 2020, Grid Protection Alliance.  All Rights Reserved.
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
//  06/21/2020 - Stephen C. Wills
//       Generated original version of source code.
//
//******************************************************************************************************

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Gemstone.Web.Hosting
{
    /// <summary>
    /// Defines extensions for web hosts.
    /// </summary>
    public static class HostExtensions
    {
        /// <summary>
        /// Gets the list of addresses the web server is listening on.
        /// </summary>
        /// <param name="host">The host of the web server.</param>
        /// <returns>The list of addresses the server is listening on.</returns>
        public static IEnumerable<Uri> GetServerAddresses(this IHost host)
        {
            static Uri ToUri(string str) => new Uri(str);
            IServer? server = host.Services.GetService<IServer>();
            IServerAddressesFeature? addressesFeature = server?.Features.Get<IServerAddressesFeature>();

            return addressesFeature?.Addresses.Select(ToUri)
                ?? Enumerable.Empty<Uri>();
        }

        /// <summary>
        /// Gets a web server address usable from the local system.
        /// </summary>
        /// <param name="host">The host of the web server.</param>
        /// <returns>The local address of the web server.</returns>
        public static async Task<string?> GetLocalWebAddressAsync(this IHost host)
        {
            static async ValueTask<string?> GetHostNameAsync(string ip)
            {
                if (!IPAddress.TryParse(ip, out IPAddress ipAddress))
                    return null;

                IPAddress[] loopbackAddresses =
                {
                    IPAddress.Any,
                    IPAddress.IPv6Any,
                    IPAddress.Loopback,
                    IPAddress.IPv6Loopback
                };

                if (loopbackAddresses.Contains(ipAddress))
                    return "localhost";

                IPHostEntry hostEntry;
                try { hostEntry = await Dns.GetHostEntryAsync(ipAddress); }
                catch (SocketException) { return null; }
                return hostEntry.HostName;
            }

            static async ValueTask<string?> GetLocalURLAsync(Uri uri)
            {
                string scheme = uri.Scheme;
                string? hostName = await GetHostNameAsync(uri.Host);
                int port = uri.Port;

                if (hostName == null)
                    return null;

                return $"{scheme}://{hostName}:{port}";
            }

            return await host
                .GetServerAddresses()
                .ToAsyncEnumerable()
                .SelectAwait(GetLocalURLAsync)
                .FirstOrDefaultAsync(url => url != null);
        }
    }
}
