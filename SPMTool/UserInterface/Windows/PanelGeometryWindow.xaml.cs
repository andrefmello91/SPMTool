using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Extensions;
using Extensions.Interface;
using Extensions.Number;
using Material.Reinforcement;
using MathNet.Numerics;
using SPM.Elements.StringerProperties;
using SPMTool.Database;
using SPMTool.Database.Elements;
using SPMTool.Enums;
using UnitsNet;
using MessageBox = System.Windows.MessageBox;
using Stringer = SPM.Elements.Stringer;
using static SPMTool.Database.Elements.Panels;
using static SPMTool.Database.Elements.ElementData;
using Window = System.Windows.Window;

namespace SPMTool.UserInterface
{
    /// <summary>
    /// Lógica interna para StringerGeometryWindow.xaml
    /// </summary>
    public partial class PanelGeometryWindow : Window
    {
	    private readonly Solid[] _panels;
	    private double[] _savedWidths;

        // Properties
        public string GeometryUnit => DataBase.Units.Geometry.Abbrev();

        /// <summary>
        /// Verify if geometry text boxes are filled.
        /// </summary>
        private bool WidthSet => WBox.Text.ParsedAndNotZero(out _);

        public PanelGeometryWindow(IEnumerable<Solid> panels)
        {
	        _panels = panels.ToArray();

			InitializeComponent();

            // Get stringer image
            CrossSection.Source = Ribbon.GetBitmap(Properties.Resources.stringer_cross_section);

            GetInitialData();

            DataContext = this;
		}

		/// <summary>
        /// Get the initial data of the panel.
        /// </summary>
        private void GetInitialData()
		{
			_savedWidths = DataBase.SavedPanelWidth?.OrderBy(w => w).ToArray();

			if (_savedWidths != null && _savedWidths.Any())
			{
				SavedWidths.ItemsSource = SavedOptions();
				SavedWidths.SelectedIndex = 0;

				WBox.Text = $"{_savedWidths[0].ConvertFromMillimeter(DataBase.Units.Geometry):0.00}";
			}
			else
			{
				SavedWidths.Disable();

				WBox.Text = $"{100.ConvertFromMillimeter(DataBase.Units.Geometry):0.00}";
			}
        }

		/// <summary>
        /// Get saved options as string collection.
        /// </summary>
        /// <returns></returns>
		private IEnumerable<string> SavedOptions() => _savedWidths.Select(geo => $"{geo.ConvertFromMillimeter(DataBase.Units.Geometry):0.00}");

		private void DoubleValidationTextBox(object sender, TextCompositionEventArgs e)
		{
			var regex = new Regex("[^0-9.]+");
			e.Handled = regex.IsMatch(e.Text);
		}

		/// <summary>
        /// Save data in the stringer object.
        /// </summary>
		private void SaveData()
		{
			// Get values
			double.TryParse(WBox.Text, out var width);
			
			// Convert to mm
			var w = width.Convert(DataBase.Units.Geometry);

			// Save on database
			Save(w);

			// Set to stringers
			foreach (var pnl in _panels)
				SetWidth(pnl, w);
		}

        private void ButtonOK_OnClick(object sender, RoutedEventArgs e)
		{
			// Check geometry
			if (!WidthSet)
				MessageBox.Show("Please set stringer geometry.", "Alert");

			else
			{
				SaveData();
				Close();
			}
		}

		private void ButtonCancel_OnClick(object sender, RoutedEventArgs e) => Close();

		private void SavedGeometries_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			var box = (ComboBox) sender;

			// Get index
			var i = box.SelectedIndex;

			if (i >= _savedWidths.Length || i < 0)
				return;

            // Update textboxes
            WBox.Text = $"{_savedWidths[i].ConvertFromMillimeter(DataBase.Units.Geometry):0.00}";
		}
    }
}
