using System;
using System.Collections.Generic;
using System.Linq;
using andrefmello91.FEMAnalysis;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using andrefmello91.Material.Reinforcement;
using andrefmello91.OnPlaneComponents;
using andrefmello91.SPMElements;
using andrefmello91.SPMElements.PanelProperties;
using SPMTool.Core.Blocks;
using SPMTool.Enums;
using SPMTool.Extensions;
using UnitsNet;
using UnitsNet.Units;
using static SPMTool.Core.DataBase;


#nullable enable

// ReSharper disable once CheckNamespace
namespace SPMTool.Core.Elements
{
	/// <summary>
	///     Panel object class.
	/// </summary>
	public class PanelObject : SPMObject<PanelGeometry>, IEntityCreator<Solid>, IEquatable<PanelObject>
	{
		#region Fields

		private WebReinforcementDirection? _x, _y;
		private Panel? _panel;

		#endregion

		#region Properties

		public override string Name => $"Panel {Number}";

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
		public PanelGeometry Geometry => PropertyField;

		public override Layer Layer => Layer.Panel;

		/// <inheritdoc />
		Solid IEntityCreator<Solid>.CreateEntity() => (Solid) CreateEntity();

		/// <inheritdoc />
		Solid? IEntityCreator<Solid>.GetEntity() => (Solid?) base.GetEntity();

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
		public Vertices Vertices
		{
			get => Geometry.Vertices;
			set => PropertyField = new PanelGeometry(value, Width);
		}

		/// <summary>
		///     Get the geometry.
		/// </summary>
		public Length Width
		{
			get => Geometry.Width;
			set => SetWidth(value);
		}

		#endregion

		#region Constructors

		/// <inheritdoc cref="PanelObject(PanelGeometry)" />
		/// <param name="vertices">The collection of panel's four <see cref="Point3d" /> vertices.</param>
		/// <param name="unit">The <see cref="LengthUnit" /> of <paramref name="vertices" />.</param>
		public PanelObject(IEnumerable<Point3d> vertices, LengthUnit unit = LengthUnit.Millimeter)
			: this (new Vertices(vertices.Select(v => v.ToPoint(unit)).ToArray()))
		{
		}

		/// <inheritdoc cref="PanelObject(PanelGeometry)" />
		/// <param name="vertices">The collection of panel's four <see cref="Point" /> vertices.</param>
		public PanelObject(IEnumerable<Point> vertices)
			: this (new Vertices(vertices))
		{
		}

		/// <inheritdoc cref="PanelObject(PanelGeometry)" />
		/// <param name="vertices">The panel <see cref="Vertices" />.</param>
		public PanelObject(Vertices vertices)
			: this(new PanelGeometry(vertices, 100))
		{
		}
		
		/// <summary>
		///     Create the panel object.
		/// </summary>
		/// <param name="geometry">The panel <see cref="Vertices" />.</param>
		public PanelObject(PanelGeometry geometry)
			: base(geometry)
		{
		}

		#endregion

		#region  Methods

		/// <summary>
		///     Read a <see cref="PanelObject" /> in the drawing.
		/// </summary>
		/// <param name="panelObjectId">The <see cref="ObjectId" /> of the node.</param>
		public static PanelObject? GetFromObjectId(ObjectId panelObjectId) => panelObjectId.GetEntity() is Solid solid
			? GetFromSolid(solid)
			: null;

		/// <summary>
		///     Read a <see cref="PanelObject" /> in the drawing.
		/// </summary>
		/// <param name="solid">The <see cref="Solid" /> object of the stringer.</param>
		public static PanelObject GetFromSolid(Solid solid) => new PanelObject(solid.GetVertices().ToArray(), Settings.Units.Geometry)
		{
			ObjectId = solid.ObjectId
		};

		public override Entity CreateEntity() => new Solid(Vertices.Vertex1.ToPoint3d(), Vertices.Vertex2.ToPoint3d(), Vertices.Vertex4.ToPoint3d(), Vertices.Vertex3.ToPoint3d())
		{
			Layer = $"{Layer}"
		};

		protected override bool GetProperties()
		{
			var w = GetWidth();

			if (w.HasValue)
				PropertyField.Width = w.Value;

			var x = GetReinforcement(Axis.X);

			if (!(x is null))
				_x = x;

			var y = GetReinforcement(Axis.Y);

			if (!(y is null))
				_y = y;

			return
				!w.HasValue && x is null && y is null;
		}

		/// <remarks>
		///		This method a linear object.
		/// </remarks>
		/// <inheritdoc/>
		public override INumberedElement GetElement() => GetElement(Model.Nodes.GetElements().Cast<Node>().ToArray());

		/// <summary>
		///     Divide this <see cref="PanelObject" /> into new ones.
		/// </summary>
		/// <param name="rows">The number of rows.</param>
		/// <param name="columns">The number of columns.</param>
		public IEnumerable<PanelObject> Divide(int rows, int columns)
		{
			if (!Vertices.IsRectangular)
			{
				yield return this;
				yield break;
			}

			var verts = Vertices.Divide(rows, columns).ToArray();

			foreach (var vert in verts)
				yield return new PanelObject(vert)
				{
					Width = Width,
					_x = _x?.Clone(),
					_y = _y?.Clone()
				};
		}

