using System.Collections.Generic;
using System.Linq;
using andrefmello91.Extensions;
using andrefmello91.OnPlaneComponents;
using andrefmello91.SPMElements;
using andrefmello91.SPMElements.StringerProperties;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using SPMTool.Application;
using SPMTool.Enums;

using UnitsNet;
#nullable enable

namespace SPMTool.Core.Blocks
{
	/// <summary>
	///     Block creator class.
	/// </summary>
	public class StringerCrackBlockCreator : BlockCreator
	{

		#region Fields

		private Length _crackOpening;

		#endregion

		#region Properties

		/// <summary>
		///     Get/set the stress state.
		/// </summary>
		public Length CrackOpening
		{
			get => _crackOpening;
			set
			{
				_crackOpening = value;

				// Update attribute
				Attributes = new[] { GetAttribute(value, RotationAngle, ScaleFactor, TextHeight, BlockTableId) };
			}
		}

		#endregion

		#region Constructors

		/// <summary>
		///     Block creator constructor.
		/// </summary>
		/// <param name="insertionPoint">The insertion <see cref="Point" /> of block.</param>
		/// <param name="crackOpening">The crack opening.</param>
		/// <param name="rotationPoint">The reference <see cref="Point" /> for block rotation.</param>
		/// <inheritdoc />
		private StringerCrackBlockCreator(Point insertionPoint, Length crackOpening, double rotationAngle, Point rotationPoint, double scaleFactor, double textHeight, ObjectId blockTableId)
			: base(insertionPoint, Block.StringerCrack, rotationAngle, scaleFactor, textHeight, blockTableId)
		{
			_crackOpening = crackOpening;

			RotationPoint = rotationPoint;

			Attributes = new[] { GetAttribute(crackOpening, rotationAngle, scaleFactor, textHeight, blockTableId) };
		}

		#endregion

		#region Methods

		/// <summary>
		///     Get the average stress <see cref="BlockCreator" />.
		/// </summary>
		/// <param name="geometry">The geometry of the stringer.</param>
		/// <param name="crackOpenings">The collection of crack openings in start, mid and end of the stringer.</param>
		public static IEnumerable<StringerCrackBlockCreator?> CreateBlocks(StringerGeometry geometry, IEnumerable<Length> crackOpenings, double scaleFactor, double textHeight, ObjectId blockTableId)
		{
			var pts = GetInsertionPoints(geometry).ToArray();

			var cracks = crackOpenings.ToArray();
			
			for (var i = 0; i < cracks.Length; i++)
				yield return !cracks[i].ApproxZero(Units.CrackTolerance)
					? new StringerCrackBlockCreator(pts[i], cracks[i], geometry.Angle, geometry.InitialPoint, scaleFactor, textHeight, blockTableId)
					: null;
		}

		/// <summary>
		///		Get the insertion points of blocks.
		/// </summary>
		/// <param name="geometry">The geometry of the stringer.</param>
		private static IEnumerable<Point> GetInsertionPoints(StringerGeometry geometry)
		{
			var l  = geometry.Length;
			var ix = geometry.InitialPoint.X + 0.1 * l;
			var y  = geometry.InitialPoint.Y;

			for (var i = 0; i < 3; i++)
				yield return new Point(ix + 0.4 * i * l, y);
		}

		/// <summary>
		///     Get the attribute for crack block.
		/// </summary>
		/// <inheritdoc cref="StringerCrackBlockCreator(Point, Length, double, Point, double, double, ObjectId)" />
		private static AttributeReference GetAttribute(Length crackOpening, double rotationAngle, double scaleFactor, double textHeight, ObjectId blockTableId)
		{
			var w = crackOpening.Value.Abs();

			// Set the insertion point
			var unit = SPMModel.GetOpenedModel(blockTableId)!.Settings.Units.Geometry;
			var pt   = new Point(0, -100).Rotate(rotationAngle).ToPoint3d(unit);

			return new AttributeReference
			{
				Position            = pt,
				TextString          = $"{w:0.00E+00}",
				Height              = textHeight,
				Layer               = $"{Layer.Cracks}",
				Justify             = AttachmentPoint.MiddleCenter,
				LockPositionInBlock = true,
				Invisible           = false,
				Rotation            = rotationAngle
			};

			// Rotate text
			// if (!rotationAngle.ApproxZero(1E-3))
			// 	attRef.TransformBy(Matrix3d.Rotation(rotationAngle, SPMModel.Ucs.Zaxis, new Point3d(0, 0, 0)));

			// return attRef;
		}

		#endregion

	}
}