using System;
using System.Linq;
using System.Collections.Generic;
using System.Windows.Controls;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Extensions.AutoCAD;
using Extensions.Number;
using Material.Concrete;
using Material.Reinforcement;
using Material.Reinforcement.Biaxial;
using MathNet.Numerics;
using SPM.Elements;
using SPM.Elements.PanelProperties;
using SPMTool.Enums;
using UnitsNet;
using UnitsNet.Units;
using Panel = SPM.Elements.Panel;

namespace SPMTool.Database.Elements
{
	/// <summary>
    /// Panels class.
    /// </summary>
	public static class Panels
	{
		/// <summary>
		/// Auxiliary list of <see cref="Vertices"/>'s.
		/// </summary>
		public static List<Vertices> VerticesList { get; private set; } = GetPanelVertices();

		/// <summary>
		/// Get the elements of the shear block.
		/// </summary>
		public static IEnumerable<Entity> ShearBlockElements 
		{
			get
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
					new Point3d( 200, -175, 0),
				};

				// Define the lines and add to the collection
				for (int i = 0; i < 4; i++)
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
		}

		/// <summary>
		/// Get the elements of the compressive block.
		/// </summary>
		public static IEnumerable<Entity> CompressiveBlockElements 
		{
			get
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
		}

		/// <summary>
		/// Get the geometry unit.
		/// </summary>
		private static LengthUnit GeometryUnit => SettingsData.SavedUnits.Geometry;

		/// <summary>
		/// Get the elements of the tensile block.
		/// </summary>
		public static IEnumerable<Entity> TensileBlockElements 
		{
			get
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
					new Point3d( 25, 137.5, 0),
				};

				Point3d[] verts3 =
				{
					new Point3d(-25, -137.5, 0),
					new Point3d(  0,   -175, 0),
					new Point3d( 25, -137.5, 0),
				};


