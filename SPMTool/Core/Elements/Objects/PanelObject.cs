﻿#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using andrefmello91.FEMAnalysis;
using andrefmello91.Material.Reinforcement;
using andrefmello91.OnPlaneComponents;
using andrefmello91.SPMElements;
using andrefmello91.SPMElements.PanelProperties;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using SPMTool.Core.Blocks;
using SPMTool.Enums;
using UnitsNet;
using UnitsNet.Units;
using static SPMTool.Core.SPMModel;

// ReSharper disable once CheckNamespace
namespace SPMTool.Core.Elements
{
	/// <summary>
	///     Panel object class.
	/// </summary>
	public class PanelObject : SPMObject<PanelGeometry>, IDBObjectCreator<Solid>, IEquatable<PanelObject>
	{

		#region Fields

		private Panel? _panel;

		private WebReinforcementDirection? _x, _y;

		#endregion

		#region Properties

		/// <summary>
		///     Get/set the horizontal <see cref="WebReinforcementDirection" />.
		/// </summary>
		public WebReinforcementDirection? DirectionX
		{
			get => _x;
			set => SetReinforcement(value, Axis.X);
		}

		/// <summary>
		///     Get/set the vertical <see cref="WebReinforcementDirection" />.
		/// </summary>
		public WebReinforcementDirection? DirectionY
		{
			get => _y;
			set => SetReinforcement(value, Axis.Y);
		}

		/// <summary>
		///     Get the geometry of this object.
		/// </summary>
		public PanelGeometry Geometry => Property;

		/// <summary>
		///     Get the <see cref="WebReinforcement" />.
		/// </summary>
		public WebReinforcement? Reinforcement
		{
			get =>
				DirectionX is null && DirectionY is null
					? null
					: new WebReinforcement(DirectionX, DirectionY, Width);
			set
			{
				DirectionX = value?.DirectionX;
				DirectionY = value?.DirectionY;
			}
		}

		/// <summary>
		///     Get panel's <see cref="andrefmello91.SPMElements.PanelProperties.Vertices" />
		/// </summary>
		public Vertices Vertices => Geometry.Vertices;

		/// <summary>
		///     Get the geometry.
		/// </summary>
		public Length Width
		{
			get => PropertyField.Width;
			set => SetWidth(value);
		}

		public override Layer Layer => Layer.Panel;

		public override string Name => $"Panel {Number}";

		#endregion

		#region Constructors

		/// <inheritdoc cref="PanelObject(PanelGeometry, ObjectId)" />
		/// <param name="vertices">The collection of panel's four <see cref="Point3d" /> vertices.</param>
		/// <param name="unit">The <see cref="LengthUnit" /> of <paramref name="vertices" />.</param>
		public PanelObject(IEnumerable<Point3d> vertices, ObjectId blockTableId, LengthUnit unit = LengthUnit.Millimeter)
			: this(Vertices.From(vertices.ToPoints(unit)), blockTableId)
		{
		}

		/// <inheritdoc cref="PanelObject(PanelGeometry, ObjectId)" />
		/// <param name="vertices">The collection of panel's four <see cref="Point" /> vertices.</param>
		public PanelObject(IEnumerable<Point> vertices, ObjectId blockTableId)
			: this(Vertices.From(vertices), blockTableId)
		{
		}

		/// <inheritdoc cref="PanelObject(PanelGeometry, ObjectId)" />
		/// <param name="vertices">The panel <see cref="Vertices" />.</param>
		public PanelObject(Vertices vertices, ObjectId blockTableId)
			: this(new PanelGeometry(vertices, 100), blockTableId)
		{
		}

		/// <summary>
		///     Create a panel object.
		/// </summary>
		/// <param name="geometry">The panel <see cref="Vertices" />.</param>
		/// <inheritdoc />
		public PanelObject(PanelGeometry geometry, ObjectId blockTableId)
			: base(geometry, blockTableId)
		{
		}

		#endregion

		#region Methods

		/// <summary>
		///     Read a <see cref="PanelObject" /> from an existing solid in the drawing.
		/// </summary>
		/// <param name="solid">The <see cref="Solid" /> object of the stringer.</param>
		/// <param name="unit">The unit for geometry.</param>
		public static PanelObject From(Solid solid, LengthUnit unit)
		{
			var pnl = new PanelObject(solid.GetVertices().ToArray(), solid.Database.BlockTableId, unit);
			pnl.AttachObject(solid.ObjectId, solid.ExtensionDictionary);
			return pnl;
		}

