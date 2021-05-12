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
				Attributes = new[] { GetAttribute(value, ScaleFactor, Layer) };
			}
		}

		#endregion

		#region Constructors

		/// <summary>
		///     Block creator constructor.
		/// </summary>
		/// <param name="insertionPoint">The insertion <see cref="Point" /> of block.</param>
		/// <param name="shearStress">The shear stress.</param>
		/// <param name="scaleFactor">The scale factor.</param>
		private ShearBlockCreator(Point insertionPoint, Pressure shearStress, double scaleFactor)
			: base(insertionPoint, Block.Shear, GetRotationAngle(shearStress), scaleFactor, Axis.Y)
		{
			_shearStress = shearStress;

			Attributes = new[] { GetAttribute(shearStress, scaleFactor, Layer) };
		}

		#endregion

		#region Methods

		/// <summary>
		///     Get the shear <see cref="BlockCreator" />.
		/// </summary>
		/// <param name="panel">The <see cref="Panel" />.</param>
		public static ShearBlockCreator? CreateBlock(Panel? panel) =>
			panel is null || panel.AverageStresses.IsXYZero
				? null
				: new ShearBlockCreator(panel.Geometry.Vertices.CenterPoint, panel.AverageStresses.TauXY, Results.ResultScaleFactor);

		/// <summary>
		///     Get the attribute for shear block.
		/// </summary>
		/// <inheritdoc cref="ShearBlockCreator(Point, Pressure, double)" />
		private static AttributeReference GetAttribute(Pressure shearStress, double scaleFactor, Layer layer)
		{
			// Get shear stress
			var tau = shearStress.ToUnit(DataBase.Settings.Units.PanelStresses).Value.Abs();

			// Create attribute
			return
				new AttributeReference
				{
					Position            = Point3d.Origin,
					TextString          = $"{tau:0.00}",
					Height              = Results.TextHeight,
					Justify             = AttachmentPoint.MiddleCenter,
					Layer               = $"{layer}",
					LockPositionInBlock = true,
					Invisible           = false
				};
		}

		/// <summary>
		///     Get the rotation angle for shear block.
		/// </summary>
		/// <inheritdoc cref="ShearBlockCreator(Point, Pressure, double)" />
		private static double GetRotationAngle(Pressure shearStress) => shearStress >= Pressure.Zero
			? 0
			: Constants.Pi;

		#endregion

	}
}