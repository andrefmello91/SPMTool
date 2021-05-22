using System.Collections.Generic;
using System.Linq;
using andrefmello91.Extensions;
using andrefmello91.OnPlaneComponents;
using andrefmello91.SPMElements;
using Autodesk.AutoCAD.DatabaseServices;
using MathNet.Numerics;
using SPMTool.Enums;

using UnitsNet;
#nullable enable

namespace SPMTool.Core.Blocks
{
	/// <summary>
	///     Block creator class.
	/// </summary>
	public class StressBlockCreator : BlockCreator
	{

		#region Fields

		private PrincipalStressState _stressState;

		#endregion

		#region Properties

		/// <summary>
		///     Get/set the stress state.
		/// </summary>
		public PrincipalStressState StressState
		{
			get => _stressState;
			set
			{
				_stressState = value;

				// Update attribute
				Attributes = GetAttributes(value, ScaleFactor, TextHeight, Layer).ToArray();
			}
		}

		#endregion

		#region Constructors

		/// <summary>
		///     Block creator constructor.
		/// </summary>
		/// <param name="stressState">The <see cref="PrincipalStressState" />.</param>
		/// <inheritdoc />
		private StressBlockCreator(Point insertionPoint, PrincipalStressState stressState, double scaleFactor, double textHeight, ObjectId blockTableId, Layer? layer = null)
			: base(insertionPoint, GetBlock(stressState), stressState.Theta1, scaleFactor, textHeight, blockTableId, Axis.Z, layer)
		{
			_stressState = stressState;

			Attributes = GetAttributes(stressState, scaleFactor, textHeight, Layer).ToArray();
		}

		#endregion

		#region Methods

		/// <summary>
		///     Get the average stress <see cref="BlockCreator" />.
		/// </summary>
		/// <inheritdoc cref="ShearBlockCreator(Point, Pressure, double, double, ObjectId)" />
		public static StressBlockCreator? From(Point insertionPoint, PrincipalStressState stressState, double scaleFactor, double textHeight, ObjectId blockTableId, Layer? layer = null) =>
			!stressState.IsZero
				? new StressBlockCreator(insertionPoint,stressState, scaleFactor, textHeight, blockTableId, layer)
				: null;

		/// <summary>
		///     Improve the angle.
		/// </summary>
		public static double ImproveAngle(double angle) => angle > Constants.PiOver2
			? angle - Constants.Pi
			: angle;

		/// <summary>
		///     Get the attribute for shear block.
		/// </summary>
		/// <inheritdoc cref="ShearBlockCreator(Point, Pressure, double, double, ObjectId)" />
		private static IEnumerable<AttributeReference> GetAttributes(PrincipalStressState stressState, double scaleFactor, double textHeight, Layer layer)
		{
			if (stressState.IsZero)
				yield break;

			// Text for sigma 1
			if (!stressState.Is1Zero)
			{
				var sigma1 = stressState.Sigma1.Value.Abs();

				// Improve angle
				var angle1 = ImproveAngle(stressState.Theta1);
				var pt1    = GetTextInsertionPoint(angle1, scaleFactor);
				var color1 = stressState.Sigma1.GetColorCode();

				yield return new AttributeReference
				{
					Position            = pt1.ToPoint3d(),
					TextString          = $"{sigma1:0.00}",
					Height              = textHeight,
					Layer               = $"{layer}",
					ColorIndex          = (short) color1,
					Justify             = AttachmentPoint.MiddleLeft,
					LockPositionInBlock = true,
					Invisible           = false
				};
			}

			// Text for sigma 1
			if (stressState.Is2Zero)
				yield break;

			var sigma2 = stressState.Sigma2.Value.Abs();

			// Improve angle
			var angle2 = ImproveAngle(stressState.Theta2);
			var pt2    = GetTextInsertionPoint(angle2, scaleFactor);
			var color2 = stressState.Sigma2.GetColorCode();

			yield return new AttributeReference
			{
				Position            = pt2.ToPoint3d(),
				TextString          = $"{sigma2:0.00}",
				Height              = textHeight,
				Layer               = $"{layer}",
				ColorIndex          = (short) color2,
				Justify             = AttachmentPoint.MiddleLeft,
				LockPositionInBlock = true,
				Invisible           = false
			};
		}

		/// <summary>
		///     Get the correct block for <paramref name="stressState" />
		/// </summary>
		private static Block GetBlock(PrincipalStressState stressState) =>
			stressState.Case switch
			{
				PrincipalCase.UniaxialTension     => Block.UniaxialTensileStress,
				PrincipalCase.PureTension         => Block.PureTensileStress,
				PrincipalCase.UniaxialCompression => Block.UniaxialCompressiveStress,
				PrincipalCase.PureCompression     => Block.PureCompressiveStress,
				_                                 => Block.CombinedStress
			};

		/// <summary>
		///     Get insertion point for text.
		/// </summary>
		/// <param name="stressAngle">The angle of the stress.</param>
		/// <param name="scaleFactor"></param>
		private static Point GetTextInsertionPoint(double stressAngle, double scaleFactor) => new Point(210 * scaleFactor, 0).Rotate(stressAngle);

		// {
		// 	var (cos, sin) = stressAngle.DirectionCosines();
		//
		// 	return
		// 		new Point(210 * cos * scaleFactor, 210 * sin * scaleFactor);
		// }

		#endregion

	}
}