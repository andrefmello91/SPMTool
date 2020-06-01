using System;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using SPMTool.Material;
using ConcreteData  = SPMTool.XData.Concrete;
using AggregateType = SPMTool.Material.Concrete.AggregateType;
using Model         = SPMTool.Material.Concrete.Model;

[assembly: CommandClass(typeof(SPMTool.AutoCAD.Material))]

namespace SPMTool.AutoCAD
{
	// Concrete
	public static partial class Material
	{
		private static readonly string ConcreteParams = "ConcreteParams";

		// Aggregate type and model names
		private static readonly string
			Basalt    = AggregateType.Basalt.ToString(),
			Quartzite = AggregateType.Quartzite.ToString(),
			Limestone = AggregateType.Limestone.ToString(),
			Sandstone = AggregateType.Sandstone.ToString(),
			MC2010    = Model.MC2010.ToString(),
			NBR6118   = Model.NBR6118.ToString(),
			Custom    = Model.Custom.ToString();

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
				phiAg = 20,
				fcr   = 2,
				Ec    = 30000,
				ec    = 0.002,
				ecu   = 0.0035;

			string
				model = MC2010,
				agrgt = Quartzite;

            // Read data
            var concrete = ReadConcreteData();

            if (concrete != null)
            {
                fc    = concrete.fc;
                phiAg = concrete.AggregateDiameter;
                fcr   = concrete.fcr;
                Ec    = concrete.Ec;
                ec    = -concrete.ec;
                ecu   = -concrete.ecu;
                model = concrete.ConcreteModel.ToString();
                agrgt = concrete.Type.ToString();
            }

            // Ask the user choose concrete model parameters
            var modelOptions = new[]
			{
				MC2010,
				NBR6118,
				Custom
			};

			var mOp = UserInput.SelectKeyword("Choose model of concrete parameters:", modelOptions, model);

			if (!mOp.HasValue)
				return;

            // Ask the user to input the concrete compressive strength
            var fcn = UserInput.GetDouble("Input concrete compressive strength (fc) in MPa:", fc);

			if (!fcn.HasValue)
				return;

			// Ask the user choose the type of the aggregate
			var agOptions = new[]
			{
				Basalt,
				Quartzite,
				Limestone,
				Sandstone
			};

			var agn = UserInput.SelectKeyword("Choose the type of the aggregate", agOptions, agrgt);

			if (!agn.HasValue)
				return;

            // Ask the user to input the maximum aggregate diameter
            var phin = UserInput.GetDouble("Input the maximum diameter for concrete aggregate:", phiAg);

			if (!phin.HasValue)
				return;

            // Get values
            model = mOp.Value.keyword;
            fc    = fcn.Value;
            agrgt = agn.Value.keyword;
            phiAg = phin.Value;
            var aggType   = (AggregateType) Enum.Parse(typeof(AggregateType), agrgt);
            var modelType = (Model) Enum.Parse(typeof(Model), model);

			// Get custom values from user
            if (modelType == Model.Custom)
            {
	            var fcrn = UserInput.GetDouble("Input concrete tensile strength (fcr) in MPa:", fcr);

	            if (!fcrn.HasValue)
		            return;

	            var Ecn = UserInput.GetDouble("Input concrete elastic module (Ec) in MPa:", Ec);

	            if (!Ecn.HasValue)
		            return;

	            var ecn = UserInput.GetDouble("Input concrete plastic strain (ec) (positive value):", ec);

	            if (!ecn.HasValue)
		            return;

	            var ecun = UserInput.GetDouble("Input concrete ultimate strain (ecu) (positive value):", ecu);

	            if (!ecun.HasValue)
		            return;

                // Get custom values
                fcr = fcrn.Value;
                Ec  = Ecn.Value;
                ec  = ecn.Value;
                ecu = ecun.Value;
            }

            // Get the Xdata size
            int size = Enum.GetNames(typeof(ConcreteData)).Length;
            var data = new TypedValue[size];

            data[(int)ConcreteData.AppName]  = new TypedValue((int) DxfCode.ExtendedDataRegAppName, Current.appName);
            data[(int)ConcreteData.XDataStr] = new TypedValue((int)DxfCode.ExtendedDataAsciiString, xdataStr);
            data[(int)ConcreteData.Model]    = new TypedValue((int)DxfCode.ExtendedDataInteger32,   (int)modelType);
            data[(int)ConcreteData.fc]       = new TypedValue((int)DxfCode.ExtendedDataReal,        fc);
            data[(int)ConcreteData.AggType]  = new TypedValue((int)DxfCode.ExtendedDataInteger32,   (int)aggType);
            data[(int)ConcreteData.AggDiam]  = new TypedValue((int)DxfCode.ExtendedDataReal,        phiAg);
            data[(int)ConcreteData.fcr]      = new TypedValue((int)DxfCode.ExtendedDataReal,        fcr);
            data[(int)ConcreteData.Ec]       = new TypedValue((int)DxfCode.ExtendedDataReal,        Ec);
            data[(int)ConcreteData.ec]       = new TypedValue((int)DxfCode.ExtendedDataReal,        ec);
            data[(int)ConcreteData.ecu]      = new TypedValue((int)DxfCode.ExtendedDataReal,        ecu);

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
            if (concrete != null)
	            concmsg = concrete.ToString();

            else
	            concmsg = "Concrete parameters not set";

			// Display the values returned
			Application.ShowAlertDialog(Current.appName + "\n\n" + concmsg);
        }

        // Read the concrete parameters
        public static Concrete ReadConcreteData()
		{
			var data = Auxiliary.ReadDictionaryEntry(ConcreteParams);

			if (data == null)
				return null;

			// Get the parameters from XData
			var model   = (Model)Convert.ToInt32(data[(int) ConcreteData.Model].Value);
			var aggType = (AggregateType)Convert.ToInt32(data[(int) ConcreteData.AggType].Value);

			double
                fc      = Convert.ToDouble(data[(int)ConcreteData.fc].Value),
				phiAg   = Convert.ToDouble(data[(int)ConcreteData.AggDiam].Value);

			if (model != Model.Custom)
				return
					new Concrete(fc, phiAg, model, aggType);

			// Get additional parameters
			double
				fcr =  Convert.ToDouble(data[(int) ConcreteData.fcr].Value),
				Ec  =  Convert.ToDouble(data[(int) ConcreteData.Ec].Value),
				ec  = -Convert.ToDouble(data[(int) ConcreteData.ec].Value),
				ecu = -Convert.ToDouble(data[(int) ConcreteData.ecu].Value);

			return
				new Concrete(fc, phiAg, model, aggType, fcr, Ec, ec, ecu);
		}
    }
}
