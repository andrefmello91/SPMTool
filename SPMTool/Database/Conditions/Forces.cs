using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Extensions.AutoCAD;
using Extensions.Number;
using MathNet.Numerics;
using SPM.Elements;
using UnitsNet.Units;
using OnPlaneComponents;
using SPMTool.Enums;

namespace SPMTool.Database.Conditions
{
    public static class Forces
    {
	    /// <summary>
        /// Auxiliary list of force blocks.
        /// </summary>
	    private static List<BlockReference> _forceList;

		/// <summary>
        /// Get the force objects in the drawing.
        /// </summary>
	    public static IEnumerable<BlockReference> GetObjects() => Layer.Force.GetDBObjects()?.ToBlocks();

		/// <summary>
        /// Get the force text objects in the drawing.
        /// </summary>
	    public static IEnumerable<DBText> GetTexts() => Layer.ForceText.GetDBObjects()?.ToTexts();

        /// <summary>
        /// Add the force blocks to the model.
        /// </summary>
        /// <param name="positions">The collection of nodes to add</param>
        /// <param name="force"></param>
        public static void AddBlocks(IReadOnlyCollection<Point3d> positions, Force force)
		{
			if (positions is null || positions.Count == 0)
				return;
            
			// Get units
			var units = UnitsData.SavedUnits;

			// Get scale factor
			var scFctr = units.ScaleFactor;

			// Start a transaction
			using (var trans = DataBase.StartTransaction())
				// Open the Block table for read
			using (var blkTbl = (BlockTable) trans.GetObject(DataBase.Database.BlockTableId, OpenMode.ForRead))
			{
				// Read the force block
				var forceBlock = blkTbl[$"{Block.ForceBlock}"];

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
							blkRef.Layer = $"{Layer.Force}";
							blkRef.Add(On_ForceErase);

							// Rotate and scale the block
							if (!rotAng.ApproxZero())
								blkRef.TransformBy(Matrix3d.Rotation(rotAng, DataBase.Ucs.Zaxis, pos));

							if (units.Geometry != LengthUnit.Millimeter)
								blkRef.TransformBy(Matrix3d.Scaling(scFctr, pos));

							// Define the force text
							var text = new DBText
							{
								TextString = $"{forceValue.Abs():0.00}",
								Position = txtPos,
								Height = 30 * scFctr,
								Layer = $"{Layer.ForceText}"
							};

							// Append the text to drawing
							text.Add(On_ForceTextErase);

							// Add the node position to the text XData
							text.SetXData(ForceTextXData(blkRef.Handle));

                            // Set XData to force block
                            blkRef.SetXData(ForceXData(forceValue.Convert(force.Unit), direction, text.Handle));
						}
					}
                }

