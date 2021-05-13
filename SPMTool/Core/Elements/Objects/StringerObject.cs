using System;
using System.Collections.Generic;
using System.Linq;
using andrefmello91.FEMAnalysis;
using andrefmello91.Material.Reinforcement;
using andrefmello91.OnPlaneComponents;
using andrefmello91.SPMElements;
using andrefmello91.SPMElements.StringerProperties;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using SPMTool.Core.Blocks;
using SPMTool.Enums;

using UnitsNet;
using UnitsNet.Units;
using static SPMTool.Core.DataBase;

#nullable enable

// ReSharper disable once CheckNamespace
namespace SPMTool.Core.Elements
{
	/// <summary>
	///     Stringer object class.
	/// </summary>
	public class StringerObject : SPMObject<StringerGeometry>, IDBObjectCreator<Line>, IEquatable<StringerObject>
	{

		#region Fields

		private UniaxialReinforcement? _reinforcement;
		private Stringer? _stringer;

		#endregion

		#region Properties

		/// <summary>
		///     Get/set the height of <see cref="Geometry" />.
		/// </summary>
		public CrossSection CrossSection
		{
			get => Geometry.CrossSection;
			set => SetCrossSection(value);
		}

		/// <summary>
		///     Get the geometry of this object.
		/// </summary>
		public StringerGeometry Geometry
		{
			get => PropertyField;
			set => PropertyField = new StringerGeometry(value.InitialPoint, value.EndPoint, CrossSection);
		}

		/// <summary>
		///     Get the absolute maximum force at the associated <see cref="Stringer" />.
		/// </summary>
		public Force MaxForce => _stringer?.MaxForce ?? Force.Zero;

		/// <summary>
		///     Get the <see cref="UniaxialReinforcement" /> of this stringer.
		/// </summary>
		public UniaxialReinforcement? Reinforcement
		{
			get => _reinforcement;
			set => SetReinforcement(value);
		}

		#region Interface Implementations

		public override Layer Layer => Layer.Stringer;

		public override string Name => $"Stringer {Number}";

		#endregion

		#endregion

		#region Constructors

		/// <inheritdoc cref="StringerObject(StringerGeometry)" />
		/// <param name="initialPoint">The initial <see cref="Point3d" />.</param>
		/// <param name="endPoint">The end <see cref="Point3d" />.</param>
		/// <param name="unit">The <see cref="LengthUnit" /> of points' coordinates.</param>
		public StringerObject(Point3d initialPoint, Point3d endPoint, LengthUnit unit = LengthUnit.Millimeter)
			: this(initialPoint.ToPoint(unit), endPoint.ToPoint(unit))
		{
		}

		/// <inheritdoc cref="StringerObject(StringerGeometry)" />
		/// <param name="initialPoint">The initial <see cref="Point" />.</param>
		/// <param name="endPoint">The end <see cref="Point" />.</param>
		public StringerObject(Point initialPoint, Point endPoint)
			: this(new StringerGeometry(initialPoint, endPoint, 100, 100))
		{
		}

		/// <summary>
		///     Create the stringer object.
		/// </summary>
		/// <param name="geometry">The <see cref="StringerGeometry" />.</param>
		public StringerObject(StringerGeometry geometry)
			: base(geometry)
		{
		}

		#endregion

		#region Methods

		/// <summary>
		///     Read a <see cref="StringerObject" /> in the drawing.
		/// </summary>
		/// <param name="line">The <see cref="Line" /> object of the stringer.</param>
		public static StringerObject ReadFromLine(Line line)
		{
			var pts = new List<Point>
			{
				line.StartPoint.ToPoint(),
				line.EndPoint.ToPoint()
			};

			// Sort list
			pts.Sort();

			return
				new StringerObject(pts[0], pts[1])
				{
					ObjectId = line.ObjectId
				};
		}

		/// <summary>
		///     Read a <see cref="StringerObject" /> in the drawing.
		/// </summary>
		/// <param name="stringerObjectId">The <see cref="ObjectId" /> of the stringer.</param>
		public static StringerObject? ReadFromObjectId(ObjectId stringerObjectId) =>
			stringerObjectId.GetEntity() is Line line
				? ReadFromLine(line)
				: null;

		/// <summary>
		///     Create crack blocks.
		/// </summary>
		public IEnumerable<StringerCrackBlockCreator?> CreateCrackBlocks() => StringerCrackBlockCreator.CreateBlocks(_stringer);

		/// <summary>
		///     Create the stringer diagram. Can be null if the stringer is unloaded.
		/// </summary>
		public StringerForceCreator? CreateDiagram() => StringerForceCreator.Create(_stringer);

		/// <summary>
		///     Divide this <see cref="StringerObject" /> in a <paramref name="number" /> of new ones.
		/// </summary>
		/// <param name="number">The number of new <see cref="StringerObject" />'s.</param>
		public IEnumerable<StringerObject> Divide(int number)
		{
			var geometries = Geometry.Divide(number).ToArray();

			foreach (var geometry in geometries)
				yield return new StringerObject(geometry)
				{
					_reinforcement = _reinforcement
				};
		}

		/// <summary>
		///     Get the displaced <see cref="Line" />.
		/// </summary>
		/// <param name="displacementMagnifier">A magnifier factor to multiply displacements.</param>
		public Line GetDisplaced(double displacementMagnifier)
		{
			// Get displaced points
			Point
				start = Geometry.InitialPoint + (_stringer?.Grip1.Displacement ?? PlaneDisplacement.Zero) * displacementMagnifier,
				end   = Geometry.EndPoint + (_stringer?.Grip3.Displacement ?? PlaneDisplacement.Zero) * displacementMagnifier;

			return new Line(start.ToPoint3d(), end.ToPoint3d())
			{
				Layer = $"{Layer.Displacements}"
			};
		}

