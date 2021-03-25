using Autodesk.AutoCAD.DatabaseServices;
using Material.Concrete;
using SPMTool.Extensions;
using UnitsNet;
using static Material.Concrete.Parameters;

#nullable enable

namespace SPMTool.Core.Materials
{
	/// <summary>
	///     Concrete database class.
	/// </summary>
	public class ConcreteData : DictionaryCreator
	{
		#region Fields

		/// <summary>
		///     Save string.
		/// </summary>
		private const string ConcreteParams = "ConcreteParams";

		private ConstitutiveModel _model;
		private IParameters _parameters;

		#endregion

		#region Properties

		/// <summary>
		///     Get <see cref="Material.Concrete.ConstitutiveModel" /> saved in database.
		/// </summary>
		public ConstitutiveModel ConstitutiveModel
		{
			get => _model;
			set => SetConstitutive(value);
		}

		/// <summary>
		///     Get/set <see cref="Material.Concrete.Parameters" /> saved in database.
		/// </summary>
		public IParameters Parameters
		{
			get => _parameters;
			set => SetParameters(value);
		}

		#endregion

		#region Constructors

		public ConcreteData()
		{
			DictionaryId = DataBase.NodId;
			GetProperties();
		}

		#endregion

		#region  Methods

		private void SetParameters(IParameters parameters)
		{
			_parameters = parameters;

			SetDictionary(_parameters.GetTypedValues(), ConcreteParams);
		}

		private void SetConstitutive(ConstitutiveModel model)
		{
			_model = model;

			SetDictionary(model.GetTypedValues(), "ConstitutiveModel");
		}

		//  /// <summary>
		//  /// Save concrete <see cref="Material.Concrete.Parameters"/> and <see cref="Material.Concrete.ConstitutiveModel"/> in database.
		//  /// </summary>
		//  /// <param name="parameters">Concrete <see cref="Material.Concrete.Parameters"/>.</param>
		//  /// <param name="constitutiveModel">Concrete <see cref="Material.Concrete.ConstitutiveModel"/>.</param>
		//  public void Save(IParameters parameters, ConstitutiveModel constitutiveModel)
		//  {
		//// Set to concrete properties
		//Parameters        = parameters;
		//ConstitutiveModel = constitutiveModel;

		//   // Definition for the Extended Data
		//   var xdataStr = "Concrete data";

		//   // Read the parameter model
		//   var parModel = Parameters.Model;

		//   // Get the Xdata size
		//   var size = Enum.GetNames(typeof(ConcreteIndex)).Length;
		//   var data = new TypedValue[size];

		//   data[(int)ConcreteIndex.AppName]  = new TypedValue((int)DxfCode.ExtendedDataRegAppName,  DataBase.AppName);
		//   data[(int)ConcreteIndex.XDataStr] = new TypedValue((int)DxfCode.ExtendedDataAsciiString, xdataStr);
		//   data[(int)ConcreteIndex.Model]    = new TypedValue((int)DxfCode.ExtendedDataInteger32,   (int)parModel);
		//   data[(int)ConcreteIndex.Behavior] = new TypedValue((int)DxfCode.ExtendedDataInteger32,   (int)constitutiveModel);
		//   data[(int)ConcreteIndex.fc]       = new TypedValue((int)DxfCode.ExtendedDataReal,        parameters.Strength.Megapascals);
		//   data[(int)ConcreteIndex.AggType]  = new TypedValue((int)DxfCode.ExtendedDataInteger32,   (int)parameters.Type);
		//   data[(int)ConcreteIndex.AggDiam]  = new TypedValue((int)DxfCode.ExtendedDataReal,        parameters.AggregateDiameter.Millimeters);
		//   data[(int)ConcreteIndex.ft]       = new TypedValue((int)DxfCode.ExtendedDataReal,        parameters.TensileStrength.Megapascals);
		//   data[(int)ConcreteIndex.Ec]       = new TypedValue((int)DxfCode.ExtendedDataReal,        parameters.ElasticModule.Megapascals);
		//   data[(int)ConcreteIndex.ec]       = new TypedValue((int)DxfCode.ExtendedDataReal,        parameters.PlasticStrain);
		//   data[(int)ConcreteIndex.ecu]      = new TypedValue((int)DxfCode.ExtendedDataReal,        parameters.UltimateStrain);

		//   // Create the entry in the NOD and add to the transaction
		//   using var rb = new ResultBuffer(data);
		//   DataBase.SaveDictionary(rb, ConcreteParams);
		//  }

		/// <summary>
		///     Read concrete <see cref="Parameters" /> saved in database.
		/// </summary>
		private IParameters GetParameters() => GetDictionary(ConcreteParams).GetParameters() ?? C30(Length.FromMillimeters(19));

		/// <summary>
		///     Read constitutive model.
		/// </summary>
		private ConstitutiveModel GetModel() => (ConstitutiveModel) (GetDictionary("ConstitutiveModel").GetEnumValue() ?? (int) ConstitutiveModel.MCFT);

		protected override bool GetProperties()
		{
			_parameters = GetParameters();
			_model      = GetModel();

			return true;
		}

		protected override void SetProperties()
		{
			SetParameters(_parameters);
			SetConstitutive(_model);
		}

		#endregion
	}
}