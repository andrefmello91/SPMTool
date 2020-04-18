using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;

namespace SPMTool
{
    // AutoCAD variables
    public static class AutoCAD
    {
        // Get the current document, database and editor
        public static Document curDoc = Application.DocumentManager.MdiActiveDocument;
        public static Database curDb = curDoc.Database;
        public static Editor edtr = curDoc.Editor;

        // Get the coordinate system for transformations
        public static Matrix3d curUCSMatrix = edtr.CurrentUserCoordinateSystem;
        public static CoordinateSystem3d curUCS = curUCSMatrix.CoordinateSystem3d;

        // Define the appName
        public static string appName = "SPMTool";

        // Color codes
        public enum Colors : short
        {
            Red      = 1,
            Yellow   = 2,
            Yellow1  = 41,
            Cyan     = 4,
            Blue1    = 5,
            Blue     = 150,
            Green    = 92,
            Grey     = 254
        }
    }

    // Constants
    public static class Constants
    {
        public const double
            Pi       = MathNet.Numerics.Constants.Pi,
            PiOver2  = MathNet.Numerics.Constants.PiOver2,
            PiOver4  = MathNet.Numerics.Constants.PiOver4,
            Pi3Over2 = MathNet.Numerics.Constants.Pi3Over2;
    }

    // Layer names
    public static class Layers
    {
        public static string
            extNode            = "ExtNode",
            intNode            = "IntNode",
            stringer           = "Stringer",
            panel              = "Panel",
            support            = "Support",
            force              = "Force",
            forceText          = "ForceText",
            stringerForce      = "StringerForces",
            panelForce         = "PanelShear",
            compressiveStress  = "CompressivePanelStress",
            tensileStress      = "TensilePanelStress",
            displacements      = "Displacements";
    }

    // Block names
    public static class Blocks
    {
	    public static string
		    supportX          = "SupportX",
		    supportY          = "SupportY",
		    supportXY         = "SupportXY",
		    forceBlock        = "ForceBlock",
		    shearBlock        = "ShearBlock",
		    CompressiveStress = "CompressiveStressBlock",
		    TensileStress     = "NonLinearStressBlock";
    }

    // XData indexers
    public static class XData
    {
        // Node indexers
        public enum Node
        {
            AppName  = 0,
            XDataStr = 1,
            Number   = 2,
            Ux       = 3,
            Uy       = 4
        }

        // Stringer indexers
        public enum Stringer
        {
            AppName   = 0,
            XDataStr  = 1,
            Number    = 2,
            Grip1     = 3,
            Grip2     = 4,
            Grip3     = 5,
            Width     = 6,
            Height    = 7,
            NumOfBars = 8,
            BarDiam   = 9,
			Steelfy   = 10,
			SteelEs   = 11
        }

        // Panel indexers
        public enum Panel
        {
            AppName  = 0,
            XDataStr = 1,
            Number   = 2,
            Grip1    = 3,
            Grip2    = 4,
            Grip3    = 5,
            Grip4    = 6,
            Width    = 7,
            XDiam    = 8,
            Sx       = 9,
			fyx      = 10,
			Esx      = 11,
            YDiam    = 12,
            Sy       = 13,
			fyy      = 14,
			Esy      = 15
        }

        // Force indexers
        public enum Force
        {
            AppName    = 0,
            XDataStr   = 1,
            Value      = 2,
            Direction  = 3
        }

        // Support Indexers
        public enum Support
        {
            AppName   = 0,
            XDataStr  = 1,
            Direction = 2,
        }
    }
}
