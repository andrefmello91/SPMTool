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
		/// <inheritdoc />
		private PanelCrackBlockCreator(Point insertionPoint, Length crackOpening, double rotationAngle, double scaleFactor)
			: base(insertionPoint, Block.PanelCrack, rotationAngle, scaleFactor)
		{
			_crackOpening = crackOpening;

			Attributes = new[] { GetAttribute(crackOpening, rotationAngle, scaleFactor) };
		}

		#endregion

		#region Methods

		/// <summary>
		///     Get the average stress <see cref="BlockCreator" />.
		/// </summary>
		/// <param name="panel">The <see cref="Panel" />.</param>
		public static PanelCrackBlockCreator? CreateBlock(Panel? panel) =>
			panel?.Model is ElementModel.Nonlinear && panel.CrackOpening > Length.Zero
				? new PanelCrackBlockCreator(panel.Geometry.Vertices.CenterPoint, panel.CrackOpening, StressBlockCreator.ImproveAngle(panel.ConcretePrincipalStresses.Theta2), Results.ResultScaleFactor)
				: null;

		/// <summary>
		///     Get the attribute for crack block.
		/// </summary>
		/// <inheritdoc cref="PanelCrackBlockCreator(Point, Length, double, double)" />
		private static AttributeReference GetAttribute(Length crackOpening, double rotationAngle, double scaleFactor)
		{
			var w = crackOpening.ToUnit(DataBase.Settings.Units.CrackOpenings).Value.Abs();

			// Set the insertion point
			var pt = new Point(0, -40 * scaleFactor);

			var attRef = new AttributeReference
			{
				Position            = pt.ToPoint3d(),
				TextString          = $"{w:0.00E+00}",
				Height              = 30 * scaleFactor,
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