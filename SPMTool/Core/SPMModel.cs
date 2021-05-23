using System;
using System.Collections.Generic;
using System.Linq;
using andrefmello91.EList;
using andrefmello91.Extensions;
using andrefmello91.FEMAnalysis;
using andrefmello91.Material.Reinforcement;
using andrefmello91.SPMElements;
using andrefmello91.SPMElements.StringerProperties;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using SPMTool.Application;
using SPMTool.Core.Conditions;
using SPMTool.Core.Elements;
using SPMTool.Core.Materials;
using SPMTool.Enums;
using UnitsNet;
using static Autodesk.AutoCAD.ApplicationServices.Core.Application;

#nullable enable

namespace SPMTool.Core
{
	/// <summary>
	///     Model class
	/// </summary>
	public class SPMModel
	{

		#region Fields

		/// <summary>
		///     Get the application name.
		/// </summary>
		public const string AppName = "SPMTool";

		/// <summary>
		///     Collection of element <see cref="Layer" />'s.
		/// </summary>
		public static readonly Layer[] ElementLayers = { Layer.ExtNode, Layer.IntNode, Layer.Stringer, Layer.Panel, Layer.Force, Layer.Support };

		/// <summary>
		///     The list of opened documents.
		/// </summary>
		public static readonly List<SPMModel> OpenedModels;

		/// <summary>
		///     Collection of removed elements.
		/// </summary>
		public readonly List<IDBObjectCreator> Trash;

		#endregion

		#region Properties

		/// <summary>
		///     Get the active model.
		/// </summary>
		public static SPMModel ActiveModel => GetOpenedModel(DocumentManager.MdiActiveDocument)!;

		/// <summary>
		///     Get the AutoCAD database related to this.
		/// </summary>
		public Database AcadDatabase => AcadDocument.Database;

		/// <summary>
		///     Get the related document.
		/// </summary>
		public Document AcadDocument { get; }

		/// <summary>
		///     Get the Block Table <see cref="ObjectId" />.
		/// </summary>
		public ObjectId BlockTableId => AcadDatabase.BlockTableId;

		/// <summary>
		///     Concrete parameters and constitutive model.
		/// </summary>
		public ConcreteData ConcreteData { get; }

		/// <summary>
		///     The collection of <see cref="ConstraintObject" />'s in the model.
		/// </summary>
		public ConstraintList Constraints { get; }

		/// <summary>
		///     Get the editor of current document.
		/// </summary>
		public Editor Editor => AcadDocument.Editor;

		/// <summary>
		///     List of distinct widths from objects in the model.
		/// </summary>
		public EList<Length> ElementWidths { get; }

		/// <summary>
		///     The collection of <see cref="ForceObject" />'s in the model.
		/// </summary>
		public ForceList Forces { get; }

		/// <summary>
		///     Get the Layer Table <see cref="ObjectId" />.
		/// </summary>
		public ObjectId LayerTableId => AcadDatabase.LayerTableId;

		/// <summary>
		///     Get the document name.
		/// </summary>
		public string Name => AcadDocument.Name;

		/// <summary>
		///     The collection of <see cref="NodeObject" />'s in the model.
		/// </summary>
		public NodeList Nodes { get; }

		/// <summary>
		///     Get Named Objects <see cref="ObjectId" />.
		/// </summary>
		public ObjectId NodId => AcadDatabase.NamedObjectsDictionaryId;

		/// <summary>
		///     List of distinct reinforcements of panels in the model.
		/// </summary>
		public EList<WebReinforcementDirection> PanelReinforcements { get; }

		/// <summary>
		///     The collection of <see cref="PanelObject" />'s in the model.
		/// </summary>
		public PanelList Panels { get; }

		/// <summary>
		///     Application settings.
		/// </summary>
		public Settings Settings { get; }

		/// <summary>
		///     List of distinct steels of elements in the model.
		/// </summary>
		public EList<Steel> Steels { get; }

		/// <summary>
		///     List of distinct stringer's <see cref="CrossSection" />'s from objects in the model.
		/// </summary>
		public EList<CrossSection> StringerCrossSections { get; }

		/// <summary>
		///     List of distinct reinforcements of stringers in the model.
		/// </summary>
		public EList<UniaxialReinforcement> StringerReinforcements { get; }

		/// <summary>
		///     The collection of <see cref="StringerObject" />'s in the model.
		/// </summary>
		public StringerList Stringers { get; }

		/// <summary>
		///     Get the text height for model objects.
		/// </summary>
		public double TextHeight => 30 * Settings.Display.TextScale * Settings.Units.ScaleFactor;

