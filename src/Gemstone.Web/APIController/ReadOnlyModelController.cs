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
using System.Runtime.Caching;
using System.Threading;
using System.Threading.Tasks;
using Gemstone.Caching;
using Gemstone.Configuration;
using Gemstone.Data;
using Gemstone.Data.Model;
using Gemstone.Security.AccessControl;
using Microsoft.AspNetCore.Mvc;

namespace Gemstone.Web.APIController
{
    /// <summary>
    /// Represents a readonly ModelController.
    /// </summary>
    public class ReadOnlyModelController<T> : ControllerBase, IReadOnlyModelController<T> where T : class, new()
    {
        #region [ Netsted Types ]

        private class ConnectionCache : IDisposable
        {
            public string Token { get; } = Guid.NewGuid().ToString();

            public SecureTableOperations<T> Table { get; }

            public IAsyncEnumerator<T?>? Records { get; set; }

            private readonly AdoDataConnection m_connection;

            private ConnectionCache()
            {
                m_connection = new AdoDataConnection(Settings.Default);
                Table = new SecureTableOperations<T>(m_connection);
            }

            public void Dispose()
            {
                m_connection.Dispose();
            }

            public static ConnectionCache Create(double expiration)
            {
                ConnectionCache cache = new();

                MemoryCache<ConnectionCache>.GetOrAdd(cache.Token, expiration, () => cache, Dispose);

                return cache;
            }

            public static bool TryGet(string token, out ConnectionCache? cache)
            {
                return MemoryCache<ConnectionCache>.TryGet(token, out cache);
            }

            public static bool Close(string token)
            {
                if (!TryGet(token, out ConnectionCache? cache))
                    return false;

                cache?.Dispose();

                MemoryCache<ConnectionCache>.Remove(token);

                return true;
            }

            private static void Dispose(CacheEntryRemovedArguments arguments)
            {
                if (arguments.RemovedReason != CacheEntryRemovedReason.Removed)
                    return;

                if (arguments.CacheItem.Value is ConnectionCache cache)
                    cache.Dispose();
            }
        }

        #endregion

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
        /// Opens a connection to the database and establishes a query operation, returning a token to be used for subsequent data requests.
        /// </summary>
        /// <param name="filterExpression">Filter expression to be used for the query.</param>
        /// <param name="parameters">Parameters to be used for the query.</param>
        /// <param name="expiration">Expiration time for the query data, in minutes, if not accessed. Defaults to 1 minute if not provided.</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>An <see cref="IActionResult"/> containing a token to be used for subsequent requests.</returns>
        [HttpGet, Route("Open/{filterExpression}/{parameters}/{expiration:double?}")]
        public Task<IActionResult> Open(string? filterExpression, object?[] parameters, double? expiration, CancellationToken cancellationToken)
        {
            ConnectionCache cache = ConnectionCache.Create(expiration ?? 1.0D);

            cache.Records = cache.Table.QueryRecordsWhereAsync(HttpContext.User, filterExpression, cancellationToken, parameters).GetAsyncEnumerator(cancellationToken);

            return Task.FromResult<IActionResult>(Ok(cache.Token));
        }

        /// <summary>
        /// Gets the next set of records from the query operation associated with the provided token.
        /// </summary>
        /// <param name="token">Token associated with the query operation.</param>
        /// <param name="count">Maximum number of records to return. Defaults 1 record if not provided.</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>
        /// An <see cref="IActionResult"/> containing the next set of records as <see cref="List{T}"/>.
        /// </returns>
        /// <remarks>
        /// When end of enumeration is reached, an empty result list is returned.
        /// </remarks>
        [HttpGet, Route("Next/{token}/{count:int?}")]
        public async Task<IActionResult> Next(string token, int? count, CancellationToken cancellationToken)
        {
            if (!ConnectionCache.TryGet(token, out ConnectionCache? cache) || cache is null)
                return NotFound();

            List<T?> records = [];

            if (cache.Records is not null)
            {
                for (int i = 0; i < (count ?? 1); i++)
                {
                    if (!await cache.Records.MoveNextAsync(cancellationToken).ConfigureAwait(false))
                        break;

                    records.Add(cache.Records.Current);
                }
            }

            return Ok(records);
        }

        /// <summary>
        /// Closes the query operation associated with the provided token.
        /// </summary>
        /// <param name="token">Token associated with the query operation.</param>
        [HttpGet, Route("Close/{token}")]
        public IActionResult Close(string token)
        {
            return ConnectionCache.Close(token) ? Ok() : NotFound();
        }

