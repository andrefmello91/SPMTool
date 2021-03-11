using System;
using System.Linq;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Extensions;
using Material.Reinforcement;
using Material.Reinforcement.Biaxial;
using Material.Reinforcement.Uniaxial;
using SPM.Analysis;
using SPM.Elements;
using SPM.Elements.StringerProperties;
using SPMTool.Core.Conditions;
using SPMTool.Core.Elements;
using SPMTool.Enums;
using SPMTool.Extensions;
using UnitsNet;

using static SPMTool.Core.DataBase;
using static Autodesk.AutoCAD.ApplicationServices.Application;

#nullable disable

namespace SPMTool.Core
{
	/// <summary>
	///     Model class
	/// </summary>
	public static class Model
	{
		#region Fields

		/// <summary>
		///     Collection of element <see cref="Layer" />'s.
		/// </summary>
		public static readonly Layer[] ElementLayers = { Layer.ExtNode, Layer.IntNode, Layer.Stringer, Layer.Panel, Layer.Force, Layer.Support }  ;

		/// <summary>
		///     Command names for undo and redo.
		/// </summary>
		private static readonly string[] CmdNames = { "UNDO", "REDO", "_U", "_R", "_.U", "_.R" }  ;

		/// <summary>
		///     The collection of <see cref="NodeObject" />'s in the model.
		/// </summary>
		public static readonly NodeList Nodes;

		/// <summary>
		///     The collection of <see cref="StringerObject" />'s in the model.
		/// </summary>
		public static readonly StringerList Stringers;

		/// <summary>
		///     The collection of <see cref="PanelObject" />'s in the model.
		/// </summary>
		public static readonly PanelList Panels;

		/// <summary>
		///     The collection of <see cref="ForceObject" />'s in the model.
		/// </summary>
		public static readonly ForceList Forces;

		/// <summary>
		///     The collection of <see cref="ConstraintObject" />'s in the model.
		/// </summary>
		public static readonly ConstraintList Constraints;

		/// <summary>
		///     List of distinct widths from objects in the model.
		/// </summary>
		public static EList<Length> ElementWidths;

		/// <summary>
		///     List of distinct stringer's <see cref="CrossSection" />'s from objects in the model.
		/// </summary>
		public static EList<CrossSection> StringerCrossSections;

		/// <summary>
		///     List of distinct reinforcements of stringers in the model.
		/// </summary>
		public static EList<UniaxialReinforcement> StringerReinforcements;

		/// <summary>
		///     List of distinct reinforcements of panels in the model.
		/// </summary>
		public static EList<WebReinforcementDirection> PanelReinforcements;

		/// <summary>
		///     List of distinct steels of elements in the model.
		/// </summary>
		public static EList<Steel> Steels;

		#endregion

		#region Properties

		/// <summary>
		///     Get application <see cref="Autodesk.AutoCAD.EditorInput.Editor" />.
		/// </summary>
		public static Autodesk.AutoCAD.EditorInput.Editor Editor => DataBase.Document.Editor;

		#endregion

		#region Constructors

		static Model()
		{
			// Get elements
			Nodes       = NodeList.ReadFromDrawing();
			Forces      = ForceList.ReadFromDrawing();
			Constraints = ConstraintList.ReadFromDrawing();
			Stringers   = StringerList.ReadFromDrawing();
			Panels      = PanelList.ReadFromDrawing();

			// Get properties
			StringerCrossSections  = GetCrossSections();
			ElementWidths          = Stringers.GetWidths().Concat(Panels.GetWidths()).Distinct().ToEList();
			StringerReinforcements = GetStringerReinforcements();
			PanelReinforcements    = GetPanelReinforcements();
			Steels                 = Stringers.GetSteels().Concat(Panels.GetSteels()).ToEList();

			// Set parameters
			SetAppParameters();
		}

		#endregion

		#region  Methods

		/// <inheritdoc cref="StringerCrossSections" />
		private static EList<CrossSection> GetCrossSections()
		{
			var list = Stringers.GetCrossSections().ToEList() ?? new EList<CrossSection>();

			list.ItemAdded += On_CrossSection_Add;

			return list;
		}

		/// <inheritdoc cref="StringerReinforcements" />
		private static EList<UniaxialReinforcement> GetStringerReinforcements()
		{
			var list = Stringers.GetReinforcements().ToEList() ?? new EList<UniaxialReinforcement>();

			list.ItemAdded += On_StrRef_Add;

			return list;
		}

