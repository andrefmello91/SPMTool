using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Extensions;
using Extensions.AutoCAD;
using Extensions.Number;
using MathNet.Numerics;
using SPM.Elements;
using UnitsNet.Units;
using OnPlaneComponents;
using SPMTool.Database;
using SPMTool.Editor;
using SPMTool.Enums;
using SPMTool.Database.Conditions;

namespace SPMTool.Database.Conditions
{
    public static class Forces
    {
        // Layer and block names
        public static readonly string
	        ForceLayer = Layer.Force.ToString(),
			TxtLayer   = Layer.ForceText.ToString(),
			BlockName  = Block.ForceBlock.ToString();

        /// <summary>
        /// Add the force blocks to the model.
        /// </summary>
        /// <param name="positions">The collection of nodes to add</param>
        /// <param name="force"></param>
        /// <param name="geometryUnit"></param>
		public static void AddBlocks(IReadOnlyCollection<Point3d> positions, Force force, LengthUnit geometryUnit)
		{
			if (positions is null || positions.Count == 0)
				return;

			// Get scale factor
			var scFctr = geometryUnit.ScaleFactor();

			// Start a transaction
			using (var trans = DataBase.StartTransaction())
				// Open the Block table for read
			using (var blkTbl = (BlockTable) trans.GetObject(DataBase.Database.BlockTableId, OpenMode.ForRead))
			{
				// Read the force block
				var forceBlock = blkTbl[BlockName];

				foreach (var pos in positions)
				{
					double
						xPos = pos.X,
						yPos = pos.Y;

					// Insert the block into the current space
					// For forces in x
					if (!force.IsComponentXZero)
						AddForceBlock(force.ComponentX, Direction.X);
					
					// For forces in y
					if (!force.IsComponentYZero)
						AddForceBlock(force.ComponentY, Direction.Y);
					
					void AddForceBlock(double forceValue, Direction direction)
					{
						// Get rotation angle and the text position
						var rotAng = direction is Direction.X
							? forceValue > 0 ? Constants.PiOver2 : -Constants.PiOver2
							: forceValue > 0 ? Constants.Pi : 0;

						var txtPos = direction is Direction.X
							? forceValue > 0 ? new Point3d(xPos - 200 * scFctr, yPos + 25 * scFctr, 0) : new Point3d(xPos + 75 * scFctr, yPos + 25 * scFctr, 0)
							: forceValue > 0 ? new Point3d(xPos + 25 * scFctr, yPos - 125 * scFctr, 0) : new Point3d(xPos + 25 * scFctr, yPos + 100 * scFctr, 0);

                        using (var blkRef = new BlockReference(pos, forceBlock))
						{
							// Append the block to drawing
							blkRef.Layer = ForceLayer;
							blkRef.Add();

							// Rotate and scale the block
							if (!rotAng.ApproxZero())
								blkRef.TransformBy(Matrix3d.Rotation(rotAng, DataBase.Ucs.Zaxis, pos));

							if (geometryUnit != LengthUnit.Millimeter)
								blkRef.TransformBy(Matrix3d.Scaling(scFctr, pos));

							// Set XData to force block
							blkRef.SetXData(ForceXData(forceValue.Convert(force.Unit), direction));

							// Define the force text
							var text = new DBText
							{
								TextString = $"{forceValue.Abs():0.00}",
								Position = txtPos,
								Height = 30 * scFctr,
								Layer = TxtLayer
							};

							// Append the text to drawing
							text.Add();

							// Add the node position to the text XData
							text.SetXData(ForceTextXData(pos, direction));
						}
					}
                }

				trans.Commit();
			}
		}

		/// <summary>
        /// Create the force block.
        /// </summary>
        public static void CreateBlock()
        {
            using (var trans = DataBase.StartTransaction())
	        // Open the Block table for read
            using (var blkTbl = (BlockTable)trans.GetObject(DataBase.Database.BlockTableId, OpenMode.ForRead))
            {
                // Check if the support blocks already exist in the drawing
                if (!blkTbl.Has(BlockName))
                {
                    // Create the X block
                    using (var blkTblRec = new BlockTableRecord())
                    {
                        blkTblRec.Name = BlockName;

                        // Add the block table record to the block table and to the transaction
                        blkTbl.UpgradeOpen();
                        blkTbl.Add(blkTblRec);
                        trans.AddNewlyCreatedDBObject(blkTblRec, true);

                        // Set the insertion point for the block
                        var origin = new Point3d(0, 0, 0);
                        blkTblRec.Origin = origin;

                        // Create a collection
                        using (var arrow = new DBObjectCollection())
                        {
                            // Create the arrow line and solid)
                            var line = new Line
                            {
                                StartPoint = new Point3d(0, 37.5, 0),
                                EndPoint   = new Point3d(0, 125, 0)
                            };
                            // Add to the collection
                            arrow.Add(line);

                            // Create the solid and add to the collection
                            var solid = new Solid(origin, new Point3d(-25, 37.5, 0), new Point3d(25, 37.5, 0));
                            arrow.Add(solid);

                            // Add the lines to the block table record
                            foreach (Entity ent in arrow)
                            {
                                blkTblRec.AppendEntity(ent);
                                trans.AddNewlyCreatedDBObject(ent, true);
                            }
                        }
                    }
                }

                // Commit and dispose the transaction
                trans.Commit();
            }
        }

