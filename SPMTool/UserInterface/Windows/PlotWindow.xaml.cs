using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using andrefmello91.Extensions;
using andrefmello91.FEMAnalysis;
using andrefmello91.SPMElements;
using LiveCharts;
using LiveCharts.Configurations;
using LiveCharts.Defaults;
using LiveCharts.Wpf;
using SPMTool.Core;
using UnitsNet;
using UnitsNet.Units;

namespace SPMTool.Application.UserInterface
{
	/// <summary>
	///     Lógica interna para GraphWindow.xaml
	/// </summary>
	public partial class PlotWindow : Window
	{

		#region Fields

		/// <summary>
		///     The <see cref="LengthUnit" /> of displacements.
		/// </summary>
		private readonly LengthUnit _displacementUnit;

		private readonly List<MonitoredDisplacement> _monitoredDisplacements = new(new[] { new MonitoredDisplacement(Length.Zero, 0) });

		private readonly List<(string name, ObservablePoint point)> _panelCracks = new();

		private readonly bool _simulate;

		private readonly List<(string name, ObservablePoint point)> _stringerCracks = new();

		private bool _done;

		private bool _inverted;

		private bool _viewCracks;

		#endregion

		#region Properties

		/// <summary>
		///     Get the displacement axis title.
		/// </summary>
		public string DisplacementTitle => $"Displacement ({_displacementUnit.Abbrev()})";

		public bool ViewCracks
		{
			get => _viewCracks;
			set
			{
				_viewCracks = value;

				StringerCracks.Visibility = PanelCracks.Visibility = value
					? Visibility.Visible
					: Visibility.Hidden;
			}
		}

		/// <summary>
		///     The <see cref="SPMOutput" />'s.
		/// </summary>
		private SPMAnalysis Analysis { get; }

		private bool Done
		{
			get => _done;
			set
			{
				_done = value;

				if (value)
					AnalysisOk();
			}
		}

		/// <summary>
		///     Get/set inverted displacement axis state.
		/// </summary>
		private bool Inverted
		{
			get => _inverted;
			set
			{
				if (_inverted == value)
					return;

				_inverted = value;
				SetMapper(value);
			}
		}

		/// <summary>
		///     Get the label of a chart point.
		/// </summary>
		private Func<ChartPoint, string> Label => point =>
			$"LF = {point.Y:0.00}\n" +
			$"u  = {Length.FromMillimeters(Inverted ? -point.X : point.X).ToUnit(_displacementUnit)}";

		private double MaxLoadFactor
		{
			get => LoadFactorAxis.MaxValue;
			set
			{
				if (LoadFactorAxis.MaxValue >= value)
					return;

				LoadFactorAxis.MaxValue = value;
			}
		}

		private Func<ChartPoint, string> PanelCrackLabel => point =>
		{
			if (!Done || !_panelCracks.Any())
				return Label(point);

			var name = _panelCracks.First(p => (Inverted ? -p.point.X : p.point.X).Approx(point.X, 1E-6) && p.point.Y.Approx(point.Y, 1E-6)).name;

			return
				$"{name} cracked!\n" +
				$"{Label(point)}";
		};

		private Func<ChartPoint, string> StringerCrackLabel => point =>
		{
			if (!Done || !_stringerCracks.Any())
				return Label(point);

			var name = _stringerCracks.First(s => (Inverted ? -s.point.X : s.point.X).Approx(point.X, 1E-6) && s.point.Y.Approx(point.Y, 1E-6)).name;

			return
				$"{name} cracked!\n" +
				$"{Label(point)}";
		};

		#endregion

		#region Constructors

		/// <summary>
		///     <see cref="PlotWindow" /> constructor.
		/// </summary>
		/// <param name="analysis">The <see cref="Analysis" />, before initiating analysis.</param>
		public PlotWindow(SPMAnalysis analysis, bool simulate)
		{
			_simulate = simulate;
			InitializeComponent();

			_displacementUnit = SPMModel.ActiveModel.Settings.Units.Displacements;
			Analysis          = analysis;
			AddEvents(Analysis);

			ContentRendered += On_WindowShown;

			DataContext = this;
		}

		#endregion

		#region Methods

		/// <summary>
		///     Get an <see cref="ObservablePoint" /> from a <see cref="MonitoredDisplacement" />.
		/// </summary>
		private static ObservablePoint GetPoint(MonitoredDisplacement monitoredDisplacement, LengthUnit unit) => new(monitoredDisplacement.Displacement.As(unit), monitoredDisplacement.LoadFactor);

		/// <summary>
		///     Add the point of element's cracking.
		/// </summary>
		private async Task AddCrackPoint(MonitoredDisplacement monitoredDisplacement, INumberedElement element)
		{
			if (element is not INonlinearSPMElement nonlinearSpmElement)
				return;

			var pt = GetPoint(monitoredDisplacement, _displacementUnit);

			var (list, vals) = element is Stringer
				? (_stringerCracks, StringerCracks.Values)
				: (_panelCracks, PanelCracks.Values);

			list.Add((nonlinearSpmElement.Name, pt));
			vals.Add(pt);

			await Task.Delay(TimeSpan.FromMilliseconds(10), CancellationToken.None);
		}

		/// <summary>
		///     Add events to analysis.
		/// </summary>
		private void AddEvents(SPMAnalysis analysis)
		{
			analysis.ElementCracked += On_ElementCracked;
		}

		/// <summary>
		///     Add the monitored point to plot.
		/// </summary>
		private void AddPoint(MonitoredDisplacement monitoredDisplacement)
		{
			_monitoredDisplacements.Add(monitoredDisplacement);
			Plot.Values.Add(GetPoint(monitoredDisplacement, _displacementUnit));
		}

