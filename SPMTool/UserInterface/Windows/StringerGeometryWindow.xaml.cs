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
using static SPMTool.Database.Elements.Stringers;
using static SPMTool.Database.Elements.ElementData;
using Window = System.Windows.Window;

namespace SPMTool.UserInterface
{
    /// <summary>
    /// Lógica interna para StringerGeometryWindow.xaml
    /// </summary>
    public partial class StringerGeometryWindow : Window
    {
	    private readonly Line[] _stringers;
	    private StringerGeometry[] _savedGeometries;

        // Properties
        public string GeometryUnit => DataBase.Units.Geometry.Abbrev();

		/// <summary>
        /// Get geometry <see cref="TextBox"/>'s.
        /// </summary>
        private IEnumerable<TextBox> GeometryBoxes => new[] { WBox, HBox };

        /// <summary>
        /// Verify if geometry text boxes are filled.
        /// </summary>
        private bool GeometrySet => CheckBoxes(GeometryBoxes);

        public StringerGeometryWindow(IEnumerable<Line> stringers)
        {
	        _stringers = stringers.ToArray();

			InitializeComponent();

            // Get stringer image
            CrossSection.Source = Ribbon.GetBitmap(Properties.Resources.stringer_cross_section);

            GetInitialData();

            DataContext = this;
		}

		/// <summary>
        /// Get the initial data of the stringer.
        /// </summary>
        private void GetInitialData()
		{
			_savedGeometries = DataBase.SavedStringerGeometry?.OrderBy(geo => geo.Height).ThenBy(geo => geo.Width).ToArray();

			if (_savedGeometries != null && _savedGeometries.Any())
			{
				SavedGeometries.ItemsSource = SavedOptions();
				SavedGeometries.SelectedIndex = 0;

				WBox.Text = $"{_savedGeometries[0].Width.ConvertFromMillimeter(DataBase.Units.Geometry):0.00}";
				HBox.Text = $"{_savedGeometries[0].Height.ConvertFromMillimeter(DataBase.Units.Geometry):0.00}";
			}
			else
			{
				SavedGeometries.Disable();

				WBox.Text = $"{100.ConvertFromMillimeter(DataBase.Units.Geometry):0.00}";
				HBox.Text = $"{100.ConvertFromMillimeter(DataBase.Units.Geometry):0.00}";
			}
        }

		/// <summary>
        /// Get saved options as string collection.
        /// </summary>
        /// <returns></returns>
		private IEnumerable<string> SavedOptions() => _savedGeometries.Select(geo => $"{geo.Width.ConvertFromMillimeter(DataBase.Units.Geometry):0.0} {(char) Character.Times} {geo.Height.ConvertFromMillimeter(DataBase.Units.Geometry):0.0}");

        /// <summary>
        /// Check if <paramref name="textBoxes"/> are filled and not zero.
        /// </summary>
        private bool CheckBoxes(IEnumerable<TextBox> textBoxes) => textBoxes.All(textBox => textBox.Text.ParsedAndNotZero(out _));

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
			double.TryParse(HBox.Text, out var height);
			
			// Convert values
			var geometry = new StringerGeometry(Point3d.Origin, Point3d.Origin, width, height, DataBase.Units.Geometry);

			// Save on database
			Save(geometry);

			// Set to stringers
			foreach (var str in _stringers)
				SetGeometry(str, geometry);
		}

        private void ButtonOK_OnClick(object sender, RoutedEventArgs e)
		{
			// Check geometry
			if (!GeometrySet)
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

			if (i >= _savedGeometries.Length || i < 0)
				return;

            // Update textboxes
            WBox.Text = $"{_savedGeometries[i].Width.ConvertFromMillimeter(DataBase.Units.Geometry):0.00}";
            HBox.Text = $"{_savedGeometries[i].Height.ConvertFromMillimeter(DataBase.Units.Geometry):0.00}";
		}
    }
}
