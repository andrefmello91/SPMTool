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
    public class UserInterface
    {
        public class Initializer : IExtensionApplication
        {
            public void Initialize()
            {
                Autodesk.AutoCAD.ApplicationServices.Application.Idle +=
                new EventHandler(on_ApplicationIdle);
            }

            public void on_ApplicationIdle(object sender, EventArgs e)
            {
                RibbonButtons();
                Autodesk.AutoCAD.ApplicationServices.Application.Idle -= on_ApplicationIdle;
            }
            
            public void Terminate()
            {
                
            }
        }

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
            Autodesk.Windows.RibbonControl ribbonControl = Autodesk.Windows.ComponentManager.Ribbon;

            // Check the current theme
            short theme = (short)Application.GetSystemVariable("COLORTHEME");

            // Initialize the icons
            Bitmap strBmp, pnlBmp,
                   setBmp, dvStrBmp, dvPnlBmp,
                   updtBmp, viewBmp,
                   cncrtBmp, stlBmp,
                   suprtBmp, fcBmp;

            // If the theme is dark (0), get the light icons
            if (theme == 0)
            {
                strBmp = Properties.Resources.stringer_large_light;
                pnlBmp = Properties.Resources.panel_large_light;
                setBmp = Properties.Resources.set_small_light;
                dvStrBmp = Properties.Resources.divstr_small_light;
                dvPnlBmp = Properties.Resources.divpnl_small_light;
                updtBmp = Properties.Resources.update_small_light;
                viewBmp = Properties.Resources.view_small_light;
                cncrtBmp = Properties.Resources.concrete_large_light;
                stlBmp = Properties.Resources.steel_large_light;
                suprtBmp = Properties.Resources.support_large_light;
                fcBmp = Properties.Resources.force_large_light;
            }
            else // If the theme is light
            {
                strBmp = Properties.Resources.stringer_large;
                pnlBmp = Properties.Resources.panel_large;
                setBmp = Properties.Resources.set_small;
                dvStrBmp = Properties.Resources.divstr_small;
                dvPnlBmp = Properties.Resources.divpnl_small;
                updtBmp = Properties.Resources.update_small;
                viewBmp = Properties.Resources.view_small;
                cncrtBmp = Properties.Resources.concrete_large;
                stlBmp = Properties.Resources.steel_large;
                suprtBmp = Properties.Resources.support_large;
                fcBmp = Properties.Resources.force_large;
            }

            // Create the Ribbon Tab
            RibbonTab Tab = new RibbonTab()
            {
                Title = Global.appName,
                Id = "ApplicationTab"
            };
            ribbonControl.Tabs.Add(Tab);

            // Create the Ribbon panels
            GeometryPanel(Tab, strBmp, pnlBmp, setBmp, dvStrBmp, dvPnlBmp, updtBmp, viewBmp);
            MaterialPanel(Tab, cncrtBmp, stlBmp, viewBmp);
            ConditionsPanel(Tab, suprtBmp, fcBmp);

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
                ToolTip = "Create a panel conecting four nodes",
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
                Text = "Stringer parameters",
                ToolTip = "Set the geometry and steel reinforcement to a selection of stringers",
                ShowText = true,
                ShowImage = true,
                Image = getBitmap(set),
                CommandHandler = new CmdHandler(),
                CommandParameter = "SetStringerParameters"
            };

            RibbonButton button5 = new RibbonButton()
            {
                Text = "Panel parameters",
                ToolTip = "Set the geometry and steel reinforcement to a selection of panels",
                ShowText = true,
                ShowImage = true,
                Image = getBitmap(set),
                CommandHandler = new CmdHandler(),
                CommandParameter = "SetPanelParameters"
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
                ToolTip = "Divide a panel into smaller ones and creates internal nodes and stringers (surrounding stringers still need to be divided).",
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
            rbSpBtn3.Items.Add(button6);
            rbSpBtn3.Items.Add(button7);

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

            // Create a dropdown menu to secondary commands
            pnlSrc.Items.Add(new RibbonPanelBreak());

            // View element data button
            RibbonButton button9 = new RibbonButton()
            {
                Text = "View element data",
                ToolTip = "View information stored in a determined element",
                ShowText = true,
                ShowImage = true,
                Image = getBitmap(view),
                CommandHandler = new CmdHandler(),
                CommandParameter = "ViewElementData"
            };
            pnlSrc.Items.Add(button9);
        }

        // Create Material Panel
        public static void MaterialPanel(RibbonTab Tab, Bitmap concrete, Bitmap steel, Bitmap view)
        {
            RibbonPanelSource pnlSrc = new RibbonPanelSource();
            pnlSrc.Title = "Materials";
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
                Text = "Steel",
                ToolTip = "Set concrete yield strength and elastic module",
                ShowText = true,
                ShowImage = true,
                LargeImage = getBitmap(steel),
                CommandHandler = new CmdHandler(),
                CommandParameter = "SetSteelParameters"
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

            // Create a dropdown menu to secondary commands
            pnlSrc.Items.Add(new RibbonPanelBreak());

            RibbonButton button3 = new RibbonButton()
            {
                Text = "View material parameters",
                ToolTip = "View concrete and steel parameters",
                ShowText = true,
                ShowImage = true,
                LargeImage = getBitmap(view),
                CommandHandler = new CmdHandler(),
                CommandParameter = "ViewMaterialParameters"
            };
            pnlSrc.Items.Add(button3);
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
                Text = "Support",
                ToolTip = "Set support condition to a group of nodes",
                ShowText = true,
                ShowImage = true,
                LargeImage = getBitmap(support),
                CommandHandler = new CmdHandler(),
                CommandParameter = "AddSupport"
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
                        Global.curDoc.SendStringToExecute(cmdText, true, false, true);
                    }
                }
            }
        }
    }
}
