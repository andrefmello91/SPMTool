using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Extensions;
using OnPlaneComponents;
using SPMTool.Enums;
using SPMTool.Extensions;

namespace SPMTool.Core.Conditions
{
	/// <summary>
	///     Supports class.
	/// </summary>
	public class ConstraintList : ConditionList<ConstraintObject, Constraint>
	{
		#region Constructors

		private ConstraintList()
		{
		}

		private ConstraintList(IEnumerable<ConstraintObject> constraints)
			: base(constraints)
		{
		}

		#endregion

		#region  Methods

		/// <summary>
		///     Get the elements of X Block.
		/// </summary>
		public static IEnumerable<Entity> XElements()
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
		public static IEnumerable<Entity> YElements()
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
		public static IEnumerable<Entity> XYElements()
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
		///     Get the support objects in the drawing.
		/// </summary>
		public static IEnumerable<BlockReference>? GetObjects() => Layer.Support.GetDBObjects()?.ToBlocks();

		/// <summary>
		///     Read all <see cref="ConstraintObject" />'s from drawing.
		/// </summary>
		public static ConstraintList ReadFromDrawing() => ReadFromBlocks(GetObjects());

		/// <summary>
		///     Read <see cref="ConstraintObject" />'s from a collection of <see cref="BlockReference" />'s.
		/// </summary>
		/// <param name="blocks">The collection containing the <see cref="BlockReference" />'s of drawing.</param>
		[return:NotNull]
		public static ConstraintList ReadFromBlocks(IEnumerable<BlockReference>? blocks) =>
			blocks.IsNullOrEmpty()
				? new ConstraintList()
				: new ConstraintList(blocks.Where(b => !(b is null)).Select(ConstraintObject.ReadFromBlock)!);

		public override bool Add(Point position, Constraint value, bool raiseEvents = true, bool sort = true) =>
			Add(new ConstraintObject(position, value), raiseEvents, sort);

		public override int AddRange(IEnumerable<Point> positions, Constraint value, bool raiseEvents = true, bool sort = true) =>
			AddRange(positions.Select(p => new ConstraintObject(p, value)), raiseEvents, sort);

		#endregion

		//     /// <summary>
		//     /// Add the force blocks to the model.
		//     /// </summary>
		//     /// <param name="positions">The collection of nodes to add.</param>
		//     /// <param name="constraint">The <see cref="Constraint"/> type.</param>
		//     public static void AddBlocks(IReadOnlyCollection<Point3d> positions, Constraint constraint)
		//     {
		//         if (positions is null || positions.Count == 0)
		//             return;

		//// Get units
		//var units = Settings.Units;

		//         // Start a transaction
		//         using (var trans = DataBase.StartTransaction())
		//         using (var blkTbl = (BlockTable)trans.GetObject(DataBase.Database.BlockTableId, OpenMode.ForRead))
		//         {
		//             // Read the force block
		//             var supBlock = blkTbl[BlockName(constraint)];

		//             foreach (var pos in positions)
		//              // Insert the block into the current space
		//              using (var blkRef = new BlockReference(pos, supBlock))
		//              {
		//               blkRef.Layer = $"{Layer.Support}";
		//               blkRef.AddToDrawing(null, trans);

		//               // Set scale to the block
		//               if (units.Geometry != LengthUnit.Millimeter)
		//                blkRef.TransformBy(Matrix3d.Scaling(units.ScaleFactor, pos));

		//               // Set XData
		//               blkRef.SetXData(ConstraintObject.CreateXData(constraint));
		//              }

		//             trans.Commit();
		//         }
		//     }

		//  /// <summary>
		//     /// Erase the supports blocks in the model.
		//     /// </summary>
		//     /// <param name="positions">The collection of nodes in the model.</param>
		//     public static void EraseBlocks(IReadOnlyCollection<Point3d> positions)
		//     {
		//      if (positions is null || positions.Count == 0)
		//       return;

		//      // Get all the force blocks in the model
		//      var sups = Model.SupportCollection?.ToArray();

		//      if (sups is null || sups.Length == 0)
		//       return;

		//         // Erase blocks in positions
		//var toErase = new List<DBObject>();

		//         foreach (var position in positions)
		//	toErase.AddRange(sups.Where(sup => sup.Position.Approx(position)));

		//toErase.RemoveFromDrawing();
		//     }

		//     /// <summary>
		//     /// Get the block name.
		//     /// </summary>
		//     /// <param name="constraint">The <see cref="Constraint"/> type.</param>
		//     private static string BlockName(Constraint constraint)
		//     {
		//      switch (constraint)
		//      {
		//             case Constraint.X:
		//              return $"{Block.SupportX}";

		//             case Constraint.Y:
		//              return $"{Block.SupportY}";

		//             case Constraint.XY:
		//              return $"{Block.SupportXY}";

		//	default:
		//		return null;
		//         }
		//     }

		//     /// <summary>
		//     /// Set supports to a collection of nodes.
		//     /// </summary>
		//     /// <param name="nodes">The collection containing all nodes of SPM model.</param>
		//     public static void Set(IEnumerable<Node> nodes)
		//     {
		//      foreach (var node in nodes)
		//       Set(node);
		//     }

		//     /// <summary>
		//     /// Set support to a node.
		//     /// </summary>
		//     /// <param name="node">The node.</param>
		//     public static void Set(Node node)
		//     {
		//      // Get forces at node position
		//      Update();

		//      var i = SupportList?.FindIndex(s => s.Position == node.Position);

		//      if (i is null || i == -1)
		//       return;

		//      // Set to node
		//      node.Constraint = ReadConstraint(SupportList[i.Value]);
		//     }

		//     /// <summary>
		//     /// Read a <see cref="Constraint"/> from an object in the drawing.
		//     /// </summary>
		//     /// <param name="objectId">The <see cref="ObjectId"/> of support object in the drawing.</param>
		//     public static Constraint ReadConstraint(ObjectId objectId) => ReadConstraint((BlockReference) objectId.ToDBObject());

		//     /// <summary>
		//     /// Read a <see cref="Constraint"/> from an object in the drawing.
		//     /// </summary>
		//     /// <param name="supportBlock">The <see cref="BlockReference"/> of support object in the drawing.</param>
		//     public static Constraint ReadConstraint(BlockReference supportBlock) => (Constraint)supportBlock.ReadXData()[(int)SupportIndex.Direction].ToInt();
	}
}