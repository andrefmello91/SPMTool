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
    }
}
