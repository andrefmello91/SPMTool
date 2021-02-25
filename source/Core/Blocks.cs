using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using SPMTool.Enums;
using SPMTool.Extensions;

namespace SPMTool.Core
{
	/// <summary>
	///     Class for block creation.
	/// </summary>
	public static class Blocks
	{
		#region  Methods

		/// <summary>
		///     Get the elements of the crack block.
		/// </summary>
		[Block(Block = Block.StringerCrack)]
		public static IEnumerable<Entity> StringerCrackBlockElements()
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
		///     Get the elements of the compressive block.
		/// </summary>
		[Block(Block = Block.CompressiveStress)]
		public static IEnumerable<Entity> PanelCompressiveBlockElements()
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
		[Block(Block = Block.PanelCrack)]
		public static IEnumerable<Entity> PanelCrackBlockElements()
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
		[Block(Block = Block.Shear)]
		public static IEnumerable<Entity> PanelShearBlockElements()
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
		[Block(Block = Block.TensileStress)]
		public static IEnumerable<Entity> PanelTensileBlockElements()
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
		///     Get the elements of the force block.
		/// </summary>
		[Block(Block = Block.Force)]
		public static IEnumerable<Entity> ForceBlockElements()
		{
			yield return
				new Line
				{
					StartPoint = new Point3d(0, 37.5, 0),
					EndPoint   = new Point3d(0, 125, 0)
				};

			yield return
				new Solid(new Point3d(0, 0, 0), new Point3d(-25, 37.5, 0), new Point3d(25, 37.5, 0));
		}

		/// <summary>
		///     Get the elements of X Block.
		/// </summary>
		[Block(Block = Block.SupportX)]
		public static IEnumerable<Entity> SupportXElements()
		{
			var origin = new Point3d(0, 0, 0);

			// Define the points to add the lines
			Point3d[] blkPts =
			{
				origin,
				new Point3d(-100, 57.5,  0),
				origin,
				new Point3d(-100, -57.5, 0),
				new Point3d(-100,  75,   0),
				new Point3d(-100, -75,   0),
				new Point3d(-125,  75,   0),
				new Point3d(-125, -75,   0)
			};

			// Define the lines and add to the collection
			for (var i = 0; i < 4; i++)
				yield return new Line
				{
					StartPoint = blkPts[2 * i],
					EndPoint = blkPts[2 * i + 1]
				};
		}

		/// <summary>
		///     Get the elements of Y Block.
		/// </summary>
		public static IEnumerable<Entity> SupportYElements()
		{
			var origin = new Point3d(0, 0, 0);

			// Define the points to add the lines
			Point3d[] blkPts =
			{
				origin,
				new Point3d(-57.5, -100, 0),
				origin,
				new Point3d( 57.5, -100, 0),
				new Point3d(-75,   -100, 0),
				new Point3d( 75,   -100, 0),
				new Point3d(-75,   -125, 0),
				new Point3d( 75,   -125, 0)
			};

			// Define the lines and add to the collection
			for (var i = 0; i < 4; i++)
				yield return new Line
				{
					StartPoint = blkPts[2 * i],
					EndPoint = blkPts[2 * i + 1]
				};
		}

		/// <summary>
		///     Get the elements of XY Block.
		/// </summary>
		[Block(Block = Block.SupportXY)]
		public static IEnumerable<Entity> SupportXYElements()
		{
			var origin = new Point3d(0, 0, 0);

			// Define the points to add the lines
			Point3d[] blkPts =
			{
				origin,
				new Point3d(-57.5, -100, 0),
				origin,
				new Point3d( 57.5, -100, 0),
				new Point3d(-75,   -100, 0),
				new Point3d( 75,   -100, 0)
			};

			// Define the lines and add to the collection
			for (var i = 0; i < 3; i++)
				yield return new Line
				{
					StartPoint = blkPts[2 * i],
					EndPoint   = blkPts[2 * i + 1]
				};

			// Create the diagonal lines
			for (var i = 0; i < 6; i++)
			{
				var xInc = 23 * i; // distance between the lines

				// Add to the collection
				yield return new Line
				{
					StartPoint = new Point3d(-57.5 + xInc,   -100, 0),
					EndPoint   = new Point3d(-70   + xInc, -122.5, 0)
				};
			}
		}

		/// <summary>
		///     Create blocks for use in SPMTool.
		/// </summary>
		public static void CreateBlocks()
		{
			// Get the block enum as an array
			var blocks = Enum.GetValues(typeof(Block)).Cast<Block>().ToArray();

			// Create the blocks
			blocks.Create();
		}

		#endregion
	}
}