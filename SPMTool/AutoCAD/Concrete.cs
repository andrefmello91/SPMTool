using System;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Material;
using Material.Concrete;
using SPMTool.Database;
using SPMTool.UserInterface;
using ConcreteData       = SPMTool.XData.Concrete;
using UnitsNet;
using UnitsNet.Units;

[assembly: CommandClass(typeof(SPMTool.AutoCAD.Material))]

namespace SPMTool.AutoCAD
{
	// Concrete
	public static partial class Material
	{
		private static readonly string ConcreteParams = "ConcreteParams";

        // Model names
        public static readonly string[] Models =
        {
	        ParameterModel.MC2010.ToString(),
	        ParameterModel.NBR6118.ToString(),
	        ParameterModel.MCFT.ToString(),
	        ParameterModel.DSFM.ToString(),
	        ParameterModel.Custom.ToString()
        };

        // Behavior names
        public static readonly string[] Behaviors =
        {
			ConstitutiveModel.MCFT.ToString(),
			ConstitutiveModel.DSFM.ToString()
        };

        // Aggregate types
        public static readonly string[] AggregateTypes =
        {
	        AggregateType.Basalt.ToString(),
	        AggregateType.Quartzite.ToString(),
	        AggregateType.Limestone.ToString(),
	        AggregateType.Sandstone.ToString()
        };

		[CommandMethod("SetConcreteParameters")]
		public static void SetConcreteParameters()
		{
			// Read data
            var concrete = ReadConcreteData(false);

			// Start the config window
			var concreteConfig = new ConcreteConfig(concrete);
			Application.ShowModalWindow(Application.MainWindow.Handle, concreteConfig, false);
		}

        public static void SaveConcreteParameters(Parameters parameters, ConstitutiveModel behaviorModel)
		{
			// Definition for the Extended Data
			string xdataStr = "Concrete data";

            // Read the parameter model
            var parModel = Parameters.ReadParameterModel(parameters);

			// Get the Xdata size
			int size = Enum.GetNames(typeof(ConcreteData)).Length;
			var data = new TypedValue[size];

			data[(int)ConcreteData.AppName]  = new TypedValue((int)DxfCode.ExtendedDataRegAppName,  DataBase.AppName);
			data[(int)ConcreteData.XDataStr] = new TypedValue((int)DxfCode.ExtendedDataAsciiString, xdataStr);
			data[(int)ConcreteData.Model]    = new TypedValue((int)DxfCode.ExtendedDataInteger32,   (int)parModel);
			data[(int)ConcreteData.Behavior] = new TypedValue((int)DxfCode.ExtendedDataInteger32,   (int)behaviorModel);
			data[(int)ConcreteData.fc]       = new TypedValue((int)DxfCode.ExtendedDataReal,        parameters.Strength);
			data[(int)ConcreteData.AggType]  = new TypedValue((int)DxfCode.ExtendedDataInteger32,   (int)parameters.Type);
			data[(int)ConcreteData.AggDiam]  = new TypedValue((int)DxfCode.ExtendedDataReal,        parameters.AggregateDiameter);
			data[(int)ConcreteData.ft]       = new TypedValue((int)DxfCode.ExtendedDataReal,        parameters.TensileStrength);
			data[(int)ConcreteData.Ec]       = new TypedValue((int)DxfCode.ExtendedDataReal,        parameters.InitialModule);
			data[(int)ConcreteData.ec]       = new TypedValue((int)DxfCode.ExtendedDataReal,        parameters.PlasticStrain);
			data[(int)ConcreteData.ecu]      = new TypedValue((int)DxfCode.ExtendedDataReal,        parameters.UltimateStrain);

			// Create the entry in the NOD and add to the transaction
			Auxiliary.SaveObjectDictionary(ConcreteParams, new ResultBuffer(data));
		}

  //      [CommandMethod("ViewConcreteParameters")]
		//public static void ViewConcreteParameters()
		//{
		//	// Get the values
		//	var concrete = ReadConcreteData();

		//	string concmsg;

  //          // Write the concrete parameters
  //          if (concrete.HasValue)
  //          {
	 //           var par = concrete.Value.parameters;

		//		// Get units
		//		var units = Config.ReadUnits();
		//		IQuantity
		//			fc    = Pressure.FromMegapascals(par.Strength),
		//			ft    = Pressure.FromMegapascals(par.TensileStrength),
		//			Ec    = Pressure.FromMegapascals(par.InitialModule),
		//			phiAg = Length.FromMillimeters(par.AggregateDiameter);

		//		char
		//			phi = (char)Characters.Phi,
		//			eps = (char)Characters.Epsilon;

		//		concmsg =
		//			"Concrete Parameters:\n" +
		//			"\nfc = " + fc.ToUnit(units.MaterialStrength) +
		//			"\nft = " + ft.ToUnit(units.MaterialStrength) +
		//			"\nEc = " + Ec.ToUnit(units.MaterialStrength) +
		//			"\n" + eps + "c = "  + $"{1000 * par.PlasticStrain,0:00}" + " E-03" +
		//			"\n" + eps + "cu = " + $"{1000 * par.UltimateStrain,0:00}" + " E-03" +
		//			"\n" + phi + ",ag = " + phiAg.ToUnit(units.Reinforcement);
  //          }
		//	else
	 //           concmsg = "Concrete parameters not set";

		//	// Display the values returned
		//	Application.ShowAlertDialog(DataBase.AppName + "\n\n" + concmsg);
  //      }

        // Read the concrete parameters

		/// <summary>
        /// Read concrete saved in database.
        /// </summary>
        /// <param name="setConcrete">Concrete must be set by user?</param>
        public static Concrete ReadConcreteData(bool setConcrete = true)
		{
			var data = Auxiliary.ReadDictionaryEntry(ConcreteParams);

			if (data is null)
			{
				if (setConcrete)
					SetConcreteParameters();

				else
					return null;
			}

			// Get the parameters from XData
			var parModel    = (ParameterModel)Convert.ToInt32(data[(int) ConcreteData.Model].Value);
			var constModel  = (ConstitutiveModel)Convert.ToInt32(data[(int) ConcreteData.Behavior].Value);
			var aggType     = (AggregateType)Convert.ToInt32(data[(int) ConcreteData.AggType].Value);

			double
                fc      = Convert.ToDouble(data[(int)ConcreteData.fc].Value),
				phiAg   = Convert.ToDouble(data[(int)ConcreteData.AggDiam].Value), 
                
                // Get additional parameters
                fcr =  Convert.ToDouble(data[(int)ConcreteData.ft].Value),
				Ec  =  Convert.ToDouble(data[(int)ConcreteData.Ec].Value),
				ec  = -Convert.ToDouble(data[(int)ConcreteData.ec].Value),
				ecu = -Convert.ToDouble(data[(int)ConcreteData.ecu].Value);

            // Get parameters and constitutive
            var parameters = Parameters.ReadParameters(parModel, fc, phiAg, aggType, fcr, Ec, ec, ecu);

			return new Concrete(parameters, constModel);
		}
    }
}
