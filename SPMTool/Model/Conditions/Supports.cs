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
using SPMTool.Model.Conditions;
using UnitsNet.Units;

[assembly: CommandClass(typeof(Supports))]

namespace SPMTool.Model.Conditions
{
	/// <summary>
    /// Supports class.
    /// </summary>
    public static class Supports
    {
        // Layer, block and direction names
        private static readonly string
	        SupportLayer = Layer.Support.ToString(),
	        BlockX       = Block.SupportX.ToString(),
	        BlockY       = Block.SupportY.ToString(),
	        BlockXY      = Block.SupportXY.ToString();

        [CommandMethod("AddConstraint")]
        public static void AddConstraint()
        {
	        // Read units
	        var units = DataBase.Units;

            // Request objects to be selected in the drawing area
            using (var nds = UserInput.SelectNodes("Select nodes to add support conditions:"))
            {
	            if (nds is null)
		            return;

	            // Ask the user set the support conditions:
	            var options = Enum.GetNames(typeof(Constraint));

	            var keyword = UserInput.SelectKeyword("Add support in which direction?", options, "Free");

	            if (keyword is null)
		            return;

	            // Set the support
	            var support = (Constraint) Enum.Parse(typeof(Constraint), keyword);

	            // Get positions
	            var positions = (from DBPoint pt in nds select pt.Position).ToArray();

	            // Erase blocks
	            EraseBlocks(positions);

	            // If the node is not Free, add the support blocks
	            if (support != Constraint.Free)
		            AddBlocks(positions, support, units.Geometry);
            }
        }

        /// <summary>
        /// Erase the supports blocks in the model.
        /// </summary>
        /// <param name="positions">The collection of nodes in the model.</param>
        private static void EraseBlocks(IReadOnlyCollection<Point3d> positions)
        {
            if (positions is null || positions.Count == 0)
                return;

            // Get all the force blocks in the model
            var sups = SPMTool.Model.Model.SupportCollection;

            if (sups is null || sups.Count == 0)
                return;

            // Start a transaction
            using (var trans = DataBase.StartTransaction())
            {
                foreach (var position in positions)
	                foreach (ObjectId supObj in sups)
		                using (var supBlk = (BlockReference) trans.GetObject(supObj, OpenMode.ForRead))
		                {
			                // Check if the position is equal to the selected node
			                if (supBlk.Position != position)
				                continue;

			                // Erase the force block
			                supBlk.UpgradeOpen();
			                supBlk.Erase();
		                }

                trans.Commit();
            }
        }

        /// <summary>
        /// Add the force blocks to the model.
        /// </summary>
        /// <param name="positions">The collection of nodes to add.</param>
        /// <param name="constraint">The <see cref="Constraint"/> type.</param>
        /// <param name="geometryUnit">The <see cref="LengthUnit"/> of geometry.</param>
        private static void AddBlocks(IReadOnlyCollection<Point3d> positions, Constraint constraint, LengthUnit geometryUnit)
        {
            if (positions is null || positions.Count == 0)
                return;

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
		                blkRef.Layer = SupportLayer;
		                blkRef.Add();

		                // Set scale to the block
		                if (geometryUnit != LengthUnit.Millimeter)
			                blkRef.TransformBy(Matrix3d.Scaling(geometryUnit.ScaleFactor(), pos));

		                // Set XData
		                blkRef.XData = SupportXData(constraint);
	                }

                trans.Commit();
            }
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
	                return Block.SupportX.ToString();

                case Constraint.Y:
	                return Block.SupportY.ToString();

                case Constraint.XY:
	                return Block.SupportXY.ToString();

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
                    if (blkTbl.Has(BlockX))
                        return;

                    // Create the X block
                    using (var blkTblRec = new BlockTableRecord())
                    {
                        blkTblRec.Name = BlockX;

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
                    if (blkTbl.Has(BlockY))
                        return;

                    // Create the Y block
                    using (var blkTblRec = new BlockTableRecord())
                    {
                        blkTblRec.Name = BlockY;

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
                    if (blkTbl.Has(BlockXY))
                        return;

                    // Create the XY block
                    using (var blkTblRec = new BlockTableRecord())
                    {
                        blkTblRec.Name = BlockXY;

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
                                lines.Add(new Line()
                                {
                                    StartPoint = blkPts[2 * i],
                                    EndPoint = blkPts[2 * i + 1]
                                });

                            // Create the diagonal lines
                            for (int i = 0; i < 6; i++)
                            {
                                int xInc = 23 * i; // distance between the lines

                                // Add to the collection
                                lines.Add(new Line
                                {
                                    StartPoint = new Point3d(-57.5 + xInc, -100, 0),
                                    EndPoint = new Point3d(-70 + xInc, -122.5, 0)
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
        private static ResultBuffer SupportXData(Constraint constraint)
        {
            // Definition for the Extended Data
            string xdataStr = "SupportDirection Data";

            // Get the Xdata size
            int size = Enum.GetNames(typeof(SupportIndex)).Length;
            var sData = new TypedValue[size];

            // Set values
            sData[(int)SupportIndex.AppName]   = new TypedValue((int)DxfCode.ExtendedDataRegAppName, DataBase.AppName);
            sData[(int)SupportIndex.XDataStr]  = new TypedValue((int)DxfCode.ExtendedDataAsciiString, xdataStr);
            sData[(int)SupportIndex.Direction] = new TypedValue((int)DxfCode.ExtendedDataInteger32, (int)constraint);

            // Add XData to force block
            return
                new ResultBuffer(sData);
        }

        // Toggle view for supports
        [CommandMethod("ToogleSupports")]
        public static void ToogleSupports()
        {
	        Layer.Support.Toogle();
        }

        /// <summary>
        /// Set constraints to a collection of nodes.
        /// </summary>
        /// <param name="supportObjectIds">The <see cref="ObjectIdCollection"/> of support objects in the drawing.</param>
        /// <param name="nodes">The collection containing all nodes of SPM model.</param>
        public static void Set(ObjectIdCollection supportObjectIds, IEnumerable<SPM.Elements.Node> nodes)
        {
	        foreach (ObjectId obj in supportObjectIds)
		        Set(obj, nodes);
        }

        /// <summary>
        /// Set constraints to a collection of nodes.
        /// </summary>
        /// <param name="objectId">The <see cref="ObjectId"/> of support object in the drawing.</param>
        /// <param name="nodes">The collection containing all nodes of SPM model.</param>
        private static void Set(ObjectId objectId, IEnumerable<SPM.Elements.Node> nodes)
        {
            // Read object
            using (var sBlock = (BlockReference)objectId.ToDBObject())

                // Set to node
                foreach (var node in nodes)
                {
                    if (!node.Position.Approx(sBlock.Position))
                        continue;

                    node.Constraint = ReadConstraint(sBlock);
                    break;
                }
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
        public static Constraint ReadConstraint(BlockReference supportBlock)
        {
	        // Read the XData and get the necessary data
	        var data = supportBlock.ReadXData();

	        // Get the direction
	        return (Constraint)data[(int)SupportIndex.Direction].ToInt();
        }
    }
}
