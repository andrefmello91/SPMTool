using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;

namespace SPMTool
{
	namespace AutoCAD
	{
		// AutoCAD variables
		public static class Current
		{
			// Get the current document, database and editor
			public static Document doc = Application.DocumentManager.MdiActiveDocument;
			public static Database db  = doc.Database;
			public static Editor edtr  = doc.Editor;

			// Get the coordinate system for transformations
			public static Matrix3d ucsMatrix     = edtr.CurrentUserCoordinateSystem;
			public static CoordinateSystem3d ucs = ucsMatrix.CoordinateSystem3d;

			// Define the appName
			public static string appName = "SPMTool";
		}

		// Color codes
		public enum Colors : short
		{
			Red     = 1,
			Yellow  = 2,
			Yellow1 = 41,
			Cyan    = 4,
			Blue1   = 5,
			Blue    = 150,
			Green   = 92,
			Grey    = 254
		}

		// Layer names
		public enum Layers
		{
			ExtNode,
			IntNode,
			Stringer,
			Panel,
			Support,
			Force,
			ForceText,
			StringerForce,
			PanelForce,
			CompressivePanelStress,
			TensilePanelStress,
			Displacements
		}

		// Block names
		public enum Blocks
		{
			SupportX,
			SupportY,
			SupportXY,
			ForceBlock,
			ShearBlock,
			CompressiveStressBlock,
			TensileStressBlock
		}
	}
}
