﻿using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Extensions.AutoCAD;
using Extensions.Number;
using Material.Concrete;
using Material.Reinforcement;
using Material.Reinforcement.Uniaxial;
using MathNet.Numerics;
using SPM.Elements;
using SPM.Elements.StringerProperties;
using SPMTool.Enums;
using SPMTool.Extensions;
using UnitsNet.Units;

namespace SPMTool.Database.Elements
{
	/// <summary>
    /// Stringers class.
	/// </summary>
	public static class Stringers
	{
		/// <summary>
		/// Auxiliary list of <see cref="StringerGeometry"/>'s.
		/// </summary>
		public static List<StringerGeometry> Geometries { get; private set; } = GetGeometries();

		/// <summary>
		/// Get the geometry unit.
		/// </summary>
		private static LengthUnit GeometryUnit => SettingsData.SavedUnits.Geometry;

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

					for (int i = 0; i < 2; i++)
					{
						// Set the start X coordinate
						double y = 40 * i;

						pts.Add(new Point3d(0, y, 0));
						pts.Add(new Point3d( 1.7633, y + 10, 0));
						pts.Add(new Point3d(-1.7633, y + 30, 0));
					}

					// Add the end point
					pts.Add(new Point3d(0, 80, 0));

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
        /// Add a stringer to drawing.
        /// </summary>
        /// <param name="startPoint">The start <see cref="Point3d"/>.</param>
        /// <param name="endPoint">The end <see cref="Point3d"/>.</param>
        public static void Add(Point3d startPoint, Point3d endPoint)
        {
	        // Order points
	        var pts = new[] { startPoint, endPoint }.Order().ToArray();

	        Add(new StringerGeometry(pts[0], pts[1], 0, 0));
        }

        /// <summary>
        /// Add a stringer to drawing.
        /// </summary>
        /// <param name="line">The <see cref="Line"/> to be the stringer.</param>
        public static void Add(Line line) => Add(line.StartPoint, line.EndPoint);

		/// <summary>
		/// Add a stringer to drawing.
		/// </summary>
		/// <param name="geometry">The <see cref="StringerGeometry"/> to add to drawing.</param>
		public static void Add(StringerGeometry geometry)
        {
	        // Check if a Stringer already exist on that position. If not, create it
	        if (Geometries.Contains(geometry))
		        return;

	        // Add to the list
	        Geometries.Add(geometry);

	        // Set layer
	        var line = new Line(geometry.InitialPoint, geometry.EndPoint)
	        {
		        Layer = $"{Layer.Stringer}"
	        };

	        // Add the object
	        line.AddToDrawing(On_StringerErase);
        }

        /// <summary>
        /// Add stringers to drawing.
        /// </summary>
        /// <param name="lines">The <see cref="Line"/>'s to be the stringers.</param>
        public static void Add(IEnumerable<Line> lines)
        {
			// Get the geometries
			var geos =
				(from Line line in lines 
					let pts = new [] { line.StartPoint, line.EndPoint }.Order().ToArray() 
					select new StringerGeometry(pts[0], pts[1], 0, 0, GeometryUnit)).ToArray();

			Add(geos);
        }

		/// <summary>
		/// Add stringers to drawing.
		/// </summary>
		/// <param name="geometries">The <see cref="StringerGeometry"/>'s to add to drawing.</param>
		public static void Add(IEnumerable<StringerGeometry> geometries)
        {
			// Get the geometries that don't exist in the drawing
			var newGeos = geometries.Distinct().Where(g => !Geometries.Contains(g)).ToArray();
			Geometries.AddRange(newGeos);

			// Create and add the stringers
			var strs = newGeos.Select(g => new Line(g.InitialPoint, g.EndPoint) { Layer = $"{Layer.Stringer}" }).ToArray();
	        strs.AddToDrawing(On_StringerErase);
        }

		/// <summary>
		/// Remove a stringer from drawing.
		/// </summary>
		/// <param name="stringer">The <see cref="Line"/> to remove from drawing.</param>
		public static void Remove(Line stringer)
		{
			if (stringer is null)
				return;

			// Remove from list
			Geometries.RemoveAll(g => g == StringerObject.GetGeometry(stringer, false));

			// Remove the stringer
			stringer.RemoveFromDrawing();
		}

		/// <summary>
		/// Remove a stringer from drawing.
		/// </summary>
		/// <param name="geometry">The <see cref="StringerGeometry"/> to remove from drawing.</param>
		public static void Remove(StringerGeometry geometry)
		{
			// Remove from list
			Geometries.RemoveAll(g => g == geometry);

			// Remove the stringer
			GetLineByGeometry(geometry).RemoveFromDrawing();
		}

