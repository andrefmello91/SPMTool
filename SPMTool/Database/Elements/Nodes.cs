using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.MacroRecorder;
using Extensions;
using Extensions.AutoCAD;
using Extensions.Number;
using OnPlaneComponents;
using SPM.Elements;
using SPMTool.Database.Conditions;
using SPMTool.Enums;
using SPMTool.Extensions;
using UnitsNet;
using UnitsNet.Units;

namespace SPMTool.Database.Elements
{
	/// <summary>
	/// Node class.
	/// </summary>
	public class Nodes : EList<NodeObject>
	{
		/// <summary>
		/// The equality comparer for <see cref="Point3d"/>.
		/// </summary>
		public static readonly Point3dComparer Comparer = new Point3dComparer { Tolerance = SettingsData.SavedUnits.Tolerance };

		/// <summary>
		/// Get of nodes' <see cref="Point3d"/> positions.
		/// </summary>
		public IEnumerable<Point3d> Positions => this.Select(n => n.Position);

		private Nodes()
			:base()
		{
			SetEvents();
		}

		private Nodes(IEnumerable<NodeObject> collection)
			: base(collection)
		{
			SetEvents();
		}

		/// <summary>
		/// Add nodes in all necessary positions (stringer start, mid and end points).
		/// </summary>
		public void Add()
		{
			// Get stringers
			var strList = Stringers.Geometries;

			if (strList is null || !strList.Any())
				return;

			// Add external nodes
			var extNds = strList.Select(str => str.InitialPoint).ToList();
			extNds.AddRange(strList.Select(str => str.EndPoint));
			Add(extNds, NodeType.External);

			// Add internal nodes
			Add(strList.Select(str => str.CenterPoint).ToArray(), NodeType.Internal);
		}

		/// <summary>
		/// Add a node to drawing in this <paramref name="position"/>.
		/// </summary>
		/// <param name="position">The <see cref="Point3d"/> position.</param>
		/// <param name="nodeType">The <see cref="NodeType"/>.</param>
		public void Add(Point3d position, NodeType nodeType) => Add(new NodeObject(position, nodeType));

		/// <summary>
		/// Add nodes to drawing in these <paramref name="positions"/>.
		/// </summary>
		/// <param name="positions">The collection of <see cref="Point3d"/> positions.</param>
		/// <param name="nodeType">The <see cref="NodeType"/>.</param>
		public void Add(IEnumerable<Point3d> positions, NodeType nodeType) => Add(positions.Distinct(Comparer).Select(p => new NodeObject(p, nodeType)));

		/// <summary>
		/// Add a <see cref="NodeObject"/> if it doesn't exist in <see cref="Model.Nodes"/>.
		/// </summary>
		public new void Add(NodeObject node)
		{
			if (node.Type is NodeType.Displaced)
			{
				node.AddToDrawing();
				return;
			}

			if (Contains(node))
				return;
			
			AddAndSort(node);
		}

		/// <summary>
		/// Add a collection of <see cref="NodeObject"/>'s if they don't exist in <see cref="Model.Nodes"/>.
		/// </summary>
		public void Add(IEnumerable<NodeObject> nodes)
		{
			var dispNds  = nodes.Where(n => n.Type is NodeType.Displaced).ToList();

			var newNodes = nodes.Distinct().Where(n => !Contains(n) && n.Type != NodeType.Displaced).ToList();

			if (dispNds.Any())
				dispNds.Select(n => n.CreateDBPoint()).ToArray().AddToDrawing();

			AddRangeAndSort(newNodes);
		}

		/// <summary>
		/// Remove unnecessary nodes from the drawing.
		/// </summary>
		public void Remove()
		{
			// Get stringers
			var strList = Stringers.Geometries;

			if (strList is null || !strList.Any())
				Clear();

			else
			{
				// Get the stringer points
				var intPts = strList.Select(str => str.CenterPoint).ToList();
				var extPts = strList.Select(str => str.InitialPoint).ToList();
				extPts.AddRange(strList.Select(str => str.EndPoint));

				// Get positions not needed
				var toRemove = this.Where(n => n.Type is NodeType.Internal && !intPts.Contains(n.Position, Comparer)).ToList();
				toRemove.AddRange(this.Where(n => n.Type is NodeType.External && !extPts.Contains(n.Position, Comparer)));

                // Get duplicated positions
                toRemove.AddRange(this.GroupBy(x => x).Where(g => g.Count() > 1).Select(y => y.Key));

                Remove(toRemove);
			}
		}

		/// <summary>
		/// Remove a <see cref="NodeObject"/> if it exists in <see cref="Model.Nodes"/>.
		/// </summary>
		public new void Remove(NodeObject node)
		{
			if (node.Type is NodeType.Displaced)
			{
				node.ObjectId.RemoveFromDrawing();
				return;
			}

			if (!Contains(node))
				return;

			RemoveAndSort(node);
		}

		/// <summary>
		/// Remove a collection of <see cref="NodeObject"/>'s if they exist in <see cref="Model.Nodes"/>.
		/// </summary>
		public void Remove(IEnumerable<NodeObject> nodes)
		{
			var dispNds = nodes.Where(n => n.Type is NodeType.Displaced).ToList();

			var toRemove = nodes.Where(n => n.Type != NodeType.Displaced).ToList();

			if (dispNds.Any())
				dispNds.Select(n => n.ObjectId).ToArray().RemoveFromDrawing();

			RemoveAllAndSort(toRemove.Contains);
		}