		/// <inheritdoc cref="GetElement()" />
		/// <param name="nodes">The collection of <see cref="Node" />'s in the drawing.</param>
		/// <param name="elementModel">The <see cref="ElementModel" />.</param>
		public Panel GetElement(IEnumerable<Node> nodes, ElementModel elementModel = ElementModel.Elastic)
		{
			_panel = Panel.FromNodes(nodes, Geometry, ConcreteData.Parameters, ConcreteData.ConstitutiveModel, Reinforcement, elementModel);
			
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

			var data = new []
			{
				new TypedValue((int) DxfCode.Real, width.Millimeters)
			};

			SetDictionary(data, "Width");
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
		///		Get the shear <see cref="BlockCreator"/>.
		/// </summary>
		private BlockCreator? ShearBlock()
		{
			return _panel is null || _panel.AverageStresses.IsXYZero
				? null
				: new ShearBlockCreator(Geometry.Vertices.CenterPoint, _panel.AverageStresses.TauXY, 0.8 * BlockScaleFactor());
		}

		/// <summary>
		///		Get the average stress <see cref="BlockCreator"/>.
		/// </summary>
		private BlockCreator? AverageStressBlock()
		{
			return _panel is null || _panel.AveragePrincipalStresses.IsZero
				? null
				: new StressBlockCreator(Geometry.Vertices.CenterPoint, _panel.AveragePrincipalStresses, BlockScaleFactor());
		}

		/// <summary>
		///		Get the concrete stress <see cref="BlockCreator"/>.
		/// </summary>
		private BlockCreator? ConcreteStressBlock()
		{
			return _panel is null || _panel.AveragePrincipalStresses.IsZero
				? null
				: new StressBlockCreator(Geometry.Vertices.CenterPoint, _panel.ConcretePrincipalStresses, BlockScaleFactor(), Layer.ConcreteStress);
		}

		/// <summary>
		///		Get panel block creators.
		/// </summary>
		public IEnumerable<BlockCreator?> GetBlocks() => new[]
		{
			ShearBlock(), AverageStressBlock(), ConcreteStressBlock()
		};
		
		
		/// <summary>
		///		Calculate the scale factor for block insertion.
		/// </summary>
		private double BlockScaleFactor() => Geometry.EdgeLengths.Max().ToUnit(Settings.Units.Geometry).Value * 0.001;
		
		/// <summary>
		///     Get the width of a panel.
		/// </summary>
		private Length? GetWidth()
		{
			var data = GetDictionary("Width");

			return data is null
				? (Length?) null
				: Length.FromMillimeters(data[0].ToDouble()).ToUnit(Settings.Units.Geometry);
		}

		/// <summary>
		///     Get the <see cref="WebReinforcement" /> of a panel.
		/// </summary>
		private WebReinforcementDirection? GetReinforcement(Axis dir) => GetDictionary($"Reinforcement{dir}").GetReinforcementDirection(dir);

		/// <summary>
		///     Set reinforcement to this object.
		/// </summary>
		/// <param name="direction">The <see cref="WebReinforcementDirection" /> for horizontal direction.</param>
		/// <param name="dir">The <see cref="Axis" /> to set (X or Y).</param>
		/// <inheritdoc cref="GetWidth" />
		private void SetReinforcement(WebReinforcementDirection? direction, Axis dir)
		{
			if (dir is Axis.X)
				_x = direction;
			else
				_y = direction;

			SetDictionary(direction.GetTypedValues(), $"Reinforcement{dir}");
		}

		#endregion

		public bool Equals(PanelObject other) => base.Equals(other);
		
		/// <summary>
		///		Get the <see cref="Panel"/> element from a <see cref="PanelObject"/>.
		/// </summary>
		public static explicit operator Panel?(PanelObject? panelObject) => (Panel?) panelObject?.GetElement();

		/// <summary>
		///		Get the <see cref="PanelObject"/> from <see cref="Model.Panels"/> associated to a <see cref="Panel"/>.
		/// </summary>
		/// <remarks>
		///		A <see cref="PanelObject"/> is created if <paramref name="panel"/> is not null and is not listed.
		/// </remarks>
		public static explicit operator PanelObject?(Panel? panel) => panel is null 
			? null 
			: Model.Panels.GetByProperty(panel.Geometry) 
			  ?? new PanelObject(panel.Geometry);

		/// <summary>
		///		Get the <see cref="PanelObject"/> from <see cref="Model.Stringers"/> associated to a <see cref="SPMElement"/>.
		/// </summary>
		/// <remarks>
		///		A <see cref="PanelObject"/> is created if <paramref name="spmElement"/> is not null and is not listed.
		/// </remarks>
		public static explicit operator PanelObject?(SPMElement? spmElement) => spmElement is Panel panel 
			? (PanelObject?) panel 
			: null;

	}
}