		/// <summary>
		///     Execute when analysis is done.
		/// </summary>
		private void AnalysisOk()
		{
			Status.Text = Analysis.Stop
				? Analysis.StopMessage
				: "Analysis done!";

			ButtonExport.IsEnabled = true;
			ButtonOk.IsEnabled     = true;
		}

		/// <summary>
		///     Initiate the plot.
		/// </summary>
		private void ConfigurePlot()
		{
			// Configure main plot
			Plot.Values        = new ChartValues<ObservablePoint>(new[] { new ObservablePoint(0, 0) });
			Plot.PointGeometry = null;
			Plot.LabelPoint    = Label;

			// Configure stringer and panel cracks
			StringerCracks.Values        = new ChartValues<ObservablePoint>();
			PanelCracks.Values           = new ChartValues<ObservablePoint>();
			StringerCracks.PointGeometry = PanelCracks.PointGeometry = DefaultGeometries.Circle;
			StringerCracks.LabelPoint    = StringerCrackLabel;
			PanelCracks.LabelPoint       = PanelCrackLabel;

			/*// Initiate series
			CartesianChart.Series = new SeriesCollection
			{
				// Full chart
				new LineSeries
				{
					Title           = "Load Factor x Displacement",
					Values          = new ChartValues<ObservablePoint>(new[] { new ObservablePoint(0, 0) }),
					PointGeometry   = null,
					StrokeThickness = 3,
					Stroke          = Brushes.LightSkyBlue,
					Fill            = Brushes.Transparent,
					DataLabels      = false,
					LabelPoint      = Label
				},
				new LineSeries
				{
					Title             = "Stringer cracking",
					Values            = new ChartValues<ObservablePoint>(),
					PointGeometry     = DefaultGeometries.Circle,
					PointGeometrySize = 15,
					PointForeground   = Brushes.Aqua,
					StrokeThickness   = 3,
					Stroke            = Brushes.Transparent,
					Fill              = Brushes.Transparent,
					DataLabels        = false,
				},
				new LineSeries
				{
					Title             = "Panel cracking",
					Values            = new ChartValues<ObservablePoint>(),
					PointGeometry     = DefaultGeometries.Circle,
					PointGeometrySize = 15,
					PointForeground   = Brushes.Gray,
					StrokeThickness   = 3,
					Stroke            = Brushes.Transparent,
					Fill              = Brushes.Transparent,
					DataLabels        = false
				}

			};*/
		}

		/// <summary>
		///     Execute the analysis asynchronously.
		/// </summary>
		private async Task<bool> ExecuteAnalysis()
		{
			// Analysis by steps
			while (true)
			{
				var md = await Task.Run(() =>
				{
					Analysis.ExecuteStep();
					return Analysis.CurrentStep.MonitoredDisplacement;
				});

				if (md.HasValue)
				{
					AddPoint(md.Value);
					UpdatePlot(md.Value);
				}

				if (Analysis.Stop || !_simulate && Analysis.CurrentStep >= Analysis.Parameters.NumberOfSteps)
					break;
			}

			return true;
		}

		/// <summary>
		///     Set mapper for inverting X axis.
		/// </summary>
		private void SetMapper(bool inverted)
		{
			// Invert x values
			foreach (var series in CartesianChart.Series)
				series.Configuration = Mappers.Xy<ObservablePoint>()
					.X(point => inverted ? -point.X : point.X)
					.Y(point => point.Y);

			// Correct the labels
			DisplacementAxis.LabelFormatter = x => $"{(inverted ? -x : x)}";
		}

		/// <summary>
		///     Update plot after adding a monitored displacement.
		/// </summary>
		private void UpdatePlot(MonitoredDisplacement monitoredDisplacement)
		{
			// Set inversion
			Inverted = _monitoredDisplacements
				.Select(md => md.Displacement)
				.Max() <= Length.Zero;

			var lf = monitoredDisplacement.LoadFactor;

			// Update maximum load factor 
			while (!lf.Approx(MaxLoadFactor, 1E-3) && lf > MaxLoadFactor)
				MaxLoadFactor += 0.2;
		}

		/// <summary>
		///     Execute when <see cref="ButtonExport" /> is clicked.
		/// </summary>
		private void ButtonExport_OnClick(object sender, RoutedEventArgs e)
		{
			// Get location and name
			string
				path = Path.GetDirectoryName(SPMModel.ActiveModel.Name)!,
				name = $"{Path.GetFileNameWithoutExtension(SPMModel.ActiveModel.Name)}_SPMResult";

			// Export
			var output = Analysis.GenerateOutput();
			output.Export(path, name, _displacementUnit);
			MessageBox.Show("Data exported to file location.");
		}

		/// <summary>
		///     Execute when <see cref="ButtonOk" /> is clicked.
		/// </summary>
		private void ButtonOK_OnClick(object sender, RoutedEventArgs e) => Close();

		/// <summary>
		///     Execute when an element cracks.
		/// </summary>
		private void On_ElementCracked(object sender, SPMElementEventArgs e)
		{
			var step = e.LoadStep!.Value;

			var md = Analysis[step - 1].MonitoredDisplacement!.Value;

			Task.Run(() => AddCrackPoint(md, e.Element));
		}

		/// <summary>
		///     Execute when the window is rendered.
		/// </summary>
		private async void On_WindowShown(object sender, EventArgs e)
		{
			if (Done)
				return;

			ConfigurePlot();

			Done = await ExecuteAnalysis();
		}

		#endregion

	}
}