using System;
using System.Windows.Media.Imaging;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.Windows;
using Autodesk.AutoCAD.Runtime;


namespace SPMTool
{
    public class Initializer : IExtensionApplication
    {
        public void Initialize()
        {
            Autodesk.AutoCAD.ApplicationServices.Application.Idle +=
            new EventHandler(on_ApplicationIdle);

            AddAppEvent();
        }

        public void on_ApplicationIdle(object sender, EventArgs e)
        {
            UserInterface.RibbonButtons();
            Autodesk.AutoCAD.ApplicationServices.Application.Idle -= on_ApplicationIdle;
        }

        public void Terminate()
        {
            RemoveAppEvent();
        }

        // Event handler for changing colortheme
        public void AddAppEvent()
        {
            Application.SystemVariableChanged +=
                new Autodesk.AutoCAD.ApplicationServices.
                    SystemVariableChangedEventHandler(appSysVarChanged);
        }

        public void RemoveAppEvent()
        {
            Application.SystemVariableChanged -=
                new Autodesk.AutoCAD.ApplicationServices.
                    SystemVariableChangedEventHandler(appSysVarChanged);
        }

        public void appSysVarChanged(object senderObj,
                                     Autodesk.AutoCAD.ApplicationServices.
                                     SystemVariableChangedEventArgs sysVarChEvtArgs)
        {
            //object oVal = Application.GetSystemVariable(sysVarChEvtArgs.Name);

            // Check if it's a theme change
            if (sysVarChEvtArgs.Name == "COLORTHEME")
            {
                // Reinitialize the ribbon buttons
                UserInterface.RibbonButtons();
            }
        }
    }

    public class UserInterface
    {
        public static BitmapImage getBitmap(Bitmap image)
        {
            MemoryStream stream = new MemoryStream();
            image.Save(stream, ImageFormat.Png);
            BitmapImage bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.StreamSource = stream;
            bmp.EndInit();
            return bmp;
        }

        public static void RibbonButtons()
        {
            RibbonControl ribbonControl = ComponentManager.Ribbon;

            // Check if the tab already exists
            RibbonTab tab = ribbonControl.FindTab(AutoCAD.appName);

            if (tab != null)
            {
                // Remove it
                ribbonControl.Tabs.Remove(tab);
            }

            // Check the current theme
            short theme = (short)Application.GetSystemVariable("COLORTHEME");

            // Initialize the icons
            Bitmap strBmp, pnlBmp, setBmp,
                   dvStrBmp, dvPnlBmp, updtBmp,
                   strRefBmp, pnlRefBmp,
                   cncrtBmp,
                   suprtBmp, fcBmp,
                   linBMP, nlinBMP,
                   viewNdBmp, viewStrBmp, viewPnlBmp, viewDtBmp,
                   strFBMP, pnlFBMP, dispBMP;

            // If the theme is dark (0), get the light icons
            if (theme == 0)
            {
                strBmp = Properties.Resources.stringer_large_light;
                pnlBmp = Properties.Resources.panel_large_light;
                setBmp = Properties.Resources.set_small_light;
                dvStrBmp = Properties.Resources.divstr_small_light;
                dvPnlBmp = Properties.Resources.divpnl_small_light;
                updtBmp = Properties.Resources.update_small_light;
                viewDtBmp = Properties.Resources.elementdata_large_light;
                strRefBmp = Properties.Resources.stringerreinforcement_large_light;
                pnlRefBmp = Properties.Resources.panelreinforcement_large_light;
                cncrtBmp = Properties.Resources.concrete_large_light;
                suprtBmp = Properties.Resources.support_large_light;
                fcBmp = Properties.Resources.force_large_light;
                linBMP = Properties.Resources.linear_large_light;
                nlinBMP = Properties.Resources.nonlinear_large_light;
                viewNdBmp = Properties.Resources.viewnode_large_light;
                viewStrBmp = Properties.Resources.viewstringer_large_light;
                viewPnlBmp = Properties.Resources.viewpanel_large_light;
                strFBMP = Properties.Resources.stringerforces_large_light;
                pnlFBMP = Properties.Resources.panelforces_large_light;
                dispBMP = Properties.Resources.displacements_large_light;
            }
            else // If the theme is light
            {
                strBmp = Properties.Resources.stringer_large;
                pnlBmp = Properties.Resources.panel_large;
                setBmp = Properties.Resources.set_small;
                dvStrBmp = Properties.Resources.divstr_small;
                dvPnlBmp = Properties.Resources.divpnl_small;
                updtBmp = Properties.Resources.update_small;
                viewDtBmp = Properties.Resources.elementdata_large;
                strRefBmp = Properties.Resources.stringerreinforcement_large;
                pnlRefBmp = Properties.Resources.panelreinforcement_large;
                cncrtBmp = Properties.Resources.concrete_large;
                suprtBmp = Properties.Resources.support_large;
                fcBmp = Properties.Resources.force_large;
                linBMP = Properties.Resources.linear_large;
                nlinBMP = Properties.Resources.nonlinear_large;
                viewNdBmp = Properties.Resources.viewnode_large;
                viewStrBmp = Properties.Resources.viewstringer_large;
                viewPnlBmp = Properties.Resources.viewpanel_large;
                strFBMP = Properties.Resources.stringerforces_large;
                pnlFBMP = Properties.Resources.panelforces_large;
                dispBMP = Properties.Resources.displacements_large;
            }

            // Create the Ribbon Tab
            RibbonTab Tab = new RibbonTab()
            {
                Title = AutoCAD.appName,
                Id = AutoCAD.appName
            };
            ribbonControl.Tabs.Add(Tab);

            // Create the Ribbon panels
            GeometryPanel(Tab, strBmp, pnlBmp, setBmp, dvStrBmp, dvPnlBmp, updtBmp, viewDtBmp);
            MaterialPanel(Tab, cncrtBmp, viewDtBmp);
            ReinforcementPanel(Tab, strRefBmp, pnlRefBmp);
            ConditionsPanel(Tab, suprtBmp, fcBmp);
            AnalysisPanel(Tab, linBMP, nlinBMP);
            ViewPanel(Tab, viewNdBmp, viewStrBmp, viewPnlBmp, viewDtBmp);
            ResultsPanel(Tab, strFBMP, pnlFBMP, dispBMP);

            // Activate tab
            Tab.IsActive = true;
        }

