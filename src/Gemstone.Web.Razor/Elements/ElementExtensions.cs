﻿//******************************************************************************************************
//  ElementExtensions.cs - Gbtc
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
//  07/14/2020 - Stephen C. Wills
//       Generated original version of source code.
//
//******************************************************************************************************

using Microsoft.Extensions.DependencyInjection;

namespace Gemstone.Web.Razor.Elements
{
    /// <summary>
    /// Extension method for elements.
    /// </summary>
    public static class ElementExtensions
    {
        /// <summary>
        /// Adds the <see cref="ElementFactory"/> service.
        /// </summary>
        /// <param name="services">The service collection.</param>
        public static void AddElements(this IServiceCollection services) =>
            services.AddTransient<ElementFactory>();
    }
}