		/// <summary>
		///     Calculate the scale factor for block insertion.
		/// </summary>
		public double BlockScaleFactor() =>
			UnitMath.Min(Geometry.Dimensions.a, Geometry.Dimensions.b).As(GetOpenedModel(BlockTableId)?.Settings.Units.Geometry ?? LengthUnit.Millimeter) * 0.001;

		/// <summary>
		///     Divide this <see cref="PanelObject" /> into new ones.
		/// </summary>
		/// <remarks>
		///     This must be rectangular.
		/// </remarks>
		/// <param name="rows">The number of rows.</param>
		/// <param name="columns">The number of columns.</param>
		/// <returns>
		///     An empty collection if this object is not rectangular.
		/// </returns>
		public IEnumerable<PanelObject> Divide(int rows, int columns)
		{
			if (!Vertices.IsRectangular)
				yield break;

			var verts = Vertices.Divide(rows, columns).ToArray();

			foreach (var vert in verts)
				yield return new PanelObject(vert, BlockTableId)
				{
					Width = Width,
					_x    = _x?.Clone(),
					_y    = _y?.Clone()
				};
		}

		/// <summary>
		///     Get panel block creators.
		/// </summary>
		/// <param name="scaleFactor">The scale factor.</param>
		/// <param name="textHeight">The text height for attributes.</param>
		/// <param name="stressUnit">The unit of panel stresses.</param>
		/// <param name="crackUnit">The unit of crack openings.</param>
		public IEnumerable<BlockCreator?> GetBlocks(double scaleFactor, double textHeight, PressureUnit stressUnit, LengthUnit crackUnit)
		{
			yield return
				ShearBlockCreator.From(_panel!.Geometry.Vertices.CenterPoint, _panel.AverageStresses.TauXY.ToUnit(stressUnit), scaleFactor, textHeight, BlockTableId);

			yield return
				StressBlockCreator.From(_panel!.Geometry.Vertices.CenterPoint, _panel.AveragePrincipalStresses.Convert(stressUnit), scaleFactor, textHeight, BlockTableId);

			yield return
				StressBlockCreator.From(_panel!.Geometry.Vertices.CenterPoint, _panel.ConcretePrincipalStresses.Convert(stressUnit), scaleFactor, textHeight, BlockTableId, Layer.ConcreteStress);

			if (_panel.Model is ElementModel.Elastic)
				yield break;

			yield return
				PanelCrackBlockCreator.From(_panel!.Geometry.Vertices.CenterPoint, _panel.CrackOpening.ToUnit(crackUnit), _panel.AveragePrincipalStresses.Theta2, scaleFactor, textHeight, BlockTableId);
		}

		/// <remarks>
		///     This method a linear object.
		/// </remarks>
		/// <inheritdoc />
		public override INumberedElement GetElement() => GetElement(GetOpenedModel(BlockTableId)!.Nodes.GetElements().Cast<Node>().ToArray()!);

		/// <inheritdoc cref="GetElement()" />
		/// <param name="nodes">The collection of <see cref="Node" />'s in the drawing.</param>
		/// <param name="elementModel">The <see cref="ElementModel" />.</param>
		public Panel GetElement(IEnumerable<Node> nodes, ElementModel elementModel = ElementModel.Elastic)
		{
			var model = GetOpenedModel(BlockTableId)!;

			_panel = Panel.FromNodes(nodes, Geometry, model.ConcreteData.Parameters, model.ConcreteData.ConstitutiveModel, Reinforcement, elementModel);

			_panel.Number = Number;

			return _panel;
		}

		/// <summary>
		///     Set <paramref name="width" /> to this object.
		/// </summary>
		/// <param name="width">The width.</param>
		public void SetWidth(Length width)
		{
			PropertyField.Width = width;

			var data = new[]
			{
				new TypedValue((int) DxfCode.Real, width.Millimeters)
			};

			SetDictionary(data, "Width");
		}

		protected override void GetProperties()
		{
			if (GetWidth() is { } width)
				PropertyField.Width = width;

			if (GetReinforcement(Axis.X) is { } xReinforcement)
				_x = xReinforcement;

			if (GetReinforcement(Axis.Y) is { } yReinforcement)
				_y = yReinforcement;
		}

		/// <inheritdoc />
		protected override bool PropertyChanged(out PanelGeometry? newProperty)
		{
			switch (ObjectId.Database.GetObject(ObjectId))
			{
				case Solid solid when Vertices.From(solid.GetVertices().ToPoints(PropertyField.Unit)) is var vertices && vertices != PropertyField.Vertices:
					newProperty = new PanelGeometry(vertices, PropertyField.Width);
					return true;

				default:
					newProperty = null;
					return false;
			}
		}

