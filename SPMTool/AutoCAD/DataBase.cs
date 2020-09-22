using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Material.Concrete;

namespace SPMTool.AutoCAD
{
	/// <summary>
    /// DataBase class.
    /// </summary>
	public static class DataBase
	{
		/// <summary>
		/// Get the application name.
		/// </summary>
		public static string AppName => "SPMTool";

        /// <summary>
        /// Get current active <see cref="Autodesk.AutoCAD.ApplicationServices.Document"/>.
        /// </summary>
        public static Document Document => Application.DocumentManager.MdiActiveDocument;

        /// <summary>
        /// Get current <see cref="Autodesk.AutoCAD.DatabaseServices.Database"/>.
        /// </summary>
        public static Database Database => Document.Database;

        /// <summary>
        /// Get application <see cref="Autodesk.AutoCAD.EditorInput.Editor"/>.
        /// </summary>
        public static Editor Editor => Document.Editor;

        /// <summary>
        /// Get Named Objects <see cref="ObjectId"/>.
        /// </summary>
        public static ObjectId Nod => Database.NamedObjectsDictionaryId;

		/// <summary>
        /// Get current user coordinate system.
        /// </summary>
		public static Matrix3d UcsMatrix => Editor.CurrentUserCoordinateSystem;

		/// <summary>
        /// Get coordinate system.
        /// </summary>
		public static CoordinateSystem3d Ucs => UcsMatrix.CoordinateSystem3d;

		/// <summary>
        /// Get <see cref="SPMTool.Units"/> saved in database.
        /// </summary>
		public static Units Units => Config.ReadUnits();

		/// <summary>
        /// Get <see cref="Concrete"/> saved in database.
        /// </summary>
		public static Concrete Concrete => Material.ReadConcreteData();
	}
}