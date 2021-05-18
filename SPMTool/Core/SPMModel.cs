using System;
using System.Collections.Generic;
using System.Linq;
using andrefmello91.EList;
using andrefmello91.Extensions;
using andrefmello91.FEMAnalysis;
using andrefmello91.Material.Reinforcement;
using andrefmello91.SPMElements;
using andrefmello91.SPMElements.StringerProperties;
using Autodesk.AutoCAD.DatabaseServices;
using SPMTool.Core.Conditions;
using SPMTool.Core.Elements;
using SPMTool.Enums;

using UnitsNet;
using static SPMTool.Core.SPMDatabase;

#nullable disable

namespace SPMTool.Core
{
	/// <summary>
	///     Model class
	/// </summary>
	public class SPMModel
	{

		#region Fields

		/// <summary>
		///     Collection of element <see cref="Layer" />'s.
		/// </summary>
		public static readonly Layer[] ElementLayers = { Layer.ExtNode, Layer.IntNode, Layer.Stringer, Layer.Panel, Layer.Force, Layer.Support };

		/// <summary>
		///		Get the active model.
		/// </summary>
		public static SPMModel ActiveModel => SPMDocument.ActiveDocument.Model;
		
		/// <summary>
		///     Collection of removed elements.
		/// </summary>
		public readonly List<IDBObjectCreator> Trash;

		/// <summary>
		///		Get the database of the model.
		/// </summary>
		public SPMDatabase Database { get; }
		
		/// <summary>
		///     The collection of <see cref="NodeObject" />'s in the model.
		/// </summary>
		public NodeList Nodes { get; }

		/// <summary>
		///     The collection of <see cref="StringerObject" />'s in the model.
		/// </summary>
		public StringerList Stringers { get; }

		/// <summary>
		///     The collection of <see cref="PanelObject" />'s in the model.
		/// </summary>
		public PanelList Panels { get; }

		/// <summary>
		///     The collection of <see cref="ForceObject" />'s in the model.
		/// </summary>
		public ForceList Forces { get; }

		/// <summary>
		///     The collection of <see cref="ConstraintObject" />'s in the model.
		/// </summary>
		public ConstraintList Constraints { get; }

		/// <summary>
		///     List of distinct widths from objects in the model.
		/// </summary>
		public EList<Length> ElementWidths { get; }

		/// <summary>
		///     List of distinct stringer's <see cref="CrossSection" />'s from objects in the model.
		/// </summary>
		public EList<CrossSection> StringerCrossSections { get; }

		/// <summary>
		///     List of distinct reinforcements of stringers in the model.
		/// </summary>
		public EList<UniaxialReinforcement> StringerReinforcements { get; }

		/// <summary>
		///     List of distinct reinforcements of panels in the model.
		/// </summary>
		public EList<WebReinforcementDirection> PanelReinforcements { get; }

		/// <summary>
		///     List of distinct steels of elements in the model.
		/// </summary>
		public EList<Steel> Steels { get; }

		#endregion

		#region Properties

		/// <summary>
		///		Get the text height for model objects.
		/// </summary>
		public double TextHeight => 30 * Database.Settings.Display.TextScale * Database.Settings.Units.ScaleFactor;
		
		#endregion

		#region Constructors

		/// <summary>
		///		Create a SPM Model.
		/// </summary>
		/// <param name="database">The autocad database.</param>
		public SPMModel(Database database)
		{
			Database = new SPMDatabase(database);
			
			// Initiate trash
			Trash = new List<IDBObjectCreator>();

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

			// Move panels to bottom
			Panels.Select(p => p.ObjectId).ToList().MoveToBottom();

			// Register events
			RegisterEventsToEntities();
		}

		#endregion

		#region Methods

		/// <summary>
		///     Create an SPM object associated to an <see cref="Entity" /> and add to the active model;
		/// </summary>
		public bool Add(Entity entity, bool raiseEvents = false) => Add(entity.CreateSPMObject(), raiseEvents);

		/// <summary>
		///     Add a SPM object to the active model.
		/// </summary>
		public bool Add(IDBObjectCreator obj, bool raiseEvents = false)
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
		///     Get the <see cref="FEMInput" /> from objects in drawing.
		/// </summary>
		/// <param name="dataOk">Returns true if data is consistent to start analysis.</param>
		/// <param name="message">Message to show if data is inconsistent.</param>
		/// <param name="analysisType">The type of analysis to perform.</param>
		public SPMInput GenerateInput(AnalysisType analysisType, out bool dataOk, out string message)
		{
			// Get the element model
			var elementModel = analysisType switch
			{
				AnalysisType.Linear => ElementModel.Elastic,
				_                   => ElementModel.Nonlinear
			};

			// Read elements
			var nodes     = Nodes.GetElements().Cast<Node>().ToList();
			var stringers = Stringers.GetElements(nodes, elementModel).ToList();
			var panels    = Panels.GetElements(nodes, elementModel).ToList();

			// Verify if there is stringers and nodes at least
			if (!nodes.Any() || !stringers.Any())
			{
				dataOk  = false;
				message = "Please input model geometry";
				return null;
			}

			// Generate input
			dataOk  = true;
			message = string.Empty;

			return
				SPMInput.From(stringers, panels, nodes, analysisType);
		}

		/// <summary>
		///     Event to run when an item is added to <see cref="StringerCrossSections" />.
		/// </summary>
		public void On_CrossSection_Add(object sender, ItemEventArgs<CrossSection> e) => ElementWidths.Add(e.Item.Width);

