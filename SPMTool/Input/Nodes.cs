﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.DatabaseServices;
using Extensions.AutoCAD;
using Material.Concrete;
using Material.Reinforcement;
using SPM.Elements;
using SPMTool.AutoCAD;
using UnitsNet;
using static SPMTool.AutoCAD.Auxiliary;
using static SPMTool.AutoCAD.Current;

namespace SPMTool.Input
{
    /// <summary>
    /// Node input class.
    /// </summary>
    public static class Nodes
    {
        /// <summary>
        /// Read <see cref="Node"/> objects from an <see cref="ObjectIdCollection"/>.
        /// </summary>
        /// <param name="nodeObjectsIds">The <see cref="ObjectIdCollection"/> containing the nodes of drawing.</param>
        /// <param name="units">Current <see cref="Units"/>.</param>
        public static Node[] Read(ObjectIdCollection nodeObjectsIds, Units units)
	    {
		   var nodes = new Node[nodeObjectsIds.Count];

		    foreach (ObjectId ndObj in nodeObjectsIds)
		    {
			    var node = Read(ndObj, units);

			    // Set to nodes
			    nodes[node.Number - 1] = node;
		    }

		    // Return the nodes
		    return nodes;
	    }

        /// <summary>
        /// Read a <see cref="Node"/> in the drawing.
        /// </summary>
        /// <param name="objectId">The <see cref="ObjectId"/> of the node.</param>
        /// <param name="units">Current <see cref="Units"/>.</param>
        public static Node Read(ObjectId objectId, Units units)
	    {
		    // Read the object as a point
		    var ndPt = (DBPoint) objectId.ToDBObject();

		    // Read the XData and get the necessary data
		    var data = ndPt.ReadXData(appName);

		    // Get the node number
		    int number = data[(int)XData.Node.Number].ToInt();

			return
				new Node(objectId, number, ndPt.Position, GetNodeType(ndPt), units.Geometry, units.Displacements);
	    }

		/// <summary>
        /// Get <see cref="NodeType"/>.
        /// </summary>
        /// <param name="nodePoint">The <see cref="DBPoint"/> object.</param>
		private static NodeType GetNodeType(DBPoint nodePoint) => nodePoint.Layer == Layers.ExtNode.ToString() ? NodeType.External : NodeType.Internal;
    }
}