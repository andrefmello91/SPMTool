using System;
using System.Linq;
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
        /// Auxiliary <see cref="Material.Concrete.Parameters"/> field.
        /// </summary>
		private static Parameters _parameters;

		/// <summary>
        /// Auxiliary <see cref="ConstitutiveModel"/> field.
        /// </summary>
		private static ConstitutiveModel? _constitutiveModel;

        /// <summary>
        /// Get <see cref="Material.Concrete.Parameters"/> saved in database.
        /// </summary>
        public static Parameters Parameters => _parameters ?? Read();

        /// <summary>
        /// Get <see cref="Material.Concrete.ConstitutiveModel"/> saved in database.
        /// </summary>
        public static ConstitutiveModel ConstitutiveModel => _constitutiveModel ?? ConstitutiveModel.MCFT;

        /// <summary>
        /// Save concrete <see cref="Material.Concrete.Parameters"/> and <see cref="Material.Concrete.ConstitutiveModel"/> in database.
        /// </summary>
        /// <param name="concrete">The <see cref="ConcreteData"/> object.</param>
        public static void Save(Concrete concrete) => Save(concrete.Parameters, ConstitutiveModel);

	    /// <summary>
	    /// Save concrete <see cref="Material.Concrete.Parameters"/> and <see cref="Material.Concrete.ConstitutiveModel"/> in database.
	    /// </summary>
	    /// <param name="parameters">Concrete <see cref="Material.Concrete.Parameters"/>.</param>
	    /// <param name="constitutiveModel">Concrete <see cref="Material.Concrete.ConstitutiveModel"/>.</param>
	    public static void Save(Parameters parameters, ConstitutiveModel constitutiveModel)
	    {
			// Set to concrete properties
			_parameters   = parameters;
			_constitutiveModel = constitutiveModel;

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
		    data[(int)ConcreteIndex.Behavior] = new TypedValue((int)DxfCode.ExtendedDataInteger32,   (int)constitutiveModel);
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
        /// <param name="setConcrete">Concrete must be set by user if it's not set yet?</param>
	    public static Parameters Read(bool setConcrete = true) => _parameters ?? ReadFromDictionary(setConcrete);

	    /// <summary>
	    /// Read concrete <see cref="Parameters"/> saved in database.
	    /// </summary>
	    /// <param name="setConcrete">Concrete must be set by user if it's not set yet?</param>
	    private static Parameters ReadFromDictionary(bool setConcrete = true)
	    {
		    var data = DataBase.ReadDictionaryEntry(ConcreteParams);

		    switch (data)
		    {
			    case null when setConcrete:
				    MaterialInput.SetConcreteParameters();
				    return _parameters;
			    case null:
				    return new MC2010Parameters(30, 19);
		    }

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
		    _parameters   = Parameters.ReadParameters(parModel, fc, phiAg, aggType, fcr, Ec, ec, ecu);
		    _constitutiveModel = constModel;

		    return _parameters;
	    }
    }
}
