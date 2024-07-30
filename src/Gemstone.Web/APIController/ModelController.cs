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
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Gemstone.Data;
using Gemstone.Data.Model;
using Microsoft.AspNetCore.Mvc;

namespace Gemstone.Web.APIController
{
    /// <summary>
    /// Defines a ModelController 
    /// </summary>
    public class ModelController<T> : ReadModelController<T>, IModelController<T> where T : class, new()
    {
        #region [ Constructor ]

        /// <summary>
        /// Creates a new <see cref="ReadModelController{T}"/>
        /// </summary>
        public ModelController(): base()
        {
            DeleteRoles = typeof(T).GetCustomAttribute<DeleteRolesAttribute>()?.Roles ?? "";
            PostRoles = typeof(T).GetCustomAttribute<PostRolesAttribute>()?.Roles ?? "";
            PatchRoles = typeof(T).GetCustomAttribute<PatchRolesAttribute>()?.Roles ?? "";
        }

        #endregion

        #region [ Properties ]
        protected string PatchRoles { get; } = "";
        protected string PostRoles { get; } = "";
        protected string DeleteRoles { get; } = "";

        #endregion

        /// <summary>
        /// Updates a record from associated table
        /// </summary>
        /// <param name="record">The record to be updated</param>
        /// <returns><see cref="IActionResult"/> containing the new record <see cref="T"/> or <see cref="Exception"/></returns>
        [HttpPatch, Route("")]
        public IActionResult Patch(T record)
        {
            if (!PatchAuthCheck())
                return Unauthorized();

            using (AdoDataConnection connection = CreateConnection())
            {
                TableOperations<T> tableOperations = new TableOperations<T>(connection);
                tableOperations.UpdateRecord(record);
            }
            return Ok(record);
        }

        /// <summary>
        /// creates new records in associated table
        /// </summary>
        /// <param name="record"> The record to be created </param>
        /// <returns><see cref="IActionResult"/> containing the new <see cref="T"/> or <see cref="Exception"/></returns>
        [HttpPost, Route("")]
        public IActionResult Post(T record)
        {
            if (!PostAuthCheck())
                return Unauthorized();

            
            using (AdoDataConnection connection = CreateConnection())
            {
                TableOperations<T> tableOperations = new TableOperations<T>(connection);
                tableOperations.AddNewRecord(record);
            }
            return Ok(record);
        }


        /// <summary>
        /// Deletes a record from associated table
        /// </summary>
        /// <param name="record"> The record to be deleted </param>
        /// <returns><see cref="IActionResult"/> containing 1 or <see cref="Exception"/></returns>
        [HttpDelete, Route("")]
        public IActionResult Delete(T record)
        {
            if (!DeleteAuthCheck())
                return Unauthorized();


            using (AdoDataConnection connection = CreateConnection())
            {
                TableOperations<T> tableOperations = new TableOperations<T>(connection);
                tableOperations.DeleteRecord(record);
            }
            return Ok(1);
        }


        /// <summary>
        /// Deletes a record from associated table
        /// </summary>
        /// <param name="id"> The primary key of the record to be deleted </param>
        /// <returns><see cref="IActionResult"/> containing 1 or <see cref="Exception"/></returns>
        [HttpDelete, Route("{id}")]
        public IActionResult Delete(string id)
        {
            if(!DeleteAuthCheck())
                return Unauthorized();


            using (AdoDataConnection connection = CreateConnection())
            {
                TableOperations<T> tableOperations = new TableOperations<T>(connection);
                tableOperations.DeleteRecordWhere($"{PrimaryKeyField} = {{0}}", id);
            }
            return Ok(1);
        }

        #region [ Methods ]

        /// <summary>
        /// Check if current User is authorized for POST Requests
        /// </summary>
        /// <returns>True if User is authorized for POST requests</returns>
        protected bool PostAuthCheck()
        {
            return PostRoles == string.Empty || User.IsInRole(PostRoles);
        }

        /// <summary>
        /// Check if current User is authorized for DELETE Requests
        /// </summary>
        /// <returns>True if User is authorized for DELETE requests</returns>
        protected bool DeleteAuthCheck()
        {
            return DeleteRoles == string.Empty || User.IsInRole(DeleteRoles);
        }

        /// <summary>
        /// Check if current User is authorized for PATCH Requests
        /// </summary>
        /// <returns>True if User is authorized for PATCH requests</returns>
        protected bool PatchAuthCheck()
        {
            return PatchRoles == string.Empty || User.IsInRole(PatchRoles);
        }

        #endregion
    }
}
