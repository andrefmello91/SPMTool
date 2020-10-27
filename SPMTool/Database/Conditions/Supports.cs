using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Extensions.AutoCAD;
using SPM.Elements;
using SPMTool.Database;
using SPMTool.Editor;
using SPMTool.Enums;
using SPMTool.Database.Conditions;
using UnitsNet.Units;

namespace SPMTool.Database.Conditions
{
	/// <summary>
    /// Supports class.
    /// </summary>
    public static class Supports
    {
	    /// <summary>
	    /// Auxiliary list of support blocks.
	    /// </summary>
	    private static List<BlockReference> _supportList;

        /// <summary>
        /// Get the support objects in the drawing.
        /// </summary>
        public static IEnumerable<BlockReference> GetObjects() => Layer.Support.GetDBObjects()?.ToBlocks();

        /// <summary>
        /// Add the force blocks to the model.
        /// </summary>
        /// <param name="positions">The collection of nodes to add.</param>
        /// <param name="constraint">The <see cref="Constraint"/> type.</param>
        /// <param name="geometryUnit">The <see cref="LengthUnit"/> of geometry.</param>
        public static void AddBlocks(IReadOnlyCollection<Point3d> positions, Constraint constraint)
        {
            if (positions is null || positions.Count == 0)
                return;

			// Get units
			var units = UnitsData.SavedUnits;

            // Start a transaction
            using (var trans = DataBase.StartTransaction())
            using (var blkTbl = (BlockTable)trans.GetObject(DataBase.Database.BlockTableId, OpenMode.ForRead))
            {
                // Read the force block
                var supBlock = blkTbl[BlockName(constraint)];

                foreach (var pos in positions)
	                // Insert the block into the current space
	                using (var blkRef = new BlockReference(pos, supBlock))
	                {
		                blkRef.Layer = $"{Layer.Support}";
		                blkRef.Add();

		                // Set scale to the block
		                if (units.Geometry != LengthUnit.Millimeter)
			                blkRef.TransformBy(Matrix3d.Scaling(units.ScaleFactor, pos));

		                // Set XData
		                blkRef.SetXData(SupportXData(constraint));
	                }

                trans.Commit();
            }
        }

        /// <summary>
        /// Update support list.
        /// </summary>
        public static void Update() => _supportList = GetObjects()?.ToList();

        /// <summary>
        /// Erase the supports blocks in the model.
        /// </summary>
        /// <param name="positions">The collection of nodes in the model.</param>
        public static void EraseBlocks(IReadOnlyCollection<Point3d> positions)
        {
	        if (positions is null || positions.Count == 0)
		        return;

	        // Get all the force blocks in the model
	        var sups = Model.SupportCollection?.ToArray();

	        if (sups is null || sups.Length == 0)
		        return;

            // Erase blocks in positions
			var toErase = new List<DBObject>();

            foreach (var position in positions)
				toErase.AddRange(sups.Where(sup => sup.Position.Approx(position)));

			toErase.Remove();
        }

        /// <summary>
        /// Get the block name.
        /// </summary>
        /// <param name="constraint">The <see cref="Constraint"/> type.</param>
        private static string BlockName(Constraint constraint)
        {
	        switch (constraint)
	        {
                case Constraint.X:
	                return $"{Block.SupportX}";

                case Constraint.Y:
	                return $"{Block.SupportY}";

                case Constraint.XY:
	                return $"{Block.SupportXY}";

				default:
					return null;
            }
        }

