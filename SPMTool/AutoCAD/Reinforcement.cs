using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.DatabaseServices;
using Material;
using Material.Reinforcement;
using UnitsNet;
using Reinforcement = Material.Reinforcement.UniaxialReinforcement;

[assembly: CommandClass(typeof(SPMTool.AutoCAD.Material))]

namespace SPMTool.AutoCAD
{
    public static partial class Material
    {
	    private static char Phi = (char)Characters.Phi;

        // Database string configurations
        private static string PnlRef = "PnlRef";
        private static string StrRef = "StrRef";
	    private static string Steel  = "Steel";

	    [CommandMethod("SetStringerReinforcement")]
	    public static void SetStringerReinforcement()
	    {
		    // Read units
		    var units = Config.ReadUnits() ?? new Units();

            // Request objects to be selected in the drawing area
            var strs = UserInput.SelectStringers(
			    "Select the stringers to assign reinforcement (you can select other elements, the properties will be only applied to stringers).");

		    if (strs is null)
			    return;

		    // Get steel parameters and reinforcement from user
		    var reinforcement = GetStringerReinforcement(units);
		    var steel         = GetSteel(units);

			if (reinforcement is null && steel is null)
				return;

		    // Start a transaction
		    using (Transaction trans = Current.db.TransactionManager.StartTransaction())
		    {
			    // Save the properties
			    foreach (DBObject obj in strs)
			    {
				    // Open the selected object for read
				    Entity ent = (Entity) trans.GetObject(obj.ObjectId, OpenMode.ForWrite);

				    // Access the XData as an array
				    var data = Auxiliary.ReadXData(ent);

				    // Set values
				    if (reinforcement != null)
				    {
					    data[(int) XData.Stringer.NumOfBars] = new TypedValue((int) DxfCode.ExtendedDataInteger32, reinforcement.NumberOfBars);
					    data[(int) XData.Stringer.BarDiam]   = new TypedValue((int) DxfCode.ExtendedDataReal, reinforcement.BarDiameter);
				    }

				    if (steel != null)
				    {
					    data[(int) XData.Stringer.Steelfy] = new TypedValue((int) DxfCode.ExtendedDataReal, steel.YieldStress);
					    data[(int) XData.Stringer.SteelEs] = new TypedValue((int) DxfCode.ExtendedDataReal, steel.ElasticModule);
				    }

				    // Add the new XData
				    ent.XData = new ResultBuffer(data);
			    }

			    // Save the new object to the database
			    trans.Commit();
		    }
	    }

	    // Get reinforcement parameters from user
		private static Reinforcement GetStringerReinforcement(Units units)
		{
			// Get saved reinforcement options
			var savedRef = ReadStringerReinforcement();

			// Get unit abreviation
			var dimAbrev = Length.GetAbbreviation(units.Reinforcement);

            // Get saved reinforcement options
            if (savedRef != null)
			{
				var options = new List<string>();

				// Get the options
				for (int i = 0; i < savedRef.Length; i++)
				{
					int    n = savedRef[i].NumberOfBars;
					double d = units.ConvertFromMillimeter(savedRef[i].BarDiameter, units.Reinforcement);

					string name = n.ToString() + Phi + d;

					options.Add(name);
				}

				// Add option to set new reinforcement
				options.Add("New");

				// Ask the user to choose the options
				var res = UserInput.SelectKeyword("Choose a reinforcement option (" + dimAbrev + ") or add a new one:", options.ToArray(),
					options[0]);

				if (!res.HasValue)
					return null;

				// Get string result
				var (index, keyword) = res.Value;

				// Get the index
				if (keyword != "New")
					return savedRef[index];
			}

			// New reinforcement
			// Ask the user to input the number of bars
			var numn = UserInput.GetInteger(
				"Input the number of Stringer reinforcement bars (only needed for nonlinear analysis):", 2);

			if (!numn.HasValue)
				return null;

			// Ask the user to input the Stringer height
			double def = units.ConvertFromMillimeter(10, units.Reinforcement);
			var phin = UserInput.GetDouble("Input the diameter (" + dimAbrev + ") of Stringer reinforcement bars:", def);

			if (!phin.HasValue)
				return null;

			// Get reinforcement
			int num    = numn.Value;
			double phi = units.ConvertToMillimeter(phin.Value, units.Reinforcement);

			var reinforcement = new Reinforcement(num, phi);

			// Save the reinforcement
			SaveStringerReinforcement(reinforcement);

			return reinforcement;
		}

