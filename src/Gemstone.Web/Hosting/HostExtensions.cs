//******************************************************************************************************
//  HostExtensions.cs - Gbtc
//
//  Copyright © 2022, Grid Protection Alliance.  All Rights Reserved.
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
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.Configuration;
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
        /// Configures the web host to listen on a random port on localhost addresses.
        /// </summary>
        /// <param name="webHostBuilder">The web host builder.</param>
        /// <returns>The web host builder.</returns>
        public static IWebHostBuilder ConfigureLocalWebAddress(this IWebHostBuilder webHostBuilder)
        {
            static IPAddress GetLocalhostAddress()
            {
                if (!Socket.OSSupportsIPv4)
                    return IPAddress.IPv6Loopback;

                if (!Socket.OSSupportsIPv6)
                    return IPAddress.Loopback;

                static IPAddress QueryDNS() => Dns.GetHostAddresses("localhost")
                    .DefaultIfEmpty(IPAddress.Loopback)
                    .First();

                try { return QueryDNS(); }
                catch { return IPAddress.Loopback; }
            }

            return webHostBuilder.ConfigureAppConfiguration((_, configurationBuilder) =>
            {
                IConfiguration? configuration = configurationBuilder.Sources
                    .OfType<ChainedConfigurationSource>()
                    .Select(source => source.Configuration)
                    .LastOrDefault();

                if (configuration == null)
                {
                    configuration = new ConfigurationBuilder()
                        .AddInMemoryCollection()
                        .Build();

                    configurationBuilder.AddConfiguration(configuration);
                }

                IPAddress localhostAddress = GetLocalhostAddress();
                string key = WebHostDefaults.ServerUrlsKey;
                string value = $"http://{localhostAddress}:0";
                configuration[key] = value;
            });
        }

        /// <summary>
        /// Gets the list of addresses the web server is listening on.
        /// </summary>
        /// <param name="host">The host of the web server.</param>
        /// <returns>The list of addresses the server is listening on.</returns>
        public static IEnumerable<string> GetServerAddresses(this IHost host)
        {
            IServer? server = host.Services.GetService<IServer>();
            IServerAddressesFeature? addressesFeature = server?.Features.Get<IServerAddressesFeature>();
            return addressesFeature?.Addresses ?? Enumerable.Empty<string>();
        }

        /// <summary>
        /// Gets a web server address usable from the local system.
        /// </summary>
        /// <param name="host">The host of the web server.</param>
        /// <returns>The local address of the web server.</returns>
        public static async Task<string?> GetLocalWebAddressAsync(this IHost host)
        {
            static async ValueTask<string?> GetValidHostAsync(string hostNameOrAddress)
            {
                if (IPAddress.TryParse(hostNameOrAddress, out IPAddress ipAddress))
                {
                    IPAddress[] loopbackAddresses =
                    {
                        IPAddress.Any,
                        IPAddress.IPv6Any,
                        IPAddress.Loopback,
                        IPAddress.IPv6Loopback
                    };

                    // "Any" addresses are not valid for DNS lookups,
                    // "Loopback" addresses will resolve to the
                    // machine name instead of localhost
                    if (loopbackAddresses.Contains(ipAddress))
                        return "localhost";

                    IPHostEntry hostEntry;
                    try { hostEntry = await Dns.GetHostEntryAsync(ipAddress); }
                    catch (SocketException) { return hostNameOrAddress; }

                    return hostEntry.AddressList.Contains(ipAddress)
                        ? hostEntry.HostName
                        : hostNameOrAddress;
                }

                // If it's not an IP, rather than looking up the host name,
                // we only need to determine whether DNS can resolve it
                try { await Dns.GetHostEntryAsync(hostNameOrAddress); }
                catch (SocketException) { return null; }
                return hostNameOrAddress;
            }

            static async ValueTask<string?> GetLocalURLAsync(string url)
            {
                UriBuilder uriBuilder = new UriBuilder(url);
                uriBuilder.Host = await GetValidHostAsync(uriBuilder.Host);
                return uriBuilder.Uri?.ToString();
            }

            return await host
                .GetServerAddresses()
                .ToAsyncEnumerable()
                .SelectAwait(GetLocalURLAsync)
                .FirstOrDefaultAsync(url => url != null);
        }
    }
}
