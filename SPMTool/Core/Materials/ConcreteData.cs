using Autodesk.AutoCAD.DatabaseServices;
using andrefmello91.Material.Concrete;
using SPMTool.Extensions;
using UnitsNet;
using static andrefmello91.Material.Concrete.Parameters;

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