		/// <summary>
		/// Remove a collection of stringers from drawing.
		/// </summary>
		/// <param name="stringers">The collection of <see cref="Line"/>'s to remove from drawing.</param>
		public static void Remove(IEnumerable<Line> stringers)
		{
			if (stringers is null || !stringers.Any())
				return;

			// Remove from list
			var geos = stringers.Select(s => StringerObject.GetGeometry(s, false)).ToArray();
			Geometries.RemoveAll(geos.Contains);

			// Remove the stringer
			stringers.RemoveFromDrawing();
		}

		/// <summary>
		/// Remove a collection of stringers from drawing.
		/// </summary>
		/// <param name="geometries">The <see cref="StringerGeometry"/>'s to remove from drawing.</param>
		public static void Remove(IEnumerable<StringerGeometry> geometries)
		{
			if(geometries is null || !geometries.Any())
				return;

			// Remove from list
			Geometries.RemoveAll(geometries.Contains);

			// Remove the stringer
			GetLinesByGeometries(geometries).ToArray().RemoveFromDrawing();
		}

		/// <summary>
		/// Return a <see cref="Line"/> in the drawing corresponding to this <paramref name="geometry"/>.
		/// </summary>
		/// <param name="geometry">The required <see cref="StringerGeometry"/>. </param>
		public static Line GetLineByGeometry(StringerGeometry geometry) => GetObjects().FirstOrDefault(s => geometry == StringerObject.GetGeometry(s, false));

		/// <summary>
		/// Return a collection of <see cref="Line"/>'s in the drawing corresponding to these <paramref name="geometries"/>.
		/// </summary>
		/// <param name="geometries">The required <see cref="StringerGeometry"/>'s. </param>
		public static IEnumerable<Line> GetLinesByGeometries(IEnumerable<StringerGeometry> geometries) => GetObjects().Where(s => geometries.Any(g => g == StringerObject.GetGeometry(s, false)));

		/// <summary>
		/// Get the collection of stringers in the drawing.
		/// </summary>
		public static IEnumerable<Line> GetObjects() => Layer.Stringer.GetDBObjects().ToLines();

        /// <summary>
        /// Update the Stringer numbers on the XData of each Stringer in the model and return the collection of stringers.
        /// </summary>
        /// <param name="updateNodes">Update nodes too?</param>
        public static void Update(bool updateNodes = true)
        {
			// Get the Stringer collection
			var strs = GetObjects()?.Order()?.ToArray();

			if (strs is null || !strs.Any())
				return;

	        // Bool to alert the user
	        bool userAlert = false;

	        // Access the stringers on the document
	        for (var i = 0; i < strs.Length; i++)
	        {
		        // Initialize the array of typed values for XData
		        TypedValue[] data;

		        // Get the Xdata size
		        int size = Enum.GetNames(typeof(StringerIndex)).Length;

		        // If XData does not exist, create it
		        if (strs[i].XData is null)
			        data = StringerObject.NewXData();

		        else // Xdata exists
		        {
			        // Get the result buffer as an array
			        data = strs[i].ReadXData();

			        // Verify the size of XData
			        if (data.Length != size)
			        {
				        data = StringerObject.NewXData();

				        // Alert the user
				        userAlert = true;
			        }
		        }
		        // Get the Stringer number
		        int strNum = i + 1;

		        // Set the updated number and nodes in ascending number and length (line 2 to 6)
		        data[(int) StringerIndex.Number] = new TypedValue((int) DxfCode.ExtendedDataReal, strNum);

                // Add the new XData
                strs[i].SetXData(data);
	        }

			// Save geometries
			Geometries = GetGeometries(strs);

			// Get all the nodes in the model
			if (updateNodes)
				Nodes.Update();

			// Alert the user
			if (userAlert)
		        Application.ShowAlertDialog("Please set Stringer geometry and reinforcement again.");
        }

        /// <summary>
        /// Get and update the stringer geometries in the drawing.
        /// </summary>
        /// <param name="stringers">The collection of <see cref="Line"/>'s.</param>
        private static List<StringerGeometry> GetGeometries(IEnumerable<Line> stringers = null) => (stringers ?? GetObjects())?.Select(str => StringerObject.GetGeometry(str, false)).ToList() ?? new List<StringerGeometry>();

        /// <summary>
		/// Read <see cref="Stringer"/> elements in drawing.
		/// </summary>
		/// <param name="lines">The collection of the stringer <see cref="Line"/>'s from AutoCAD drawing.</param>
		/// <param name="nodes">The collection containing all <see cref="Node"/>'s of SPM model.</param>
		/// <param name="units">Units current in use <see cref="Units"/>.</param>
		/// <param name="concreteParameters">The concrete parameters <see cref="Parameters"/>.</param>
		/// <param name="model">The concrete <see cref="ConstitutiveModel"/>.</param>
		/// <param name="analysisType">Type of analysis to perform (<see cref="AnalysisType"/>).</param>
		public static IEnumerable<Stringer> Read(IEnumerable<Line> lines, Units units, Parameters concreteParameters, ConstitutiveModel model, IEnumerable<Node> nodes, AnalysisType analysisType = AnalysisType.Linear) =>
			lines?.Select(line => StringerObject.Read(line, units, concreteParameters, model, nodes, analysisType)).OrderBy(str => str.Number);

