namespace SPMTool.Application
{
	/// <summary>
	///		Display settings class.
	/// </summary>
	public class DisplaySettings
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
	}
}