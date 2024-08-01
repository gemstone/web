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
        /// <returns>An <see cref="IActionResult"/> containing <see cref="IEnumerable{T}"/> or <see cref="Exception"/>.</returns>
        [HttpGet, Route("{page:min(0)}/{parentID?}")]
        public Task<IActionResult> Get(string? parentID, int page)
        {
            if (!GetAuthCheck())
                return Task.FromResult<IActionResult>(Unauthorized());

            using AdoDataConnection connection = CreateConnection();
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

            IEnumerable<T> result = tableOperations.QueryRecords(DefaultSort, DefaultSortDirection, page, PageSize, filter );

            return Task.FromResult<IActionResult>(Ok(result));
        }

        /// <summary>
        /// Gets all records from associated table sorted by the provided field.
        /// </summary>
        /// <param name="sort">Field to be used for sorting.</param>
        /// <param name="ascending">Flag when <c>true</c> will sort ascending; otherwise, descending.</param>
        /// <param name="page">The 0 based page number to be returned.</param>
        /// <returns>An <see cref="IActionResult"/> containing <see cref="IEnumerable{T}"/> or <see cref="Exception"/>.</returns>
        [HttpGet, Route("{page:min(0)}/{sort}/{ascending:bool}")]
        public Task<IActionResult> Get(string sort, bool ascending, int page)
        {
            if (!GetAuthCheck())
                return Task.FromResult<IActionResult>(Unauthorized());

            using AdoDataConnection connection = CreateConnection();
            TableOperations<T> tableOperations = new(connection);
            RecordFilter<T>? filter = null;

            IEnumerable<T> result = tableOperations.QueryRecords(sort, ascending, page, PageSize, filter);

            return Task.FromResult<IActionResult>(Ok(result));
        }

        /// <summary>
        /// Gets all records from associated table sorted by the provided field.
        /// </summary>
        /// <param name="sort">Field to be used for sorting.</param>
        /// <param name="ascending">Flag when <c>true</c> will sort ascending; otherwise, descending.</param>
        /// <param name="page">The 0 based page number to be returned.</param>
        /// <param name="parentID">Parent ID to be used if table has set parent keys.</param>
        /// <returns>An <see cref="IActionResult"/> containing <see cref="IEnumerable{T}"/> or <see cref="Exception"/>.</returns>
        [HttpGet, Route("{page:min(0)}/{parentID}/{sort}/{ascending:bool}")]
        public Task<IActionResult> Get(string parentID, string sort, bool ascending, int page)
        {
            if (!GetAuthCheck())
                return Task.FromResult<IActionResult>(Unauthorized());

            using AdoDataConnection connection = CreateConnection();
            TableOperations<T> tableOperations = new(connection);
            RecordFilter<T> filter = new()
            {
                FieldName = ParentKey,
                Operator = "=",
                SearchParameter = parentID
            };
            
            IEnumerable<T> result = tableOperations.QueryRecords(sort, ascending, page, PageSize, filter);

            return Task.FromResult<IActionResult>(Ok(result));
        }

        /// <summary>
        /// Gets a single record from associated table.
        /// </summary>
        /// <param name="id">The PrimaryKey value of the Model to be returned.</param>
        /// <returns>An <see cref="IActionResult"/> containing a <see cref="T"/> or <see cref="Exception"/>.</returns>
        [HttpGet, Route("One/{id}")]
        public Task<IActionResult> GetOne(string id)
        {
            if (!GetAuthCheck())
                return Task.FromResult<IActionResult>(Unauthorized());

            using AdoDataConnection connection = CreateConnection();
            TableOperations<T> tableOperations = new(connection);
            T? result = tableOperations.QueryRecord(new RecordRestriction($"{PrimaryKeyField} = {{0}}",id));

            return result is null ? 
                Task.FromResult<IActionResult>(NotFound()) : 
                Task.FromResult<IActionResult>(Ok(result));
        }

        /// <summary>
        /// Gets all records from associated table matching the provided search criteria.
        /// </summary>
        /// <param name="postData">Search criteria.</param>
        /// <param name="page">the 0 based page number to be returned.</param>
        /// <returns>An <see cref="IActionResult"/> containing <see cref="IEnumerable{T}"/> or <see cref="Exception"/>.</returns>
        [HttpPost, Route("Search/{page:min(0)}")]
        public Task<IActionResult> Search([FromBody] SearchPost<T> postData, int page)
        {
            if (!GetAuthCheck())
                return Task.FromResult<IActionResult>(Unauthorized());

            using AdoDataConnection connection = CreateConnection();
            TableOperations<T> tableOperations = new(connection);
            IEnumerable<T> result = tableOperations.QueryRecords(postData.OrderBy, postData.Ascending, page, PageSize, postData.Searches.ToArray());

            return Task.FromResult<IActionResult>(Ok(result));
        }

        /// <summary>
        /// Gets the pagination information for the provided search criteria.
        /// </summary>
        /// <param name="postData">Search criteria.</param>
        /// <returns>A <see cref="PageInfo"/> object containing the pagination information or <see cref="Exception"/>.</returns>
        [HttpPost, Route("PageInfo")]
        public Task<IActionResult> GetPageInfo(SearchPost<T> postData)
        {
            if (!GetAuthCheck())
                return Task.FromResult<IActionResult>(Unauthorized());

            using AdoDataConnection connection = CreateConnection();
            TableOperations<T> tableOperations = new(connection);
            int recordCount = tableOperations.QueryRecordCount(postData.Searches.ToArray());

            return Task.FromResult<IActionResult>(Ok(new PageInfo()
            {
                PageSize = PageSize,
                PageCount = (int)Math.Ceiling(recordCount / (double)PageSize),
                TotalCount = recordCount
            }));
        }

        /// <summary>
        /// Gets pagination information.
        /// </summary>
        /// <returns>A <see cref="PageInfo"/> object containing the pagination information or <see cref="Exception"/>.</returns>
        [HttpGet, Route("PageInfo")]
        public Task<IActionResult> GetPageInfo()
        {
            if (!GetAuthCheck())
                return Task.FromResult<IActionResult>(Unauthorized());
            
            using AdoDataConnection connection = CreateConnection();
            TableOperations<T> tableOperations = new(connection);
            int recordCount = tableOperations.QueryRecordCount((RecordRestriction?)null);
            
            return Task.FromResult<IActionResult>(Ok(new PageInfo()
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
