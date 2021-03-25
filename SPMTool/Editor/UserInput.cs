using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Extensions;
using OnPlaneComponents;
using SPM.Elements;
using SPMTool.Core;
using SPMTool.Enums;
using SPMTool.Extensions;
using UnitsNet.Units;

using static Autodesk.AutoCAD.ApplicationServices.Core.Application;
using static SPMTool.Core.DataBase;

#nullable enable

namespace SPMTool.Editor
{
	/// <summary>
	///     User input class.
	/// </summary>
	public static class UserInput
	{
		#region  Methods

		/// <summary>
		///     Get a <see cref="Point3d" /> from user.
		/// </summary>
		/// <param name="message">The message to display.</param>
		/// <param name="basePoint">The base point to use, if needed.</param>
		public static Point3d? GetPoint3d(string message, Point3d? basePoint = null)
		{
			// Prompt for the start point of Stringer
			var ptOp = new PromptPointOptions($"\n{message}");

			if (basePoint.HasValue)
			{
				ptOp.UseBasePoint = true;
				ptOp.BasePoint = basePoint.Value;
			}

			var ptRes = Model.Editor.GetPoint(ptOp);

			if (ptRes.Status == PromptStatus.OK)
				return ptRes.Value;

			return null;
		}

		/// <summary>
		///     Get a <see cref="Point" /> from user.
		/// </summary>
		/// <inheritdoc cref="GetPoint3d"/>
		public static Point? GetPoint(string message, Point? basePoint = null) => GetPoint3d(message, basePoint?.ToPoint3d())?.ToPoint(Settings.Units.Geometry);

		/// <summary>
		///     Get an <see cref="Entity" /> from user.
		/// </summary>
		/// <inheritdoc cref="GetPoint3d"/>
		/// <param name="layers">The collection of layers to filter the object. Leave null to select  any layer.</param>
		public static Entity? SelectEntity(string message, IEnumerable<Layer>? layers = null)
		{
			// Get element
			for ( ; ; )
			{
				// Request the object to be selected in the drawing area
				var entOp  = new PromptEntityOptions($"\n{message}");
				var entRes = Model.Editor.GetEntity(entOp);

				if (entRes.Status == PromptStatus.Cancel)
					return null;

				// Start a transaction
				using var trans = StartTransaction();

				var ent = (Entity) trans.GetObject(entRes.ObjectId, OpenMode.ForRead);

				// Get layername
				var layer = (Layer) Enum.Parse(typeof(Layer), ent.Layer);

				if (layers is null || layers.Contains(layer))
					return ent;

				ShowAlertDialog("Selected object is not the requested.");
			}
		}

		/// <summary>
		///     Get a collection of <see cref="DBObject" />'s from user.
		/// </summary>
		/// <inheritdoc cref="SelectEntity" />
		private static IEnumerable<TDBObject>? SelectObjects<TDBObject>(string message, IEnumerable<Layer>? layers = null)
			where TDBObject : DBObject
		{
			// Prompt for user select elements
			var selOp = new PromptSelectionOptions
			{
				MessageForAdding = $"\n{message}"
			};

			// Get the selection filter
			var filter = layers?.LayerFilter();

			var selRes = filter is null
				? Model.Editor.GetSelection(selOp)
				: Model.Editor.GetSelection(selOp, filter);

			if (selRes.Status == PromptStatus.Cancel || selRes.Value is null)
				return null;

			return
				(from SelectedObject obj in selRes.Value select obj.ObjectId).ToArray().GetDBObjects<TDBObject>().Where(t => !(t is null))!;
		}

		/// <summary>
		///     Get a collection of nodes' <see cref="DBPoint" />'s from user.
		/// </summary>
		/// <inheritdoc cref="SelectEntity" />
		/// <param name="nodeType">
		///     The <see cref="NodeType" /> to filter selection. Leave null to allow
		///     <seealso cref="NodeType.External" /> and <see cref="NodeType.Internal" />.
		/// </param>
		public static IEnumerable<DBPoint>? SelectNodes(string message, NodeType? nodeType = null)
		{
			var layers = new List<Layer>();

			if (nodeType is null || nodeType == NodeType.External)
				layers.Add(Layer.ExtNode);

			if (nodeType is null || nodeType == NodeType.Internal)
				layers.Add(Layer.IntNode);

			// Create an infinite loop for selecting elements
			for ( ; ; )
			{
				var nds = SelectObjects<DBPoint>(message, layers)?.ToArray();

				if (nds is null)
					return null;

				if (nds.Any())
					return nds;

				// No nodes selected
				ShowAlertDialog($"Please select at least one {nodeType} nodes.");
			}
		}

