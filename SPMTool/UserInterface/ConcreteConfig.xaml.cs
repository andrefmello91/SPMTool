using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Extensions;
using Extensions.Interface;
using Extensions.Number;
using Material;
using Material.Concrete;
using Parameters = Material.Concrete.Parameters;
using SPMTool.Database;
using SPMTool.Database.Materials;
using ComboBox = System.Windows.Controls.ComboBox;
using MessageBox = System.Windows.MessageBox;
using TextBox = System.Windows.Controls.TextBox;
using static SPMTool.Database.Materials.ConcreteData;

namespace SPMTool.UserInterface
{
    /// <summary>
    /// Lógica interna para ConcreteConfig.xaml
    /// </summary>
    public partial class ConcreteConfig : Window
	{
		// Properties
		private Units _units;
		private ParameterModel _parameterModel;
		private ConstitutiveModel _constitutiveModel;
		private Parameters _parameters;

		/// <summary>
        /// Get the stress unit.
        /// </summary>
		protected string StressUnit => _units.MaterialStrength.Abbrev();

		/// <summary>
        /// Get aggregate diameter unit.
        /// </summary>
        protected string AggregateUnit => _units.Reinforcement.Abbrev();

		/// <summary>
        /// Get the output <see cref="ConcreteData"/> object.
        /// </summary>
		public Concrete OutputConcrete => new Concrete(_parameters, _constitutiveModel);

        public ConcreteConfig()
	        : this (Read(false))
        {
        }

        public ConcreteConfig(Concrete concrete)
	        : this (concrete?.Parameters ?? new MC2010Parameters(30, 19), ConstitutiveModel.MCFT)
        {
		}

        public ConcreteConfig(Parameters parameters, Constitutive constitutive)
	        : this (parameters, Constitutive.ReadConstitutiveModel(constitutive))
        {
		}

		public ConcreteConfig(Parameters parameters, ConstitutiveModel constitutiveModel)
		{
			// Read units
			_units = DataBase.Units;

            InitializeComponent();

			// Get settings
			_parameters        = parameters;
			_parameterModel    = Parameters.ReadParameterModel(_parameters);
			_constitutiveModel = constitutiveModel;

			// Initiate combo boxes with units set
            InitiateComboBoxes();

            DataContext = this;
		}

		/// <summary>
		/// Verify if strength and aggregate diameter text boxes are filled.
		/// </summary>
		private bool ParametersSet => CheckBoxes(new[] { StrengthBox, AggDiamBox });

		/// <summary>
		/// Verify if custom parameters text boxes are filled.
		/// </summary>
		private bool CustomParametersSet => CheckBoxes(new[] { ModuleBox, TensileBox, PlasticStrainBox, UltStrainBox });

		/// <summary>
        /// Check if <paramref name="textBoxes"/> are filled and not zero.
        /// </summary>
		private bool CheckBoxes(IEnumerable<TextBox> textBoxes) => textBoxes.All(textBox => textBox.Text.ParsedAndNotZero(out _));

        /// <summary>
        /// Initiate combo boxes items.
        /// </summary>
        private void InitiateComboBoxes()
		{
			StrengthBox.Text = $"{_parameters.Strength.ConvertFromMPa(_units.MaterialStrength):0.00}";

			AggDiamBox.Text = $"{_parameters.AggregateDiameter.ConvertFromMillimeter(_units.Reinforcement):0.00}";

            AggTypeBox.ItemsSource  = Enum.GetNames(typeof(AggregateType));
			AggTypeBox.SelectedItem = _parameters.Type.ToString();

			ConstitutiveBox.ItemsSource  = Enum.GetNames(typeof(ConstitutiveModel));
			ConstitutiveBox.SelectedItem = _constitutiveModel.ToString();

			ParameterBox.ItemsSource = Enum.GetNames(typeof(ParameterModel));
			ParameterBox.SelectedItem = _parameterModel.ToString();

            UpdateCustomParameters();
		}

