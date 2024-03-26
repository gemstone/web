//******************************************************************************************************
//  SearchPost.cs - Gbtc
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

using System.Collections.Generic;
using Gemstone.Data.Model;

namespace Gemstone.Web.APIController
{
    /// <summary>
    /// Content of a POST Request for searching in <see cref="IReadModelController{T}"/>
    /// </summary>
    public class SearchPost<T> where T : class, new()
    {
        /// <summary>
        /// The <see cref="RecordFilter{T}"/> to be applied to the search
        /// </summary>
        public IEnumerable<RecordFilter<T>> Searches { get; set; }

        /// <summary>
        /// The Field to oreder the results by
        /// </summary>
        public string OrderBy { get; set; }

        /// <summary>
        /// {true} if the results should be ordered ascending
        /// </summary>
        public bool Ascending { get; set; }
    }
}
