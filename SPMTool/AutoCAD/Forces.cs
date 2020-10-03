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
using SPMTool.Database;
using SPMTool.Model;
using SPM.Elements;
using UnitsNet.Units;
using OnPlaneComponents;
using ForceTextData  = SPMTool.XData.ForceText;
using ForceData      = SPMTool.XData.Force;

[assembly: CommandClass(typeof(SPMTool.AutoCAD.Forces))]

namespace SPMTool.AutoCAD
{
    public static class Forces
    {
        // Layer and block names
        public static readonly string
	        ForceLayer = Layer.Force.ToString(),
			TxtLayer   = Layer.ForceText.ToString(),
			BlockName  = Block.ForceBlock.ToString();

        [CommandMethod("AddForce")]
        public static void AddForce()
        {
            // Check if the layer Force and ForceText already exists in the drawing. If it doesn't, then it's created:
            Auxiliary.CreateLayer(Layer.Force, Color.Yellow);
            Auxiliary.CreateLayer(Layer.ForceText, Color.Yellow);

			// Read units
			var units = DataBase.Units;

            // Check if the force block already exist. If not, create the blocks
            CreateForceBlock();

            // Request objects to be selected in the drawing area
            using (var nds = UserInput.SelectNodes("Select nodes to add load:", NodeType.External))
            {
	            if (nds is null)
		            return;

	            // Get force from user
	            var force = GetForceValue(units.AppliedForces);

	            if (!force.HasValue)
		            return;

	            // Get node positions
	            var positions = (from DBPoint nd in nds select nd.Position).ToArray();


	            // Erase blocks
	            EraseForceBlocks(positions);

	            // Add force blocks
	            AddForceBlocks(positions, force.Value, units.Geometry);
            }
        }

		/// <summary>
        /// Get the force values from user.
        /// </summary>
        /// <param name="forceUnit">The current <see cref="ForceUnit"/>.</param>
        private static Force? GetForceValue(ForceUnit forceUnit)
        {
	        var fAbrev = forceUnit.Abbrev();

            // Ask the user set the load value in x direction:
            var xFn = UserInput.GetDouble($"Enter force (in {fAbrev}) in X direction(positive following axis direction)?", 0, true, true);

	        if (!xFn.HasValue)
		        return null;

	        // Ask the user set the load value in y direction:
	        var yFn = UserInput.GetDouble($"Enter force (in {fAbrev}) in Y direction(positive following axis direction)?", 0, true, true);

	        if (!yFn.HasValue)
		        return null;

	        return new Force(xFn.Value, yFn.Value, forceUnit);
        }

		/// <summary>
        /// Add the force blocks to the model.
        /// </summary>
        /// <param name="positions">The collection of nodes to add</param>
        /// <param name="force"></param>
        /// <param name="geometryUnit"></param>
		private static void AddForceBlocks(IReadOnlyCollection<Point3d> positions, Force force, LengthUnit geometryUnit)
		{
			if (positions is null || positions.Count == 0)
				return;

			// Get scale factor
			var scFctr = GlobalAuxiliary.ScaleFactor(geometryUnit);

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
						AddForceBlock(force.ComponentX, Directions.X);
					
					// For forces in y
					if (!force.IsComponentYZero)
						AddForceBlock(force.ComponentY, Directions.Y);
					
					void AddForceBlock(double forceValue, Directions direction)
					{
						// Get rotation angle and the text position
						var rotAng = direction is Directions.X
							? forceValue > 0 ? Constants.PiOver2 : -Constants.PiOver2
							: forceValue > 0 ? Constants.Pi : 0;

						var txtPos = direction is Directions.X
							? forceValue > 0 ? new Point3d(xPos - 200 * scFctr, yPos + 25 * scFctr, 0) : new Point3d(xPos + 75 * scFctr, yPos + 25 * scFctr, 0)
							: forceValue > 0 ? new Point3d(xPos + 25 * scFctr, yPos - 125 * scFctr, 0) : new Point3d(xPos + 25 * scFctr, yPos + 100 * scFctr, 0);

                        using (var blkRef = new BlockReference(pos, forceBlock))
						{
							// Append the block to drawing
							blkRef.Layer = ForceLayer;
							Auxiliary.AddObject(blkRef);

							// Rotate and scale the block
							if (!rotAng.ApproxZero())
								blkRef.TransformBy(Matrix3d.Rotation(rotAng, DataBase.Ucs.Zaxis, pos));

							if (geometryUnit != LengthUnit.Millimeter)
								blkRef.TransformBy(Matrix3d.Scaling(scFctr, pos));

							// Set XData to force block
							blkRef.XData = ForceXData(forceValue.Convert(force.Unit), direction);

							// Define the force text
							var text = new DBText
							{
								TextString = $"{forceValue.Abs():0.00}",
								Position = txtPos,
								Height = 30 * scFctr,
								Layer = TxtLayer
							};

							// Append the text to drawing
							Auxiliary.AddObject(text);

							// Add the node position to the text XData
							text.XData = ForceTextXData(pos, direction);
						}
					}
                }

