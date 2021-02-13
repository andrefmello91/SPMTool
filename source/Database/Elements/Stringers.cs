using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Extensions;
using OnPlaneComponents;
using SPM.Elements;
using SPM.Elements.StringerProperties;
using SPMTool.Enums;
using SPMTool.Extensions;

using static SPMTool.Database.DataBase;
using static SPMTool.Units;

using Force = UnitsNet.Force;

#nullable enable

namespace SPMTool.Database.Elements
{
	/// <summary>
	///     Stringers class.
	/// </summary>
	public class Stringers : SPMObjects<StringerObject, StringerGeometry, Stringer>
	{
		#region Constructors

		private Stringers()
			: base()
		{
		}

		private Stringers(IEnumerable<StringerObject> stringerObjects)
			: base(stringerObjects)
		{
		}

		#endregion

		#region  Methods

		/// <summary>
		///     Get the elements of the crack block.
		/// </summary>
		public static IEnumerable<Entity> CrackBlockElements()
		{
			// Define the points to add the lines
			var crkPts = CrackPoints().ToArray();

			IEnumerable<Point3d> CrackPoints()
			{
				for (var i = 0; i < 2; i++)
				{
					// Set the start X coordinate
					double y = 40 * i;

					yield return new Point3d(0, y, 0);
					yield return new Point3d( 1.7633, y + 10, 0);
					yield return new Point3d(-1.7633, y + 30, 0);
				}

				// Add the end point
				yield return new Point3d(0, 80, 0);
			}

			// Define the lines and add to the collection
			for (var i = 0; i < crkPts.Length - 1; i++)
				yield return new Line
				{
					StartPoint = crkPts[i],
					EndPoint   = crkPts[i + 1],
					LineWeight = LineWeight.LineWeight035
				};
		}

		/// <summary>
		///     Get the collection of stringers in the drawing.
		/// </summary>
		public static IEnumerable<Line>? GetObjects() => Layer.Stringer.GetDBObjects()?.ToLines();

		/// <summary>
		///     Read all the <see cref="StringerObject" />'s in the drawing.
		/// </summary>
		public static Stringers ReadFromDrawing() => ReadFromLines(GetObjects());

		/// <summary>
		///     Read <see cref="StringerObject" />'s from a collection of <see cref="Line" />'s.
		/// </summary>
		/// <param name="stringerLines">The collection containing the <see cref="Line" />'s of drawing.</param>
		public static Stringers ReadFromLines(IEnumerable<Line>? stringerLines) =>
			stringerLines.IsNullOrEmpty()
				? new Stringers()
				: new Stringers(stringerLines.Select(StringerObject.ReadFromLine));

		/// <summary>
		///     Draw stringer forces.
		/// </summary>
		/// <param name="stringers">The collection of <see cref="Stringer" />'s.</param>
		/// <param name="maxForce">The maximum stringer force.</param>
		public static void DrawForces(IEnumerable<Stringer> stringers, Force maxForce)
		{
			// Get units
			var units = Settings.Units;

			// Get the scale factor
			var scFctr = units.ScaleFactor;

			foreach (var stringer in stringers)
			{
				// Check if the stringer is loaded
				if (stringer.State is Stringer.ForceState.Unloaded)
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
						dgrm.ColorIndex = Math.Max(N1.Value, N3.Value) > 0 ? (short) Color.Blue1 : (short) Color.Red;

						// Rotate the diagram
						dgrm.TransformBy(Matrix3d.Rotation(ang, DataBase.Ucs.Zaxis, stPt));

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
						dgrm1.ColorIndex = N1.Value > 0 ? (short) Color.Blue1 : (short) Color.Red;

						// Rotate the diagram
						dgrm1.TransformBy(Matrix3d.Rotation(ang, DataBase.Ucs.Zaxis, stPt));

						// Add the diagram to the drawing
						dgrm1.AddToDrawing();
					}

					using (var dgrm3 = new Solid(vrts3[0], vrts3[1], vrts3[2]))
					{
						// Set the layer and transparency
						dgrm3.Layer = $"{Layer.StringerForce}";
						dgrm3.Transparency = 80.Transparency();

						// Set the color (blue to compression and red to tension)
						dgrm3.ColorIndex = N3.Value > 0 ? (short) Color.Blue1 : (short) Color.Red;

						// Rotate the diagram
						dgrm3.TransformBy(Matrix3d.Rotation(ang, DataBase.Ucs.Zaxis, stPt));

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
								? (short) Color.Blue1
								: (short) Color.Red,

							Position = N1.Value > 0
								? new Point3d(stPt.X + 10 * scFctr, stPt.Y + h1 + 20 * scFctr, 0)
								: new Point3d(stPt.X + 10 * scFctr, stPt.Y + h1 - 50 * scFctr, 0)
						};

						// Rotate the text
						txt1.TransformBy(Matrix3d.Rotation(ang, DataBase.Ucs.Zaxis, stPt));

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
							? (short) Color.Blue1
							: (short) Color.Red,

						Position = N3.Value > 0
							? new Point3d(stPt.X + l - 10 * scFctr, stPt.Y + h3 + 20 * scFctr, 0)
							: new Point3d(stPt.X + l - 10 * scFctr, stPt.Y + h3 - 50 * scFctr, 0),

						HorizontalMode = TextHorizontalMode.TextRight
					};

