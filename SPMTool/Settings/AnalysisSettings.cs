using System;
using andrefmello91.Extensions;
using andrefmello91.FEMAnalysis;
using Autodesk.AutoCAD.DatabaseServices;

namespace SPMTool.Application
{
	/// <summary>
	///     Analysis settings class.
	/// </summary>
	public class AnalysisSettings : IEquatable<AnalysisSettings>
	{

		#region Properties

		/// <summary>
		///     Default <see cref="AnalysisSettings" /> object.
		/// </summary>
		public static AnalysisSettings Default { get; } = new()
		{
			ForceTolerance        = 1E-3,
			DisplacementTolerance = 1E-8,
			NumberOfSteps         = 50,
			MaxIterations         = 10000,
			Solver                = NonLinearSolver.NewtonRaphson
		};

		/// <summary>
		///     Returns true if this <see cref="AnalysisSettings" /> has the default values.
		/// </summary>
		public bool IsDefault => Equals(Default);

		/// <summary>
		///     Get/set the maximum number of iterations.
		/// </summary>
		public int MaxIterations { get; set; }

		/// <summary>
		///     Get/set the number of steps.
		/// </summary>
		public int NumberOfSteps { get; set; }

		/// <summary>
		///     Get/set the nonlinear solver.
		/// </summary>
		public NonLinearSolver Solver { get; set; }

		/// <summary>
		///     Get/set the convergence tolerance for residual forces.
		/// </summary>
		public double ForceTolerance { get; set; }
		
		/// <summary>
		///     Get/set the convergence tolerance for displacement increments.
		/// </summary>
		public double DisplacementTolerance { get; set; }

		#endregion

		#region Methods

		#region Interface Implementations

		/// <summary>
		///     Returns true if all parameters coincide.
		/// </summary>
		/// <param name="other">The other <see cref="AnalysisSettings" /> object.</param>
		public bool Equals(AnalysisSettings? other) => other is not null && ForceTolerance.Approx(other.ForceTolerance) && NumberOfSteps == other.NumberOfSteps && MaxIterations == other.MaxIterations;

		#endregion

		#region Object override

		public override bool Equals(object obj) => obj is AnalysisSettings settings && Equals(settings);

		public override int GetHashCode() => base.GetHashCode();

		#endregion

		#endregion

		#region Operators

		/// <summary>
		///     Returns true if all units coincide.
		/// </summary>
		public static bool operator ==(AnalysisSettings? left, AnalysisSettings? right) => left.IsEqualTo(right);

		/// <summary>
		///     Returns true if at least a unit do not coincide.
		/// </summary>
		public static bool operator !=(AnalysisSettings? left, AnalysisSettings? right) => left.IsNotEqualTo(right);

		/// <inheritdoc cref="SPMTool.Extensions.GetTypedValues(AnalysisSettings)" />
		public static explicit operator TypedValue[](AnalysisSettings? settings) => settings.GetTypedValues();

		/// <inheritdoc cref="SPMTool.Extensions.GetAnalysisSettings" />
		public static explicit operator AnalysisSettings?(TypedValue[]? values) => values.GetAnalysisSettings();

		#endregion

	}
}