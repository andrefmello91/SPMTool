using System;
using Extensions.Number;
using UnitsNet.Units;

namespace SPMTool
{
	/// <summary>
    /// Analysis settings class.
    /// </summary>
	public class AnalysisSettings : IEquatable<AnalysisSettings>
	{
		/// <summary>
		/// Get/set the convergence tolerance
		/// </summary>
		public double Tolerance { get; set; }

		/// <summary>
		/// Get/set the number of load steps.
		/// </summary>
		public int NumLoadSteps { get; set; }

		/// <summary>
		/// Get/set the maximum number of iterations.
		/// </summary>
		public int MaxIterations { get; set; }

		/// <summary>
		/// Returns true if this <see cref="AnalysisSettings"/> has the default values.
		/// </summary>
		public bool IsDefault => Equals(Default);

        /// <summary>
        /// Default <see cref="AnalysisSettings"/> object.
        /// </summary>
        public static readonly AnalysisSettings Default = new AnalysisSettings
		{
			Tolerance     = 1E-6,
			NumLoadSteps  = 50,
			MaxIterations = 10000
		};

        /// <summary>
        /// Returns true if all parameters coincide.
        /// </summary>
        /// <param name="other">The other <see cref="AnalysisSettings"/> object.</param>
        public bool Equals(AnalysisSettings other) => Tolerance.Approx(other.Tolerance) && NumLoadSteps == other.NumLoadSteps && MaxIterations == other.MaxIterations;

		public override bool Equals(object obj) => obj is AnalysisSettings settings && Equals(settings);

		public override int GetHashCode() => base.GetHashCode();

		/// <summary>
		/// Returns true if all units coincide.
		/// </summary>
		public static bool operator == (AnalysisSettings left, AnalysisSettings right) => !(left is null) && left.Equals(right);

		/// <summary>
		/// Returns true if at least a unit do not coincide.
		/// </summary>
		public static bool operator != (AnalysisSettings left, AnalysisSettings right) => !(left is null) && !left.Equals(right);
    }
}
