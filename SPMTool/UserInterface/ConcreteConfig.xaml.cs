using System;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using Material;
using Material.Concrete;
using Parameters = Material.Concrete.Parameters;
using Material.Concrete;
using SPMTool.AutoCAD;
using UnitsNet;
using UnitsNet.Units;
using ComboBox = System.Windows.Controls.ComboBox;
using MessageBox = System.Windows.MessageBox;
using TextBox = System.Windows.Controls.TextBox;

namespace SPMTool.UserInterface
{
    /// <summary>
    /// Lógica interna para ConcreteConfig.xaml
    /// </summary>
    public partial class ConcreteConfig : Window
	{
		// Properties
		private Units             Units              { get; }
		private Parameters        Parameters         { get; set; }
		private Constitutive      Constitutive       { get; }
		private ParameterModel    ParameterModel     { get; set; }
		private ConstitutiveModel ConstitutiveModel  { get; }
		public string             StressUnit         { get; }
		public string             AggregateUnit      { get; }

		// Unit options
		private readonly string[]
			ParOpts = AutoCAD.Material.Models,
			BhOpts  = AutoCAD.Material.Behaviors,
			AgOpts  = AutoCAD.Material.AggregateTypes;

		public ConcreteConfig(Parameters parameters, Constitutive constitutive, Units units = null)
		{
			// Read units
			Units = (units ?? Config.ReadUnits()) ?? new Units();
			StressUnit = Pressure.GetAbbreviation(Units.MaterialStrength);
			AggregateUnit = Length.GetAbbreviation(Units.Reinforcement);

            InitializeComponent();

			// Get settings
			Parameters        = parameters;
			Constitutive      = constitutive;
			ParameterModel    = Parameters.ReadParameterModel(Parameters);
			ConstitutiveModel = Constitutive.ReadConstitutiveModel(Constitutive);

			// Initiate combo boxes with units set
            InitiateComboBoxes();

            DataContext = this;
		}

		/// <summary>
		/// Verify if strength and aggregate diameter text boxes are filled.
		/// </summary>
		private bool ParametersSet
		{
			get
			{
				var textBoxes = new[] { StrengthBox, AggDiamBox };
				foreach (var textBox in textBoxes)
				{
					if (!GlobalAuxiliary.ParsedAndNotZero(textBox.Text))
						return false;
				}

				return true;
			}
		}

		/// <summary>
		/// Verify if custom parameters text boxes are filled.
		/// </summary>
		private bool CustomParametersSet
		{
			get
			{
				var textBoxes = new[] { ModuleBox, TensileBox, PlasticStrainBox, UltStrainBox };
				foreach (var textBox in textBoxes)
				{
					if (!GlobalAuxiliary.ParsedAndNotZero(textBox.Text))
						return false;
				}

				return true;
			}
		}

        // Get combo boxes items
        private void InitiateComboBoxes()
		{
			StrengthBox.Text = $"{Units.ConvertFromMPa(Parameters.Strength, Units.MaterialStrength):0.00}";

			AggDiamBox.Text = $"{Units.ConvertFromMillimeter(Parameters.AggregateDiameter, Units.Reinforcement):0.00}";

            AggTypeBox.ItemsSource  = AgOpts;
			AggTypeBox.SelectedItem = Parameters.Type.ToString();

			BehaviorBox.ItemsSource  = BhOpts;
			BehaviorBox.SelectedItem = ConstitutiveModel.ToString();

			ParameterBox.ItemsSource = ParOpts;
			ParameterBox.SelectedItem = ParameterModel.ToString();

            UpdateCustomParameters();
		}

		// Update parameters
		private void UpdateParameters()
		{
			if (ParameterModel != ParameterModel.Custom && StrengthBox.Text != string.Empty && AggDiamBox.Text != string.Empty && AggTypeBox.SelectedItem.ToString() != string.Empty)
			{
				// Read parameters
				double
					fc    = Units.ConvertToMPa(double.Parse(StrengthBox.Text, CultureInfo.InvariantCulture), Units.MaterialStrength),
					phiAg = Units.ConvertToMillimeter(double.Parse(AggDiamBox.Text, CultureInfo.InvariantCulture), Units.Reinforcement);

				var aggType = (AggregateType) Enum.Parse(typeof(AggregateType), AggTypeBox.SelectedItem.ToString());

				// Get parameters
				Parameters = Parameters.ReadParameters(ParameterModel, fc, phiAg, aggType);
			}
		}

