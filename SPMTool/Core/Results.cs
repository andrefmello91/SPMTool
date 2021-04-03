using System;
using System.Collections.Generic;
using System.Linq;
using andrefmello91.Extensions;
using andrefmello91.SPMElements;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using MathNet.Numerics;
using SPMTool.Enums;
using SPMTool.Extensions;
using UnitsNet;
using static SPMTool.Core.DataBase;
using static SPMTool.Core.Model;
using static SPMTool.Units;
using static SPMTool.Extensions.Extensions;

namespace SPMTool.Core
{
	/// <summary>
	///     Results drawing class.
	/// </summary>
	public static class Results
	{

		#region Fields

		/// <summary>
		///     Collection of result <see cref="Layer" />'s.
		/// </summary>
		public static readonly Layer[] ResultLayers = { Layer.StringerForce, Layer.PanelForce, Layer.PanelStress, Layer.ConcreteStress, Layer.Displacements, Layer.Cracks };

		#endregion

		#region Methods

		/// <summary>
		///     Draw panel cracks.
		/// </summary>
		/// <inheritdoc cref="DrawResults" />
		public static void DrawCracks(IEnumerable<Panel> panels)
		{
			var units = Settings.Units;

			// Start a transaction
			using var trans = StartTransaction();

			using var blkTbl = (BlockTable) trans.GetObject(DataBase.Database.BlockTableId, OpenMode.ForRead);

			// Read the object Id of the crack block
			var crackBlock = blkTbl[$"{Block.PanelCrack}"];

			foreach (var pnl in panels)
			{
				// Get the average crack opening
				var w = pnl.CrackOpening;

				if (w.ApproxZero(CrackTolerance))
					continue;

				// Get panel data
				var l      = pnl.Geometry.EdgeLengths;
				var cntrPt = pnl.Geometry.Vertices.CenterPoint.ToPoint3d();

				// Get the maximum length of the panel
				var lMax = l.Max().ToUnit(units.Geometry).Value;

				// Calculate the scale factor for the block and text
				var scFctr = 0.001 * lMax;

				// Add crack blocks
				AddCrackBlock();

				// Create crack block
				void AddCrackBlock()
				{
					// Get the cracking angle
					var theta2   = pnl.ConcretePrincipalStrains.Theta2;
					var crkAngle = theta2 <= Constants.PiOver2 ? theta2 : theta2 - Constants.Pi;

					// Insert the block into the current space
					using (var blkRef = new BlockReference(cntrPt, crackBlock))
					{
						blkRef.Layer = $"{Layer.Cracks}";

						// Set the scale of the block
						blkRef.TransformBy(Matrix3d.Scaling(scFctr, cntrPt));

						// Rotate
						if (!crkAngle.ApproxZero(1E-3))
							blkRef.TransformBy(Matrix3d.Rotation(crkAngle, Ucs.Zaxis, cntrPt));

						blkRef.AddToDrawing(null, trans);
					}

					// Create the texts
					using (var crkTxt = new DBText())
					{
						// Set the alignment point
						var algnPt = new Point3d(cntrPt.X, cntrPt.Y - 40 * scFctr, 0);

						// Set the parameters
						crkTxt.Layer          = $"{Layer.Cracks}";
						crkTxt.Height         = 30 * units.ScaleFactor;
						crkTxt.TextString     = $"{w.ToUnit(units.CrackOpenings).Value:0.00E+00}";
						crkTxt.Position       = algnPt;
						crkTxt.HorizontalMode = TextHorizontalMode.TextCenter;
						crkTxt.AlignmentPoint = algnPt;

						// Rotate text
						if (!crkAngle.ApproxZero(1E-3))
							crkTxt.TransformBy(Matrix3d.Rotation(crkAngle, Ucs.Zaxis, cntrPt));

						// Add the text to the drawing
						crkTxt.AddToDrawing(null, trans);
					}
				}
			}

			// Save the new objects to the database
			trans.Commit();
		}

