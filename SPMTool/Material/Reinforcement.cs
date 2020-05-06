using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using MathNet.Numerics.LinearAlgebra;
using SPMTool.ACAD;

[assembly: CommandClass(typeof(SPMTool.Material.Reinforcement))]

namespace SPMTool
{
	namespace Material
	{
		public class Reinforcement
		{
			public class Stringer : Reinforcement
			{
				// Properties
				public double         NumberOfBars  { get; }
				public double         BarDiameter   { get; }
				public Material.Steel Steel         { get; }

				// Constructor
				public Stringer(double numberOfBars, double barDiameter, Material.Steel steel)
				{
					NumberOfBars = numberOfBars;
					BarDiameter  = barDiameter;
					Steel        = steel;
				}

				// Calculated reinforcement area
				public double Area
				{
					get
					{
						// Initialize As
						double As = 0;

						if (NumberOfBars > 0 && BarDiameter > 0)
							As = 0.25 * NumberOfBars * Constants.Pi * BarDiameter * BarDiameter;

						return As;
					}
				}

				// Set steel strains
				public void SetStrains(Vector<double> strains)
				{
					Steel.SetStrain(strains[0]);
					Steel.SetStrain(strains[1]);
				}

				// Set steel stresses
				public void SetStresses(Vector<double> strains)
				{
					Steel.SetStress(strains[0]);
					Steel.SetStress(strains[1]);
				}

				// Set steel strain and stresses
				public void SetStrainsAndStresses(Vector<double> strains)
				{
					SetStrains(strains);
					SetStresses(strains);
				}
			}

            public class Panel : Reinforcement
			{
				// Properties
				public (double X, double Y)                 BarDiameter { get; }
				public (double X, double Y)                 BarSpacing  { get; }
				public (Material.Steel X, Material.Steel Y) Steel       { get; }
				private double                              PanelWidth  { get; }

				// Constructor
				public Panel((double X, double Y) barDiameter, (double X, double Y) barSpacing,
					(Material.Steel X, Material.Steel Y) steel, double panelWidth)
				{
					BarDiameter = barDiameter;
					BarSpacing  = barSpacing;
					Steel       = steel;
					PanelWidth  = panelWidth;
				}

				// Calculate the panel reinforcement ratio
				public (double X, double Y) Ratio
				{
					get
					{
						// Initialize psx and psy
						double
							psx = 0,
							psy = 0;

						if (BarDiameter.X > 0 && BarSpacing.X > 0)
							psx = 0.5 * Constants.Pi * BarDiameter.X * BarDiameter.X / (BarSpacing.X * PanelWidth);

						if (BarDiameter.Y > 0 && BarSpacing.Y > 0)
							psy = 0.5 * Constants.Pi * BarDiameter.Y * BarDiameter.Y / (BarSpacing.Y * PanelWidth);

						return
							(psx, psy);
					}
				}

				// Get reinforcement stresses
				public (double fsx, double fsy) Stresses => (Steel.X.Stress, Steel.Y.Stress);

				// Get reinforcement secant module
				public (double Esx, double Esy) SecantModule => (Steel.X.SecantModule, Steel.Y.SecantModule);

				// Set steel strains
				public void SetStrains(Vector<double> strains)
				{
					Steel.X.SetStrain(strains[0]);
					Steel.Y.SetStrain(strains[1]);
				}

				// Set steel stresses
				public void SetStresses(Vector<double> strains)
				{
					Steel.X.SetStress(strains[0]);
					Steel.Y.SetStress(strains[1]);
				}

				// Set steel strain and stresses
				public void SetStrainsAndStresses(Vector<double> strains)
				{
					SetStrains(strains);
					SetStresses(strains);
				}
			}

