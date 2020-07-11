namespace SPMTool
{
    // Constants
    public static class Constants
    {
        public const double
            Pi       = MathNet.Numerics.Constants.Pi,
            PiOver2  = MathNet.Numerics.Constants.PiOver2,
            PiOver4  = MathNet.Numerics.Constants.PiOver4,
            Pi3Over2 = MathNet.Numerics.Constants.Pi3Over2;
    }

	// Element names
	public enum Elements
	{
		Default,
		Node,
		Stringer,
		Panel,
		Support,
		Force
	}

	// Direction names
	public enum Directions
	{
		X,
		Y,
		XY
	}

	// Special characters
	public enum Characters
	{
		Alpha   = '\u03B1',
		Epsilon = '\u03B5',
		Gamma   = '\u03B3',
		Phi     = '\u00F8',
		Rho     = '\u03C1',
		Times   = '\u00D7'
	}

    // XData indexers
    public static class XData
    {
        // Node indexers
        public enum Node
        {
            AppName,
            XDataStr,
            Number,
            Ux,
            Uy
        }

        // Stringer indexers
        public enum Stringer
        {
            AppName,
            XDataStr,
            Number,
            Grip1,
            Grip2,
            Grip3,
            Width,
            Height,
            NumOfBars,
            BarDiam,
			Steelfy,
			SteelEs
        }

        // Panel indexers
        public enum Panel
        {
            AppName,
            XDataStr,
            Number,
            Grip1,
            Grip2,
            Grip3,
            Grip4,
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

        // Force indexers
        public enum Force
        {
            AppName,
            XDataStr,
            Value,
            Direction
        }

        // Force text indexers
        public enum ForceText
        {
            AppName,
            XDataStr,
			XPosition,
			YPosition,
            Direction
        }

        // SupportDirection Indexers
        public enum Support
        {
            AppName,
            XDataStr,
            Direction,
        }

		// Concrete indexers
		public enum Concrete
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

        // Unit indexers
        public enum Units
        {
            AppName,
            XDataStr,
            Geometry,
            Reinforcement,
            Displacements,
            AppliedForces,
            StringerForces,
            PanelStresses,
            MaterialStrength
        }
    }
}
