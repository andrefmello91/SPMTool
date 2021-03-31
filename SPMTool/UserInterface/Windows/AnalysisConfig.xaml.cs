﻿using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using andrefmello91.Extensions;
using SPMTool.Core;

namespace SPMTool.Application.UserInterface
{
	/// <summary>
	///     Lógica interna para AnalysisConfig.xaml
	/// </summary>
	public partial class AnalysisConfig : Window
	{
		#region Properties

		/// <summary>
		///     Get/set settings.
		/// </summary>
		private AnalysisSettings AnalysisSettings
		{
			get => new AnalysisSettings
			{
				Tolerance     = double.Parse(ToleranceBox.Text),
				NumLoadSteps  = int.Parse(LoadStepsBox.Text),
				MaxIterations = int.Parse(IterationsBox.Text)
			};

			set
			{
				ToleranceBox.Text  = $"{value.Tolerance:G}";
				LoadStepsBox.Text  = $"{value.NumLoadSteps}";
				IterationsBox.Text = $"{value.MaxIterations}";
			}
		}

		#endregion

		#region Constructors

		public AnalysisConfig()
		{
			InitializeComponent();

			// Read units
			AnalysisSettings = DataBase.Settings.Analysis;
		}

		#endregion

		#region  Methods

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
		///     Close window if cancel button is clicked.
		/// </summary>
		private void ButtonCancel_OnClick(object sender, RoutedEventArgs e) => Close();

		/// <summary>
		///     Save units if OK button is clicked.
		/// </summary>
		private void ButtonOK_OnClick(object sender, RoutedEventArgs e)
		{
			// Check if tolerance is positive
			if (double.TryParse(ToleranceBox.Text, out var t) && t <= 0)
			{
				MessageBox.Show("Please set tolerance greater than zero.");
				return;
			}

			var boxes = new [] {ToleranceBox, LoadStepsBox, IterationsBox};

			// Check if parameters parse
			if (!boxes.All(d => d.Text.ParsedAndNotZero(out _)))
			{
				MessageBox.Show("Please set valid parameters.");
				return;
			}

			// Save units on database
			DataBase.Settings.Analysis = AnalysisSettings;

			Close();
		}

		/// <summary>
		///     Set default analysis settings.
		/// </summary>
		private void ButtonDefault_OnClick(object sender, RoutedEventArgs e) => AnalysisSettings = AnalysisSettings.Default;

		#endregion
	}
}