		/// <summary>
		///     Event to execute when an object is copied.
		/// </summary>
		public void On_ObjectCopied(object sender, ObjectEventArgs e)
		{
			var entity = (Entity) e.DBObject;

			if (entity is null)
				return;

			var obj = entity.CreateSPMObject();

			Add(obj);
			// SPMDocument.Editor.WriteMessage($"\n{obj.GetType()} copied.");
		}

		/// <summary>
		///     Event to execute when an object is erased or unerased.
		/// </summary>
		public void On_ObjectErase(object sender, ObjectErasedEventArgs e)
		{
			var entity = (Entity) sender;

			if (entity is null)
				return;

			if (e.Erased)
			{
				var obj = entity.GetSPMObject();

				if (!Remove(obj))
					return;

				Trash.Add(obj);

				// SPMDocument.Editor.WriteMessage($"\n{obj.Name} removed");

				return;
			}

			IDBObjectCreator obj1;
			try
			{
				obj1 = Trash.Find(t => t.ObjectId == entity.ObjectId);
			}
			catch
			{
				// SPMDocument.Editor.WriteMessage("\nNot found in trash.");
				obj1 = entity.CreateSPMObject();
			}

			if (!Add(obj1))
				return;

			Trash.Remove(obj1);

			// SPMDocument.Editor.WriteMessage($"\n{obj1.Name} re-added");
		}

		/// <summary>
		///     Event to run when an item is added to <see cref="PanelReinforcements" />.
		/// </summary>
		public void On_PanRef_Add(object sender, ItemEventArgs<WebReinforcementDirection> e) => Steels.Add(e.Item?.Steel);

		/// <summary>
		///     Event to run when an item is added to <see cref="StringerReinforcements" />.
		/// </summary>
		public void On_StrRef_Add(object sender, ItemEventArgs<UniaxialReinforcement> e) => Steels.Add(e.Item?.Steel);

		/// <summary>
		///     Register events for AutoCAD entities.
		/// </summary>
		public void RegisterEventsToEntities()
		{
			// Get object ids
			var ids = Nodes.Select(n => n.ObjectId)
				.Concat(Forces.Select(f => f.ObjectId))
				.Concat(Constraints.Select(c => c.ObjectId))
				.Concat(Stringers.Select(s => s.ObjectId))
				.Concat(Panels.Select(p => p.ObjectId))
				.ToList();

			// Register event
			ids.RegisterErasedEvent(On_ObjectErase);
		}

		/// <summary>
		///     Get the SPM object associated to an <see cref="Entity" /> and remove from its list.
		/// </summary>
		public bool Remove(Entity entity, bool raiseEvents = false) => Remove(entity.GetSPMObject(), raiseEvents);

		/// <summary>
		///     Remove a SPM object from its list.
		/// </summary>
		public bool Remove(IDBObjectCreator obj, bool raiseEvents = false)
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
		///		Update scale of forces and supports.
		/// </summary>
		/// <param name="oldScale">The old scale factor.</param>
		/// <param name="newScale">The new scale factor.</param>
		public void UpdateConditionsScale(double oldScale, double newScale) =>
			Constraints.Select(c => c.ObjectId)
				.Concat(Forces.Select(f => f.ObjectId))
				.UpdateScale(oldScale, newScale);

		/// <summary>
		///     Update all the elements in the drawing.
		/// </summary>
		/// <param name="addNodes">Add nodes to stringer start, mid and end points?</param>
		/// <param name="removeNodes">Remove nodes at unnecessary positions?</param>
		public void UpdateElements(bool addNodes = true, bool removeNodes = true)
		{
			Stringers.Update();
			Panels.Update();
			Nodes.Update(addNodes, removeNodes);
		}

		/// <summary>
		///		Update text height in the model.
		/// </summary>
		public void UpdateTextHeight()
		{
			var objs    = Forces.Select(f => f.ObjectId).ToList();
			var rLayers = new[] { Layer.StringerForce, Layer.PanelForce, Layer.PanelStress, Layer.ConcreteStress, Layer.Cracks }.Select(l => $"{l}").ToList();
			var results = Database.AcadDatabase.GetDocument().GetObjectIds(rLayers)?.ToList();
			
			if (!results.IsNullOrEmpty())
				objs.AddRange(results);
			
			objs.UpdateTextHeight(TextHeight);
		}
		
		/// <inheritdoc cref="StringerCrossSections" />
		private EList<CrossSection> GetCrossSections()
		{
			var list = Stringers.GetCrossSections().ToEList() ?? new EList<CrossSection>();

			list.ItemAdded += On_CrossSection_Add;

			return list;
		}

		/// <inheritdoc cref="PanelReinforcements" />
		private EList<WebReinforcementDirection> GetPanelReinforcements()
		{
			var list = Panels.GetReinforcementDirections().ToEList() ?? new EList<WebReinforcementDirection>();

			list.ItemAdded += On_PanRef_Add;

			return list;
		}

		/// <inheritdoc cref="StringerReinforcements" />
		private EList<UniaxialReinforcement> GetStringerReinforcements()
		{
			var list = Stringers.GetReinforcements().ToEList() ?? new EList<UniaxialReinforcement>();

			list.ItemAdded += On_StrRef_Add;

			return list;
		}

		#endregion

	}
}