		/// <summary>
		/// Enumerate all the nodes in the model and return the collection of nodes.
		/// </summary>
		/// <param name="addNodes">Add nodes to stringer start, mid and end points?</param>
		/// <param name="removeNodes">Remove nodes at unnecessary positions?</param>
		public void Update(bool addNodes = true, bool removeNodes = true)
		{
			// Add nodes to all needed positions
			if (addNodes)
				Add();

			// Remove nodes at unnecessary positions
			if (removeNodes)
				Remove();

			// Set the style for all point objects in the drawing
			Model.SetPointSize();
		}

		/// <summary>
		/// Add this collection of <see cref="NodeObject"/>'s to drawing and set their <see cref="ObjectId"/>.
		/// </summary>
		public void AddToDrawing()
		{
			var points = this.Where(n => !(n is null))
				.Select(n => n.CreateDBPoint())
				.ToList();
			
			// Add objects to drawing
			var objIds = points.AddToDrawing().ToArray();

			// Set object ids
			for (int i = 0; i < Count; i++)
				this[i].ObjectId = objIds[i];
		}

		/// <summary>
		/// Get the collection of internal nodes in the drawing.
		/// </summary>
		public static IEnumerable<DBPoint> GetIntNodes() => Layer.IntNode.GetDBObjects()?.ToPoints();

		/// <summary>
		/// Get the collection of external nodes in the drawing.
		/// </summary>
		public static IEnumerable<DBPoint> GetExtNodes() => Layer.ExtNode.GetDBObjects()?.ToPoints();

		/// <summary>
		/// Get the collection of internal and external nodes in the drawing.
		/// </summary>
		public static IEnumerable<DBPoint> GetAllNodes() => GetIntNodes()?.Concat(GetExtNodes());

		/// <summary>
		/// Get the node list from elements in drawing.
		/// </summary>
		public static Nodes ReadFromDrawing()
		{
			var nodes = new Nodes(ReadFromDrawing(GetAllNodes()) ?? new List<NodeObject>());

			// Sort list
			nodes.Sort();

			return nodes;
		}

		/// <summary>
		/// Read <see cref="NodeObject"/>'s from a collection.
		/// </summary>
		/// <param name="nodePoints">The collection containing the nodes of drawing.</param>
		public static IEnumerable<NodeObject> ReadFromDrawing(IEnumerable<DBPoint> nodePoints) => nodePoints?.Select(NodeObject.ReadFromDrawing);

		/// <summary>
		/// Get a node from the list with corresponding <see cref="ObjectId"/>.
		/// </summary>
		public NodeObject GetFromList(ObjectId objectId) => Find(n => n.ObjectId == objectId);

		/// <summary>
		/// Set events to this collection.
		/// </summary>
		private void SetEvents()
		{
			ItemAdded    += On_NodeAdded;
			ItemRemoved  += On_NodeRemoved;
			RangeAdded   += On_NodesAdded;
			RangeRemoved += On_NodesRemoved;
			ListSorted   += On_ListSort;
		}

		/// <summary>
		/// Get <see cref="NodeType"/>.
		/// </summary>
		/// <param name="nodePoint">The <see cref="Entity"/> object.</param>
		public static NodeType GetNodeType(Entity nodePoint) => nodePoint.Layer == $"{Layer.ExtNode}" ? NodeType.External : NodeType.Internal;

		/// <summary>
		/// Get the layer name based on <paramref name="nodeType"/>.
		/// </summary>
		/// <param name="nodeType">The <see cref="NodeType"/> (excluding <see cref="NodeType.All"/>).</param>
		public static Layer GetLayer(NodeType nodeType)
		{
			switch (nodeType)
			{
				case NodeType.Internal:
					return Layer.IntNode;

				case NodeType.Displaced:
					return Layer.Displacements;

				default:
					return Layer.ExtNode;
			}
		}

		/// <summary>
		/// Event to execute when a node is erased.
		/// </summary>
		public static void On_NodeErase(object sender, ObjectErasedEventArgs e)
		{
			if (!Model.Nodes.Any() || !(sender is DBPoint nd))
				return;

			Model.Nodes.RemoveAllAndSortBase(p => p.ObjectId == nd.ObjectId);
		}

		/// <summary>
		/// Event to execute when a node is added.
		/// </summary>
		private static void On_NodeAdded(object sender, ItemEventArgs<NodeObject> e) => e.Item?.AddToDrawing();

		/// <summary>
		/// Event to execute when a range of nodes is added.
		/// </summary>
		private static void On_NodesAdded(object sender, RangeEventArgs<NodeObject> e) => ((Nodes) e.ItemCollection)?.AddToDrawing();

		/// <summary>
		/// Event to execute when a <see cref="NodeObject"/> is removed.
		/// </summary>
		public static void On_NodeRemoved(object sender, ItemEventArgs<NodeObject> e) => Model.RemoveFromDrawing(e.Item);

		/// <summary>
		/// Event to execute when a range of <see cref="NodeObject"/>'s is removed.
		/// </summary>
		public static void On_NodesRemoved(object sender, RangeEventArgs<NodeObject> e) => Model.RemoveFromDrawing(e.ItemCollection);

		/// <summary>
		/// Event to execute when an <see cref="ISPMObject"/> list is sorted.
		/// </summary>
		public static void On_ListSort(object sender, EventArgs e)
		{
			if (!(sender is EList<NodeObject> list))
				return;

			Model.SetNumbers(list);
		}
	}
}