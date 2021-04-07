using System;
using System.Collections.Generic;
using System.Linq;
using andrefmello91.Extensions;
using andrefmello91.OnPlaneComponents;
using andrefmello91.SPMElements;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using SPMTool.Enums;
using SPMTool.Extensions;
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

		private readonly Stringer _stringer;

		#endregion

		#region Properties

		/// <inheritdoc />
		public Layer Layer => Layer.StringerForce;

		/// <inheritdoc />
		public string Name => $"Stringer Force {_stringer.Number}";

		/// <inheritdoc />
		public ObjectId ObjectId { get; set; }

		#endregion

		#region Constructors

		/// <summary>
		///     Stringer force creator constructor.
		/// </summary>
		/// <param name="stringer">The <see cref="Stringer"/>.</param>
		public StringerForceCreator(Stringer stringer) => _stringer = stringer;

		#endregion

		#region Methods

		/// <summary>
		///     Get the entities for combined diagram.
		/// </summary>
		/// <inheritdoc cref="PureTensionOrCompression" />
		private static IEnumerable<Entity> Combined(Stringer stringer)
		{
			var stPt     = stringer.Geometry.InitialPoint;
			var l        = stringer.Geometry.Length;
			var maxForce = Results.MaxStringerForce;
			var (n1, n3) = stringer.NormalForces;
			var angle = stringer.Geometry.Angle;
			
			// Correct units
			var unit = DataBase.Settings.Units.StringerForces;
			n1 = n1.ToUnit(unit);
			n3 = n3.ToUnit(unit);

			// Calculate the dimensions to draw the solid (the maximum dimension will be 150 mm)
			// Invert tension and compression axis
			Length
				h1 = - Length.FromMillimeters(150) * n1 / maxForce,
				h3 = - Length.FromMillimeters(150) * n3 / maxForce;

			// Calculate the point where the Stringer force will be zero
			var x     = h1.Abs() * l / (h1.Abs() + h3.Abs());
			var invPt = new Point(stPt.X + x, stPt.Y);

			// Calculate the points (the solid will be rotated later)
			var vrts1 = new[]
				{
					stPt,
					invPt,
					new(stPt.X, stPt.Y + h1)
				}
				.ToPoint3ds()!.ToArray();


			var vrts3 = new[]
				{
					invPt,
					new(stPt.X + l, stPt.Y),
					new(stPt.X + l, stPt.Y + h3)
				}
				.ToPoint3ds()!.ToArray();

			// Create the diagrams as solids with 3 segments (3 points)
			var dgrm1 = new Solid(vrts1[0], vrts1[1], vrts1[2])
			{
				Layer      = $"{Layer.StringerForce}",
				ColorIndex = (short) n1.GetColorCode()
			};

			// Rotate the diagram
			dgrm1.TransformBy(Matrix3d.Rotation(angle, DataBase.Ucs.Zaxis, stPt.ToPoint3d()));

			var dgrm3 = new Solid(vrts3[0], vrts3[1], vrts3[2])
			{
				Layer      = $"{Layer.StringerForce}",
				ColorIndex = (short) n3.GetColorCode()
			};

			// Rotate the diagram
			dgrm3.TransformBy(Matrix3d.Rotation(angle, DataBase.Ucs.Zaxis, stPt.ToPoint3d()));

			return
				new[] { dgrm1, dgrm3 };
		}

		/// <summary>
		///		Create diagram for stringer.
		/// </summary>
		/// <param name="stringer">The <see cref="Stringer" />.</param>
		private static IEnumerable<Entity> CreateDiagram(Stringer stringer)
		{
			var entities = stringer.State is StringerForceState.Combined
				? Combined(stringer).ToArray()
				: new[] { PureTensionOrCompression(stringer) };

			return entities.Concat(GetTexts(stringer));
		}

		/// <summary>
		///     Get the attributes for stringer force block.
		/// </summary>
		/// <inheritdoc cref="CreateDiagram" />
		private static IEnumerable<DBText> GetTexts(Stringer stringer)
		{
			var stPt     = stringer.Geometry.InitialPoint;
			var l        = stringer.Geometry.Length;
			var maxForce = Results.MaxStringerForce;

			var (n1, n3) = stringer.NormalForces;
			var angle       = stringer.Geometry.Angle;
			var scaleFactor = Results.ResultScaleFactor;

			// Correct units
			var unit = DataBase.Settings.Units.StringerForces;
			n1 = n1.ToUnit(unit);
			n3 = n3.ToUnit(unit);

			// Calculate the dimensions to draw the solid (the maximum dimension will be 150 mm)
			// Invert tension and compression axis
			Length
				h1 = - Length.FromMillimeters(150) * n1 / maxForce,
				h3 = - Length.FromMillimeters(150) * n3 / maxForce;

			// Create attributes

			if (!n1.Value.ApproxZero(1E-3))
			{
				var pt1 = (n1.Value > 0
					? new Point(stPt.X + Length.FromMillimeters(10) * scaleFactor, stPt.Y + h1 - Length.FromMillimeters(30) * scaleFactor)
					: new Point(stPt.X + Length.FromMillimeters(10) * scaleFactor, stPt.Y + h1 + Length.FromMillimeters(30) * scaleFactor))
					.ToPoint3d();

				// Rotate
				var txt1 = new DBText
				{
					Position       = pt1,
					TextString     = $"{n1.Value.Abs():0.00}",
					Height         = 30 * scaleFactor,
					Justify        = AttachmentPoint.MiddleLeft,
					AlignmentPoint = pt1,
					Layer          = $"{Layer.StringerForce}",
					ColorIndex     = (short) n1.GetColorCode()
				};


				txt1.TransformBy(Matrix3d.Rotation(angle, DataBase.Ucs.Zaxis, stPt.ToPoint3d()));

				yield return txt1;
			}

			if (n3.Value.ApproxZero(1E-3))
				yield break;

			var pt3 = (n3.Value > 0
					? new Point(stPt.X + l - Length.FromMillimeters(10) * scaleFactor, stPt.Y + h3 - Length.FromMillimeters(30) * scaleFactor)
					: new Point(stPt.X + l - Length.FromMillimeters(10) * scaleFactor, stPt.Y + h3 + Length.FromMillimeters(30) * scaleFactor))
				.ToPoint3d();

			var txt3 = new DBText
			{
				Position       = pt3,
				TextString     = $"{n3.Value.Abs():0.00}",
				Height         = 30 * scaleFactor,
				Justify        = AttachmentPoint.MiddleRight,
				AlignmentPoint = pt3,
				Layer          = $"{Layer.StringerForce}",
				ColorIndex     = (short) n3.GetColorCode()
			};

			// Set alignment point

			// Rotate
			txt3.TransformBy(Matrix3d.Rotation(angle, DataBase.Ucs.Zaxis, stPt.ToPoint3d()));

			yield return txt3;
		}

		/// <summary>
		///     Get the entities for pure tension/compression diagram.
		/// </summary>
		/// <inheritdoc cref="CreateDiagram" />
		private static Entity PureTensionOrCompression(Stringer stringer)
		{
			var stPt     = stringer.Geometry.InitialPoint;
			var l        = stringer.Geometry.Length;
			var maxForce = Results.MaxStringerForce;
			var (n1, n3) = stringer.NormalForces;
			var angle = stringer.Geometry.Angle;

			// Correct units
			var unit = DataBase.Settings.Units.StringerForces;
			n1 = n1.ToUnit(unit);
			n3 = n3.ToUnit(unit);

			// Calculate the dimensions to draw the solid (the maximum dimension will be 150 mm)
			// Invert tension and compression axis
			Length
				h1 = - Length.FromMillimeters(150) * n1 / maxForce,
				h3 = - Length.FromMillimeters(150) * n3 / maxForce;

			// Calculate the points (the solid will be rotated later)
			var vrts = new[]
				{
					stPt,
					new(stPt.X + l, stPt.Y),
					new(stPt.X, stPt.Y + h1),
					new(stPt.X + l, stPt.Y + h3)
				}
				.ToPoint3ds()!.ToArray();

			// Create the diagram as a solid with 4 segments (4 points)
			var nMax = n1.Abs() > n3.Abs()
				? n1
				: n3;
			
			var dgrm = new Solid(vrts[0], vrts[1], vrts[2], vrts[3])
			{
				Layer      = $"{Layer.StringerForce}",
				ColorIndex = (short) nMax.GetColorCode()
			};

			// Rotate the diagram
			dgrm.TransformBy(Matrix3d.Rotation(angle, DataBase.Ucs.Zaxis, stPt.ToPoint3d()));

			return dgrm;
		}

		/// <inheritdoc />
		public void AddToDrawing() => ObjectId = CreateDiagram(_stringer).ToList().AddToDrawingAsGroup(Name);

		/// <inheritdoc />
		public void RemoveFromDrawing() => ObjectId.RemoveFromDrawing();
		
		/// <inheritdoc />
		DBObject IDBObjectCreator.CreateObject() => CreateObject();

		/// <inheritdoc />
		DBObject? IDBObjectCreator.GetObject() => GetObject();

		/// <inheritdoc />
		public Group CreateObject() => new (Name, true);

		/// <inheritdoc />
		public Group? GetObject() => (Group?) ObjectId.GetDBObject();

		#endregion

	}
}