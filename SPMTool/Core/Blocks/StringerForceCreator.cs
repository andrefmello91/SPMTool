using System.Collections.Generic;
using System.Linq;
using andrefmello91.Extensions;
using andrefmello91.OnPlaneComponents;
using andrefmello91.SPMElements.StringerProperties;
using Autodesk.AutoCAD.DatabaseServices;
using SPMTool.Application;
using SPMTool.Enums;
using UnitsNet;
#nullable enable

namespace SPMTool.Core.Blocks
{
	/// <summary>
	///     Block creator class.
	/// </summary>
	public class StringerForceCreator : IDBObjectCreator<Group>
	{

		#region Fields

		private readonly StringerGeometry _geometry;
		private readonly Force _n1;
		private readonly Force _n2;
		private readonly Force _maxForce;
		private readonly int _number;
		private readonly double _scaleFactor;
		private readonly double _textHeight;

		#endregion

		#region Properties

		#region Interface Implementations

		/// <inheritdoc />
		public ObjectId BlockTableId { get; set; }

		/// <inheritdoc />
		public Layer Layer => Layer.StringerForce;

		/// <inheritdoc />
		public string Name => $"Stringer Force {_number}";

		/// <inheritdoc />
		public ObjectId ObjectId { get; set; }

		#endregion

		#endregion

		#region Constructors

		/// <summary>
		///     Stringer force creator constructor.
		/// </summary>
		/// <inheritdoc cref="From" />
		private StringerForceCreator(StringerGeometry geometry, (Force N1, Force N2) normalForces, Force maxForce, double scaleFactor, double textHeight, int stringerNumber, ObjectId blockTableId)
		{
			_geometry    = geometry;
			(_n1, _n2)   = normalForces;
			_maxForce    = maxForce;
			_scaleFactor = scaleFactor;
			_textHeight  = textHeight;
			_number      = stringerNumber;
			BlockTableId = blockTableId;
		}

		#endregion

		#region Methods

		/// <summary>
		///     Create the stringer diagram. Can be null if the stringer is unloaded.
		/// </summary>
		/// <param name="geometry">The geometry.</param>
		/// <param name="normalForces">The normal forces at the start and end of the stringer.</param>
		/// <param name="maxForce">The maximum normal force in all of the stringers in the model.</param>
		/// <param name="scaleFactor">The scale factor.</param>
		/// <param name="textHeight">The text height for attributes.</param>
		/// <param name="stringerNumber">The number of the stringer.</param>
		/// <param name="blockTableId">The <see cref="ObjectId"/> of the block table that contains this object.</param>
		public static StringerForceCreator? From(StringerGeometry geometry, (Force N1, Force N2) normalForces, Force maxForce, double scaleFactor, double textHeight, int stringerNumber, ObjectId blockTableId) =>
			!normalForces.N1.ApproxZero(Units.ForceTolerance) || !normalForces.N2.ApproxZero(Units.ForceTolerance)
				? new StringerForceCreator(geometry, normalForces, maxForce, scaleFactor, textHeight, stringerNumber, blockTableId)
				: null;

		/// <summary>
		///     Get the entities for combined diagram.
		/// </summary>
		/// <inheritdoc cref="From" />
		private static IEnumerable<Entity> Combined(StringerGeometry geometry, (Force N1, Force N2) normalForces, Force maxForce, double scaleFactor)
		{
			var stPt = geometry.InitialPoint;
			var l    = geometry.Length;
			var (n1, n3) = normalForces;
			var angle = geometry.Angle;

			// Calculate the dimensions to draw the solid (the maximum dimension will be 150 mm)
			// Invert tension and compression axis
			Length
				h1 = -Length.FromMillimeters(150) * scaleFactor * n1 / maxForce,
				h3 = -Length.FromMillimeters(150) * scaleFactor * n3 / maxForce;

			// Calculate the point where the Stringer force will be zero
			var x     = h1.Abs() * l / (h1.Abs() + h3.Abs());
			var invPt = new Point(stPt.X + x, stPt.Y);

			// Calculate the points (rotated)
			var vrts1 = new[]
				{
					stPt,
					invPt,
					new(stPt.X, stPt.Y + h1)
				}
				.Select(p => p.Rotate(angle)).ToPoint3ds().ToArray();


			var vrts3 = new[]
				{
					invPt,
					new(stPt.X + l, stPt.Y),
					new(stPt.X + l, stPt.Y + h3)
				}
				.Select(p => p.Rotate(angle)).ToPoint3ds().ToArray();

			// Create the diagrams as solids with 3 segments (3 points)
			yield return
				new Solid(vrts1[0], vrts1[1], vrts1[2])
				{
					Layer      = $"{Layer.StringerForce}",
					ColorIndex = (short) n1.GetColorCode()
				};

			// Rotate the diagram
			// dgrm1.TransformBy(Matrix3d.Rotation(angle, SPMModel.Ucs.Zaxis, stPt.ToPoint3d()));

			yield return
				new Solid(vrts3[0], vrts3[1], vrts3[2])
				{
					Layer      = $"{Layer.StringerForce}",
					ColorIndex = (short) n3.GetColorCode()
				};

			// Rotate the diagram
			// dgrm3.TransformBy(Matrix3d.Rotation(angle, SPMModel.Ucs.Zaxis, stPt.ToPoint3d()));

			// return
			// 	new[] { dgrm1, dgrm3 };
		}

