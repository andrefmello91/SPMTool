using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Extensions;
using Material.Concrete;
using SPMTool.Core;
using SPMTool.Extensions;
using UnitsNet;
using UnitsNet.Units;

namespace SPMTool.Application.UserInterface
{
	/// <summary>
	///     Lógica interna para ConcreteConfig.xaml
	/// </summary>
	public partial class ConcreteConfig : Window
	{
		#region Fields

		// Options
		private readonly string[]
			_aggTypeOptions     = Enum.GetNames(typeof(AggregateType)),
			_contitutiveOptions = { ConstitutiveModel.MCFT.ToString(), ConstitutiveModel.DSFM.ToString() },
			_parameterOptions   = Enum.GetNames(typeof(ParameterModel));

		private readonly LengthUnit _aggUnit;

		// Properties
		private readonly PressureUnit _stressUnit;
		private ConstitutiveModel _constitutiveModel;
		private IParameters _parameters;

		#endregion

		#region Properties

		/// <summary>
		///     Get aggregate diameter unit.
		/// </summary>
		public string AggregateUnit => _aggUnit.Abbrev();

		/// <summary>
		///		Get the text boxes for custom parameters.
		/// </summary>
		private TextBox[] CustomParameterBoxes => new[] { ModuleBox, TensileBox, PlasticStrainBox, UltStrainBox };

		/// <summary>
		///     Verify if custom parameters text boxes are filled.
		/// </summary>
		private bool CustomParametersSet => CheckBoxes(CustomParameterBoxes);

		/// <summary>
		///     Verify if strength and aggregate diameter text boxes are filled.
		/// </summary>
		private bool ParametersSet => CheckBoxes(new[] { StrengthBox, AggDiamBox });

		/// <summary>
		///     Get the stress unit.
		/// </summary>
		public string StressUnit => _stressUnit.Abbrev();

		#endregion

		#region Constructors

		public ConcreteConfig()
		{
			// Read units
			_stressUnit = DataBase.Settings.Units.MaterialStrength;
			_aggUnit    = DataBase.Settings.Units.Reinforcement;

			// Get settings
			_parameters = DataBase.ConcreteData.Parameters;
			_constitutiveModel = DataBase.ConcreteData.ConstitutiveModel;

			// Update units
			_parameters.ChangeUnit(_stressUnit);
			_parameters.ChangeUnit(_aggUnit);

			InitializeComponent();

			// Get image
			Graph.Source = Icons.GetBitmap(Properties.Resources.concrete_constitutive);

			// Initiate combo boxes and set events
			InitiateComboBoxes();
			SetEvents();

			DataContext = this;
		}

		#endregion

		#region  Methods

		/// <summary>
		///     Check if <paramref name="textBoxes" /> are filled and not zero.
		/// </summary>
		private bool CheckBoxes(IEnumerable<TextBox> textBoxes) => textBoxes.All(textBox => textBox.Text.ParsedAndNotZero(out _));

		/// <summary>
		///     Initiate combo boxes items.
		/// </summary>
		private void InitiateComboBoxes()
		{
			StrengthBox.Text = $"{_parameters.Strength.Value:0.00}";

			AggDiamBox.Text = $"{_parameters.AggregateDiameter.Value:0.00}";

			AggTypeBox.ItemsSource  = _aggTypeOptions;
			AggTypeBox.SelectedItem = _parameters.Type.ToString();

			ParameterBox.ItemsSource  = _parameterOptions;
			ParameterBox.SelectedItem = _parameters.Model.ToString();

			ConstitutiveBox.ItemsSource  = _contitutiveOptions;
			ConstitutiveBox.SelectedItem = _constitutiveModel.ToString();

			UpdateCustomParameterBoxes();

			if (_parameters.Model != ParameterModel.Custom)
				CustomParameterBoxes.Disable();
		}

		/// <summary>
		///		Set events in UI elements.
		/// </summary>
		private void SetEvents()
		{
			StrengthBox.TextChanged          += StrengthBox_OnTextChanged;
			AggTypeBox.SelectionChanged      += AggTypeBox_OnSelectionChanged;
			ConstitutiveBox.SelectionChanged += ConstitutiveBox_OnSelectionChanged;
			ParameterBox.SelectionChanged    += ParameterBox_OnSelectionChanged;
		}

		/// <summary>
		///     Update parameters.
		/// </summary>
		private void UpdateParameters(ParameterModel model)
		{
			if (StrengthBox.Text == string.Empty || AggDiamBox.Text == string.Empty || AggTypeBox.SelectedItem.ToString() == string.Empty)
				return;

			var type  = (AggregateType)Enum.Parse(typeof(AggregateType), AggTypeBox.SelectedItem.ToString()!);

			_parameters = _parameters switch
			{
				CustomParameters cusPar when model != ParameterModel.Custom => cusPar.ToParameters(model, type),
				Parameters       par    when model == ParameterModel.Custom => par.ToCustomParameters(),
				_                                                           => _parameters
			};

			// Read parameters
			_parameters.Model             = model;
			_parameters.Strength          = Pressure.From(double.Parse(StrengthBox.Text), _stressUnit);
			_parameters.AggregateDiameter = Length.From(double.Parse(AggDiamBox.Text), _aggUnit);
			_parameters.Type              = type;
		}

