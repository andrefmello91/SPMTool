using System;
using System.Collections.Generic;
using System.Linq;
using andrefmello91.Extensions;
using andrefmello91.OnPlaneComponents;
using andrefmello91.SPMElements;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using SPMTool.Core;
using SPMTool.Enums;
using UnitsNet.Units;
using static Autodesk.AutoCAD.ApplicationServices.Core.Application;

#nullable enable

namespace SPMTool
{
	/// <summary>
	///     User input class.
	/// </summary>
	public static partial class Extensions
	{

		#region Methods

		/// <summary>
		///     Get a <see cref="Nullable" /> <see cref="double" /> from user.
		/// </summary>
		/// <param name="message">The message to display.</param>
		/// <param name="defaultValue">The default value to display.</param>
		/// <param name="allowNegative">Allow negative input?</param>
		/// <param name="allowZero">Allow zero input?</param>
		public static double? GetDouble(this Editor editor, string message, double defaultValue = 0, bool allowNegative = false, bool allowZero = false)
		{
			// Ask the user to input the panel width
			var dbOp = new PromptDoubleOptions($"\n{message}")
			{
				DefaultValue  = defaultValue,
				AllowZero     = allowZero,
				AllowNegative = allowNegative
			};

			// Get the result
			var dbRes = editor.GetDouble(dbOp);

			return
				dbRes.Status is PromptStatus.OK
					? dbRes.Value
					: null;
		}

		/// <summary>
		///     Get an <see cref="Entity" /> from user.
		/// </summary>
		/// <inheritdoc cref="GetPoint3d" />
		/// <param name="pickedPoint">The picked point in the model. Null if command is canceled.</param>
		/// <param name="layers">The collection of layers to filter the object. Leave null to select  any layer.</param>
		public static Entity? GetEntity(this Database database, string message, IEnumerable<Layer>? layers = null)
		{
			var model  = SPMModel.GetOpenedModel(database);
			var editor = model?.Editor ?? database.GetDocument().Editor;
			var unit   = model?.Settings.Units.Geometry ?? LengthUnit.Millimeter;

			// Start a transaction
			using var trans = database.TransactionManager.StartTransaction();

			// Get element
			while (true)
			{
				// Request the object to be selected in the drawing area
				var entOp = new PromptEntityOptions($"\n{message}");
				entOp.AllowNone = true;
				var entRes = editor.GetEntity(entOp);

				if (entRes.Status == PromptStatus.Cancel)
					return null;

				var ent = (Entity) trans.GetObject(entRes.ObjectId, OpenMode.ForRead);

				// Get layername
				var layer = (Layer) Enum.Parse(typeof(Layer), ent.Layer);

				if (layers is null || layers.Contains(layer))
					return ent;

				ShowAlertDialog("Selected object is not the requested.");
			}
		}

		/// <summary>
		///     Get the force values from user.
		/// </summary>
		/// <param name="initialForce">The initial value to display.</param>
		/// <param name="unit">The <see cref="ForceUnit" />.</param>
		public static PlaneForce? GetForce(this Editor editor, PlaneForce? initialForce = null, ForceUnit unit = ForceUnit.Kilonewton)
		{
			var fAbrev = unit.Abbrev();

			var force = initialForce ?? PlaneForce.Zero;

			// Convert
			force.ChangeUnit(unit);

			// Ask the user set the load value in x direction:
			var xFn = editor.GetDouble($"Enter force (in {fAbrev}) in X direction(positive following axis direction)?", force.X.Value, true, true);

			if (!xFn.HasValue)
				return null;

			// Ask the user set the load value in y direction:
			var yFn = editor.GetDouble($"Enter force (in {fAbrev}) in Y direction(positive following axis direction)?", force.Y.Value, true, true);

			return
				yFn.HasValue
					? new PlaneForce(xFn.Value, yFn.Value, unit)
					: null;
		}

		/// <summary>
		///     Get a <see cref="Nullable" /> <see cref="int" /> from user.
		/// </summary>
		/// <param name="message">The message to display.</param>
		/// <param name="defaultValue">The default value to display.</param>
		/// <param name="allowNegative">Allow negative input?</param>
		/// <param name="allowZero">Allow zero input?</param>
		public static int? GetInteger(this Editor editor, string message, int defaultValue = 0, bool allowNegative = false, bool allowZero = false)
		{
			// Prompt for the number of rows
			var intOp = new PromptIntegerOptions($"\n{message}")
			{
				DefaultValue  = defaultValue,
				AllowNegative = allowNegative,
				AllowZero     = allowZero
			};

			// Get the number
			var intRes = editor.GetInteger(intOp);

			return
				intRes.Status is PromptStatus.OK
					? intRes.Value
					: null;
		}

