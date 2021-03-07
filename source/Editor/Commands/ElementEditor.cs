﻿using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.Runtime;
using Extensions;
using SPMTool.Application.UserInterface;
using SPMTool.Core;
using SPMTool.Core.Elements;
using SPMTool.Editor.Commands;
using SPMTool.Extensions;

using static Autodesk.AutoCAD.ApplicationServices.Core.Application;
using static SPMTool.Core.Model;

[assembly: CommandClass(typeof(ElementEditor))]

namespace SPMTool.Editor.Commands
{
	/// <summary>
	///     Element editor command class.
	/// </summary>
	public static class ElementEditor
	{
		#region  Methods

		/// <summary>
		///     Divide a stringer into new ones.
		/// </summary>
		[CommandMethod("DivideStringer")]
		public static void DivideStringer()
		{
			// Prompt for select stringers
			var strs = UserInput.SelectStringers("Select stringers to divide")?.ToArray();

			if (strs.IsNullOrEmpty())
				return;

			// Prompt for the number of segments
			var numn = UserInput.GetInteger("Enter the number of new stringers:", 2);

			if (!numn.HasValue)
				return;

			var num = numn.Value;

			// Get stringers from list
			var toDivide = Stringers.GetByObjectIds(strs.GetObjectIds().ToArray())?.ToArray();

			if (toDivide.IsNullOrEmpty())
				return;

			// Remove mid nodes
			Nodes.RemoveRange(toDivide.Select(s => s.Geometry.CenterPoint).ToArray());

			// Divide the stringers
			var newStrs = toDivide.SelectMany(s => s.Divide(num)).ToArray();

			// Erase the original stringers
			Stringers.RemoveRange(toDivide);

			// Add the stringers
			Stringers.AddRange(newStrs);

			// Update nodes
			Nodes.Update();
		}

