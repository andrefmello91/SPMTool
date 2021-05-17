using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using andrefmello91.Extensions;
using andrefmello91.FEMAnalysis;
using SPMTool.Core;

namespace SPMTool.Application.UserInterface
{
	/// <summary>
	///     Lógica interna para AnalysisConfig.xaml
	/// </summary>
	public partial class AnalysisConfig : BaseWindow
	{
		/// <summary>
		///		The solver names to show in <see cref="SolverBox"/>.
		/// </summary>
		private static readonly List<string> SolverNames = new()
		{
			"Newton-Raphson",
			"Mod. Newton-Raphson",
			"Secant"
		};
		
		#region Properties

		/// <summary>
		///     Get/set settings.
		/// </summary>
		private AnalysisSettings AnalysisSettings
		{
			get => new()
			{
				Tolerance     = double.Parse(ToleranceBox.Text),
				NumLoadSteps  = int.Parse(LoadStepsBox.Text),
				MaxIterations = int.Parse(IterationsBox.Text),
				Solver        = (NonLinearSolver) SolverBox.SelectedIndex
			};

			set
			{
				ToleranceBox.Text       = $"{value.Tolerance:G}";
				LoadStepsBox.Text       = $"{value.NumLoadSteps}";
				IterationsBox.Text      = $"{value.MaxIterations}";
				SolverBox.SelectedIndex = (int) value.Solver;
			}
		}

		#endregion

		#region Constructors

		public AnalysisConfig()
		{
			InitializeComponent();

			// Set solver options
			SolverBox.ItemsSource = SolverNames;

			// Read saved settings
			AnalysisSettings = SPMDatabase.Settings.Analysis;
		}

		#endregion

		#region Methods

		/// <summary>
		///     Set default analysis settings.
		/// </summary>
		private void ButtonDefault_OnClick(object sender, RoutedEventArgs e) => AnalysisSettings = AnalysisSettings.Default;

		/// <summary>
		///     Save units if OK button is clicked.
		/// </summary>
		private void ButtonOK_OnClick(object sender, RoutedEventArgs e)
		{
			// Check if parameters parse
			if (!CheckBoxes(ToleranceBox, LoadStepsBox, IterationsBox))
			{
				MessageBox.Show("Please set positive and non zero values.");
				return;
			}

			// Save units on database
			SPMDatabase.Settings.Analysis = AnalysisSettings;

			Close();
		}

		#endregion

	}
}