		/// <summary>
		///     Get a collection of stringers' <see cref="Line" />'s from user.
		/// </summary>
		/// <inheritdoc cref="SelectEntity" />
		public static IEnumerable<Line>? SelectStringers(string message)
		{
			var layers = new[] { Layer.Stringer };

			// Create an infinite loop for selecting elements
			for ( ; ; )
			{
				var strs = SelectObjects<Line>(message, layers)?.ToArray();

				if (strs is null)
					return null;

				if (strs.Any())
					return strs;

				ShowAlertDialog("Please select at least one stringer.");
			}
		}

		/// <summary>
		///     Get a collection of panels' <see cref="Solid" />'s from user.
		/// </summary>
		/// <inheritdoc cref="SelectEntity" />
		public static IEnumerable<Solid>? SelectPanels(string message)
		{
			var layers = new[] { Layer.Panel };

			// Create an infinite loop for selecting elements
			for ( ; ; )
			{
				var pnls = SelectObjects<Solid>(message, layers)?.ToArray();

				if (pnls is null)
					return null;

				if (pnls.Any())
					return pnls;

				ShowAlertDialog("Please select at least one panel.");
			}
		}

		/// <summary>
		///     Get a <see cref="Nullable" /> <see cref="int" /> from user.
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
			var intRes = Model.Editor.GetInteger(intOp);

			if (intRes.Status == PromptStatus.OK)
				return intRes.Value;

			return null;
		}

		/// <summary>
		///     Get a <see cref="Nullable" /> <see cref="double" /> from user.
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
			var dbRes = Model.Editor.GetDouble(dbOp);

			if (dbRes.Status == PromptStatus.OK)
				return dbRes.Value;

			return null;
		}

		/// <summary>
		///     Get a keyword from user.
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
		///     Get a keyword from user.
		/// </summary>
		/// <param name="message">The message to display.</param>
		/// <param name="options">Keyword options.</param>
		/// <param name="defaultKeyword">The default keyword.</param>
		/// <param name="allowNone">Allow no keyword selection?</param>
		public static string? SelectKeyword(string message, IEnumerable<string> options, string? defaultKeyword = null, bool allowNone = false)
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

			var result = Model.Editor.GetKeywords(keyOp);

			if (result.Status == PromptStatus.Cancel)
				return null;

			var keyword = result.StringResult;

			return keyword;
		}

		/// <summary>
		///     Get the force values from user.
		/// </summary>
		/// <param name="initialForce">The initial value to display.</param>
		public static PlaneForce? GetForceValue(PlaneForce? initialForce = null)
		{
			var forceUnit = Settings.Units.AppliedForces;
			var fAbrev = forceUnit.Abbrev();

			var force = initialForce ?? PlaneForce.Zero;

			// Convert
			force.ChangeUnit(forceUnit);

			// Ask the user set the load value in x direction:
			var xFn = GetDouble($"Enter force (in {fAbrev}) in X direction(positive following axis direction)?", force.X.Value, true, true);

			if (!xFn.HasValue)
				return null;

			// Ask the user set the load value in y direction:
			var yFn = GetDouble($"Enter force (in {fAbrev}) in Y direction(positive following axis direction)?", force.Y.Value, true, true);

			if (!yFn.HasValue)
				return null;

			return new PlaneForce(xFn.Value, yFn.Value, forceUnit);
		}

		/// <summary>
		///     Ask the user to select a node to monitor and return the DoF index.
		/// </summary>
		public static int? MonitoredIndex()
		{
			// Ask user to select a node
			var nd = SelectEntity("Select a node to monitor displacement:", new [] { Layer.ExtNode, Layer.IntNode });

			if (nd is null)
				return null;

			// Ask direction to monitor
			var options = new []
			{
				$"{Direction.X}",
				$"{Direction.Y}"
			};

			var res = SelectKeyword("Select a direction to monitor displacement:", options, out var dirIndex, options[0]);

			if (res is null)
				return null;

			// Get the node global indexes
			var node  = Model.Nodes.GetByObjectId(nd.ObjectId)?.GetElement();
			var index = node?.DoFIndex;

			return
				index?[dirIndex];
		}

		#endregion
	}
}