		[CommandMethod("DividePanel")]
		public static void DividePanel()
		{
			// Prompt for select panels
			var pnls = UserInput.SelectPanels("Select panels to divide")?.ToArray();

			if (pnls.IsNullOrEmpty())
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

			// Get the panels and stringers to divide
			var pnlsToDivide = Panels.GetByObjectIds(pnls.GetObjectIds())?.ToArray();

			if (pnlsToDivide.IsNullOrEmpty())
				return;

			// Remove non-rectangular panels
			var nonRecSelected = pnlsToDivide.Any(p => !p.Vertices.IsRectangular);

			if (nonRecSelected)
				pnlsToDivide = pnlsToDivide.Where(p => p.Vertices.IsRectangular).ToArray();

			// Verify if there is at least one panel to divide
			if (!pnlsToDivide.Any())
			{
				ShowAlertDialog("Please select at least one rectangular panel.");
				return;
			}

			// Get the panels divided
			var newPanels = pnlsToDivide.SelectMany(p => p.Divide(row, cln)).ToArray();

			// Get the stringers to divide and divide them
			var strsToDivide = new List<StringerObject>();
			var newStrs      = new List<StringerObject>();

			foreach (var pnl in pnlsToDivide)
			{
				var strEdges = Stringers.GetFromPanelGeometry(pnl.Geometry).ToArray();

				if (strEdges.IsNullOrEmpty())
					continue;

				strsToDivide.AddRange(strEdges.Where(s => !(s is null))!);

				// Divide by correct number
				for (var i = 0; i < 2; i++)
				{
					var j = 2 * i;

					var hor = strEdges[j    ]?.Divide(cln)?.Where(s => !(s is null))?.ToArray();
					var ver = strEdges[j + 1]?.Divide(row)?.Where(s => !(s is null))?.ToArray();

					if (!hor.IsNullOrEmpty())
						newStrs.AddRange(hor);

					if (!ver.IsNullOrEmpty())
						newStrs.AddRange(ver);
				}
			}

			// Remove mid nodes
			Nodes.RemoveRange(strsToDivide.Select(s => s.Geometry.CenterPoint).ToArray());

			// Erase the original elements
			Panels.RemoveRange(pnlsToDivide);
			Stringers.RemoveRange(strsToDivide);

			// Add the elements
			Panels.AddRange(newPanels);
			Stringers.AddRange(newStrs);

			// Update nodes
			Nodes.Update();

			// Show alert if there was a non-rectangular panel
			if (nonRecSelected)
				ShowAlertDialog("Only rectangular panels were divided.");

			//// Get the list of start and endpoints
			//var geoList = StringerList.GetGeometries;

			//// Get the list of panels
			//var pnlList = PanelList.VerticesList;

			//// Create a list of lines for adding the stringers later
			//var newStrList = new List<Line>();

			//// Auxiliary rectangular panel error
			//var error = false;

			//         // Create a collection of stringers and nodes to erase
			//         var toErase = new List<DBObject>();

			//// Access the stringers in the model
			//var strs = Model.StringerCollection;

			//         // Access the internal nodes in the model
			//         var intNds = Model.IntNodeCollection;

			//// Get the selection set and analyze the elements
			//foreach (var pnl in pnls)
			//{
			//	// Get vertices
			//	var verts = pnl.GetVertices().ToArray();

			//	// Get panel geometry
			//	var geometry = new PanelGeometry(verts, 0, units.Geometry);

			//	// Verify if the panel is rectangular
			//	if (geometry.Rectangular) // panel is rectangular
			//	{
			//		// Get the surrounding stringers to erase
			//		foreach (var str in strs)
			//		{
			//			// Verify if the Stringer starts and ends in a panel vertex
			//			if (!verts.Contains(str.StartPoint) || !verts.Contains(str.EndPoint))
			//				continue;

			//                     // Add the internal nodes for erasing
			//                     toErase.AddRange(intNds.Where(nd => nd.Position.Approx(str.MidPoint())));

			//			// Erase and remove from the list
			//			toErase.Add(str);
			//			geoList.Remove(new StringerGeometry(str.StartPoint, str.EndPoint, 0, 0));
			//		}

			//		// Calculate the distance of the points in X and Y
			//		double
			//			distX = (geometry.Edge1.Length / cln).ConvertFromMillimeter(units.Geometry),
			//			distY = (geometry.Edge2.Length / row).ConvertFromMillimeter(units.Geometry);

			//		// Initialize the start point
			//		var stPt = verts[0];

			//		// Create the new panels
			//		for (int i = 0; i < row; i++)
			//		{
			//			for (int j = 0; j < cln; j++)
			//			{
			//				// Get the vertices of the panel and add to a list
			//				var newVerts = new[]
			//				{
			//					new Point3d(stPt.X + j * distX, stPt.Y + i * distY, 0),
			//					new Point3d(stPt.X + (j + 1) * distX, stPt.Y + i * distY, 0),
			//					new Point3d(stPt.X + j * distX, stPt.Y + (i + 1) * distY, 0),
			//					new Point3d(stPt.X + (j + 1) * distX, stPt.Y + (i + 1) * distY, 0)
			//				};

			//				// Create the panel with XData of the original panel
			//				PanelList.Add(newVerts);

			//				// Create tuples to adding the stringers later
			//				var strsToAdd = new[]
			//				{
			//					new StringerGeometry(newVerts[0], newVerts[1], 0, 0), 
			//					new StringerGeometry(newVerts[0], newVerts[2], 0, 0),
			//					new StringerGeometry(newVerts[2], newVerts[3], 0, 0),
			//					new StringerGeometry(newVerts[1], newVerts[3], 0, 0)
			//				};

			//				// Add to the list of new stringers
			//				newStrList.AddRange(strsToAdd.Where(geo => !geoList.Contains(geo)).Select(geo => new Line(geo.InitialPoint, geo.EndPoint)));
			//			}
			//		}

			//		// Add to objects to erase
			//		toErase.Add(pnl);

			//		// Remove from the list
			//		var list = pnlList.ToList();
			//		list.Remove(geometry.Vertices);
			//		pnlList = list;
			//	}

			//	else // panel is not rectangular
			//	{
			//		error = true;
			//		break;
			//	}
			//}

			//if (error)
			//{
			//	Application.ShowAlertDialog("\nAt least one selected panel is not rectangular.");
			//	return;
			//}

			//// Erase objects
			//toErase.RemoveFromDrawing();

			//// Create the stringers
			//foreach (var str in newStrList)
			//	StringerList.Add(str);

			//// Create the nodes and update elements
			//NodeList.Update();
			//StringerList.Update(false);
			//PanelList.Update(false);

			//// Show an alert for editing stringers
			//Application.ShowAlertDialog("Alert: stringers and panels' parameters must be set again.");
		}