		/// <summary>
		///     Get coordinate system.
		/// </summary>
		public CoordinateSystem3d Ucs => UcsMatrix.CoordinateSystem3d;

		/// <summary>
		///     Get current user coordinate system.
		/// </summary>
		public Matrix3d UcsMatrix => Editor.CurrentUserCoordinateSystem;

		#endregion

		#region Constructors

		/// <summary>
		///     Get the opened documents and set app events.
		/// </summary>
		static SPMModel()
		{
			OpenedModels = new List<SPMModel>();

			foreach (Document doc in DocumentManager)
				OpenedModels.Add(new SPMModel(doc));

			DocumentManager.DocumentCreated       += On_DocumentCreated;
			DocumentManager.DocumentToBeDestroyed += On_DocumentClosed;
		}

		/// <summary>
		///     Create a SPM model.
		/// </summary>
		/// <param name="acadDocument">The AutoCAD document.</param>
		public SPMModel(Document acadDocument)
		{
			AcadDocument = acadDocument;

			// Initiate dependencies
			RegisterApp(acadDocument);
			CreateLayers(acadDocument);
			CreateBlocks(acadDocument);

			// Get app settings
			Settings     = new Settings(AcadDatabase);
			ConcreteData = new ConcreteData(AcadDatabase);

			// Initiate trash
			Trash = new List<IDBObjectCreator>();

			// Get elements
			var unit    = Settings.Units.Geometry;
			Nodes       = NodeList.From(acadDocument, unit);
			Forces      = ForceList.From(acadDocument, unit);
			Constraints = ConstraintList.From(acadDocument, unit);
			Stringers   = StringerList.From(acadDocument, unit);
			Panels      = PanelList.From(acadDocument, unit);

			// Set events
			SetEvents(Nodes);
			SetEvents(Forces);
			SetEvents(Constraints);
			SetEvents(Stringers);
			SetEvents(Panels);

			// Get properties
			StringerCrossSections  = GetCrossSections(Stringers);
			ElementWidths          = Stringers.GetWidths().Concat(Panels.GetWidths()).Distinct().ToEList() ?? new EList<Length>();
			StringerReinforcements = GetStringerReinforcements(Stringers);
			PanelReinforcements    = GetPanelReinforcements(Panels);
			Steels                 = Stringers.GetSteels().Concat(Panels.GetSteels()).ToEList() ?? new EList<Steel>();

			// Move panels to bottom
			acadDocument.MoveToBottom(Panels.ObjectIds);

			// Register events
			SetEvents(Settings.Display);
			RegisterEventsToEntities();

			// Set parameters
			SetAppParameters();
		}

		#endregion

		#region Methods

		/// <summary>
		///     Get folder path of current file.
		/// </summary>
		public static string GetFilePath() => GetSystemVariable("DWGPREFIX").ToString()!;

		/// <summary>
		///     Get an opened SPM model that contains an <see cref="ObjectId" />.
		/// </summary>
		/// <param name="objectId">The <see cref="ObjectId" /> of an existing object.</param>
		public static SPMModel? GetOpenedModel(ObjectId objectId) => !objectId.IsNull
			? GetOpenedModel(objectId.Database)
			: null;

		/// <summary>
		///     Get an opened SPM model.
		/// </summary>
		/// <param name="documentName">The opened document name.</param>
		public static SPMModel? GetOpenedModel(string documentName) => OpenedModels.Find(d => d.Name == documentName);

		/// <summary>
		///     Get an opened SPM model.
		/// </summary>
		/// <param name="document">The opened document.</param>
		public static SPMModel GetOpenedModel(Document document) => GetOpenedModel(document.Name)!;

		/// <summary>
		///     <inheritdoc cref="GetOpenedModel(Document)" />
		/// </summary>
		/// <param name="database">The opened database.</param>
		public static SPMModel GetOpenedModel(Database database) => GetOpenedModel(database.GetDocument());

		/// <summary>
		///     Add the app to the Registered Applications Record.
		/// </summary>
		public static void RegisterApp(Document document)
		{
			// Start a transaction
			using var lck   = document.LockDocument();
			using var trans = document.Database.TransactionManager.StartTransaction();

			// Open the Registered Applications table for read
			var regAppTbl = (RegAppTable) trans.GetObject(document.Database.RegAppTableId, OpenMode.ForRead);

			if (regAppTbl.Has(AppName))
				return;

			var regAppTblRec = new RegAppTableRecord { Name = AppName };
			regAppTbl.UpgradeOpen();
			regAppTbl.Add(regAppTblRec);
			trans.AddNewlyCreatedDBObject(regAppTblRec, true);

			// Commit and dispose the transaction
			trans.Commit();
		}