		/// <remarks>
		///     This method returns a linear object.
		/// </remarks>
		/// <inheritdoc />
		public override INumberedElement GetElement() => GetElement(Model.Nodes.GetElements().Cast<Node>().ToArray());

		/// <inheritdoc cref="SPMObject{T}.GetElement()" />
		/// <param name="nodes">The collection of <see cref="Node" />'s in the drawing.</param>
		/// <param name="elementModel">The <see cref="ElementModel" />.</param>
		public Stringer GetElement(IEnumerable<Node> nodes, ElementModel elementModel = ElementModel.Elastic)
		{
			_stringer        = Stringer.FromNodes(nodes, Geometry.InitialPoint, Geometry.EndPoint, Geometry.CrossSection, ConcreteData.Parameters, ConcreteData.ConstitutiveModel, Reinforcement?.Clone(), elementModel);
			_stringer.Number = Number;
			return _stringer;
		}

		protected override void GetProperties()
		{
			var cs = GetCrossSection();

			if (cs.HasValue)
				PropertyField.CrossSection = cs.Value;

			var rf = GetReinforcement();

			if (rf is not null)
				_reinforcement = GetReinforcement();
		}

		protected override void SetProperties()
		{
			SetDictionary(_reinforcement.GetTypedValues(), "Reinforcement");
			SetDictionary(PropertyField.CrossSection.GetTypedValues(), "CrossSection");
		}

		/// <summary>
		///     Get the <see cref="CrossSection" /> from XData.
		/// </summary>
		private CrossSection? GetCrossSection() => GetDictionary("CrossSection").GetCrossSection();

		/// <summary>
		///     Get this stringer <see cref="UniaxialReinforcement" />.
		/// </summary>
		private UniaxialReinforcement? GetReinforcement() => GetDictionary("Reinforcement").GetReinforcement();

		/// <summary>
		///     Set the <seealso cref="CrossSection" /> to <see cref="Geometry" /> and XData.
		/// </summary>
		/// <param name="crossSection">The <seealso cref="CrossSection" /> to set. Leave null to leave unchanged.</param>
		private void SetCrossSection(CrossSection crossSection)
		{
			PropertyField.CrossSection = crossSection;

			SetDictionary(crossSection.GetTypedValues(), "CrossSection");
		}

		/// <summary>
		///     Set <paramref name="reinforcement" /> to XData.
		/// </summary>
		/// <param name="reinforcement">The <see cref="UniaxialReinforcement" /> to set.</param>
		private void SetReinforcement(UniaxialReinforcement? reinforcement)
		{
			_reinforcement = reinforcement;

			SetDictionary(reinforcement?.GetTypedValues(), "Reinforcement");
		}

		#region Interface Implementations

		public override DBObject CreateObject() =>
			new Line(Geometry.InitialPoint.ToPoint3d(), Geometry.EndPoint.ToPoint3d())
			{
				Layer = $"{Layer}"
			};

		/// <inheritdoc />
		Line IDBObjectCreator<Line>.CreateObject() => (Line) CreateObject();

		public bool Equals(StringerObject other) => base.Equals(other);

		/// <inheritdoc />
		Line? IDBObjectCreator<Line>.GetObject() => (Line?) base.GetObject();

		#endregion

		#endregion

		#region Operators

		/// <summary>
		///     Get the <see cref="Stringer" /> element from a <see cref="StringerObject" />.
		/// </summary>
		public static explicit operator Stringer?(StringerObject? stringerObject) => (Stringer?) stringerObject?.GetElement();

		/// <summary>
		///     Get the <see cref="StringerObject" /> from <see cref="Model.Stringers" /> associated to a <see cref="Stringer" />.
		/// </summary>
		/// <remarks>
		///     A <see cref="StringerObject" /> is created if <paramref name="stringer" /> is not null and is not listed.
		/// </remarks>
		public static explicit operator StringerObject?(Stringer? stringer) => stringer is null
			? null
			: Model.Stringers.GetByProperty(stringer.Geometry)
			  ?? new StringerObject(stringer.Geometry);

		/// <summary>
		///     Get the <see cref="StringerObject" /> from <see cref="Model.Stringers" /> associated to a
		///     <see cref="SPMElement{T}" />
		///     .
		/// </summary>
		/// <remarks>
		///     A <see cref="StringerObject" /> is created if <paramref name="spmElement" /> is not null and is not listed.
		/// </remarks>
		public static explicit operator StringerObject?(SPMElement<StringerGeometry>? spmElement) => spmElement is Stringer stringer
			? (StringerObject?) stringer
			: null;

		/// <summary>
		///     Get the <see cref="StringerObject" /> from <see cref="Model.Stringers" /> associated to a <see cref="Line" />.
		/// </summary>
		/// <remarks>
		///     Can be null if <paramref name="line" /> is null or doesn't correspond to a <see cref="StringerObject" />
		/// </remarks>
		public static explicit operator StringerObject?(Line? line) => line is null
			? null
			: Model.Stringers.GetByObjectId(line.ObjectId);

		/// <summary>
		///     Get the <see cref="Line" /> associated to a <see cref="StringerObject" />.
		/// </summary>
		/// <remarks>
		///     Can be null if <paramref name="stringerObject" /> is null or doesn't exist in drawing.
		/// </remarks>
		public static explicit operator Line?(StringerObject? stringerObject) => (Line?) stringerObject?.GetObject();

		#endregion

	}
}