using System;
using andrefmello91.Extensions;

namespace SPMTool.Application
{
	/// <summary>
	///		Display settings class.
	/// </summary>
	public class DisplaySettings : IEquatable<DisplaySettings>
	{
		/// <summary>
		///		The default values for display settings.
		/// </summary>
		public static DisplaySettings Default { get; } = new()
		{
			DisplacementScale = 200,
			NodeScale         = 1,
			ResultScale       = 1,
			TextScale         = 1
		};
		
		/// <summary>
		///		Get/set the magnifier scale factor for the displaced model.
		/// </summary>
		public double DisplacementScale { get; set; }

		/// <summary>
		///		Get/set the scale factor for nodes.
		/// </summary>
		public double NodeScale { get; set; }
		
		/// <summary>
		///		Get/set the scale factor for results. This affects panel's blocks.
		/// </summary>
		public double ResultScale { get; set; }

		/// <summary>
		///		Get/set the scale factor for texts.
		/// </summary>
		public double TextScale { get; set; }

		/// <inheritdoc />
		public bool Equals(DisplaySettings? other) =>
			other is not null && DisplacementScale.Approx(other.DisplacementScale) &&
			NodeScale.Approx(other.NodeScale) && ResultScale.Approx(other.ResultScale) &&
			TextScale.Approx(other.TextScale);

		/// <summary>
		///		<inheritdoc cref="Equals"/>
		/// </summary>
		/// <returns>
		///		True if objects are equal.
		/// </returns>
		public static bool operator ==(DisplaySettings? left, DisplaySettings? right) => left.IsEqualTo(right);

		/// <summary>
		///		<inheritdoc cref="Equals"/>
		/// </summary>
		/// <returns>
		///		True if objects are not equal.
		/// </returns>
		public static bool operator !=(DisplaySettings? left, DisplaySettings? right) => left.IsNotEqualTo(right);
	}
}