		/// <summary>
		///     Get the attributes for stringer force block.
		/// </summary>
		/// <inheritdoc cref="From" />
		private static IEnumerable<DBText> GetTexts(StringerGeometry geometry, (Force N1, Force N2) normalForces, Force maxForce, double scaleFactor, double textHeight)
		{
			var stPt = geometry.InitialPoint;
			var l    = geometry.Length;
			var (n1, n3) = normalForces;
			var angle = geometry.Angle;

			// Calculate the dimensions to draw the solid (the maximum dimension will be 150 mm)
			// Invert tension and compression axis
			Length
				h1 = -Length.FromMillimeters(150) * scaleFactor * n1 / maxForce,
				h3 = -Length.FromMillimeters(150) * scaleFactor * n3 / maxForce;

			// Create attributes

			if (!n1.ApproxZero(Units.StringerForceTolerance))
			{
				var pt1 = (n1.Value > 0
						? new Point(stPt.X + Length.FromMillimeters(10) * scaleFactor, stPt.Y + h1 - Length.FromMillimeters(30) * scaleFactor)
						: new Point(stPt.X + Length.FromMillimeters(10) * scaleFactor, stPt.Y + h1 + Length.FromMillimeters(30) * scaleFactor))
					.Rotate(stPt, angle).ToPoint3d();

				// Rotate
				yield return
					new DBText
					{
						Position       = pt1,
						TextString     = $"{n1.Value.Abs():0.00}",
						Height         = textHeight,
						Justify        = AttachmentPoint.MiddleLeft,
						AlignmentPoint = pt1,
						Layer          = $"{Layer.StringerForce}",
						ColorIndex     = (short) n1.GetColorCode(),
						Rotation       = angle
					};


				// txt1.TransformBy(Matrix3d.Rotation(angle, SPMModel.Ucs.Zaxis, stPt.ToPoint3d()));
				//
				// yield return txt1;
			}

			if (n3.ApproxZero(Units.StringerForceTolerance))
				yield break;

			var pt3 = (n3.Value > 0
					? new Point(stPt.X + l - Length.FromMillimeters(10) * scaleFactor, stPt.Y + h3 - Length.FromMillimeters(30) * scaleFactor)
					: new Point(stPt.X + l - Length.FromMillimeters(10) * scaleFactor, stPt.Y + h3 + Length.FromMillimeters(30) * scaleFactor))
				.Rotate(stPt, angle).ToPoint3d();

			yield return
				new DBText
				{
					Position       = pt3,
					TextString     = $"{n3.Value.Abs():0.00}",
					Height         = textHeight,
					Justify        = AttachmentPoint.MiddleRight,
					AlignmentPoint = pt3,
					Layer          = $"{Layer.StringerForce}",
					ColorIndex     = (short) n3.GetColorCode(),
					Rotation       = angle
				};

			// Set alignment point

			// Rotate
			// txt3.TransformBy(Matrix3d.Rotation(angle, SPMModel.Ucs.Zaxis, stPt.ToPoint3d()));
			//
			// yield return txt3;
		}

		/// <summary>
		///     Get the entities for pure tension/compression diagram.
		/// </summary>
		/// <inheritdoc cref="From" />
		private static Entity PureTensionOrCompression(StringerGeometry geometry, (Force N1, Force N2) normalForces, Force maxForce, double scaleFactor)
		{
			var stPt = geometry.InitialPoint;
			var l    = geometry.Length;
			var (n1, n3) = normalForces;
			var angle = geometry.Angle;

			// Calculate the dimensions to draw the solid (the maximum dimension will be 150 mm)
			// Invert tension and compression axis
			Length
				h1 = -Length.FromMillimeters(150) * scaleFactor * n1 / maxForce,
				h3 = -Length.FromMillimeters(150) * scaleFactor * n3 / maxForce;

			// Calculate the points (the solid will be rotated later)
			var vrts = new[]
				{
					stPt,
					new(stPt.X + l, stPt.Y),
					new(stPt.X, stPt.Y + h1),
					new(stPt.X + l, stPt.Y + h3)
				}
				.Select(p => p.Rotate(angle)).ToPoint3ds().ToArray();

			// Create the diagram as a solid with 4 segments (4 points)
			var nMax = n1.Abs() > n3.Abs()
				? n1
				: n3;

			return new Solid(vrts[0], vrts[1], vrts[2], vrts[3])
			{
				Layer      = $"{Layer.StringerForce}",
				ColorIndex = (short) nMax.GetColorCode()
			};

			// Rotate the diagram
			// dgrm.TransformBy(Matrix3d.Rotation(angle, SPMModel.Ucs.Zaxis, stPt.ToPoint3d()));
			//
			// return dgrm;
		}

		/// <summary>
		///     Create diagram for stringer.
		/// </summary>
		public IEnumerable<Entity> CreateDiagram()
		{
			var combined = UnitMath.Max(_n1, _n2) > Force.Zero && UnitMath.Min(_n1, _n2) < Force.Zero;

			var entities = combined
				? Combined(_geometry, (_n1, _n2), _maxForce, _scaleFactor).ToArray()
				: new[] { PureTensionOrCompression(_geometry, (_n1, _n2), _maxForce, _scaleFactor) };

			return entities.Concat(GetTexts(_geometry, (_n1, _n2), _maxForce, _scaleFactor, _textHeight));
		}

		#region Interface Implementations

		/// <inheritdoc />
		DBObject IDBObjectCreator.CreateObject() => CreateObject();

		/// <inheritdoc />
		public Group CreateObject() => new(Name, true);

		/// <inheritdoc />
		DBObject? IDBObjectCreator.GetObject() => GetObject();

		/// <inheritdoc />
		public Group? GetObject() => (Group?) SPMModel.GetOpenedModel(BlockTableId)?.AcadDatabase.GetObject(ObjectId);

		#endregion

		#endregion

	}
}