		/// <summary>
        /// Erase the force blocks and texts in the model.
        /// </summary>
        /// <param name="positions">The collection of nodes in the model.</param>
        public static void EraseBlocks(IReadOnlyCollection<Point3d> positions)
        {
			if (positions is null || positions.Count == 0)
				return;

	        // Get all the force blocks in the model
	        var fcs    = Model.ForceCollection?.ToArray();

	        // Get all the force texts in the model
	        var fcTxts = Model.ForceTextCollection?.ToArray();

			if (fcs is null && fcTxts is null)
				return;

			var toErase = new List<DBObject>();

            // Erase force blocks that are located in positions
			if (fcs != null)
				foreach (var position in positions)
					// Add force blocks
					toErase.AddRange(fcs.Where(fc => fc.Position.Approx(position)));

			if (fcTxts != null && fcTxts.Length > 0)
				foreach (var fcTxt in fcTxts)
				{
					var pt = AssociatedPoint(fcTxt);

					if (pt.HasValue)
						toErase.AddRange(from position in positions where position.Approx(pt.Value) select fcTxt);
				}

			// Erase objects
			toErase.Erase();
        }

		/// <summary>
        /// Get the position associated to this <paramref name="forceText"/>.
        /// </summary>
        /// <param name="forceText">The <see cref="DBText"/>.</param>
		private static Point3d? AssociatedPoint(DBText forceText)
		{
			var txtData = forceText?.ReadXData();

			if (txtData is null)
				return null;

			// Get the position of the node of the text
			double
				ndX = txtData[(int)ForceTextIndex.XPosition].ToDouble(),
				ndY = txtData[(int)ForceTextIndex.YPosition].ToDouble();

			return new Point3d(ndX, ndY, 0);
		}


        /// <summary>
        /// Create XData for forces
        /// </summary>
        /// <param name="forceValue">The force value, in N.</param>
        /// <param name="forceDirection">The force direction.</param>
        /// <returns></returns>
        private static TypedValue[] ForceXData(double forceValue, Direction forceDirection)
        {
            // Definition for the Extended Data
            var xdataStr = "Force Data";

            // Get the Xdata size
            var size  = Enum.GetNames(typeof(ForceIndex)).Length;
            var data = new TypedValue[size];

            // Set values
            data[(int)ForceIndex.AppName]   = new TypedValue((int)DxfCode.ExtendedDataRegAppName,  DataBase.AppName);
            data[(int)ForceIndex.XDataStr]  = new TypedValue((int)DxfCode.ExtendedDataAsciiString, xdataStr);
            data[(int)ForceIndex.Value]     = new TypedValue((int)DxfCode.ExtendedDataReal,        forceValue);
            data[(int)ForceIndex.Direction] = new TypedValue((int)DxfCode.ExtendedDataInteger32,  (int)forceDirection);

            // Add XData to force block
            return data;
        }

        // Create XData for force text
        private static TypedValue[] ForceTextXData(Point3d forcePosition, Direction forceDirection)
        {
            // Get the Xdata size
            var size = Enum.GetNames(typeof(ForceTextIndex)).Length;
            var data = new TypedValue[size];

            // Set values
            data[(int)ForceTextIndex.AppName]   = new TypedValue((int)DxfCode.ExtendedDataRegAppName,  DataBase.AppName);
            data[(int)ForceTextIndex.XDataStr]  = new TypedValue((int)DxfCode.ExtendedDataAsciiString, "Force at nodes");
            data[(int)ForceTextIndex.XPosition] = new TypedValue((int)DxfCode.ExtendedDataReal,         forcePosition.X);
            data[(int)ForceTextIndex.YPosition] = new TypedValue((int)DxfCode.ExtendedDataReal,         forcePosition.Y);
            data[(int)ForceTextIndex.Direction] = new TypedValue((int)DxfCode.ExtendedDataInteger32,   (int)forceDirection);

            // Add XData to force block
            return data;
        }

        /// <summary>
        /// Set forces to a collection of nodes.
        /// </summary>
        /// <param name="forceObjectIds">The collection of force objects in the drawing.</param>
        /// <param name="nodes">The collection containing all nodes of SPM model.</param>
        public static void Set(IEnumerable<BlockReference> forceObjectIds, IEnumerable<Node> nodes)
        {
	        foreach (var obj in forceObjectIds)
		        Set(obj, nodes);
        }

        /// <summary>
        /// Set forces to a collection of nodes.
        /// </summary>
        /// <param name="forceObject">The <see cref="BlockReference"/> of force object in the drawing.</param>
        /// <param name="nodes">The collection containing all nodes of SPM model.</param>
        private static void Set(BlockReference forceObject, IEnumerable<Node> nodes)
        {
            // Set to node
            foreach (var node in nodes)
            {
                if (!node.Position.Approx(forceObject.Position))
                    continue;

                node.Force += ReadForce(forceObject);
                break;
            }
        }

        /// <summary>
        /// Read a <see cref="Force"/> from an object in the drawing.
        /// </summary>
        /// <param name="objectId">The <see cref="ObjectId"/> of force object in the drawing.</param>
        public static Force ReadForce(ObjectId objectId) => ReadForce((BlockReference) objectId.ToDBObject());

        /// <summary>
        /// Read a <see cref="Force"/> from an object in the drawing.
        /// </summary>
        /// <param name="forceBlock">The <see cref="BlockReference"/> of force object in the drawing.</param>
        public static Force ReadForce(BlockReference forceBlock)
        {
	        // Read the XData and get the necessary data
	        var data = forceBlock.ReadXData();

	        // Get value and direction
	        var value     = data[(int)ForceIndex.Value].ToDouble();
	        var direction = (Direction)data[(int)ForceIndex.Direction].ToInt();

	        // Get force
	        return
		        direction is Direction.X ? Force.InX(value) : Force.InY(value);
        }
    }
}
