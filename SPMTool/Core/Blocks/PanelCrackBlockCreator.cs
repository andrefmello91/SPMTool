using System;
using andrefmello91.Extensions;
using andrefmello91.OnPlaneComponents;
using andrefmello91.SPMElements;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using SPMTool.Enums;

using UnitsNet;
#nullable enable

namespace SPMTool.Core.Blocks
{
	/// <summary>
	///     Block creator class.
	/// </summary>
	public class PanelCrackBlockCreator : BlockCreator
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
				Attributes = new[] { GetAttribute(value, RotationAngle, ScaleFactor, TextHeight) };
			}
		}

		#endregion

		#region Constructors

		/// <summary>
		///     Block creator constructor.
		/// </summary>
		/// <param name="crackOpening">The crack opening.</param>
		/// <inheritdoc />
		private PanelCrackBlockCreator(Point insertionPoint, Length crackOpening, double rotationAngle, double scaleFactor, double textHeight, ObjectId blockTableId)
			: base(insertionPoint, Block.PanelCrack, rotationAngle, scaleFactor, textHeight, blockTableId)
		{
			_crackOpening = crackOpening;

			Attributes = new[] { GetAttribute(crackOpening, rotationAngle, scaleFactor, textHeight) };
		}

		#endregion

		#region Methods

		/// <summary>
		///     Get the average stress <see cref="BlockCreator" />.
		/// </summary>
		/// <inheritdoc cref="PanelCrackBlockCreator(Point, Length, double, double, double, ObjectId)" />
		public static PanelCrackBlockCreator? From(Point insertionPoint, Length crackOpening, double rotationAngle, double scaleFactor, double textHeight, ObjectId blockTableId) =>
			crackOpening > Length.Zero
				? new PanelCrackBlockCreator(insertionPoint, crackOpening, StressBlockCreator.ImproveAngle(rotationAngle), scaleFactor, textHeight, blockTableId)
				: null;

		/// <summary>
		///     Get the attribute for crack block.
		/// </summary>
		/// <inheritdoc cref="PanelCrackBlockCreator(Point, Length, double, double, double, ObjectId)" />
		private static AttributeReference GetAttribute(Length crackOpening, double rotationAngle, double scaleFactor, double textHeight)
		{
			var w = crackOpening.Value.Abs();

			// Set the insertion point
			var pt = new Point(0, -40 * scaleFactor);

			var attRef = new AttributeReference
			{
				Position            = pt.ToPoint3d(),
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

			return attRef;
		}

		#endregion

	}
}