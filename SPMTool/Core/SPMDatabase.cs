using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using SPMTool.Application;
using SPMTool.Core.Blocks;
using SPMTool.Core.Materials;
using SPMTool.Enums;

using static Autodesk.AutoCAD.ApplicationServices.Core.Application;

#nullable enable

namespace SPMTool.Core
{
	/// <summary>
	///     DataBase class.
	/// </summary>
	public class SPMDatabase
	{

		#region Fields

		/// <summary>
		///     Application settings.
		/// </summary>
		public Settings Settings { get; }

		/// <summary>
		///     Concrete parameters and constitutive model.
		/// </summary>
		public ConcreteData ConcreteData { get; }

		#endregion

		#region Properties

		/// <summary>
		///     Get the Block Table <see cref="ObjectId" />.
		/// </summary>
		public ObjectId BlockTableId => AcadDatabase.BlockTableId;

		/// <summary>
		///		Get the AutoCAD database related to this.
		/// </summary>
		public Database AcadDatabase { get; }
		
		/// <summary>
		///     Get current <see cref="Autodesk.AutoCAD.DatabaseServices.Database" />.
		/// </summary>
		public static SPMDatabase ActiveDatabase => SPMModel.ActiveModel.Database;

		/// <summary>
		///     Get the Layer Table <see cref="ObjectId" />.
		/// </summary>
		public ObjectId LayerTableId => AcadDatabase.LayerTableId;

		/// <summary>
		///     Get Named Objects <see cref="ObjectId" />.
		/// </summary>
		public ObjectId NodId => AcadDatabase.NamedObjectsDictionaryId;

		#endregion

		#region Constructors

		/// <summary>
		///		Create a SPM database.
		/// </summary>
		/// <param name="acadDatabase">The AutoCAD database.</param>
		public SPMDatabase(Database acadDatabase)
		{
			AcadDatabase = acadDatabase;
			// Get app settings
			Settings     = new Settings(acadDatabase);
			ConcreteData = new ConcreteData(acadDatabase);
		}

		#endregion

		#region Methods

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

		#endregion

	}
}