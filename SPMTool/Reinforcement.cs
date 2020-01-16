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
                                if (ent.Layer == Layers.strLyr)
                                {
                                    // Upgrade the OpenMode
                                    ent.UpgradeOpen();

                                    // Access the XData as an array
                                    ResultBuffer rb = ent.GetXDataForApplication(AutoCAD.appName);
                                    TypedValue[] data = rb.AsArray();

                                    // Set the new reinforcement
                                    data[StringerXDataIndex.nBars] = new TypedValue((int)DxfCode.ExtendedDataReal, nBars);
                                    data[StringerXDataIndex.phi]   = new TypedValue((int)DxfCode.ExtendedDataReal, phi);

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
                                        if (ent.Layer == Layers.pnlLyr)
                                        {
                                            // Upgrade the OpenMode
                                            ent.UpgradeOpen();

                                            // Access the XData as an array
                                            ResultBuffer rb = ent.GetXDataForApplication(AutoCAD.appName);
                                            TypedValue[] data = rb.AsArray();

                                            // Set the new geometry and reinforcement (line 7 to 9 of the array)
                                            data[PanelXDataIndex.phiX] = new TypedValue((int)DxfCode.ExtendedDataReal, phiX);
                                            data[PanelXDataIndex.sx]   = new TypedValue((int)DxfCode.ExtendedDataReal, sx);
                                            data[PanelXDataIndex.phiY] = new TypedValue((int)DxfCode.ExtendedDataReal, phiY);
                                            data[PanelXDataIndex.sy]   = new TypedValue((int)DxfCode.ExtendedDataReal, sy);

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
                As = nBars * Constants.pi * phi * phi / 4;

            return As;
        }

        // Calculate the panel reinforcement ratio
        public static double[] PanelReinforcement(double[] phi, double[] s, double w)
        {
            // Get the values for each direction
            double phiX = phi[0],
                   phiY = phi[1],
                   sx = s[0],
                   sy = s[1];

            // Initialize psx and psy
            double psx = 0,
                   psy = 0;

            if (phiX > 0 && sx > 0)
                psx = Constants.pi * phiX * phiX / (2 * sx * w);

            if (phiY > 0 && sy > 0)
                psy = Constants.pi * phiY * phiY / (2 * sy * w);

            return new double[]{psx, psy};
        }
    }
}
