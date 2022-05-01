#nullable enable

using System;
using andrefmello91.FEMAnalysis;
using andrefmello91.OnPlaneComponents;
using andrefmello91.SPMElements;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using SPMTool.Enums;
using UnitsNet.Units;
using static SPMTool.Core.Elements.NodeList;
using static SPMTool.Core.SPMModel;

// ReSharper disable once CheckNamespace
namespace SPMTool.Core.Elements
{
	/// <summary>
	///     Node object class.
	/// </summary>
	public class NodeObject : SPMObject<Point>, IDBObjectCreator<DBPoint>, IEquatable<NodeObject>
	{

		#region Fields

		private PlaneDisplacement _displacement = PlaneDisplacement.Zero;
		private Node? _node;

		#endregion

		#region Properties

		/// <summary>
		///     Get/set the <see cref="andrefmello91.OnPlaneComponents.Constraint" /> in this object.
		/// </summary>
		public Constraint Constraint => GetOpenedModel(BlockTableId)?.Constraints[Position]?.Value ?? Constraint.Free;

		/// <summary>
		///     Get the <see cref="PlaneDisplacement" /> of this node object.
		/// </summary>
		public PlaneDisplacement Displacement
		{
			get => _displacement;
			set => SetDisplacement(value);
		}

		/// <summary>
		///     Get/set the <see cref="Force" /> in this object.
		/// </summary>
		public PlaneForce Force
		{
			get
			{
				var model = GetOpenedModel(BlockTableId)!;
				var unit  = model.Settings.Units.AppliedForces;
				var force = model.Forces[Position]?.Value ?? PlaneForce.Zero;

				return
					force.Convert(unit);
			}
		}

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

		public override Layer Layer => GetLayer(Type);

		public override string Name => $"Node {Number}";

		#endregion

		#region Constructors

		/// <summary>
		///     Create a node object.
		/// </summary>
		/// <param name="position">The <see cref="Point" /> position.</param>
		/// <param name="type">The <see cref="NodeType" />.</param>
		/// <inheritdoc />
		public NodeObject(Point position, NodeType type, ObjectId blockTableId)
			: base(position, blockTableId) => Type = type;

		/// <param name="position">The <see cref="Point3d" /> position.</param>
		/// <param name="unit">The <see cref="LengthUnit" /> of <paramref name="position" /> coordinates</param>
		/// <inheritdoc cref="NodeObject(Point, NodeType, ObjectId)" />
		public NodeObject(Point3d position, NodeType type, ObjectId blockTableId, LengthUnit unit = LengthUnit.Millimeter)
			: this(position.ToPoint(unit), type, blockTableId)
		{
		}

		#endregion

		#region Methods

		/// <summary>
		///     Read a <see cref="NodeObject" /> from an existing <see cref="DBPoint" /> in the drawing.
		/// </summary>
		/// <param name="dbPoint">The <see cref="DBPoint" /> object of the node.</param>
		/// <param name="unit">The unit for geometry.</param>
		public static NodeObject From(DBPoint dbPoint, LengthUnit unit) =>
			new(dbPoint.Position, dbPoint.GetNodeType(), dbPoint.Database.BlockTableId, unit)
			{
				ObjectId = dbPoint.ObjectId
			};

		/// <summary>
		///     Get this object as a <see cref="Node" />.
		/// </summary>
		public override INumberedElement GetElement() =>
			_node = new Node(Position, Type, GetOpenedModel(BlockTableId)?.Settings.Units.Displacements ?? LengthUnit.Millimeter)
			{
				Number = Number,

				// Displacement = Displacement,
				Force      = Force,
				Constraint = Constraint
			};

		/// <summary>
		///     Set displacement from the associated <see cref="Node" />.
		/// </summary>
		public void SetDisplacementFromNode()
		{
			if (_node is not null)
				Displacement = _node.Displacement;
		}

		/// <inheritdoc />
		public override string ToString() => _node?.ToString() ?? base.ToString();

		protected override void GetProperties() => _displacement = GetDisplacement() ?? PlaneDisplacement.Zero;

		protected override void SetProperties() => SetDisplacement(_displacement);

		/// <summary>
		///     Get <see cref="PlaneDisplacement" /> saved in XData.
		/// </summary>
		private PlaneDisplacement? GetDisplacement() => GetDictionary("Displacements").GetDisplacement();

		/// <summary>
		///     Set <see cref="PlaneDisplacement" /> to this object XData.
		/// </summary>
		/// <param name="displacement">The <see cref="PlaneDisplacement" /> to set.</param>
		private void SetDisplacement(PlaneDisplacement displacement)
		{
			_displacement = displacement;
			SetDictionary(displacement.GetTypedValues(), "Displacements");
		}

		public override DBObject CreateObject() =>
			new DBPoint(Position.ToPoint3d(GetOpenedModel(BlockTableId)!.Settings.Units.Geometry))
			{
				Layer = $"{Layer}"
			};

		/// <inheritdoc />
		DBPoint IDBObjectCreator<DBPoint>.CreateObject() => (DBPoint) CreateObject();

		/// <inheritdoc />
		DBPoint? IDBObjectCreator<DBPoint>.GetObject() => (DBPoint?) base.GetObject();

		public bool Equals(NodeObject other) => base.Equals(other);

		#endregion

		#region Operators

		/// <summary>
		///     Get the <see cref="Node" /> element from a <see cref="NodeObject" />.
		/// </summary>
		public static explicit operator Node?(NodeObject? nodeObject) => (Node?) nodeObject?.GetElement();

		/// <summary>
		///     Get the <see cref="NodeObject" /> from active model associated to a <see cref="Node" />.
		/// </summary>
		public static explicit operator NodeObject?(Node? node) => node is not null
			? ActiveModel.Nodes[node.Position]
			: null;

		/// <summary>
		///     Get the <see cref="NodeObject" /> from the active model associated to a <see cref="DBPoint" />.
		/// </summary>
		/// <remarks>
		///     Can be null if <paramref name="dbPoint" /> is null or doesn't correspond to a <see cref="NodeObject" />
		/// </remarks>
		public static explicit operator NodeObject?(DBPoint? dbPoint) => (NodeObject?) dbPoint.GetSPMObject();

		/// <summary>
		///     Get the <see cref="DBPoint" /> associated to a <see cref="NodeObject" />.
		/// </summary>
		/// <remarks>
		///     Can be null if <paramref name="nodeObject" /> is null or doesn't exist in drawing.
		/// </remarks>
		public static explicit operator DBPoint?(NodeObject? nodeObject) => (DBPoint?) nodeObject?.GetObject();

		#endregion

	}
}