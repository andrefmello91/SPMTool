using System.Collections.Generic;
using System.Linq;
using andrefmello91.Extensions;
using andrefmello91.OnPlaneComponents;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using MathNet.Numerics;
using SPMTool.Enums;
using SPMTool.Extensions;
using UnitsNet;
#nullable enable

namespace SPMTool.Core.Blocks
{
	/// <summary>
	///     Block creator class.
	/// </summary>
	public class StressBlockCreator : BlockCreator
	{
		private PrincipalStressState _stressState;

		/// <summary>
		///		Get/set the stress state.
		/// </summary>
		public PrincipalStressState StressState
		{
			get => _stressState;
			set
			{
				_stressState = value;
				
				// Update attribute
				Attributes = GetAttributes(value, ScaleFactor).ToArray();
			}
		}

		/// <summary>
		///     Block creator constructor.
		/// </summary>
		/// <param name="insertionPoint">The insertion <see cref="Point" /> of block.</param>
		/// <param name="stressState">The <see cref="PrincipalStressState"/>.</param>
		/// <param name="scaleFactor">The scale factor.</param>
		/// <inheritdoc />
		public StressBlockCreator(Point insertionPoint, PrincipalStressState stressState, double scaleFactor, Layer? layer = null)
			: base(insertionPoint, GetBlock(stressState), stressState.Theta1, scaleFactor, Axis.Z, layer)
		{
			_stressState = stressState;
			
			Attributes   = GetAttributes(stressState, scaleFactor).ToArray();
		}

		/// <summary>
		///		Get the correct block for <paramref name="stressState"/>
		/// </summary>
		private static Block GetBlock(PrincipalStressState stressState) =>
			stressState.Case switch
			{
				PrincipalCase.UniaxialTension     => Block.UniaxialTensileStress,
				PrincipalCase.PureTension         => Block.PureTensileStress,
				PrincipalCase.UniaxialCompression => Block.UniaxialCompressiveStress,
				PrincipalCase.PureCompression     => Block.PureCompressiveStress,
				_                                 => Block.CombinedStress,
			};

		/// <summary>
		///		Get the attribute for shear block.
		/// </summary>
		/// <inheritdoc cref="ShearBlockCreator(Point, Pressure, double)"/>
		private static IEnumerable<AttributeReference> GetAttributes(PrincipalStressState stressState, double scaleFactor)
		{
			if (stressState.IsZero)
				yield break;

			// Text for sigma 1
			if (!stressState.Is1Zero)
			{
				var sigma1 = stressState.Sigma1.ToUnit(DataBase.Settings.Units.PanelStresses).Value.Abs();
				var pt1    = GetTextInsertionPoint(stressState.Theta1, scaleFactor);
				var color1 = stressState.Sigma1.GetColorCode();

				yield return new AttributeReference
				{
					Position            = pt1.ToPoint3d(),
					TextString          = $"{sigma1:0.00}",
					Height              = 30 * scaleFactor,
					Color               = Color.FromColorIndex(ColorMethod.ByAci, (short) color1),
					Justify             = AttachmentPoint.MiddleLeft,
					LockPositionInBlock = true,
					Invisible           = false
				};
			}
			
			// Text for sigma 1
			if (stressState.Is2Zero)
				yield break;

			var sigma2 = stressState.Sigma2.ToUnit(DataBase.Settings.Units.PanelStresses).Value.Abs();
			var pt2    = GetTextInsertionPoint(stressState.Theta1 - Constants.PiOver2, scaleFactor);
			var color2 = stressState.Sigma2.GetColorCode();

			yield return new AttributeReference
			{
				Position            = pt2.ToPoint3d(),
				TextString          = $"{sigma2:0.00}",
				Height              = 30 * scaleFactor,
				Color               = Color.FromColorIndex(ColorMethod.ByAci, (short) color2),
				Justify             = AttachmentPoint.MiddleLeft,
				LockPositionInBlock = true,
				Invisible           = false
			};
		}

		/// <summary>
		///		Get insertion point for text.
		/// </summary>
		/// <param name="stressAngle">The angle of the stress.</param>
		/// <param name="scaleFactor"></param>
		/// <returns></returns>
		private static Point GetTextInsertionPoint(double stressAngle, double scaleFactor)
		{
			var (cos, sin) = stressAngle.DirectionCosines();

			return
				new Point(210 * cos * scaleFactor, 210 * sin * scaleFactor);
		}
	}
}