		/// <summary>
		///     Create blocks for use in SPMTool.
		/// </summary>
		private static void CreateBlocks(Document document) => document.Create(Enum.GetValues(typeof(Block)).Cast<Block>().ToArray());

		/// <summary>
		///     Create layers for use with SPMTool.
		/// </summary>
		private static void CreateLayers(Document document) => document.Create(Enum.GetValues(typeof(Layer)).Cast<Layer>().ToArray());

		/// <summary>
		///     Create an SPM object associated to an <see cref="Entity" /> and add to the active model;
		/// </summary>
		public bool Add(Entity entity, bool raiseEvents = false) => Add(entity.CreateSPMObject(Settings.Units.Geometry), raiseEvents);

		/// <summary>
		///     Add a SPM object to the active model.
		/// </summary>
		public bool Add(IDBObjectCreator? obj, bool raiseEvents = false)
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
		public SPMInput? GenerateInput(AnalysisType analysisType, out bool dataOk, out string message)
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
		///     Read dictionary entries that contains <paramref name="name" />.
		/// </summary>
		/// <param name="name">The name of entry.</param>
		public IEnumerable<ResultBuffer> ReadDictionaryEntries(string name)
		{
			// Start a transaction
			using var trans = StartTransaction();

			using var nod = (DBDictionary) trans.GetObject(NodId, OpenMode.ForRead);

			var resList = new List<ResultBuffer>();

			// Check if name contains
			foreach (var entry in nod)
			{
				if (!entry.Key.Contains(name))
					continue;

				var xRec = (Xrecord) trans.GetObject(entry.Value, OpenMode.ForRead);

				// Add data
				resList.Add(xRec.Data);
			}

			return resList;
		}

		/// <summary>
		///     Read data on a dictionary entry.
		/// </summary>
		/// <param name="name">The name of entry.</param>
		/// <param name="fullName">Return only data corresponding to full name?</param>
		public TypedValue[]? ReadDictionaryEntry(string name, bool fullName = true)
		{
			// Start a transaction
			using var trans = StartTransaction();

			using var nod = (DBDictionary) trans.GetObject(NodId, OpenMode.ForRead);

			// Check if it exists as full name
			if (fullName && nod.Contains(name))

				// Read the concrete Xrecord
			{
				using var xrec = (Xrecord) trans.GetObject(nod.GetAt(name), OpenMode.ForRead);
				return
					xrec.Data.AsArray();
			}

			// Check if name contains
			foreach (var entry in nod)
			{
				if (!entry.Key.Contains(name))
					continue;

				// Read data
				var refXrec = (Xrecord) trans.GetObject(entry.Value, OpenMode.ForRead);

				return
					refXrec.Data.AsArray();
			}

			// Not set
			return null;
		}

		/// <summary>
		///     Get the SPM object associated to an <see cref="Entity" /> and remove from its list.
		/// </summary>
		public bool Remove(Entity entity, bool raiseEvents = false) => Remove(entity.GetSPMObject(), raiseEvents);

		/// <summary>
		///     Remove a SPM object from its list.
		/// </summary>
		public bool Remove(IDBObjectCreator? obj, bool raiseEvents = false)
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
		///     Save <paramref name="data" /> in <see cref="DBDictionary" />.
		/// </summary>
		/// <param name="data">The <see cref="ResultBuffer" /> to save.</param>
		/// <param name="name">The name to save.</param>
		/// <param name="overwrite">Overwrite data with the same <paramref name="name" />?</param>
		public void SaveDictionary(ResultBuffer data, string name, bool overwrite = true)
		{
			// Start a transaction
			using var trans = StartTransaction();

			using var nod = (DBDictionary) trans.GetObject(NodId, OpenMode.ForWrite);

			// Verify if object exists and must be overwrote
			if (!overwrite && nod.Contains(name))
				return;

			// Create and add data to an Xrecord
			var xRec = new Xrecord
			{
				Data = data
			};

			// Create the entry in the NOD and add to the transaction
			nod.SetAt(name, xRec);
			trans.AddNewlyCreatedDBObject(xRec, true);

			// Save the new object to the database
			trans.Commit();
		}

		/// <summary>
		///     Set application parameters for drawing.
		/// </summary>
		public void SetAppParameters()
		{
			using var lck = AcadDocument.LockDocument();
			UpdatePointSize();
			SetLineWeightDisplay();
		}

		/// <summary>
		///     Turn off fillmode setting.
		/// </summary>
		public void SetFillMode() => AcadDatabase.Fillmode = false;

