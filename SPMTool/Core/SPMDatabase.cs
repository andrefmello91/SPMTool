using System.Collections.Generic;
using System.Linq;
using andrefmello91.EList;
using andrefmello91.Material.Reinforcement;
using andrefmello91.SPMElements.StringerProperties;
using Autodesk.AutoCAD.DatabaseServices;
using SPMTool.Application;
using SPMTool.Core.Elements;
using SPMTool.Core.Materials;
using UnitsNet;
#nullable enable

namespace SPMTool.Core
{
	/// <summary>
	///     DataBase class.
	/// </summary>
	public class SPMDatabase
	{

		#region Properties

		/// <summary>
		///     Get current <see cref="Autodesk.AutoCAD.DatabaseServices.Database" />.
		/// </summary>
		public static SPMDatabase ActiveDatabase => SPMModel.ActiveModel.Database;

		/// <summary>
		///     Get the opened SPM databases.
		/// </summary>
		public static List<SPMDatabase> OpenedDatabases => SPMModel.OpenedModels.Select(m => m.Database).ToList();

		/// <summary>
		///     Get the AutoCAD database related to this.
		/// </summary>
		public Database AcadDatabase { get; }

		/// <summary>
		///     Get the Block Table <see cref="ObjectId" />.
		/// </summary>
		public ObjectId BlockTableId => AcadDatabase.BlockTableId;

		/// <summary>
		///     Concrete parameters and constitutive model.
		/// </summary>
		public ConcreteData ConcreteData { get; }

		/// <summary>
		///     Get the document name associated to this database.
		/// </summary>
		public string DocName { get; }

		/// <summary>
		///     List of distinct widths from objects in the model.
		/// </summary>
		public EList<Length> ElementWidths { get; }

		/// <summary>
		///     Get the Layer Table <see cref="ObjectId" />.
		/// </summary>
		public ObjectId LayerTableId => AcadDatabase.LayerTableId;

		/// <summary>
		///     Get Named Objects <see cref="ObjectId" />.
		/// </summary>
		public ObjectId NodId => AcadDatabase.NamedObjectsDictionaryId;

		/// <summary>
		///     List of distinct reinforcements of panels in the model.
		/// </summary>
		public EList<WebReinforcementDirection> PanelReinforcements { get; }

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

		#endregion

		#region Constructors

		/// <summary>
		///     Create a SPM database.
		/// </summary>
		/// <param name="model">The SPM model.</param>
		public SPMDatabase(SPMModel model)
		{
			AcadDatabase = model.AcadDocument.Database;
			DocName      = model.Name;

			// Get app settings
			Settings     = new Settings(AcadDatabase);
			ConcreteData = new ConcreteData(AcadDatabase);

			// Get properties
			StringerCrossSections  = GetCrossSections(model.Stringers);
			ElementWidths          = model.Stringers.GetWidths().Concat(model.Panels.GetWidths()).Distinct().ToEList() ?? new EList<Length>();
			StringerReinforcements = GetStringerReinforcements(model.Stringers);
			PanelReinforcements    = GetPanelReinforcements(model.Panels);
			Steels                 = model.Stringers.GetSteels().Concat(model.Panels.GetSteels()).ToEList() ?? new EList<Steel>();
		}

		#endregion

		#region Methods

		/// <summary>
		///     Get an opened database from a document name.
		/// </summary>
		/// <param name="documentName">The document name.</param>
		public static SPMDatabase? GetOpenedDatabase(string documentName) => OpenedDatabases.Find(d => d.DocName == documentName);

		/// <summary>
		///     Get an opened database from an AutoCAD database.
		/// </summary>
		/// <param name="database">The AutoCAD database.</param>
		public static SPMDatabase GetOpenedDatabase(Database database) => GetOpenedDatabase(database.GetDocument().Name)!;

		/// <summary>
		///     Get an opened database from an <see cref="ObjectId" />.
		/// </summary>
		/// <param name="objectId">The <see cref="ObjectId" />.</param>
		public static SPMDatabase? GetOpenedDatabase(ObjectId objectId) => !objectId.IsNull
			? GetOpenedDatabase(objectId.Database)
			: null;

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
		///     Start a new transaction in <see cref="AcadDatabase" />.
		/// </summary>
		public Transaction StartTransaction() => AcadDatabase.TransactionManager.StartTransaction();

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

		#endregion

		#region Events

		/// <summary>
		///     Event to run when an item is added to <see cref="StringerCrossSections" />.
		/// </summary>
		private void On_CrossSection_Add(object sender, ItemEventArgs<CrossSection> e) => ElementWidths.Add(e.Item.Width);

		/// <summary>
		///     Event to run when an item is added to <see cref="PanelReinforcements" />.
		/// </summary>
		private void On_PanRef_Add(object sender, ItemEventArgs<WebReinforcementDirection> e) => Steels.Add(e.Item?.Steel);

		/// <summary>
		///     Event to run when an item is added to <see cref="StringerReinforcements" />.
		/// </summary>
		private void On_StrRef_Add(object sender, ItemEventArgs<UniaxialReinforcement> e) => Steels.Add(e.Item?.Steel);

		#endregion

	}
}