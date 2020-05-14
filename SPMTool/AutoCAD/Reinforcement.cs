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
		// Database string configurations
	    private static string StringerReinforcement = "StringerReinforcement";
	    private static string Steel                 = "Steel";

        [CommandMethod("SetStringerReinforcement")]
        public static void SetStringerReinforcement()
        {
	        // Request objects to be selected in the drawing area
	        var selOp = new PromptSelectionOptions()
	        {
		        MessageForAdding = "Select the stringers to assign reinforcement (you can select other elements, the properties will be only applied to stringers)."
	        };

	        //Current.edtr.WriteMessage(
	        //    "\nSelect the stringers to assign reinforcement (you can select other elements, the properties will be only applied to stringers).");

	        PromptSelectionResult selRes = Current.edtr.GetSelection(selOp);

	        // If the prompt status is OK, objects were selected
	        if (selRes.Status == PromptStatus.Cancel)
		        return;

	        SelectionSet set = selRes.Value;

            // Get steel parameters and reinforcement from user
            var reinforcement = GetStringerReinforcement();
            var steel         = GetSteel();

            if (steel != null && reinforcement != null)
            {
	            // Get the values
	            int nBars = reinforcement.NumberOfBars;
	            double
		            phi = reinforcement.BarDiameter,
		            fy  = steel.YieldStress,
		            Es  = steel.ElasticModule;

	            // Start a transaction
	            using (Transaction trans = Current.db.TransactionManager.StartTransaction())
	            {
		            // Save the properties
		            foreach (SelectedObject obj in set)
		            {
			            // Open the selected object for read
			            Entity ent = trans.GetObject(obj.ObjectId, OpenMode.ForRead) as Entity;

			            // Check if the selected object is a node
			            if (ent.Layer == Geometry.Stringer.StringerLayer)
			            {
				            // Upgrade the OpenMode
				            ent.UpgradeOpen();

				            // Access the XData as an array
				            ResultBuffer rb = ent.GetXDataForApplication(Current.appName);
				            TypedValue[] data = rb.AsArray();

				            // Set the new reinforcement
				            data[(int) XData.Stringer.NumOfBars] =
					            new TypedValue((int) DxfCode.ExtendedDataInteger32, nBars);
				            data[(int) XData.Stringer.BarDiam]   =
					            new TypedValue((int) DxfCode.ExtendedDataReal,      phi);
				            data[(int) XData.Stringer.Steelfy]   =
					            new TypedValue((int) DxfCode.ExtendedDataReal,      fy);
				            data[(int) XData.Stringer.SteelEs]   =
					            new TypedValue((int) DxfCode.ExtendedDataReal,      Es);

				            // Add the new XData
				            ent.XData = new ResultBuffer(data);
			            }
		            }

		            // Save the new object to the database
		            trans.Commit();
	            }
            }
        }

		// Get reinforcement parameters from user
		private static StringerReinforcement GetStringerReinforcement()
		{
            // Initiate values
            StringerReinforcement reinforcement = null;
            int    num = 2;
            double phi = 10;
            bool newRef = false;

			// Get saved reinforcement options
			var savedRef = ReadReinforcement();

			// Get saved reinforcement options
			if (savedRef != null)
			{
				string[] keywords = new string[savedRef.Length];

                // Ask the user to choose the options
                PromptKeywordOptions options = new PromptKeywordOptions("Choose a reinforcement option or add a new one:")
                {
					AllowNone = false
                };

				// Get the options
				for (int i = 0; i < savedRef.Length; i++)
				{
					int    n = savedRef[i].NumberOfBars;
					double d = savedRef[i].BarDiameter;

					string name = n + " ∅" + d + " mm";

					options.Keywords.Add(name);
					keywords[i] = name;
				}

				// Add option to set new reinforcement
				options.Keywords.Add("New");

				PromptResult result = Current.edtr.GetKeywords(options);

				if (result.Status == PromptStatus.Cancel)
					return null;

				// Get string result
				string res = result.StringResult;
				
				// Get the index
				if (res != "New")
				{
					for (int i = 0; i < keywords.Length; i++)
					{
						if (options.Keywords[i].Enabled)
							reinforcement = savedRef[i];
					}
                }
                else
                    newRef = true;
            }

			else
				newRef = true;

            if (newRef)
			{
				// Ask the user to input the number of bars
				PromptIntegerOptions nBarsOp =
					new PromptIntegerOptions(
						"\nInput the number of Stringer reinforcement bars (only needed for nonlinear analysis):")
					{
						DefaultValue  = num,
						AllowNegative = false
					};

				// Get the result
				PromptIntegerResult nBarsRes = Current.edtr.GetInteger(nBarsOp);

				if (nBarsRes.Status == PromptStatus.Cancel)
					return null;

				num = nBarsRes.Value;

				// Ask the user to input the Stringer height
				PromptDoubleOptions phiOp =
					new PromptDoubleOptions("\nInput the diameter (in mm) of Stringer reinforcement bars:")
					{
						DefaultValue  = phi,
						AllowNegative = false
					};

				// Get the result
				PromptDoubleResult phiRes = AutoCAD.Current.edtr.GetDouble(phiOp);

				if (phiRes.Status == PromptStatus.Cancel)
					return null;

				phi = phiRes.Value;

				reinforcement = new StringerReinforcement(num, phi);
			}

            // Save the reinforcement
			SaveStringerReinforcement(reinforcement, savedRef);

            return reinforcement;
		}

        // Get steel parameters from user
        private static Steel GetSteel()
		{
			// Initiate values
			Steel steel = null;
			double
				fy = 500,
				Es = 210000;
			bool newSteel = false;

			// Get steel data saved on database
			var savedSteel = ReadSteel();

			// Get saved reinforcement options
			if (savedSteel != null)
			{
				string[] keywords = new string[savedSteel.Length];

				// Ask the user to choose the options
				PromptKeywordOptions options = new PromptKeywordOptions("Choose a steel option or add a new one:");

				// Get the options
				for (int i = 0; i < savedSteel.Length; i++)
				{
					double
						f = savedSteel[i].YieldStress,
						E = savedSteel[i].ElasticModule;

					string name = "fy = " + f + " MPa, Es = " + E + " MPa";

					options.Keywords.Add(name);
					keywords[i] = name;
				}

				// Add option to set new reinforcement
				options.Keywords.Add("New");

				PromptResult result = Current.edtr.GetKeywords(options);

				if (result.Status == PromptStatus.Cancel)
					return null;

				// Get string result
				string res = result.StringResult;

				// Get the index
				if (res != "New")
				{
					for (int i = 0; i < keywords.Length; i++)
					{
						if (options.Keywords[i].Enabled)
							steel = savedSteel[i];
					}
                }
                else
                    newSteel = true;
            }

            else
                newSteel = true;

            // If it's a new steel
            if (newSteel)
			{
				// Ask the user to input the Steel yield strength
				PromptDoubleOptions fyOp =
					new PromptDoubleOptions("\nInput the yield strength (MPa) of Steel:")
					{
						DefaultValue  = fy,
						AllowNegative = false
					};

				// Get the result
				PromptDoubleResult fyRes = Current.edtr.GetDouble(fyOp);

				if (fyRes.Status == PromptStatus.Cancel)
					return null;

				fy = fyRes.Value;

				// Ask the user to input the Steel elastic modulus
				PromptDoubleOptions EsOp =
					new PromptDoubleOptions("\nInput the elastic modulus (MPa) of Steel:")
					{
						DefaultValue  = Es,
						AllowNegative = false
					};

				// Get the result
				PromptDoubleResult EsRes = Current.edtr.GetDouble(EsOp);

				if (EsRes.Status == PromptStatus.Cancel)
					return null;

				Es = EsRes.Value;

				steel = new Steel(fy, Es);
			}

			// Save steel
			SaveSteel(steel, savedSteel);

			return steel;
        }

        [CommandMethod("SetPanelReinforcement")]
        public static void SetPanelReinforcement()
        {
            // Start a transaction
            using (Transaction trans = AutoCAD.Current.db.TransactionManager.StartTransaction())
            {
                // Request objects to be selected in the drawing area
                Current.edtr.WriteMessage(
                    "\nSelect the panels to assign reinforcement (you can select other elements, the properties will be only applied to elements with 'Panel' layer activated).");
                PromptSelectionResult selRes = Current.edtr.GetSelection();

                // If the prompt status is OK, objects were selected
                if (selRes.Status == PromptStatus.Cancel)
                    return;

                // Get the selection
                SelectionSet set = selRes.Value;

                // Ask the user to input the diameter of bars
                PromptDoubleOptions phiXOp =
                    new PromptDoubleOptions(
                        "\nInput the reinforcement bar diameter (in mm) for the X direction for selected panels (only needed for nonlinear analysis):")
                    {
                        DefaultValue = 0,
                        AllowNegative = false
                    };

                // Get the result
                PromptDoubleResult phiXRes = AutoCAD.Current.edtr.GetDouble(phiXOp);

                if (phiXRes.Status == PromptStatus.Cancel)
                    return;

                double phiX = phiXRes.Value;

                // Ask the user to input the bar spacing
                PromptDoubleOptions sxOp =
                    new PromptDoubleOptions("\nInput the bar spacing (in mm) for the X direction:")
                    {
                        DefaultValue = 0,
                        AllowNegative = false
                    };

                // Get the result
                PromptDoubleResult sxRes = AutoCAD.Current.edtr.GetDouble(sxOp);

                if (sxRes.Status == PromptStatus.Cancel)
                    return;

                double sx = sxRes.Value;

                // Ask the user to input the Steel yield strength
                PromptDoubleOptions fyxOp =
                    new PromptDoubleOptions(
                        "\nInput the yield strength (MPa) of panel reinforcement bars in X direction:")
                    {
                        DefaultValue = 500,
                        AllowNegative = false
                    };

                // Get the result
                PromptDoubleResult fyxRes = AutoCAD.Current.edtr.GetDouble(fyxOp);

                if (fyxRes.Status == PromptStatus.Cancel)
                    return;

                double fyx = fyxRes.Value;

                // Ask the user to input the Steel elastic modulus
                PromptDoubleOptions EsxOp =
                    new PromptDoubleOptions(
                        "\nInput the elastic modulus (MPa) of panel reinforcement bars in X direction:")
                    {
                        DefaultValue = 210000,
                        AllowNegative = false
                    };

                // Get the result
                PromptDoubleResult EsxRes = AutoCAD.Current.edtr.GetDouble(EsxOp);

                if (EsxRes.Status == PromptStatus.Cancel)
                    return;

                double Esx = EsxRes.Value;


                // Ask the user to input the diameter of bars
                PromptDoubleOptions phiYOp =
                    new PromptDoubleOptions(
                        "\nInput the reinforcement bar diameter (in mm) for the Y direction for selected panels (only needed for nonlinear analysis):")
                    {
                        DefaultValue = phiX,
                        AllowNegative = false
                    };

                // Get the result
                PromptDoubleResult phiYRes = Current.edtr.GetDouble(phiYOp);

                if (phiYRes.Status == PromptStatus.Cancel)
                    return;

                double phiY = phiYRes.Value;

                // Ask the user to input the bar spacing
                PromptDoubleOptions syOp =
                    new PromptDoubleOptions("\nInput the bar spacing (in mm) for the Y direction:")
                    {
                        DefaultValue = sx,
                        AllowNegative = false
                    };

                // Get the result
                PromptDoubleResult syRes = Current.edtr.GetDouble(syOp);

                if (syRes.Status == PromptStatus.Cancel)
                    return;

                double sy = syRes.Value;

                // Ask the user to input the Steel yield strength
                PromptDoubleOptions fyyOp =
                    new PromptDoubleOptions(
                        "\nInput the yield strength (MPa) of panel reinforcement bars in Y direction:")
                    {
                        DefaultValue = fyx,
                        AllowNegative = false
                    };

                // Get the result
                PromptDoubleResult fyyRes = Current.edtr.GetDouble(fyyOp);

                if (fyyRes.Status == PromptStatus.Cancel)
                    return;

                double fyy = fyyRes.Value;

                // Ask the user to input the Steel elastic modulus
                PromptDoubleOptions EsyOp =
                    new PromptDoubleOptions(
                        "\nInput the elastic modulus (MPa) of panel reinforcement bars in X direction:")
                    {
                        DefaultValue = Esx,
                        AllowNegative = false
                    };

                // Get the result
                PromptDoubleResult EsyRes = AutoCAD.Current.edtr.GetDouble(EsyOp);

                if (EsyRes.Status == PromptStatus.Cancel)
                    return;

                double Esy = EsyRes.Value;

                foreach (SelectedObject obj in set)
                {
                    // Open the selected object for read
                    Entity ent = trans.GetObject(obj.ObjectId, OpenMode.ForRead) as Entity;

                    // Check if the selected object is a node
                    if (ent.Layer == Geometry.Panel.PanelLayer)
                    {
                        // Upgrade the OpenMode
                        ent.UpgradeOpen();

                        // Access the XData as an array
                        ResultBuffer rb = ent.GetXDataForApplication(AutoCAD.Current.appName);
                        TypedValue[] data = rb.AsArray();

                        // Set the new geometry and reinforcement (line 7 to 9 of the array)
                        data[(int) XData.Panel.XDiam] = new TypedValue((int) DxfCode.ExtendedDataReal, phiX);
                        data[(int) XData.Panel.Sx]    = new TypedValue((int) DxfCode.ExtendedDataReal, sx);
                        data[(int) XData.Panel.fyx]   = new TypedValue((int) DxfCode.ExtendedDataReal, fyx);
                        data[(int) XData.Panel.Esx]   = new TypedValue((int) DxfCode.ExtendedDataReal, Esx);
                        data[(int) XData.Panel.YDiam] = new TypedValue((int) DxfCode.ExtendedDataReal, phiY);
                        data[(int) XData.Panel.Sy]    = new TypedValue((int) DxfCode.ExtendedDataReal, sy);
                        data[(int) XData.Panel.fyy]   = new TypedValue((int) DxfCode.ExtendedDataReal, fyy);
                        data[(int) XData.Panel.Esy]   = new TypedValue((int) DxfCode.ExtendedDataReal, Esy);

                        // Add the new XData
                        ent.XData = new ResultBuffer(data);
                    }
                }

                // Save the new object to the database
                trans.Commit();
            }
        }

        // Save steel configuration on database
        private static void SaveSteel(Steel steel, Steel[] savedSteel)
        {
	        if (steel != null)
	        {
		        bool contains = false;

		        if (savedSteel != null)
		        {
			        foreach (var sSt in savedSteel)
			        {
				        if (steel.YieldStress == sSt.YieldStress && steel.ElasticModule == sSt.ElasticModule)
				        {
					        contains = true;
					        break;
				        }
			        }
		        }

		        if (savedSteel == null || !contains)
		        {
			        // Get data
			        double
				        fy = steel.YieldStress,
				        Es = steel.ElasticModule;

			        // Start a transaction
			        using (Transaction trans = Current.db.TransactionManager.StartTransaction())
			        {
				        // Get the NOD in the database
				        var nod = (DBDictionary) trans.GetObject(Current.db.NamedObjectsDictionaryId,
					        OpenMode.ForWrite);

				        // Read the configurations saved and get the number to save config
				        int i = 0;

				        foreach (var entry in nod)
					        if (entry.Key.Contains(Steel))
						        i++;

				        // Get the name to save
				        string name = Steel + i;

				        // Save the variables on the Xrecord
				        using (ResultBuffer rb = new ResultBuffer())
				        {
					        rb.Add(new TypedValue((int) DxfCode.ExtendedDataRegAppName, Current.appName)); // 0
					        rb.Add(new TypedValue((int) DxfCode.ExtendedDataAsciiString, name));           // 1
					        rb.Add(new TypedValue((int) DxfCode.ExtendedDataReal, fy));                    // 2
					        rb.Add(new TypedValue((int) DxfCode.ExtendedDataReal, Es));                    // 3

					        // Create and add data to an Xrecord
					        Xrecord xRec = new Xrecord();
					        xRec.Data = rb;

					        // Create the entry in the NOD and add to the transaction
					        nod.SetAt(name, xRec);
					        trans.AddNewlyCreatedDBObject(xRec, true);
				        }

				        // Save the new object to the database
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
		        var nod = (DBDictionary) trans.GetObject(Current.db.NamedObjectsDictionaryId, OpenMode.ForRead);

		        // Check saved reinforcements
		        foreach (var entry in nod)
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

	        if (stList.Count > 0)
		        return
			        stList.ToArray();

	        // None
	        return null;
        }

        // Save reinforcement configuration on database
        private static void SaveStringerReinforcement(StringerReinforcement reinforcement, StringerReinforcement[] savedReinforcement)
        {
	        if (reinforcement != null)
	        {
		        bool contains = false;

		        if (savedReinforcement != null)
		        {
			        foreach (var sRef in savedReinforcement)
			        {
				        if (reinforcement.NumberOfBars == sRef.NumberOfBars &&
				            reinforcement.BarDiameter == sRef.BarDiameter)
				        {
					        contains = true;
					        break;
				        }
			        }
		        }

		        if (savedReinforcement == null || !contains)
		        {
			        // Get data
			        int    num = reinforcement.NumberOfBars;
			        double phi = reinforcement.BarDiameter;

			        // Start a transaction
			        using (Transaction trans = Current.db.TransactionManager.StartTransaction())
			        {
				        // Get the NOD in the database
				        var nod = (DBDictionary) trans.GetObject(Current.db.NamedObjectsDictionaryId,
					        OpenMode.ForWrite);

				        // Read the configurations saved and get the number to save config
				        int i = 0;

				        foreach (var entry in nod)
					        if (entry.Key.Contains(StringerReinforcement))
						        i++;

				        // Get the name to save
				        string name = StringerReinforcement + i;

				        // Save the variables on the Xrecord
				        using (ResultBuffer rb = new ResultBuffer())
				        {
					        rb.Add(new TypedValue((int) DxfCode.ExtendedDataRegAppName,  Current.appName)); // 0
					        rb.Add(new TypedValue((int) DxfCode.ExtendedDataAsciiString, name));            // 1
					        rb.Add(new TypedValue((int) DxfCode.ExtendedDataInteger32,   num));             // 2
					        rb.Add(new TypedValue((int) DxfCode.ExtendedDataReal,        phi));             // 3

					        // Create and add data to an Xrecord
					        Xrecord xRec = new Xrecord();
					        xRec.Data = rb;

					        // Create the entry in the NOD and add to the transaction
					        nod.SetAt(name, xRec);
					        trans.AddNewlyCreatedDBObject(xRec, true);
				        }

				        // Save the new object to the database
				        trans.Commit();
			        }
		        }
	        }
        }

		// Read stringer reinforcement on database
		private static StringerReinforcement[] ReadReinforcement()
		{
			// Create a list of reinforcement
			var refList = new List<StringerReinforcement>();

			// Start a transaction
			using (Transaction trans = Current.db.TransactionManager.StartTransaction())
			{
				// Get the NOD in the database
				var nod = (DBDictionary) trans.GetObject(Current.db.NamedObjectsDictionaryId, OpenMode.ForRead);

				// Check saved reinforcements
				foreach (var entry in nod)
					if (entry.Key.Contains(StringerReinforcement))
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

			if (refList.Count > 0)
				return 
					refList.ToArray();

			// None
			return null;
        }
    }
}