using System;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.DatabaseServices;
using Extensions.AutoCAD;
using SPMTool.Enums;
using UnitsNet.Units;
using UnitsNet;

namespace SPMTool.Database
{
	/// <summary>
    /// Units data class.
    /// </summary>
	public static class UnitsData
	{
		/// <summary>
		/// Auxiliary <see cref="SPMTool.Units"/> field.
		/// </summary>
		private static Units? _units;

		// Unit names
		private const string Units = "Units";

		// Unit options
		public static readonly string[]
			DimOpts =
			{
				Length.GetAbbreviation(LengthUnit.Millimeter),
				Length.GetAbbreviation(LengthUnit.Centimeter),
				Length.GetAbbreviation(LengthUnit.Meter)
			},
			FOpts   =
			{
				Force.GetAbbreviation(ForceUnit.Newton),
				Force.GetAbbreviation(ForceUnit.Kilonewton),
				Force.GetAbbreviation(ForceUnit.Meganewton)
			},
			StOpts  =
			{
				Pressure.GetAbbreviation(PressureUnit.Pascal),
				Pressure.GetAbbreviation(PressureUnit.Kilopascal),
				Pressure.GetAbbreviation(PressureUnit.Megapascal),
				Pressure.GetAbbreviation(PressureUnit.Gigapascal)
			};

		/// <summary>
        /// Save this <paramref name="units"/> in database.
        /// </summary>
		public static void Save(Units units)
		{
			_units = units;

			// Get the Xdata size
			int size = Enum.GetNames(typeof(UnitsIndex)).Length;
			var data = new TypedValue[size];

			// Set data
			data[(int) UnitsIndex.AppName]            = new TypedValue((int) DxfCode.ExtendedDataRegAppName,  DataBase.AppName);
			data[(int) UnitsIndex.XDataStr]           = new TypedValue((int) DxfCode.ExtendedDataAsciiString, Units);
			data[(int) UnitsIndex.Geometry]           = new TypedValue((int) DxfCode.ExtendedDataInteger32, (int) units.Geometry);
			data[(int) UnitsIndex.Reinforcement]      = new TypedValue((int) DxfCode.ExtendedDataInteger32, (int) units.Reinforcement);
			data[(int) UnitsIndex.Displacements]      = new TypedValue((int) DxfCode.ExtendedDataInteger32, (int) units.Displacements);
			data[(int) UnitsIndex.AppliedForces]      = new TypedValue((int) DxfCode.ExtendedDataInteger32, (int) units.AppliedForces);
			data[(int) UnitsIndex.StringerForces]     = new TypedValue((int) DxfCode.ExtendedDataInteger32, (int) units.StringerForces);
			data[(int) UnitsIndex.PanelStresses]      = new TypedValue((int) DxfCode.ExtendedDataInteger32, (int) units.PanelStresses);
			data[(int) UnitsIndex.MaterialStrength]   = new TypedValue((int) DxfCode.ExtendedDataInteger32, (int) units.MaterialStrength);
			data[(int) UnitsIndex.DisplacementFactor] = new TypedValue((int) DxfCode.ExtendedDataReal,      units.DisplacementMagnifier);

			// Create the entry in the NOD and add to the transaction
			using (var rb = new ResultBuffer(data))
				DataBase.SaveDictionary(rb, Units);
		}

        /// <summary>
        /// Read units on database.
        /// </summary>
        /// <param name="setUnits">Units must be set by user if it's not set yet?</param>
		public static Units Read(bool setUnits = true) => _units ?? ReadFromDictionary(setUnits);

		/// <summary>
		/// Read units on dictionary.
		/// </summary>
		/// <param name="setUnits">Units must be set by user if it's not set yet?</param>
		private static Units ReadFromDictionary(bool setUnits = true)
        {
	        var data = DataBase.ReadDictionaryEntry(Units);

	        switch (data)
	        {
		        case null when setUnits:
			        Editor.Commands.Settings.SetUnits();
			        data = DataBase.ReadDictionaryEntry(Units);
			        break;
		        case null:
			        return SPMTool.Units.Default;
	        }

	        // Get the parameters from XData
	        _units = new Units
	        {
		        Geometry              = (LengthUnit)data[(int)UnitsIndex.Geometry].ToInt(),
		        Reinforcement         = (LengthUnit)data[(int)UnitsIndex.Reinforcement].ToInt(),
		        Displacements         = (LengthUnit)data[(int)UnitsIndex.Displacements].ToInt(),
		        AppliedForces         = (ForceUnit)data[(int)UnitsIndex.AppliedForces].ToInt(),
		        StringerForces        = (ForceUnit)data[(int)UnitsIndex.StringerForces].ToInt(),
		        PanelStresses         = (PressureUnit)data[(int)UnitsIndex.PanelStresses].ToInt(),
		        MaterialStrength      = (PressureUnit)data[(int)UnitsIndex.MaterialStrength].ToInt(),
                DisplacementMagnifier = data[(int)UnitsIndex.DisplacementFactor].ToInt()
            };

	        return _units.Value;
        }
    }
}
