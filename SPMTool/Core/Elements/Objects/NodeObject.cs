﻿using System;
using System.Linq;
using andrefmello91.FEMAnalysis;
using andrefmello91.OnPlaneComponents;
using andrefmello91.SPMElements;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using SPMTool.Enums;
using SPMTool.Extensions;
using UnitsNet;
using UnitsNet.Units;
using static SPMTool.Core.Elements.NodeList;
using static SPMTool.Core.DataBase;

#nullable enable

// ReSharper disable once CheckNamespace
namespace SPMTool.Core.Elements
{
	/// <summary>
	///     Node object class.
	/// </summary>
	public class NodeObject : SPMObject<Point>, IEquatable<NodeObject>
	{
		#region Fields

		private PlaneDisplacement _displacement = PlaneDisplacement.Zero;
		private Node? _node;

		#endregion

		#region Properties

		public override string Name => $"Node {Number}";

		/// <summary>
		///     Get/set the <see cref="andrefmello91.OnPlaneComponents.Constraint" /> in this object.
		/// </summary>
		public Constraint Constraint => Model.Constraints.GetConstraintByPosition(Position);

		/// <summary>
		///     Get the <see cref="PlaneDisplacement" /> of this node object.
		/// </summary>
		public PlaneDisplacement Displacement
		{
			get => _displacement;
			set => SetDisplacement(value);
		}

		public override Layer Layer => GetLayer(Type);

		/// <summary>
		///     Get/set the <see cref="Force" /> in this object.
		/// </summary>
		public PlaneForce Force => Model.Forces.GetForceByPosition(Position);

		/// <summary>
		///     Get the position.
		/// </summary>
		public Point Position
		{
			get => PropertyField;
			set => PropertyField = value;
		}

		/// <summary>
		///     Get the node type.
		/// </summary>
		public NodeType Type { get; }

		#endregion

		#region Constructors

		/// <summary>
		///     Create the node object.
		/// </summary>
		/// <param name="position">The <see cref="Point" /> position.</param>
		/// <param name="type">The <see cref="NodeType" />.</param>
		public NodeObject(Point position, NodeType type)
			: base(position) => Type = type;

		/// <param name="position">The <see cref="Point3d" /> position.</param>
		/// <param name="unit">The <see cref="LengthUnit" /> of <paramref name="position" /> coordinates</param>
		/// <inheritdoc cref="NodeObject(Point, NodeType)" />
		public NodeObject(Point3d position, NodeType type, LengthUnit unit = LengthUnit.Millimeter)
			: this(position.ToPoint(unit), type)
		{
		}

		#endregion

		#region  Methods

		/// <summary>
		///     Read a <see cref="NodeObject" /> from an <see cref="ObjectId" />.
		/// </summary>
		/// <param name="nodeObjectId">The <see cref="ObjectId" /> of the node.</param>
		public static NodeObject? GetFromObjectId(ObjectId nodeObjectId) => nodeObjectId.GetEntity() is DBPoint point 
			? GetFromPoint(point) 
			: null;

		/// <summary>
		///     Read a <see cref="NodeObject" /> from a <see cref="DBPoint" />.
		/// </summary>
		/// <param name="dbPoint">The <see cref="DBPoint" /> object of the node.</param>
		public static NodeObject GetFromPoint(DBPoint dbPoint) => new NodeObject(dbPoint.Position, GetNodeType(dbPoint), Settings.Units.Geometry)
		{
			ObjectId = dbPoint.ObjectId
		};

		public override Entity CreateEntity() => new DBPoint(Position.ToPoint3d())
		{
			Layer = $"{Layer}"
		};

		/// <summary>
		///     Get this object as a <see cref="Node" />.
		/// </summary>
		public override INumberedElement GetElement()
		{
			_node = new Node(Position, Type, Settings.Units.Displacements)
			{
				Number       = Number,
				// Displacement = Displacement,
				Force        = Force,
				Constraint   = Constraint
			};

			return _node;
		}
		
		protected override bool GetProperties()
		{
			var disp = GetDisplacement();

			if (!disp.HasValue)
				return false;

			_displacement = disp.Value;
			return true;

			//_displacement = PlaneDisplacement.Zero;
		}

		protected override void SetProperties() => SetDisplacement(_displacement);

		/// <summary>
		///     Set <see cref="PlaneDisplacement" /> to this object XData.
		/// </summary>
		/// <param name="displacement">The <see cref="PlaneDisplacement" /> to set.</param>
		private void SetDisplacement(PlaneDisplacement displacement)
		{
			_displacement = displacement;
			SetDictionary(Displacement.GetTypedValues(), "Displacements");
		}

		/// <summary>
		///     Get <see cref="PlaneDisplacement" /> saved in XData.
		/// </summary>
		private PlaneDisplacement? GetDisplacement() => GetDictionary("Displacements").GetDisplacement();

		/// <summary>
		///		Set displacement from the associated <see cref="Node"/>.
		/// </summary>
		public void SetDisplacementFromNode()
		{
			if (_node is not null)
				Displacement = _node.Displacement;
		}

		/// <inheritdoc />
		public override string ToString() => _node?.ToString() ?? base.ToString();

		#endregion

		public bool Equals(NodeObject other) => base.Equals(other);

		/// <summary>
		///		Get the <see cref="Node"/> element from a <see cref="NodeObject"/>.
		/// </summary>
		public static explicit operator Node?(NodeObject? nodeObject) => (Node?) nodeObject?.GetElement();

		/// <summary>
		///		Get the <see cref="NodeObject"/> from <see cref="Model.Nodes"/> associated to a <see cref="Node"/>.
		/// </summary>
		/// <remarks>
		///		A <see cref="NodeObject"/> is created if <paramref name="node"/> is not null and is not listed.
		/// </remarks>
		public static explicit operator NodeObject?(Node? node) => node is null 
			? null 
			: Model.Nodes.GetByProperty(node.Position) 
			  ?? new NodeObject(node.Position, node.Type);
	}
}