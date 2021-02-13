namespace SPMTool.Enums
{
	/// <summary>
	/// Directions.
	/// </summary>
	public enum Direction
	{
		X,
		Y
	}

	/// <summary>
	/// Unicode characters.
	/// </summary>
	public enum Character
	{
		Alpha   = '\u03B1',
		Epsilon = '\u03B5',
		Gamma   = '\u03B3',
		Phi     = '\u00F8',
		Rho     = '\u03C1',
		Times   = '\u00D7'
	}

    /// <summary>
    /// Color codes.
    /// </summary>
    public enum Color : short
	{
		White   = 0,
		Red     = 1,
		Yellow  = 2,
		Yellow1 = 41,
		Cyan    = 4,
		Blue1   = 5,
		Blue    = 150,
		Green   = 92,
		Grey    = 254
	}

	/// <summary>
    /// Layer names.
    /// </summary>
	public enum Layer
	{
		ExtNode,
		IntNode,
		Stringer,
		Panel,
		Support,
		Force,
		ForceText,
		StringerForce,
		PanelForce,
		CompressivePanelStress,
		TensilePanelStress,
		ConcreteCompressiveStress,
		ConcreteTensileStress,
		Displacements,
		Cracks
	}

	/// <summary>
    /// Block names.
    /// </summary>
	public enum Block
	{
		SupportX,
		SupportY,
		SupportXY,
		Force,
		Shear,
		CompressiveStress,
		TensileStress,
		PanelCrack,
		StringerCrack
	}

	/// <summary>
	/// Extended data index for nodes.
	/// </summary>
	public enum NodeIndex
	{
		AppName,
		XDataStr,
		Number,
		Ux,
		Uy
	}

	/// <summary>
	/// Extended data index for stringers.
	/// </summary>
	public enum StringerIndex
	{
		AppName,
		XDataStr,
		Number,
		Width,
		Height,
		NumOfBars,
		BarDiam,
		Steelfy,
		SteelEs
	}

	/// <summary>
	/// Extended data index for panels.
	/// </summary>
	public enum PanelIndex
	{
		AppName,
		XDataStr,
		Number,
		Width,
		XDiam,
		Sx,
		fyx,
		Esx,
		YDiam,
		Sy,
		fyy,
		Esy
	}

	/// <summary>
	/// Extended data index for forces.
	/// </summary>
	public enum ForceIndex
	{
		AppName,
		XDataStr,
		Value,
		Direction,
		TextHandle
	}

	/// <summary>
	/// Extended data index for force texts.
	/// </summary>
	public enum ForceTextIndex
	{
		AppName,
		XDataStr,
		BlockHandle
	}

	/// <summary>
	/// Extended data index for supports.
	/// </summary>
	public enum SupportIndex
	{
		AppName,
		XDataStr,
		Direction,
	}

	/// <summary>
	/// Extended data index for concrete.
	/// </summary>
	public enum ConcreteIndex
	{
		AppName,
		XDataStr,
		Model,
		Behavior,
		fc,
		AggType,
		AggDiam,
		ft,
		Ec,
		ec,
		ecu
	}

	/// <summary>
	/// Extended data index for units.
	/// </summary>
	public enum UnitsIndex
	{
		AppName,
		XDataStr,
		Geometry,
		Reinforcement,
		Displacements,
		AppliedForces,
		StringerForces,
		PanelStresses,
		MaterialStrength,
		DisplacementFactor,
		CrackOpenings
	}

	/// <summary>
	/// Extended data index for analysis settings.
	/// </summary>
	public enum AnalysisIndex
	{
		AppName,
		XDataStr,
		Tolerance,
		NumLoadSteps,
		MaxIterations
	}
}
