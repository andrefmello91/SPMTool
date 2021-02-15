using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Extensions;
using MathNet.Numerics;
using OnPlaneComponents;
using SPM.Elements;
using SPM.Elements.PanelProperties;
using SPMTool.Enums;
using SPMTool.Extensions;
using UnitsNet;
using static SPMTool.Core.DataBase;
using static SPMTool.Units;

#nullable enable

namespace SPMTool.Core.Elements
{
	/// <summary>
	///     Panels class.
	/// </summary>
	public class PanelList : SPMObjectList<PanelObject, PanelGeometry, Panel>
	{
		#region Constructors

		private PanelList()
			: base()
        {
		}

		private PanelList(IEnumerable<PanelObject> panelObjects)
			: base(panelObjects)
		{
		}

		#endregion

		#region  Methods

		/// <summary>
		///     Get the elements of the compressive block.
		/// </summary>
		public static IEnumerable<Entity> CompressiveBlockElements()
		{
			// Get vertices of the solid
			Point3d[] verts1 =
			{
				new Point3d(-50, -50, 0),
				new Point3d( 50, -50, 0),
				new Point3d(-50,  50, 0),
				new Point3d( 50,  50, 0)
			};

			// Create a solid
			yield return new Solid(verts1[0], verts1[1], verts1[2], verts1[3]);

			// Create two arrows for compressive stress
			// Create lines
			yield return new Line
			{
				StartPoint = new Point3d(-175, 0, 0),
				EndPoint   = new Point3d(-87.5, 0, 0)
			};

			yield return new Line
			{
				StartPoint = new Point3d(87.5, 0, 0),
				EndPoint   = new Point3d(175, 0, 0)
			};

			// Get vertices of the solids
			Point3d[] verts2 =
			{
				new Point3d(-87.5, -25, 0),
				new Point3d(-87.5,  25, 0),
				new Point3d(  -50,   0, 0)
			};

			Point3d[] verts3 =
			{
				new Point3d(  50,   0, 0),
				new Point3d(87.5, -25, 0),
				new Point3d(87.5,  25, 0)
			};


			// Create the arrow solids and add to the collection
			yield return new Solid(verts2[0], verts2[1], verts2[2]);
			yield return new Solid(verts3[0], verts3[1], verts3[2]);
		}

		/// <summary>
		///     Get the elements of the crack block.
		/// </summary>
		public static IEnumerable<Entity> CrackBlockElements()
		{
			// Define the points to add the lines
			var crkPts = CrackPoints();

			List<Point3d> CrackPoints()
			{
				var pts = new List<Point3d>();

				for (var i = 0; i < 6; i++)
				{
					// Set the start X coordinate
					double x = 60 * i;

					pts.Add(new Point3d(x, 0, 0));
					pts.Add(new Point3d(x + 15, 3.5265, 0));
					pts.Add(new Point3d(x + 45, -3.5265, 0));
				}

				// Add the end point
				pts.Add(new Point3d(360, 0, 0));

				return pts;
			}

			// Define the lines and add to the collection
			for (var i = 0; i < crkPts.Count - 1; i++)
				yield return new Line
				{
					StartPoint = crkPts[i],
					EndPoint   = crkPts[i + 1],
					LineWeight = LineWeight.LineWeight035
				};
		}

		/// <summary>
		///     Get the elements of the shear block.
		/// </summary>
		public static IEnumerable<Entity> ShearBlockElements()
		{
			// Define the points to add the lines
			Point3d[] blkPts =
			{
				new Point3d(-140, -230, 0),
				new Point3d(-175, -200, 0),
				new Point3d( 175, -200, 0),
				new Point3d(-230, -140, 0),
				new Point3d(-200, -175, 0),
				new Point3d(-200,  175, 0),
				new Point3d( 140,  230, 0),
				new Point3d( 175,  200, 0),
				new Point3d(-175,  200, 0),
				new Point3d( 230,  140, 0),
				new Point3d( 200,  175, 0),
				new Point3d( 200, -175, 0)
			};

			// Define the lines and add to the collection
			for (var i = 0; i < 4; i++)
			{
				yield return new Line
				{
					StartPoint = blkPts[3 * i],
					EndPoint   = blkPts[3 * i + 1]
				};

				yield return new Line
				{
					StartPoint = blkPts[3 * i + 1],
					EndPoint   = blkPts[3 * i + 2]
				};
			}
		}

		/// <summary>
		///     Get the elements of the tensile block.
		/// </summary>
		public static IEnumerable<Entity> TensileBlockElements()
		{
			// Create two arrows for tensile stress
			// Create lines
			yield return new Line
			{
				StartPoint = new Point3d(0, 50, 0),
				EndPoint = new Point3d(0, 137.5, 0)
			};

			yield return new Line
			{
				StartPoint = new Point3d(0, -50, 0),
				EndPoint = new Point3d(0, -137.5, 0)
			};

			// Get vertices of the solids
			Point3d[] verts2 =
			{
				new Point3d(-25, 137.5, 0),
				new Point3d(  0,   175, 0),
				new Point3d( 25, 137.5, 0)
			};

			Point3d[] verts3 =
			{
				new Point3d(-25, -137.5, 0),
				new Point3d(  0,   -175, 0),
				new Point3d( 25, -137.5, 0)
			};


			// Create the arrow solids and add to the collection
			yield return new Solid(verts2[0], verts2[1], verts2[2]);
			yield return new Solid(verts3[0], verts3[1], verts3[2]);
		}

		/// <summary>
		///     Get the collection of panels in the drawing.
		/// </summary>
		public static IEnumerable<Solid>? GetObjects() => Layer.Panel.GetDBObjects()?.ToSolids();

