using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Extensions;
using Extensions.AutoCAD;
using Extensions.Number;
using Material.Reinforcement;
using Material.Reinforcement.Biaxial;
using Material.Reinforcement.Uniaxial;
using SPM.Elements;
using SPM.Elements.StringerProperties;
using SPMTool.Database;
using SPMTool.Database.Elements;
using SPMTool.Database.Materials;
using SPMTool.Enums;
using UnitsNet.Units;
using Application = Autodesk.AutoCAD.ApplicationServices.Core.Application;
using Force = OnPlaneComponents.Force;

namespace SPMTool.Editor
{
	/// <summary>
    /// User input class.
    /// </summary>
	public static class UserInput
	{
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

			var ptRes = Model.Editor.GetPoint(ptOp);

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
				var entRes = Model.Editor.GetEntity(entOp);

				if (entRes.Status == PromptStatus.Cancel)
					return null;

				// Start a transaction
				using (var trans = DataBase.StartTransaction())
				{
					var ent = (Entity) trans.GetObject(entRes.ObjectId, OpenMode.ForRead);

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
        public static IEnumerable<DBObject> SelectObjects(string message, IEnumerable<Layer> layers = null)
		{
			// Prompt for user select elements
			var selOp = new PromptSelectionOptions()
			{
				MessageForAdding = $"\n{message}"
			};

			var selRes = Model.Editor.GetSelection(selOp);

			if (selRes.Status == PromptStatus.Cancel)
				return null;

			var set = selRes.Value;

			var collection = new List<DBObject>();

			if (set.Count == 0)
				return collection;

			var filter = layers?.ToArray();
			
			// Start a transaction
			using (var trans = DataBase.StartTransaction())
			{
				// Get the objects in the selection and add to the collection only the external nodes
				foreach (SelectedObject obj in set)
				{
					var ent = (Entity) trans.GetObject(obj.ObjectId, OpenMode.ForRead);

                    // Get layername
                    var layer = (Layer) Enum.Parse(typeof(Layer), ent.Layer);

					// Check if it is a external node
					if (layers is null || filter.Contains(layer))
						collection.Add(ent);
				}
			}

			return collection;
		}

        /// <summary>
        /// Get a collection of nodes from user.
        /// </summary>
        /// <param name="message">The message to display.</param>
        /// <param name="nodeType">The <see cref="NodeType"/> to filter selection.</param>
        public static IEnumerable<DBPoint> SelectNodes(string message, NodeType nodeType = NodeType.External)
		{
			var layers = new List<Layer>();

			if (nodeType == NodeType.External || nodeType == NodeType.All)
				layers.Add(Layer.ExtNode);

			if (nodeType == NodeType.Internal || nodeType == NodeType.All)
				layers.Add(Layer.IntNode);

			// Create an infinite loop for selecting elements
			for ( ; ; )
			{
				var nds = SelectObjects(message, layers);

				if (nds is null)
					return null;

				if (nds.Any())
					return nds.ToPoints();

                // No nodes selected
                Application.ShowAlertDialog($"Please select at least one {nodeType} nodes.");
			}
		}

        /// <summary>
        /// Get a collection of stringers from user.
        /// </summary>
        /// <param name="message">The message to display.</param>
        public static IEnumerable<Line> SelectStringers(string message)
		{
			var layers = new[] { Layer.Stringer };

            // Create an infinite loop for selecting elements
            for ( ; ; )
			{
				var strs = SelectObjects(message, layers);

				if (strs is null)
					return null;

				if (strs.Any())
					return strs.ToLines();

                Application.ShowAlertDialog("Please select at least one stringer.");
			}
		}

        /// <summary>
        /// Get a collection of panels from user.
        /// </summary>
        /// <param name="message">The message to display.</param>
		public static IEnumerable<Solid> SelectPanels(string message)
		{
			var layers = new[] { Layer.Panel };

            // Create an infinite loop for selecting elements
            for ( ; ; )
			{
				var pnls = SelectObjects(message, layers);

				if (pnls is null)
					return null;

				if (pnls.Any())
					return pnls.ToSolids();

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
			var intRes = Model.Editor.GetInteger(intOp);

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
			var dbRes = Model.Editor.GetDouble(dbOp);

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

			var result = Model.Editor.GetKeywords(keyOp);
			
			if (result.Status == PromptStatus.Cancel)
				return null;
			
			var keyword = result.StringResult;

			return keyword;
        }

        /// <summary>
        /// Get <see cref="StringerGeometry"/> from user.
        /// </summary>
        /// <param name="geometryUnit">The <see cref="LengthUnit"/> of geometry.</param>
        public static StringerGeometry? GetStringerGeometry(LengthUnit geometryUnit)
        {
	        // Get unit abbreviation
	        var dimAbrev = geometryUnit.Abbrev();

	        // Get saved reinforcement options
	        var savedGeo = ElementData.SavedStringerGeometry;

	        // Get saved reinforcement options
	        if (savedGeo != null)
	        {
		        // Get the options
		        var options = savedGeo.Select(g => $"{g.Width:0.00} {(char)Character.Times} {g.Height:0.00}").ToList();

		        // Add option to set new reinforcement
		        options.Add("New");

		        // Get string result
		        var res = SelectKeyword($"Choose a geometry option ({dimAbrev} x {dimAbrev}) or add a new one:", options, out var index, options[0]);

		        if (res is null)
			        return null;

		        // Get the index
		        if (res != "New")
			        return savedGeo[index];
	        }

	        // New reinforcement
	        var def = 100.ConvertFromMillimeter(geometryUnit);

	        // Ask the user to input the Stringer width
	        var wn = GetDouble($"Input width ({dimAbrev}) for selected stringers:", def);

	        // Ask the user to input the Stringer height
	        var hn = GetDouble($"Input height ({dimAbrev}) for selected stringers:", def);

	        if (!wn.HasValue || !hn.HasValue)
		        return null;

	        double
		        w = wn.Value.Convert(geometryUnit),
		        h = hn.Value.Convert(geometryUnit);

	        // Save geometry
	        var strGeo = new StringerGeometry(Point3d.Origin, Point3d.Origin, w, h);
	        ElementData.Save(strGeo);
	        return strGeo;
        }

        /// <summary>
        /// Get panel width (in mm) from user.
        /// </summary>
        /// <param name="unit">The <see cref="LengthUnit"/> of geometry.</param>
        public static double? GetPanelWidth(LengthUnit unit)
        {
	        // Get saved reinforcement options
	        var savedGeo = ElementData.SavedPanelWidth;

	        // Get unit abreviation
	        var dimAbrev = unit.Abbrev();

	        // Get saved reinforcement options
	        if (savedGeo != null)
	        {
		        // Get the options
		        var options = savedGeo.Select(t => $"{t.ConvertFromMillimeter(unit):0.00}").ToList();

		        // Add option to set new reinforcement
		        options.Add("New");

		        // Get string result
		        var res = SelectKeyword($"Choose panel width ({dimAbrev}) or add a new one:", options, out var index, options[0]);

		        if (res is null)
			        return null;

		        // Get the index
		        if (res != "New")
			        return savedGeo[index];
	        }

	        // New reinforcement
	        var def    = 100.ConvertFromMillimeter(unit);
	        var widthn = GetDouble($"Input width ({dimAbrev}) for selected panels:", def);

	        if (!widthn.HasValue)
		        return null;

	        var width = widthn.Value.Convert(unit);

	        // Save geometry
	        ElementData.Save(width);

	        return width;
        }

        /// <summary>
        /// Get the force values from user.
        /// </summary>
        /// <param name="forceUnit">The current <see cref="ForceUnit"/>.</param>
        public static Force? GetForceValue(ForceUnit forceUnit)
        {
	        var fAbrev = forceUnit.Abbrev();

	        // Ask the user set the load value in x direction:
	        var xFn = GetDouble($"Enter force (in {fAbrev}) in X direction(positive following axis direction)?", 0, true, true);

	        if (!xFn.HasValue)
		        return null;

	        // Ask the user set the load value in y direction:
	        var yFn = GetDouble($"Enter force (in {fAbrev}) in Y direction(positive following axis direction)?", 0, true, true);

	        if (!yFn.HasValue)
		        return null;

	        return new Force(xFn.Value, yFn.Value, forceUnit);
        }

        /// <summary>
        /// Get reinforcement parameters from user.
        /// </summary>
        /// <param name="units">Current <see cref="Units"/>.</param>
        public static UniaxialReinforcement GetUniaxialReinforcement(Units units)
        {
	        // Get saved reinforcement options
	        var savedRef = ReinforcementData.SavedStringerReinforcement;

	        // Get unit abbreviation
	        var dimAbbrev = units.Reinforcement.Abbrev();

	        // Get saved reinforcement options
	        if (savedRef != null)
	        {
		        // Get the options
		        var options = savedRef.Select(r => $"{r.NumberOfBars}{Character.Phi}{r.BarDiameter.ConvertFromMillimeter(units.Reinforcement):0.00}").ToList();

		        // Add option to set new reinforcement
		        options.Add("New");

		        // Ask the user to choose the options
		        var res = SelectKeyword($"Choose a reinforcement option ({dimAbbrev}) or add a new one:", options, out var index, options[0]);

		        if (res is null)
			        return null;

		        // Get the index
		        if (res != "New")
			        return savedRef[index];
	        }

	        // New reinforcement
	        // Ask the user to input the number of bars
	        var numn = GetInteger("Input the number of Stringer reinforcement bars (only needed for nonlinear analysis):", 2);

	        if (!numn.HasValue)
		        return null;

	        // Ask the user to input the Stringer height
	        var def  = 10.ConvertFromMillimeter(units.Reinforcement);
	        var phin = GetDouble($"Input the diameter ({dimAbbrev}) of Stringer reinforcement bars:", def);

	        if (!phin.HasValue)
		        return null;

	        // Get steel
	        var steel = GetSteel(units.MaterialStrength);

	        if (steel is null)
		        return null;

	        // Get reinforcement
	        var num = numn.Value;
	        var phi = phin.Value.Convert(units.Reinforcement);

	        var reinforcement = new UniaxialReinforcement(num, phi, steel);

	        // Save the reinforcement
	        ReinforcementData.Save(reinforcement);

	        return reinforcement;
        }

        /// <summary>
        /// Get steel parameters from user.
        /// </summary>
        /// <param name="unit">The <see cref="PressureUnit"/> of steel parameters.</param>
        public static Steel GetSteel(PressureUnit unit)
        {
	        // Get steel data saved on database
	        var savedSteel = ReinforcementData.SavedSteel;

	        // Get unit abbreviation
	        var matAbrev = unit.Abbrev();

	        // Get saved reinforcement options
	        if (savedSteel != null)
	        {
		        // Get the options
		        var options = savedSteel.Select(s => $"{s.YieldStress.ConvertFromMPa(unit):0.00}|{s.ElasticModule.ConvertFromMPa(unit):0.00}").ToList();

		        // Add option to set new reinforcement
		        options.Add("New");

		        // Ask the user to choose the options
		        var res = SelectKeyword($"Choose a steel option (fy | Es) ({matAbrev}) or add a new one:", options, out var index, options[0]);

		        if (res is null)
			        return null;

		        // Get the index
		        if (res != "New")
			        return savedSteel[index];
	        }

	        // Ask the user to input the Steel yield strength
	        var fDef = 500.ConvertFromMPa(unit);
	        var fyn  = GetDouble($"Input the yield strength ({matAbrev}) of Steel:", fDef);

	        if (!fyn.HasValue)
		        return null;

	        // Ask the user to input the Steel elastic modulus
	        var eDef = 210000.ConvertFromMPa(unit);
	        var Esn  = GetDouble($"Input the elastic modulus ({matAbrev}) of Steel:", eDef);

	        if (!Esn.HasValue)
		        return null;

	        double
		        fy = fyn.Value.Convert(unit),
		        Es = Esn.Value.Convert(unit);

	        var steel = new Steel(fy, Es);

	        // Save steel
	        ReinforcementData.Save(steel);

	        return steel;
        }

        /// <summary>
        /// Get panel reinforcement parameters from user.
        /// </summary>
        /// <param name="direction">The direction of reinforcement.</param>
        /// <param name="units">Current <see cref="Units"/>.</param>
        public static WebReinforcementDirection GetWebReinforcement(Direction direction, Units units)
        {
	        // Get saved reinforcement options
	        var savedRef = ReinforcementData.SavedPanelReinforcement;

	        // Get unit abbreviation
	        var dimAbrev = units.Geometry.Abbrev();
	        var refAbrev = units.Reinforcement.Abbrev();

	        // Get saved reinforcement options
	        if (savedRef != null)
	        {
		        // Get the options
		        var options = savedRef.Select(r => $"{Character.Phi}{r.BarDiameter.ConvertFromMillimeter(units.Reinforcement):0.00}|{r.BarSpacing.ConvertFromMillimeter(units.Geometry):0.00}").ToList();

		        // Add option to set new reinforcement
		        options.Add("New");

		        // Ask the user to choose the options
		        var res = SelectKeyword($"Choose a reinforcement option ({Character.Phi} | s)({refAbrev} | {dimAbrev}) for {direction} direction or add a new one:", options, out var index, options[0]);

		        if (res is null)
			        return null;

		        // Get the index
		        if (res != "New")
			        return savedRef[index];
	        }

	        // New reinforcement
	        // Ask the user to input the diameter of bars
	        var phin = GetDouble($"Input the reinforcement bar diameter ({refAbrev}) for {direction} direction for selected panels (only needed for nonlinear analysis):", 10.ConvertFromMillimeter(units.Reinforcement));

	        if (!phin.HasValue)
		        return null;

	        // Ask the user to input the bar spacing
	        var sn = GetDouble($"Input the bar spacing ({dimAbrev}) for {direction} direction:", 100.ConvertFromMillimeter(units.Geometry));

	        if (!sn.HasValue)
		        return null;

	        // Get steel
	        var steel = GetSteel(units.MaterialStrength);

	        if (steel is null)
		        return null;

	        // Save the reinforcement
	        double
		        phi = phin.Value.Convert(units.Reinforcement),
		        s   = sn.Value.Convert(units.Geometry);

	        var reinforcement = new WebReinforcementDirection(phi, s, steel, 0, 0);

	        ReinforcementData.Save(reinforcement);

	        return reinforcement;
        }

        /// <summary>
        /// Ask the user to select a node to monitor and return the DoF index.
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
	        var node  = Nodes.Read((DBPoint) nd, Units.Default);
	        var index = node.DoFIndex;

	        return
		        index[dirIndex];
        }
	}
}
