using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using SPMTool.Application;
using SPMTool.Core.Materials;
using SPMTool.Enums;
using SPMTool.Extensions;
using static Autodesk.AutoCAD.ApplicationServices.Core.Application;

namespace SPMTool.Core
{
	/// <summary>
	///     DataBase class.
	/// </summary>
	public static class DataBase
	{
		#region Fields

		/// <summary>
		///     Get the application name.
		/// </summary>
		public const string AppName = "SPMTool";

		/// <summary>
		///		Application settings.
		/// </summary>
		public static readonly Settings Settings = new Settings();

		/// <summary>
		///		Concrete parameters and constitutive model.
		/// </summary>
		public static readonly ConcreteData ConcreteData = new ConcreteData();

		#endregion

		#region Properties

		/// <summary>
		///     Get BlockTable for read.
		/// </summary>
		public static BlockTable BlockTable => (BlockTable) StartTransaction().GetObject(BlockTableId, OpenMode.ForRead);

		/// <summary>
		///     Get the Block Table <see cref="ObjectId" />.
		/// </summary>
		public static ObjectId BlockTableId => Database.BlockTableId;

		/// <summary>
		///     Get current <see cref="Autodesk.AutoCAD.DatabaseServices.Database" />.
		/// </summary>
		public static Database Database => Document.Database;

		/// <summary>
		///     Get current active <see cref="Autodesk.AutoCAD.ApplicationServices.Document" />.
		/// </summary>
		public static Document Document => DocumentManager.MdiActiveDocument;

		/// <summary>
		///     Get the Layer Table <see cref="ObjectId" />.
		/// </summary>
		public static ObjectId LayerTableId => Database.LayerTableId;

		/// <summary>
		///     Get Named Objects Dictionary for read.
		/// </summary>
		public static DBDictionary Nod => (DBDictionary) StartTransaction().GetObject(NodId, OpenMode.ForRead);

		/// <summary>
		///     Get Named Objects <see cref="ObjectId" />.
		/// </summary>
		public static ObjectId NodId => Database.NamedObjectsDictionaryId;

		/// <summary>
		///     Get coordinate system.
		/// </summary>
		public static CoordinateSystem3d Ucs => UcsMatrix.CoordinateSystem3d;

		/// <summary>
		///     Get current user coordinate system.
		/// </summary>
		public static Matrix3d UcsMatrix => Model.Editor.CurrentUserCoordinateSystem;

		#endregion

		#region  Methods

		/// <summary>
		///     Start a new transaction in <see cref="Database" />.
		/// </summary>
		public static Transaction StartTransaction() => Database.TransactionManager.StartTransaction();

		/// <summary>
		///     Add the app to the Registered Applications Record.
		/// </summary>
		public static void RegisterApp()
		{
			// Start a transaction
			using var trans = StartTransaction();

			// Open the Registered Applications table for read
			using var regAppTbl = (RegAppTable) trans.GetObject(Database.RegAppTableId, OpenMode.ForRead);

			if (regAppTbl.Has(AppName))
				return;

			using var regAppTblRec = new RegAppTableRecord { Name = AppName };
			regAppTbl.UpgradeOpen();
			regAppTbl.Add(regAppTblRec);
			trans.AddNewlyCreatedDBObject(regAppTblRec, true);

			// Commit and dispose the transaction
			trans.Commit();
		}

		/// <summary>
		///     Create layers for use with SPMTool.
		/// </summary>
		public static void CreateLayers()
		{
			// Get the layer enum as an array
			var layers = Enum.GetValues(typeof(Layer)).Cast<Layer>().ToArray();

			// Create layers
			layers.Create();
		}

		/// <summary>
		///     Get folder path of current file.
		/// </summary>
		public static string GetFilePath() => GetSystemVariable("DWGPREFIX").ToString()!;

		/// <summary>
		///     Save <paramref name="data" /> in <see cref="DBDictionary" />.
		/// </summary>
		/// <param name="data">The <see cref="ResultBuffer" /> to save.</param>
		/// <param name="name">The name to save.</param>
		/// <param name="overwrite">Overwrite data with the same <paramref name="name" />?</param>
		public static void SaveDictionary(ResultBuffer data, string name, bool overwrite = true)
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
		///     Read data on a dictionary entry.
		/// </summary>
		/// <param name="name">The name of entry.</param>
		/// <param name="fullName">Return only data corresponding to full name?</param>
		public static TypedValue[]? ReadDictionaryEntry(string name, bool fullName = true)
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
		///     Read dictionary entries that contains <paramref name="name" />.
		/// </summary>
		/// <param name="name">The name of entry.</param>
		public static IEnumerable<ResultBuffer> ReadDictionaryEntries(string name)
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

		#endregion
	}
}