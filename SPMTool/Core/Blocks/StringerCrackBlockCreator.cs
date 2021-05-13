using System.Collections.Generic;
using andrefmello91.Extensions;
using andrefmello91.OnPlaneComponents;
using andrefmello91.SPMElements;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using SPMTool.Application;
using SPMTool.Enums;
using SPMTool.Extensions;
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
				Attributes = new[] { GetAttribute(value, RotationAngle, ScaleFactor) };
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
		private StringerCrackBlockCreator(Point insertionPoint, Length crackOpening, double rotationAngle, Point rotationPoint, double scaleFactor)
			: base(insertionPoint, Block.StringerCrack, rotationAngle, scaleFactor)
		{
			_crackOpening = crackOpening;

			RotationPoint = rotationPoint;

			Attributes = new[] { GetAttribute(crackOpening, rotationAngle, scaleFactor) };
		}

		#endregion

		#region Methods

		/// <summary>
		///     Get the average stress <see cref="BlockCreator" />.
		/// </summary>
		/// <param name="stringer">The <see cref="Stringer" />.</param>
		public static IEnumerable<StringerCrackBlockCreator?> CreateBlocks(Stringer? stringer)
		{
			var blocks = new StringerCrackBlockCreator?[3];

			if (stringer.Model is ElementModel.Elastic)
				return blocks;

			var l  = stringer.Geometry.Length;
			var ix = stringer.Geometry.InitialPoint.X + 0.1 * l;
			var y  = stringer.Geometry.InitialPoint.Y;

			for (var i = 0; i < 3; i++)
				blocks[i] = !stringer.CrackOpenings[i].ApproxZero(Units.CrackTolerance)
					? new StringerCrackBlockCreator(new Point(ix + 0.4 * i * l, y), stringer.CrackOpenings[i], stringer.Geometry.Angle, stringer.Geometry.InitialPoint, Results.ResultScaleFactor)
					: null;

			return blocks;
		}

		/// <summary>
		///     Get the attribute for crack block.
		/// </summary>
		/// <inheritdoc cref="PanelCrackBlockCreator(Point, Length, double, double)" />
		private static AttributeReference GetAttribute(Length crackOpening, double rotationAngle, double scaleFactor)
		{
			var w = crackOpening.ToUnit(DataBase.Settings.Units.CrackOpenings).Value.Abs();

			// Set the insertion point
			var pt = new Point(0, -100 * scaleFactor);

			var attRef = new AttributeReference
			{
				Position            = pt.ToPoint3d(),
				TextString          = $"{w:0.00E+00}",
				Height              = Results.TextHeight,
				Layer               = $"{Layer.Cracks}",
				Justify             = AttachmentPoint.MiddleCenter,
				LockPositionInBlock = true,
				Invisible           = false
			};

			// Rotate text
			if (!rotationAngle.ApproxZero(1E-3))
				attRef.TransformBy(Matrix3d.Rotation(rotationAngle, DataBase.Ucs.Zaxis, new Point3d(0, 0, 0)));

			return attRef;
		}

		#endregion

	}
}