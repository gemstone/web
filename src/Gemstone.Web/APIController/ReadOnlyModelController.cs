//******************************************************************************************************
//  ReadOnlyModelController.cs - Gbtc
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
//  03/26/2024 - C. Lackner
//       Generated original version of source code.
//
//******************************************************************************************************
// ReSharper disable CoVariantArrayConversion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Gemstone.Configuration;
using Gemstone.Data;
using Gemstone.Data.Model;
using Microsoft.AspNetCore.Mvc;

namespace Gemstone.Web.APIController
{
    /// <summary>
    /// Represents a readonly ModelController.
    /// </summary>
    public class ReadOnlyModelController<T> : ControllerBase, IReadOnlyModelController<T> where T : class, new()
    {
        #region [ Constructor ]

        /// <summary>
        /// Creates a new <see cref="ReadOnlyModelController{T}"/>
        /// </summary>
        public ReadOnlyModelController()
        {
            PrimaryKeyField = typeof(T).GetProperties().FirstOrDefault(p => p.GetCustomAttributes<PrimaryKeyAttribute>().Any())?.Name ?? "ID";

            ParentKey = typeof(T).GetProperties().FirstOrDefault(p => p.GetCustomAttributes<ParentKeyAttribute>().Any())?.Name ?? "";

            PropertyInfo? propertyInfo = typeof(T).GetProperties().FirstOrDefault(p => p.GetCustomAttributes<DefaultSortOrderAttribute>().Any());
            DefaultSortOrderAttribute? sortOrderAttribute = propertyInfo?.GetCustomAttribute<DefaultSortOrderAttribute>();

            if (sortOrderAttribute is not null)
            {
                DefaultSort = $"{propertyInfo?.Name}";
                DefaultSortDirection = sortOrderAttribute.Ascending;
            }

            GetRoles = typeof(T).GetCustomAttribute<GetRolesAttribute>()?.Roles ?? "";
            PageSize = typeof(T).GetCustomAttribute<PageSizeAttribute>()?.Limit ?? 50;
        }

        #endregion

        #region [ Properties ]

        /// <summary>
        /// Gets or sets the primary key field for the model.
        /// </summary>
        protected string PrimaryKeyField { get; set; }

        /// <summary>
        /// Gets or sets the parent key field for the model.
        /// </summary>
        protected string ParentKey { get; set; }

        /// <summary>
        /// Gets or sets the default sort field for the model.
        /// </summary>
        protected string? DefaultSort { get; }

        /// <summary>
        /// Gets or sets the default sort direction for the model.
        /// </summary>
        protected bool DefaultSortDirection { get; }

        /// <summary>
        /// Gets the roles required for GET requests.
        /// </summary>
        protected string GetRoles { get; }

        /// <summary>
        /// Gets the page size for the model.
        /// </summary>
        private int PageSize { get; }

        #endregion

        /// <summary>
        /// Gets all records from associated table, filtered to parent keys if provided.
        /// </summary>
        /// <param name="parentID">Parent ID to be used if table has set parent keys.</param>
        /// <param name="page">The 0 based page number to be returned.</param>
        /// <param name="cancellationToken">Propagates notification that operations should be canceled.</param>
        /// <returns>An <see cref="IActionResult"/> containing <see cref="IEnumerable{T}"/> or <see cref="Exception"/>.</returns>
        [HttpGet, Route("{page:min(0)}/{parentID?}")]
        public async Task<IActionResult> Get(string? parentID, int page, CancellationToken cancellationToken)
        {
            if (!GetAuthCheck())
                return await Task.FromResult<IActionResult>(Unauthorized());

            await using AdoDataConnection connection = CreateConnection();
            TableOperations<T> tableOperations = new(connection);
            RecordFilter<T>? filter = null;

            if (ParentKey != string.Empty && parentID is not null)
            {
                filter = new RecordFilter<T>() { 
                    FieldName = ParentKey,
                    Operator = "=",
                    SearchParameter = parentID
                };
            }

            IAsyncEnumerable<T> result = tableOperations.QueryRecordsAsync(DefaultSort, DefaultSortDirection, page, PageSize, cancellationToken, filter);

            return await Task.FromResult<IActionResult>(Ok(result));
        }