        /// <summary>
        /// Draw stringer forces.
        /// </summary>
        /// <param name="stringers">The collection of <see cref="Stringer"/>'s.</param>
        /// <param name="maxForce">The maximum stringer force.</param>
        public static void DrawForces(IEnumerable<Stringer> stringers, double maxForce)
        {
	        // Get units
	        var units = SettingsData.SavedUnits;

	        // Get the scale factor
	        var scFctr = units.ScaleFactor;

	        foreach (var stringer in stringers)
	        {
		        // Check if the stringer is loaded
		        if (stringer.State is Stringer.ForceState.Unloaded)
			        continue;

		        // Get the parameters of the Stringer
		        double
			        l   = stringer.Geometry.Length.ConvertFromMillimeter(units.Geometry),
			        ang = stringer.Geometry.Angle;

		        // Get the start point
		        var stPt = stringer.Geometry.InitialPoint;

		        // Get normal forces
		        var (N1, N3) = stringer.NormalForces;

		        // Calculate the dimensions to draw the solid (the maximum dimension will be 150 mm)
		        double
			        h1 = (150 * N1 / maxForce).ConvertFromMillimeter(units.Geometry),
			        h3 = (150 * N3 / maxForce).ConvertFromMillimeter(units.Geometry);

		        // Check if load state is pure tension or compression
		        if (stringer.State != Stringer.ForceState.Combined)
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
				        dgrm.ColorIndex = Math.Max(N1, N3) > 0 ? (short)Color.Blue1 : (short)Color.Red;

				        // Rotate the diagram
				        dgrm.TransformBy(Matrix3d.Rotation(ang, DataBase.Ucs.Zaxis, stPt));

				        // Add the diagram to the drawing
				        dgrm.AddToDrawing();
			        }
		        }

		        void Combined()
		        {
                    // Calculate the point where the Stringer force will be zero
                    double x = h1.Abs() * l / (h1.Abs() + h3.Abs());
                    var invPt = new Point3d(stPt.X + x, stPt.Y, 0);

                    // Calculate the points (the solid will be rotated later)
                    Point3d[] vrts1 =
                    {
                        stPt,
                        invPt,
                        new Point3d(stPt.X, stPt.Y + h1, 0),
                    };

                    Point3d[] vrts3 =
                    {
                        invPt,
                        new Point3d(stPt.X + l, stPt.Y,      0),
                        new Point3d(stPt.X + l, stPt.Y + h3, 0),
                    };

                    // Create the diagrams as solids with 3 segments (3 points)
                    using (var dgrm1 = new Solid(vrts1[0], vrts1[1], vrts1[2]))
                    {
                        // Set the layer and transparency
                        dgrm1.Layer = $"{Layer.StringerForce}";
                        dgrm1.Transparency = 80.Transparency();

                        // Set the color (blue to compression and red to tension)
                        dgrm1.ColorIndex = N1 > 0 ? (short) Color.Blue1 : (short) Color.Red;

                        // Rotate the diagram
                        dgrm1.TransformBy(Matrix3d.Rotation(ang, Database.DataBase.Ucs.Zaxis, stPt));

                        // Add the diagram to the drawing
                        dgrm1.AddToDrawing();
                    }

                    using (var dgrm3 = new Solid(vrts3[0], vrts3[1], vrts3[2]))
                    {
                        // Set the layer and transparency
                        dgrm3.Layer = $"{Layer.StringerForce}";
                        dgrm3.Transparency = 80.Transparency();

                        // Set the color (blue to compression and red to tension)
                        dgrm3.ColorIndex = N3 > 0 ? (short) Color.Blue1 : (short) Color.Red;

                        // Rotate the diagram
                        dgrm3.TransformBy(Matrix3d.Rotation(ang, Database.DataBase.Ucs.Zaxis, stPt));

                        // Add the diagram to the drawing
                        dgrm3.AddToDrawing();
                    }

                }

