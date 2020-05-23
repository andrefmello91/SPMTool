using System;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using SPMTool.Material;
using AggregateType = SPMTool.Material.Concrete.AggregateType;

[assembly: CommandClass(typeof(SPMTool.AutoCAD.Material))]

namespace SPMTool.AutoCAD
{
	// Concrete
	public static partial class Material
	{
		private static readonly string ConcreteParams = "ConcreteParams";

		// Aggregate type names
		private static readonly string
			Basalt    = AggregateType.Basalt.ToString(),
			Quartzite = AggregateType.Quartzite.ToString(),
			Limestone = AggregateType.Limestone.ToString(),
			Sandstone = AggregateType.Sandstone.ToString();

		[CommandMethod("SetConcreteParameters")]
		public static void SetConcreteParameters()
		{
			// Definition for the Extended Data
			string xdataStr = "Concrete data";

			// Open the Registered Applications table and check if custom app exists. If it doesn't, then it's created:
			Auxiliary.RegisterApp();

			// Initiate default values
			double
				fc    = 30,
				phiAg = 20;

			string agrgt = Quartzite;

			// Read data
			var concrete = ReadConcreteData();

			if (concrete != null)
			{
				fc    = concrete.Strength;
				phiAg = concrete.AggregateDiameter;
				agrgt = concrete.Type.ToString();
			}

			// Ask the user to input the concrete compressive strength
			var fcn = UserInput.GetDouble("Input the concrete mean compressive strength (fcm) in MPa:", fc);

			if (!fcn.HasValue)
				return;

			// Ask the user choose the type of the aggregate
			var options = new[]
			{
				Basalt,
				Quartzite,
				Limestone,
				Sandstone
			};

			var agn = UserInput.SelectKeyword("Choose the type of the aggregate", options, agrgt);

			if (!agn.HasValue)
				return;

            // Ask the user to input the maximum aggregate diameter
            var phin = UserInput.GetDouble("Input the maximum diameter for concrete aggregate:", phiAg);

			if (!phin.HasValue)
				return;

            // Get values
            fc    = fcn.Value;
            agrgt = agn.Value.keyword;
            phiAg = phin.Value;
            var aggType = (AggregateType) Enum.Parse(typeof(AggregateType), agrgt);

            // Save the variables on the Xrecord
            using (ResultBuffer rb = new ResultBuffer())
            {
	            rb.Add(new TypedValue((int)DxfCode.ExtendedDataRegAppName,  Current.appName));    // 0
	            rb.Add(new TypedValue((int)DxfCode.ExtendedDataAsciiString, xdataStr));           // 1
	            rb.Add(new TypedValue((int)DxfCode.ExtendedDataReal,        fc));                 // 2
	            rb.Add(new TypedValue((int)DxfCode.ExtendedDataInteger32,  (int)aggType));        // 3
	            rb.Add(new TypedValue((int)DxfCode.ExtendedDataReal,        phiAg));              // 4

	            // Create the entry in the NOD and add to the transaction
	            Auxiliary.SaveObjectDictionary(ConcreteParams, rb);
            }
		}

		[CommandMethod("ViewConcreteParameters")]
		public static void ViewConcreteParameters()
		{
			// Get the values
			var concrete = ReadConcreteData();

			string concmsg;
            // Write the concrete parameters
            if (concrete != null)
            {
	            // Get the parameters
	            concmsg =
		            "\nConcrete Parameters:\n"                                    +
		            "\nfcm = "    + concrete.Strength                   + " MPa"  +
		            "\nfctm = "   + Math.Round(concrete.fcr, 2)         + " MPa"  +
		            "\nEci = "    + Math.Round(concrete.Ec, 2)          + " MPa"  +
		            "\nεc1 = "    + Math.Round(1000 * concrete.ec, 2)   + " E-03" +
		            "\nεc,lim = " + Math.Round(1000 * concrete.ecu, 2)  + " E-03" +
		            "\nφ,ag = "   + concrete.AggregateDiameter          + " mm";

            }
            else
	            concmsg = "Concrete parameters not set";

			// Display the values returned
			Application.ShowAlertDialog(Current.appName + "\n\n" + concmsg);
        }

        // Read the concrete parameters
        public static Concrete ReadConcreteData()
		{
			var concData = Auxiliary.ReadDictionaryEntry(ConcreteParams);

			if (concData == null)
				return null;

			// Get the parameters from XData
			double
				fc      = Convert.ToDouble(concData[2].Value),
				aggType = Convert.ToInt32 (concData[3].Value),
				phiAg   = Convert.ToDouble(concData[4].Value);

			return
				new Concrete(fc, phiAg, (AggregateType)aggType);
		}
	}
}
