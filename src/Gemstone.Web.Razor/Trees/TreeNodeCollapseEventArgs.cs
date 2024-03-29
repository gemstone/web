﻿//******************************************************************************************************
//  TreeNodeEventArgs.cs - Gbtc
//
//  Copyright © 2022, Grid Protection Alliance.  All Rights Reserved.
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
//  07/04/2020 - Stephen C. Wills
//       Generated original version of source code.
//
//******************************************************************************************************

using System;

namespace Gemstone.Web.Razor.Trees
{
    /// <summary>
    /// Arguments for collapsed and expanded events.
    /// </summary>
    public class TreeNodeCollapseEventArgs : EventArgs
    {
        /// <summary>
        /// The tree view containing the node that was collapsed or expanded.
        /// </summary>
        public TreeView? View { get; }

        /// <summary>
        /// The node that was collapsed or expanded.
        /// </summary>
        public TreeNode Node { get; }

        internal TreeNodeCollapseEventArgs(TreeView? view, TreeNode node)
        {
            View = view;
            Node = node;
        }
    }
}