        // Create Geometry Panel
        public static void GeometryPanel(RibbonTab Tab, Bitmap stringer, Bitmap panel, Bitmap set, Bitmap divideStringer, Bitmap dividePanel, Bitmap update, Bitmap view)
        {
            RibbonPanelSource pnlSrc = new RibbonPanelSource();
            pnlSrc.Title = "Geometry";
            RibbonPanel Panel = new RibbonPanel();
            Panel.Source = pnlSrc;
            Tab.Panels.Add(Panel);

            RibbonButton button2 = new RibbonButton()
            {
                Text = "Add stringer",
                ToolTip = "Create a stringer conecting two nodes",
                ShowText = true,
                ShowImage = true,
                LargeImage = getBitmap(stringer),
                CommandHandler = new CmdHandler(),
                CommandParameter = "AddStringer"
            };

            RibbonButton button3 = new RibbonButton()
            {
                Text = "Add panel",
                ToolTip = "Create a panel connecting four nodes",
                ShowText = true,
                ShowImage = true,
                LargeImage = getBitmap(panel),
                CommandHandler = new CmdHandler(),
                CommandParameter = "AddPanel"
            };

            // Create a split button for geometry creation
            RibbonSplitButton rbSpBtn1 = new RibbonSplitButton()
            {
                ShowText = true,
                IsSplit = true,
                Size = RibbonItemSize.Large,
                IsSynchronizedWithCurrentItem = true
            };
            rbSpBtn1.Items.Add(button2);
            rbSpBtn1.Items.Add(button3);

            // Add to the panel source
            pnlSrc.Items.Add(rbSpBtn1);

            // Create a secondary panel
            RibbonRowPanel subPnl = new RibbonRowPanel();

            // Element parameters buttons
            RibbonButton button4 = new RibbonButton()
            {
                Text = "Stringer geometry",
                ToolTip = "Set the geometry to a selection of stringers",
                ShowText = true,
                ShowImage = true,
                Image = getBitmap(set),
                CommandHandler = new CmdHandler(),
                CommandParameter = "SetStringerGeometry"
            };

            RibbonButton button5 = new RibbonButton()
            {
                Text = "Panel geometry",
                ToolTip = "Set the geometry to a selection of panels",
                ShowText = true,
                ShowImage = true,
                Image = getBitmap(set),
                CommandHandler = new CmdHandler(),
                CommandParameter = "SetPanelGeometry"
            };

            // Create a split button for Element parameters
            RibbonSplitButton rbSpBtn2 = new RibbonSplitButton()
            {
                ShowText = true,
                IsSplit = true,
                IsSynchronizedWithCurrentItem = true
            };
            rbSpBtn2.Items.Add(button4);
            rbSpBtn2.Items.Add(button5);

            // Add to the sub panel and create a new ribbon row
            subPnl.Items.Add(rbSpBtn2);
            subPnl.Items.Add(new RibbonRowBreak());

            // Element division buttons

            RibbonButton button6 = new RibbonButton()
            {
                Text = "Divide stringer",
                ToolTip = "Divide a stringer into smaller ones",
                ShowText = true,
                ShowImage = true,
                Image = getBitmap(divideStringer),
                CommandHandler = new CmdHandler(),
                CommandParameter = "DivideStringer"
            };

            RibbonButton button7 = new RibbonButton()
            {
                Text = "Divide panel",
                ToolTip = "Divide a panel and surrounding stringers",
                ShowText = true,
                ShowImage = true,
                Image = getBitmap(dividePanel),
                CommandHandler = new CmdHandler(),
                CommandParameter = "DividePanel"
            };

            // Create a split button for Element division
            RibbonSplitButton rbSpBtn3 = new RibbonSplitButton()
            {
                ShowText = true,
                IsSplit = true,
                IsSynchronizedWithCurrentItem = true
            };
            rbSpBtn3.Items.Add(button7);
            rbSpBtn3.Items.Add(button6);

            // Add to the sub panel and create a new ribbon row
            subPnl.Items.Add(rbSpBtn3);
            subPnl.Items.Add(new RibbonRowBreak());

            // Update elements button
            RibbonButton button8 = new RibbonButton()
            {
                Text = "Update elements",
                ToolTip = "Update the number of nodes, stringers and panels in the whole model",
                ShowText = true,
                ShowImage = true,
                Image = getBitmap(update),
                CommandHandler = new CmdHandler(),
                CommandParameter = "UpdateElements"
            };
            subPnl.Items.Add(button8);
            
            // Add the sub panel to the panel source
            pnlSrc.Items.Add(subPnl);
        }