        /// <summary>
        /// Create support blocks.
        /// </summary>
        public static void CreateBlocks()
        {
            // Start a transaction
            using (var trans = DataBase.StartTransaction())
            using (var blkTbl = (BlockTable)trans.GetObject(DataBase.BlockTableId, OpenMode.ForRead))
            {
                // Check if the support blocks already exist in the drawing
				CreateBlockX();
				CreateBlockY();
				CreateBlockXY();

                // Commit and dispose the transaction
                trans.Commit();

                void CreateBlockX()
                {
                    if (blkTbl.Has($"{Block.SupportX}"))
                        return;

                    // Create the X block
                    using (var blkTblRec = new BlockTableRecord())
                    {
                        blkTblRec.Name = $"{Block.SupportX}";

                        // Add the block table record to the block table and to the transaction
                        blkTbl.UpgradeOpen();
                        blkTbl.Add(blkTblRec);
                        trans.AddNewlyCreatedDBObject(blkTblRec, true);

                        // Set the insertion point for the block
                        var origin = new Point3d(0, 0, 0);
                        blkTblRec.Origin = origin;

                        // Create a object collection and add the lines
                        using (var lines = new DBObjectCollection())
                        {
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
                            for (int i = 0; i < 4; i++)
                                lines.Add(new Line
                                {
                                    StartPoint = blkPts[2 * i],
                                    EndPoint = blkPts[2 * i + 1]
                                });

                            // Add the lines to the block table record
                            foreach (Entity ent in lines)
                            {
                                blkTblRec.AppendEntity(ent);
                                trans.AddNewlyCreatedDBObject(ent, true);
                            }
                        }
                    }
                }

                void CreateBlockY()
                {
                    if (blkTbl.Has($"{Block.SupportY}"))
                        return;

                    // Create the Y block
                    using (var blkTblRec = new BlockTableRecord())
                    {
                        blkTblRec.Name = $"{Block.SupportY}";

                        // Set the insertion point for the block
                        var origin = new Point3d(0, 0, 0);
                        blkTblRec.Origin = origin;

                        // Add the block table record to the block table and to the transaction
                        blkTbl.UpgradeOpen();
                        blkTbl.Add(blkTblRec);
                        trans.AddNewlyCreatedDBObject(blkTblRec, true);

                        // Create a object collection and add the lines
                        using (var lines = new DBObjectCollection())
                        {
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
                            for (int i = 0; i < 4; i++)
                                lines.Add(new Line()
                                {
                                    StartPoint = blkPts[2 * i],
                                    EndPoint = blkPts[2 * i + 1]
                                });

                            // Add the lines to the block table record
                            foreach (Entity ent in lines)
                            {
                                blkTblRec.AppendEntity(ent);
                                trans.AddNewlyCreatedDBObject(ent, true);
                            }
                        }
                    }
                }

                void CreateBlockXY()
                {
                    if (blkTbl.Has($"{Block.SupportXY}"))
                        return;

                    // Create the XY block
                    using (var blkTblRec = new BlockTableRecord())
                    {
                        blkTblRec.Name = $"{Block.SupportXY}";

                        // Add the block table record to the block table and to the transaction
                        blkTbl.UpgradeOpen();
                        blkTbl.Add(blkTblRec);
                        trans.AddNewlyCreatedDBObject(blkTblRec, true);

                        // Set the insertion point for the block
                        var origin = new Point3d(0, 0, 0);
                        blkTblRec.Origin = origin;

                        // Create a object collection and add the lines
                        using (var lines = new DBObjectCollection())
                        {
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
                            for (int i = 0; i < 3; i++)
                                lines.Add(new Line
                                {
                                    StartPoint = blkPts[2 * i],
                                    EndPoint   = blkPts[2 * i + 1]
                                });

                            // Create the diagonal lines
                            for (int i = 0; i < 6; i++)
                            {
                                int xInc = 23 * i; // distance between the lines

                                // Add to the collection
                                lines.Add(new Line
                                {
                                    StartPoint = new Point3d(-57.5 + xInc, -100, 0),
                                    EndPoint   = new Point3d(-70 + xInc, -122.5, 0)
                                });
                            }

                            // Add the lines to the block table record
                            foreach (Entity ent in lines)
                            {
                                blkTblRec.AppendEntity(ent);
                                trans.AddNewlyCreatedDBObject(ent, true);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Create XData for supports.
        /// </summary>
        /// <param name="constraint">The <see cref="Constraint"/> type.</param>
        private static TypedValue[] SupportXData(Constraint constraint)
        {
            // Definition for the Extended Data
            string xdataStr = "SupportDirection Data";

            // Get the Xdata size
            int size = Enum.GetNames(typeof(SupportIndex)).Length;
            var data = new TypedValue[size];

            // Set values
            data[(int)SupportIndex.AppName]   = new TypedValue((int)DxfCode.ExtendedDataRegAppName, DataBase.AppName);
            data[(int)SupportIndex.XDataStr]  = new TypedValue((int)DxfCode.ExtendedDataAsciiString, xdataStr);
            data[(int)SupportIndex.Direction] = new TypedValue((int)DxfCode.ExtendedDataInteger32, (int)constraint);

            // Add XData to force block
            return data;
        }

        /// <summary>
        /// Set supports to a collection of nodes.
        /// </summary>
        /// <param name="nodes">The collection containing all nodes of SPM model.</param>
        public static void Set(IEnumerable<Node> nodes)
        {
	        foreach (var node in nodes)
		        Set(node);
        }

        /// <summary>
        /// Set support to a node.
        /// </summary>
        /// <param name="node">The node.</param>
        public static void Set(Node node)
        {
	        // Get forces at node position
	        if (_supportList is null)
		        Update();

	        var i = _supportList?.FindIndex(s => s.Position == node.Position);

	        if (i is null || i == -1)
		        return;

	        // Set to node
	        node.Constraint = ReadConstraint(_supportList[i.Value]);
        }

        /// <summary>
        /// Read a <see cref="Constraint"/> from an object in the drawing.
        /// </summary>
        /// <param name="objectId">The <see cref="ObjectId"/> of support object in the drawing.</param>
        public static Constraint ReadConstraint(ObjectId objectId) => ReadConstraint((BlockReference) objectId.ToDBObject());

        /// <summary>
        /// Read a <see cref="Constraint"/> from an object in the drawing.
        /// </summary>
        /// <param name="supportBlock">The <see cref="BlockReference"/> of support object in the drawing.</param>
        public static Constraint ReadConstraint(BlockReference supportBlock) => (Constraint)supportBlock.ReadXData()[(int)SupportIndex.Direction].ToInt();
    }
}
