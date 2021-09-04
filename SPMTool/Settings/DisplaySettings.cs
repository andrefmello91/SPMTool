using System;
using andrefmello91.Extensions;
using Autodesk.AutoCAD.DatabaseServices;

namespace SPMTool.Application
{
	/// <summary>
	///     Display settings class.
	/// </summary>
	public class DisplaySettings : IEquatable<DisplaySettings>
	{

		#region Fields

		private double _conditionScale;
		private int _displacementMagnifier;

		private double _nodeScale;
		private double _resultScale;
		private double _textScale;

		#endregion

		#region Properties

		/// <summary>
		///     The default values for display settings.
		/// </summary>
		public static DisplaySettings Default { get; } = new()
		{
			NodeScale             = 1,
			ConditionScale        = 1,
			ResultScale           = 1,
			TextScale             = 1,
			DisplacementMagnifier = 200
		};

		/// <summary>
		///     Get/set the scale factor for conditions (forces and supports).
		/// </summary>
		public double ConditionScale
		{
			get => _conditionScale;
			set
			{
				if (_conditionScale.Approx(value, 1E-3))
					return;

				var old = _conditionScale;

				_conditionScale = value;

				ConditionScaleChanged?.Invoke(this, new ScaleChangedEventArgs(old, value));
			}
		}

		/// <summary>
		///     Get/set the magnifier scale factor for the displaced model.
		/// </summary>
		public int DisplacementMagnifier
		{
			get => _displacementMagnifier;
			set
			{
				if (_displacementMagnifier == value)
					return;

				var old = _displacementMagnifier;

				_displacementMagnifier = value;

				DisplacementMagnifierChanged?.Invoke(this, new ScaleChangedEventArgs(old, value));
			}
		}

		/// <summary>
		///     Get/set the scale factor for nodes.
		/// </summary>
		public double NodeScale
		{
			get => _nodeScale;
			set
			{
				if (_nodeScale.Approx(value, 1E-3))
					return;

				var old = _nodeScale;

				_nodeScale = value;

				NodeScaleChanged?.Invoke(this, new ScaleChangedEventArgs(old, value));
			}
		}

		/// <summary>
		///     Get/set the scale factor for results. This affects panel's blocks.
		/// </summary>
		public double ResultScale
		{
			get => _resultScale;
			set
			{
				if (_resultScale.Approx(value, 1E-3))
					return;

				var old = _resultScale;

				_resultScale = value;

				ResultScaleChanged?.Invoke(this, new ScaleChangedEventArgs(old, value));
			}
		}

		/// <summary>
		///     Get/set the scale factor for texts.
		/// </summary>
		public double TextScale
		{
			get => _textScale;
			set
			{
				if (_textScale.Approx(value, 1E-3))
					return;

				var old = _textScale;

				_textScale = value;

				TextScaleChanged?.Invoke(this, new ScaleChangedEventArgs(old, value));
			}
		}

		#endregion

		#region Methods

		#region Interface Implementations

		/// <inheritdoc />
		public bool Equals(DisplaySettings? other) =>
			other is not null && DisplacementMagnifier == other.DisplacementMagnifier &&
			NodeScale.Approx(other.NodeScale) && ResultScale.Approx(other.ResultScale) &&
			TextScale.Approx(other.TextScale);

		#endregion

		#endregion

		#region Operators

		/// <summary>
		///     <inheritdoc cref="Equals" />
		/// </summary>
		/// <returns>
		///     True if objects are equal.
		/// </returns>
		public static bool operator ==(DisplaySettings? left, DisplaySettings? right) => left.IsEqualTo(right);

		/// <inheritdoc cref="SPMTool.Extensions.GetTypedValues(DisplaySettings)" />
		public static explicit operator TypedValue[](DisplaySettings? settings) => settings.GetTypedValues();

		/// <inheritdoc cref="SPMTool.Extensions.GetDisplaySettings" />
		public static explicit operator DisplaySettings?(TypedValue[]? values) => values.GetDisplaySettings();

		/// <summary>
		///     <inheritdoc cref="Equals" />
		/// </summary>
		/// <returns>
		///     True if objects are not equal.
		/// </returns>
		public static bool operator !=(DisplaySettings? left, DisplaySettings? right) => left.IsNotEqualTo(right);

		#endregion

		#region Events

		/// <summary>
		///     Event to run when <see cref="NodeScale" /> changes.
		/// </summary>
		public event EventHandler<ScaleChangedEventArgs>? NodeScaleChanged;

		/// <summary>
		///     Event to run when <see cref="ConditionScale" /> changes.
		/// </summary>
		public event EventHandler<ScaleChangedEventArgs>? ConditionScaleChanged;

		/// <summary>
		///     Event to run when <see cref="ResultScale" /> changes.
		/// </summary>
		public event EventHandler<ScaleChangedEventArgs>? ResultScaleChanged;

		/// <summary>
		///     Event to run when <see cref="TextScale" /> changes.
		/// </summary>
		public event EventHandler<ScaleChangedEventArgs>? TextScaleChanged;

		/// <summary>
		///     Event to run when <see cref="DisplacementMagnifier" /> changes.
		/// </summary>
		public event EventHandler<ScaleChangedEventArgs>? DisplacementMagnifierChanged;

		#endregion

	}

	public class ScaleChangedEventArgs : EventArgs
	{

		#region Properties

		public double NewScale { get; }
		public double OldScale { get; }

		#endregion

		#region Constructors

		public ScaleChangedEventArgs(double oldScale, double newScaleScale)
		{
			OldScale = oldScale;
			NewScale = newScaleScale;
		}

		#endregion

	}
}