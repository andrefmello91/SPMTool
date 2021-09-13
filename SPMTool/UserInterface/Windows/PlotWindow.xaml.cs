using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
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
		private readonly bool _simulate;

		private int? _crackedPanel;

		private int? _crackedStringer;

		private bool _done;

		private bool _inverted;

		#endregion

		#region Properties

		/// <summary>
		///     Get the displacement axis title.
		/// </summary>
		public string DisplacementTitle => $"Displacement ({_displacementUnit.Abbrev()})";

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

		private double MaxDisplacement
		{
			get => DisplacementAxis.MaxValue;
			set
			{
				if (DisplacementAxis.MaxValue >= value)
					return;

				DisplacementAxis.MaxValue = value;
			}
		}

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

		private double MinDisplacement
		{
			get => DisplacementAxis.MinValue;
			set
			{
				if (DisplacementAxis.MinValue <= value)
					return;

				DisplacementAxis.MinValue = value;
			}
		}

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
			InitiatePlot();

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
			var el = element is Stringer
				? Element.Stringer
				: Element.Panel;

			if (el is Element.Stringer)
				_crackedStringer = element.Number;
			else
				_crackedPanel = element.Number;

			var vals = CartesianChart.Series[(int) el].Values;

			vals.Add(GetPoint(monitoredDisplacement, _displacementUnit));

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
			CartesianChart.Series[0].Values.Add(GetPoint(monitoredDisplacement, _displacementUnit));
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

			if (_crackedStringer.HasValue)
				CartesianChart.Series[1].LabelPoint = point => $"Stringer {_crackedStringer.Value} cracked!\n{Label(point)}";

			if (_crackedPanel.HasValue)
				CartesianChart.Series[2].LabelPoint = point => $"Panel {_crackedPanel.Value} cracked!\n{Label(point)}";
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
		///     Initiate the plot.
		/// </summary>
		private void InitiatePlot()
		{
			// Initiate series
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

			};
		}

		/// <summary>
		///     Get the label of a <paramref name="point" />.
		/// </summary>
		/// <param name="point">The <see cref="ChartPoint" />.</param>
		private string Label(ChartPoint point) =>
			$"LF = {point.Y:0.00}\n" +
			$"u  = {Length.FromMillimeters(Inverted ? -point.X : point.X).ToUnit(_displacementUnit)}";

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
		private void ButtonOK_OnClick(object sender, RoutedEventArgs e)
		{
			Close();
		}

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

			Done = await ExecuteAnalysis();
		}

		#endregion

		/// <summary>
		///     Element auxiliary enumeration.
		/// </summary>
		private enum Element
		{
			Stringer = 1,
			Panel = 2
		}
	}
}