		// Get steel parameters from user
		private static Steel GetSteel(Units units)
		{
			// Get steel data saved on database
			var savedSteel = ReadSteel();

			// Get unit abreviation
			var matAbrev = Pressure.GetAbbreviation(units.MaterialStrength);

            // Get saved reinforcement options
            if (savedSteel != null)
			{
				var options = new List<string>();

				// Get the options
				for (int i = 0; i < savedSteel.Length; i++)
				{
					double
						f = units.ConvertFromMPa(savedSteel[i].YieldStress,   units.MaterialStrength),
						E = units.ConvertFromMPa(savedSteel[i].ElasticModule, units.MaterialStrength);

					string name = f + "|" + E;

					options.Add(name);
				}

				// Add option to set new reinforcement
				options.Add("New");

				// Ask the user to choose the options
				var res = UserInput.SelectKeyword("Choose a steel option (fy | Es) (" + matAbrev + ") or add a new one:",
					options.ToArray(),
					options[0]);

				if (!res.HasValue)
					return null;

				// Get string result
				var (index, keyword) = res.Value;

				// Get the index
				if (keyword != "New")
					return savedSteel[index];
			}

			// Ask the user to input the Steel yield strength
			double fDef = units.ConvertFromMPa(500, units.MaterialStrength);
			var fyn = UserInput.GetDouble("Input the yield strength (" + matAbrev + ") of Steel:", fDef);

			if (!fyn.HasValue)
				return null;

            // Ask the user to input the Steel elastic modulus
            double eDef = units.ConvertFromMPa(210000, units.MaterialStrength);
            var Esn = UserInput.GetDouble("Input the elastic modulus (" + matAbrev + ") of Steel:", eDef);

			if (!Esn.HasValue)
				return null;

			double
				fy = units.ConvertToMPa(fyn.Value, units.MaterialStrength),
				Es = units.ConvertToMPa(Esn.Value, units.MaterialStrength);

			var steel = new Steel(fy, Es);

			// Save steel
			SaveSteel(steel);

			return steel;
		}

		[CommandMethod("SetPanelReinforcement")]
		public static void SetPanelReinforcement()
		{
			// Read units
			var units = Config.ReadUnits() ?? new Units();

            // Request objects to be selected in the drawing area
            var pnls = UserInput.SelectPanels(
				"Select the panels to assign reinforcement (you can select other elements, the properties will be only applied to panels).");

			if (pnls is null)
				return;

			// Get the values
			var refX   = GetPanelReinforcement(Directions.X, units);
			var steelX = GetSteel(units);
			var refY   = GetPanelReinforcement(Directions.Y, units);
			var steelY = GetSteel(units);

			if (!refX.HasValue && !refY.HasValue && steelX is null && steelY is null)
				return;

			// Start a transaction
			using (Transaction trans = Current.db.TransactionManager.StartTransaction())
			{
				foreach (DBObject obj in pnls)
				{
					// Open the selected object for read
					Entity ent = trans.GetObject(obj.ObjectId, OpenMode.ForWrite) as Entity;

					// Access the XData as an array
					ResultBuffer rb = ent.GetXDataForApplication(Current.appName);
					TypedValue[] data = rb.AsArray();

					// Set the new reinforcement (line 7 to 9 of the array)
					if (refX.HasValue)
					{
						data[(int) XData.Panel.XDiam] =
							new TypedValue((int) DxfCode.ExtendedDataReal, refX.Value.diameter);
						data[(int) XData.Panel.Sx]    =
							new TypedValue((int) DxfCode.ExtendedDataReal, refX.Value.spacing);
					}

					if (steelX != null)
					{
						data[(int) XData.Panel.fyx]   =
							new TypedValue((int) DxfCode.ExtendedDataReal, steelX.YieldStress);
						data[(int) XData.Panel.Esx]   =
							new TypedValue((int) DxfCode.ExtendedDataReal, steelX.ElasticModule);
					}

					if (refY.HasValue)
					{
						data[(int) XData.Panel.YDiam] =
							new TypedValue((int) DxfCode.ExtendedDataReal, refY.Value.diameter);
						data[(int) XData.Panel.Sy]    =
							new TypedValue((int) DxfCode.ExtendedDataReal, refY.Value.spacing);
					}

					if (steelY != null)
					{
						data[(int) XData.Panel.fyy]   =
							new TypedValue((int) DxfCode.ExtendedDataReal, steelY.YieldStress);
						data[(int) XData.Panel.Esy]   =
							new TypedValue((int) DxfCode.ExtendedDataReal, steelY.ElasticModule);
					}

					// Add the new XData
					ent.XData = new ResultBuffer(data);
				}

				// Save the new object to the database
				trans.Commit();
			}
		}