		/// <summary>
		///     Update custom parameters.
		/// </summary>
		private void UpdateCustomParameterBoxes()
		{
			ModuleBox.Text  = $"{_parameters.ElasticModule.Value:0.00}";

			TensileBox.Text = $"{_parameters.TensileStrength.Value:0.00}";

			PlasticStrainBox.Text = $"{-1000 * _parameters.PlasticStrain:0.00}";

			UltStrainBox.Text = $"{-1000 * _parameters.UltimateStrain:0.00}";
		}

		/// <summary>
		///     Get custom parameters.
		/// </summary>
		private void GetCustomParameters()
		{
			if (!(_parameters is CustomParameters cusPar))
				return;

			// Read parameters
			cusPar.TensileStrength = Pressure.From(double.Parse(TensileBox.Text), _stressUnit);
			cusPar.ElasticModule = Pressure.From(double.Parse(ModuleBox.Text), _stressUnit);
			cusPar.PlasticStrain = double.Parse(PlasticStrainBox.Text) * -0.001;
			cusPar.UltimateStrain = double.Parse(UltStrainBox.Text) * -0.001;

			_parameters = cusPar;

			//double
			//	fc    = double.Parse(StrengthBox.Text),
			//	phiAg = double.Parse(AggDiamBox.Text),
			//	Ec    = double.Parse(ModuleBox.Text),
			//	ft    = double.Parse(TensileBox.Text),
			//	ec    = double.Parse(PlasticStrainBox.Text) * -0.001,
			//	ecu   = double.Parse(UltStrainBox.Text) * -0.001;

			//_parameters = new CustomParameters(fc, phiAg, ft, Ec, ec, ecu, _stressUnit, _aggUnit);
		}

        private void ParameterBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			var parBox = (ComboBox) sender;

			if (parBox.SelectedItem.ToString() == string.Empty)
				return;

			var model = (ParameterModel) Enum.Parse(typeof(ParameterModel), parBox.SelectedItem.ToString()!);

			// Update parameters
			UpdateParameters(model);

			if (model != ParameterModel.Custom)
			{
				UpdateCustomParameterBoxes();
				CustomParameterBoxes.Disable();
			}
			else
			{
				GetCustomParameters();
				CustomParameterBoxes.Enable();
			}
		}

		private void StrengthBox_OnTextChanged(object sender, TextChangedEventArgs e)
		{
			var fcBox = (TextBox) sender;

			if (_parameters.Model == ParameterModel.Custom || fcBox.Text == string.Empty)
				return;

			_parameters.Strength = Pressure.From(double.Parse(fcBox.Text), _stressUnit);
			UpdateCustomParameterBoxes();
		}

		private void AggTypeBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			var aggBox = (ComboBox) sender;

			if (_parameters.Model == ParameterModel.Custom || aggBox.SelectedItem.ToString() == string.Empty)
				return;

			_parameters.Type = (AggregateType) Enum.Parse(typeof(AggregateType), aggBox.SelectedItem.ToString()!);
			UpdateCustomParameterBoxes();
		}

		private void ConstitutiveBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			var constBox = (ComboBox) sender;

			if (constBox.SelectedItem.ToString() == string.Empty)
				return;

			_constitutiveModel = (ConstitutiveModel) Enum.Parse(typeof(ConstitutiveModel), constBox.SelectedItem.ToString()!);
		}

		private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
		{
			var regex = new Regex("[^0-9.]+");
			e.Handled = regex.IsMatch(e.Text);
		}

		private void ButtonCancel_OnClick(object sender, RoutedEventArgs e) => Close();

		private void ButtonOK_OnClick(object sender, RoutedEventArgs e)
		{
			// Verify if text boxes are filled
			if (!ParametersSet)
			{
				MessageBox.Show("Please set concrete strength and aggregate diameter.", "Alert");
				return;
			}

			var model = (ParameterModel)Enum.Parse(typeof(ParameterModel), ParameterBox.SelectedItem.ToString()!);

			UpdateParameters(model);

			if (model == ParameterModel.Custom)
			{
				if (!CustomParametersSet)
				{
					MessageBox.Show("Please set concrete custom parameters.", "Alert");
					return;
				}

				GetCustomParameters();
			}

			// Save units on database
			DataBase.ConcreteData.Parameters        = _parameters;
			DataBase.ConcreteData.ConstitutiveModel = _constitutiveModel;
			Close();
		}

		#endregion
	}
}