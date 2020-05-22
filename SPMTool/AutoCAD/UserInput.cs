using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using NodeType = SPMTool.Core.Node.NodeType;

namespace SPMTool.AutoCAD
{
	public static class UserInput
	{
		// Ask user to select a point (nullable)
		public static Point3d? SelectPoint(string message, Point3d? basePoint = null)
		{
			// Prompt for the start point of Stringer
			PromptPointOptions ptOp = new PromptPointOptions("\n" + message);

			if (basePoint.HasValue)
			{
				ptOp.UseBasePoint = true;
				ptOp.BasePoint = basePoint.Value;
			}

			PromptPointResult ptRes = Current.edtr.GetPoint(ptOp);

			if (ptRes.Status == PromptStatus.OK)
				return ptRes.Value;

			return null;
		}

        // Ask user to select an entity
        public static Entity SelectEntity(string message, Elements element = default, Layers[] layers = null)
		{
			for ( ; ; )
			{
				// Request the object to be selected in the drawing area
				PromptEntityOptions entOp = new PromptEntityOptions("\n" + message);
				PromptEntityResult entRes = Current.edtr.GetEntity(entOp);

				if (entRes.Status == PromptStatus.Cancel)
					return null;

				// Start a transaction
				using (Transaction trans = Current.db.TransactionManager.StartTransaction())
				{
					// Get the entity for read
					Entity ent = trans.GetObject(entRes.ObjectId, OpenMode.ForRead) as Entity;

					// Get layername
					var layer = (Layers) Enum.Parse(typeof(Layers), ent.Layer);

					if (layers == null || layers.Contains(layer))
						return ent;
				}

				Application.ShowAlertDialog("Selected object is not a " + element.ToString());
			}
		}

		// Ask user to select objects
		public static DBObjectCollection SelectObjects(string message, Layers[] layers = null)
		{
			// Prompt for user select elements
			var selOp = new PromptSelectionOptions()
			{
				MessageForAdding = "\n" + message
			};

			PromptSelectionResult selRes = Current.edtr.GetSelection(selOp);

			if (selRes.Status == PromptStatus.Cancel)
				return null;

			var set = selRes.Value;

			var collection = new DBObjectCollection();

			// Start a transaction
			using (Transaction trans = Current.db.TransactionManager.StartTransaction())
			{
				// Get the objects in the selection and add to the collection only the external nodes
				foreach (SelectedObject obj in set)
				{
					// Read as entity
					Entity ent = trans.GetObject(obj.ObjectId, OpenMode.ForRead) as Entity;
					
					// Get layername
					var layer = (Layers)Enum.Parse(typeof(Layers), ent.Layer);

                    // Check if it is a external node
                    if (layers == null || layers.Contains(layer))
	                    collection.Add(ent);
				}
			}

			if (collection.Count > 0)
				return collection;

			return null;
		}

		// Ask user to select nodes
		public static DBObjectCollection SelectNodes(string message, NodeType nodeType)
		{
			var layers = new List<Layers>();

			if (nodeType == NodeType.External || nodeType == NodeType.All)
				layers.Add(Layers.ExtNode);

			if (nodeType == NodeType.Internal || nodeType == NodeType.All)
				layers.Add(Layers.IntNode);

			return
				SelectObjects(message, layers.ToArray());
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
			PromptIntegerResult intRes = Current.edtr.GetInteger(intOp);

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
			PromptDoubleResult dbRes = Current.edtr.GetDouble(dbOp);

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
				AllowNone = allowNone
			};

			// Get the options
			foreach (var option in options)
				keyOp.Keywords.Add(option);

			// Set default
			if (defaultKeyword != null)
				keyOp.Keywords.Default = defaultKeyword;

			PromptResult result = Current.edtr.GetKeywords(keyOp);

			if (result.Status == PromptStatus.Cancel)
				return null;

			string keyword = result.StringResult;
			int index = Array.IndexOf(options, keyword);

			return
				(index, keyword);
		}
    }
}