            [CommandMethod("SetStringerReinforcement")]
            public static void SetStringerReinforcement()
            {
                // Start a transaction
                using (Transaction trans = ACAD.Current.db.TransactionManager.StartTransaction())
                {
                    // Request objects to be selected in the drawing area
                    ACAD.Current.edtr.WriteMessage(
                        "\nSelect the stringers to assign reinforcement (you can select other elements, the properties will be only applied to elements with 'Stringer' layer activated).");
                    PromptSelectionResult selRes = ACAD.Current.edtr.GetSelection();

                    // If the prompt status is OK, objects were selected
                    if (selRes.Status == PromptStatus.Cancel)
                        return;

                    SelectionSet set = selRes.Value;

                    // Ask the user to input the stringer width
                    PromptIntegerOptions nBarsOp =
                        new PromptIntegerOptions(
                            "\nInput the number of stringer reinforcement bars (only needed for nonlinear analysis):")
                        {
                            DefaultValue = 0,
                            AllowNegative = false
                        };

                    // Get the result
                    PromptIntegerResult nBarsRes = ACAD.Current.edtr.GetInteger(nBarsOp);

                    if (nBarsRes.Status == PromptStatus.Cancel)
                        return;

                    double nBars = nBarsRes.Value;

                    // Ask the user to input the stringer height
                    PromptDoubleOptions phiOp =
                        new PromptDoubleOptions("\nInput the diameter (in mm) of stringer reinforcement bars:")
                        {
                            DefaultValue = 0,
                            AllowNegative = false
                        };

                    // Get the result
                    PromptDoubleResult phiRes = ACAD.Current.edtr.GetDouble(phiOp);

                    if (phiRes.Status == PromptStatus.Cancel)
                        return;

                    double phi = phiRes.Value;

                    // Ask the user to input the Steel yield strength
                    PromptDoubleOptions fyOp =
                        new PromptDoubleOptions("\nInput the yield strength (MPa) of stringer reinforcement bars:")
                        {
                            DefaultValue = 500,
                            AllowNegative = false
                        };

                    // Get the result
                    PromptDoubleResult fyRes = ACAD.Current.edtr.GetDouble(fyOp);

                    if (fyRes.Status == PromptStatus.Cancel)
                        return;

                    double fy = fyRes.Value;

                    // Ask the user to input the Steel elastic modulus
                    PromptDoubleOptions EsOp =
                        new PromptDoubleOptions("\nInput the elastic modulus (MPa) of stringer reinforcement bars:")
                        {
                            DefaultValue = 210000,
                            AllowNegative = false
                        };

                    // Get the result
                    PromptDoubleResult EsRes = ACAD.Current.edtr.GetDouble(EsOp);

                    if (EsRes.Status == PromptStatus.Cancel)
                        return;

                    double Es = EsRes.Value;

                    // Save the properties
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
                            ResultBuffer rb = ent.GetXDataForApplication(ACAD.Current.appName);
                            TypedValue[] data = rb.AsArray();

                            // Set the new reinforcement
                            data[(int)XData.Stringer.NumOfBars] =
                                new TypedValue((int)DxfCode.ExtendedDataReal, nBars);
                            data[(int)XData.Stringer.BarDiam] = new TypedValue((int)DxfCode.ExtendedDataReal, phi);
                            data[(int)XData.Stringer.Steelfy] = new TypedValue((int)DxfCode.ExtendedDataReal, fy);
                            data[(int)XData.Stringer.SteelEs] = new TypedValue((int)DxfCode.ExtendedDataReal, Es);

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
                using (Transaction trans = ACAD.Current.db.TransactionManager.StartTransaction())
                {
                    // Request objects to be selected in the drawing area
                    ACAD.Current.edtr.WriteMessage(
                        "\nSelect the panels to assign reinforcement (you can select other elements, the properties will be only applied to elements with 'Panel' layer activated).");
                    PromptSelectionResult selRes = ACAD.Current.edtr.GetSelection();

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
                    PromptDoubleResult phiXRes = ACAD.Current.edtr.GetDouble(phiXOp);

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
                    PromptDoubleResult sxRes = ACAD.Current.edtr.GetDouble(sxOp);

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
                    PromptDoubleResult fyxRes = ACAD.Current.edtr.GetDouble(fyxOp);

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
                    PromptDoubleResult EsxRes = ACAD.Current.edtr.GetDouble(EsxOp);

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
                    PromptDoubleResult phiYRes = ACAD.Current.edtr.GetDouble(phiYOp);

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
                    PromptDoubleResult syRes = ACAD.Current.edtr.GetDouble(syOp);

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
                    PromptDoubleResult fyyRes = ACAD.Current.edtr.GetDouble(fyyOp);

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
                    PromptDoubleResult EsyRes = ACAD.Current.edtr.GetDouble(EsyOp);

                    if (EsyRes.Status == PromptStatus.Cancel)
                        return;

                    double Esy = EsyRes.Value;

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
                            ResultBuffer rb = ent.GetXDataForApplication(ACAD.Current.appName);
                            TypedValue[] data = rb.AsArray();

                            // Set the new geometry and reinforcement (line 7 to 9 of the array)
                            data[(int)XData.Panel.XDiam] = new TypedValue((int)DxfCode.ExtendedDataReal, phiX);
                            data[(int)XData.Panel.Sx] = new TypedValue((int)DxfCode.ExtendedDataReal, sx);
                            data[(int)XData.Panel.fyx] = new TypedValue((int)DxfCode.ExtendedDataReal, fyx);
                            data[(int)XData.Panel.Esx] = new TypedValue((int)DxfCode.ExtendedDataReal, Esx);
                            data[(int)XData.Panel.YDiam] = new TypedValue((int)DxfCode.ExtendedDataReal, phiY);
                            data[(int)XData.Panel.Sy] = new TypedValue((int)DxfCode.ExtendedDataReal, sy);
                            data[(int)XData.Panel.fyy] = new TypedValue((int)DxfCode.ExtendedDataReal, fyy);
                            data[(int)XData.Panel.Esy] = new TypedValue((int)DxfCode.ExtendedDataReal, Esy);

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