                void AddTexts()
                {
                    if (!N1.ApproxZero(0.01))
                        using (var txt1 = new DBText())
                        {
                            // Set the parameters
                            txt1.Layer = $"{Layer.StringerForce}";
                            txt1.Height = 30 * scFctr;

                            // Write force in unit
                            txt1.TextString = $"{N1.ConvertFromNewton(units.StringerForces).Abs():0.00}";

                            // Set the color (blue to compression and red to tension) and position
                            if (N1 > 0)
                            {
                                txt1.ColorIndex = (short)Color.Blue1;
                                txt1.Position = new Point3d(stPt.X + 10 * scFctr, stPt.Y + h1 + 20 * scFctr, 0);
                            }
                            else
                            {
                                txt1.ColorIndex = (short)Color.Red;
                                txt1.Position = new Point3d(stPt.X + 10 * scFctr, stPt.Y + h1 - 50 * scFctr, 0);
                            }

                            // Rotate the text
                            txt1.TransformBy(Matrix3d.Rotation(ang, Database.DataBase.Ucs.Zaxis, stPt));

                            // Add the text to the drawing
                            txt1.AddToDrawing();
                        }

                    if (!N3.ApproxZero(0.01))
                        using (var txt3 = new DBText())
                        {
                            // Set the parameters
                            txt3.Layer = $"{Layer.StringerForce}";
                            txt3.Height = 30 * scFctr;

                            // Write force in unit
                            txt3.TextString = $"{N3.ConvertFromNewton(units.StringerForces).Abs():0.00}";

                            // Set the color (blue to compression and red to tension) and position
                            if (N3 > 0)
                            {
                                txt3.ColorIndex = (short)Color.Blue1;
                                txt3.Position = new Point3d(stPt.X + l - 10 * scFctr, stPt.Y + h3 + 20 * scFctr, 0);
                            }
                            else
                            {
                                txt3.ColorIndex = (short)Color.Red;
                                txt3.Position = new Point3d(stPt.X + l - 10 * scFctr, stPt.Y + h3 - 50 * scFctr, 0);
                            }

                            // Adjust the alignment
                            txt3.HorizontalMode = TextHorizontalMode.TextRight;
                            txt3.AlignmentPoint = txt3.Position;

                            // Rotate the text
                            txt3.TransformBy(Matrix3d.Rotation(ang, Database.DataBase.Ucs.Zaxis, stPt));

                            // Add the text to the drawing
                            txt3.AddToDrawing();
                        }
                }
	        }

	        // Turn the layer on
	        Layer.StringerForce.On();
        }

		/// <summary>
		/// Draw cracks at the stringers.
		/// </summary>
		/// <param name="stringers">The collection of stringers in the model.</param>
		public static void DrawCracks(IEnumerable<Stringer> stringers)
		{
			// Get units
			var units  = SettingsData.SavedUnits;
			var scFctr = units.ScaleFactor;

			// Start a transaction
			using (var trans = DataBase.StartTransaction())
			using (var blkTbl = (BlockTable)trans.GetObject(DataBase.Database.BlockTableId, OpenMode.ForRead))
			{
				// Read the object Id of the crack block
				var crackBlock = blkTbl[$"{Block.StringerCrack}"];

				foreach (var str in stringers)
				{
					// Get the average crack opening
					var cracks = str.CrackOpenings;

					// Get length converted and angle
					var l = str.Geometry.Length.ConvertFromMillimeter(units.Geometry);
					var a = str.Geometry.Angle;

					// Get insertion points
					var stPt = str.Geometry.InitialPoint;

					var points = new[]
					{
						new Point3d(stPt.X + 0.1 * l, 0, 0),
						new Point3d(stPt.X + 0.5 * l, 0, 0),
						new Point3d(stPt.X + 0.9 * l, 0, 0)
					};

					for (int i = 0; i < cracks.Length; i++)
					{
						if(cracks[i].ApproxZero(1E-4))
							continue;

						// Add crack blocks
						AddCrackBlock(cracks[i], points[i]);
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
								blkRef.TransformBy(Matrix3d.Rotation(a, DataBase.Ucs.Zaxis, stPt));

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
							crkTxt.TextString = $"{Math.Abs(w.ConvertFromMillimeter(units.CrackOpenings)):0.00E+00}";
							crkTxt.Position = algnPt;
							crkTxt.HorizontalMode = TextHorizontalMode.TextCenter;
							crkTxt.AlignmentPoint = algnPt;

							// Rotate text
							if (!a.ApproxZero(1E-3))
								crkTxt.TransformBy(Matrix3d.Rotation(a, DataBase.Ucs.Zaxis, stPt));

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
		/// Event to execute when a stringer is erased.
		/// </summary>
		private static void On_StringerErase(object sender, ObjectErasedEventArgs e)
		{
			if (Geometries is null || !Geometries.Any() || !(sender is Line str))
				return;

			Geometries.RemoveAll(g => g == StringerObject.GetGeometry(str, false));

			// Update and remove unnecessary nodes
			Update(false);
			Nodes.Update(false);
		}
	}
}