using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using SPMTool.Enums;
using static Autodesk.AutoCAD.ApplicationServices.Core.Application;

namespace SPMTool.Core
{
	/// <summary>
	///     SPM document class.
	/// </summary>
	public class SPMDocument
	{

		#region Fields

		/// <summary>
		///     Get the application name.
		/// </summary>
		public const string AppName = "SPMTool";
		/// <summary>
		///     The list of opened documents.
		/// </summary>
		public static readonly List<SPMDocument> OpenedDocuments;

		#endregion

		#region Properties

		/// <summary>
		///     Get current active <see cref="SPMDocument" />.
		/// </summary>
		public static SPMDocument ActiveDocument => GetOpenedDocument(DocumentManager.MdiActiveDocument);

		/// <summary>
		///     Get the editor of current document.
		/// </summary>
		public Autodesk.AutoCAD.EditorInput.Editor Editor => AcadDocument.Editor;

		/// <summary>
		///     Get coordinate system.
		/// </summary>
		public CoordinateSystem3d Ucs => UcsMatrix.CoordinateSystem3d;

		/// <summary>
		///     Get current user coordinate system.
		/// </summary>
		public Matrix3d UcsMatrix => Editor.CurrentUserCoordinateSystem;

		/// <summary>
		///     Get the database.
		/// </summary>
		public SPMDatabase Database => Model.Database;

		/// <summary>
		///		Get the model.
		/// </summary>
		public SPMModel Model { get; }
		
		/// <summary>
		///     Get the related document.
		/// </summary>
		public Document AcadDocument { get; }

		/// <summary>
		///     Get the document name.
		/// </summary>
		public string Name => AcadDocument.Name;

		#endregion

		#region Constructors

		/// <summary>
		///     Get the opened documents and set app events.
		/// </summary>
		static SPMDocument()
		{
			OpenedDocuments = new List<SPMDocument>();

			foreach (Document doc in DocumentManager)
				OpenedDocuments.Add(new SPMDocument(doc));

			DocumentManager.DocumentCreated       += On_DocumentCreated;
			DocumentManager.DocumentToBeDestroyed += On_DocumentClosed;
		}

		/// <summary>
		///     Create a SPM document.
		/// </summary>
		/// <param name="acadDocument">The AutoCAD document.</param>
		public SPMDocument(Document acadDocument)
		{
			AcadDocument = acadDocument;
			Model    = new SPMModel(acadDocument.Database);
			RegisterApp(acadDocument);
			CreateLayers(acadDocument);
			SetAppParameters();
		}

		#endregion

		#region Methods

		/// <summary>
		///     Create layers for use with SPMTool.
		/// </summary>
		public static void CreateLayers(Document document) => document.Create(Enum.GetValues(typeof(Layer)).Cast<Layer>().ToArray());

		/// <summary>
		///     Get folder path of current file.
		/// </summary>
		public static string GetFilePath() => GetSystemVariable("DWGPREFIX").ToString()!;

		/// <summary>
		///     Get an opened document.
		/// </summary>
		/// <param name="document">The opened document.</param>
		public static SPMDocument GetOpenedDocument(Document document) => OpenedDocuments.Find(d => d.Name == document.Name);

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
		///     Set application parameters for drawing.
		/// </summary>
		public void SetAppParameters()
		{
			UpdatePointSize();
			SetLineWeightDisplay();
		}

		/// <summary>
		///     Turn off fillmode setting.
		/// </summary>
		public void SetFillMode() => Database.AcadDatabase.Fillmode = false;

		/// <summary>
		///     Turn on line weight display.
		/// </summary>
		public void SetLineWeightDisplay() => Database.AcadDatabase.LineWeightDisplay = true;

		/// <summary>
		///     Update size of points in the drawing.
		/// </summary>
		public void UpdatePointSize()
		{
			// Set the style for all point objects in the drawing
			Database.AcadDatabase.Pdmode = 32;
			Database.AcadDatabase.Pdsize = 40 * Database.Settings.Units.ScaleFactor * Database.Settings.Display.NodeScale;
			Editor.Regen();
		}

		private static void On_DocumentClosed(object sender, DocumentCollectionEventArgs e) => OpenedDocuments.RemoveAll(d => d.Name == e.Document.Name);

		private static void On_DocumentCreated(object sender, DocumentCollectionEventArgs e) => OpenedDocuments.Add(new SPMDocument(e.Document));

		#endregion

	}
}