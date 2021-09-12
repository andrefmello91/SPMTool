using System;
using System.Collections.Generic;
using System.Collections.Specialized;
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

		private readonly int _monitoredIndex;

		private readonly bool _simulate;

		private int? _crackedPanel;

		private int? _crackedStringer;

		private bool _done;

		private bool _inverted;

		private IProgress<MonitoredDisplacement> _progress;

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

		#endregion

		#region Constructors

		/// <summary>
		///     <see cref="PlotWindow" /> constructor.
		/// </summary>
		/// <param name="analysis">The <see cref="Analysis" />, before initiating analysis.</param>
		public PlotWindow(SPMAnalysis analysis, int monitoredIndex, bool simulate)
		{
			_monitoredIndex = monitoredIndex;
			InitializeComponent();

			_displacementUnit = SPMModel.ActiveModel.Settings.Units.Displacements;
			Analysis          = analysis;
			_simulate         = simulate;
			AddEvents(Analysis);
			InitiatePlot();

			ContentRendered += On_WindowShown;

			ButtonExport.IsEnabled                            =  false;
			ButtonOk.IsEnabled                                =  false;
			Status.Text                                       =  "Running Analysis...";
			CartesianChart.Series[0].Values.CollectionChanged += On_CollectionChanged;

			DataContext = this;
		}

		#endregion

		#region Methods

		private static ObservablePoint GetPoint(MonitoredDisplacement monitoredDisplacement, LengthUnit unit) => new(monitoredDisplacement.Displacement.As(unit), monitoredDisplacement.LoadFactor);

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

		private void AddEvents(SPMAnalysis analysis)
		{
			analysis.StepConverged  += On_StepConverged;
			analysis.ElementCracked += On_ElementCracked;

			// analysis.AnalysisAborted  += On_AnalysisComplete;
			// analysis.AnalysisComplete += On_AnalysisComplete;
		}

		private async Task AddPoint(MonitoredDisplacement monitoredDisplacement)
		{
			var mds  = _monitoredDisplacements;
			var vals = CartesianChart.Series[0].Values;

			mds.Add(monitoredDisplacement);
			vals.Add(GetPoint(monitoredDisplacement, _displacementUnit));

			await Task.Delay(TimeSpan.FromMilliseconds(10), CancellationToken.None);
		}

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

		private void InitiatePlot()
		{
			// Set max load factor
			LoadFactorAxis.MaxValue = 1;

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
					Title             = "First stringer crack",
					Values            = new ChartValues<ObservablePoint>(),
					PointGeometry     = DefaultGeometries.Circle,
					PointGeometrySize = 15,
					PointForeground   = new SolidColorBrush((Color) ColorConverter.ConvertFromString("#282c34")),
					StrokeThickness   = 3,
					Stroke            = Brushes.Aqua,
					Fill              = Brushes.Transparent,
					DataLabels        = false,
				},
				new LineSeries
				{
					Title             = "First panel crack",
					Values            = new ChartValues<ObservablePoint>(),
					PointGeometry     = DefaultGeometries.Circle,
					PointGeometrySize = 15,
					PointForeground   = new SolidColorBrush((Color) ColorConverter.ConvertFromString("#282c34")),
					StrokeThickness   = 3,
					Stroke            = Brushes.Gray,
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

		private void ButtonOK_OnClick(object sender, RoutedEventArgs e)
		{
			Close();
		}

		private void On_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			var mds = _monitoredDisplacements;

			Inverted = mds.Select(md => md.Displacement).Max() <= Length.Zero;
		}

		private void On_ElementCracked(object sender, SPMElementEventArgs e)
		{
			var step = e.LoadStep!.Value;

			var md = Analysis[step - 1].MonitoredDisplacement!.Value;

			Task.Run(() => AddCrackPoint(md, e.Element));
		}

		private void On_StepConverged(object sender, StepEventArgs e)
		{
			if (!e.Step.MonitoredDisplacement.HasValue)
				return;

			var md = e.Step.MonitoredDisplacement.Value;

			Task.Run(() => AddPoint(md));
		}

		private async void On_WindowShown(object sender, EventArgs e)
		{
			if (Done)
				return;

			Done = await Task.Run(() =>
			{
				Analysis.Execute(_monitoredIndex, _simulate);
				return true;
			});

			// // AddEvents(Analysis);
			//
			// var task = new Task(() =>
			// {
			// 	Analysis.Execute(_monitoredIndex, _simulate);
			// });
			//
			// task.Start();
			//
			// // Task.Run(async () => await UpdatePlot());
			//
			// // Analysis.Execute(_monitoredIndex, _simulate);
			//
			// task.Wait();
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