		/// <summary>
		///     Set geometry to a selection of stringers.
		/// </summary>
		[CommandMethod("SetStringerGeometry")]
		public static void SetStringerGeometry()
		{
			// Request objects to be selected in the drawing area
			var strs = UserInput.SelectStringers("Select the stringers to assign properties (you can select other elements, the properties will be only applied to stringers)")?.ToArray();

			if (strs.IsNullOrEmpty())
				return;

			// Start the config window
			var geoWindow = new StringerWindow(Stringers.GetByObjectIds(strs.GetObjectIds())!);
			ShowModalWindow(MainWindow.Handle, geoWindow, false);
		}

		/// <summary>
		///     Set geometry to a selection of panels.
		/// </summary>
		[CommandMethod("SetPanelGeometry")]
		public static void SetPanelGeometry()
		{
			// Request objects to be selected in the drawing area
			var pnls = UserInput.SelectPanels("Select the panels to assign properties (you can select other elements, the properties will be only applied to panels)")?.ToArray();

			if (pnls.IsNullOrEmpty())
				return;

			// Start the config window
			var geoWindow = new PanelWindow(Panels.GetByObjectIds(pnls.GetObjectIds())!);
			ShowModalWindow(MainWindow.Handle, geoWindow, false);
		}

		/// <summary>
		///     Set the reinforcement in a collection of stringers.
		/// </summary>
		[CommandMethod("SetStringerReinforcement")]
		public static void SetStringerReinforcement()
		{
			// Request objects to be selected in the drawing area
			var strs = UserInput.SelectStringers("Select the stringers to assign reinforcement (you can select other elements, the properties will be only applied to stringers).")?.ToArray();

			if (strs.IsNullOrEmpty())
				return;

			// Start the config window
			var geoWindow = new StringerWindow(Stringers.GetByObjectIds(strs.GetObjectIds())!);
			ShowModalWindow(MainWindow.Handle, geoWindow, false);
		}

		/// <summary>
		///     Set reinforcement to a collection of panels.
		/// </summary>
		[CommandMethod("SetPanelReinforcement")]
		public static void SetPanelReinforcement()
		{
			// Request objects to be selected in the drawing area
			var pnls = UserInput.SelectPanels("Select the panels to assign reinforcement (you can select other elements, the properties will be only applied to panels).")?.ToArray();

			if (pnls.IsNullOrEmpty())
				return;

			// Start the config window
			var geoWindow = new PanelWindow(Panels.GetByObjectIds(pnls.GetObjectIds())!);
			ShowModalWindow(MainWindow.Handle, geoWindow, false);
		}

		/// <summary>
		///     Update all the elements in the drawing.
		/// </summary>
		[CommandMethod("UpdateElements")]
		public static void UpdateElements()
		{
			Model.UpdateElements();

			// Display the number of updated elements
			Model.Editor.WriteMessage($"\n{Nodes.Count} nodes, {Stringers.Count} stringers and {Panels.Count} panels updated.");
		}

		#endregion
	}
}