        // Create Reinforcement Panel
        public static void ReinforcementPanel(RibbonTab Tab, Bitmap stringerRef, Bitmap panelRef)
        {
            RibbonPanelSource pnlSrc = new RibbonPanelSource();
            pnlSrc.Title = "Reinforcement";
            RibbonPanel Panel = new RibbonPanel();
            Panel.Source = pnlSrc;
            Tab.Panels.Add(Panel);

            RibbonButton button1 = new RibbonButton()
            {
                Text = "Stringer",
                ToolTip = "Set reinforcement to a selection of stringers (only needed in nonlinear analysis)",
                ShowText = true,
                ShowImage = true,
                LargeImage = getBitmap(stringerRef),
                CommandHandler = new CmdHandler(),
                CommandParameter = "SetStringerReinforcement"
            };

            RibbonButton button2 = new RibbonButton()
            {
                Text = "Panel",
                ToolTip = "Set reinforcement to a selection of panels (only needed in nonlinear analysis)",
                ShowText = true,
                ShowImage = true,
                LargeImage = getBitmap(panelRef),
                CommandHandler = new CmdHandler(),
                CommandParameter = "SetPanelReinforcement"
            };

            // Create a split button for conditions
            RibbonSplitButton rbSpBtn1 = new RibbonSplitButton()
            {
                ShowText = true,
                IsSplit = true,
                Size = RibbonItemSize.Large,
                IsSynchronizedWithCurrentItem = true
            };
            rbSpBtn1.Items.Add(button1);
            rbSpBtn1.Items.Add(button2);

            // Add to the panel source
            pnlSrc.Items.Add(rbSpBtn1);
        }

