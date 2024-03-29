﻿//******************************************************************************************************
//  ElementFactory.cs - Gbtc
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

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Gemstone.Web.Razor.Elements
{
    /// <summary>
    /// Factory for <see cref="Element"/> instances.
    /// </summary>
    public class ElementFactory
    {
        private IJSRuntime JSRuntime { get; }

        /// <summary>
        /// Creates a new instance of the <see cref="ElementFactory"/> class.
        /// </summary>
        /// <param name="jsRuntime">The JavaScript runtime.</param>
        public ElementFactory(IJSRuntime jsRuntime) =>
            JSRuntime = jsRuntime;

        /// <summary>
        /// Gets the referenced <see cref="Element"/> instance.
        /// </summary>
        /// <param name="reference">The reference to the element.</param>
        /// <returns>The referenced element.</returns>
        public Element GetElement(ElementReference reference) =>
            new(reference, JSRuntime);
    }
}