		/// <inheritdoc cref="PanelReinforcements" />
		private static EList<WebReinforcementDirection> GetPanelReinforcements()
		{
			var list = Panels.GetReinforcementDirections().ToEList() ?? new EList<WebReinforcementDirection>();

			list.ItemAdded += On_PanRef_Add;

			return list;
		}

		/// <summary>
		///     Update all the elements in the drawing.
		/// </summary>
		/// <param name="addNodes">Add nodes to stringer start, mid and end points?</param>
		/// <param name="removeNodes">Remove nodes at unnecessary positions?</param>
		public static void UpdateElements(bool addNodes = true, bool removeNodes = true)
		{
			Stringers.Update();
			Panels.Update();
			Nodes.Update(addNodes, removeNodes);
		}

		/// <summary>
		///     Get the <see cref="InputData" /> from objects in drawing.
		/// </summary>
		/// <param name="dataOk">Returns true if data is consistent to start analysis.</param>
		/// <param name="message">Message to show if data is inconsistent.</param>
		/// <param name="analysisType">The type of analysis to perform.</param>
		public static InputData? GenerateInput(AnalysisType analysisType, out bool dataOk, out string message)
		{
			// Read elements
			var nodes     = Nodes.GetElements();
			var stringers = Stringers.GetElements(nodes, analysisType);
			var panels    = Panels.GetElements(nodes, analysisType);

			// Verify if there is stringers and nodes at least
			if (nodes.Count == 0 || stringers.Count == 0)
			{
				dataOk = false;
				message = "Please input model geometry";
				return null;
			}

			// Generate input
			dataOk  = true;
			message = string.Empty;

			return
				new InputData(nodes, stringers, panels, analysisType);
		}

		/// <summary>
		///     Set application parameters for drawing.
		/// </summary>
		public static void SetAppParameters()
		{
			SetPointSize();
			SetLineWeightDisplay();
		}

		/// <summary>
		///     Set size to points in the drawing.
		/// </summary>
		public static void SetPointSize()
		{
			// Set the style for all point objects in the drawing
			DataBase.Database.Pdmode = 32;
			DataBase.Database.Pdsize = 40 * Settings.Units.ScaleFactor;
		}

		/// <summary>
		///     Turn off fillmode setting.
		/// </summary>
		public static void SetFillMode() => DataBase.Database.Fillmode = false;

		/// <summary>
		///     Turn on line weight display.
		/// </summary>
		public static void SetLineWeightDisplay() => DataBase.Database.LineWeightDisplay = true;

		/// <summary>
		///		Get the SPM object associated to an <see cref="Entity"/> and remove from its list.
		/// </summary>
		public static bool Remove(Entity entity, bool raiseEvents = false) => Remove(entity.GetSPMObject(), raiseEvents);

		/// <summary>
		///		Remove a SPM object from its list.
		/// </summary>
		public static bool Remove(IEntityCreator<Entity> obj, bool raiseEvents = false)
		{
			if (obj is null)
				return false;

			switch (obj)
			{
				case NodeObject node:
					var nd = Nodes.Remove(node, raiseEvents, false);
					Nodes.Update();
					return nd;

				case StringerObject stringer:
					var str = Stringers.Remove(stringer, raiseEvents);
					Nodes.Update();
					return str;

				case PanelObject panel:
					return Panels.Remove(panel, raiseEvents);

				case ForceObject force:
					return Forces.Remove(force, raiseEvents);

				case ConstraintObject constraint:
					return Constraints.Remove(constraint, raiseEvents);

				default:
					return false;
			}
		}

		/// <summary>
		///		Create an SPM object associated to an <see cref="Entity"/> and add to its list.
		/// </summary>
		public static bool Add(Entity entity, bool raiseEvents = false) => Add(entity.CreateSPMObject(), raiseEvents);

		/// <summary>
		///		Add a SPM object to its list.
		/// </summary>
		public static bool Add(IEntityCreator<Entity> obj, bool raiseEvents = false)
		{
			if (obj is null)
				return false;

			switch (obj)
			{
				case NodeObject node:
					var add = Nodes.Add(node, raiseEvents, false);
					Nodes.Update();
					return add;

				case StringerObject stringer:
					var adds = Stringers.Add(stringer, raiseEvents);
					Nodes.Update();
					return adds;

				case PanelObject panel:
					return Panels.Add(panel, raiseEvents);

				case ForceObject force:
					return Forces.Add(force, raiseEvents);

				case ConstraintObject constraint:
					return Constraints.Add(constraint, raiseEvents);

				default:
					return false;
			}
		}

