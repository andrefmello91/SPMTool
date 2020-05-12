using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using MathNet.Numerics.LinearAlgebra;
using SPMTool.AutoCAD;

[assembly: CommandClass(typeof(SPMTool.AutoCAD.Reinforcement))]

namespace SPMTool.AutoCAD
{
    public class Reinforcement
    {
        [CommandMethod("SetStringerReinforcement")]
        public static void SetStringerReinforcement()
        {
            // Start a transaction
            using (Transaction trans = AutoCAD.Current.db.TransactionManager.StartTransaction())
            {
                // Request objects to be selected in the drawing area
                AutoCAD.Current.edtr.WriteMessage(
                    "\nSelect the stringers to assign reinforcement (you can select other elements, the properties will be only applied to elements with 'Stringer' layer activated).");
                PromptSelectionResult selRes = AutoCAD.Current.edtr.GetSelection();

                // If the prompt status is OK, objects were selected
                if (selRes.Status == PromptStatus.Cancel)
                    return;

                SelectionSet set = selRes.Value;

                // Ask the user to input the Stringer width
                PromptIntegerOptions nBarsOp =
                    new PromptIntegerOptions(
                        "\nInput the number of Stringer reinforcement bars (only needed for nonlinear analysis):")
                    {
                        DefaultValue = 0,
                        AllowNegative = false
                    };

                // Get the result
                PromptIntegerResult nBarsRes = AutoCAD.Current.edtr.GetInteger(nBarsOp);

                if (nBarsRes.Status == PromptStatus.Cancel)
                    return;

                double nBars = nBarsRes.Value;

                // Ask the user to input the Stringer height
                PromptDoubleOptions phiOp =
                    new PromptDoubleOptions("\nInput the diameter (in mm) of Stringer reinforcement bars:")
                    {
                        DefaultValue = 0,
                        AllowNegative = false
                    };

                // Get the result
                PromptDoubleResult phiRes = AutoCAD.Current.edtr.GetDouble(phiOp);

                if (phiRes.Status == PromptStatus.Cancel)
                    return;

                double phi = phiRes.Value;

                // Ask the user to input the Steel yield strength
                PromptDoubleOptions fyOp =
                    new PromptDoubleOptions("\nInput the yield strength (MPa) of Stringer reinforcement bars:")
                    {
                        DefaultValue = 500,
                        AllowNegative = false
                    };

                // Get the result
                PromptDoubleResult fyRes = AutoCAD.Current.edtr.GetDouble(fyOp);

                if (fyRes.Status == PromptStatus.Cancel)
                    return;

                double fy = fyRes.Value;

                // Ask the user to input the Steel elastic modulus
                PromptDoubleOptions EsOp =
                    new PromptDoubleOptions("\nInput the elastic modulus (MPa) of Stringer reinforcement bars:")
                    {
                        DefaultValue = 210000,
                        AllowNegative = false
                    };

                // Get the result
                PromptDoubleResult EsRes = AutoCAD.Current.edtr.GetDouble(EsOp);

                if (EsRes.Status == PromptStatus.Cancel)
                    return;

                double Es = EsRes.Value;

                // Save the properties
                foreach (SelectedObject obj in set)
                {
                    // Open the selected object for read
                    Entity ent = trans.GetObject(obj.ObjectId, OpenMode.ForRead) as Entity;

                    // Check if the selected object is a node
                    if (ent.Layer == Geometry.Stringer.LayerName)
                    {
                        // Upgrade the OpenMode
                        ent.UpgradeOpen();

                        // Access the XData as an array
                        ResultBuffer rb = ent.GetXDataForApplication(AutoCAD.Current.appName);
                        TypedValue[] data = rb.AsArray();

                        // Set the new reinforcement
                        data[(int) XData.Stringer.NumOfBars] = new TypedValue((int) DxfCode.ExtendedDataReal, nBars);
                        data[(int) XData.Stringer.BarDiam] = new TypedValue((int) DxfCode.ExtendedDataReal, phi);
                        data[(int) XData.Stringer.Steelfy] = new TypedValue((int) DxfCode.ExtendedDataReal, fy);
                        data[(int) XData.Stringer.SteelEs] = new TypedValue((int) DxfCode.ExtendedDataReal, Es);

                        // Add the new XData
                        ent.XData = new ResultBuffer(data);
                    }
                }

                // Save the new object to the database
                trans.Commit();
            }
        }

