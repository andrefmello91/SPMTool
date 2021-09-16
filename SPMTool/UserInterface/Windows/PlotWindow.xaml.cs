using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
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
using SPMTool.Annotations;
using SPMTool.Core;
using UnitsNet;
using UnitsNet.Units;

namespace SPMTool.Application.UserInterface
{
	/// <summary>
	///     Lógica interna para GraphWindow.xaml
	/// </summary>
	public partial class PlotWindow : INotifyPropertyChanged
	{

		#region Fields

		private readonly List<(string label, ObservablePoint point)>
			_crackLabels = new(),
			_yieldLabels = new(),
			_crushLabels = new();

		/// <summary>
		///     The <see cref="LengthUnit" /> of displacements.
		/// </summary>
		private readonly LengthUnit _displacementUnit;

		private readonly List<MonitoredDisplacement> _monitoredDisplacements = new(new[] { new MonitoredDisplacement(Length.Zero, 0) });

		private readonly bool _simulate;

		private bool _done, _inverted, _showCracks, _showCrushing, _showYielding;

		#endregion

		#region Properties

		/// <summary>
		///     Get the displacement axis title.
		/// </summary>
		public string DisplacementTitle => $"Displacement ({_displacementUnit.Abbrev()})";

		public bool Done
		{
			get => _done;
			set
			{
				_done = value;

				if (value)
					AnalysisOk();

				OnPropertyChanged();
			}
		}

		public bool ShowCracks
		{
			get => _showCracks;
			set
			{
				_showCracks = value;

				if (value)
					ShowCrushing = ShowYielding = false;

				OnPropertyChanged();
			}
		}

		public bool ShowCrushing
		{
			get => _showCrushing;
			set
			{
				_showCrushing = value;

				if (value)
					ShowCracks = ShowYielding = false;

				OnPropertyChanged();
			}
		}

		public bool ShowYielding
		{
			get => _showYielding;
			set
			{
				_showYielding = value;

				if (value)
					ShowCracks = ShowCrushing = false;

				OnPropertyChanged();
			}
		}

		/// <summary>
		///     The <see cref="SPMOutput" />'s.
		/// </summary>
		private SPMAnalysis Analysis { get; }

		private Func<ChartPoint, string> CrackLabel => point =>
		{
			if (!_crackLabels.Any())
				return Label(point);

			var label = _crackLabels.First(p => (Inverted ? -p.point.X : p.point.X).Approx(point.X, 1E-6) && p.point.Y.Approx(point.Y, 1E-6)).label;

			return
				(label.IsNullOrEmpty() ? string.Empty : $"{label}\n") +
				$"{Label(point)}";
		};

		private Func<ChartPoint, string> CrushLabel => point =>
		{
			if (!_crushLabels.Any())
				return Label(point);

			var label = _crushLabels.First(p => (Inverted ? -p.point.X : p.point.X).Approx(point.X, 1E-6) && p.point.Y.Approx(point.Y, 1E-6)).label;

			return
				(label.IsNullOrEmpty() ? string.Empty : $"{label}\n") +
				$"{Label(point)}";
		};

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

		private Func<ChartPoint, string> YieldLabel => point =>
		{
			if (!_yieldLabels.Any())
				return Label(point);

			var label = _yieldLabels.First(p => (Inverted ? -p.point.X : p.point.X).Approx(point.X, 1E-6) && p.point.Y.Approx(point.Y, 1E-6)).label;

			return
				(label.IsNullOrEmpty() ? string.Empty : $"{label}\n") +
				$"{Label(point)}";
		};

		#endregion

		#region Events

