﻿using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Extensions;
using OnPlaneComponents;
using SPM.Elements;
using SPM.Elements.StringerProperties;
using SPMTool.Enums;
using SPMTool.Extensions;
using static SPMTool.Database.SettingsData;
using static SPMTool.Database.Model;

#nullable enable

namespace SPMTool.Database.Elements
{
	/// <summary>
	///     Nodes class.
	/// </summary>
	public class Nodes : SPMObjects<NodeObject, Point, Node>
	{
		#region Constructors

		private Nodes()
			: base()
		{
		}

		private Nodes(IEnumerable<NodeObject> nodeObjects)
			: base(nodeObjects)
		{
		}

		#endregion

		#region  Methods

		/// <summary>
		///     Get the collection of <see cref="DBPoint" />'s in the drawing, based in the <see cref="NodeType" />.
		/// </summary>
		/// <remarks>
		///     Leave <paramref name="type" /> null to get all <see cref="NodeType.Internal" /> and
		///     <see cref="NodeType.External" /> nodes.
		/// </remarks>
		/// <param name="type">The <see cref="NodeType" />.</param>
		public static IEnumerable<DBPoint>? GetDBPoints(NodeType? type = null) =>
			type switch
			{
				NodeType.Internal  => Layer.IntNode.GetDBObjects()?.ToPoints(),
				NodeType.External  => Layer.ExtNode.GetDBObjects()?.ToPoints(),
				NodeType.Displaced => Layer.Displacements.GetDBObjects()?.ToPoints(),
				_                  => new[] { Layer.IntNode, Layer.ExtNode }.GetDBObjects()?.ToPoints()
			};

		/// <summary>
		///     Read all <see cref="NodeObject" />'s from drawing.
		/// </summary>
		public static Nodes ReadFromDrawing() => ReadFromPoints(GetDBPoints());

		/// <summary>
		///     Read <see cref="NodeObject" />'s from a collection of <see cref="DBPoint" />'s.
		/// </summary>
		/// <param name="nodePoints">The collection containing the <see cref="DBPoint" />'s of drawing.</param>
		public static Nodes ReadFromPoints(IEnumerable<DBPoint>? nodePoints) =>
			nodePoints.IsNullOrEmpty()
				? new Nodes()
				: new Nodes(nodePoints.Select(NodeObject.ReadFromPoint));

		/// <summary>
		///     Get <see cref="NodeType" />.
		/// </summary>
		/// <param name="nodePoint">The <see cref="DBPoint" /> object.</param>
		public static NodeType GetNodeType(DBPoint nodePoint) =>
			nodePoint.Layer == $"{Layer.ExtNode}"
				? NodeType.External
				: nodePoint.Layer == $"{Layer.IntNode}"
					? NodeType.Internal
					: NodeType.Displaced;

		/// <summary>
		///     Get the layer name based on <paramref name="nodeType" />.
		/// </summary>
		/// <param name="nodeType">The <see cref="NodeType" /> (excluding <see cref="NodeType.All" />).</param>
		public static Layer GetLayer(NodeType nodeType) =>
			nodeType switch
			{
				NodeType.Internal  => Layer.IntNode,
				NodeType.External  => Layer.ExtNode,
				_                  => Layer.Displacements
			};

		/// <summary>
		///     Get a list of nodes' <see cref="Point" /> positions.
		/// </summary>
		public List<Point> GetPositions() => GetProperties();

		/// <summary>
		///     Add nodes in all necessary positions, based on a collection of <seealso cref="StringerGeometry" />'s.
		/// </summary>
		/// <remarks>
		///     Nodes are added at each <see cref="StringerGeometry.InitialPoint" /> , <see cref="StringerGeometry.CenterPoint" />
		///     and <see cref="StringerGeometry.EndPoint" />.
		/// </remarks>
		/// <param name="geometries">The collection of <see cref="StringerGeometry" />'s for adding nodes.</param>
		/// <inheritdoc cref="AddRange(IEnumerable{NodeObject}, bool, bool)" />
		public int AddNecessary(IEnumerable<StringerGeometry>? geometries, bool raiseEvents = true, bool sort = true)
		{
			if (geometries.IsNullOrEmpty())
				return 0;

			var geoList = geometries.ToList();

			// Create a list
			var extNds = new EList<Point>();

			// Add external nodes
			extNds.AddRange(geoList.Select(str => str.InitialPoint));
			extNds.AddRange(geoList.Select(str => str.EndPoint));
			var c = AddRange(extNds, NodeType.External, raiseEvents, sort);

			// Add internal nodes
			return
				c + AddRange(geoList.Select(str => str.CenterPoint).ToArray(), NodeType.Internal, raiseEvents, sort);
		}

