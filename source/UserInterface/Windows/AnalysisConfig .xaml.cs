using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using Extensions.Number;
using static SPMTool.ApplicationSettings.Settings;
using MessageBox = System.Windows.MessageBox;

namespace SPMTool.UserInterface
{
	/// <summary>
	/// Lógica interna para AnalysisConfig.xaml
	/// </summary>
	public partial class AnalysisConfig : Window
    {
		// Properties
		private AnalysisSettings _settings;
		
        public AnalysisConfig()
			: this (Read())
        {
        }

        public AnalysisConfig(AnalysisSettings settings)
        {
	        InitializeComponent();

            // Read units
            _settings = settings;

			// Initiate combo boxes with units set
			InitiateComboBoxes();
        }

        /// <summary>
        /// Get combo boxes items.
        /// </summary>
        private void InitiateComboBoxes()
        {
	        ToleranceBox.Text  = $"{_settings.Tolerance:G}";
	        LoadStepsBox.Text  = $"{_settings.NumLoadSteps}";
	        IterationsBox.Text = $"{_settings.MaxIterations}";
        }

        private void IntValidationTextBox(object sender, TextCompositionEventArgs e)
        {
	        var regex = new Regex("[^0-9]+");
	        e.Handled = regex.IsMatch(e.Text);
        }

        private void DoubleValidationTextBox(object sender, TextCompositionEventArgs e)
        {
	        var regex = new Regex("[^0-9.]+e");
	        e.Handled = regex.IsMatch(e.Text);
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
			var boxes = new [] {ToleranceBox, LoadStepsBox, IterationsBox};

			// Check if parameters parse
			if (!boxes.All(d => d.Text.ParsedAndNotZero(out _)))
			{
				MessageBox.Show("Please set valid parameters.");
				return;
			}

			// Save units on database
			Save(new AnalysisSettings
			{
				Tolerance     = double.Parse(ToleranceBox.Text),
				NumLoadSteps  = int.Parse(LoadStepsBox.Text),
				MaxIterations = int.Parse(IterationsBox.Text),
			});

			Close();
        }

		/// <summary>
        /// Set default analysis settings.
        /// </summary>
		private void ButtonDefault_OnClick(object sender, RoutedEventArgs e)
		{
			_settings = AnalysisSettings.Default;
			InitiateComboBoxes();
		}
    }
}