		public event PropertyChangedEventHandler? PropertyChanged;

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
		///     Get the label for a collection of SPM elements.
		/// </summary>
		private static string GetLabel(IEnumerable<ISPMElement> elements, ElementPlot elementPlot)
		{
			var append = elementPlot switch
			{
				ElementPlot.Cracking => ": Concrete cracked!",
				ElementPlot.Crushing => ": Concrete crushed!",
				_                    => null
			};

			switch (elements.Count())
			{
				case 0:
					return string.Empty;

				case 1:
					var element = (INonlinearSPMElement) elements.First()!;

					append ??= element.ConcreteYielded
						? ": Concrete yielded!"
						: ": Steel yielded!";

					return elements.First().Name + append;

				default:
					var stringers = elements
						.Where(e => e is Stringer and INonlinearSPMElement)
						.Cast<INonlinearSPMElement>()
						.ToList();

					var panels = elements
						.Where(e => e is Panel and INonlinearSPMElement)
						.Cast<INonlinearSPMElement>()
						.ToList();

					var sLabel = stringers.Any() switch
					{
						true when append is not null && stringers.Count == 1 => stringers[0].Name + append,
						true when append is not null                         => stringers.Aggregate("Stringers", (s, element) => $"{s} {element.Number},").Trim(',') + append,
						true when append is null                             => GetYieldLabel(stringers),
						_                                                    => null
					};

					var pLabel = panels.Any() switch
					{
						true when append is not null && panels.Count == 1 => panels[0].Name + append,
						true when append is not null                      => panels.Aggregate("Panels", (s, element) => $"{s} {element.Number},").Trim(',') + append,
						true when append is null                          => GetYieldLabel(panels),
						_                                                 => null
					};

					// Remove ","
					return
						(sLabel ?? string.Empty) + (pLabel is null ? string.Empty : $"\n{pLabel}");
			}
		}

		/// <summary>
		///     Get an <see cref="ObservablePoint" /> from a <see cref="MonitoredDisplacement" />.
		/// </summary>
		private static ObservablePoint GetPoint(MonitoredDisplacement monitoredDisplacement, LengthUnit unit) => new(monitoredDisplacement.Displacement.As(unit), monitoredDisplacement.LoadFactor);

		/// <summary>
		///     Get the label for yielded elements.
		/// </summary>
		private static string GetYieldLabel<TNonlinearSPMElement>(IEnumerable<TNonlinearSPMElement> elements)
			where TNonlinearSPMElement : INonlinearSPMElement
		{
			switch (elements.Count())
			{
				case 0:
					return string.Empty;

				case 1:
					var element = elements.First()!;

					var append = element.ConcreteYielded
						? ": Concrete yielded!"
						: ": Steel yielded!";

					return elements.First().Name + append;

				default:
					var prepend = elements.First() is Stringer
						? "Stringers"
						: "Panels";

					var cYield = elements
						.Where(e => e.ConcreteYielded)
						.ToList();

					var sYield = elements
						.Where(e => e.SteelYielded)
						.ToList();

					var label = string.Empty;

					if (cYield.Any())
						label += $"{cYield.Aggregate(prepend, (s, element) => $"{s} {element.Number},").Trim(',')}: Concrete yielded!";

					if (sYield.Any())
						label += (label == string.Empty ? string.Empty : "\n") +
						         $"{sYield.Aggregate(prepend, (s, element) => $"{s} {element.Number},").Trim(',')}: Steel yielded!";

					return label;
			}
		}

		[NotifyPropertyChangedInvocator]
		protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		/// <summary>
		///     Add events to analysis.
		/// </summary>
		private void AddEvents(SPMAnalysis analysis)
		{
			analysis.ElementsCracked += OnElementsCracked;
			analysis.ElementsYielded += OnElementsYielded;
			analysis.ElementsCrushed += OnElementsCrushed;
		}

		/// <summary>
		///     Add the monitored point to plot.
		/// </summary>
		private void AddPoint(MonitoredDisplacement monitoredDisplacement)
		{
			_monitoredDisplacements.Add(monitoredDisplacement);
			Plot.Values.Add(GetPoint(monitoredDisplacement, _displacementUnit));
		}

