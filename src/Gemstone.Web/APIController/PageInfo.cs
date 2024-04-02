//******************************************************************************************************
//  PageInfo.cs - Gbtc
//
//  Copyright © 2024, Grid Protection Alliance.  All Rights Reserved.
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
//  03/19/2024 - C. Lackner
//       Generated original version of source code.
//
//******************************************************************************************************

namespace Gemstone.Web.APIController
{
    /// <summary>
    /// Content of a POST Return for providing pageination info to client from <see cref="IReadModelController{T}"/>
    /// </summary>
    public class PageInfo
    {
        /// <summary>
        /// Number of Models per Page
        /// </summary>
        public int PageSize { get; set; }

        /// <summary>
        /// Total Number of Pages
        /// </summary>
        public int PageCount { get; set; }

        /// <summary>
        /// Number of Models
        /// </summary>
        public int TotalCount { get; set; }
    }
}
