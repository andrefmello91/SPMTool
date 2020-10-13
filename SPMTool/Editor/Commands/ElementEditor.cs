using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Extensions.AutoCAD;
using Extensions.Number;
using SPM.Elements;
using SPM.Elements.PanelProperties;
using SPM.Elements.StringerProperties;
using SPMTool.Database;
using SPMTool.Database.Elements;
using SPMTool.Editor.Commands;
using SPMTool.Enums;

[assembly: CommandClass(typeof(ElementEditor))]

namespace SPMTool.Editor.Commands
{
    /// <summary>
    /// Element editor command class.
    /// </summary>
    public static class ElementEditor
    {
		/// <summary>
        /// Divide a stringer into new ones.
        /// </summary>
	    [CommandMethod("DivideStringer")]
	    public static void DivideStringer()
	    {
		    // Get units
		    var units = DataBase.Units;

		    // Prompt for select stringers
		    var strs = UserInput.SelectStringers("Select stringers to divide")?.ToArray();

		    if (strs is null)
			    return;

		    // Prompt for the number of segments
		    var numn = UserInput.GetInteger("Enter the number of new stringers:", 2);

		    if (!numn.HasValue)
			    return;

		    int num = numn.Value;

		    // Get the list of start and endpoints
		    var stringerCollection = Stringers.StringerGeometries();

		    // Create lists of points for adding the nodes later
		    List<Point3d> newIntNds = new List<Point3d>(),
			    newExtNds = new List<Point3d>();

            // Create a list to erase the internal nodes
            var ndsToErase = new List<DBObject>();

            // Access the internal nodes in the model
            var intNds = Layer.IntNode.GetDBObjects().ToArray();

		    foreach (var obj in strs)
			    using (var str = (Line) obj)
			    {
				    // Get the coordinates of the initial and end points
				    Point3d
					    strSt  = str.StartPoint,
					    strEnd = str.EndPoint;

				    // Calculate the distance of the points in X and Y
				    double
					    distX = strEnd.DistanceInX(strSt) / num,
					    distY = strEnd.DistanceInY(strSt) / num;

				    // Initialize the start point
				    var stPt = strSt;

				    // Get the midpoint
				    var midPt = strSt.MidPoint(strEnd);

				    // Read the internal nodes to erase
				    ndsToErase.AddRange(intNds.Where(nd => ((DBPoint) nd).Position.Approx(midPt)));
				    
				    // Create the new stringers
				    for (int i = 1; i <= num; i++)
				    {
					    // Get the coordinates of the other points
					    double
						    xCrd = str.StartPoint.X + i * distX,
						    yCrd = str.StartPoint.Y + i * distY;

					    var endPt = new Point3d(xCrd, yCrd, 0);

					    // Create the Stringer
					    Stringers.Add(stPt, endPt, ref stringerCollection, str.XData);

					    // Get the mid point
					    midPt = stPt.MidPoint(endPt);

					    // Add the position of the nodes to the list
					    if (!newExtNds.Contains(stPt))
						    newExtNds.Add(stPt);

					    if (!newExtNds.Contains(endPt))
						    newExtNds.Add(endPt);

					    if (!newIntNds.Contains(midPt))
						    newIntNds.Add(midPt);

					    // Set the start point of the next Stringer
					    stPt = endPt;
				    }

				    // Remove from the list
				    var strList = stringerCollection.ToList();
				    strList.Remove(new StringerGeometry(strSt, strEnd, 0, 0));
				    stringerCollection = strList;
			    }

		    // Erase original stringers and internal nodes
		    strs.Erase();
			ndsToErase.Erase();

		    // Create the nodes
		    Nodes.Add(newExtNds, NodeType.External);
		    Nodes.Add(newIntNds, NodeType.Internal);

		    // Update nodes and stringers
		    Nodes.Update(units.Geometry);
		    Stringers.Update();
	    }