        /// <summary>
        /// Gets all records from associated table, filtered to parent keys if provided.
        /// </summary>
        /// <param name="parentID">Parent ID to be used if table has set parent keys.</param>
        /// <param name="page">The 0 based page number to be returned.</param>
        /// <param name="cancellationToken">Propagates notification that operations should be canceled.</param>
        /// <returns>An <see cref="IActionResult"/> containing <see cref="T:T[]"/> or <see cref="Exception"/>.</returns>
        [HttpGet, Route("{page:min(0)}/{parentID?}")]
        public virtual async Task<IActionResult> Get(string? parentID, int page, CancellationToken cancellationToken)
        {
            await using AdoDataConnection connection = CreateConnection();
            SecureTableOperations<T> tableOperations = new(connection);
            RecordFilter<T>? filter = null;

            if (ParentKey != string.Empty && parentID is not null)
            {
                filter = new RecordFilter<T>()
                {
                    FieldName = ParentKey,
                    Operator = "=",
                    SearchParameter = parentID
                };
            }

            IAsyncEnumerable<T> result = tableOperations.QueryRecordsAsync(HttpContext.User, DefaultSort, DefaultSortDirection, page, PageSize, cancellationToken, filter);

            return Ok(await result.ToArrayAsync(cancellationToken).ConfigureAwait(false));
        }

        /// <summary>
        /// Gets all records from associated table sorted by the provided field.
        /// </summary>
        /// <param name="sort">Field to be used for sorting.</param>
        /// <param name="ascending">Flag when <c>true</c> will sort ascending; otherwise, descending.</param>
        /// <param name="page">The 0 based page number to be returned.</param>
        /// <param name="cancellationToken">Propagates notification that operations should be canceled.</param>
        /// <returns>An <see cref="IActionResult"/> containing <see cref="T:T[]"/> or <see cref="Exception"/>.</returns>
        [HttpGet, Route("{page:min(0)}/{sort}/{ascending:bool}")]
        public virtual async Task<IActionResult> Get(string sort, bool ascending, int page, CancellationToken cancellationToken)
        {
            await using AdoDataConnection connection = CreateConnection();
            SecureTableOperations<T> tableOperations = new(connection);
            RecordFilter<T>? filter = null;

            IAsyncEnumerable<T> result = tableOperations.QueryRecordsAsync(HttpContext.User, sort, ascending, page, PageSize, cancellationToken, filter);

            return Ok(await result.ToArrayAsync(cancellationToken).ConfigureAwait(false));
        }

        /// <summary>
        /// Gets all records from associated table sorted by the provided field.
        /// </summary>
        /// <param name="sort">Field to be used for sorting.</param>
        /// <param name="ascending">Flag when <c>true</c> will sort ascending; otherwise, descending.</param>
        /// <param name="page">The 0 based page number to be returned.</param>
        /// <param name="parentID">Parent ID to be used if table has set parent keys.</param>
        /// <param name="cancellationToken">Propagates notification that operations should be canceled.</param>
        /// <returns>An <see cref="IActionResult"/> containing <see cref="T:T[]"/> or <see cref="Exception"/>.</returns>
        [HttpGet, Route("{page:min(0)}/{parentID}/{sort}/{ascending:bool}")]
        public virtual async Task<IActionResult> Get(string parentID, string sort, bool ascending, int page, CancellationToken cancellationToken)
        {
            await using AdoDataConnection connection = CreateConnection();
            SecureTableOperations<T> tableOperations = new(connection);
            RecordFilter<T> filter = new()
            {
                FieldName = ParentKey,
                Operator = "=",
                SearchParameter = parentID
            };

            IAsyncEnumerable<T> result = tableOperations.QueryRecordsAsync(HttpContext.User, sort, ascending, page, PageSize, cancellationToken, filter);

            return Ok(await result.ToArrayAsync(cancellationToken).ConfigureAwait(false));
        }

        /// <summary>
        /// Gets a single record from associated table.
        /// </summary>
        /// <param name="id">The PrimaryKey value of the Model to be returned.</param>
        /// <param name="cancellationToken">Propagates notification that operations should be canceled.</param>
        /// <returns>An <see cref="IActionResult"/> containing a <see cref="T"/> or <see cref="Exception"/>.</returns>
        [HttpGet, Route("One/{id}")]
        public virtual async Task<IActionResult> GetOne(string id, CancellationToken cancellationToken)
        {
            await using AdoDataConnection connection = CreateConnection();
            SecureTableOperations<T> tableOperations = new(connection);
            T? result = await tableOperations.QueryRecordAsync(HttpContext.User, new RecordRestriction($"{PrimaryKeyField} = {{0}}", id), cancellationToken).ConfigureAwait(false);

            return result is null ?
                NotFound() :
                Ok(result);
        }

        /// <summary>
        /// Gets all records from associated table matching the provided search criteria.
        /// </summary>
        /// <param name="postData">Search criteria.</param>
        /// <param name="page">the 0 based page number to be returned.</param>
        /// <param name="parentID">Parent ID to be used if table has set parent keys.</param>
        /// <param name="cancellationToken">Propagates notification that operations should be canceled.</param>
        /// <returns>An <see cref="IActionResult"/> containing <see cref="T:T[]"/> or <see cref="Exception"/>.</returns>
        [HttpPost, Route("Search/{page:min(0)}/{parentID?}")]
        [ResourceAccess(ResourceAccessType.Read)]
        public virtual async Task<IActionResult> Search([FromBody] SearchPost<T> postData, int page, string? parentID, CancellationToken cancellationToken)
        {
            await using AdoDataConnection connection = CreateConnection();
            SecureTableOperations<T> tableOperations = new(connection);
            RecordFilter<T>[] filters = postData.Searches.ToArray();

            if (ParentKey != string.Empty && parentID is not null)
            {
                filters.Append(new RecordFilter<T>()
                {
                    FieldName = ParentKey,
                    Operator = "=",
                    SearchParameter = parentID
                });
            }

            IAsyncEnumerable<T> result = tableOperations.QueryRecordsAsync(HttpContext.User, postData.OrderBy, postData.Ascending, page, PageSize, cancellationToken, filters);

            return Ok(await result.ToArrayAsync(cancellationToken).ConfigureAwait(false));
        }

