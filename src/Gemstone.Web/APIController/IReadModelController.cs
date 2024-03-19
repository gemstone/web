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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Gemstone.Web.APIController
{
    /// <summary>
    /// Defines an interface for a common ModelController
    /// </summary>
    public interface IReadModelController
    {
        /// <summary>
        /// endpoint to get all Models related to a given <see cref={parentID}/>
        /// </summary>
        public IHttpActionResult Get(string parentID) {}

        /// <summary>
        /// endpoint to get all Models in <see cref={sort} /> order.
        /// </summary>
        public IHttpActionResult Get(string sort, int ascending) {}

        /// <summary>
        /// endpoint to get all Models in <see cref={sort} /> order related to a given <see cref={parentID} />
        /// </summary>
        public IHttpActionResult Get(string parentID, string sort, int ascending) {}
        
        /// <summary>
        /// endpoint to get a specific Model 
        /// </summary>    
        public IHttpActionResult GetOne(string id) {}

        /// <summary>
        /// endpoint to search models
        /// </summary>
        public IHttpActionResult Search(PostData postData, int page) {}
    }
}
