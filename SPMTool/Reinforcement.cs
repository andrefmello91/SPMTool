﻿using System;
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
                        PromptDoubleOptions phiOp = new PromptDoubleOptions("\nInput the diameter (in mm) of stringer reinforcement bars:")
                        {
                            DefaultValue = 0,
                            AllowNegative = false
                        };

                        // Get the result
                        PromptDoubleResult phiRes = AutoCAD.edtr.GetDouble(phiOp);

                        if (phiRes.Status == PromptStatus.OK)
                        {
                            double phi = phiRes.Value;

                            foreach (SelectedObject obj in set)
                            {
                                // Open the selected object for read
                                Entity ent = trans.GetObject(obj.ObjectId, OpenMode.ForRead) as Entity;

                                // Check if the selected object is a node
                                if (ent.Layer == Layers.stringer)
                                {
                                    // Upgrade the OpenMode
                                    ent.UpgradeOpen();

                                    // Access the XData as an array
                                    ResultBuffer rb = ent.GetXDataForApplication(AutoCAD.appName);
                                    TypedValue[] data = rb.AsArray();

                                    // Set the new reinforcement
                                    data[(int)XData.Stringer.NumOfBars] = new TypedValue((int)DxfCode.ExtendedDataReal, nBars);
                                    data[(int)XData.Stringer.BarDiam]   = new TypedValue((int)DxfCode.ExtendedDataReal, phi);

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
                    PromptDoubleOptions phiXOp = new PromptDoubleOptions("\nInput the reinforcement bar diameter (in mm) for the X direction for selected panels (only needed for nonlinear analysis):")
                    {
                        DefaultValue = 0,
                        AllowNegative = false
                    };

                    // Get the result
                    PromptDoubleResult phiXRes = AutoCAD.edtr.GetDouble(phiXOp);

                    if (phiXRes.Status == PromptStatus.OK)
                    {
                        double phiX = phiXRes.Value;

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
                            PromptDoubleOptions phiYOp = new PromptDoubleOptions("\nInput the reinforcement bar diameter (in mm) for the Y direction for selected panels (only needed for nonlinear analysis):")
                            {
                                DefaultValue = 0,
                                AllowNegative = false
                            };

                            // Get the result
                            PromptDoubleResult phiYRes = AutoCAD.edtr.GetDouble(phiYOp);

                            if (phiYRes.Status == PromptStatus.OK)
                            {
                                double phiY = phiYRes.Value;

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
                                        if (ent.Layer == Layers.panel)
                                        {
                                            // Upgrade the OpenMode
                                            ent.UpgradeOpen();

                                            // Access the XData as an array
                                            ResultBuffer rb = ent.GetXDataForApplication(AutoCAD.appName);
                                            TypedValue[] data = rb.AsArray();

                                            // Set the new geometry and reinforcement (line 7 to 9 of the array)
                                            data[(int)XData.Panel.XDiam] = new TypedValue((int)DxfCode.ExtendedDataReal, phiX);
                                            data[(int)XData.Panel.Sx]    = new TypedValue((int)DxfCode.ExtendedDataReal, sx);
                                            data[(int)XData.Panel.YDiam] = new TypedValue((int)DxfCode.ExtendedDataReal, phiY);
                                            data[(int)XData.Panel.Sy]    = new TypedValue((int)DxfCode.ExtendedDataReal, sy);

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
        public static double StringerReinforcement(double nBars, double phi)
        {
            // Initialize As
            double As = 0;

            if (nBars > 0 && phi > 0)
                As = nBars * Constants.Pi * phi * phi / 4;

            return As;
        }

        // Calculate the panel reinforcement ratio
        public static (double X, double Y) PanelReinforcement((double X, double Y) barDiameter, (double X, double Y) spacing, double width)
        {
            // Initialize psx and psy
            double
                psx = 0,
                psy = 0;

            if (barDiameter.X > 0 && spacing.X > 0)
                psx = Constants.Pi * barDiameter.X * barDiameter.X / (2 * spacing.X * width);

            if (barDiameter.Y > 0 && spacing.Y > 0)
                psy = Constants.Pi * barDiameter.Y * barDiameter.Y / (2 * spacing.Y * width);

            return (psx, psy);
        }
    }
}