        // Create Material Panel
        public static void MaterialPanel(RibbonTab Tab, Bitmap concrete, Bitmap view)
        {
            RibbonPanelSource pnlSrc = new RibbonPanelSource();
            pnlSrc.Title = "Material";
            RibbonPanel Panel = new RibbonPanel();
            Panel.Source = pnlSrc;
            Tab.Panels.Add(Panel);
            
            // Material parameters buttons
            RibbonButton button1 = new RibbonButton()
            {
                Text = "Concrete",
                ToolTip = "Set concrete compressive strength and elastic module",
                ShowText = true,
                ShowImage = true,
                LargeImage = getBitmap(concrete),
                CommandHandler = new CmdHandler(),
                CommandParameter = "SetConcreteParameters"
            };

            RibbonButton button2 = new RibbonButton()
            {
                Text = "View concrete parameters",
                ToolTip = "View concrete parameters",
                ShowText = true,
                ShowImage = true,
                LargeImage = getBitmap(view),
                CommandHandler = new CmdHandler(),
                CommandParameter = "ViewConcreteParameters"
            };

            // Create a split button for materials
            RibbonSplitButton rbSpBtn1 = new RibbonSplitButton()
            {
	            ShowText = true,
	            IsSplit = true,
	            Size = RibbonItemSize.Large,
	            IsSynchronizedWithCurrentItem = true
            };
            rbSpBtn1.Items.Add(button1);
            rbSpBtn1.Items.Add(button2);

            // Add to the panel source
            pnlSrc.Items.Add(rbSpBtn1);
        }

        // Create Conditions Panel
        public static void ConditionsPanel(RibbonTab Tab, Bitmap support, Bitmap force)
        {
            RibbonPanelSource pnlSrc = new RibbonPanelSource();
            pnlSrc.Title = "Conditions";
            RibbonPanel Panel = new RibbonPanel();
            Panel.Source = pnlSrc;
            Tab.Panels.Add(Panel);

            RibbonButton button1 = new RibbonButton()
            {
                Text = "Constraints",
                ToolTip = "Set constraint condition to a group of nodes",
                ShowText = true,
                ShowImage = true,
                LargeImage = getBitmap(support),
                CommandHandler = new CmdHandler(),
                CommandParameter = "AddConstraint"
            };

            RibbonButton button2 = new RibbonButton()
            {
                Text = "Force",
                ToolTip = "Add forces to a group of nodes",
                ShowText = true,
                ShowImage = true,
                LargeImage = getBitmap(force),
                CommandHandler = new CmdHandler(),
                CommandParameter = "AddForce"
            };

            // Create a split button for conditions
            RibbonSplitButton rbSpBtn1 = new RibbonSplitButton()
            {
                ShowText = true,
                IsSplit = true,
                Size = RibbonItemSize.Large,
                IsSynchronizedWithCurrentItem = true
            };
            rbSpBtn1.Items.Add(button1);
            rbSpBtn1.Items.Add(button2);

            // Add to the panel source
            pnlSrc.Items.Add(rbSpBtn1);
        }

        // Create Analysis Panel
        public static void AnalysisPanel(RibbonTab Tab, Bitmap linear, Bitmap nonlinear)
        {
            RibbonPanelSource pnlSrc = new RibbonPanelSource();
            pnlSrc.Title = "Analysis";
            RibbonPanel Panel = new RibbonPanel();
            Panel.Source = pnlSrc;
            Tab.Panels.Add(Panel);

            RibbonButton button1 = new RibbonButton()
            {
                Text = "Linear analysis",
                ToolTip = "Do an elastic analysis of the model",
                ShowText = true,
                ShowImage = true,
                LargeImage = getBitmap(linear),
                CommandHandler = new CmdHandler(),
                CommandParameter = "DoLinearAnalysis"
            };

            RibbonButton button2 = new RibbonButton()
            {
                Text = "Nonlinear Analysis",
                ToolTip = "Do nonlinear analysis of the model",
                ShowText = true,
                ShowImage = true,
                LargeImage = getBitmap(nonlinear),
                CommandHandler = new CmdHandler(),
                CommandParameter = "DoNonlinearAnalysis"
            };

            // Create a split button for conditions
            RibbonSplitButton rbSpBtn1 = new RibbonSplitButton()
            {
                ShowText = true,
                IsSplit = true,
                Size = RibbonItemSize.Large,
                IsSynchronizedWithCurrentItem = true
            };
            rbSpBtn1.Items.Add(button1);
            rbSpBtn1.Items.Add(button2);

            // Add to the panel source
            pnlSrc.Items.Add(rbSpBtn1);
        }

