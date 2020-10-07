using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using SPM.Elements;
using SPMTool.Database;
using SPMTool.Enums;
using Application = Autodesk.AutoCAD.ApplicationServices.Core.Application;

namespace SPMTool.Editor
{
	/// <summary>
    /// User input class.
    /// </summary>
	public static class UserInput
	{
		/// <summary>
		/// Get application <see cref="Autodesk.AutoCAD.EditorInput.Editor"/>.
		/// </summary>
		public static Autodesk.AutoCAD.EditorInput.Editor Editor => DataBase.Document.Editor;

        /// <summary>
        /// Get a <see cref="Point3d"/> from user.
        /// </summary>
        /// <param name="message">The message to display.</param>
        /// <param name="basePoint">The base point to use, if needed.</param>
        public static Point3d? GetPoint(string message, Point3d? basePoint = null)
		{
			// Prompt for the start point of Stringer
			var ptOp = new PromptPointOptions($"\n{message}");

			if (basePoint.HasValue)
			{
				ptOp.UseBasePoint = true;
				ptOp.BasePoint = basePoint.Value;
			}

			var ptRes = Editor.GetPoint(ptOp);

			if (ptRes.Status == PromptStatus.OK)
				return ptRes.Value;

			return null;
		}

		/// <summary>
        /// Get an <see cref="Entity"/> from user.
        /// </summary>
        /// <param name="message">The message to display.</param>
        /// <param name="layers">The collection of layers to filter the object.</param>
        public static Entity SelectEntity(string message, IEnumerable<Layer> layers = null)
		{
			// Get element
			for ( ; ; )
			{
				// Request the object to be selected in the drawing area
				var entOp  = new PromptEntityOptions($"\n{message}");
				var entRes = Editor.GetEntity(entOp);

				if (entRes.Status == PromptStatus.Cancel)
					return null;

				// Start a transaction
				using (var trans = DataBase.StartTransaction())
				using (var ent   = (Entity) trans.GetObject(entRes.ObjectId, OpenMode.ForRead))
				{
					// Get layername
					var layer = (Layer) Enum.Parse(typeof(Layer), ent.Layer);

					if (layers is null || layers.Contains(layer))
						return ent;
				}

				Application.ShowAlertDialog("Selected object is not the requested.");
			}
		}

        /// <summary>
        /// Get a collection of objects from user.
        /// </summary>
        /// <param name="message">The message to display.</param>
        /// <param name="layers">The collection of layers to filter the objects.</param>
        public static DBObjectCollection SelectObjects(string message, Layer[] layers = null)
		{
			// Prompt for user select elements
			var selOp = new PromptSelectionOptions()
			{
				MessageForAdding = $"\n{message}"
			};

			var selRes = Editor.GetSelection(selOp);

			if (selRes.Status == PromptStatus.Cancel)
				return null;

			var set = selRes.Value;

			using (var collection = new DBObjectCollection())
			{
				if (set.Count == 0)
					return collection;

				// Start a transaction
				using (var trans = DataBase.StartTransaction())
				{
					// Get the objects in the selection and add to the collection only the external nodes
					foreach (SelectedObject obj in set)
						using (var ent = (Entity) trans.GetObject(obj.ObjectId, OpenMode.ForRead))
						{
							// Get layername
							var layer = (Layer) Enum.Parse(typeof(Layer), ent.Layer);

							// Check if it is a external node
							if (layers is null || layers.Contains(layer))
								collection.Add(ent);
						}

					return collection;
				}
			}
		}

        /// <summary>
        /// Get a collection of nodes from user.
        /// </summary>
        /// <param name="message">The message to display.</param>
        /// <param name="nodeType">The <see cref="NodeType"/> to filter selection.</param>
        public static DBObjectCollection SelectNodes(string message, NodeType nodeType = NodeType.External)
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
                Application.ShowAlertDialog($"Please select at least one {nodeType} nodes.");
			}
		}

        /// <summary>
        /// Get a collection of stringers from user.
        /// </summary>
        /// <param name="message">The message to display.</param>
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

        /// <summary>
        /// Get a collection of panels from user.
        /// </summary>
        /// <param name="message">The message to display.</param>
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

        /// <summary>
        /// Get a <see cref="Nullable"/> <see cref="int"/> from user.
        /// </summary>
        /// <param name="message">The message to display.</param>
        /// <param name="defaultValue">The default value to display.</param>
        /// <param name="allowNegative">Allow negative input?</param>
        /// <param name="allowZero">Allow zero input?</param>
        public static int? GetInteger(string message, int defaultValue = 0, bool allowNegative = false, bool allowZero = false)
		{
			// Prompt for the number of rows
			var intOp = new PromptIntegerOptions($"\n{message}")
			{
				DefaultValue  = defaultValue,
				AllowNegative = allowNegative,
				AllowZero     = allowZero
			};

			// Get the number
			var intRes = Editor.GetInteger(intOp);

			if (intRes.Status == PromptStatus.OK)
				return intRes.Value;

			return null;
		}

        /// <summary>
        /// Get a <see cref="Nullable"/> <see cref="double"/> from user.
        /// </summary>
        /// <param name="message">The message to display.</param>
        /// <param name="defaultValue">The default value to display.</param>
        /// <param name="allowNegative">Allow negative input?</param>
        /// <param name="allowZero">Allow zero input?</param>
		public static double? GetDouble(string message, double defaultValue = 0, bool allowNegative = false, bool allowZero = false)
		{
			// Ask the user to input the panel width
			var dbOp = new PromptDoubleOptions($"\n{message}")
			{
				DefaultValue  = defaultValue,
				AllowZero     = allowZero,
				AllowNegative = allowNegative
			};

			// Get the result
			var dbRes = Editor.GetDouble(dbOp);

			if (dbRes.Status == PromptStatus.OK)
				return dbRes.Value;

			return null;
		}

        /// <summary>
        /// Get a keyword from user.
        /// </summary>
        /// <param name="message">The message to display.</param>
        /// <param name="options">Keyword options.</param>
        /// <param name="index">The index of selection.</param>
        /// <param name="defaultKeyword">The default keyword.</param>
        /// <param name="allowNone">Allow no keyword selection?</param>
        public static string SelectKeyword(string message, IEnumerable<string> options, out int index, string defaultKeyword = null, bool allowNone = false)
        {
	        index = 0;

	        var keyword = SelectKeyword(message, options, defaultKeyword, allowNone);

			if (keyword != null)
				index = options.ToList().IndexOf(keyword);

			return keyword;
        }

        /// <summary>
        /// Get a keyword from user.
        /// </summary>
        /// <param name="message">The message to display.</param>
        /// <param name="options">Keyword options.</param>
        /// <param name="defaultKeyword">The default keyword.</param>
        /// <param name="allowNone">Allow no keyword selection?</param>
        public static string SelectKeyword(string message, IEnumerable<string> options, string defaultKeyword = null, bool allowNone = false)
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

			var result = Editor.GetKeywords(keyOp);
			
			if (result.Status == PromptStatus.Cancel)
				return null;

			var keyword = result.StringResult;

			return keyword;
        }
	}
}