		[CommandMethod("DividePanel")]
		public static void DividePanel()
		{
			// Get units
			var units = DataBase.Units;

			// Prompt for select panels
			var pnls = UserInput.SelectPanels("Select panels to divide");

			if (pnls is null)
				return;

			// Prompt for the number of rows
			var rown = UserInput.GetInteger("Enter the number of rows for division:", 2);

			if (!rown.HasValue)
				return;

			// Prompt for the number of columns
			var clnn = UserInput.GetInteger("Enter the number of columns for division:", 2);

			if (!clnn.HasValue)
				return;

			// Get values
			int 
				row = rown.Value,
				cln = clnn.Value;

			// Get the list of start and endpoints
			var strList = Stringers.StringerGeometries().ToList();

			// Get the list of panels
			var pnlList = Panels.PanelVertices().ToList();

			// Create lists of points for adding the nodes later
			List<Point3d> newIntNds = new List<Point3d>(),
				newExtNds = new List<Point3d>();

			// Create a list of start and end points for adding the stringers later
			var newStrList = new List<(Point3d start, Point3d end)>();

			// Auxiliary rectangular panel error
			var error = false;

			// Create a collection of stringers and nodes to erase
			using (var toErase = new ObjectIdCollection())

				// Access the stringers in the model
			using (var strs = Extensions.GetObjectIds(Layer.Stringer).ToDBObjectCollection())

				// Access the internal nodes in the model
			using (var intNds = Extensions.GetObjectIds(Layer.IntNode).ToDBObjectCollection())
			{
				// Get the selection set and analyse the elements
				foreach (Solid pnl in pnls)
				{
					// Get vertices
					var verts = pnl.GetVertices().ToArray();

					// Get panel geometry
					var geometry = new PanelGeometry(verts, 0, units.Geometry);

					// Verify if the panel is rectangular
					if (geometry.Rectangular) // panel is rectangular
					{
						// Get the surrounding stringers to erase
						foreach (Line str in strs)
						{
							// Read geometry
							var strGeo = Stringers.GetGeometry(str, units.Geometry, false);

							// Verify if the Stringer starts and ends in a panel vertex
							if (!verts.Contains(strGeo.InitialPoint) || !verts.Contains(strGeo.EndPoint))
								continue;

							// Read the internal nodes
							foreach (DBPoint nd in intNds)
								// Erase the internal node and remove from the list
								if (nd.Position.Approx(strGeo.CenterPoint))
									toErase.Add(nd.ObjectId);

							// Erase and remove from the list
							strList.Remove(strGeo);
							toErase.Add(str.ObjectId);
						}

						// Calculate the distance of the points in X and Y
						double
							distX = (geometry.Edge1.Length / cln).ConvertFromMillimeter(units.Geometry),
							distY = (geometry.Edge2.Length / row).ConvertFromMillimeter(units.Geometry);

						// Initialize the start point
						var stPt = verts[0];

						// Create the new panels
						for (int i = 0; i < row; i++)
						{
							for (int j = 0; j < cln; j++)
							{
								// Get the vertices of the panel and add to a list
								var newVerts = new[]
								{
									new Point3d(stPt.X + j * distX, stPt.Y + i * distY, 0),
									new Point3d(stPt.X + (j + 1) * distX, stPt.Y + i * distY, 0),
									new Point3d(stPt.X + j * distX, stPt.Y + (i + 1) * distY, 0),
									new Point3d(stPt.X + (j + 1) * distX, stPt.Y + (i + 1) * distY, 0)
								};

								// Create the panel with XData of the original panel
								Panels.Add(newVerts, units.Geometry, pnl.XData);

								// Add the vertices to the list for creating external nodes
								foreach (var pt in newVerts.Where(pt => !newExtNds.Contains(pt)))
									newExtNds.Add(pt);

								// Create tuples to adding the stringers later
								var strsToAdd = new[]
								{
									(newVerts[0], newVerts[1]),
									(newVerts[0], newVerts[2]),
									(newVerts[2], newVerts[3]),
									(newVerts[1], newVerts[3])
								};

								// Add to the list of new stringers
								foreach (var pts in strsToAdd.Where(pts => !newStrList.Contains(pts)))
									newStrList.Add(pts);
							}
						}

						// Add to objects to erase
						toErase.Add(pnl.ObjectId);

						// Remove from the list
						pnlList.Remove(geometry.Vertices);
					}

					else // panel is not rectangular
					{
						error = true;
						break;
					}
				}

				if (error)
					UserInput.Editor.WriteMessage("\nAt least one selected panel is not rectangular.");
			}

			// Create the stringers
			foreach (var pts in newStrList)
			{
				new Stringers(pts.start, pts.end, strList);

				// Get the midpoint to add the external node
				Point3d midPt = Auxiliary.MidPoint(pts.Item1, pts.Item2);
				if (!newIntNds.Contains(midPt))
					newIntNds.Add(midPt);
			}

			// Create the nodes
			new Nodes(newExtNds, NodeType.External);
			new Nodes(newIntNds, NodeType.Internal);

			// Update the elements
			Nodes.Update(units);
			Stringers.Update();
			Panels.Update();

			// Show an alert for editing stringers
			Application.ShowAlertDialog("Alert: stringers parameters must be set again.");
		}

