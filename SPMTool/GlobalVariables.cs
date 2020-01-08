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

    // XData indexers
    // Node indexers
    public class NodeXDataIndex
    {
        public static int appName  = 0,
                          xdataStr = 1,
                          num      = 2,
                          support  = 3,
                          Fx       = 4,
                          Fy       = 5,
                          ux       = 6,
                          uy       = 7;

        // Size of XData
        public static int size = 8;
    }

    // Stringer indexers
    public class StringerXDataIndex
    {
        public static int appName  = 0,
                          xdataStr = 1,
                          num      = 2,
                          grip1    = 3,
                          grip2    = 4,
                          grip3    = 5,
                          w        = 6,
                          h        = 7,
                          nBars    = 8,
                          phi      = 9;

        // Size of XData
        public static int size = 10;
    }

    // Panel indexers
    public class PanelXDataIndex
    {
        public static int appName  = 0,
                          xdataStr = 1,
                          num      = 2,
                          grip1    = 3,
                          grip2    = 4,
                          grip3    = 5,
                          grip4    = 6,
                          w        = 7,
                          phiX     = 8,
                          sx       = 9,
                          phiY     = 10,
                          sy       = 11;

        // Size of XData
        public static int size = 12;
    }
}