		// /// <summary>
		// ///     Add the point of element's cracking.
		// /// </summary>
		// private async Task AddCrackPoint(MonitoredDisplacement monitoredDisplacement, IEnumerable<ISPMElement> elements)
		// {
		// 	var pt = GetPoint(monitoredDisplacement, _displacementUnit);
		//
		// 	var stringers = elements
		// 		.Where(e => e is Stringer)
		// 		.ToList();
		//
		// 	var panels = elements
		// 		.Where(e => e is Panel)
		// 		.ToList();
		//
		// 	var label = GetLabel(stringers, " cracked!") ?? string.Empty;
		//
		// 	var pLabel = GetLabel(panels, " cracked!");
		//
		// 	if (pLabel is not null)
		// 		label += (label == string.Empty ? label : "\n") + pLabel;
		//
		// 	_crackLabels.Add((label, pt));
		// 	CrackingPlot.Values.Add(pt);
		//
		// 	await Task.Delay(TimeSpan.FromMilliseconds(10), CancellationToken.None);
		// }

		/// <summary>
		///     Add the point of element's cracking, yielding or crushing.
		/// </summary>
		private async Task AddPoints(MonitoredDisplacement monitoredDisplacement, IEnumerable<ISPMElement> elements, ElementPlot elementPlot)
		{
			var pt = GetPoint(monitoredDisplacement, _displacementUnit);

			var (plot, labels) = elementPlot switch
			{
				ElementPlot.Cracking => (CrackingPlot, _crackLabels),
				ElementPlot.Yielding => (YieldingPlot, _yieldLabels),
				_                    => (CrushingPlot, _crushLabels)
			};

			var label = GetLabel(elements, elementPlot);

			labels.Add((label, pt));
			plot.Values.Add(pt);

			await Task.Delay(TimeSpan.FromMilliseconds(10), CancellationToken.None);
		}

		/// <summary>
		///     Execute when analysis is done.
		/// </summary>
		private void AnalysisOk()
		{
			Status.Text = Analysis.Stop
				? Analysis.StopMessage
				: "Analysis done!";
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

			// Configure crack plot
			CrackingPlot.Values        = new ChartValues<ObservablePoint>();
			CrackingPlot.PointGeometry = DefaultGeometries.Circle;
			CrackingPlot.LabelPoint    = CrackLabel;

			// Configure yield plot
			YieldingPlot.Values        = new ChartValues<ObservablePoint>();
			YieldingPlot.PointGeometry = DefaultGeometries.Circle;
			YieldingPlot.LabelPoint    = YieldLabel;

			// Configure crush plot
			CrushingPlot.Values        = new ChartValues<ObservablePoint>();
			CrushingPlot.PointGeometry = DefaultGeometries.Circle;
			CrushingPlot.LabelPoint    = CrushLabel;
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
		///     Execute when the window is rendered.
		/// </summary>
		private async void On_WindowShown(object sender, EventArgs e)
		{
			if (Done)
				return;

			ConfigurePlot();

			Done = await ExecuteAnalysis();
		}

		/// <summary>
		///     Execute when an element cracks.
		/// </summary>
		private async void OnElementsCracked(object sender, SPMElementEventArgs e)
		{
			var step = e.LoadStep!.Value;

			var md = Analysis[step - 1].MonitoredDisplacement!.Value;

			await AddPoints(md, e.Elements, ElementPlot.Cracking);
		}

		/// <summary>
		///     Execute when an element crushes.
		/// </summary>
		private async void OnElementsCrushed(object sender, SPMElementEventArgs e)
		{
			var step = e.LoadStep!.Value;

			var md = Analysis[step - 1].MonitoredDisplacement!.Value;

			await AddPoints(md, e.Elements, ElementPlot.Crushing);
		}

		/// <summary>
		///     Execute when an element yields.
		/// </summary>
		private async void OnElementsYielded(object sender, SPMElementEventArgs e)
		{
			var step = e.LoadStep!.Value;

			var md = Analysis[step - 1].MonitoredDisplacement!.Value;

			await AddPoints(md, e.Elements, ElementPlot.Yielding);
		}

		#endregion

		private enum ElementPlot
		{
			Cracking,
			Yielding,
			Crushing
		}
	}
}