using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Extensions;
using MathNet.Numerics;
using andrefmello91.SPMElements;
using Autodesk.AutoCAD.Windows;
using SPMTool.Enums;
using SPMTool.Extensions;
using UnitsNet;
using static SPMTool.Core.DataBase;
using static SPMTool.Core.Model;
using static SPMTool.Units;

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
		public static readonly Layer[] ResultLayers = { Layer.StringerForce, Layer.PanelForce, Layer.CompressivePanelStress, Layer.TensilePanelStress, Layer.ConcreteCompressiveStress, Layer.ConcreteTensileStress, Layer.Displacements, Layer.Cracks};

		#endregion

		#region  Methods

		/// <summary>
		///     Draw panel stresses.
		/// </summary>
		/// <param name="panels">The collection of <see cref="Panel" />'s.</param>
		public static void DrawStresses(IEnumerable<Panel> panels)
		{
			// Get units
			var units = Settings.Units;

			// Get tolerances
			var sTol = StressTolerance;
			var tol = sTol.ToUnit(units.PanelStresses).Value;

			// Read the object Ids of the support blocks
			using (var trans = StartTransaction())
			using (var blkTbl = (BlockTable) trans.GetObject(DataBase.Database.BlockTableId, OpenMode.ForRead))
			{
				// Read the object Ids of the support blocks
				ObjectId
					shearBlock = blkTbl[$"{Block.Shear}"],
					compStress = blkTbl[$"{Block.CompressiveStress}"],
					tensStress = blkTbl[$"{Block.TensileStress}"];

				foreach (var pnl in panels)
				{
					// Get panel data
					var l      = pnl.Geometry.EdgeLengths;
					var cntrPt = pnl.Geometry.Vertices.CenterPoint.ToPoint3d();

					// Get the maximum length of the panel
					var lMax = l.Max().ToUnit(units.Geometry).Value;

					// Get the average stress
					var tauAvg = pnl.AverageStresses.TauXY.ToUnit(units.PanelStresses).Value;

					// Calculate the scale factor for the block and text
					var scFctr = 0.001 * lMax;

					// Add shear block
					AddShearBlock();

					// Add average stresses blocks
					var stresses = pnl.AveragePrincipalStresses;
					AddCompressiveBlock(Layer.CompressivePanelStress);
					AddTensileBlock(Layer.TensilePanelStress);

					// Add concrete stresses blocks
					stresses = pnl.ConcretePrincipalStresses;
					AddCompressiveBlock(Layer.ConcreteCompressiveStress);
					AddTensileBlock(Layer.ConcreteTensileStress);

					// Create shear block
					void AddShearBlock()
					{
						if (tauAvg.ApproxZero())
							return;

						// Insert the block into the current space
						using (var blkRef = new BlockReference(cntrPt, shearBlock))
						{
							blkRef.Layer = $"{Layer.PanelForce}";

							// Set the scale of the block
							blkRef.TransformBy(Matrix3d.Scaling(scFctr, cntrPt));

							// If the shear is negative, mirror the block
							if (tauAvg < 0) blkRef.TransformBy(Matrix3d.Rotation(Constants.Pi, Ucs.Yaxis, cntrPt));

							blkRef.AddToDrawing(null, trans);
						}

						// Create the texts
						using (var tauTxt = new DBText())
						{
							// Set the alignment point
							var algnPt = new Point3d(cntrPt.X, cntrPt.Y, 0);

							// Set the parameters
							tauTxt.Layer = $"{Layer.PanelForce}";
							tauTxt.Height = 30 * scFctr;
							tauTxt.TextString = $"{Math.Abs(tauAvg):0.00}";
							tauTxt.Position = algnPt;
							tauTxt.HorizontalMode = TextHorizontalMode.TextCenter;
							tauTxt.AlignmentPoint = algnPt;

							// Add the text to the drawing
							tauTxt.AddToDrawing(null, trans);
						}
					}

					// Create compressive stress block
					void AddCompressiveBlock(Layer layer)
					{
						var sig2 = stresses.Sigma2.ToUnit(units.PanelStresses).Value;

						if (sig2.ApproxZero(tol))
							return;

						// Create compressive stress block
						using (var blkRef = new BlockReference(cntrPt, compStress))
						{
							blkRef.Layer = $"{layer}";
							blkRef.ColorIndex = (int) ColorCode.Blue1;

							// Set the scale of the block
							blkRef.TransformBy(Matrix3d.Scaling(scFctr, cntrPt));

							// Rotate the block in theta angle
							if (!stresses.Theta2.ApproxZero())
								blkRef.TransformBy(Matrix3d.Rotation(stresses.Theta2, Ucs.Zaxis,
									cntrPt));

							blkRef.AddToDrawing(null, trans);
						}

						// Create the text
						using (var sigTxt = new DBText())
						{
							// Create a line and rotate to get insertion point
							var ln = new Line
							{
								StartPoint = cntrPt,
								EndPoint = new Point3d(cntrPt.X + 210 * scFctr, cntrPt.Y, 0)
							};

							ln.TransformBy(Matrix3d.Rotation(stresses.Theta2, Ucs.Zaxis, cntrPt));

							// Set the alignment point
							var algnPt = ln.EndPoint;

							// Set the parameters
							sigTxt.Layer = $"{layer}";
							sigTxt.Height = 30 * scFctr;
							sigTxt.TextString = $"{sig2.Abs():0.00}";
							sigTxt.Position = algnPt;
							sigTxt.HorizontalMode = TextHorizontalMode.TextCenter;
							sigTxt.AlignmentPoint = algnPt;

							// Add the text to the drawing
							sigTxt.AddToDrawing(null, trans);
						}
					}

					// Create tensile stress block
					void AddTensileBlock(Layer layer)
					{
						var sig1 = stresses.Sigma1.ToUnit(units.PanelStresses).Value;

						// Verify tensile stress
						if (sig1.ApproxZero(tol))
							return;

						// Create tensile stress block
						using (var blkRef = new BlockReference(cntrPt, tensStress))
						{
							blkRef.Layer = $"{layer}";

							// Set the scale of the block
							blkRef.TransformBy(Matrix3d.Scaling(scFctr, cntrPt));

							// Rotate the block in theta angle
							if (!stresses.Theta2.ApproxZero())
								blkRef.TransformBy(Matrix3d.Rotation(stresses.Theta2, Ucs.Zaxis,
									cntrPt));

							blkRef.AddToDrawing(null, trans);
						}

						// Create the text
						using (var sigTxt = new DBText())
						{
							// Create a line and rotate to get insertion point
							var ln = new Line
							{
								StartPoint = cntrPt,
								EndPoint = new Point3d(cntrPt.X, cntrPt.Y + 210 * scFctr, 0)
							};

							ln.TransformBy(Matrix3d.Rotation(stresses.Theta2, Ucs.Zaxis, cntrPt));

							// Set the alignment point
							var algnPt = ln.EndPoint;

							// Set the parameters
							sigTxt.Layer = $"{layer}";
							sigTxt.Height = 30 * scFctr;
							sigTxt.TextString = $"{sig1.Abs():0.00}";
							sigTxt.Position = algnPt;
							sigTxt.HorizontalMode = TextHorizontalMode.TextCenter;
							sigTxt.AlignmentPoint = algnPt;

							// Add the text to the drawing
							sigTxt.AddToDrawing(null, trans);
						}
					}
				}

				// Save the new objects to the database
				trans.Commit();
			}

			// Turn the layer on
			Layer.PanelForce.On();
			Layer.CompressivePanelStress.Off();
			Layer.TensilePanelStress.Off();
			Layer.ConcreteCompressiveStress.Off();
			Layer.ConcreteTensileStress.Off();
			Layer.Cracks.Off();
		}

		/// <summary>
		///     Draw panel cracks.
		/// </summary>
		/// <param name="panels">The collection of <see cref="Panel" />'s.</param>
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
						crkTxt.Layer = $"{Layer.Cracks}";
						crkTxt.Height = 30 * units.ScaleFactor;
						crkTxt.TextString = $"{w.ToUnit(units.CrackOpenings).Value:0.00E+00}";
						crkTxt.Position = algnPt;
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
		///     Draw stringer forces.
		/// </summary>
		/// <param name="stringers">The collection of <see cref="Stringer" />'s.</param>
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
						new Point3d(stPt.X + l,      stPt.Y, 0),
						new Point3d(    stPt.X, stPt.Y + h1, 0),
						new Point3d(stPt.X + l, stPt.Y + h3, 0)
					};

					// Create the diagram as a solid with 4 segments (4 points)
					using (var dgrm = new Solid(vrts[0], vrts[1], vrts[2], vrts[3]))
					{
						// Set the layer and transparency
						dgrm.Layer = $"{Layer.StringerForce}";
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
					var x = h1.Abs() * l / (h1.Abs() + h3.Abs());
					var invPt = new Point3d(stPt.X + x, stPt.Y, 0);

					// Calculate the points (the solid will be rotated later)
					Point3d[] vrts1 =
					{
						stPt,
						invPt,
						new Point3d(stPt.X, stPt.Y + h1, 0)
					};

					Point3d[] vrts3 =
					{
						invPt,
						new Point3d(stPt.X + l, stPt.Y,      0),
						new Point3d(stPt.X + l, stPt.Y + h3, 0)
					};

					// Create the diagrams as solids with 3 segments (3 points)
					using (var dgrm1 = new Solid(vrts1[0], vrts1[1], vrts1[2]))
					{
						// Set the layer and transparency
						dgrm1.Layer = $"{Layer.StringerForce}";
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
						dgrm3.Layer = $"{Layer.StringerForce}";
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
		///     Draw cracks at the stringers.
		/// </summary>
		/// <param name="stringers">The collection of stringers in the model.</param>
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
							crkTxt.Layer = $"{Layer.Cracks}";
							crkTxt.Height = 30 * scFctr;
							crkTxt.TextString = $"{w.Abs():0.00E+00}";
							crkTxt.Position = algnPt;
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
		///     Draw results of analysis.
		/// </summary>
		/// <param name="stringers">The collection of <see cref="Stringer"/>'s in the model.</param>
		/// <param name="panels">The collection of <see cref="Panel"/>'s in the model.</param>
		/// <param name="drawCracks">Draw cracks after nonlinear analysis?</param>
		public static void DrawResults(IEnumerable<Stringer> stringers, IEnumerable<Panel> panels, bool drawCracks)
		{
			// Erase result objects
			ResultLayers.EraseObjects();

			//Nodes.SetDisplacements(analysis.Nodes);
			DrawDisplacements(stringers);
			DrawForces(stringers);
			DrawStresses(panels);

			if (!drawCracks)
				return;

			DrawCracks(panels);
			DrawCracks(stringers);
		}

		/// <summary>
		///     Draw displacements.
		/// </summary>
		/// <param name="stringers">The collection of <see cref="Stringer" />'s.</param>
		public static void DrawDisplacements(IEnumerable<Stringer> stringers)
		{
			// Get units
			var units = Settings.Units;

			// Turn the layer off
			Layer.Displacements.Off();

			// Set a scale factor for displacements
			var scFctr = units.DisplacementScaleFactor;

			// Create lists of points for adding the nodes later
			var dispNds = new List<Point3d>();

			foreach (var str in stringers)
			{
				// Get displacements of the initial and end nodes
				var d1 = str.Grip1.Displacement.Clone();
				var d3 = str.Grip3.Displacement.Clone();
				d1.ChangeUnit(units.Displacements);
				d3.ChangeUnit(units.Displacements);

				double
					ux1 = d1.X.Value * scFctr,
					uy1 = d1.X.Value * scFctr,
					ux3 = d3.X.Value * scFctr,
					uy3 = d3.X.Value * scFctr,
					ix  = str.Geometry.InitialPoint.X.ToUnit(units.Geometry).Value,
					iy  = str.Geometry.InitialPoint.Y.ToUnit(units.Geometry).Value,
					ex  = str.Geometry.EndPoint.X.ToUnit(units.Geometry).Value,
					ey  = str.Geometry.EndPoint.Y.ToUnit(units.Geometry).Value;

				// Calculate the displaced nodes
				Point3d
					stPt = new Point3d(ix + ux1, iy + uy1, 0),
					enPt = new Point3d(ex + ux3, ey + uy3, 0),
					midPt = stPt.MidPoint(enPt);

				// Draw the displaced Stringer
				using (var newStr = new Line(stPt, enPt))
				{
					// Set the layer to Stringer
					newStr.Layer = $"{Layer.Displacements}";

					// Add the line to the drawing
					newStr.AddToDrawing();
				}

				// Add the position of the nodes to the list
				if (!dispNds.Contains(stPt))
					dispNds.Add(stPt);

				if (!dispNds.Contains(enPt))
					dispNds.Add(enPt);

				if (!dispNds.Contains(midPt))
					dispNds.Add(midPt);
			}

			// Add the nodes
			Nodes.AddRange(dispNds.ToPoints(units.Geometry), NodeType.Displaced, false, false);
		}

		#endregion
	}
}