		protected override void SetProperties()
		{
			var wData = new[]
			{
				new TypedValue((int) DxfCode.Real, PropertyField.Width.Millimeters)
			};

			SetDictionary(wData, "Width");

			SetDictionary(_x.GetTypedValues(), $"Reinforcement{Axis.X}");

			SetDictionary(_y.GetTypedValues(), $"Reinforcement{Axis.Y}");
		}

		/// <summary>
		///     Get the <see cref="WebReinforcement" /> of a panel.
		/// </summary>
		private WebReinforcementDirection? GetReinforcement(Axis dir) => GetDictionary($"Reinforcement{dir}").GetReinforcementDirection(dir);

		/// <summary>
		///     Get the width of a panel.
		/// </summary>
		private Length? GetWidth()
		{
			var data = GetDictionary("Width");

			return data is not null
				? Length.FromMillimeters(data[0].ToDouble())
				: null;
		}

		/// <summary>
		///     Set reinforcement to this object.
		/// </summary>
		/// <param name="direction">The <see cref="WebReinforcementDirection" /> for horizontal direction.</param>
		/// <param name="dir">The <see cref="Axis" /> to set (X or Y).</param>
		/// <inheritdoc cref="GetWidth" />
		private void SetReinforcement(WebReinforcementDirection? direction, Axis dir)
		{
			switch (dir)
			{
				case Axis.Y:
					_y = direction;
					break;

				case Axis.X:
					_x = direction;
					break;

				default:
					return;
			}

			SetDictionary(direction.GetTypedValues(), $"Reinforcement{dir}");
		}

		public override DBObject CreateObject()
		{
			var unit = GetOpenedModel(BlockTableId)!.Settings.Units.Geometry;

			return
				new Solid(Vertices.Vertex1.ToPoint3d(unit), Vertices.Vertex2.ToPoint3d(unit), Vertices.Vertex4.ToPoint3d(unit), Vertices.Vertex3.ToPoint3d(unit))
				{
					Layer = $"{Layer}"
				};
		}

		/// <inheritdoc />
		Solid IDBObjectCreator<Solid>.CreateObject() => (Solid) CreateObject();

		/// <inheritdoc />
		Solid? IDBObjectCreator<Solid>.GetObject() => (Solid?) base.GetObject();

		public bool Equals(PanelObject other) => base.Equals(other);

		#endregion

		#region Operators

		/// <summary>
		///     Get the <see cref="Panel" /> element from a <see cref="PanelObject" />.
		/// </summary>
		public static explicit operator Panel?(PanelObject? panelObject) => (Panel?) panelObject?.GetElement();

		/// <summary>
		///     Get the <see cref="PanelObject" /> from the active model associated to a <see cref="Panel" />.
		/// </summary>
		public static explicit operator PanelObject?(Panel? panel) => panel is not null
			? ActiveModel.Panels[panel.Geometry]
			: null;

		/// <summary>
		///     Get the <see cref="PanelObject" /> from <see cref="SPMModel.Stringers" /> associated to a
		///     <see cref="SPMElement{T}" />
		///     .
		/// </summary>
		/// <remarks>
		///     A <see cref="PanelObject" /> is created if <paramref name="spmElement" /> is not null and is not listed.
		/// </remarks>
		public static explicit operator PanelObject?(SPMElement<PanelGeometry>? spmElement) => spmElement is Panel panel
			? (PanelObject?) panel
			: null;

		/// <summary>
		///     Get the <see cref="PanelObject" /> from <see cref="SPMModel.Panels" /> associated to a <see cref="Solid" />.
		/// </summary>
		/// <remarks>
		///     Can be null if <paramref name="solid" /> is null or doesn't correspond to a <see cref="PanelObject" />
		/// </remarks>
		public static explicit operator PanelObject?(Solid? solid) => (PanelObject?) solid.GetSPMObject();

		/// <summary>
		///     Get the <see cref="Solid" /> associated to a <see cref="PanelObject" />.
		/// </summary>
		/// <remarks>
		///     Can be null if <paramref name="panelObject" /> is null or doesn't exist in drawing.
		/// </remarks>
		public static explicit operator Solid?(PanelObject? panelObject) => (Solid?) panelObject?.GetObject();

		#endregion

	}
}