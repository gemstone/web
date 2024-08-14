//******************************************************************************************************
//  IReadModelController.cs - Gbtc
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

using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace Gemstone.Web.APIController
{
    /// <summary>
    /// Defines an interface for common readonly ModelController operations.
    /// </summary>
    public interface IReadOnlyModelController<T> where T : class, new()
    {
        /// <summary>
        /// Endpoint to get all models related to a given parent ID.
        /// </summary>
        public Task<IActionResult> Get(string parentID, int page, CancellationToken cancellationToken);

        /// <summary>
        /// Endpoint to get all models in order.
        /// </summary>
        public Task<IActionResult> Get(string sort, bool ascending, int page, CancellationToken cancellationToken);

        /// <summary>
        /// Endpoint to get all models in order related to a given ParentID.
        /// </summary>
        public Task<IActionResult> Get(string parentID, string sort, bool ascending, int page, CancellationToken cancellationToken);
        
        /// <summary>
        /// Endpoint to get a specific model.
        /// </summary>    
        public Task<IActionResult> GetOne(string id, CancellationToken cancellationToken);

        /// <summary>
        /// Endpoint to search models.
        /// </summary>
        public Task<IActionResult> Search(SearchPost<T> postData, int page, string? parentID, CancellationToken cancellationToken);

        /// <summary>
        /// Endpoint to get pagination information.
        /// </summary>
        public Task<IActionResult> GetPageInfo(SearchPost<T> postData, string? parentID, CancellationToken cancellationToken);

        /// <summary>
        /// Endpoint to get pagination information.
        /// </summary>
        public Task<IActionResult> GetPageInfo(string? parentID, CancellationToken cancellationToken);

    }
}