					// Adjust the alignment
					txt3.AlignmentPoint = txt3.Position;

					// Rotate the text
					txt3.TransformBy(Matrix3d.Rotation(ang, DataBase.Ucs.Zaxis, stPt));

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
			using (var trans = DataBase.StartTransaction())
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
							crkTxt.TextString = $"{w.Abs():0.00E+00}";
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
		///     Get the list of <see cref="StringerGeometry" />'s from objects in this collection.
		/// </summary>
		public List<StringerGeometry> GetGeometries() => GetProperties();

		/// <summary>
		///     Update all the stringers in this collection from drawing.
		/// </summary>
		public void Update()
		{
			Clear(false);

			AddRange(ReadFromDrawing(), false);
		}

		/// <inheritdoc cref="EList{T}.Add(T, bool, bool)" />
		/// <param name="startPoint">The start <see cref="Point" />.</param>
		/// <param name="endPoint">The end <see cref="Point" />.</param>
		public bool Add(Point startPoint, Point endPoint, bool raiseEvents = true, bool sort = true)
		{
			// Order points
			var pts = new[] { startPoint, endPoint }.ToList();
			pts.Sort();

			return
				Add(new StringerObject(pts[0], pts[1]), raiseEvents, sort);
		}

		/// <inheritdoc cref="EList{T}.Add(T, bool, bool)" />
		/// <param name="geometry">The <see cref="StringerGeometry" /> to add.</param>
		public bool Add(StringerGeometry geometry, bool raiseEvents = true, bool sort = true) => Add(new StringerObject(geometry), raiseEvents, sort);

		/// <inheritdoc cref="EList{T}.AddRange(IEnumerable{T}, bool, bool)" />
		/// <param name="geometries">The <see cref="StringerGeometry" />'s to add.</param>
		public int AddRange(IEnumerable<StringerGeometry>? geometries, bool raiseEvents = true, bool sort = true) => AddRange(geometries?.Select(g => new StringerObject(g)), raiseEvents, sort);

		/// <inheritdoc cref="EList{T}.Remove(T, bool, bool)" />
		/// <param name="geometry">The <see cref="StringerGeometry" /> to remove from this list.</param>
		public bool Remove(StringerGeometry geometry, bool raiseEvents = true, bool sort = true) => Remove(new StringerObject(geometry), raiseEvents, sort);

		/// <inheritdoc cref="EList{T}.RemoveRange(IEnumerable{T}, bool, bool)" />
		/// <param name="geometries">The <see cref="StringerGeometry" />'s to remove from drawing.</param>
		public int RemoveRange(IEnumerable<StringerGeometry>? geometries, bool raiseEvents = true, bool sort = true) => RemoveRange(geometries.Select(g => new StringerObject(g)), raiseEvents, sort);

		#endregion
	}
}