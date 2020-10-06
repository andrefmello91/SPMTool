using System;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Runtime;
using Material.Concrete;
using SPMTool.Database.Materials;
using SPMTool.Enums;
using SPMTool.UserInterface;

[assembly: CommandClass(typeof(ConcreteData))]

namespace SPMTool.Database.Materials
{
    /// <summary>
    /// Concrete database class.
    /// </summary>
    public static class ConcreteData
    {
		/// <summary>
        /// Save string.
        /// </summary>
	    private const string ConcreteParams = "ConcreteParams";

        /// <summary>
        /// Model names.
        /// </summary>
        public static readonly string[] Models =
	    {
		    ParameterModel.MC2010.ToString(),
		    ParameterModel.NBR6118.ToString(),
		    ParameterModel.MCFT.ToString(),
		    ParameterModel.DSFM.ToString(),
		    ParameterModel.Custom.ToString()
	    };

		/// <summary>
        /// Constitutive names
        /// </summary>
	    public static readonly string[] ConstitutiveModels =
	    {
		    ConstitutiveModel.MCFT.ToString(),
		    ConstitutiveModel.DSFM.ToString()
	    };

        /// <summary>
        /// Aggregate types
        /// </summary>
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

        /// <summary>
        /// Save concrete <see cref="Parameters"/> and <see cref="ConstitutiveModel"/> in database.
        /// </summary>
        /// <param name="concrete">The <see cref="ConcreteData"/> object.</param>
        public static void Save(Concrete concrete) => Save(concrete.Parameters, Constitutive.ReadConstitutiveModel(concrete.Constitutive));

	    /// <summary>
	    /// Save concrete <see cref="Parameters"/> and <see cref="ConstitutiveModel"/> in database.
	    /// </summary>
	    /// <param name="parameters">Concrete <see cref="Parameters"/>.</param>
	    /// <param name="behaviorModel">Concrete <see cref="ConstitutiveModel"/>.</param>
	    public static void Save(Parameters parameters, ConstitutiveModel behaviorModel)
	    {
		    // Definition for the Extended Data
		    var xdataStr = "Concrete data";

		    // Read the parameter model
		    var parModel = Parameters.ReadParameterModel(parameters);

		    // Get the Xdata size
		    var size = Enum.GetNames(typeof(ConcreteIndex)).Length;
		    var data = new TypedValue[size];

		    data[(int)ConcreteIndex.AppName]  = new TypedValue((int)DxfCode.ExtendedDataRegAppName,  DataBase.AppName);
		    data[(int)ConcreteIndex.XDataStr] = new TypedValue((int)DxfCode.ExtendedDataAsciiString, xdataStr);
		    data[(int)ConcreteIndex.Model]    = new TypedValue((int)DxfCode.ExtendedDataInteger32,   (int)parModel);
		    data[(int)ConcreteIndex.Behavior] = new TypedValue((int)DxfCode.ExtendedDataInteger32,   (int)behaviorModel);
		    data[(int)ConcreteIndex.fc]       = new TypedValue((int)DxfCode.ExtendedDataReal,        parameters.Strength);
		    data[(int)ConcreteIndex.AggType]  = new TypedValue((int)DxfCode.ExtendedDataInteger32,   (int)parameters.Type);
		    data[(int)ConcreteIndex.AggDiam]  = new TypedValue((int)DxfCode.ExtendedDataReal,        parameters.AggregateDiameter);
		    data[(int)ConcreteIndex.ft]       = new TypedValue((int)DxfCode.ExtendedDataReal,        parameters.TensileStrength);
		    data[(int)ConcreteIndex.Ec]       = new TypedValue((int)DxfCode.ExtendedDataReal,        parameters.InitialModule);
		    data[(int)ConcreteIndex.ec]       = new TypedValue((int)DxfCode.ExtendedDataReal,        parameters.PlasticStrain);
		    data[(int)ConcreteIndex.ecu]      = new TypedValue((int)DxfCode.ExtendedDataReal,        parameters.UltimateStrain);

		    // Create the entry in the NOD and add to the transaction
		    using (var rb = new ResultBuffer(data))
			    DataBase.SaveDictionary(rb, ConcreteParams);
	    }

	    /// <summary>
	    /// Read concrete saved in database.
	    /// </summary>
	    /// <param name="setConcrete">Concrete must be set by user?</param>
	    public static Concrete ReadConcreteData(bool setConcrete = true)
	    {
		    var data = DataBase.ReadDictionaryEntry(ConcreteParams);

		    if (data is null)
		    {
			    if (setConcrete)
				    SetConcreteParameters();

			    else
				    return null;
		    }

		    // Get the parameters from XData
		    var parModel    = (ParameterModel)Convert.ToInt32(data[(int) ConcreteIndex.Model].Value);
		    var constModel  = (ConstitutiveModel)Convert.ToInt32(data[(int) ConcreteIndex.Behavior].Value);
		    var aggType     = (AggregateType)Convert.ToInt32(data[(int) ConcreteIndex.AggType].Value);

		    double
			    fc      = Convert.ToDouble(data[(int)ConcreteIndex.fc].Value),
			    phiAg   = Convert.ToDouble(data[(int)ConcreteIndex.AggDiam].Value), 
                
			    // Get additional parameters
			    fcr =  Convert.ToDouble(data[(int)ConcreteIndex.ft].Value),
			    Ec  =  Convert.ToDouble(data[(int)ConcreteIndex.Ec].Value),
			    ec  = -Convert.ToDouble(data[(int)ConcreteIndex.ec].Value),
			    ecu = -Convert.ToDouble(data[(int)ConcreteIndex.ecu].Value);

		    // Get parameters and constitutive
		    var parameters = Parameters.ReadParameters(parModel, fc, phiAg, aggType, fcr, Ec, ec, ecu);

		    return new Concrete(parameters, constModel);
	    }
    }
}