				// Create the arrow solids and add to the collection
				yield return new Solid(verts2[0], verts2[1], verts2[2]);
				yield return new Solid(verts3[0], verts3[1], verts3[2]);
			}
		}

		/// <summary>
		/// Get the elements of the crack block.
		/// </summary>
		public static IEnumerable<Entity> CrackBlockElements
		{
			get
			{
				// Define the points to add the lines
				var crkPts = CrackPoints();
				List<Point3d> CrackPoints()
				{
					var pts = new List<Point3d>();

					for (int i = 0; i < 6; i++)
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
				for (int i = 0; i < crkPts.Count - 1; i++)
					yield return new Line
					{
						StartPoint = crkPts[i],
						EndPoint   = crkPts[i + 1],
						LineWeight = LineWeight.LineWeight035
					};
			}
		}

		/// <summary>
		/// Add a panel to the drawing.
		/// </summary>
		/// <param name="vertices">The collection of <see cref="Point3d"/> vertices.</param>
		public static void Add(IEnumerable<Point3d> vertices) => Add(new Vertices(vertices, GeometryUnit));

		/// <summary>
		/// Add a panel to the drawing.
		/// </summary>
		/// <param name="vertices">The panel <see cref="Vertices"/> object.</param>
		public static void Add(Vertices vertices)
		{
			// Check if a panel already exist on that position. If not, create it
			if (VerticesList.Contains(vertices))
				return;

            // Add to the list
            VerticesList.Add(vertices);

            // Create the panel as a solid with 4 segments (4 points)
            var solid = new Solid(vertices.Vertex1, vertices.Vertex2, vertices.Vertex4, vertices.Vertex3)
            {
	            Layer = $"{Layer.Panel}"
            };

			solid.AddToDrawing(On_PanelErase);
		}

		/// <summary>
		/// Add a panel to the drawing.
		/// </summary>
		/// <param name="solid">The <see cref="Solid"/> that represents the panel.</param>
		public static void Add(Solid solid) => Add(solid.GetVertices());

		/// <summary>
		/// Add multiple panels to the drawing.
		/// </summary>
		/// <param name="verticesCollection">The collection of <see cref="Vertices"/>'s that represents the panels.</param>
		public static void Add(IEnumerable<Vertices> verticesCollection)
		{
			// Get solids' vertices that don't exist in the drawing
			var vertices = verticesCollection.Distinct().Where(v => !VerticesList.Contains(v)).ToArray();
			VerticesList.AddRange(vertices);

			// Create and add the panels to drawing
			var panels = vertices.Select(v => new Solid(v.Vertex1, v.Vertex2, v.Vertex4, v.Vertex3) { Layer = $"{Layer.Panel}" }).ToArray();
			panels.AddToDrawing(On_PanelErase);
		}


		/// <summary>
		/// Remove a panel from drawing.
		/// </summary>
		/// <param name="vertices">The <see cref="Vertices"/> of panel to remove.</param>
		public static void Remove(Vertices vertices)
		{
			// Remove from list
			VerticesList.RemoveAll(v => v == vertices);

			// Remove the panel
			GetSolidByVertices(vertices).RemoveFromDrawing();
		}

		/// <summary>
		/// Remove a collection of panels from drawing.
		/// </summary>
		/// <param name="vertices">The collection of <see cref="Vertices"/> of panels to remove.</param>
		public static void Remove(IEnumerable<Vertices> vertices)
		{
			// Remove from list
			VerticesList.RemoveAll(v => vertices.Any(v2 => v == v2));

			// Remove the panels
			GetSolidsByVertices(vertices).ToArray().RemoveFromDrawing();
		}


		/// <summary>
		/// Add multiple panels to the drawing.
		/// </summary>
		/// <param name="solids">The <see cref="Solid"/>'s that represents the panels.</param>
		public static void Add(IEnumerable<Solid> solids) => Add(solids.Select(GetVertices).ToArray());

		/// <summary>
		/// Return a <see cref="Solid"/> in the drawing with the corresponding <paramref name="vertices"/>.
		/// </summary>
		/// <param name="vertices">The <see cref="Vertices"/> required.</param>
		public static Solid GetSolidByVertices(Vertices vertices) => GetObjects().FirstOrDefault(s => vertices == GetVertices(s));

		/// <summary>
		/// Return a collection of <see cref="Solid"/>'s in the drawing with the corresponding collection of <see cref="Vertices"/>.
		/// </summary>
		/// <param name="vertices">The collection of <see cref="Vertices"/> required.</param>
		public static IEnumerable<Solid> GetSolidsByVertices(IEnumerable<Vertices> vertices) => GetObjects().Where(s => vertices.Any(v => v == GetVertices(s)));

		/// <summary>
		/// Get the collection of panels in the drawing.
		/// </summary>
		public static IEnumerable<Solid> GetObjects() => Layer.Panel.GetDBObjects()?.ToSolids();

        /// <summary>
        /// Update panel numbers on the XData of each panel in the model and return the collection of panels.
        /// </summary>
        /// <param name="updateNodes">Update nodes too?</param>
        public static void Update(bool updateNodes = true)
		{
			// Create the panels collection and initialize getting the elements on node layer
			var pnls = GetObjects()?.Order()?.ToArray();

			if (pnls is null || !pnls.Any())
				return;

            // Get the Xdata size
            int size = Enum.GetNames(typeof(PanelIndex)).Length;

            // Bool to alert the user
            var userAlert = false;

            for (var i = 0; i < pnls.Length; i++)
            {
	            // Get XData
	            var data = pnls[i].XData?.AsArray() ?? NewXData();

	            // Verify the size of XData
	            if (data.Length != size)
	            {
		            data = NewXData();

		            // Alert user
		            userAlert = true;
	            }

	            // Get the panel number
	            int pnlNum = i + 1;

	            // Set the updated panel number
	            data[(int) PanelIndex.Number] = new TypedValue((int) DxfCode.ExtendedDataReal, pnlNum);

                // Add the new XData
                pnls[i].SetXData(data);
            }

			// Update vertices
			VerticesList = GetPanelVertices(pnls);

            // Move panels to bottom
			pnls.MoveToBottom();

			// Update nodes
			if (updateNodes)
				Nodes.Update(false, false);

			// Alert user
			if (userAlert)
	            Application.ShowAlertDialog("Please set panel geometry and reinforcement again.");
		}

		/// <summary>
        /// Get <see cref="Vertices"/> of a <see cref="Solid"/>.
        /// </summary>
        /// <param name="panel">The quadrilateral <see cref="Solid"/> object.</param>
        public static Vertices GetVertices(Solid panel) => new Vertices(panel.GetVertices(), SettingsData.SavedUnits.Geometry);

		/// <summary>
        /// Get the width of a panel.
        /// </summary>
        /// <param name="panel">The quadrilateral <see cref="Solid"/> object.</param>
        public static double GetWidth(Solid panel) => panel.ReadXData()[(int) PanelIndex.Width].ToDouble();

		/// <summary>
		/// Get the <see cref="WebReinforcement"/> of a panel.
		/// </summary>
		/// <param name="panel">The quadrilateral <see cref="Solid"/> object.</param>
		public static WebReinforcement GetReinforcement(Solid panel)
		{
			var data = panel.ReadXData();

			// Get reinforcement
			double
				width = data[(int)PanelIndex.Width].ToDouble(),
				phiX  = data[(int)PanelIndex.XDiam].ToDouble(),
				phiY  = data[(int)PanelIndex.YDiam].ToDouble(),
				sx    = data[(int)PanelIndex.Sx].ToDouble(),
				sy    = data[(int)PanelIndex.Sy].ToDouble();

			// Get steel data
			double
				fyx = data[(int)PanelIndex.fyx].ToDouble(),
				Esx = data[(int)PanelIndex.Esx].ToDouble(),
				fyy = data[(int)PanelIndex.fyy].ToDouble(),
				Esy = data[(int)PanelIndex.Esy].ToDouble();

			Steel
				steelX = new Steel(fyx, Esx),
				steelY = new Steel(fyy, Esy);

			// Get reinforcement
			return new WebReinforcement(phiX, sx, steelX, phiY, sy, steelY, width);
		}

		/// <summary>
		/// Get the collection of <see cref="Vertices"/> of existing panels.
		/// </summary>
		/// <param name="panels">The collection of <see cref="Solid"/>'s.</param>
		public static List<Vertices> GetPanelVertices(IEnumerable<Solid> panels = null) => (panels ?? GetObjects())?.Select(GetVertices).ToList() ?? new List<Vertices>();

		/// <summary>
		/// Create new XData for panels.
		/// </summary>
		/// <returns></returns>
		private static TypedValue[] NewXData()
		{
			// Definition for the Extended Data
			string xdataStr = "Panel Data";

			// Get the Xdata size
			int size = Enum.GetNames(typeof(PanelIndex)).Length;

			var newData = new TypedValue[size];

			// Set the initial parameters
			newData[(int) PanelIndex.AppName]  = new TypedValue((int) DxfCode.ExtendedDataRegAppName, DataBase.AppName);
			newData[(int) PanelIndex.XDataStr] = new TypedValue((int) DxfCode.ExtendedDataAsciiString, xdataStr);
			newData[(int) PanelIndex.Width]    = new TypedValue((int) DxfCode.ExtendedDataReal, 100);
			newData[(int) PanelIndex.XDiam]    = new TypedValue((int) DxfCode.ExtendedDataReal, 0);
			newData[(int) PanelIndex.Sx]       = new TypedValue((int) DxfCode.ExtendedDataReal, 0);
			newData[(int) PanelIndex.fyx]      = new TypedValue((int) DxfCode.ExtendedDataReal, 0);
			newData[(int) PanelIndex.Esx]      = new TypedValue((int) DxfCode.ExtendedDataReal, 0);
			newData[(int) PanelIndex.YDiam]    = new TypedValue((int) DxfCode.ExtendedDataReal, 0);
			newData[(int) PanelIndex.Sy]       = new TypedValue((int) DxfCode.ExtendedDataReal, 0);
			newData[(int) PanelIndex.fyy]      = new TypedValue((int) DxfCode.ExtendedDataReal, 0);
			newData[(int) PanelIndex.Esy]      = new TypedValue((int) DxfCode.ExtendedDataReal, 0);

			return newData;
		}

		/// <summary>
		/// Read the <see cref="SPM.Elements.Panel"/> objects in the drawing.
		/// </summary>
		/// <param name="panelObjects">The collection of panels objects in the drawing.</param>
		/// <param name="units">Units current in use <see cref="Units"/>.</param>
		/// <param name="concreteParameters">The concrete parameters <see cref="Parameters"/>.</param>
		/// <param name="model">The concrete <see cref="ConstitutiveModel"/>.</param>
		/// <param name="nodes">The collection containing all <see cref="Node"/>'s of SPM model.</param>
		/// <param name="analysisType">Type of analysis to perform (<see cref="AnalysisType"/>).</param>
		public static IEnumerable<Panel> Read(IEnumerable<Solid> panelObjects, Units units, Parameters concreteParameters, ConstitutiveModel model, IEnumerable<Node> nodes, AnalysisType analysisType = AnalysisType.Linear) =>
			panelObjects.Select(pnl => Read(pnl, units, concreteParameters, model, nodes, analysisType)).OrderBy(pnl => pnl.Number);

		/// <summary>
		/// Read a <see cref="Panel"/> in drawing.
		/// </summary>
		/// <param name="panelObject">The <see cref="Solid"/> object of the panel from AutoCAD drawing.</param>
		/// <param name="units">Units current in use <see cref="Units"/>.</param>
		/// <param name="concreteParameters">The concrete parameters <see cref="Parameters"/>.</param>
		/// <param name="model">The concrete <see cref="ConstitutiveModel"/>.</param>
        /// <param name="nodes">The collection containing all <see cref="Node"/>'s of SPM model.</param>
		/// <param name="analysisType">Type of analysis to perform (<see cref="AnalysisType"/>).</param>
		public static Panel Read(Solid panelObject, Units units, Parameters concreteParameters, ConstitutiveModel model, IEnumerable<Node> nodes, AnalysisType analysisType = AnalysisType.Linear)
		{
			// Read the XData and get the necessary data
			var pnlData = panelObject.ReadXData(DataBase.AppName);

			// Get the panel parameters
			var number = pnlData[(int)PanelIndex.Number].ToInt();
			var width  = pnlData[(int)PanelIndex.Width].ToDouble();

			// Get reinforcement
			Length
				phiX = Length.FromMillimeters(pnlData[(int)PanelIndex.XDiam].ToDouble()).ToUnit(units.Reinforcement),
				phiY = Length.FromMillimeters(pnlData[(int)PanelIndex.YDiam].ToDouble()).ToUnit(units.Reinforcement),
				sx   = Length.FromMillimeters(pnlData[(int)PanelIndex.Sx].ToDouble()).ToUnit(units.Geometry),
				sy   = Length.FromMillimeters(pnlData[(int)PanelIndex.Sy].ToDouble()).ToUnit(units.Geometry);

			// Get steel data
			Pressure
				fyx = Pressure.FromMegapascals(pnlData[(int)PanelIndex.fyx].ToDouble()).ToUnit(units.MaterialStrength),
				Esx = Pressure.FromMegapascals(pnlData[(int)PanelIndex.Esx].ToDouble()).ToUnit(units.MaterialStrength),
				fyy = Pressure.FromMegapascals(pnlData[(int)PanelIndex.fyy].ToDouble()).ToUnit(units.MaterialStrength),
				Esy = Pressure.FromMegapascals(pnlData[(int)PanelIndex.Esy].ToDouble()).ToUnit(units.MaterialStrength);

			Steel
				steelX = new Steel(fyx, Esx),
				steelY = new Steel(fyy, Esy);
            
			// Get reinforcement
			var reinforcement = new WebReinforcement(phiX, sx, steelX, phiY, sy, steelY, Length.FromMillimeters(width).ToUnit(units.Geometry));

			return Panel.Read(analysisType, panelObject.ObjectId, number, nodes, panelObject.GetVertices(), width.ConvertFromMillimeter(units.Geometry), concreteParameters, model, reinforcement, units.Geometry);
		}

		/// <summary>
		/// Set <paramref name="width"/> to a <paramref name="panel"/>
		/// </summary>
		/// <param name="panel">The panel <see cref="Solid"/> object.</param>
		/// <param name="width">The width, in mm.</param>
		public static void SetWidth(Solid panel, double width)
		{
			// Access the XData as an array
			var data = panel.ReadXData();

			// Set the new geometry and reinforcement (line 7 to 9 of the array)
			data[(int)PanelIndex.Width] = new TypedValue((int)DxfCode.ExtendedDataReal, width);

			// Add the new XData
			panel.SetXData(data);
		}

        /// <summary>
        /// Set reinforcement to a <paramref name="panel"/>
        /// </summary>
        /// <param name="panel">The panel <see cref="Solid"/> object.</param>
        /// <param name="directionX">The <see cref="WebReinforcementDirection"/> for horizontal direction.</param>
        /// <param name="directionY">The <see cref="WebReinforcementDirection"/> for vertical direction.</param>
        public static void SetReinforcement(Solid panel, WebReinforcementDirection directionX, WebReinforcementDirection directionY)
		{
			// Access the XData as an array
			var data = panel.ReadXData();

			// Set X direction
			data[(int)PanelIndex.XDiam] = new TypedValue((int)DxfCode.ExtendedDataReal, directionX?.BarDiameter ?? 0);
			data[(int)PanelIndex.Sx]    = new TypedValue((int)DxfCode.ExtendedDataReal, directionX?.BarSpacing  ?? 0);
			data[(int)PanelIndex.fyx]   = new TypedValue((int)DxfCode.ExtendedDataReal, directionX?.Steel?.YieldStress   ?? 0);
			data[(int)PanelIndex.Esx]   = new TypedValue((int)DxfCode.ExtendedDataReal, directionX?.Steel?.ElasticModule ?? 0);

			// Set Y direction
			data[(int)PanelIndex.YDiam] = new TypedValue((int)DxfCode.ExtendedDataReal, directionY?.BarDiameter ?? 0);
			data[(int)PanelIndex.Sy]    = new TypedValue((int)DxfCode.ExtendedDataReal, directionY?.BarSpacing  ?? 0);
			data[(int)PanelIndex.fyy]   = new TypedValue((int)DxfCode.ExtendedDataReal, directionY?.Steel?.YieldStress   ?? 0);
			data[(int)PanelIndex.Esy]   = new TypedValue((int)DxfCode.ExtendedDataReal, directionY?.Steel?.ElasticModule ?? 0);

			// Add the new XData
			panel.SetXData(data);
		}

        /// <summary>
        /// Set reinforcement to a <paramref name="panel"/>
        /// </summary>
        /// <param name="panel">The panel <see cref="Solid"/> object.</param>
        /// <param name="reinforcement">The <see cref="WebReinforcement"/>.</param>
        public static void SetReinforcement(Solid panel, WebReinforcement reinforcement) => SetReinforcement(panel, reinforcement?.DirectionX, reinforcement?.DirectionY);

		/// <summary>
        /// Draw panel stresses.
        /// </summary>
        /// <param name="panels">The collection of <see cref="Panel"/>'s.</param>
        public static void DrawStresses(IEnumerable<Panel> panels)
        {
	        // Get units
	        var units = SettingsData.SavedUnits;

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
			        var cntrPt = pnl.Geometry.Vertices.CenterPoint;

			        // Get the maximum length of the panel
			        double lMax = l.Max().ConvertFromMillimeter(units.Geometry);

			        // Get the average stress
			        double tauAvg = pnl.AverageStresses.TauXY.ConvertFromMPa(units.PanelStresses);

			        // Calculate the scale factor for the block and text
			        double scFctr = 0.001 * lMax;

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
					        if (tauAvg < 0)
					        {
						        blkRef.TransformBy(Matrix3d.Rotation(Constants.Pi, DataBase.Ucs.Yaxis, cntrPt));
					        }

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
				        if (stresses.Sigma2.ApproxZero())
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
					        {
						        blkRef.TransformBy(Matrix3d.Rotation(stresses.Theta2, DataBase.Ucs.Zaxis,
							        cntrPt));
					        }

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

					        ln.TransformBy(Matrix3d.Rotation(stresses.Theta2, Database.DataBase.Ucs.Zaxis, cntrPt));

					        // Set the alignment point
					        var algnPt = ln.EndPoint;

					        // Set the parameters
					        sigTxt.Layer = $"{layer}";
					        sigTxt.Height = 30 * scFctr;
					        sigTxt.TextString = $"{stresses.Sigma2.Abs().ConvertFromMPa(units.PanelStresses):0.00}";
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
				        // Verify tensile stress
				        if (stresses.Sigma1.ApproxZero())
					        return;

				        // Create tensile stress block
				        using (var blkRef = new BlockReference(cntrPt, tensStress))
				        {
					        blkRef.Layer = $"{layer}";

					        // Set the scale of the block
					        blkRef.TransformBy(Matrix3d.Scaling(scFctr, cntrPt));

					        // Rotate the block in theta angle
					        if (!stresses.Theta2.ApproxZero())
					        {
						        blkRef.TransformBy(Matrix3d.Rotation(stresses.Theta2, Database.DataBase.Ucs.Zaxis,
							        cntrPt));
					        }

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
					        sigTxt.TextString = $"{stresses.Sigma1.Abs().ConvertFromMPa(units.PanelStresses):0.00}";
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
        /// Draw panel cracks.
        /// </summary>
        /// <param name="panels">The collection of <see cref="Panel"/>'s.</param>
        public static void DrawCracks(IEnumerable<Panel> panels)
		{
			var units = SettingsData.SavedUnits;

	        // Start a transaction
	        using (var trans = DataBase.StartTransaction())
	        using (var blkTbl = (BlockTable) trans.GetObject(DataBase.Database.BlockTableId, OpenMode.ForRead))
	        {
		        // Read the object Id of the crack block
		        var crackBlock = blkTbl[$"{Block.PanelCrack}"];

		        foreach (var pnl in panels)
		        {
			        // Get the average crack opening
			        double w = pnl.CrackOpening;

			        if (w.ApproxZero(1E-6))
				        continue;

					// Get panel data
					var l      = pnl.Geometry.EdgeLengths;
			        var cntrPt = pnl.Geometry.Vertices.CenterPoint;

			        // Get the maximum length of the panel
			        double lMax = l.Max().ConvertFromMillimeter(units.Geometry);

			        // Calculate the scale factor for the block and text
			        double scFctr = 0.001 * lMax;

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
					        crkTxt.TextString = $"{w.ConvertFromMillimeter(units.CrackOpenings):0.00E+00}";
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
 
		/// <summary>
		/// Event to execute when a panel is erased.
		/// </summary>
		private static void On_PanelErase(object sender, ObjectErasedEventArgs e)
		{
			if (VerticesList is null || !VerticesList.Any() || !(sender is Solid pnl))
				return;

			var vertices = GetVertices(pnl);

			VerticesList.RemoveAll(v => v == vertices);

			Update(false);
		}

    }
}