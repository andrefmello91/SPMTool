using System.Linq;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.DatabaseServices;
using Extensions.Number;
using Material.Reinforcement;
using SPMTool.Database;
using SPMTool.Database.Materials;
using SPMTool.Editor;
using SPMTool.Enums;
using UnitsNet;

[assembly: CommandClass(typeof(ElementReinforcement))]

namespace SPMTool.Editor
{
	/// <summary>
    /// Element reinforcement class.
    /// </summary>
    public static class ElementReinforcement
    {
	    private static char Phi = (char)Character.Phi;

		/// <summary>
        /// Set the reinforcement in a collection of stringers.
        /// </summary>
        [CommandMethod("SetStringerReinforcement")]
	    public static void SetStringerReinforcement()
	    {
		    // Read units
		    var units = DataBase.Units;

            // Request objects to be selected in the drawing area
            var strs = UserInput.SelectStringers("Select the stringers to assign reinforcement (you can select other elements, the properties will be only applied to stringers).");

		    if (strs is null)
			    return;

		    // Get steel parameters and reinforcement from user
		    var reinforcement = GetStringerReinforcement(units);

			if (reinforcement is null)
				return;

		    // Start a transaction
		    using (var trans = DataBase.StartTransaction())
		    {
			    // Save the properties
			    foreach (DBObject obj in strs)
					using(var ent = (Entity)trans.GetObject(obj.ObjectId, OpenMode.ForWrite))
				    {
					    // Access the XData as an array
					    var data = ent.ReadXData();

					    // Set values
					    data[(int) StringerIndex.NumOfBars] = new TypedValue((int) DxfCode.ExtendedDataInteger32, reinforcement.NumberOfBars);
					    data[(int) StringerIndex.BarDiam]   = new TypedValue((int) DxfCode.ExtendedDataReal, reinforcement.BarDiameter);

					    var steel = reinforcement.Steel;

					    if (steel != null)
					    {
						    data[(int) StringerIndex.Steelfy] = new TypedValue((int) DxfCode.ExtendedDataReal, steel.YieldStress);
						    data[(int) StringerIndex.SteelEs] = new TypedValue((int) DxfCode.ExtendedDataReal, steel.ElasticModule);
					    }

					    // Add the new XData
					    ent.XData = new ResultBuffer(data);
				    }

			    // Save the new object to the database
			    trans.Commit();
		    }
	    }

        /// <summary>
        /// Get reinforcement parameters from user.
        /// </summary>
        /// <param name="units">Current <see cref="Units"/>.</param>
        private static UniaxialReinforcement GetStringerReinforcement(Units units)
		{
			// Get saved reinforcement options
			var savedRef = DataBase.SavedStringerReinforcement;

			// Get unit abreviation
			var dimAbrev = Length.GetAbbreviation(units.Reinforcement);

            // Get saved reinforcement options
            if (savedRef != null)
			{
                // Get the options
                var options = savedRef.Select(r => $"{r.NumberOfBars}{Phi}{r.BarDiameter.ConvertFromMillimeter(units.Reinforcement):0.00}").ToList();

				// Add option to set new reinforcement
				options.Add("New");

				// Ask the user to choose the options
				var res = UserInput.SelectKeyword($"Choose a reinforcement option ({dimAbrev}) or add a new one:", options, out var index, options[0]);

				if (res is null)
					return null;

				// Get the index
				if (res != "New")
					return savedRef[index];
			}

			// New reinforcement
			// Ask the user to input the number of bars
			var numn = UserInput.GetInteger("Input the number of Stringer reinforcement bars (only needed for nonlinear analysis):", 2);

			if (!numn.HasValue)
				return null;

			// Ask the user to input the Stringer height
			var def  = 10.ConvertFromMillimeter(units.Reinforcement);
			var phin = UserInput.GetDouble($"Input the diameter ({dimAbrev}) of Stringer reinforcement bars:", def);

			if (!phin.HasValue)
				return null;

			// Get steel
			var steel = GetSteel(units);

			if (steel is null)
				return null;

            // Get reinforcement
            var num = numn.Value;
			var phi = phin.Value.Convert(units.Reinforcement);

			var reinforcement = new UniaxialReinforcement(num, phi, steel);

			// Save the reinforcement
			ReinforcementData.Save(reinforcement);

			return reinforcement;
		}

        /// <summary>
        /// Get steel parameters from user.
        /// </summary>
        /// <param name="units">Current <see cref="Units"/>.</param>
		private static Steel GetSteel(Units units)
		{
			// Get steel data saved on database
			var savedSteel = Database.DataBase.SavedSteel;

			// Get unit abbreviation
			var matAbrev = Pressure.GetAbbreviation(units.MaterialStrength);

            // Get saved reinforcement options
            if (savedSteel != null)
			{
				// Get the options
				var options = savedSteel.Select(s => $"{s.YieldStress.ConvertFromMPa(units.MaterialStrength):0.00}|{s.ElasticModule.ConvertFromMPa(units.MaterialStrength):0.00}").ToList();

                // Add option to set new reinforcement
                options.Add("New");

				// Ask the user to choose the options
				var res = UserInput.SelectKeyword($"Choose a steel option (fy | Es) ({matAbrev}) or add a new one:", options, out var index, options[0]);

				if (res is null)
					return null;

				// Get the index
				if (res != "New")
					return savedSteel[index];
			}

			// Ask the user to input the Steel yield strength
			var fDef = 500.ConvertFromMPa(units.MaterialStrength);
			var fyn  = UserInput.GetDouble($"Input the yield strength ({matAbrev}) of Steel:", fDef);

			if (!fyn.HasValue)
				return null;

            // Ask the user to input the Steel elastic modulus
            var eDef = 210000.ConvertFromMPa(units.MaterialStrength);
            var Esn  = UserInput.GetDouble($"Input the elastic modulus ({matAbrev}) of Steel:", eDef);

			if (!Esn.HasValue)
				return null;

			double
				fy = fyn.Value.Convert(units.MaterialStrength),
				Es = Esn.Value.Convert(units.MaterialStrength);

			var steel = new Steel(fy, Es);

			// Save steel
			ReinforcementData.Save(steel);

			return steel;
		}

