//******************************************************************************************************
//  WebExtensions.cs - Gbtc
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
//  06/11/2020 - Billy Ernest
//       Generated original version of source code.
//
//******************************************************************************************************

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;

namespace Gemstone.Web
{
    /// <summary>
    /// Defines extension methods useful for web applications.
    /// </summary>
    public static class WebExtensions
    {
        /// <summary>
        /// Used to create <see cref="StaticFileOptions"/> for serving Gemstone.Web embedded javascript and css stylesheets.
        /// </summary>
        /// <returns><see cref="StaticFileOptions"/></returns>
        public static StaticFileOptions StaticFileEmbeddedResources()
        {
            ManifestEmbeddedFileProvider embeddedFileProvider = new(Assembly.GetExecutingAssembly(), "Shared");
            return new StaticFileOptions
            {
                FileProvider = embeddedFileProvider,
                RequestPath = new PathString("/@Gemstone")
            };
        }

        /// <summary>
        /// Used to load headers, respone phrase, and status code into <see cref="HttpResponse"/> for use of proxy endpoints between .NET Core and .NET Framework Apps.
        /// </summary>
        /// <param name="result">HttpResponse from controller.</param>
        /// <param name="message">Response message from .NET Framework App.</param>
        /// <param name="cancellationToken">Token to cancel the action.</param>
        public static async Task SetValues(this HttpResponse result, HttpResponseMessage? message, CancellationToken cancellationToken) =>
            await result.SetValues(message, null, null, cancellationToken);


        /// <summary>
        /// Used to load headers, respone phrase, and status code into <see cref="HttpResponse"/> for use of proxy endpoints between .NET Core and .NET Framework Apps.
        /// </summary>
        /// <param name="result">HttpResponse from controller.</param>
        /// <param name="message">Response message from .NET Framework App.</param>
        /// <param name="excludedHeaders">Additional headers to exclude on the copy over between objects.</param>
        /// <param name="cookieCallback">Callback to append additional cookies to resposne header.</param>
        /// <param name="cancellationToken">Token to cancel the action.</param>
        public static async Task SetValues(this HttpResponse result, HttpResponseMessage? message, HashSet<string>? excludedHeaders, Action<IResponseCookies>? cookieCallback, CancellationToken cancellationToken)
        {
            if (message is null)
            {
                result.StatusCode = (int)HttpStatusCode.ServiceUnavailable;
                return;
            }

            if (message.Headers.TryGetValues("Set-Cookie", out IEnumerable<string>? setCookieHeaders))
            {
                // Forward any "Set-Cookie" headers from message back to reponse using ASP.NET Core cookie
                foreach (string header in setCookieHeaders)
                {
                    // Try parsing HttpResponseMessage cookie header with an HttpResponse CookieOptions value,
                    // as a fallback, forward raw header to avoid losing possible auth/session variables
                    if (TryParseSetCookie(header, out string name, out string? value, out CookieOptions options))
                        result.Cookies.Append(name, value ?? string.Empty, options);
                    else
                        result.Headers.Append("Set-Cookie", header);
                }
            }

            result.StatusCode = (int)message.StatusCode;
            IHttpResponseFeature? feature = result.HttpContext.Response.HttpContext.Features.Get<IHttpResponseFeature>();
            if (feature is not null)
                feature.ReasonPhrase = message.ReasonPhrase;

            if (cookieCallback is not null)
            {
                cookieCallback(result.Cookies);
            }

            foreach (KeyValuePair<string, IEnumerable<string>> header in message.Headers)
            {
                if (excludedHeaders is not null && excludedHeaders.Contains(header.Key))
                    continue;

                result.Headers[header.Key] = header.Value.ToArray();
            }

            foreach (KeyValuePair<string, IEnumerable<string>> header in message.Content.Headers)
            {
                if (excludedHeaders is not null && excludedHeaders.Contains(header.Key))
                    continue;

                result.Headers[header.Key] = header.Value.ToArray();
            }

            result.Headers.Remove("transfer-encoding");

            // Set content type
            if (message.Content.Headers.ContentType is not null)
                result.ContentType = message.Content.Headers.ContentType.ToString();

            if (message.Content.Headers.ContentLength.GetValueOrDefault() <= 0)
                return;

            try
            {
                await message.Content.CopyToAsync(result.Body, cancellationToken);
            }
            catch (ObjectDisposedException)
            {
                // Ignore
            }

            return;
        }

        private static bool TryParseSetCookie(string header, out string name, out string? value, out CookieOptions options)
        {
            name = string.Empty;
            value = null;
            options = new CookieOptions { Path = "/" };

            if (string.IsNullOrWhiteSpace(header))
                return false;

            // Split segments by ';'
            string[] parts = header.Split(';');

            if (parts.Length == 0)
                return false;

            // First segment: cookie-pair "name=value"
            string cookieKVP = parts[0].Trim();
            int index = cookieKVP.IndexOf('=');

            if (index <= 0)
                return false;

            name = cookieKVP[..index].Trim();
            value = cookieKVP[(index + 1)..].Trim();

            // Remaining parts: attributes
            for (int i = 1; i < parts.Length; i++)
            {
                string part = parts[i].Trim();

                if (part.Length == 0)
                    continue;

                index = part.IndexOf('=');
                string attrName = index > 0 ? part[..index].Trim() : part;
                string attrValue = index > 0 ? part[(index + 1)..].Trim() : string.Empty;

                switch (attrName.ToLowerInvariant())
                {
                    case "domain":
                        if (!string.IsNullOrEmpty(attrValue))
                            options.Domain = attrValue;
                        break;

                    case "path":
                        if (!string.IsNullOrEmpty(attrValue))
                            options.Path = attrValue;
                        break;

                    case "expires":
                        if (!string.IsNullOrEmpty(attrValue) && DateTimeOffset.TryParse(attrValue, out DateTimeOffset dto))
                            options.Expires = dto;
                        break;

                    case "max-age":
                        if (!string.IsNullOrEmpty(attrValue) && int.TryParse(attrValue, out int seconds))
                            options.Expires = DateTimeOffset.UtcNow.AddSeconds(seconds);
                        break;

                    case "samesite":
                        if (!string.IsNullOrEmpty(attrValue))
                        {
                            switch (attrValue.Trim().ToLowerInvariant())
                            {
                                case "lax":
                                    options.SameSite = SameSiteMode.Lax;
                                    break;
                                case "strict":
                                    options.SameSite = SameSiteMode.Strict;
                                    break;
                                case "none":
                                    options.SameSite = SameSiteMode.None;
                                    break;
                            }
                        }
                        break;

                    case "secure":
                        options.Secure = true;
                        break;

                    case "httponly":
                        options.HttpOnly = true;
                        break;
                }
            }

            return !string.IsNullOrEmpty(name);
        }
    }
}
