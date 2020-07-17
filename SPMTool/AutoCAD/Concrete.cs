using System;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Material;
using SPMTool.UserInterface;
using ConcreteData       = SPMTool.XData.Concrete;
using AggregateType      = Material.Concrete.AggregateType;
using ConcreteParameters = Material.Concrete.Parameters;
using ConcreteBehavior   = Material.Concrete.Behavior;
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
	        Concrete.ParameterModel.MC2010.ToString(),
	        Concrete.ParameterModel.NBR6118.ToString(),
	        Concrete.ParameterModel.MCFT.ToString(),
	        Concrete.ParameterModel.DSFM.ToString(),
	        Concrete.ParameterModel.Custom.ToString()
        };

        // Behavior names
        public static readonly string[] Behaviors =
        {
			Concrete.BehaviorModel.MCFT.ToString(),
			Concrete.BehaviorModel.DSFM.ToString()
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
			// Definition for the Extended Data
			string xdataStr = "Concrete data";

			// Open the Registered Applications table and check if custom app exists. If it doesn't, then it's created:
			Auxiliary.RegisterApp();

			// Get units
			var units = Config.ReadUnits() ?? new Units();

			// Initiate default values
			double
				fc    = 30,
				phiAg = 20,
				ft    = 2,
				Ec    = 30000,
				ec    = 0.002,
				ecu   = 0.0035;


            // Read data
            var concreteData = ReadConcreteData();

            var parameters = concreteData?.parameters ?? new ConcreteParameters.MC2010(fc, phiAg);
            var behavior   = concreteData?.behavior ?? new ConcreteBehavior.MCFT(parameters);

			// Start the config window
			var concreteConfig = new ConcreteConfig(parameters, behavior, units);
			Application.ShowModalWindow(Application.MainWindow.Handle, concreteConfig, false);
		}

        public static void SaveConcreteParameters(ConcreteParameters parameters, Concrete.BehaviorModel behaviorModel)
		{
			// Definition for the Extended Data
			string xdataStr = "Concrete data";

            // Read the parameter model
            var parModel = (Concrete.ParameterModel)Enum.Parse(typeof(Concrete.ParameterModel), parameters.GetType().Name);

			// Get the Xdata size
			int size = Enum.GetNames(typeof(ConcreteData)).Length;
			var data = new TypedValue[size];

			data[(int)ConcreteData.AppName]  = new TypedValue((int)DxfCode.ExtendedDataRegAppName,  Current.appName);
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

        [CommandMethod("ViewConcreteParameters")]
		public static void ViewConcreteParameters()
		{
			// Get the values
			var concrete = ReadConcreteData();

			string concmsg;

            // Write the concrete parameters
            if (concrete.HasValue)
            {
	            var par = concrete.Value.parameters;

				// Get units
				var units = Config.ReadUnits();
				IQuantity
					fc    = Pressure.FromMegapascals(par.Strength),
					ft    = Pressure.FromMegapascals(par.TensileStrength),
					Ec    = Pressure.FromMegapascals(par.InitialModule),
					phiAg = Length.FromMillimeters(par.AggregateDiameter);

				char
					phi = (char)Characters.Phi,
					eps = (char)Characters.Epsilon;

				concmsg =
					"Concrete Parameters:\n" +
					"\nfc = " + fc.ToUnit(units.MaterialStrength) +
					"\nft = " + ft.ToUnit(units.MaterialStrength) +
					"\nEc = " + Ec.ToUnit(units.MaterialStrength) +
					"\n" + eps + "c = " + Math.Round(1000 * par.PlasticStrain, 2) + " E-03" +
					"\n" + eps + "cu = " + Math.Round(1000 * par.UltimateStrain, 2) + " E-03" +
					"\n" + phi + ",ag = " + phiAg.ToUnit(units.Reinforcement);
            }
			else
	            concmsg = "Concrete parameters not set";

			// Display the values returned
			Application.ShowAlertDialog(Current.appName + "\n\n" + concmsg);
        }

        // Read the concrete parameters
        public static (ConcreteParameters parameters, ConcreteBehavior behavior)? ReadConcreteData()
		{
			var data = Auxiliary.ReadDictionaryEntry(ConcreteParams);

			if (data is null)
				return null;

			// Get the parameters from XData
			var par      = (Concrete.ParameterModel)Convert.ToInt32(data[(int) ConcreteData.Model].Value);
			var bhModel  = (Concrete.BehaviorModel)Convert.ToInt32(data[(int) ConcreteData.Behavior].Value);
			var aggType  = (AggregateType)Convert.ToInt32(data[(int) ConcreteData.AggType].Value);

			double
                fc      = Convert.ToDouble(data[(int)ConcreteData.fc].Value),
				phiAg   = Convert.ToDouble(data[(int)ConcreteData.AggDiam].Value);

			// Verify which parameters are set
			ConcreteParameters parameters = null;
			ConcreteBehavior   behavior   = null;

			// Get parameters
			switch (par)
			{
                case Concrete.ParameterModel.MC2010:
					parameters = new ConcreteParameters.MC2010(fc, phiAg, aggType);
					break;

                case Concrete.ParameterModel.NBR6118:
					parameters = new ConcreteParameters.NBR6118(fc, phiAg, aggType);
					break;

                case Concrete.ParameterModel.MCFT:
					parameters = new ConcreteParameters.MCFT(fc, phiAg, aggType);
					break;

                case Concrete.ParameterModel.DSFM:
					parameters = new ConcreteParameters.DSFM(fc, phiAg, aggType);
					break;

                case Concrete.ParameterModel.Custom:
	                // Get additional parameters
	                double
		                fcr = Convert.ToDouble(data[(int)ConcreteData.ft].Value),
		                Ec  = Convert.ToDouble(data[(int)ConcreteData.Ec].Value),
		                ec  = -Convert.ToDouble(data[(int)ConcreteData.ec].Value),
		                ecu = -Convert.ToDouble(data[(int)ConcreteData.ecu].Value);

	                parameters = new ConcreteParameters.Custom(fc, phiAg, fcr, Ec, ec, ecu);
                    break;
            }

			// Get behavior
			switch (bhModel)
			{
                case Concrete.BehaviorModel.Linear:
					break;

                case Concrete.BehaviorModel.MCFT:
					behavior = new ConcreteBehavior.MCFT(parameters);
					break;

                case Concrete.BehaviorModel.DSFM:
					behavior = new ConcreteBehavior.DSFM(parameters);
					break;
			}

			return
				(parameters, behavior);
		}
    }
}
