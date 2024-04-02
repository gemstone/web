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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Claims;
using System.Web.Http;
using System.Web.Http.Results;
using Azure;
using Gemstone.Data;
using Gemstone.Data.Model;

namespace Gemstone.Web.APIController
{
    /// <summary>
    /// Defines a Read Only ModelController 
    /// </summary>
    public class ReadModelController<T> : ApiController, IReadModelController<T> where T : class, new()
    {
        #region [ Constructor ]

        /// <summary>
        /// Creates a new <see cref="ReadModelController{T}"/>
        /// </summary>
        public ReadModelController()
        {
            PrimaryKeyField = typeof(T).GetProperties().FirstOrDefault(p => p.GetCustomAttributes<PrimaryKeyAttribute>().Any())?.Name ?? "ID";

            ParentKey = typeof(T).GetProperties().FirstOrDefault(p => p.GetCustomAttributes<ParentKeyAttribute>().Any())?.Name ?? "";

            PropertyInfo? pi = typeof(T).GetProperties().FirstOrDefault(p => p.GetCustomAttributes<DefaultSortOrderAttribute>().Any());
            DefaultSortOrderAttribute? dsoa = pi?.GetCustomAttribute<DefaultSortOrderAttribute>();

            if (dsoa != null)
            {
                DefaultSort = $"{pi?.Name}";
                DefaultSortDirection = dsoa.Ascending;
            }

            GetRoles = typeof(T).GetCustomAttribute<GetRolesAttribute>()?.Roles ?? "";
            PageSize = typeof(T).GetCustomAttribute<PageSizeAttribute>()?.Limit ?? 50;
        }

        #endregion

        #region [ Properties ]

        protected string PrimaryKeyField { get; set; } = "ID";
        protected string ParentKey { get; set; } = "";
        protected string? DefaultSort { get; } = null;
        protected bool DefaultSortDirection { get; } = false;
        protected string GetRoles { get; } = "";
        private int PageSize { get; } = 50;

        #endregion

        /// <summary>
        /// Gets all records from associated table, filtered to parent key ID if provided
        /// </summary>
        /// <param name="parentID">Parent ID to be used if Table has a set Parent Key</param>
        /// <param name="page">the 0 based page number to be returned</param>
        /// <returns><see cref="IHttpActionResult"/> containing <see cref="IEnumerable{T}"/> or <see cref="Exception"/></returns>
        [HttpGet, Route("{page:min(0)}/{parentID?}")]
        public IHttpActionResult Get(string parentID, int page)
        {
            if (!GetAuthCheck())
                return Unauthorized();

            IEnumerable<T> result;
            using (AdoDataConnection connection = CreateConnection())
            {
                TableOperations<T> tableOperations = new TableOperations<T>(connection);
                RecordFilter<T>? filter = null;

                if (ParentKey != string.Empty && parentID != null)
                {
                    filter = new RecordFilter<T>() { 
                        FieldName = ParentKey,
                        Operator = "=",
                        SearchParameter = parentID
                    };
                }

                result = tableOperations.QueryRecords(DefaultSort, DefaultSortDirection, page, PageSize, filter );
            }
            return Ok(result);
        }

        /// <summary>
        /// Gets all records from associated table sorted by the provided field
        /// </summary>
        /// <param name="sort"> Field to be used for sorting </param>
        /// <param name="ascending"> true t sort by ascending </param>
        /// <param name="page">the 0 based page number to be returned</param>
        /// <returns><see cref="IHttpActionResult"/> containing <see cref="IEnumerable{T}"/> or <see cref="Exception"/></returns>
        [HttpGet, Route("{page:min(0)}/{sort}/{ascending:bool}")]
        public IHttpActionResult Get(string sort, bool ascending, int page)
        {
            if (!GetAuthCheck())
                return Unauthorized();

            IEnumerable<T> result;
            using (AdoDataConnection connection = CreateConnection())
            {
                TableOperations<T> tableOperations = new TableOperations<T>(connection);
                RecordFilter<T>? filter = null;

                result = tableOperations.QueryRecords(sort, ascending, page, PageSize, filter);
            }
            return Ok(result);
        }