		/// <summary>
		///     Event to run after undo or redo commands.
		/// </summary>
		public static void On_UndoOrRedo(object sender, CommandEventArgs e)
		{
			if (CmdNames.Any(cmd => cmd.Contains(e.GlobalCommandName.ToUpper())))
				UpdateElements(false);
		}

		/// <summary>
		///     Event to execute when an object is erased or unerased.
		/// </summary>
		public static void On_ObjectErase(object sender, ObjectErasedEventArgs e)
		{
			var entity = (Entity) sender;

			if (entity is null)
				return;

			if (e.Erased)
			{
				var obj = entity.GetSPMObject();
				if (Remove(obj))
					Editor.WriteMessage($"\n{obj.GetType()} removed");
				return;
			}

			var obj1 = entity.CreateSPMObject();

			if (Add(obj1))
				Editor.WriteMessage($"\n{obj1.GetType()} added");
		}

		/// <summary>
		///		Event to execute when an object is unappended from database.
		/// </summary>
		public static void On_ObjectUnappended(object sender, EventArgs e)
		{
			var entity = (Entity) sender;

			if (entity is null)
				return;

			var obj = entity.GetSPMObject();

			Remove(obj);
			Editor.WriteMessage($"\n{obj.GetType()} removed");
		}
		
		/// <summary>
		///		Event to execute when an object is reappended to database.
		/// </summary>
		public static void On_ObjectReappended(object sender, EventArgs e)
		{
			var entity = (Entity) sender;

			if (entity is null)
				return;

			var obj = entity.CreateSPMObject();

			Add(obj);
			Editor.WriteMessage($"\n{obj.GetType()} added");
		}

		/// <summary>
		///		Event to execute when an object is copied.
		/// </summary>
		public static void On_ObjectCopied(object sender, ObjectEventArgs e)
		{
			var entity = (Entity)e.DBObject;

			if (entity is null)
				return;

			var obj = entity.CreateSPMObject();

			Add(obj);
			Editor.WriteMessage($"\n{obj.GetType()} copied.");
		}

		/// <summary>
		///     Event to run when an item is added to <see cref="StringerCrossSections" />.
		/// </summary>
		public static void On_CrossSection_Add(object sender, ItemEventArgs<CrossSection> e) => ElementWidths.Add(e.Item.Width);

		/// <summary>
		///     Event to run when an item is added to <see cref="StringerReinforcements" />.
		/// </summary>
		public static void On_StrRef_Add(object sender, ItemEventArgs<UniaxialReinforcement> e) => Steels.Add(e.Item?.Steel);

		/// <summary>
		///     Event to run when an item is added to <see cref="PanelReinforcements" />.
		/// </summary>
		public static void On_PanRef_Add(object sender, ItemEventArgs<WebReinforcementDirection> e) => Steels.Add(e.Item?.Steel);

		#endregion

		///// <summary>
		/////     Return an <see cref="SPMElement" /> from <paramref name="entity" />.
		///// </summary>
		///// <param name="entity">The <see cref="Entity" /> of SPM object.</param>
		//public static SPMElement GetElement(Entity entity)
		//{
		//	// Get element layer
		//	var layer = (Layer)Enum.Parse(typeof(Layer), entity.Layer);

		//	if (!ElementLayers.Contains(layer))
		//		return null;

		//	// Get concrete and units
		//	var parameters = ConcreteData.Parameters;
		//	var constitutive = ConcreteData.ConstitutiveModel;
		//	var units = Settings.Units;

		//	if (layer is Layer.IntNode || layer is Layer.ExtNode)
		//		return Nodes.GetByObjectId(entity.ObjectId).AsNode();

		//	// Read nodes
		//	var nodes = NodeList.ReadFromPoints(NodeCollection).Select(n => n.AsNode()).ToArray();

		//	if (layer is Layer.Stringer)
		//		return Stringers.Read((Line)entity, units, parameters, constitutive, nodes);

		//	if (layer is Layer.Panel)
		//		return Panels.Read((Solid)entity, units, parameters, constitutive, nodes);

		//	return null;
		//}

		///// <summary>
		/////     Return an <see cref="SPMElement" /> from <paramref name="objectId" />.
		///// </summary>
		///// <param name="objectId">The <see cref="ObjectId" /> of SPM object.</param>
		//public static SPMElement GetElement(ObjectId objectId) => GetElement(objectId.GetEntity());
	}
}