		// Get reinforcement parameters from user
		private static (double diameter, double spacing)? GetPanelReinforcement(Directions direction, Units units)
		{
			// Get saved reinforcement options
			var savedRef = ReadPanelReinforcement();

			// Get unit abreviation
			var dimAbrev = Length.GetAbbreviation(units.Geometry);
			var refAbrev = Length.GetAbbreviation(units.Reinforcement);

			// Get saved reinforcement options
            if (savedRef != null)
			{
				var options = new List<string>();

				// Get the options
				for (int i = 0; i < savedRef.Length; i++)
				{
					double
						d  = units.ConvertFromMillimeter(savedRef[i].diameter, units.Reinforcement),
						si = units.ConvertFromMillimeter(savedRef[i].spacing,  units.Geometry);

					string name = Phi.ToString() + d + "|" + si;

					options.Add(name);
				}

				// Add option to set new reinforcement
				options.Add("New");

				// Ask the user to choose the options
				var res = UserInput.SelectKeyword(
					"Choose a reinforcement option (" + Phi + " | s)(" + refAbrev + " | " + dimAbrev +") for " + direction + " direction or add a new one:",
					options.ToArray(),
					options[0]);

				if (!res.HasValue)
					return null;

				// Get string result
				var (index, keyword) = res.Value;

				// Get the index
				if (keyword != "New")
					return savedRef[index];
			}

			// New reinforcement
			// Ask the user to input the diameter of bars
			double phiDef = units.ConvertFromMillimeter(10, units.Reinforcement);
			var phin = UserInput.GetDouble(
				"Input the reinforcement bar diameter (" + refAbrev + ") for " + direction +
				" direction for selected panels (only needed for nonlinear analysis):", phiDef);

			if (!phin.HasValue)
				return null;

            // Ask the user to input the bar spacing
            double sDef = units.ConvertFromMillimeter(100, units.Geometry);
            var sn = UserInput.GetDouble(
                "Input the bar spacing (" + dimAbrev + ") for " + direction + " direction:", sDef);

			if (!sn.HasValue)
				return null;

			// Save the reinforcement
			double
				phi = units.ConvertToMillimeter(phin.Value, units.Reinforcement),
				s   = units.ConvertToMillimeter(sn.Value,   units.Geometry);

			SavePanelReinforcement(phi, s);

			return (phi, s);
		}

		// Save steel configuration on database
        private static void SaveSteel(Steel steel)
        {
	        if (steel != null)
	        {
		        // Get data
		        double
			        fy = steel.YieldStress,
			        Es = steel.ElasticModule;

		        // Get the name to save
		        string name = Steel + "f" + fy + "E" + Es;

		        // Save the variables on the Xrecord
		        using (ResultBuffer rb = new ResultBuffer())
		        {
			        rb.Add(new TypedValue((int)DxfCode.ExtendedDataRegAppName, Current.appName)); // 0
			        rb.Add(new TypedValue((int)DxfCode.ExtendedDataAsciiString, name));           // 1
			        rb.Add(new TypedValue((int)DxfCode.ExtendedDataReal, fy));                    // 2
			        rb.Add(new TypedValue((int)DxfCode.ExtendedDataReal, Es));                    // 3

			        // Create the entry in the NOD if it doesn't exist
			        Auxiliary.SaveObjectDictionary(name, rb, false);
		        }
	        }
        }

