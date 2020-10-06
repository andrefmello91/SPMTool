using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using SPM.Elements;
using SPMTool.Database;
using SPMTool.Enums;

namespace SPMTool.Model.Conditions
{
	public static class UserInput
	{
		// Ask user to select a point (nullable)
		public static Point3d? GetPoint(string message, Point3d? basePoint = null)
		{
			// Prompt for the start point of Stringer
			PromptPointOptions ptOp = new PromptPointOptions("\n" + message);

			if (basePoint.HasValue)
			{
				ptOp.UseBasePoint = true;
				ptOp.BasePoint = basePoint.Value;
			}

			PromptPointResult ptRes = Database.DataBase.Editor.GetPoint(ptOp);

			if (ptRes.Status == PromptStatus.OK)
				return ptRes.Value;

			return null;
		}

        // Ask user to select an entity
        public static Entity SelectEntity(string message, Layer[] layers = null)
		{
			// Get element
			for ( ; ; )
			{
				// Request the object to be selected in the drawing area
				PromptEntityOptions entOp = new PromptEntityOptions("\n" + message);
				PromptEntityResult entRes = Database.DataBase.Editor.GetEntity(entOp);

				if (entRes.Status == PromptStatus.Cancel)
					return null;

				// Start a transaction
				using (Transaction trans = Database.DataBase.StartTransaction())
				{
					// Get the entity for read
					Entity ent = trans.GetObject(entRes.ObjectId, OpenMode.ForRead) as Entity;

					// Get layername
					var layer = (Layer) Enum.Parse(typeof(Layer), ent.Layer);

					if (layers is null || layers.Contains(layer))
						return ent;
				}

				Application.ShowAlertDialog("Selected object is not the requested.");
			}
		}

		// Ask user to select objects
		public static DBObjectCollection SelectObjects(string message, Layer[] layers = null)
		{
			// Prompt for user select elements
			var selOp = new PromptSelectionOptions()
			{
				MessageForAdding = "\n" + message
			};

			PromptSelectionResult selRes = Database.DataBase.Editor.GetSelection(selOp);

			if (selRes.Status == PromptStatus.Cancel)
				return null;

			var set = selRes.Value;

			var collection = new DBObjectCollection();

			if (set.Count > 0)
			{
				// Start a transaction
				using (Transaction trans = Database.DataBase.StartTransaction())
				{
					// Get the objects in the selection and add to the collection only the external nodes
					foreach (SelectedObject obj in set)
					{
						// Read as entity
						Entity ent = trans.GetObject(obj.ObjectId, OpenMode.ForRead) as Entity;

						// Get layername
						var layer = (Layer) Enum.Parse(typeof(Layer), ent.Layer);

						// Check if it is a external node
						if (layers is null || layers.Contains(layer))
							collection.Add(ent);
					}
				}
			}

			return collection;
		}

		// Ask user to select nodes
		public static DBObjectCollection SelectNodes(string message, NodeType nodeType)
		{
			DBObjectCollection nds;
            var layers = new List<Layer>();

			if (nodeType == NodeType.External || nodeType == NodeType.All)
				layers.Add(Layer.ExtNode);

			if (nodeType == NodeType.Internal || nodeType == NodeType.All)
				layers.Add(Layer.IntNode);

			// Create an infinite loop for selecting elements
			for ( ; ; )
			{
				nds = SelectObjects(message, layers.ToArray());

				if (nds is null)
					return null;

				if (nds.Count > 0)
					return nds;

                // No nodes selected
                Application.ShowAlertDialog("Please select at least one " + nodeType + " nodes.");
			}
		}

		// Ask user to select stringers
		public static DBObjectCollection SelectStringers(string message)
		{
			DBObjectCollection strs;
			var layers = new[] { Layer.Stringer };

            // Create an infinite loop for selecting elements
            for ( ; ; )
			{
				strs = SelectObjects(message, layers);

				if (strs is null)
					return null;

				if (strs.Count > 0)
					return strs;

                Application.ShowAlertDialog("Please select at least one stringer.");
			}
		}

		// Ask user to select panels
		public static DBObjectCollection SelectPanels(string message)
		{
			DBObjectCollection pnls;
			var layers = new[] { Layer.Panel };

            // Create an infinite loop for selecting elements
            for ( ; ; )
			{
				pnls = SelectObjects(message, layers);

				if (pnls is null)
					return null;

				if (pnls.Count > 0)
					return pnls;

                Application.ShowAlertDialog("Please select at least one panel.");
			}
		}

		// Get an integer from user (nullable)
		public static int? GetInteger(string message, int defaultValue = 0, bool allowNegative = false, bool allowZero = false)
		{
			// Prompt for the number of rows
			var intOp = new PromptIntegerOptions("\n" + message)
			{
				DefaultValue  = defaultValue,
				AllowNegative = allowNegative,
				AllowZero     = allowZero
			};

			// Get the number
			PromptIntegerResult intRes = Database.DataBase.Editor.GetInteger(intOp);

			if (intRes.Status == PromptStatus.OK)
				return intRes.Value;

			return null;
		}

		// Get double from user (nullable)
		public static double? GetDouble(string message, double defaultValue = 0, bool allowNegative = false, bool allowZero = false)
		{
			// Ask the user to input the panel width
			var dbOp = new PromptDoubleOptions("\n" + message)
			{
				DefaultValue  = defaultValue,
				AllowZero     = allowZero,
				AllowNegative = allowNegative
			};

			// Get the result
			PromptDoubleResult dbRes = Database.DataBase.Editor.GetDouble(dbOp);

			if (dbRes.Status == PromptStatus.OK)
				return dbRes.Value;

			return null;
		}

		// Get keyword from user
		public static (int index, string keyword)? SelectKeyword(string message, string[] options, string defaultKeyword = null, bool allowNone = false)
		{
			// Ask the user to choose the options
			var keyOp = new PromptKeywordOptions("\n" + message)
			{
				AllowNone           = allowNone,
				AllowArbitraryInput = false
			};

			// Get the options
			foreach (var option in options)
				keyOp.Keywords.Add(option);

			// Set default
			if (defaultKeyword != null)
				keyOp.Keywords.Default = defaultKeyword;

			PromptResult result = Database.DataBase.Editor.GetKeywords(keyOp);
			
			if (result.Status == PromptStatus.Cancel)
				return null;

			string keyword = result.StringResult;
			int index = Array.IndexOf(options, keyword);

			return
				(index, keyword);
		}
    }
}