		/// <summary>
		///     Draw cracks at the stringers.
		/// </summary>
		/// <inheritdoc cref="DrawResults" />
		public static void DrawCracks(IEnumerable<Stringer> stringers)
		{
			// Get units
			var units  = Settings.Units;
			var scFctr = units.ScaleFactor;

			// Start a transaction
			using (var trans = StartTransaction())
			using (var blkTbl = (BlockTable) trans.GetObject(DataBase.Database.BlockTableId, OpenMode.ForRead))
			{
				// Read the object Id of the crack block
				var crackBlock = blkTbl[$"{Block.StringerCrack}"];

				foreach (var str in stringers)
				{
					// Get the average crack opening
					var cracks = str.CrackOpenings;

					// Get length converted and angle
					var l = str.Geometry.Length.ToUnit(units.Geometry).Value;
					var a = str.Geometry.Angle;

					// Get insertion points
					var stPt = str.Geometry.InitialPoint.ToPoint3d();

					var points = new[]
					{
						new Point3d(stPt.X + 0.1 * l, 0, 0),
						new Point3d(stPt.X + 0.5 * l, 0, 0),
						new Point3d(stPt.X + 0.9 * l, 0, 0)
					};

					for (var i = 0; i < cracks.Length; i++)
					{
						if (cracks[i].ApproxZero(LengthTolerance))
							continue;

						// Add crack blocks
						AddCrackBlock(cracks[i].ToUnit(Settings.Units.CrackOpenings).Value, points[i]);
					}

					// Create crack block
					void AddCrackBlock(double w, Point3d position)
					{
						// Insert the block into the current space
						using (var blkRef = new BlockReference(position, crackBlock))
						{
							blkRef.Layer = $"{Layer.Cracks}";

							// Set the scale of the block
							blkRef.TransformBy(Matrix3d.Scaling(scFctr, position));

							// Rotate
							if (!a.ApproxZero(1E-3))
								blkRef.TransformBy(Matrix3d.Rotation(a, Ucs.Zaxis, stPt));

							blkRef.AddToDrawing(null, trans);
						}

						// Create the texts
						using (var crkTxt = new DBText())
						{
							// Set the alignment point
							var algnPt = new Point3d(position.X, position.Y - 100 * scFctr, 0);

							// Set the parameters
							crkTxt.Layer          = $"{Layer.Cracks}";
							crkTxt.Height         = 30 * scFctr;
							crkTxt.TextString     = $"{w.Abs():0.00E+00}";
							crkTxt.Position       = algnPt;
							crkTxt.HorizontalMode = TextHorizontalMode.TextCenter;
							crkTxt.AlignmentPoint = algnPt;

							// Rotate text
							if (!a.ApproxZero(1E-3))
								crkTxt.TransformBy(Matrix3d.Rotation(a, Ucs.Zaxis, stPt));

							// Add the text to the drawing
							crkTxt.AddToDrawing(null, trans);
						}
					}
				}

				// Save the new objects to the database
				trans.Commit();
			}
		}

