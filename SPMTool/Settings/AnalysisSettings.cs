﻿using System;
using System.Collections.Generic;
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

		#region Fields

		/// <summary>
		///     Default <see cref="AnalysisSettings" /> object.
		/// </summary>
		public static AnalysisSettings Default { get; } = new()
		{
			Tolerance     = 1E-3,
			NumLoadSteps  = 50,
			MaxIterations = 10000,
			Solver        = NonLinearSolver.Secant
		};

		#endregion

		#region Properties

		/// <summary>
		///     Returns true if this <see cref="AnalysisSettings" /> has the default values.
		/// </summary>
		public bool IsDefault => Equals(Default);

		/// <summary>
		///		Get/set the nonlinear solver.
		/// </summary>
		public NonLinearSolver Solver { get; set; }
		
		/// <summary>
		///     Get/set the maximum number of iterations.
		/// </summary>
		public int MaxIterations { get; set; }

		/// <summary>
		///     Get/set the number of load steps.
		/// </summary>
		public int NumLoadSteps { get; set; }

		/// <summary>
		///     Get/set the convergence tolerance
		/// </summary>
		public double Tolerance { get; set; }

		#endregion

		#region Methods

		#region Interface Implementations

		/// <summary>
		///     Returns true if all parameters coincide.
		/// </summary>
		/// <param name="other">The other <see cref="AnalysisSettings" /> object.</param>
		public bool Equals(AnalysisSettings? other) => other is not null && Tolerance.Approx(other.Tolerance) && NumLoadSteps == other.NumLoadSteps && MaxIterations == other.MaxIterations;

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

		/// <inheritdoc cref="SPMTool.Extensions.GetTypedValues(AnalysisSettings)"/>
		public static explicit operator TypedValue[](AnalysisSettings? settings) => settings.GetTypedValues();
		
		/// <inheritdoc cref="SPMTool.Extensions.GetAnalysisSettings"/>
		public static explicit operator AnalysisSettings?(TypedValue[]? values) => values.GetAnalysisSettings();

		#endregion

	}
}