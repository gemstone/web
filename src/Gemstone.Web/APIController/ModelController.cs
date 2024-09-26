//******************************************************************************************************
//  ModelController.cs - Gbtc
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
//  07/29/2024 - C. Lackner
//       Generated original version of source code.
//
//******************************************************************************************************

using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Gemstone.Data;
using Gemstone.Data.Model;
using Microsoft.AspNetCore.Mvc;

namespace Gemstone.Web.APIController
{
    /// <summary>
    /// Represents a ModelController.
    /// </summary>
    public class ModelController<T> : ReadOnlyModelController<T>, IModelController<T> where T : class, new()
    {
        #region [ Constructor ]

        /// <summary>
        /// Creates a new <see cref="ReadOnlyModelController{T}"/>
        /// </summary>
        public ModelController()
        {
            DeleteRoles = typeof(T).GetCustomAttribute<DeleteRolesAttribute>()?.Roles ?? "";
            PostRoles = typeof(T).GetCustomAttribute<PostRolesAttribute>()?.Roles ?? "";
            PatchRoles = typeof(T).GetCustomAttribute<PatchRolesAttribute>()?.Roles ?? "";
        }

        #endregion

        #region [ Properties ]

        /// <summary>
        /// Gets the roles required for PATCH requests.
        /// </summary>
        protected string PatchRoles { get; }

        /// <summary>
        /// Gets the roles required for POST requests.
        /// </summary>
        protected string PostRoles { get; }

        /// <summary>
        /// Gets the roles required for DELETE requests.
        /// </summary>
        protected string DeleteRoles { get; }

        #endregion

        /// <summary>
        /// Updates a record from associated table.
        /// </summary>
        /// <param name="record">The record to be updated.</param>
        /// <param name="cancellationToken">Propagates notification that operations should be canceled.</param>
        /// <returns>An <see cref="IActionResult"/> containing the new record <see cref="T"/> or <see cref="Exception"/>.</returns>
        [HttpPatch, Route("")]
        public async Task<IActionResult> Patch(T record, CancellationToken cancellationToken)
        {
            if (!PatchAuthCheck())
                return await Task.FromResult<IActionResult>(Unauthorized());

            await using AdoDataConnection connection = CreateConnection();
            TableOperations<T> tableOperations = new(connection);
            await tableOperations.UpdateRecordAsync(record, cancellationToken);

            return await Task.FromResult<IActionResult>(Ok(record));
        }

        /// <summary>
        /// Creates new record in associated table.
        /// </summary>
        /// <param name="record">The record to be created.</param>
        /// <param name="cancellationToken">Propagates notification that operations should be canceled.</param>
        /// <returns>An <see cref="IActionResult"/> containing the new <see cref="T"/> or <see cref="Exception"/>.</returns>
        [HttpPost, Route("")]
        public async Task<IActionResult> Post(T record, CancellationToken cancellationToken)
        {
            if (!PostAuthCheck())
                return await Task.FromResult<IActionResult>(Unauthorized());

            await using AdoDataConnection connection = CreateConnection();
            TableOperations<T> tableOperations = new(connection);
            await tableOperations.AddNewRecordAsync(record, cancellationToken);

            return await Task.FromResult<IActionResult>(Ok(record));
        }

        /// <summary>
        /// Deletes a record from associated table.
        /// </summary>
        /// <param name="record">The record to be deleted.</param>
        /// <param name="cancellationToken">Propagates notification that operations should be canceled.</param>
        /// <returns>An <see cref="IActionResult"/> containing 1 or <see cref="Exception"/>.</returns>
        [HttpDelete, Route("")]
        public async Task<IActionResult> Delete(T record, CancellationToken cancellationToken)
        {
            if (!DeleteAuthCheck())
                return await Task.FromResult<IActionResult>(Unauthorized());

            await using AdoDataConnection connection = CreateConnection();
            TableOperations<T> tableOperations = new(connection);
            await tableOperations.DeleteRecordAsync(cancellationToken, record);

            return await Task.FromResult<IActionResult>(Ok(1));
        }

        /// <summary>
        /// Deletes a record from associated table by primary key.
        /// </summary>
        /// <param name="id">The primary key of the record to be deleted.</param>
        /// <param name="cancellationToken">Propagates notification that operations should be canceled.</param>
        /// <returns>An <see cref="IActionResult"/> containing 1 or <see cref="Exception"/>.</returns>
        [HttpDelete, Route("{id}")]
        public async Task<IActionResult> Delete(string id, CancellationToken cancellationToken)
        {
            if(!DeleteAuthCheck())
                return await Task.FromResult<IActionResult>(Unauthorized());

            await using AdoDataConnection connection = CreateConnection();
            TableOperations<T> tableOperations = new(connection);
            await tableOperations.DeleteRecordWhereAsync($"{PrimaryKeyField} = {{0}}", cancellationToken, id);

            return await Task.FromResult<IActionResult>(Ok(1));
        }

        #region [ Methods ]

        /// <summary>
        /// Check if current user is authorized for POST Requests.
        /// </summary>
        /// <returns><c>true</c> if User is authorized for POST requests; otherwise, <c>false</c>.</returns>
        protected virtual bool PostAuthCheck()
        {
            return PostRoles == string.Empty || User.IsInRole(PostRoles);
        }

        /// <summary>
        /// Check if current user is authorized for DELETE Requests.
        /// </summary>
        /// <returns><c>true</c> if User is authorized for DELETE requests; otherwise, <c>false</c>.</returns>
        protected virtual bool DeleteAuthCheck()
        {
            return DeleteRoles == string.Empty || User.IsInRole(DeleteRoles);
        }

        /// <summary>
        /// Check if current user is authorized for PATCH Requests.
        /// </summary>
        /// <returns><c>true</c> if User is authorized for PATCH requests; otherwise, <c>false</c>.</returns>
        protected virtual bool PatchAuthCheck()
        {
            return PatchRoles == string.Empty || User.IsInRole(PatchRoles);
        }

        #endregion
    }
}
