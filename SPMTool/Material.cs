using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Colors;

namespace SPMTool
{
    class Material
    {
        [CommandMethod("SetConcreteParameters")]
        public static void SetConcreteParameters()
        {
            // Simplified typing for editor:
            Editor ed = Application.DocumentManager.MdiActiveDocument.Editor;

            // Get the current document and database
            Document curDoc = Application.DocumentManager.MdiActiveDocument;
            Database curDb = curDoc.Database;

            // Ask the user to input the concrete compressive strength
            PromptIntegerOptions fcOp = new PromptIntegerOptions("Input the concrete compressive strength (fc) in MPa:");
            
            // Restrict input to positive and non-negative values
            fcOp.AllowZero = false;
            fcOp.AllowNegative = false;

            // Get the result
            PromptIntegerResult fcRes = ed.GetInteger(fcOp);
            int fc = fcRes.Value;

            // Ask the user to input the concrete Elastic Module
            PromptIntegerOptions EcOp = new PromptIntegerOptions("Input the concrete Elastic Module (Ec) in MPa:");

            // Restrict input to positive and non-negative values
            EcOp.AllowZero = false;
            EcOp.AllowNegative = false;

            // Get the result
            PromptIntegerResult EcRes = ed.GetInteger(EcOp);
            int Ec = EcRes.Value;

            // Save the variables on the Xrecord
            using (Xrecord concXrec = new Xrecord())
            {
                concXrec.Data = new ResultBuffer(
                                             new TypedValue((int)DxfCode.ExtendedDataInteger32, fc),
                                             new TypedValue((int)DxfCode.ExtendedDataInteger32, Ec));
            }
        }

        [CommandMethod("SetSteelParameters")]
        public static void SetSteelParameters()
        {
            // Simplified typing for editor:
            Editor ed = Application.DocumentManager.MdiActiveDocument.Editor;

            // Get the current document and database
            Document curDoc = Application.DocumentManager.MdiActiveDocument;
            Database curDb = curDoc.Database;

            // Ask the user to input the steel tensile strength
            PromptIntegerOptions fyOp = new PromptIntegerOptions("Input the steel tensile strength (fy) in MPa:");

            // Restrict input to positive and non-negative values
            fyOp.AllowZero = false;
            fyOp.AllowNegative = false;

            // Get the result
            PromptIntegerResult fyRes = ed.GetInteger(fyOp);
            int fy = fyRes.Value;

            // Ask the user to input the steel Elastic Module
            PromptIntegerOptions EsOp = new PromptIntegerOptions("Input the steel Elastic Module (Es) in MPa:");

            // Restrict input to positive and non-negative values
            EsOp.AllowZero = false;
            EsOp.AllowNegative = false;

            // Get the result
            PromptIntegerResult EsRes = ed.GetInteger(EsOp);
            int Es = EsRes.Value;

            // Save the variables on the Xrecord
            using (Xrecord steelXrec = new Xrecord())
            {
                steelXrec.Data = new ResultBuffer(
                                             new TypedValue((int)DxfCode.ExtendedDataInteger32, fy),
                                             new TypedValue((int)DxfCode.ExtendedDataInteger32, Es));
            }
        }
    }
}