        // Create View Panel
        public static void ViewPanel(RibbonTab Tab, Bitmap viewNode, Bitmap viewStringer, Bitmap viewPanel, Bitmap viewData)
        {
            RibbonPanelSource pnlSrc = new RibbonPanelSource();
            pnlSrc.Title = "View";
            RibbonPanel Panel = new RibbonPanel();
            Panel.Source = pnlSrc;
            Tab.Panels.Add(Panel);

            RibbonButton button1 = new RibbonButton()
            {
                Text = "Nodes",
                ToolTip = "Toogle view for nodes",
                ShowText = true,
                ShowImage = true,
                LargeImage = getBitmap(viewNode),
                CommandHandler = new CmdHandler(),
                CommandParameter = "ToogleNodes"
            };

            RibbonButton button2 = new RibbonButton()
            {
                Text = "Stringers",
                ToolTip = "Toogle view for stringers",
                ShowText = true,
                ShowImage = true,
                LargeImage = getBitmap(viewStringer),
                CommandHandler = new CmdHandler(),
                CommandParameter = "ToogleStringers"
            };

            RibbonButton button3 = new RibbonButton()
            {
                Text = "Panels",
                ToolTip = "Toogle view for panels",
                ShowText = true,
                ShowImage = true,
                LargeImage = getBitmap(viewPanel),
                CommandHandler = new CmdHandler(),
                CommandParameter = "TooglePanels"
            };

            // View element data button
            RibbonButton button4 = new RibbonButton()
            {
                Text = "Element data",
                ToolTip = "View data stored in a selected element",
                ShowText = true,
                ShowImage = true,
                LargeImage = getBitmap(viewData),
                CommandHandler = new CmdHandler(),
                CommandParameter = "ViewElementData"
            };

            // Create a split button
            RibbonSplitButton rbSpBtn1 = new RibbonSplitButton()
            {
                ShowText = true,
                IsSplit = true,
                Size = RibbonItemSize.Large,
                IsSynchronizedWithCurrentItem = true
            };
            rbSpBtn1.Items.Add(button1);
            rbSpBtn1.Items.Add(button2);
            rbSpBtn1.Items.Add(button3);
            rbSpBtn1.Items.Add(button4);

            // Add to the panel source
            pnlSrc.Items.Add(rbSpBtn1);
        }


        // Create Results Panel
        public static void ResultsPanel(RibbonTab Tab, Bitmap stringerF, Bitmap panelF, Bitmap displacements)
        {
            RibbonPanelSource pnlSrc = new RibbonPanelSource();
            pnlSrc.Title = "Results";
            RibbonPanel Panel = new RibbonPanel();
            Panel.Source = pnlSrc;
            Tab.Panels.Add(Panel);

            RibbonButton button1 = new RibbonButton()
            {
                Text = "Stringer forces",
                ToolTip = "Toogle view for stringer forces",
                ShowText = true,
                ShowImage = true,
                LargeImage = getBitmap(stringerF),
                CommandHandler = new CmdHandler(),
                CommandParameter = "ToogleStringerForces"
            };

            RibbonButton button2 = new RibbonButton()
            {
                Text = "Panel shear stresses",
                ToolTip = "Toogle view for panel shear stresses",
                ShowText = true,
                ShowImage = true,
                LargeImage = getBitmap(panelF),
                CommandHandler = new CmdHandler(),
                CommandParameter = "TooglePanelForces"
            };

            RibbonButton button3 = new RibbonButton()
            {
                Text = "Displacements",
                ToolTip = "Toogle view for magnified displacements of the model",
                ShowText = true,
                ShowImage = true,
                LargeImage = getBitmap(displacements),
                CommandHandler = new CmdHandler(),
                CommandParameter = "ToogleDisplacements"
            };

            // Create a split button
            RibbonSplitButton rbSpBtn1 = new RibbonSplitButton()
            {
                ShowText = true,
                IsSplit = true,
                Size = RibbonItemSize.Large,
                IsSynchronizedWithCurrentItem = true
            };
            rbSpBtn1.Items.Add(button1);
            rbSpBtn1.Items.Add(button2);
            rbSpBtn1.Items.Add(button3);

            // Add to the panel source
            pnlSrc.Items.Add(rbSpBtn1);
        }


        // Command Handler
        public class CmdHandler : System.Windows.Input.ICommand
        {
            public bool CanExecute(object parameter)
            {
                return true;
            }

            public event EventHandler CanExecuteChanged;

            public void Execute(object parameter)
            {
                if (parameter is RibbonButton)
                {
                    RibbonButton button = parameter as RibbonButton;

                    if (button != null)
                    {
                        //Make sure the command text either ends with ";", or a " "
                        string cmdText = ((string)button.CommandParameter).Trim();
                        if (!cmdText.EndsWith(";")) cmdText = cmdText + " ";
                        AutoCAD.curDoc.SendStringToExecute(cmdText, true, false, true);
                    }
                }
            }
        }
    }
}