		/// <summary>
        /// Set reinforcement to a collection of panels.
        /// </summary>
		[CommandMethod("SetPanelReinforcement")]
		public static void SetPanelReinforcement()
		{
			// Read units
			var units = Database.DataBase.Units;

            // Request objects to be selected in the drawing area
            var pnls = UserInput.SelectPanels("Select the panels to assign reinforcement (you can select other elements, the properties will be only applied to panels).");

			if (pnls is null)
				return;

			// Get the values
			var refX   = GetPanelReinforcement(Direction.X, units);
			var refY   = GetPanelReinforcement(Direction.Y, units);

			if (refX is null && refY is null)
				return;

			// Start a transaction
			using (var trans = DataBase.StartTransaction())
			{
				foreach (DBObject obj in pnls)
					using (var ent = (Entity) trans.GetObject(obj.ObjectId, OpenMode.ForWrite))
					{
						// Access the XData as an array
						var data = ent.ReadXData();

						// Set the new reinforcement (line 7 to 9 of the array)
						if (refX != null)
						{
							data[(int) PanelIndex.XDiam] = new TypedValue((int) DxfCode.ExtendedDataReal, refX.BarDiameter);
							data[(int) PanelIndex.Sx]    = new TypedValue((int) DxfCode.ExtendedDataReal, refX.BarSpacing);

							var steelX = refX.Steel;

							if (steelX != null)
							{
								data[(int) PanelIndex.fyx] = new TypedValue((int) DxfCode.ExtendedDataReal, steelX.YieldStress);
								data[(int) PanelIndex.Esx] = new TypedValue((int) DxfCode.ExtendedDataReal, steelX.ElasticModule);
							}
						}

						if (refY != null)
						{
							data[(int) PanelIndex.YDiam] = new TypedValue((int) DxfCode.ExtendedDataReal, refY.BarDiameter);
							data[(int) PanelIndex.Sy]    = new TypedValue((int) DxfCode.ExtendedDataReal, refY.BarSpacing);

							var steelY = refY.Steel;

							if (steelY != null)
							{
								data[(int) PanelIndex.fyy] = new TypedValue((int) DxfCode.ExtendedDataReal, steelY.YieldStress);
								data[(int) PanelIndex.Esy] = new TypedValue((int) DxfCode.ExtendedDataReal, steelY.ElasticModule);
							}
						}

						// Add the new XData
						ent.XData = new ResultBuffer(data);
					}

				// Save the new object to the database
				trans.Commit();
			}
		}

        /// <summary>
        /// Get panel reinforcement parameters from user.
        /// </summary>
        /// <param name="direction">The direction of reinforcement.</param>
        /// <param name="units">Current <see cref="Units"/>.</param>
        private static WebReinforcementDirection GetPanelReinforcement(Direction direction, Units units)
		{
			// Get saved reinforcement options
			var savedRef = DataBase.SavedPanelReinforcement;

			// Get unit abreviation
			var dimAbrev = Length.GetAbbreviation(units.Geometry);
			var refAbrev = Length.GetAbbreviation(units.Reinforcement);

			// Get saved reinforcement options
            if (savedRef != null)
			{
				// Get the options
                var options = savedRef.Select(r => $"{Phi}{r.BarDiameter.ConvertFromMillimeter(units.Reinforcement):0.00}|{r.BarSpacing.ConvertFromMillimeter(units.Geometry):0.00}").ToList();

                // Add option to set new reinforcement
                options.Add("New");

				// Ask the user to choose the options
				var res = UserInput.SelectKeyword($"Choose a reinforcement option ({Phi} | s)({refAbrev} | {dimAbrev}) for {direction} direction or add a new one:", options, out var index, options[0]);

				if (res is null)
					return null;

				// Get the index
				if (res != "New")
					return savedRef[index];
			}

			// New reinforcement
			// Ask the user to input the diameter of bars
			var phin = UserInput.GetDouble($"Input the reinforcement bar diameter ({refAbrev}) for {direction} direction for selected panels (only needed for nonlinear analysis):", 10.ConvertFromMillimeter(units.Reinforcement));

			if (!phin.HasValue)
				return null;

            // Ask the user to input the bar spacing
            var sn = UserInput.GetDouble($"Input the bar spacing ({dimAbrev}) for {direction} direction:", 100.ConvertFromMillimeter(units.Geometry));

			if (!sn.HasValue)
				return null;

            // Get steel
            var steel = GetSteel(units);

            if (steel is null)
	            return null;

            // Save the reinforcement
            double
                phi = phin.Value.Convert(units.Reinforcement),
				s   = sn.Value.Convert(units.Geometry);

			var reinforcement = new WebReinforcementDirection(phi, s, steel, 0, 0);

			ReinforcementData.Save(reinforcement);

			return reinforcement;
		}
    }
}