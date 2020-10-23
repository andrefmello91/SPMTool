using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Extensions.AutoCAD;
using Extensions.Number;
using Material.Concrete;
using Material.Reinforcement;
using SPM.Elements;
using SPM.Elements.StringerProperties;
using SPMTool.Enums;
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
		private static List<StringerGeometry> _geometries;

        /// <summary>
        /// Add a stringer to drawing.
        /// </summary>
        /// <param name="line">The <see cref="Line"/> to be the stringer.</param>
        public static void Add(Line line)
		{
			// Get the list of stringers if it's not imposed
			if (_geometries is null)
				_geometries = new List<StringerGeometry>(StringerGeometries());

			// Check if a Stringer already exist on that position. If not, create it
			var geometry = new StringerGeometry(line.StartPoint, line.EndPoint, 0, 0);

			if (_geometries.Contains(geometry))
				return;

            // Add to the list
            _geometries.Add(geometry);

            // Set layer
            line.Layer = $"{Layer.Stringer}";

            // Add the object
            line.Add(On_StringerErase);
        }

        /// <summary>
        /// Add a stringer to drawing.
        /// </summary>
        /// <param name="startPoint">The start <see cref="Point3d"/>.</param>
        /// <param name="endPoint">The end <see cref="Point3d"/>.</param>
        public static void Add(Point3d startPoint, Point3d endPoint)
        {
	        using (var line = new Line(startPoint, endPoint))
				Add(line);
        }

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
	        // Get all the nodes in the model
			if (updateNodes)
				Nodes.Update();

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
			        data = NewXData();

		        else // Xdata exists
		        {
			        // Get the result buffer as an array
			        data = strs[i].ReadXData();

			        // Verify the size of XData
			        if (data.Length != size)
			        {
				        data = NewXData();

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
			_geometries = strs.Select(str => GetGeometry(str, false)).ToList();

	        // Alert the user
	        if (userAlert)
		        Application.ShowAlertDialog("Please set Stringer geometry and reinforcement again.");
        }

        /// <summary>
        /// Get the <see cref="StringerGeometry"/> from this <see cref="Line"/>.
        /// </summary>
        /// <param name="line">The <see cref="Line"/> object.</param>
        /// <param name="readXData">Read extended data of <paramref name="line"/>?</param>
        public static StringerGeometry GetGeometry(Line line, bool readXData = true)
        {
	        double
		        w = 0,
		        h = 0;

	        var unit = DataBase.Units.Geometry;

	        if (readXData)
	        {
		        var data = line.ReadXData();
		        w = data[(int) StringerIndex.Width].ToDouble().ConvertFromMillimeter(unit);
		        h = data[(int) StringerIndex.Height].ToDouble().ConvertFromMillimeter(unit);
	        }

			return new StringerGeometry(line.StartPoint, line.EndPoint, w, h, unit);
        }

        /// <summary>
        /// Get the <see cref="UniaxialReinforcement"/> from this <see cref="Line"/>.
        /// </summary>
        /// <param name="line">The <see cref="Line"/> object.</param>
        public static UniaxialReinforcement GetReinforcement(Line line)
        {
	        var data = line.ReadXData();

	        var n = data[(int) StringerIndex.NumOfBars].ToInt();
	        var d = data[(int) StringerIndex.BarDiam].ToDouble();

	        if (n == 0 || d.ApproxZero())
		        return null;

	        double
		        fy = data[(int) StringerIndex.Steelfy].ToDouble(),
		        Es = data[(int) StringerIndex.SteelEs].ToDouble();

			return new UniaxialReinforcement(n, d, new Steel(fy, Es));
        }

		/// <summary>
        /// Get a collection containing all the stringer geometries in the drawing.
        /// </summary>
		public static IEnumerable<StringerGeometry> StringerGeometries() => GetObjects()?.Select(str => GetGeometry(str, false));

		/// <summary>
        /// Create new extended data for stringers.
        /// </summary>
		private static TypedValue[] NewXData()
		{
			// Definition for the Extended Data
			string xdataStr = "Stringer Data";

			// Get the Xdata size
			int size = Enum.GetNames(typeof(StringerIndex)).Length;

			var newData = new TypedValue[size];

			// Set the initial parameters
			newData[(int) StringerIndex.AppName]   = new TypedValue((int) DxfCode.ExtendedDataRegAppName, DataBase.AppName);
			newData[(int) StringerIndex.XDataStr]  = new TypedValue((int) DxfCode.ExtendedDataAsciiString, xdataStr);
			newData[(int) StringerIndex.Width]     = new TypedValue((int) DxfCode.ExtendedDataReal, 100);
			newData[(int) StringerIndex.Height]    = new TypedValue((int) DxfCode.ExtendedDataReal, 100);
			newData[(int) StringerIndex.NumOfBars] = new TypedValue((int) DxfCode.ExtendedDataInteger32, 0);
			newData[(int) StringerIndex.BarDiam]   = new TypedValue((int) DxfCode.ExtendedDataReal, 0);
			newData[(int) StringerIndex.Steelfy]   = new TypedValue((int) DxfCode.ExtendedDataReal, 0);
			newData[(int) StringerIndex.SteelEs]   = new TypedValue((int) DxfCode.ExtendedDataReal, 0);

			return newData;
		}

		/// <summary>
		/// Read <see cref="Stringer"/> elements in drawing.
		/// </summary>
		/// <param name="lines">The collection of the stringer <see cref="Line"/>'s from AutoCAD drawing.</param>
		/// <param name="nodes">The collection containing all <see cref="Node"/>'s of SPM model.</param>
		/// <param name="units">Units current in use <see cref="Units"/>.</param>
		/// <param name="concreteParameters">The concrete parameters <see cref="Parameters"/>.</param>
		/// <param name="concreteConstitutive">The concrete constitutive <see cref="Constitutive"/>.</param>
		/// <param name="analysisType">Type of analysis to perform (<see cref="AnalysisType"/>).</param>
		public static IEnumerable<Stringer> Read(IEnumerable<Line> lines, Units units, Parameters concreteParameters, Constitutive concreteConstitutive, IEnumerable<Node> nodes, AnalysisType analysisType = AnalysisType.Linear) =>
			lines?.Select(line => Read(line, units, concreteParameters, concreteConstitutive, nodes, analysisType)).OrderBy(str => str.Number);

        /// <summary>
        /// Read a <see cref="Stringer"/> in drawing.
        /// </summary>
        /// <param name="line">The <see cref="Line"/> object of the stringer from AutoCAD drawing.</param>
        /// <param name="nodes">The collection containing all <see cref="Node"/>'s of SPM model.</param>
		/// <param name="units">Units current in use <see cref="Units"/>.</param>
        /// <param name="concreteParameters">The concrete parameters <see cref="Parameters"/>.</param>
        /// <param name="concreteConstitutive">The concrete constitutive <see cref="Constitutive"/>.</param>
        /// <param name="analysisType">Type of analysis to perform (<see cref="AnalysisType"/>).</param>
        public static Stringer Read(Line line, Units units, Parameters concreteParameters, Constitutive concreteConstitutive, IEnumerable<Node> nodes, AnalysisType analysisType = AnalysisType.Linear)
		{
			// Read the XData and get the necessary data
			var data = line.ReadXData();

			// Get the Stringer number
			int number = data[(int)StringerIndex.Number].ToInt();

			// Get geometry
			double
				width  = data[(int)StringerIndex.Width].ToDouble().ConvertFromMillimeter(units.Geometry), 
				height = data[(int)StringerIndex.Height].ToDouble().ConvertFromMillimeter(units.Geometry);

			// Get reinforcement
			var reinforcement = GetReinforcement(data, width * height);

			return Stringer.Read(analysisType, line.ObjectId, number, nodes, line.StartPoint, line.EndPoint, width, height, concreteParameters, concreteConstitutive, reinforcement, units.Geometry);
		}

        /// <summary>
		/// Save extended data to this <paramref name="stringer"/>.
		/// </summary>
		/// <param name="stringer">The <see cref="Stringer"/>.</param>
		public static void SaveStringerData(Stringer stringer) => SaveStringerData(stringer.ObjectId, stringer.Geometry, stringer.Reinforcement);

		/// <summary>
		/// Save extended data to the stringer related to this <paramref name="objectId"/>.
		/// </summary>
		/// <param name="objectId">The <see cref="ObjectId"/>.</param>
		/// <param name="geometry">The <see cref="StringerGeometry"/>.</param>
		/// <param name="reinforcement">The <see cref="UniaxialReinforcement"/>.</param>
		public static void SaveStringerData(ObjectId objectId, StringerGeometry geometry, UniaxialReinforcement reinforcement)
		{
			// Access the XData as an array
			var data = objectId.ReadXData();

			// Set the new geometry
			data[(int) StringerIndex.Width]  = new TypedValue((int) DxfCode.ExtendedDataReal,  geometry.Width);
			data[(int) StringerIndex.Height] = new TypedValue((int) DxfCode.ExtendedDataReal, geometry.Height);

			// Save reinforcement
			data[(int) StringerIndex.NumOfBars] = new TypedValue((int) DxfCode.ExtendedDataInteger32, reinforcement?.NumberOfBars    ?? 0);
			data[(int) StringerIndex.BarDiam]   = new TypedValue((int) DxfCode.ExtendedDataReal, reinforcement?.BarDiameter          ?? 0);
			data[(int) StringerIndex.Steelfy]   = new TypedValue((int) DxfCode.ExtendedDataReal, reinforcement?.Steel?.YieldStress   ?? 0);
			data[(int) StringerIndex.SteelEs]   = new TypedValue((int) DxfCode.ExtendedDataReal, reinforcement?.Steel?.ElasticModule ?? 0);

			// Add the new XData
			using (var ent = objectId.ToEntity())
				ent.SetXData(data);
		}

		/// <summary>
		/// Get stringer <see cref="UniaxialReinforcement"/> from <paramref name="stringerXData"/>.
		/// </summary>
		/// <param name="stringerXData">The <see cref="Array"/> containing stringer XData.</param>
		/// <param name="stringerArea">The area of stringer cross-section, in mm2.</param>
		/// <returns></returns>
		private static UniaxialReinforcement GetReinforcement(TypedValue[] stringerXData, double stringerArea)
		{
			// Get reinforcement
			int numOfBars = stringerXData[(int)StringerIndex.NumOfBars].ToInt();
			double phi    = stringerXData[(int)StringerIndex.BarDiam].ToDouble();

			if (numOfBars == 0 || phi.ApproxZero())
				return null;

			// Get steel data
			double
				fy = stringerXData[(int)StringerIndex.Steelfy].ToDouble(),
				Es = stringerXData[(int)StringerIndex.SteelEs].ToDouble();

			// Set reinforcement
			return new UniaxialReinforcement(numOfBars, phi, new Steel(fy, Es), stringerArea);
		}

		/// <summary>
		/// Set <paramref name="geometry"/> to a <paramref name="stringer"/>
		/// </summary>
		/// <param name="stringer">The stringer <see cref="Line"/> object.</param>
		/// <param name="geometry">The <see cref="StringerGeometry"/> to set.</param>
		public static void SetGeometry(Line stringer, StringerGeometry geometry)
		{
			// Access the XData as an array
			var data = stringer.ReadXData();

			// Set the new geometry and reinforcement (line 7 to 9 of the array)
			data[(int)StringerIndex.Width]  = new TypedValue((int)DxfCode.ExtendedDataReal, geometry.Width);
			data[(int)StringerIndex.Height] = new TypedValue((int)DxfCode.ExtendedDataReal, geometry.Height);

            // Add the new XData
            stringer.SetXData(data);
		}

        /// <summary>
        /// Set <paramref name="reinforcement"/> to a <paramref name="stringer"/>
        /// </summary>
        /// <param name="stringer">The stringer <see cref="Line"/> object.</param>
        /// <param name="reinforcement">The <see cref="UniaxialReinforcement"/> to set.</param>
        public static void SetReinforcement(Line stringer, UniaxialReinforcement reinforcement)
		{
			// Access the XData as an array
			var data = stringer.ReadXData();
			
            // Set values
            data[(int)StringerIndex.NumOfBars] = new TypedValue((int)DxfCode.ExtendedDataInteger32, reinforcement?.NumberOfBars ?? 0);
			data[(int)StringerIndex.BarDiam]   = new TypedValue((int)DxfCode.ExtendedDataReal,      reinforcement?.BarDiameter  ?? 0);

			data[(int)StringerIndex.Steelfy] = new TypedValue((int)DxfCode.ExtendedDataReal, reinforcement?.Steel?.YieldStress   ?? 0);
			data[(int)StringerIndex.SteelEs] = new TypedValue((int)DxfCode.ExtendedDataReal, reinforcement?.Steel?.ElasticModule ?? 0);

			// Add the new XData
			stringer.SetXData(data);
		}

		/// <summary>
        /// Draw stringer forces.
        /// </summary>
        /// <param name="stringers">The collection of <see cref="Stringer"/>'s.</param>
        /// <param name="maxForce">The maximum stringer force.</param>
        /// <param name="units">Current <see cref="Units"/>.</param>
        public static void DrawForces(IEnumerable<Stringer> stringers, double maxForce, Units units)
        {
	        // Erase all the Stringer forces in the drawing
	        Layer.StringerForce.EraseObjects();

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
				        dgrm.Add();
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
                        dgrm1.Add();
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
                        dgrm3.Add();
                    }

                }

                void AddTexts()
                {
                    if (!N1.ApproxZero())
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
                            txt1.Add();
                        }

                    if (!N3.ApproxZero())
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
                            txt3.Add();
                        }
                }
	        }

	        // Turn the layer on
	        Layer.StringerForce.On();
        }

		/// <summary>
		/// Event to execute when a stringer is erased.
		/// </summary>
		private static void On_StringerErase(object sender, ObjectErasedEventArgs e)
		{
			if (_geometries is null || !_geometries.Any() || !(sender is Line str))
				return;

			var geometry = GetGeometry(str, false);

			if (_geometries.Contains(geometry))
			{
				_geometries.Remove(geometry);
				Model.Editor.WriteMessage($"\nRemoved: {geometry}");
			}
		}
	}
}