        /// <summary>
        /// Gets all records from associated table sorted by the provided field.
        /// </summary>
        /// <param name="sort">Field to be used for sorting.</param>
        /// <param name="ascending">Flag when <c>true</c> will sort ascending; otherwise, descending.</param>
        /// <param name="page">The 0 based page number to be returned.</param>
        /// <param name="cancellationToken">Propagates notification that operations should be canceled.</param>
        /// <returns>An <see cref="IActionResult"/> containing <see cref="IEnumerable{T}"/> or <see cref="Exception"/>.</returns>
        [HttpGet, Route("{page:min(0)}/{sort}/{ascending:bool}")]
        public async Task<IActionResult> Get(string sort, bool ascending, int page, CancellationToken cancellationToken)
        {
            if (!GetAuthCheck())
                return await Task.FromResult<IActionResult>(Unauthorized());

            await using AdoDataConnection connection = CreateConnection();
            TableOperations<T> tableOperations = new(connection);
            RecordFilter<T>? filter = null;

            IAsyncEnumerable<T> result = tableOperations.QueryRecordsAsync(sort, ascending, page, PageSize, cancellationToken, filter);

            return await Task.FromResult<IActionResult>(Ok(result));
        }

        /// <summary>
        /// Gets all records from associated table sorted by the provided field.
        /// </summary>
        /// <param name="sort">Field to be used for sorting.</param>
        /// <param name="ascending">Flag when <c>true</c> will sort ascending; otherwise, descending.</param>
        /// <param name="page">The 0 based page number to be returned.</param>
        /// <param name="parentID">Parent ID to be used if table has set parent keys.</param>
        /// <param name="cancellationToken">Propagates notification that operations should be canceled.</param>
        /// <returns>An <see cref="IActionResult"/> containing <see cref="IEnumerable{T}"/> or <see cref="Exception"/>.</returns>
        [HttpGet, Route("{page:min(0)}/{parentID}/{sort}/{ascending:bool}")]
        public async Task<IActionResult> Get(string parentID, string sort, bool ascending, int page, CancellationToken cancellationToken)
        {
            if (!GetAuthCheck())
                return await Task.FromResult<IActionResult>(Unauthorized());

            await using AdoDataConnection connection = CreateConnection();
            TableOperations<T> tableOperations = new(connection);
            RecordFilter<T> filter = new()
            {
                FieldName = ParentKey,
                Operator = "=",
                SearchParameter = parentID
            };
            
            IAsyncEnumerable<T> result = tableOperations.QueryRecordsAsync(sort, ascending, page, PageSize, cancellationToken, filter);

            return await Task.FromResult<IActionResult>(Ok(result));
        }

        /// <summary>
        /// Gets a single record from associated table.
        /// </summary>
        /// <param name="id">The PrimaryKey value of the Model to be returned.</param>
        /// <param name="cancellationToken">Propagates notification that operations should be canceled.</param>
        /// <returns>An <see cref="IActionResult"/> containing a <see cref="T"/> or <see cref="Exception"/>.</returns>
        [HttpGet, Route("One/{id}")]
        public async Task<IActionResult> GetOne(string id, CancellationToken cancellationToken)
        {
            if (!GetAuthCheck())
                return await Task.FromResult<IActionResult>(Unauthorized());

            await using AdoDataConnection connection = CreateConnection();
            TableOperations<T> tableOperations = new(connection);
            T? result = await tableOperations.QueryRecordAsync(new RecordRestriction($"{PrimaryKeyField} = {{0}}",id), cancellationToken);

            return result is null ?
                await Task.FromResult<IActionResult>(NotFound()) :
                await Task.FromResult<IActionResult>(Ok(result));
        }