		/// <summary>
		///     Get a keyword from user.
		/// </summary>
		/// <param name="message">The message to display.</param>
		/// <param name="options">Keyword options.</param>
		/// <param name="index">The index of selection.</param>
		/// <param name="defaultKeyword">The default keyword.</param>
		/// <param name="allowNone">Allow no keyword selection?</param>
		public static string? GetKeyword(this Editor editor, string message, IEnumerable<string> options, out int index, string defaultKeyword = null, bool allowNone = false)
		{
			index = 0;

			var keyword = editor.GetKeyword(message, options, defaultKeyword, allowNone);

			index = keyword is not null
				? options.ToList().IndexOf(keyword)
				: 0;

			return keyword;
		}

		/// <summary>
		///     Get a keyword from user.
		/// </summary>
		/// <param name="message">The message to display.</param>
		/// <param name="options">Keyword options.</param>
		/// <param name="defaultKeyword">The default keyword.</param>
		/// <param name="allowNone">Allow no keyword selection?</param>
		public static string? GetKeyword(this Editor editor, string message, IEnumerable<string> options, string? defaultKeyword = null, bool allowNone = false)
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
			if (!string.IsNullOrEmpty(defaultKeyword))
				keyOp.Keywords.Default = defaultKeyword;

			var result = editor.GetKeywords(keyOp);

			return
				result.Status is PromptStatus.OK
					? result.StringResult
					: null;
		}

		/// <summary>
		///     Ask the user to select a node to monitor and return the DoF index.
		/// </summary>
		public static int? GetMonitoredIndex(this SPMModel model)
		{
			// Ask user to select a node
			var nd = model.AcadDatabase.GetEntity("Select a node to monitor displacement:", new[] { Layer.ExtNode, Layer.IntNode });

			if (nd is null)
				return null;

			// Ask direction to monitor
			var options = new[]
			{
				$"{Axis.X}",
				$"{Axis.Y}"
			};

			var res = model.Editor.GetKeyword("Select a direction to monitor displacement:", options, out var dirIndex, options[0]);

			if (res is null)
				return null;

			// Get the node global indexes
			var node  = model.Nodes[nd.ObjectId]?.GetElement();
			var index = node?.DoFIndex;

			return
				index?[dirIndex];
		}

		/// <summary>
		///     Get a collection of nodes' <see cref="DBPoint" />'s from user.
		/// </summary>
		/// <inheritdoc cref="GetEntity" />
		/// <param name="nodeType">
		///     The <see cref="NodeType" /> to filter selection. Leave null to allow
		///     <seealso cref="NodeType.External" /> and <see cref="NodeType.Internal" />.
		/// </param>
		public static IEnumerable<DBPoint>? GetNodes(this Database database, string message, NodeType? nodeType = null)
		{
			var layers = new List<Layer>();

			if (nodeType is null or NodeType.External)
				layers.Add(Layer.ExtNode);

			if (nodeType is null or NodeType.Internal)
				layers.Add(Layer.IntNode);

			// Create an infinite loop for selecting elements
			while (true)
			{
				var nds = database.GetObjects<DBPoint>(message, layers)?.ToArray();

				if (nds is null)
					return null;

				if (nds.Any())
					return nds;

				// No nodes selected
				ShowAlertDialog($"Please select at least one {nodeType} nodes.");
			}
		}

		/// <summary>
		///     Get a collection of <see cref="DBObject" />'s from user.
		/// </summary>
		/// <inheritdoc cref="GetEntity" />
		public static IEnumerable<DBObject>? GetObjects(this Database database, string message, IEnumerable<Layer>? layers = null)
		{
			var editor = database.GetDocument().Editor;

			// Prompt for user select elements
			var selOp = new PromptSelectionOptions
			{
				MessageForAdding = $"\n{message}"
			};

			var selRes = layers is null
				? editor.GetSelection(selOp)
				: editor.GetSelection(selOp, layers.LayerFilter());

			return
				(selRes.Status == PromptStatus.OK && selRes.Value is not null
					? database.GetObjects(selRes.Value.GetObjectIds()).Where(d => d is not null)
					: null)!;

		}