        // Read steel on database
        private static Steel[] ReadSteel()
        {
	        // Create a list of reinforcement
	        var stList = new List<Steel>();

	        // Get dictionary entries
	        var entries = Auxiliary.ReadDictionaryEntries(Steel);

	        if (entries is null)
		        return null;

	        foreach (var entry in entries)
	        {
		        // Read data
		        var data = entry.AsArray();

		        double
			        fy = Convert.ToDouble(data[2].Value),
			        Es = Convert.ToDouble(data[3].Value);

		        // Create new reinforcement
		        var steel = new Steel(fy, Es);

		        // Add to the list
		        stList.Add(steel);
	        }

	        if (stList.Count > 0)
		        return
			        stList.ToArray();

	        // None
	        return null;
        }

        // Save reinforcement configuration on database
        private static void SaveStringerReinforcement(Reinforcement reinforcement)
        {
	        if (reinforcement != null)
	        {
		        // Get data
		        int    num = reinforcement.NumberOfBars;
		        double phi = reinforcement.BarDiameter;

		        // Get the name to save
		        string name = StrRef + "n" + num + "d" + phi;

		        // Save the variables on the Xrecord
		        using (ResultBuffer rb = new ResultBuffer())
		        {
			        rb.Add(new TypedValue((int)DxfCode.ExtendedDataRegAppName,  Current.appName)); // 0
			        rb.Add(new TypedValue((int)DxfCode.ExtendedDataAsciiString, name));            // 1
			        rb.Add(new TypedValue((int)DxfCode.ExtendedDataInteger32,   num));             // 2
			        rb.Add(new TypedValue((int)DxfCode.ExtendedDataReal,        phi));             // 3
					 
			        // Create the entry in the NOD if it doesn't exist
			        Auxiliary.SaveObjectDictionary(name, rb, false);
		        }
	        }
        }

		// Read stringer reinforcement on database
		private static Reinforcement[] ReadStringerReinforcement()
		{
			// Create a list of reinforcement
			var refList = new List<Reinforcement>();

			// Get dictionary entries
			var entries = Auxiliary.ReadDictionaryEntries(StrRef);

			if (entries is null)
				return null;

			foreach (var entry in entries)
			{
				// Read data
				var data = entry.AsArray();

				int num    = Convert.ToInt32 (data[2].Value);
				double phi = Convert.ToDouble(data[3].Value);

				// Create new reinforcement
				var reinforcement = new Reinforcement(num, phi);

				// Add to the list
				refList.Add(reinforcement);
			}

			if (refList.Count > 0)
				return 
					refList.ToArray();

			// None
			return null;
        }

        // Save reinforcement configuration on database
        private static void SavePanelReinforcement(double barDiameter, double spacing)
        {
	        // Get the names to save
	        string name = PnlRef + "d" + barDiameter + "s" + spacing;

	        using (ResultBuffer rb = new ResultBuffer())
	        {
		        rb.Add(new TypedValue((int)DxfCode.ExtendedDataRegAppName,  Current.appName)); // 0
		        rb.Add(new TypedValue((int)DxfCode.ExtendedDataAsciiString, name));            // 1
		        rb.Add(new TypedValue((int)DxfCode.ExtendedDataReal,        barDiameter));     // 2
		        rb.Add(new TypedValue((int)DxfCode.ExtendedDataReal,        spacing));         // 3

                // Create the entry in the NOD if it doesn't exist
                Auxiliary.SaveObjectDictionary(name, rb, false);
	        }
        }

        // Read panel reinforcement on database
        private static (double diameter, double spacing)[] ReadPanelReinforcement()
        {
	        // Create a list of reinforcement
	        var refList = new List<(double diameter, double spacing)>();

	        // Get dictionary entries
	        var entries = Auxiliary.ReadDictionaryEntries(PnlRef);

	        if (entries is null)
		        return null;

	        foreach (var entry in entries)
	        {
		        // Read data
		        var data = entry.AsArray();

		        double
			        phi = Convert.ToDouble(data[2].Value),
			        s   = Convert.ToDouble(data[3].Value);

		        // Add to the list
		        refList.Add((phi, s));
	        }

	        if (refList.Count > 0)
		        return
			        refList.ToArray();

	        // None
	        return null;
        }

    }
}