        [CommandMethod("SetPanelReinforcement")]
        public static void SetPanelReinforcement()
        {
            // Start a transaction
            using (Transaction trans = AutoCAD.Current.db.TransactionManager.StartTransaction())
            {
                // Request objects to be selected in the drawing area
                AutoCAD.Current.edtr.WriteMessage(
                    "\nSelect the panels to assign reinforcement (you can select other elements, the properties will be only applied to elements with 'Panel' layer activated).");
                PromptSelectionResult selRes = AutoCAD.Current.edtr.GetSelection();

                // If the prompt status is OK, objects were selected
                if (selRes.Status == PromptStatus.Cancel)
                    return;

                // Get the selection
                SelectionSet set = selRes.Value;

                // Ask the user to input the diameter of bars
                PromptDoubleOptions phiXOp =
                    new PromptDoubleOptions(
                        "\nInput the reinforcement bar diameter (in mm) for the X direction for selected panels (only needed for nonlinear analysis):")
                    {
                        DefaultValue = 0,
                        AllowNegative = false
                    };

                // Get the result
                PromptDoubleResult phiXRes = AutoCAD.Current.edtr.GetDouble(phiXOp);

                if (phiXRes.Status == PromptStatus.Cancel)
                    return;

                double phiX = phiXRes.Value;

                // Ask the user to input the bar spacing
                PromptDoubleOptions sxOp =
                    new PromptDoubleOptions("\nInput the bar spacing (in mm) for the X direction:")
                    {
                        DefaultValue = 0,
                        AllowNegative = false
                    };

                // Get the result
                PromptDoubleResult sxRes = AutoCAD.Current.edtr.GetDouble(sxOp);

                if (sxRes.Status == PromptStatus.Cancel)
                    return;

                double sx = sxRes.Value;

                // Ask the user to input the Steel yield strength
                PromptDoubleOptions fyxOp =
                    new PromptDoubleOptions(
                        "\nInput the yield strength (MPa) of panel reinforcement bars in X direction:")
                    {
                        DefaultValue = 500,
                        AllowNegative = false
                    };

                // Get the result
                PromptDoubleResult fyxRes = AutoCAD.Current.edtr.GetDouble(fyxOp);

                if (fyxRes.Status == PromptStatus.Cancel)
                    return;

                double fyx = fyxRes.Value;

                // Ask the user to input the Steel elastic modulus
                PromptDoubleOptions EsxOp =
                    new PromptDoubleOptions(
                        "\nInput the elastic modulus (MPa) of panel reinforcement bars in X direction:")
                    {
                        DefaultValue = 210000,
                        AllowNegative = false
                    };

                // Get the result
                PromptDoubleResult EsxRes = AutoCAD.Current.edtr.GetDouble(EsxOp);

                if (EsxRes.Status == PromptStatus.Cancel)
                    return;

                double Esx = EsxRes.Value;


                // Ask the user to input the diameter of bars
                PromptDoubleOptions phiYOp =
                    new PromptDoubleOptions(
                        "\nInput the reinforcement bar diameter (in mm) for the Y direction for selected panels (only needed for nonlinear analysis):")
                    {
                        DefaultValue = phiX,
                        AllowNegative = false
                    };

                // Get the result
                PromptDoubleResult phiYRes = AutoCAD.Current.edtr.GetDouble(phiYOp);

                if (phiYRes.Status == PromptStatus.Cancel)
                    return;

                double phiY = phiYRes.Value;

                // Ask the user to input the bar spacing
                PromptDoubleOptions syOp =
                    new PromptDoubleOptions("\nInput the bar spacing (in mm) for the Y direction:")
                    {
                        DefaultValue = sx,
                        AllowNegative = false
                    };

                // Get the result
                PromptDoubleResult syRes = AutoCAD.Current.edtr.GetDouble(syOp);

                if (syRes.Status == PromptStatus.Cancel)
                    return;

                double sy = syRes.Value;

                // Ask the user to input the Steel yield strength
                PromptDoubleOptions fyyOp =
                    new PromptDoubleOptions(
                        "\nInput the yield strength (MPa) of panel reinforcement bars in Y direction:")
                    {
                        DefaultValue = fyx,
                        AllowNegative = false
                    };

                // Get the result
                PromptDoubleResult fyyRes = AutoCAD.Current.edtr.GetDouble(fyyOp);

                if (fyyRes.Status == PromptStatus.Cancel)
                    return;

                double fyy = fyyRes.Value;

                // Ask the user to input the Steel elastic modulus
                PromptDoubleOptions EsyOp =
                    new PromptDoubleOptions(
                        "\nInput the elastic modulus (MPa) of panel reinforcement bars in X direction:")
                    {
                        DefaultValue = Esx,
                        AllowNegative = false
                    };

                // Get the result
                PromptDoubleResult EsyRes = AutoCAD.Current.edtr.GetDouble(EsyOp);

                if (EsyRes.Status == PromptStatus.Cancel)
                    return;

                double Esy = EsyRes.Value;

                foreach (SelectedObject obj in set)
                {
                    // Open the selected object for read
                    Entity ent = trans.GetObject(obj.ObjectId, OpenMode.ForRead) as Entity;

                    // Check if the selected object is a node
                    if (ent.Layer == Geometry.Panel.LayerName)
                    {
                        // Upgrade the OpenMode
                        ent.UpgradeOpen();

                        // Access the XData as an array
                        ResultBuffer rb = ent.GetXDataForApplication(AutoCAD.Current.appName);
                        TypedValue[] data = rb.AsArray();

                        // Set the new geometry and reinforcement (line 7 to 9 of the array)
                        data[(int) XData.Panel.XDiam] = new TypedValue((int) DxfCode.ExtendedDataReal, phiX);
                        data[(int) XData.Panel.Sx]    = new TypedValue((int) DxfCode.ExtendedDataReal, sx);
                        data[(int) XData.Panel.fyx]   = new TypedValue((int) DxfCode.ExtendedDataReal, fyx);
                        data[(int) XData.Panel.Esx]   = new TypedValue((int) DxfCode.ExtendedDataReal, Esx);
                        data[(int) XData.Panel.YDiam] = new TypedValue((int) DxfCode.ExtendedDataReal, phiY);
                        data[(int) XData.Panel.Sy]    = new TypedValue((int) DxfCode.ExtendedDataReal, sy);
                        data[(int) XData.Panel.fyy]   = new TypedValue((int) DxfCode.ExtendedDataReal, fyy);
                        data[(int) XData.Panel.Esy]   = new TypedValue((int) DxfCode.ExtendedDataReal, Esy);

                        // Add the new XData
                        ent.XData = new ResultBuffer(data);
                    }
                }

                // Save the new object to the database
                trans.Commit();
            }
        }
    }
}