		/// <summary>
		///     Turn on line weight display.
		/// </summary>
		public void SetLineWeightDisplay() => AcadDatabase.LineWeightDisplay = true;

		/// <summary>
		///     Start a new transaction in <see cref="AcadDatabase" />.
		/// </summary>
		public Transaction StartTransaction() => AcadDatabase.TransactionManager.StartTransaction();

		/// <summary>
		///     Update scale of forces and supports.
		/// </summary>
		/// <param name="oldScale">The old scale factor.</param>
		/// <param name="newScale">The new scale factor.</param>
		public void UpdateConditionsScale(double oldScale, double newScale) =>
			AcadDocument.UpdateScale(
				Constraints.Select(c => c.ObjectId)
					.Concat(Forces.Select(f => f.ObjectId))
					.ToList(),
				oldScale, newScale);

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
		///     Update size of points in the drawing.
		/// </summary>
		public void UpdatePointSize()
		{
			// Set the style for all point objects in the drawing
			AcadDatabase.Pdmode = 32;
			AcadDatabase.Pdsize = 40 * Settings.Units.ScaleFactor * Settings.Display.NodeScale;
			Editor.Regen();
		}

		/// <summary>
		///     Update text height in the model.
		/// </summary>
		public void UpdateTextHeight()
		{
			var objs = Forces.Select(f => f.ObjectId).ToList();
			var results = AcadDocument
				.GetObjectIds(Layer.StringerForce, Layer.PanelForce, Layer.PanelStress, Layer.ConcreteStress, Layer.Cracks)?
				.ToList();

			if (!results.IsNullOrEmpty())
				objs.AddRange(results);

			AcadDocument.UpdateTextHeight(objs, TextHeight);
		}

		/// <inheritdoc cref="StringerCrossSections" />
		private EList<CrossSection> GetCrossSections(StringerList stringers)
		{
			var list = stringers.GetCrossSections().ToEList() ?? new EList<CrossSection>();

			list.ItemAdded += On_CrossSection_Add;

			return list;
		}

		/// <inheritdoc cref="PanelReinforcements" />
		private EList<WebReinforcementDirection> GetPanelReinforcements(PanelList panels)
		{
			var list = panels.GetReinforcementDirections().ToEList() ?? new EList<WebReinforcementDirection>();

			list.ItemAdded += On_PanRef_Add;

			return list;
		}

		/// <inheritdoc cref="StringerReinforcements" />
		private EList<UniaxialReinforcement> GetStringerReinforcements(StringerList stringers)
		{
			var list = stringers.GetReinforcements().ToEList() ?? new EList<UniaxialReinforcement>();

			list.ItemAdded += On_StrRef_Add;

			return list;
		}

		/// <summary>
		///     Register events for AutoCAD entities.
		/// </summary>
		private void RegisterEventsToEntities()
		{
			// Get object ids
			var ids = Nodes.ObjectIds
				.Concat(Forces.ObjectIds)
				.Concat(Constraints.ObjectIds)
				.Concat(Stringers.ObjectIds)
				.Concat(Panels.ObjectIds)
				.ToList();

			// Register event
			AcadDocument.RegisterErasedEvent(ids, On_ObjectErase);
		}

		/// <summary>
		///     Set events to object creator lists.
		/// </summary>
		private void SetEvents<TDBObjectCreator>(IEList<TDBObjectCreator> list)
			where TDBObjectCreator : IDBObjectCreator, IEquatable<TDBObjectCreator>, IComparable<TDBObjectCreator>
		{
			list.ItemAdded    += On_ObjectAdded;
			list.RangeAdded   += On_ObjectsAdded;
			list.ItemRemoved  += On_ObjectRemoved;
			list.RangeRemoved += On_ObjectsRemoved;
		}

		/// <summary>
		///     Set events to display settings change.
		/// </summary>
		private void SetEvents(DisplaySettings displaySettings)
		{
			displaySettings.NodeScaleChanged      += On_NodeScaleChange;
			displaySettings.ConditionScaleChanged += On_ConditionScaleChange;
			displaySettings.TextScaleChanged      += On_TextScaleChange;
		}

		#endregion

		#region Events