        /// <summary>
        /// Update parameters.
        /// </summary>
        private void UpdateParameters()
		{
			if (_parameterModel == ParameterModel.Custom || StrengthBox.Text == string.Empty || AggDiamBox.Text == string.Empty || AggTypeBox.SelectedItem.ToString() == string.Empty)
				return;

			// Read parameters
			double
				fc    = double.Parse(StrengthBox.Text, CultureInfo.InvariantCulture).Convert(_units.MaterialStrength),
				phiAg = double.Parse(AggDiamBox.Text, CultureInfo.InvariantCulture).Convert(_units.Reinforcement);

			var aggType = (AggregateType) Enum.Parse(typeof(AggregateType), AggTypeBox.SelectedItem.ToString());

			// Get parameters
			_parameters = Parameters.ReadParameters(_parameterModel, fc, phiAg, aggType);
		}

        /// <summary>
        /// Update custom parameters.
        /// </summary>
        private void UpdateCustomParameters()
		{
			_parameters.UpdateParameters();

			ModuleBox.Text  = $"{_parameters.InitialModule.ConvertFromMPa(_units.MaterialStrength):0.00}";

			TensileBox.Text = $"{_parameters.TensileStrength.ConvertFromMPa(_units.MaterialStrength):0.00}";

			PlasticStrainBox.Text = $"{-1000 * _parameters.PlasticStrain:0.00}";

			UltStrainBox.Text = $"{-1000 * _parameters.UltimateStrain:0.00}";
		}

        /// <summary>
        /// Get custom parameters.
        /// </summary>
        private void GetCustomParameters()
		{
			// Read parameters
			double
				fc    = double.Parse(StrengthBox.Text).Convert(_units.MaterialStrength),
				phiAg = double.Parse(AggDiamBox.Text).Convert(_units.Reinforcement),
				Ec    = double.Parse(ModuleBox.Text).Convert(_units.MaterialStrength),
				ft    = double.Parse(TensileBox.Text).Convert(_units.MaterialStrength),
				ec    = double.Parse(PlasticStrainBox.Text) * -0.001,
				ecu   = double.Parse(UltStrainBox.Text) * -0.001;

			_parameters = new CustomParameters(fc, phiAg, ft, Ec, ec, ecu);
		}

		private void ParameterBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			var parBox = (ComboBox) sender;

			if (parBox.SelectedItem.ToString() == string.Empty)
				return;

			_parameterModel = (ParameterModel) Enum.Parse(typeof(ParameterModel), parBox.SelectedItem.ToString());

			var customBoxes = new[] { ModuleBox, TensileBox, PlasticStrainBox, UltStrainBox };

			if (_parameterModel == ParameterModel.Custom)
				customBoxes.Enable();

			else
			{
				UpdateParameters();
				UpdateCustomParameters();
				customBoxes.Disable();
			}
		}

		private void StrengthBox_OnTextChanged(object sender, TextChangedEventArgs e)
		{
			var fcBox = (TextBox) sender;

			if (_parameterModel == ParameterModel.Custom || fcBox.Text == string.Empty)
				return;

			_parameters.Strength = double.Parse(fcBox.Text);
            UpdateCustomParameters();
		}

		private void AggTypeBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			var aggBox = (ComboBox) sender;

			if (_parameterModel == ParameterModel.Custom || aggBox.SelectedItem.ToString() == string.Empty)
				return;

			_parameters.Type = (AggregateType)Enum.Parse(typeof(AggregateType), aggBox.SelectedItem.ToString());
			UpdateCustomParameters();
		}

		private void ConstitutiveBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			var constBox = (ComboBox)sender;

			if (constBox.SelectedItem.ToString() == string.Empty)
				return;

			_constitutiveModel = (ConstitutiveModel)Enum.Parse(typeof(ConstitutiveModel), constBox.SelectedItem.ToString());
		}

        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
		{
			var regex = new Regex("[^0-9.]+");
			e.Handled = regex.IsMatch(e.Text);
		}

        private void ButtonCancel_OnClick(object sender, RoutedEventArgs e)
		{
			Close();
		}

		private void ButtonOK_OnClick(object sender, RoutedEventArgs e)
		{
			// Verify if text boxes are filled
			if (!ParametersSet)
				MessageBox.Show("Please set concrete strength and aggregate diameter.", "Alert");

			else if (_parameterModel == ParameterModel.Custom && !CustomParametersSet)
				MessageBox.Show("Please set concrete custom parameters.", "Alert");

			else
			{
				if (_parameterModel == ParameterModel.Custom)
					GetCustomParameters();
				else
					UpdateParameters();

				// Save units on database
				Save(_parameters, _constitutiveModel);
				Close();
			}
		}

	}
}
