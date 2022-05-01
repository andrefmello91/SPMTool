#nullable enable

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
using static SPMTool.Core.SPMModel;

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

		public override Layer Layer => Layer.Stringer;

		public override string Name => $"Stringer {Number}";

		#endregion

		#region Constructors

		/// <inheritdoc cref="StringerObject(StringerGeometry, ObjectId)" />
		/// <param name="initialPoint">The initial <see cref="Point3d" />.</param>
		/// <param name="endPoint">The end <see cref="Point3d" />.</param>
		/// <param name="unit">The <see cref="LengthUnit" /> of points' coordinates.</param>
		public StringerObject(Point3d initialPoint, Point3d endPoint, ObjectId blockTableId, LengthUnit unit = LengthUnit.Millimeter)
			: this(initialPoint.ToPoint(unit), endPoint.ToPoint(unit), blockTableId)
		{
		}

		/// <inheritdoc cref="StringerObject(StringerGeometry, ObjectId)" />
		/// <param name="initialPoint">The initial <see cref="Point" />.</param>
		/// <param name="endPoint">The end <see cref="Point" />.</param>
		public StringerObject(Point initialPoint, Point endPoint, ObjectId blockTableId)
			: this(new StringerGeometry(initialPoint, endPoint, 100, 100), blockTableId)
		{
		}

		/// <summary>
		///     Create a stringer object.
		/// </summary>
		/// <param name="geometry">The <see cref="StringerGeometry" />.</param>
		/// <inheritdoc />
		public StringerObject(StringerGeometry geometry, ObjectId blockTableId)
			: base(geometry, blockTableId)
		{
		}

		#endregion

		#region Methods

		/// <summary>
		///     Read a <see cref="StringerObject" /> from an existing line in the drawing.
		/// </summary>
		/// <param name="line">The <see cref="Line" /> object of the stringer.</param>
		/// <param name="unit">The unit for geometry.</param>
		public static StringerObject From(Line line, LengthUnit unit)
		{
			var pts = new[]
				{
					line.StartPoint.ToPoint(unit),
					line.EndPoint.ToPoint(unit)
				}.OrderBy(p => p.Y)
				.ThenBy(p => p.X)
				.ToArray();

			// Get correct order
			var (p1, p2) = pts[1].X > pts[0].X
				? (pts[0], pts[1])
				: (pts[1], pts[0]);

			return
				new StringerObject(p1, p2, line.Database.BlockTableId)
				{
					ObjectId = line.ObjectId
				};
		}

		/// <summary>
		///     Create crack blocks.
		/// </summary>
		/// <param name="scaleFactor">The scale factor.</param>
		/// <param name="textHeight">The text height for attributes.</param>
		/// <param name="crackUnit">The unit for crack openings.</param>
		public IEnumerable<StringerCrackBlockCreator?> CreateCrackBlocks(double scaleFactor, double textHeight, LengthUnit crackUnit) =>
			_stringer!.Model is ElementModel.Nonlinear
				? StringerCrackBlockCreator.CreateBlocks(_stringer!.Geometry, _stringer.CrackOpenings.Select(c => c.ToUnit(crackUnit)).ToArray(), scaleFactor, textHeight, BlockTableId)
				: new StringerCrackBlockCreator?[] { null, null, null };

		/// <summary>
		///     Create the stringer diagram. Can be null if the stringer is unloaded.
		/// </summary>
		/// <param name="scaleFactor">The scale factor.</param>
		/// <param name="textHeight">The text height for attributes.</param>
		/// <param name="maxForce">The maximum normal force in all of the stringers in the model.</param>
		public StringerForceCreator? CreateDiagram(double scaleFactor, double textHeight, Force maxForce, ForceUnit unit) =>
			StringerForceCreator.From(_stringer!.Geometry, (_stringer.NormalForces.N1.ToUnit(unit), _stringer.NormalForces.N3.ToUnit(unit)), maxForce, scaleFactor, textHeight, Number, BlockTableId);

		/// <summary>
		///     Divide this <see cref="StringerObject" /> in a <paramref name="number" /> of new ones.
		/// </summary>
		/// <param name="number">The number of new <see cref="StringerObject" />'s.</param>
		public IEnumerable<StringerObject> Divide(int number)
		{
			var geometries = Geometry.Divide(number).ToArray();

			foreach (var geometry in geometries)
				yield return new StringerObject(geometry, BlockTableId)
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

			var unit = GetOpenedModel(BlockTableId)!.Settings.Units.Geometry;

			return
				new Line(start.ToPoint3d(unit), end.ToPoint3d(unit))
				{
					Layer = $"{Layer.Displacements}"
				};
		}

		/// <remarks>
		///     This method returns a linear object.
		/// </remarks>
		/// <inheritdoc />
		public override INumberedElement GetElement() => GetElement(GetOpenedModel(BlockTableId)!.Nodes.GetElements().Cast<Node>().ToArray()!);

		/// <inheritdoc cref="SPMObject{T}.GetElement()" />
		/// <param name="nodes">The collection of <see cref="Node" />'s in the drawing.</param>
		/// <param name="elementModel">The <see cref="ElementModel" />.</param>
		public Stringer GetElement(IEnumerable<Node> nodes, ElementModel elementModel = ElementModel.Elastic)
		{
			var database = GetOpenedModel(BlockTableId)!;
			_stringer        = Stringer.FromNodes(nodes, Geometry.InitialPoint, Geometry.EndPoint, Geometry.CrossSection, database.ConcreteData.Parameters, database.ConcreteData.ConstitutiveModel, Reinforcement?.Clone(), elementModel);
			_stringer.Number = Number;
			return _stringer;
		}

		protected override void GetProperties()
		{
			if (GetCrossSection() is { } crossSection)
				PropertyField.CrossSection = crossSection;

			if (GetReinforcement() is { } reinforcement)
				_reinforcement = reinforcement;
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

		public override DBObject CreateObject()
		{
			var unit = GetOpenedModel(BlockTableId)!.Settings.Units.Geometry;

			return
				new Line(Geometry.InitialPoint.ToPoint3d(unit), Geometry.EndPoint.ToPoint3d(unit))
				{
					Layer = $"{Layer}"
				};
		}

		/// <inheritdoc />
		Line IDBObjectCreator<Line>.CreateObject() => (Line) CreateObject();

		/// <inheritdoc />
		Line? IDBObjectCreator<Line>.GetObject() => (Line?) base.GetObject();

		public bool Equals(StringerObject other) => base.Equals(other);

		#endregion

		#region Operators

		/// <summary>
		///     Get the <see cref="Stringer" /> element from a <see cref="StringerObject" />.
		/// </summary>
		public static explicit operator Stringer?(StringerObject? stringerObject) => (Stringer?) stringerObject?.GetElement();

		/// <summary>
		///     Get the <see cref="StringerObject" /> from the active model associated to a <see cref="Stringer" />.
		/// </summary>
		public static explicit operator StringerObject?(Stringer? stringer) => stringer is not null
			? ActiveModel.Stringers[stringer.Geometry]
			: null;

		/// <summary>
		///     Get the <see cref="StringerObject" /> from <see cref="SPMModel.Stringers" /> associated to a
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
		///     Get the <see cref="StringerObject" /> from <see cref="SPMModel.Stringers" /> associated to a <see cref="Line" />.
		/// </summary>
		/// <remarks>
		///     Can be null if <paramref name="line" /> is null or doesn't correspond to a <see cref="StringerObject" />
		/// </remarks>
		public static explicit operator StringerObject?(Line? line) => (StringerObject?) line.GetSPMObject();

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