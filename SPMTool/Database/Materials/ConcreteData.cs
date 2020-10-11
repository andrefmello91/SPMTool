using System;
using Autodesk.AutoCAD.DatabaseServices;
using Extensions.AutoCAD;
using Material.Concrete;
using SPMTool.Editor.Commands;
using SPMTool.Enums;

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
	    public static Concrete Read(bool setConcrete = true)
	    {
		    var data = DataBase.ReadDictionaryEntry(ConcreteParams);

		    if (data is null && setConcrete)
			    MaterialInput.SetConcreteParameters();

		    else
			    return null;

            // Get the parameters from XData
            var parModel    = (ParameterModel)data[(int) ConcreteIndex.Model].ToInt();
		    var constModel  = (ConstitutiveModel)data[(int) ConcreteIndex.Behavior].ToInt();
		    var aggType     = (AggregateType)data[(int) ConcreteIndex.AggType].ToInt();

		    double
			    fc      = data[(int)ConcreteIndex.fc].ToDouble(),
			    phiAg   = data[(int)ConcreteIndex.AggDiam].ToDouble(), 
                
			    // Get additional parameters
			    fcr =  data[(int)ConcreteIndex.ft].ToDouble(),
			    Ec  =  data[(int)ConcreteIndex.Ec].ToDouble(),
			    ec  = -data[(int)ConcreteIndex.ec].ToDouble(),
			    ecu = -data[(int)ConcreteIndex.ecu].ToDouble();

		    // Get parameters and constitutive
		    var parameters = Parameters.ReadParameters(parModel, fc, phiAg, aggType, fcr, Ec, ec, ecu);

		    return new Concrete(parameters, constModel);
	    }
    }
}