        /// <summary>
        /// Gets all records from associated table sorted by the provided field
        /// </summary>
        /// <param name="sort"> Field to be used for sorting </param>
        /// <param name="ascending"> true t sort by ascending </param>
        /// <param name="page">the 0 based page number to be returned</param>
        /// <param name="parentID">Parent ID to be used if Table has a set Parent Key</param>
        /// <returns><see cref="IHttpActionResult"/> containing <see cref="IEnumerable{T}"/> or <see cref="Exception"/></returns>
        [HttpGet, Route("{page:min(0)}/{parentID}/{sort}/{ascending:bool}")]
        public IHttpActionResult Get(string parentID, string sort, bool ascending, int page)
        {
            if (!GetAuthCheck())
                return Unauthorized();

            IEnumerable<T> result;
            using (AdoDataConnection connection = CreateConnection())
            {
                TableOperations<T> tableOperations = new TableOperations<T>(connection);
                RecordFilter<T> filter = new RecordFilter<T>()
                    {
                        FieldName = ParentKey,
                        Operator = "=",
                        SearchParameter = parentID
                    };
                

                result = tableOperations.QueryRecords(sort, ascending, page, PageSize, filter);
            }
            return Ok(result);
        }

        /// <summary>
        /// Gets a single record from associated table
        /// </summary>
        /// <param name="id"> The PrimaryKey value of the Model to be returned </param>
        /// <returns><see cref="IHttpActionResult"/> containing a <see cref="T"/> or <see cref="Exception"/></returns>
        [HttpGet, Route("One/{id}")]
        public IHttpActionResult GetOne(string id)
        {
            if (!GetAuthCheck())
                return Unauthorized();

            T? result;
            using (AdoDataConnection connection = CreateConnection())
            {
                TableOperations<T> tableOperations = new TableOperations<T>(connection);
                result = tableOperations.QueryRecord(new RecordRestriction($"{PrimaryKeyField} = {{0}}",id));
            }

            if (result is null)
                return NotFound();

            return Ok(result);
        }

        /// <summary>
        /// Gets all records from associated table matching the provided search criteria
        /// </summary>
        /// <param name="postData"> The Search Criteria </param>
        /// <param name="page">the 0 based page number to be returned</param>
        /// <returns><see cref="IHttpActionResult"/> containing <see cref="IEnumerable{T}"/> or <see cref="Exception"/></returns>
        [HttpPost, Route("Search/{page:min(0)}")]
        public IHttpActionResult Search([FromBody] SearchPost<T> postData, int page)
        {
            if (!GetAuthCheck())
                return Unauthorized();

            IEnumerable<T> result;
            using (AdoDataConnection connection = CreateConnection())
            {
                TableOperations<T> tableOperations = new TableOperations<T>(connection);
                result = tableOperations.QueryRecords(postData.OrderBy, postData.Ascending, page, PageSize, postData.Searches.ToArray());
            }

            return Ok(result);
        }

        /// <summary>
        /// Gets the Pagination Information for the provided Search Criteria
        /// </summary>
        /// <param name="postData"> search criteria</param>
        /// <returns> a <see cref="PageInfo"/> object containting the pageination information or <see cref="Exception"/> </returns>
        [HttpPost, Route("PageInfo")]
        public IHttpActionResult GetPageInfo(SearchPost<T> postData)
        {
            if (!GetAuthCheck())
                return Unauthorized();

            using (AdoDataConnection connection = CreateConnection())
            {
                TableOperations<T> tableOperations = new TableOperations<T>(connection);
                int nRecords = tableOperations.QueryRecordCount(postData.Searches.ToArray());
                return Ok(new PageInfo()
                {
                    PageSize = PageSize,
                    PageCount = (int)Math.Ceiling((double)nRecords/(double)PageSize),
                    TotalCount = nRecords
                });
            }
        }

        /// <summary>
        /// Gets the Pagination Information
        /// </summary>
        /// <returns> a <see cref="PageInfo"/> object containting the pageination information or <see cref="Exception"/> </returns>
        [HttpGet, Route("PageInfo")]
        public IHttpActionResult GetPageInfo()
        {
            if (!GetAuthCheck())
                return Unauthorized();
            using (AdoDataConnection connection = CreateConnection())
            {
                TableOperations<T> tableOperations = new TableOperations<T>(connection);
                int nRecords = tableOperations.QueryRecordCount((RecordRestriction?)null);
                return Ok(new PageInfo()
                {
                    PageSize = PageSize,
                    PageCount = (int)Math.Ceiling((double)nRecords / (double)PageSize),
                    TotalCount = nRecords
                });
            }
        }
        #region [ Methods ]

        /// <summary>
        /// Check if current User is authorized for GET Requests
        /// </summary>
        /// <returns>True if User is authorized for GET requests</returns>
        protected bool GetAuthCheck()
        {
            return GetRoles == string.Empty || User.IsInRole(GetRoles);
        }

        /// <summary>
        /// Create the <see cref="AdoDataConnection"/> for the controller.
        /// </summary>
        /// <returns>a new <see cref="AdoDataConnection"/></returns>
        protected AdoDataConnection CreateConnection() => new AdoDataConnection("systemSettings");

       
        #endregion
    }
}
