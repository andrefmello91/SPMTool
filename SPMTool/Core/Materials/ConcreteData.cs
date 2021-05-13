using andrefmello91.Material.Concrete;
using Autodesk.AutoCAD.DatabaseServices;
using SPMTool.Enums;

using UnitsNet;
using static andrefmello91.Material.Concrete.Parameters;

#nullable enable

namespace SPMTool.Core.Materials
{
	/// <summary>
	///     Concrete database class.
	/// </summary>
	public class ConcreteData : ExtendedObject
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

		/// <inheritdoc />
		public override Layer Layer => default;

		/// <inheritdoc />
		public override string Name => ConcreteParams;

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

		#region Methods

		/// <inheritdoc />
		public override DBObject CreateObject() => new Xrecord
		{
			Data = new ResultBuffer(_parameters.GetTypedValues())
		};

		protected override void GetProperties()
		{
			_parameters = GetParameters();
			_model      = GetModel();
		}

		protected override void SetProperties()
		{
			SetParameters(_parameters);
			SetConstitutive(_model);
		}

		/// <summary>
		///     Read constitutive model.
		/// </summary>
		private ConstitutiveModel GetModel() => (ConstitutiveModel) (GetDictionary("ConstitutiveModel").GetEnumValue() ?? (int) ConstitutiveModel.MCFT);

		/// <summary>
		///     Read concrete <see cref="Parameters" /> saved in database.
		/// </summary>
		private IParameters GetParameters() => GetDictionary(ConcreteParams).GetParameters() ?? C30(Length.FromMillimeters(19));

		private void SetConstitutive(ConstitutiveModel model)
		{
			_model = model;

			SetDictionary(model.GetTypedValues(), "ConstitutiveModel");
		}

		private void SetParameters(IParameters parameters)
		{
			_parameters = parameters;

			SetDictionary(_parameters.GetTypedValues(), ConcreteParams);
		}

		#endregion

	}
}