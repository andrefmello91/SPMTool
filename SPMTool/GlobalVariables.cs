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
    }

    // Constants
    public class Constants
    {
        public static double pi = MathNet.Numerics.Constants.Pi,
                             piOver2 = MathNet.Numerics.Constants.PiOver2,
                             pi3Over2 = MathNet.Numerics.Constants.Pi3Over2;
    }

    // Color codes
    public class Colors
    {
        public static short red = 1,
                            yellow = 2,
                            yellow1 = 41,
                            cyan = 4,
                            blue1 = 5,
                            blue = 150,
                            green = 92,
                            grey = 254;
    }

    // Layer names
    public class Layers
    {
        public static string extNdLyr = "ExtNode",
                             intNdLyr = "IntNode",
                             strLyr = "Stringer",
                             pnlLyr = "Panel",
                             supLyr = "Support",
                             fLyr = "Force",
                             fTxtLyr = "ForceText",
                             strFLyr = "StringerForces",
                             pnlFLyr = "PanelShear",
                             dispLyr = "Displacements";
    }

    // Block names
    public class Blocks
    {
        public static string supportX = "SupportX",
                             supportY = "SupportY",
                             supportXY = "SupportXY",
                             forceBlock = "ForceBlock",
                             shearBlock = "ShearBlock";
    }
}