		/// <summary>
		///     Event to execute when an object is erased or unerased in a database.
		/// </summary>
		public static void On_ObjectErase(object sender, ObjectErasedEventArgs e)
		{
			if (sender is not Entity entity || ElementLayers.Contains((Layer) Enum.Parse(typeof(Layer), entity.Layer)) || GetOpenedModel(entity.ObjectId) is not { } model)
				return;

			switch (e.Erased)
			{
				case true when entity.GetSPMObject() is { } obj && model.Remove(obj):

					model.Trash.Add(obj);

					model.Editor.WriteMessage($"\n{obj.Name} removed");

					return;

				case false:

					var obj1 = model.Trash.Find(t => t.ObjectId == entity.ObjectId) ?? entity.CreateSPMObject(model.Settings.Units.Geometry);

					if (!model.Add(obj1))
						return;

					model.Trash.Remove(obj1);

					model.Editor.WriteMessage($"\n{obj1.Name} re-added");

					return;
			}
		}

		private static void On_DocumentClosed(object sender, DocumentCollectionEventArgs e) => OpenedModels.RemoveAll(d => d.Name == e.Document.Name);

		private static void On_DocumentCreated(object sender, DocumentCollectionEventArgs e) => OpenedModels.Add(new SPMModel(e.Document));

		/// <summary>
		///     Event to execute when an object is copied.
		/// </summary>
		public void On_ObjectCopied(object sender, ObjectEventArgs e)
		{
			var entity = (Entity) e.DBObject;

			if (entity is null)
				return;

			var obj = entity.CreateSPMObject(Settings.Units.Geometry);

			Add(obj);

			// SPMDocument.Editor.WriteMessage($"\n{obj.GetType()} copied.");
		}

		private void On_ConditionScaleChange(object sender, ScaleChangedEventArgs e) => UpdateConditionsScale(e.OldScale, e.NewScale);

		/// <summary>
		///     Event to run when an item is added to <see cref="StringerCrossSections" />.
		/// </summary>
		private void On_CrossSection_Add(object sender, ItemEventArgs<CrossSection> e) => ElementWidths.Add(e.Item.Width);

		private void On_NodeScaleChange(object sender, ScaleChangedEventArgs e) => UpdatePointSize();


		/// <summary>
		///     Event to execute when an object is added to a list.
		/// </summary>
		private void On_ObjectAdded<TDBObjectCreator>(object? sender, ItemEventArgs<TDBObjectCreator> e)
			where TDBObjectCreator : IDBObjectCreator
		{
			if (e.Item is not { } obj)
				return;

			// Remove from trash
			Trash.Remove(obj);

			// Add to drawing
			AcadDocument.AddObject(obj);
		}

		/// <summary>
		///     Event to execute when an object is removed from a list.
		/// </summary>
		private void On_ObjectRemoved<TDBObjectCreator>(object? sender, ItemEventArgs<TDBObjectCreator> e)
			where TDBObjectCreator : IDBObjectCreator
		{
			if (e.Item is not { } obj)
				return;

			// Add to trash
			if (!Trash.Contains(obj))
				Trash.Add(obj);

			// Remove
			AcadDocument.EraseObject(obj);
		}

		/// <summary>
		///     Event to execute when a range of objects is added to a list.
		/// </summary>
		private void On_ObjectsAdded<TDBObjectCreator>(object? sender, RangeEventArgs<TDBObjectCreator> e)
			where TDBObjectCreator : IDBObjectCreator
		{
			var objs = e.ItemCollection;

			if (objs.IsNullOrEmpty())
				return;

			Trash.RemoveAll(objs.Cast<IDBObjectCreator>().Contains);

			// Add to drawing
			AcadDocument.AddObjects(objs);
		}

		/// <summary>
		///     Event to execute when a range of objects is removed from a list.
		/// </summary>
		private void On_ObjectsRemoved<TDBObjectCreator>(object? sender, RangeEventArgs<TDBObjectCreator> e)
			where TDBObjectCreator : IDBObjectCreator
		{
			var objs = e.ItemCollection;

			if (objs.IsNullOrEmpty())
				return;

			// Add to trash
			Trash.RemoveAll(objs.Cast<IDBObjectCreator>().Where(obj => obj is not null).Contains);

			// Remove
			AcadDocument.EraseObjects(objs);
		}

		/// <summary>
		///     Event to run when an item is added to <see cref="PanelReinforcements" />.
		/// </summary>
		private void On_PanRef_Add(object sender, ItemEventArgs<WebReinforcementDirection> e) => Steels.Add(e.Item?.Steel);

		/// <summary>
		///     Event to run when an item is added to <see cref="StringerReinforcements" />.
		/// </summary>
		private void On_StrRef_Add(object sender, ItemEventArgs<UniaxialReinforcement> e) => Steels.Add(e.Item?.Steel);

		private void On_TextScaleChange(object sender, ScaleChangedEventArgs e) => UpdateTextHeight();

		#endregion

	}
}