		/// <summary>
		///     Get a collection of <see cref="DBObject" />'s from user.
		/// </summary>
		/// <inheritdoc cref="GetEntity" />
		public static IEnumerable<TDBObject>? GetObjects<TDBObject>(this Database database, string message, IEnumerable<Layer>? layers = null)
			where TDBObject : DBObject =>
			database.GetObjects(message, layers)?.Where(o => o is TDBObject).Cast<TDBObject>();

		/// <summary>
		///     Get a collection of panels' <see cref="Solid" />'s from user.
		/// </summary>
		/// <inheritdoc cref="GetEntity" />
		public static IEnumerable<Solid>? GetPanels(this Database database, string message)
		{
			List<Solid>? pnls = null;

			var model = SPMModel.GetOpenedModel(database);
			var unit  = model!.Settings.Units.Geometry;

			var layers = new[] { Layer.Panel, Layer.PanelCenter };

			// Create auxiliary points on panel centers
			var auxPts = model.Panels.Select(p => new DBPoint(p.Vertices.CenterPoint.ToPoint3d(unit)) { Layer = $"{Layer.PanelCenter}" }).ToList();
			model.AcadDocument.AddObjects(auxPts);

			// Create an infinite loop for selecting elements
			while (true)
			{
				var objs = database.GetObjects(message, layers)?.ToList();

				if (objs is null)
					break;

				pnls = objs.Where(o => o is Solid)
					.Cast<Solid>()
					.ToList();

				var pts = objs.Where(o => o is DBPoint)
					.Cast<DBPoint>()
					.Select(d => d.Position.ToPoint(unit))
					.ToList();

				if (pts.Any())
				{
					// Get selected panel vertices and object ids
					var ids = pnls.GetObjectIds()!;
					var otherPanels = database.GetDocument()
						.GetObjects(Layer.Panel)!
						.Where(p => p is Solid && !ids.Contains(p.ObjectId))
						.Cast<Solid>();

					// Get panels from center points and add to the list
					pnls.AddRange(otherPanels.Where(pnl => pts.Any(pt => pt == pnl.CenterPoint().ToPoint(unit))));
				}

				if (pnls.Any())
					break;

				ShowAlertDialog("Please select at least one panel.");
			}

			// Remove panel auxiliary points
			model.AcadDocument.EraseObjects(Layer.PanelCenter);

			return pnls;
		}

		/// <summary>
		///     Get a <see cref="Point" /> from user.
		/// </summary>
		/// <inheritdoc cref="GetPoint3d" />
		public static Point? GetPoint(this Editor editor, string message, Point? basePoint = null, LengthUnit unit = LengthUnit.Millimeter) =>
			editor.GetPoint3d(message, basePoint?.ToPoint3d(unit))?.ToPoint(unit);

		/// <summary>
		///     Get a <see cref="Point3d" /> from user.
		/// </summary>
		/// <param name="message">The message to display.</param>
		/// <param name="basePoint">The base point to use, if needed.</param>
		public static Point3d? GetPoint3d(this Editor editor, string message, Point3d? basePoint = null)
		{
			// Prompt for the start point of Stringer
			var ptOp = new PromptPointOptions($"\n{message}");

			if (basePoint.HasValue)
			{
				ptOp.UseBasePoint = true;
				ptOp.BasePoint    = basePoint.Value;
			}

			var ptRes = editor.GetPoint(ptOp);

			return
				ptRes.Status is PromptStatus.OK
					? ptRes.Value
					: null;
		}

		/// <summary>
		///     Get a collection of stringers' <see cref="Line" />'s from user.
		/// </summary>
		/// <inheritdoc cref="GetEntity" />
		public static IEnumerable<Line>? GetStringers(this Database database, string message)
		{
			var layers = new[] { Layer.Stringer };

			// Create an infinite loop for selecting elements
			while (true)
			{
				var strs = database.GetObjects<Line>(message, layers)?.ToArray();

				if (strs is null)
					return null;

				if (strs.Any())
					return strs;

				ShowAlertDialog("Please select at least one stringer.");
			}
		}

		#endregion

	}
}