        /// <summary>
        /// Gets all records from associated table matching the provided search criteria.
        /// </summary>
        /// <param name="postData">Search criteria.</param>
        /// <param name="page">the 0 based page number to be returned.</param>
        /// <param name="cancellationToken">Propagates notification that operations should be canceled.</param>
        /// <returns>An <see cref="IActionResult"/> containing <see cref="IEnumerable{T}"/> or <see cref="Exception"/>.</returns>
        [HttpPost, Route("Search/{page:min(0)}")]
        public async Task<IActionResult> Search([FromBody] SearchPost<T> postData, int page, CancellationToken cancellationToken)
        {
            if (!GetAuthCheck())
                return await Task.FromResult<IActionResult>(Unauthorized());

            await using AdoDataConnection connection = CreateConnection();
            TableOperations<T> tableOperations = new(connection);
            IAsyncEnumerable<T> result = tableOperations.QueryRecordsAsync(postData.OrderBy, postData.Ascending, page, PageSize, cancellationToken, postData.Searches.ToArray());

            return await Task.FromResult<IActionResult>(Ok(result));
        }

        /// <summary>
        /// Gets the pagination information for the provided search criteria.
        /// </summary>
        /// <param name="postData">Search criteria.</param>
        /// <param name="cancellationToken">Propagates notification that operations should be canceled.</param>
        /// <returns>A <see cref="PageInfo"/> object containing the pagination information or <see cref="Exception"/>.</returns>
        [HttpPost, Route("PageInfo")]
        public async Task<IActionResult> GetPageInfo(SearchPost<T> postData, CancellationToken cancellationToken)
        {
            if (!GetAuthCheck())
                return await Task.FromResult<IActionResult>(Unauthorized());

            await using AdoDataConnection connection = CreateConnection();
            TableOperations<T> tableOperations = new(connection);
            int recordCount = await tableOperations.QueryRecordCountAsync(cancellationToken, postData.Searches.ToArray());

            return await Task.FromResult<IActionResult>(Ok(new PageInfo()
            {
                PageSize = PageSize,
                PageCount = (int)Math.Ceiling(recordCount / (double)PageSize),
                TotalCount = recordCount
            }));
        }

        /// <summary>
        /// Gets pagination information.
        /// </summary>
        /// <param name="cancellationToken">Propagates notification that operations should be canceled.</param>
        /// <returns>A <see cref="PageInfo"/> object containing the pagination information or <see cref="Exception"/>.</returns>
        [HttpGet, Route("PageInfo")]
        public async Task<IActionResult> GetPageInfo(CancellationToken cancellationToken)
        {
            if (!GetAuthCheck())
                return await Task.FromResult<IActionResult>(Unauthorized());

            await using AdoDataConnection connection = CreateConnection();
            TableOperations<T> tableOperations = new(connection);
            int recordCount = await tableOperations.QueryRecordCountAsync(cancellationToken, (RecordRestriction?)null);
            
            return await Task.FromResult<IActionResult>(Ok(new PageInfo()
            {
                PageSize = PageSize,
                PageCount = (int)Math.Ceiling(recordCount / (double)PageSize),
                TotalCount = recordCount
            }));
        }

        /// <summary>
        /// Check if current User is authorized for GET Requests.
        /// </summary>
        /// <returns><c>true</c> if User is authorized for GET requests; otherwise, <c>false</c>.</returns>
        protected virtual bool GetAuthCheck()
        {
            return GetRoles == string.Empty || User.IsInRole(GetRoles);
        }

        /// <summary>
        /// Create the <see cref="AdoDataConnection"/> for the controller.
        /// </summary>
        /// <returns>A new <see cref="AdoDataConnection"/>.</returns>
        protected virtual AdoDataConnection CreateConnection()
        {
            return new AdoDataConnection(Settings.Default);
        }
    }
}
