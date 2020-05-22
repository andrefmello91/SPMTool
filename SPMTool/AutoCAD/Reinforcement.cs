using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using SPMTool.Material;

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
		    // Request objects to be selected in the drawing area
		    var strs = UserInput.SelectObjects(
			    "Select the stringers to assign reinforcement (you can select other elements, the properties will be only applied to stringers).",
			    new[] { Layers.Stringer });

		    if (strs == null)
			    return;

		    // Get steel parameters and reinforcement from user
		    var reinforcement = GetStringerReinforcement();
		    var steel         = GetSteel();

			if (reinforcement == null && steel == null)
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
		private static StringerReinforcement GetStringerReinforcement()
		{
			// Get saved reinforcement options
			var savedRef = ReadStringerReinforcement();

			// Get saved reinforcement options
			if (savedRef != null)
			{
				var options = new List<string>();

				// Get the options
				for (int i = 0; i < savedRef.Length; i++)
				{
					int    n = savedRef[i].NumberOfBars;
					double d = savedRef[i].BarDiameter;

					string name = n.ToString() + Phi + d;

					options.Add(name);
				}

				// Add option to set new reinforcement
				options.Add("New");

				// Ask the user to choose the options
				var res = UserInput.SelectKeyword("Choose a reinforcement option or add a new one:", options.ToArray(),
					options[0]);

				if (!res.HasValue)
					return null;

				// Get string result
				var (index, keyword) = res.Value;

				// Get the index
				if (keyword != "New")
				{
					return savedRef[index];
					//for (int i = 0; i < options.Keywords.Count; i++)
					//{
					//	if (res == options.Keywords[i].GlobalName)
					//		reinforcement = savedRef[i];
					//}
				}
			}

			// New reinforcement
			// Ask the user to input the number of bars
			var numn = UserInput.GetInteger(
				"Input the number of Stringer reinforcement bars (only needed for nonlinear analysis):", 2);

			if (!numn.HasValue)
				return null;

			// Ask the user to input the Stringer height
			var phin = UserInput.GetDouble("Input the diameter (in mm) of Stringer reinforcement bars:", 10);

			if (!phin.HasValue)
				return null;

			// Get reinforcement
			int num    = numn.Value;
			double phi = phin.Value;

			var reinforcement = new StringerReinforcement(num, phi);

			// Save the reinforcement
			SaveStringerReinforcement(reinforcement);

			return reinforcement;
		}

		// Get steel parameters from user
		private static Steel GetSteel()
		{
			// Get steel data saved on database
			var savedSteel = ReadSteel();

			// Get saved reinforcement options
			if (savedSteel != null)
			{
				var options = new List<string>();

				// Get the options
				for (int i = 0; i < savedSteel.Length; i++)
				{
					double
						f = savedSteel[i].YieldStress,
						E = savedSteel[i].ElasticModule;

					string name = f + "|" + E;

					options.Add(name);
				}

				// Add option to set new reinforcement
				options.Add("New");

				// Ask the user to choose the options
				var res = UserInput.SelectKeyword("Choose a steel option (fy | Es) or add a new one:", options.ToArray(),
					options[0]);

				if (!res.HasValue)
					return null;

				// Get string result
				var (index, keyword) = res.Value;

				// Get the index
				if (keyword != "New")
				{
					return savedSteel[index];
					//for (int i = 0; i < options.Keywords.Count; i++)
					//{
					//	if (res == options.Keywords[i].GlobalName)
					//		steel = savedSteel[i];
					//}
				}
			}

			// If it's a new steel
			//if (steel == null)
			//{
                // Ask the user to input the Steel yield strength
                var fyn = UserInput.GetDouble("Input the yield strength (MPa) of Steel:", 500);

				if (!fyn.HasValue)
					return null;

				// Ask the user to input the Steel elastic modulus
            var Esn = UserInput.GetDouble("Input the elastic modulus (MPa) of Steel:", 210000);

				if (!Esn.HasValue)
					return null;

				double
					fy = fyn.Value,
					Es = Esn.Value;

				var steel = new Steel(fy, Es);

				// Save steel
				SaveSteel(steel);
			//}

			return steel;
		}

		[CommandMethod("SetPanelReinforcement")]
        public static void SetPanelReinforcement()
        {
	        // Request objects to be selected in the drawing area
	        var pnls = UserInput.SelectObjects(
		        "Select the panels to assign reinforcement (you can select other elements, the properties will be only applied to panels).",
		        new[] { Layers.Panel });

	        if (pnls == null)
		        return;

	        // Get the values
	        var refX   = GetPanelReinforcement(Directions.X);
	        var steelX = GetSteel();
	        var refY   = GetPanelReinforcement(Directions.Y);
	        var steelY = GetSteel();

			if (!refX.HasValue && !refY.HasValue && steelX == null && steelY == null)
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
			                data[(int) XData.Panel.XDiam] = new TypedValue((int) DxfCode.ExtendedDataReal, refX.Value.diameter);
			                data[(int) XData.Panel.Sx]    = new TypedValue((int) DxfCode.ExtendedDataReal, refX.Value.spacing);
		                }

		                if (steelX != null)
		                {
			                data[(int) XData.Panel.fyx]   = new TypedValue((int) DxfCode.ExtendedDataReal, steelX.YieldStress);
			                data[(int) XData.Panel.Esx]   = new TypedValue((int) DxfCode.ExtendedDataReal, steelX.ElasticModule);
		                }

		                if (refY.HasValue)
		                {
			                data[(int) XData.Panel.YDiam] = new TypedValue((int) DxfCode.ExtendedDataReal, refY.Value.diameter);
			                data[(int) XData.Panel.Sy]    = new TypedValue((int) DxfCode.ExtendedDataReal, refY.Value.spacing);
		                }

		                if (steelY != null)
		                {
			                data[(int) XData.Panel.fyy]   = new TypedValue((int) DxfCode.ExtendedDataReal, steelY.YieldStress);
			                data[(int) XData.Panel.Esy]   = new TypedValue((int) DxfCode.ExtendedDataReal, steelY.ElasticModule);
		                }

		                // Add the new XData
		                ent.XData = new ResultBuffer(data);
	                }

	                // Save the new object to the database
	                trans.Commit();
                }
        }

        // Get reinforcement parameters from user
        private static (double diameter, double spacing)? GetPanelReinforcement(Directions direction)
        {
            // Get saved reinforcement options
            var savedRef = ReadPanelReinforcement();

            // Get saved reinforcement options
            if (savedRef != null)
            {
	            var options = new List<string>();

                // Get the options
                for (int i = 0; i < savedRef.Length; i++)
                {
	                double
		                d  = savedRef[i].diameter,
		                si = savedRef[i].spacing;

                    string name = Phi.ToString() + d + "|" + si;

                    options.Add(name);
                }

                // Add option to set new reinforcement
                options.Add("New");

                // Ask the user to choose the options
                var res = UserInput.SelectKeyword("Choose a reinforcement option (" + Phi + "|s) for " + direction + " direction or add a new one:", options.ToArray(),
	                options[0]);

                if (!res.HasValue)
                    return null;

                // Get string result
                var (index, keyword) = res.Value;

                // Get the index
                if (keyword != "New")
                {
	                return savedRef[index];
	                //for (int i = 0; i < options.Keywords.Count; i++)
	                //{
	                //    if (res == options.Keywords[i].GlobalName)
	                //        reinforcement = savedRef[i];
	                //}
                }
            }

            // New reinforcement
            // Ask the user to input the diameter of bars
	            var phin = UserInput.GetDouble(
		            "Input the reinforcement bar diameter (in mm) for " + direction +
		            " direction for selected panels (only needed for nonlinear analysis):", 10);

	            if (!phin.HasValue)
		            return null;

            // Ask the user to input the bar spacing
            var sn = UserInput.GetDouble(
	            "Input the bar spacing (in mm) for " + direction + " direction:", 100);

	            if (!sn.HasValue)
		            return null;

            // Save the reinforcement
            double
	            phi = phin.Value,
	            s   = sn.Value;

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

		        // Start a transaction
		        using (Transaction trans = Current.db.TransactionManager.StartTransaction())
		        {
			        // Get the NOD in the database
			        var nod = (DBDictionary) trans.GetObject(Current.nod, OpenMode.ForRead);

			        // Get the name to save
			        string name = Steel + "f" + fy + "E" + Es;

			        if (!nod.Contains(name))
			        {
				        // Save the variables on the Xrecord
				        using (ResultBuffer rb = new ResultBuffer())
				        {
					        rb.Add(new TypedValue((int) DxfCode.ExtendedDataRegAppName, Current.appName)); // 0
					        rb.Add(new TypedValue((int) DxfCode.ExtendedDataAsciiString, name));           // 1
					        rb.Add(new TypedValue((int) DxfCode.ExtendedDataReal, fy));                    // 2
					        rb.Add(new TypedValue((int) DxfCode.ExtendedDataReal, Es));                    // 3

					        // Create the entry in the NOD
					        Auxiliary.SaveObjectDictionary(name, rb);
				        }

				        trans.Commit();
			        }
		        }
	        }
        }

        // Read steel on database
        private static Steel[] ReadSteel()
        {
	        // Create a list of reinforcement
	        var stList = new List<Steel>();

	        // Start a transaction
	        using (Transaction trans = Current.db.TransactionManager.StartTransaction())
	        {
		        // Get the NOD in the database
		        var nod = (DBDictionary) trans.GetObject(Current.nod, OpenMode.ForRead);

		        // Check saved reinforcements
		        foreach (var entry in nod)
		        {
			        if (entry.Key.Contains(Steel))
			        {
				        // Read data
				        var stXrec = (Xrecord) trans.GetObject(entry.Value, OpenMode.ForRead);
				        var stDAta = stXrec.Data.AsArray();

				        double
					        fy = Convert.ToDouble(stDAta[2].Value),
					        Es = Convert.ToDouble(stDAta[3].Value);

				        // Create new reinforcement
				        var steel = new Steel(fy, Es);

				        // Add to the list
				        stList.Add(steel);
			        }
		        }
	        }

	        if (stList.Count > 0)
		        return
			        stList.ToArray();

	        // None
	        return null;
        }

        // Save reinforcement configuration on database
        private static void SaveStringerReinforcement(StringerReinforcement reinforcement)
        {
	        if (reinforcement != null)
	        {
		        // Get data
		        int    num = reinforcement.NumberOfBars;
		        double phi = reinforcement.BarDiameter;

		        // Start a transaction
		        using (Transaction trans = Current.db.TransactionManager.StartTransaction())
		        {
			        // Get the NOD in the database
			        var nod = (DBDictionary) trans.GetObject(Current.nod, OpenMode.ForRead);

			        // Get the name to save
			        string name = StrRef + "n" + num + "d" + phi;

			        if (!nod.Contains(name))
			        {
				        // Save the variables on the Xrecord
				        using (ResultBuffer rb = new ResultBuffer())
				        {
					        rb.Add(new TypedValue((int) DxfCode.ExtendedDataRegAppName,  Current.appName)); // 0
					        rb.Add(new TypedValue((int) DxfCode.ExtendedDataAsciiString, name));            // 1
					        rb.Add(new TypedValue((int) DxfCode.ExtendedDataInteger32,   num));             // 2
					        rb.Add(new TypedValue((int) DxfCode.ExtendedDataReal,        phi));             // 3

					        // Create the entry in the NOD
					        Auxiliary.SaveObjectDictionary(name, rb);
				        }

				        trans.Commit();
			        }
		        }
	        }
        }

		// Read stringer reinforcement on database
		private static StringerReinforcement[] ReadStringerReinforcement()
		{
			// Create a list of reinforcement
			var refList = new List<StringerReinforcement>();

			// Start a transaction
			using (Transaction trans = Current.db.TransactionManager.StartTransaction())
			{
				// Get the NOD in the database
				var nod = (DBDictionary) trans.GetObject(Current.nod, OpenMode.ForRead);

				// Check saved reinforcements
				foreach (var entry in nod)
				{
					if (entry.Key.Contains(StrRef))
					{
						// Read data
						var refXrec = (Xrecord) trans.GetObject(entry.Value, OpenMode.ForRead);
						var refDAta = refXrec.Data.AsArray();

						int    num = Convert.ToInt32 (refDAta[2].Value);
						double phi = Convert.ToDouble(refDAta[3].Value);

						// Create new reinforcement
						var reinforcement = new StringerReinforcement(num, phi);

						// Add to the list
						refList.Add(reinforcement);
					}
				}
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
	        // Start a transaction
	        using (Transaction trans = Current.db.TransactionManager.StartTransaction())
	        {
		        // Get the NOD in the database
		        var nod = (DBDictionary) trans.GetObject(Current.nod, OpenMode.ForRead);

		        // Get the names to save
		        string name = PnlRef + "d" + barDiameter + "s" + spacing;

		        if (!nod.Contains(name))
		        {
			        // Save the variables on the Xrecord
			        using (ResultBuffer rb = new ResultBuffer())
			        {
				        rb.Add(new TypedValue((int) DxfCode.ExtendedDataRegAppName,  Current.appName)); // 0
				        rb.Add(new TypedValue((int) DxfCode.ExtendedDataAsciiString, name));            // 1
				        rb.Add(new TypedValue((int) DxfCode.ExtendedDataReal,        barDiameter));     // 2
				        rb.Add(new TypedValue((int) DxfCode.ExtendedDataReal,        spacing));         // 3

				        // Create the entry in the NOD
				        Auxiliary.SaveObjectDictionary(name, rb);
			        }

			        trans.Commit();
		        }
	        }
        }

        // Read panel reinforcement on database
        private static (double diameter, double spacing)[] ReadPanelReinforcement()
        {
	        // Create a list of reinforcement
	        var refList = new List<(double diameter, double spacing)>();

	        // Start a transaction
	        using (Transaction trans = Current.db.TransactionManager.StartTransaction())
	        {
		        // Get the NOD in the database
		        var nod = (DBDictionary)trans.GetObject(Current.nod, OpenMode.ForRead);

		        // Check saved reinforcements
		        foreach (var entry in nod)
		        {
			        if (entry.Key.Contains(PnlRef))
			        {
				        // Read data
				        var refXrec = (Xrecord)trans.GetObject(entry.Value, OpenMode.ForRead);
				        var refDAta = refXrec.Data.AsArray();

				        double
					        phi = Convert.ToDouble(refDAta[2].Value),
					        s   = Convert.ToDouble(refDAta[3].Value);

				        // Add to the list
				        refList.Add((phi, s));
			        }
		        }
	        }

	        if (refList.Count > 0)
		        return
			        refList.ToArray();

	        // None
	        return null;
        }

    }
}