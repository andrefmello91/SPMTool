using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Extensions.AutoCAD;
using Extensions;
using MathNet.Numerics;
using SPM.Elements;
using UnitsNet.Units;
using OnPlaneComponents;
using SPMTool.ApplicationSettings;
using SPMTool.Enums;
using SPMTool.Extensions;

namespace SPMTool.Database.Conditions
{
    public static class Forces
    {
	    /// <summary>
	    /// Get/set the list of force blocks.
	    /// </summary>
	    public static List<BlockReference> ForceList { get; private set; } = GetObjects().ToList();

		/// <summary>
		/// Get force blocks' positions.
		/// </summary>
	    public static List<Point3d> ForcePositions => ForceList.Select(f => f.Position).ToList();

	    /// <summary>
	    /// Get the elements of the force block.
	    /// </summary>
	    public static Entity[] BlockElements => new Entity[]
	    {
		    new Line
		    {
			    StartPoint = new Point3d(0, 37.5, 0),
			    EndPoint   = new Point3d(0, 125, 0)
		    },

		    new Solid(new Point3d(0, 0, 0), new Point3d(-25, 37.5, 0), new Point3d(25, 37.5, 0))
	    };

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
        /// <param name="planeForce"></param>
        public static void AddBlocks(IReadOnlyCollection<Point3d> positions, PlaneForce planeForce)
		{
			if (positions is null || positions.Count == 0)
				return;
            
			// Get units
			var units = Settings.Units;

			// Get scale factor
			var scFctr = units.ScaleFactor;

			// Start a transaction
			using (var trans = DataBase.StartTransaction())
				// Open the Block table for read
			using (var blkTbl = (BlockTable) trans.GetObject(DataBase.Database.BlockTableId, OpenMode.ForRead))
			{
				// Read the force block
				var forceBlock = blkTbl[$"{Block.Force}"];

				foreach (var pos in positions)
				{
					double
						xPos = pos.X,
						yPos = pos.Y;

					// Insert the block into the current space
					// For forces in x
					if (!planeForce.IsComponentXZero)
						AddForceBlock(planeForce.ComponentX, Direction.X);
					
					// For forces in y
					if (!planeForce.IsComponentYZero)
						AddForceBlock(planeForce.ComponentY, Direction.Y);
					
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
							blkRef.AddToDrawing(On_ForceErase, trans);

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
							text.AddToDrawing(On_ForceTextErase, trans);

							// Add the node position to the text XData
							text.SetXData(ForceTextXData(blkRef.Handle));

                            // Set XData to force block
                            blkRef.SetXData(ForceXData(forceValue.Convert(planeForce.Unit), direction, text.Handle));
						}
					}
                }

				trans.Commit();
			}
		}

        /// <summary>
        /// Update force list.
        /// </summary>
        public static void Update() => ForceList = GetObjects()?.ToList();

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
			toErase.RemoveFromDrawing();
        }

        /// <summary>
        /// Get the <see cref="Entity"/> associated to this <paramref name="forceBlock"/>.
        /// </summary>
        /// <param name="forceBlock">The force block.</param>
        private static ObjectId AssociatedText(Entity forceBlock) => new Handle(Convert.ToInt64(forceBlock.ReadXData()[(int) ForceIndex.TextHandle].Value.ToString(), 16)).GetObjectId();

        /// <summary>
        /// Get the <see cref="Entity"/> associated to this <paramref name="forceText"/>.
        /// </summary>
        /// <param name="forceText">The force block.</param>
        private static ObjectId AssociatedBlock(Entity forceText) => new Handle(Convert.ToInt64(forceText.ReadXData()[(int) ForceTextIndex.BlockHandle].Value.ToString(), 16)).GetObjectId();

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
	        Update();

			var fcs = ForceList?.Where(f => f.Position == node.Position).ToArray();

			if (fcs is null || !fcs.Any())
				return;

			// Set to node
			foreach (var fc in fcs)
				node.PlaneForce += ReadForce(fc);
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
	        text.RemoveFromDrawing();

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
            block.RemoveFromDrawing();

			// Update forces
			Update();
		}
    }
}
