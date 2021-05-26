using andrefmello91.Extensions;
using andrefmello91.OnPlaneComponents;
using andrefmello91.SPMElements;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using MathNet.Numerics;
using SPMTool.Enums;
using UnitsNet;
#nullable enable

namespace SPMTool.Core.Blocks
{
	/// <summary>
	///     Block creator class.
	/// </summary>
	public class ShearBlockCreator : BlockCreator
	{

		#region Fields

		private Pressure _shearStress;

		#endregion

		#region Properties

		/// <summary>
		///     Get/set the shear stress.
		/// </summary>
		public Pressure ShearStress
		{
			get => _shearStress;
			set
			{
				_shearStress = value;

				// Update attribute
				Attributes = new[] { GetAttribute(value, TextHeight, Layer) };
			}
		}

		#endregion

		#region Constructors

		/// <summary>
		///     Block creator constructor.
		/// </summary>
		/// <param name="shearStress">The shear stress, in the unit required.</param>
		/// <inheritdoc />
		private ShearBlockCreator(Point insertionPoint, Pressure shearStress, double scaleFactor, double textHeight, ObjectId blockTableId)
			: base(insertionPoint, Block.Shear, GetRotationAngle(shearStress), scaleFactor, textHeight, blockTableId, Axis.Y)
		{
			_shearStress = shearStress;

			Attributes = new[] { GetAttribute(shearStress, textHeight, Layer) };
		}

		#endregion

		#region Methods

		/// <summary>
		///     Get the shear <see cref="BlockCreator" />.
		/// </summary>
		/// <inheritdoc cref="ShearBlockCreator(Point, Pressure, double, double, ObjectId)" />
		public static ShearBlockCreator? From(Point insertionPoint, Pressure shearStress, double scaleFactor, double textHeight, ObjectId blockTableId) =>
			!shearStress.ApproxZero(StressState.Tolerance)
				? new ShearBlockCreator(insertionPoint, shearStress, scaleFactor, textHeight, blockTableId)
				: null;

		/// <summary>
		///     Get the attribute for shear block.
		/// </summary>
		/// <inheritdoc cref="ShearBlockCreator(Point, Pressure, double, double, ObjectId)" />
		private static AttributeReference GetAttribute(Pressure shearStress, double textHeight, Layer layer)
		{
			// Get shear stress
			var tau = shearStress.Value.Abs();

			// Create attribute
			return
				new AttributeReference
				{
					Position            = Point3d.Origin,
					TextString          = $"{tau:G4}",
					Height              = textHeight,
					Justify             = AttachmentPoint.MiddleCenter,
					Layer               = $"{layer}",
					LockPositionInBlock = true,
					Invisible           = false
				};
		}

		/// <summary>
		///     Get the rotation angle for shear block.
		/// </summary>
		/// <inheritdoc cref="ShearBlockCreator(Point, Pressure, double, double, ObjectId)" />
		private static double GetRotationAngle(Pressure shearStress) => shearStress >= Pressure.Zero
			? 0
			: Constants.Pi;

		#endregion

	}
}