		/// <summary>
		/// Set geometry to a selection of stringers.
		/// </summary>
		[CommandMethod("SetStringerGeometry")]
		public static void SetStringerGeometry()
		{
			// Read units
			var units = DataBase.Units;

			// Request objects to be selected in the drawing area
			var strs = UserInput.SelectStringers("Select the stringers to assign properties (you can select other elements, the properties will be only applied to stringers)")?.ToArray();

			if (strs is null)
				return;

			// Get geometry
			var geometry = UserInput.GetStringerGeometry(units.Geometry);

			if (!geometry.HasValue)
				return;

			// Start a transaction
			foreach (var str in strs)
				Stringers.SetGeometry(str, geometry.Value);
		}

		/// <summary>
        /// Set geometry to a selection of panels.
        /// </summary>
        [CommandMethod("SetPanelGeometry")]
		public static void SetPanelGeometry()
		{
			// Read units
			var units = DataBase.Units;

			// Request objects to be selected in the drawing area
			var pnls = UserInput.SelectPanels("Select the panels to assign properties (you can select other elements, the properties will be only applied to panels)")?.ToArray();

			if (pnls is null)
				return;

			// Get width
			var wn = UserInput.GetPanelWidth(units.Geometry);

			if (!wn.HasValue)
				return;

			// Start a transaction
			foreach (var pnl in pnls)
				Panels.SetWidth(pnl, wn.Value);
		}

		/// <summary>
		/// Set the reinforcement in a collection of stringers.
		/// </summary>
		[CommandMethod("SetStringerReinforcement")]
		public static void SetStringerReinforcement()
		{
			// Read units
			var units = DataBase.Units;

			// Request objects to be selected in the drawing area
			var strs = UserInput.SelectStringers("Select the stringers to assign reinforcement (you can select other elements, the properties will be only applied to stringers).")?.ToArray();

			if (strs is null)
				return;

			// Get steel parameters and reinforcement from user
			var reinforcement = UserInput.GetUniaxialReinforcement(units);

			if (reinforcement is null)
				return;

			// Save the properties
			foreach (var str in strs)
				Stringers.SetUniaxialReinforcement(str, reinforcement);
		}

		/// <summary>
		/// Set reinforcement to a collection of panels.
		/// </summary>
		[CommandMethod("SetPanelReinforcement")]
		public static void SetPanelReinforcement()
		{
			// Read units
			var units = DataBase.Units;

			// Request objects to be selected in the drawing area
			var pnls = UserInput.SelectPanels("Select the panels to assign reinforcement (you can select other elements, the properties will be only applied to panels).")?.ToArray();

			if (pnls is null)
				return;

			// Get the values
			var refX   = UserInput.GetWebReinforcement(Direction.X, units);
			var refY   = UserInput.GetWebReinforcement(Direction.Y, units);

			if (refX is null && refY is null)
				return;

			foreach (var pnl in pnls)
				Panels.SetReinforcement(pnl, refX, refY);
		}

		/// <summary>
		/// Update all the elements in the drawing.
		/// </summary>
		[CommandMethod("UpdateElements")]
		public static void UpdateElements()
		{
			// Enumerate and get the number of nodes
			var nds = Model.NodeCollection;

			// Update and get the number of stringers
			var strs = Model.StringerCollection;

			// Update and get the number of panels
			var pnls = Model.PanelCollection;

			// Display the number of updated elements
			UserInput.Editor.WriteMessage($"\n{nds.Length} nodes, {strs.Length} stringers and {pnls.Length} panels updated.");
		}
    }
}