        /// <summary>
        /// Gets the pagination information for the provided search criteria.
        /// </summary>
        /// <param name="postData">Search criteria.</param>
        /// <param name="parentID">Parent ID to be used if table has set parent keys.</param>
        /// <param name="cancellationToken">Propagates notification that operations should be canceled.</param>
        /// <returns>A <see cref="PageInfo"/> object containing the pagination information or <see cref="Exception"/>.</returns>
        [HttpPost, Route("PageInfo/{parentID?}")]
        [ResourceAccess(ResourceAccessType.Read)]
        public virtual async Task<IActionResult> GetPageInfo([FromBody] SearchPost<T> postData, string? parentID, CancellationToken cancellationToken)
        {
            await using AdoDataConnection connection = CreateConnection();
            SecureTableOperations<T> tableOperations = new(connection);
            RecordFilter<T>[] filters = postData.Searches.ToArray();

            if (ParentKey != string.Empty && parentID is not null)
            {
                filters.Append(new RecordFilter<T>()
                {
                    FieldName = ParentKey,
                    Operator = "=",
                    SearchParameter = parentID
                });
            }

            int recordCount = await tableOperations.QueryRecordCountAsync(HttpContext.User, cancellationToken, filters).ConfigureAwait(false);

            return Ok(new PageInfo()
            {
                PageSize = PageSize,
                PageCount = (int)Math.Ceiling(recordCount / (double)PageSize),
                TotalCount = recordCount
            });
        }

        /// <summary>
        /// Gets pagination information.
        /// </summary>
        /// <param name="cancellationToken">Propagates notification that operations should be canceled.</param>
        /// <param name="parentID">Parent ID to be used if table has set parent keys.</param>

        /// <returns>A <see cref="PageInfo"/> object containing the pagination information or <see cref="Exception"/>.</returns>
        [HttpGet, Route("PageInfo/{parentID?}")]
        public virtual async Task<IActionResult> GetPageInfo(string? parentID, CancellationToken cancellationToken)
        {
            await using AdoDataConnection connection = CreateConnection();
            SecureTableOperations<T> tableOperations = new(connection);
            RecordFilter<T>[] filters = [];

            if (ParentKey != string.Empty && parentID is not null)
            {
                filters.Append(new RecordFilter<T>()
                {
                    FieldName = ParentKey,
                    Operator = "=",
                    SearchParameter = parentID
                });
            }

            int recordCount = await tableOperations.QueryRecordCountAsync(HttpContext.User, cancellationToken, filters).ConfigureAwait(false);

            return Ok(new PageInfo()
            {
                PageSize = PageSize,
                PageCount = (int)Math.Ceiling(recordCount / (double)PageSize),
                TotalCount = recordCount
            });
        }

        /// <summary>
        /// Gets a new record.
        /// </summary>
        /// <param name="cancellationToken">Propagates notification that operations should be canceled.</param>
        /// <returns>A <see cref="T"/> object or <see cref="Exception"/>.</returns>
        [HttpGet, Route("New")]
        public virtual async Task<IActionResult> New(CancellationToken cancellationToken)
        {
            await using AdoDataConnection connection = CreateConnection();
            SecureTableOperations<T> tableOperations = new(connection);

            T? result = tableOperations.NewRecord();
            return Ok(result);
        }

        /// <summary>
        /// Gets max value for the specified field.
        /// </summary>
        /// <param name="fieldName">Field to find max value for</param>
        /// <param name="cancellationToken">Propagates notification that operations should be canceled.</param>
        /// <returns>An <see cref="int"/>.</returns>
        [HttpGet, Route("Max/{fieldName}")]
        public virtual async Task<IActionResult> GetMaxValue(string fieldName, CancellationToken cancellationToken)
        {
            // Validate that the field exists on the model T using reflection
            PropertyInfo? property = typeof(T).GetProperty(fieldName);
            if (property is null)
                return BadRequest();

            // Create a connection and table operations instance
            await using AdoDataConnection connection = CreateConnection();
            SecureTableOperations<T> tableOperations = new(connection);
            string tableName = tableOperations.TableName;
            string sql = $"SELECT MAX([{fieldName}]) FROM [{tableName}]";

            object? maxValue = await connection.ExecuteScalarAsync(sql, cancellationToken).ConfigureAwait(false);

            return Ok(maxValue ?? 0);
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