		/// <summary>
		///     Read all the <see cref="PanelObject" />'s in the drawing.
		/// </summary>
		[return: NotNull]
		public static PanelList ReadFromDrawing() => ReadFromSolids(GetObjects());

		/// <summary>
		///     Read <see cref="PanelObject" />'s from a collection of <see cref="Solid" />'s.
		/// </summary>
		/// <param name="panelSolids">The collection containing the <see cref="Solid" />'s of drawing.</param>
		[return: NotNull]
		public static PanelList ReadFromSolids(IEnumerable<Solid>? panelSolids) =>
			panelSolids.IsNullOrEmpty()
				? new PanelList()
				: new PanelList(panelSolids.Select(PanelObject.ReadFromSolid));

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
			using (var trans = DataBase.StartTransaction())
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
							if (tauAvg < 0) blkRef.TransformBy(Matrix3d.Rotation(Constants.Pi, DataBase.Ucs.Yaxis, cntrPt));

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
							blkRef.ColorIndex = (int) Color.Blue1;

							// Set the scale of the block
							blkRef.TransformBy(Matrix3d.Scaling(scFctr, cntrPt));

							// Rotate the block in theta angle
							if (!stresses.Theta2.ApproxZero())
								blkRef.TransformBy(Matrix3d.Rotation(stresses.Theta2, DataBase.Ucs.Zaxis,
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

							ln.TransformBy(Matrix3d.Rotation(stresses.Theta2, DataBase.Ucs.Zaxis, cntrPt));

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
								blkRef.TransformBy(Matrix3d.Rotation(stresses.Theta2, DataBase.Ucs.Zaxis,
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

							ln.TransformBy(Matrix3d.Rotation(stresses.Theta2, DataBase.Ucs.Zaxis, cntrPt));

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
			using (var trans = DataBase.StartTransaction())
			using (var blkTbl = (BlockTable) trans.GetObject(DataBase.Database.BlockTableId, OpenMode.ForRead))
			{
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
								blkRef.TransformBy(Matrix3d.Rotation(crkAngle, DataBase.Ucs.Zaxis, cntrPt));

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
								crkTxt.TransformBy(Matrix3d.Rotation(crkAngle, DataBase.Ucs.Zaxis, cntrPt));

							// Add the text to the drawing
							crkTxt.AddToDrawing(null, trans);
						}
					}
				}

				// Save the new objects to the database
				trans.Commit();
			}
		}

		/// <inheritdoc cref="EList{T}.Remove(T, bool, bool)" />
		/// <param name="vertices">The <see cref="Vertices" /> of panel to remove.</param>
		public bool Remove(Vertices vertices, bool raiseEvents = true, bool sort = true) => Remove(new PanelObject(vertices), raiseEvents, sort);

		/// <inheritdoc cref="EList{T}.RemoveRange(IEnumerable{T}, bool, bool)" />
		/// <param name="vertices">The collection of <see cref="Vertices" /> of panels to remove.</param>
		public int RemoveRange(IEnumerable<Vertices>? vertices, bool raiseEvents = true, bool sort = true) => RemoveRange(vertices?.Select(v => new PanelObject(v)), raiseEvents, sort);

		/// <summary>
		///     Get a <see cref="PanelObject" /> in this collection with the corresponding <paramref name="vertices" />.
		/// </summary>
		/// <param name="vertices">The <see cref="Vertices" /> required.</param>
		public PanelObject? GetByVertices(Vertices vertices) => Find(p => p.Vertices == vertices);

		/// <summary>
		///     Get a collection of <see cref="PanelObject" />'s in this collection with the corresponding
		///     <paramref name="vertices" />.
		/// </summary>
		/// <param name="vertices">The collection of <see cref="Vertices" /> required.</param>
		public IEnumerable<PanelObject>? GetByVertices(IEnumerable<Vertices>? vertices) => this.Where(p => vertices.Contains(p.Vertices));

		/// <summary>
		///     Update all the panels in this collection from drawing.
		/// </summary>
		public void Update()
		{
			Clear(false);

			AddRange(ReadFromDrawing(), false);
		}

		/// <summary>
		///     Get the list of <see cref="PanelGeometry" />'s from this collection.
		/// </summary>
		public List<PanelGeometry> GetGeometries() => GetProperties();

		/// <summary>
		///     Get the list of <see cref="Vertices" />'s from this collection.
		/// </summary>
		public List<Vertices> GetVertices() => GetGeometries().Select(g => g.Vertices).ToList();

		/// <summary>
		///     Get the list of distinct widths from this collection.
		/// </summary>
		public List<Length> GetWidths() => GetGeometries().Select(g => g.Width).Distinct().ToList();

		/// <inheritdoc cref="EList{T}.Add(T, bool, bool)" />
		/// <param name="vertices">The collection of four <see cref="Point" /> vertices, in any order.</param>
		public bool Add(IEnumerable<Point>? vertices, bool raiseEvents = true, bool sort = true) => !(vertices is null) && Add(new PanelObject(vertices), raiseEvents, sort);

		/// <inheritdoc cref="EList{T}.Add(T, bool, bool)" />
		/// <param name="vertices">The panel <see cref="Vertices" /> object.</param>
		public bool Add(Vertices vertices, bool raiseEvents = true, bool sort = true) => Add(new PanelObject(vertices), raiseEvents, sort);

		/// <inheritdoc cref="EList{T}.AddRange(IEnumerable{T}, bool, bool)" />
		/// <param name="verticesCollection">The collection of <see cref="Vertices" />'s that represents the panels.</param>
		public int AddRange(IEnumerable<Vertices>? verticesCollection, bool raiseEvents = true, bool sort = true) => AddRange(verticesCollection?.Select(v => new PanelObject(v)), raiseEvents, sort);

		#endregion
	}
}