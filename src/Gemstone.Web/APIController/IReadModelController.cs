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

using System.Web.Http;

namespace Gemstone.Web.APIController
{
    /// <summary>
    /// Defines an interface for a common ModelController
    /// </summary>
    public interface IReadModelController<T> where T : class, new()
    {
        /// <summary>
        /// endpoint to get all Models related to a given ParentID
        /// </summary>
        public IHttpActionResult Get(string parentID, int page);

        /// <summary>
        /// endpoint to get all Models in order.
        /// </summary>
        public IHttpActionResult Get(string sort, int ascending, int page);

        /// <summary>
        /// endpoint to get all Models in order related to a given ParentID
        /// </summary>
        public IHttpActionResult Get(string parentID, string sort, int ascending, int page) ;
        
        /// <summary>
        /// endpoint to get a specific Model 
        /// </summary>    
        public IHttpActionResult GetOne(string id);

        /// <summary>
        /// endpoint to search models
        /// </summary>
        public IHttpActionResult Search(SearchPost<T> postData, int page);
    }
}