				trans.Commit();
			}
		}

        /// <summary>
        /// Update force list.
        /// </summary>
        public static void Update() => _forceList = GetObjects()?.ToList();

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
                if (!blkTbl.Has($"{Block.ForceBlock}"))
                {
                    // Create the X block
                    using (var blkTblRec = new BlockTableRecord())
                    {
                        blkTblRec.Name = $"{Block.ForceBlock}";

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

			if (fcs is null || !fcs.Any())
				return;

			var toErase = new List<ObjectId>();

            // Erase force blocks that are located in positions
			foreach (var position in positions)
			{
                var blks = fcs.Where(fc => fc.Position.Approx(position)).ToArray();

                // Get associated texts
                var txts = blks.Select(AssociatedText).ToArray();
				
                // Unregister erased event
                blks.GetObjectIds().UnregisterErasedEvent(On_ForceErase);
				txts.UnregisterErasedEvent(On_ForceTextErase);

                // Add force blocks and texts to erase
                toErase.AddRange(blks.GetObjectIds());
                toErase.AddRange(txts);
            }

			// Erase objects
			toErase.Remove();
        }

        /// <summary>
        /// Get the <see cref="Entity"/> associated to this <paramref name="forceBlock"/>.
        /// </summary>
        /// <param name="forceBlock">The force block.</param>
        private static ObjectId AssociatedText(Entity forceBlock) => new Handle(Convert.ToInt64(forceBlock.ReadXData()[(int) ForceIndex.TextHandle].Value.ToString(), 16)).ToObjectId();

        /// <summary>
        /// Get the <see cref="Entity"/> associated to this <paramref name="forceText"/>.
        /// </summary>
        /// <param name="forceText">The force block.</param>
        private static ObjectId AssociatedBlock(Entity forceText) => new Handle(Convert.ToInt64(forceText.ReadXData()[(int) ForceTextIndex.BlockHandle].Value.ToString(), 16)).ToObjectId();
		
        /// <summary>
		/// Create XData for forces
		/// </summary>
		/// <param name="forceValue">The force value, in N.</param>
		/// <param name="forceDirection">The force direction.</param>
		/// <param name="textHandle">The <see cref="Handle"/> of the text object.</param>
		private static TypedValue[] ForceXData(double forceValue, Direction forceDirection, Handle textHandle)
        {
            // Definition for the Extended Data
            var xdataStr = "Force Data";

            // Get the Xdata size
            var size  = Enum.GetNames(typeof(ForceIndex)).Length;
            var data = new TypedValue[size];

            // Set values
            data[(int)ForceIndex.AppName]    = new TypedValue((int)DxfCode.ExtendedDataRegAppName,  DataBase.AppName);
            data[(int)ForceIndex.XDataStr]   = new TypedValue((int)DxfCode.ExtendedDataAsciiString, xdataStr);
            data[(int)ForceIndex.Value]      = new TypedValue((int)DxfCode.ExtendedDataReal,        forceValue);
            data[(int)ForceIndex.Direction]  = new TypedValue((int)DxfCode.ExtendedDataInteger32,  (int)forceDirection);
            data[(int)ForceIndex.TextHandle] = new TypedValue((int)DxfCode.ExtendedDataHandle,      textHandle);

            // Add XData to force block
            return data;
        }

        // Create XData for force text
        private static TypedValue[] ForceTextXData(Handle blockHandle)
        {
            // Get the Xdata size
            var size = Enum.GetNames(typeof(ForceTextIndex)).Length;
            var data = new TypedValue[size];

            // Set values
            data[(int)ForceTextIndex.AppName]     = new TypedValue((int)DxfCode.ExtendedDataRegAppName,  DataBase.AppName);
            data[(int)ForceTextIndex.XDataStr]    = new TypedValue((int)DxfCode.ExtendedDataAsciiString, "Force at nodes");
            data[(int)ForceTextIndex.BlockHandle] = new TypedValue((int)DxfCode.ExtendedDataHandle,      blockHandle);
			
            // Add XData to force block
            return data;
        }

        /// <summary>
        /// Set forces to a collection of nodes.
        /// </summary>
        /// <param name="nodes">The collection containing all nodes of SPM model.</param>
        public static void Set(IEnumerable<Node> nodes)
        {
	        foreach (var node in nodes)
		        Set(node);
        }

        /// <summary>
        /// Set forces to a node.
        /// </summary>
        /// <param name="node">The node.</param>
        public static void Set(Node node)
        {
			// Get forces at node position
			if (_forceList is null)
				Update();

			var fcs = _forceList?.Where(f => f.Position == node.Position).ToArray();

			if (fcs is null || !fcs.Any())
				return;

			// Set to node
			foreach (var fc in fcs)
				node.Force += ReadForce(fc);
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
	        var force     = UnitsNet.Force.FromNewtons(data[(int)ForceIndex.Value].ToDouble()).ToUnit(UnitsData.SavedUnits.AppliedForces);
	        var direction = (Direction)data[(int)ForceIndex.Direction].ToInt();

	        // Get force
	        return
		        direction == Direction.X ? Force.InX(force) : Force.InY(force);
        }

        /// <summary>
        /// Execute when the force block is erased.
        /// </summary>
        private static void On_ForceErase(object sender, ObjectErasedEventArgs e)
        {
	        var text = AssociatedText((Entity) sender);

			// Remove event handler
			text.UnregisterErasedEvent(On_ForceTextErase);

			// Erase it
	        text.Remove();

			// Update forces
	        Update();
        }

        /// <summary>
        /// Execute when the force text is erased.
        /// </summary>
        private static void On_ForceTextErase(object sender, ObjectErasedEventArgs e)
		{
			var block = AssociatedBlock((Entity)sender);

            // Remove event handler
            block.UnregisterErasedEvent(On_ForceErase);

            // Erase it
            block.Remove();

			// Update forces
			Update();
		}
    }
}
