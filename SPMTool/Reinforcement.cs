using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;

[assembly: CommandClass(typeof(SPMTool.Reinforcement))]

namespace SPMTool
{
    class Reinforcement
    {
        [CommandMethod("SetStringerReinforcement")]
        public void SetStringerReinforcement()
        {
            // Start a transaction
            using (Transaction trans = AutoCAD.curDb.TransactionManager.StartTransaction())
            {
                // Request objects to be selected in the drawing area
                AutoCAD.edtr.WriteMessage("\nSelect the stringers to assign reinforcement (you can select other elements, the properties will be only applied to elements with 'Stringer' layer activated).");
                PromptSelectionResult selRes = AutoCAD.edtr.GetSelection();

                // If the prompt status is OK, objects were selected
                if (selRes.Status == PromptStatus.OK)
                {
                    SelectionSet set = selRes.Value;

                    // Ask the user to input the stringer width
                    PromptIntegerOptions nBarsOp = new PromptIntegerOptions("\nInput the number of stringer reinforcement bars (only needed for nonlinear analysis):")
                    {
                        DefaultValue = 0,
                        AllowNegative = false
                    };

                    // Get the result
                    PromptIntegerResult nBarsRes = AutoCAD.edtr.GetInteger(nBarsOp);

                    if (nBarsRes.Status == PromptStatus.OK)
                    {
                        double nBars = nBarsRes.Value;

                        // Ask the user to input the stringer height
                        PromptDoubleOptions dBarOp = new PromptDoubleOptions("\nInput the diameter (in mm) of stringer reinforcement bars:")
                        {
                            DefaultValue = 0,
                            AllowNegative = false
                        };

                        // Get the result
                        PromptDoubleResult dBarRes = AutoCAD.edtr.GetDouble(dBarOp);

                        if (dBarRes.Status == PromptStatus.OK)
                        {
                            double dBar = dBarRes.Value;

                            foreach (SelectedObject obj in set)
                            {
                                // Open the selected object for read
                                Entity ent = trans.GetObject(obj.ObjectId, OpenMode.ForRead) as Entity;

                                // Check if the selected object is a node
                                if (ent.Layer == Layers.strLyr)
                                {
                                    // Upgrade the OpenMode
                                    ent.UpgradeOpen();

                                    // Access the XData as an array
                                    ResultBuffer rb = ent.GetXDataForApplication(AutoCAD.appName);
                                    TypedValue[] data = rb.AsArray();

                                    // Set the new reinforcement
                                    data[StringerXDataIndex.nBars] = new TypedValue((int)DxfCode.ExtendedDataReal, nBars);
                                    data[StringerXDataIndex.dBars] = new TypedValue((int)DxfCode.ExtendedDataReal, dBar);

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
        }

        [CommandMethod("SetPanelReinforcement")]
        public void SetPanelReinforcement()
        {
            // Start a transaction
            using (Transaction trans = AutoCAD.curDb.TransactionManager.StartTransaction())
            {
                // Request objects to be selected in the drawing area
                AutoCAD.edtr.WriteMessage("\nSelect the panels to assign reinforcement (you can select other elements, the properties will be only applied to elements with 'Panel' layer activated).");
                PromptSelectionResult selRes = AutoCAD.edtr.GetSelection();

                // If the prompt status is OK, objects were selected
                if (selRes.Status == PromptStatus.OK)
                {
                    // Get the selection
                    SelectionSet set = selRes.Value;

                    // Ask the user to input the diameter of bars
                    PromptDoubleOptions dBarXOp = new PromptDoubleOptions("\nInput the reinforcement bar diameter (in mm) for the X direction for selected panels (only needed for nonlinear analysis):")
                    {
                        DefaultValue = 0,
                        AllowNegative = false
                    };

                    // Get the result
                    PromptDoubleResult dBarXRes = AutoCAD.edtr.GetDouble(dBarXOp);

                    if (dBarXRes.Status == PromptStatus.OK)
                    {
                        double dBarX = dBarXRes.Value;

                        // Ask the user to input the bar spacing
                        PromptDoubleOptions sxOp = new PromptDoubleOptions("\nInput the bar spacing (in mm) for the X direction:")
                        {
                            DefaultValue = 0,
                            AllowNegative = false
                        };

                        // Get the result
                        PromptDoubleResult sxRes = AutoCAD.edtr.GetDouble(sxOp);

                        if (sxRes.Status == PromptStatus.OK)
                        {
                            double sx = sxRes.Value;

                            // Ask the user to input the diameter of bars
                            PromptDoubleOptions dBarYOp = new PromptDoubleOptions("\nInput the reinforcement bar diameter (in mm) for the Y direction for selected panels (only needed for nonlinear analysis):")
                            {
                                DefaultValue = 0,
                                AllowNegative = false
                            };

                            // Get the result
                            PromptDoubleResult dBarYRes = AutoCAD.edtr.GetDouble(dBarYOp);

                            if (dBarYRes.Status == PromptStatus.OK)
                            {
                                double dBarY = dBarYRes.Value;

                                // Ask the user to input the bar spacing
                                PromptDoubleOptions syOp = new PromptDoubleOptions("\nInput the bar spacing (in mm) for the Y direction:")
                                {
                                    DefaultValue = 0,
                                    AllowNegative = false
                                };

                                // Get the result
                                PromptDoubleResult syRes = AutoCAD.edtr.GetDouble(syOp);

                                if (syRes.Status == PromptStatus.OK)
                                {
                                    double sy = syRes.Value;

                                    foreach (SelectedObject obj in set)
                                    {
                                        // Open the selected object for read
                                        Entity ent = trans.GetObject(obj.ObjectId, OpenMode.ForRead) as Entity;

                                        // Check if the selected object is a node
                                        if (ent.Layer == Layers.pnlLyr)
                                        {
                                            // Upgrade the OpenMode
                                            ent.UpgradeOpen();

                                            // Access the XData as an array
                                            ResultBuffer rb = ent.GetXDataForApplication(AutoCAD.appName);
                                            TypedValue[] data = rb.AsArray();

                                            // Set the new geometry and reinforcement (line 7 to 9 of the array)
                                            data[PanelXDataIndex.dBarsX] = new TypedValue((int)DxfCode.ExtendedDataReal, dBarX);
                                            data[PanelXDataIndex.sx]     = new TypedValue((int)DxfCode.ExtendedDataReal, sx);
                                            data[PanelXDataIndex.dBarsY] = new TypedValue((int)DxfCode.ExtendedDataReal, dBarY);
                                            data[PanelXDataIndex.sy]     = new TypedValue((int)DxfCode.ExtendedDataReal, sy);

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
                }
            }
        }

        // Calculate the stringer reinforcement area
        public static double StringerReinforcement(double nBars, double dBars)
        {
            double As = nBars * Constants.pi * dBars * dBars / 4;
            return As;
        }
    }
}