				trans.Commit();
			}
		}

		/// <summary>
        /// Create the force block.
        /// </summary>
        private static void CreateForceBlock()
        {
            using (var trans = DataBase.StartTransaction())
	        // Open the Block table for read
            using (var blkTbl = (BlockTable)trans.GetObject(DataBase.Database.BlockTableId, OpenMode.ForRead))
            {
                // Initialize the block Ids
                var forceBlock = ObjectId.Null;

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

                        // Set the name
                        forceBlock = blkTblRec.Id;

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
        private static void EraseForceBlocks(IReadOnlyCollection<Point3d> positions)
        {
			if (positions is null || positions.Count == 0)
				return;

	        // Get all the force blocks in the model
	        var fcs    = Model.Model.ForceCollection;

	        // Get all the force texts in the model
	        var fcTxts = Model.Model.ForceTextCollection;

			if (fcs is null && fcTxts is null)
				return;

            // Start a transaction
            using (var trans = DataBase.StartTransaction())
            {
	            foreach (var position in positions)
	            {
		            // Check if there is a force block at the node position
		            if (fcs != null && fcs.Count > 0)
		            {
			            foreach (ObjectId fcObj in fcs)
			            {
				            // Read as a block reference
				            var fcBlk = (BlockReference) trans.GetObject(fcObj, OpenMode.ForRead);

				            // Check if the position is equal to the selected node
				            if (fcBlk.Position != position)
					            continue;

				            // Erase the force block
				            fcBlk.UpgradeOpen();
				            fcBlk.Erase();
			            }
		            }

		            // Check if there is a force text at the node position
		            if (fcTxts is null || fcTxts.Count == 0)
			            continue;

		            foreach (ObjectId txtObj in fcTxts)
		            {
			            // Read as text
			            var txtEnt = (Entity) trans.GetObject(txtObj, OpenMode.ForRead);
			            var txtData = txtEnt.ReadXData(DataBase.AppName);

			            // Get the position of the node of the text
			            double
				            ndX = txtData[(int) ForceTextData.XPosition].ToDouble(),
				            ndY = txtData[(int) ForceTextData.YPosition].ToDouble();

			            var ndTxtPos = new Point3d(ndX, ndY, 0);

			            // Check if the position is equal to the selected node
			            if (ndTxtPos != position)
				            continue;

			            // Erase the text
			            txtEnt.UpgradeOpen();
			            txtEnt.Erase();
		            }
	            }

				trans.Commit();
            }
        }

        /// <summary>
        /// Create XData for forces
        /// </summary>
        /// <param name="forceValue">The force value, in N.</param>
        /// <param name="forceDirection">The force direction.</param>
        /// <returns></returns>
        private static ResultBuffer ForceXData(double forceValue, Directions forceDirection)
        {
            // Definition for the Extended Data
            string xdataStr = "Force Data";

            // Get the Xdata size
            int size  = Enum.GetNames(typeof(ForceData)).Length;
            var fData = new TypedValue[size];

            // Set values
            fData[(int)ForceData.AppName]   = new TypedValue((int)DxfCode.ExtendedDataRegAppName,  DataBase.AppName);
            fData[(int)ForceData.XDataStr]  = new TypedValue((int)DxfCode.ExtendedDataAsciiString, xdataStr);
            fData[(int)ForceData.Value]     = new TypedValue((int)DxfCode.ExtendedDataReal,        forceValue);
            fData[(int)ForceData.Direction] = new TypedValue((int)DxfCode.ExtendedDataInteger32,  (int)forceDirection);

            // Add XData to force block
            return
                new ResultBuffer(fData);
        }

        // Create XData for force text
        private static ResultBuffer ForceTextXData(Point3d forcePosition, Directions forceDirection)
        {
            // Get the Xdata size
            int size = Enum.GetNames(typeof(ForceTextData)).Length;
            var fData = new TypedValue[size];

            // Set values
            fData[(int)ForceTextData.AppName]   = new TypedValue((int)DxfCode.ExtendedDataRegAppName,  DataBase.AppName);
            fData[(int)ForceTextData.XDataStr]  = new TypedValue((int)DxfCode.ExtendedDataAsciiString, "Force at nodes");
            fData[(int)ForceTextData.XPosition] = new TypedValue((int)DxfCode.ExtendedDataReal,         forcePosition.X);
            fData[(int)ForceTextData.YPosition] = new TypedValue((int)DxfCode.ExtendedDataReal,         forcePosition.Y);
            fData[(int)ForceTextData.Direction] = new TypedValue((int)DxfCode.ExtendedDataInteger32,   (int)forceDirection);

            // Add XData to force block
            return
                new ResultBuffer(fData);
        }


        // Toggle view for stringers
        [CommandMethod("ToogleForces")]
        public static void ToogleForces()
        {
	        Auxiliary.ToogleLayer(Layer.Force);
	        Auxiliary.ToogleLayer(Layer.ForceText);
        }

    }
}
