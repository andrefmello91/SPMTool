using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using Extensions;
using UnitsNet;
using UnitsNet.Units;
using ComboBox = System.Windows.Controls.ComboBox;
using static SPMTool.Database.UnitsData;

namespace SPMTool.UserInterface
{
    /// <summary>
    /// Lógica interna para UnitsConfig.xaml
    /// </summary>
    public partial class UnitsConfig : Window
    {
		// Properties
		private Units _units;

        public UnitsConfig()
			: this (Read(false))
        {
        }

        public UnitsConfig(Units units)
        {
	        InitializeComponent();

            // Read units
            _units = units;

			// Initiate combo boxes with units set
			InitiateComboBoxes();
        }

        /// <summary>
        /// Get combo boxes items.
        /// </summary>
        private void InitiateComboBoxes()
        {
	        GeometryBox.ItemsSource  = DimOpts;
	        GeometryBox.SelectedItem = _units.Geometry.Abbrev();

	        ReinforcementBox.ItemsSource  = DimOpts;
	        ReinforcementBox.SelectedItem = _units.Reinforcement.Abbrev();

	        DisplacementsBox.ItemsSource  = DimOpts;
	        DisplacementsBox.SelectedItem = _units.Displacements.Abbrev();

	        AppliedForcesBox.ItemsSource  = FOpts;
	        AppliedForcesBox.SelectedItem = _units.AppliedForces.Abbrev();

	        StringerForcesBox.ItemsSource  = FOpts;
	        StringerForcesBox.SelectedItem = _units.StringerForces.Abbrev();

	        PanelStressesBox.ItemsSource  = StOpts;
	        PanelStressesBox.SelectedItem = _units.PanelStresses.Abbrev();

	        MaterialBox.ItemsSource  = StOpts;
	        MaterialBox.SelectedItem = _units.MaterialStrength.Abbrev();
        }

		/// <summary>
        /// Update units after selection changes.
        /// </summary>
        private void Box_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
	        var cmbx = (ComboBox) sender;

	        switch (cmbx.Name)
	        {
                case "GeometryBox":
	                _units.Geometry = UnitParser.Default.Parse<LengthUnit>((string) cmbx.SelectedItem);
					break;

                case "ReinforcementBox":
	                _units.Reinforcement = UnitParser.Default.Parse<LengthUnit>((string)cmbx.SelectedItem);
	                break;

                case "DisplacementsBox":
	                _units.Displacements = UnitParser.Default.Parse<LengthUnit>((string)cmbx.SelectedItem);
	                break;

                case "AppliedForcesBox":
	                _units.AppliedForces = UnitParser.Default.Parse<ForceUnit>((string)cmbx.SelectedItem);
	                break;

                case "StringerForcesBox":
	                _units.StringerForces = UnitParser.Default.Parse<ForceUnit>((string)cmbx.SelectedItem);
	                break;

                case "PanelStressesBox":
	                _units.PanelStresses = UnitParser.Default.Parse<PressureUnit>((string)cmbx.SelectedItem);
	                break;

                case "MaterialBox":
	                _units.MaterialStrength = UnitParser.Default.Parse<PressureUnit>((string)cmbx.SelectedItem);
	                break;
	        }
        }

		/// <summary>
        /// Close window if cancel button is clicked.
        /// </summary>
        private void ButtonCancel_OnClick(object sender, RoutedEventArgs e) => Close();

		/// <summary>
        /// Save units if OK button is clicked.
        /// </summary>
        private void ButtonOK_OnClick(object sender, RoutedEventArgs e)
        {
			// Save units on database
			Save(_units);

			Close();
        }
    }
}
