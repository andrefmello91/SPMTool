using System.Collections.Generic;
using System.Windows;
using andrefmello91.FEMAnalysis;
using SPMTool.Core;

namespace SPMTool.Application.UserInterface
{
	/// <summary>
	///     Lógica interna para AnalysisConfig.xaml
	/// </summary>
	public partial class AnalysisConfig : BaseWindow
	{

		#region Fields

		/// <summary>
		///     The solver names to show in <see cref="SolverBox" />.
		/// </summary>
		private static readonly List<string> SolverNames = new()
		{
			"Newton-Raphson",
			"Mod. Newton-Raphson",
			"Secant"
		};

		private readonly SPMModel _database;

		#endregion

		#region Properties

		/// <summary>
		///     Get/set settings.
		/// </summary>
		private AnalysisParameters Parameters
		{
			get => AnalysisParameters.Default with
			{
				ForceTolerance = double.Parse(FToleranceBox.Text),
				DisplacementTolerance = double.Parse(DToleranceBox.Text),
				NumberOfSteps = int.Parse(LoadStepsBox.Text),
				MaxIterations = int.Parse(IterationsBox.Text),
				Solver = (NonLinearSolver) SolverBox.SelectedIndex
			};

			set
			{
				FToleranceBox.Text      = $"{value.ForceTolerance:G2}";
				DToleranceBox.Text      = $"{value.DisplacementTolerance:G2}";
				LoadStepsBox.Text       = $"{value.NumberOfSteps}";
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
			_database  = SPMModel.ActiveModel;
			Parameters = _database.Settings.Analysis;
		}

		#endregion

		#region Methods

		/// <summary>
		///     Set default analysis settings.
		/// </summary>
		private void ButtonDefault_OnClick(object sender, RoutedEventArgs e) => Parameters = AnalysisParameters.Default;

		/// <summary>
		///     Save units if OK button is clicked.
		/// </summary>
		private void ButtonOK_OnClick(object sender, RoutedEventArgs e)
		{
			// Check if parameters parse
			if (!CheckBoxes(FToleranceBox, DToleranceBox, LoadStepsBox, IterationsBox))
			{
				MessageBox.Show("Please set positive and non zero values.");
				return;
			}

			if (double.Parse(FToleranceBox.Text) >= 1 || double.Parse(DToleranceBox.Text) >= 1)
			{
				MessageBox.Show("Please set a tolerance smaller than 1.");
				return;
			}

			if (int.Parse(IterationsBox.Text) < 1000)
			{
				MessageBox.Show("Please set at least 1000 for maximum iterations.");
				return;
			}

			// Save units on database
			_database.Settings.Analysis = Parameters;

			Close();
		}

		#endregion

	}
}