		// Update custom parameters
		private void UpdateCustomParameters()
		{
			Parameters.UpdateParameters();

			ModuleBox.Text  = $"{Units.ConvertFromMPa(Parameters.InitialModule, Units.MaterialStrength):0.00}";

			TensileBox.Text = $"{Units.ConvertFromMPa(Parameters.TensileStrength, Units.MaterialStrength):0.00}";

			PlasticStrainBox.Text = $"{-1000 * Parameters.PlasticStrain:0.00}";

			UltStrainBox.Text = $"{-1000 * Parameters.UltimateStrain:0.00}";
		}

		// Get custom parameters
		private void GetCustomParameters()
		{
			// Read parameters
			double
				fc    = Units.ConvertToMPa(double.Parse(StrengthBox.Text), Units.MaterialStrength),
				phiAg = Units.ConvertToMillimeter(double.Parse(AggDiamBox.Text), Units.Reinforcement),
				Ec    = Units.ConvertToMPa(double.Parse(ModuleBox.Text), Units.MaterialStrength),
				ft    = Units.ConvertToMPa(double.Parse(TensileBox.Text), Units.MaterialStrength),
				ec    = double.Parse(PlasticStrainBox.Text) * -0.001,
				ecu   = double.Parse(UltStrainBox.Text) * -0.001;

			Parameters = new CustomParameters(fc, phiAg, ft, Ec, ec, ecu);
		}

		// Disable a textbox
		private void DisableTextBox(TextBox textBox)
		{
			textBox.IsEnabled = false;
		}

		// Enable a textbox
		private void EnableTextBox(TextBox textBox)
		{
			textBox.IsEnabled = true;
		}

		private void ParameterBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			var parBox = (ComboBox) sender;

			if (parBox.SelectedItem.ToString() != string.Empty)
			{
				ParameterModel = (ParameterModel) Enum.Parse(typeof(ParameterModel), parBox.SelectedItem.ToString());

				var customBoxes = new[] { ModuleBox, TensileBox, PlasticStrainBox, UltStrainBox };

				if (ParameterModel == ParameterModel.Custom)
				{
					foreach (var box in customBoxes)
					{
						EnableTextBox(box);
					}
				}
				else
				{
					UpdateParameters();
					UpdateCustomParameters();
					foreach (var box in customBoxes)
					{
						DisableTextBox(box);
					}
				}
			}
		}

		private void StrengthBox_OnTextChanged(object sender, TextChangedEventArgs e)
		{
			var fcBox = (TextBox) sender;

            if (ParameterModel != ParameterModel.Custom && fcBox.Text != string.Empty)
            {
	            Parameters.Strength = double.Parse(fcBox.Text);
				UpdateCustomParameters();
			}
		}

		private void AggTypeBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			var aggBox = (ComboBox) sender;

			if (ParameterModel != ParameterModel.Custom && aggBox.SelectedItem.ToString() != string.Empty)
			{
				Parameters.Type = (AggregateType)Enum.Parse(typeof(AggregateType), aggBox.SelectedItem.ToString());
                UpdateCustomParameters();
			}
		}

		private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
		{
			Regex regex = new Regex("[^0-9.]+");
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
			{
				MessageBox.Show("Please set concrete strength and aggregate diameter.", "Alert");
			}

			else if (ParameterModel == ParameterModel.Custom && !CustomParametersSet)
			{
				MessageBox.Show("Please set concrete custom parameters.", "Alert");
			}

			else
			{
				if (ParameterModel == ParameterModel.Custom)
					GetCustomParameters();
				else
					UpdateParameters();

				// Save units on database
				AutoCAD.Material.SaveConcreteParameters(Parameters, ConstitutiveModel);
				Close();
			}
		}
	}
}