		/// <summary>
		///     Draw stringer forces.
		/// </summary>
		/// <inheritdoc cref="DrawResults" />
		public static void DrawForces(IEnumerable<Stringer> stringers)
		{
			// Get units
			var units = Settings.Units;

			// Get the scale factor
			var scFctr = units.ScaleFactor;

			// Get maximum force
			var maxForce = stringers.Select(s => s.MaxForce.Abs()).Max();

			foreach (var stringer in stringers)
			{
				// Check if the stringer is loaded
				if (stringer.State is StringerForceState.Unloaded)
					continue;

				// Get the parameters of the Stringer
				double
					l   = stringer.Geometry.Length.ToUnit(units.Geometry).Value,
					ang = stringer.Geometry.Angle;

				// Get the start point
				var stPt = stringer.Geometry.InitialPoint.ToPoint3d();

				// Get normal forces
				var (N1, N3) = stringer.NormalForces;

				// Calculate the dimensions to draw the solid (the maximum dimension will be 150 mm)
				double
					h1 = (150 * N1 / maxForce).ConvertFromMillimeter(units.Geometry),
					h3 = (150 * N3 / maxForce).ConvertFromMillimeter(units.Geometry);

				// Check if load state is pure tension or compression
				if (stringer.State != StringerForceState.Combined)
					PureTensionOrCompression();

				else
					Combined();

				// Create the texts if forces are not zero
				AddTexts();

				void PureTensionOrCompression()
				{
					// Calculate the points (the solid will be rotated later)
					Point3d[] vrts =
					{
						stPt,
						new(stPt.X + l, stPt.Y, 0),
						new(stPt.X, stPt.Y + h1, 0),
						new(stPt.X + l, stPt.Y + h3, 0)
					};

					// Create the diagram as a solid with 4 segments (4 points)
					using (var dgrm = new Solid(vrts[0], vrts[1], vrts[2], vrts[3]))
					{
						// Set the layer and transparency
						dgrm.Layer        = $"{Layer.StringerForce}";
						dgrm.Transparency = 80.Transparency();

						// Set the color (blue to compression and red to tension)
						dgrm.ColorIndex = Math.Max(N1.Value, N3.Value) > 0 ? (short) ColorCode.Blue1 : (short) ColorCode.Red;

						// Rotate the diagram
						dgrm.TransformBy(Matrix3d.Rotation(ang, Ucs.Zaxis, stPt));

						// Add the diagram to the drawing
						dgrm.AddToDrawing();
					}
				}

				void Combined()
				{
					// Calculate the point where the Stringer force will be zero
					var x     = h1.Abs() * l / (h1.Abs() + h3.Abs());
					var invPt = new Point3d(stPt.X + x, stPt.Y, 0);

					// Calculate the points (the solid will be rotated later)
					Point3d[] vrts1 =
					{
						stPt,
						invPt,
						new(stPt.X, stPt.Y + h1, 0)
					};

					Point3d[] vrts3 =
					{
						invPt,
						new(stPt.X + l, stPt.Y, 0),
						new(stPt.X + l, stPt.Y + h3, 0)
					};

					// Create the diagrams as solids with 3 segments (3 points)
					using (var dgrm1 = new Solid(vrts1[0], vrts1[1], vrts1[2]))
					{
						// Set the layer and transparency
						dgrm1.Layer        = $"{Layer.StringerForce}";
						dgrm1.Transparency = 80.Transparency();

						// Set the color (blue to compression and red to tension)
						dgrm1.ColorIndex = N1.Value > 0 ? (short) ColorCode.Blue1 : (short) ColorCode.Red;

						// Rotate the diagram
						dgrm1.TransformBy(Matrix3d.Rotation(ang, Ucs.Zaxis, stPt));

						// Add the diagram to the drawing
						dgrm1.AddToDrawing();
					}

					using (var dgrm3 = new Solid(vrts3[0], vrts3[1], vrts3[2]))
					{
						// Set the layer and transparency
						dgrm3.Layer        = $"{Layer.StringerForce}";
						dgrm3.Transparency = 80.Transparency();

						// Set the color (blue to compression and red to tension)
						dgrm3.ColorIndex = N3.Value > 0 ? (short) ColorCode.Blue1 : (short) ColorCode.Red;

						// Rotate the diagram
						dgrm3.TransformBy(Matrix3d.Rotation(ang, Ucs.Zaxis, stPt));

						// Add the diagram to the drawing
						dgrm3.AddToDrawing();
					}
				}

				void AddTexts()
				{
					if (!N1.ApproxZero(ForceTolerance))
					{
						// Set the parameters
						// Set the color (blue to compression and red to tension) and position
						using var txt1 = new DBText
						{
							Layer = $"{Layer.StringerForce}",

							Height = 30 * scFctr,

							TextString = $"{N1.ToUnit(units.StringerForces).Value.Abs():0.00}",

							ColorIndex = N1.Value > 0
								? (short) ColorCode.Blue1
								: (short) ColorCode.Red,

							Position = N1.Value > 0
								? new Point3d(stPt.X + 10 * scFctr, stPt.Y + h1 + 20 * scFctr, 0)
								: new Point3d(stPt.X + 10 * scFctr, stPt.Y + h1 - 50 * scFctr, 0)
						};

						// Rotate the text
						txt1.TransformBy(Matrix3d.Rotation(ang, Ucs.Zaxis, stPt));

						// Add the text to the drawing
						txt1.AddToDrawing();
					}

					if (N3.ApproxZero(ForceTolerance))
						return;

					using var txt3 = new DBText
					{
						Layer = $"{Layer.StringerForce}",

						Height = 30 * scFctr,

						TextString = $"{N3.ToUnit(units.StringerForces).Value.Abs():0.00}",

						ColorIndex = N3.Value > 0
							? (short) ColorCode.Blue1
							: (short) ColorCode.Red,

						Position = N3.Value > 0
							? new Point3d(stPt.X + l - 10 * scFctr, stPt.Y + h3 + 20 * scFctr, 0)
							: new Point3d(stPt.X + l - 10 * scFctr, stPt.Y + h3 - 50 * scFctr, 0),

						HorizontalMode = TextHorizontalMode.TextRight
					};

					// Adjust the alignment
					txt3.AlignmentPoint = txt3.Position;

					// Rotate the text
					txt3.TransformBy(Matrix3d.Rotation(ang, Ucs.Zaxis, stPt));

					// Add the text to the drawing
					txt3.AddToDrawing();
				}
			}

			// Turn the layer on
			Layer.StringerForce.On();
		}

		/// <summary>
		///     Draw results of analysis.
		/// </summary>
		/// <param name="input">The <see cref="SPMInput" />.</param>
		/// <param name="drawCracks">Draw cracks after nonlinear analysis?</param>
		public static void DrawResults(SPMInput input, bool drawCracks)
		{
			// Erase result objects
			ResultLayers.EraseObjects();

			SetDisplacements();
			DrawPanelStresses();
			DrawDisplacedModel();

			// DrawForces(stringers);

			if (!drawCracks)
				return;

			DrawCracks(input.Panels);
			DrawCracks(input.Stringers);
		}

		/// <summary>
		///		Draw panel stresses.
		/// </summary>
		private static void DrawPanelStresses()
		{
			// Get panel blocks
			var blocks = Panels.SelectMany(p => p.GetBlocks()).ToList();

			// Add to drawing and set attributes
			blocks.AddToDrawing();
			blocks.SetAttributes();
			
			// Turn off stresses layer
			TurnOff(Layer.PanelStress, Layer.ConcreteStress);
			Layer.PanelForce.On();
		}

		/// <summary>
		///		Draw the displaced model.
		/// </summary>
		private static void DrawDisplacedModel()
		{
			var mFactor   = Settings.Units.DisplacementMagnifier;
			
			var displaced = Stringers.Select(s => s.GetDisplaced(mFactor)).ToList();

			var _ = displaced.AddToDrawing();

			// Turn off displacement layer
			Layer.Displacements.Off();
		}

		/// <summary>
		///     Set displacement to <see cref="Model.Nodes" />.
		/// </summary>
		/// <inheritdoc cref="DrawResults" />
		public static void SetDisplacements()
		{
			foreach (var node in Nodes)
				node.SetDisplacementFromNode();
		}

		#endregion

	}
}