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
    public class Constants
    {
        public static double
            pi       = MathNet.Numerics.Constants.Pi,
            piOver2  = MathNet.Numerics.Constants.PiOver2,
            pi3Over2 = MathNet.Numerics.Constants.Pi3Over2;
    }

    // Layer names
    public class Layers
    {
        public static string
            extNode        = "ExtNode",
            intNode        = "IntNode",
            stringer       = "Stringer",
            panel          = "Panel",
            support        = "Support",
            force          = "Force",
            forceText      = "ForceText",
            stringerForce  = "StringerForces",
            panelForce     = "PanelShear",
            displacements  = "Displacements";
    }

    // Block names
    public class Blocks
    {
        public static string
            supportX = "SupportX",
            supportY = "SupportY",
            supportXY = "SupportXY",
            forceBlock = "ForceBlock",
            shearBlock = "ShearBlock";
    }

    // XData indexers
    public class XData
    {
        // Node indexers
        public enum Node
        {
            AppName  = 0,
            XDataStr = 1,
            Number   = 2,
            Support  = 3,
            Fx       = 4,
            Fy       = 5,
            Ux       = 6,
            Uy       = 7
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
            BarDiam   = 9
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
            YDiam    = 10,
            Sy       = 11
        }
    }
}