		/// <inheritdoc cref="Add(NodeObject, bool, bool)" />
		/// <param name="position">The <see cref="Point" /> position.</param>
		/// <param name="nodeType">The <see cref="NodeType" />.</param>
		public bool Add(Point position, NodeType nodeType, bool raiseEvents = true, bool sort = true) => Add(new NodeObject(position, nodeType), raiseEvents, sort);

		/// <inheritdoc cref="EList{T}.Add(T, bool, bool)" />
		/// <remarks>
		///     If node type is <see cref="NodeType.Displaced" />, the node is only added to drawing and false is returned.
		/// </remarks>
		public new bool Add(NodeObject item, bool raiseEvents = true, bool sort = true)
		{
			if (item.Type != NodeType.Displaced)
				return
					base.Add(item, raiseEvents, sort);

			// Just add to drawing
			item.AddToDrawing();
			return false;
		}

		/// <inheritdoc cref="AddRange(IEnumerable{NodeObject}, bool, bool)" />
		/// <param name="positions">The collection of <see cref="Point" /> positions.</param>
		/// <param name="nodeType">The <see cref="NodeType" />.</param>
		public int AddRange(IEnumerable<Point>? positions, NodeType nodeType, bool raiseEvents = true, bool sort = true) =>
			AddRange(positions?.Distinct()?.Select(p => new NodeObject(p, nodeType))?.ToList(), raiseEvents, sort);

		/// <inheritdoc cref="EList{T}.AddRange(IEnumerable{T}, bool, bool)" />
		/// <inheritdoc cref="Add(NodeObject, bool, bool)" />
		public new int AddRange(IEnumerable<NodeObject>? collection, bool raiseEvents = true, bool sort = true)
		{
			if (collection.IsNullOrEmpty())
				return 0;

			// Get displaced nodes
			var dispNodes = collection.Where(n => n.Type is NodeType.Displaced).ToList();

			if (dispNodes.Any())
				AddToDrawing(dispNodes);

			return
				base.AddRange(collection.Where(n => n.Type != NodeType.Displaced), raiseEvents, sort);
		}

		public int RemoveRange(IEnumerable<Point>? positions, bool raiseEvents = true, bool sort = true) =>
			positions.IsNullOrEmpty()
				? 0
				: RemoveAll(n => positions.Contains(n.Position), raiseEvents, sort);

		/// <summary>
		///     Remove unnecessary nodes from this collection based on a collection of <see cref="StringerGeometry" />'s.
		/// </summary>
		/// <remarks>
		///     Nodes are removed at positions that doesn't match any <see cref="StringerGeometry.InitialPoint" />,
		///     <see cref="StringerGeometry.CenterPoint" /> and <see cref="StringerGeometry.EndPoint" /> in
		///     <paramref name="geometries" />.
		/// </remarks>
		/// <param name="geometries">The collection of <see cref="StringerGeometry" />'s for removing nodes.</param>
		/// <inheritdoc cref="EList{T}.RemoveRange(IEnumerable{T}, bool, bool)" />
		public int RemoveUnnecessary(IEnumerable<StringerGeometry>? geometries, bool raiseEvents = true, bool sort = true)
		{
			if (geometries.IsNullOrEmpty())
			{
				// There is no stringers, remove all nodes.
				var c = Count;

				Clear(raiseEvents);

				return c;
			}

			// Get stringers
			var geoList = geometries.ToList();

			// Get the stringer points
			var toRemove = geoList.Select(str => str.CenterPoint).ToEList();
			toRemove!.AddRange(geoList.Select(str => str.InitialPoint));
			toRemove!.AddRange(geoList.Select(str => str.EndPoint));

			return
				RemoveAll(n => toRemove.Contains(n.Position), raiseEvents, sort);
		}

		/// <summary>
		///     Update all the nodes in this collection.
		/// </summary>
		/// <param name="addNodes">Add nodes to stringer start, mid and end points?</param>
		/// <param name="removeNodes">Remove nodes at unnecessary positions?</param>
		public void Update(bool addNodes = true, bool removeNodes = true)
		{
			var geometries = Model.Stringers.GetGeometries();

			// Add nodes to all needed positions
			if (addNodes)
				AddNecessary(geometries, true, false);

			// Remove nodes at unnecessary positions
			if (removeNodes)
				RemoveUnnecessary(geometries);

			// Set the style for all point objects in the drawing
			SetPointSize();
		}

		/// <summary>
		///     Get a node from the list with corresponding <see cref="ObjectId" />.
		/// </summary>
		public NodeObject? GetByObjectId(ObjectId objectId) => Find(n => n.ObjectId == objectId);

		#endregion
	}
}