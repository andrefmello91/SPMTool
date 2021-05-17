using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Media.Imaging;
using SPMTool.Enums;
using SPMTool.Properties;
using static Autodesk.AutoCAD.ApplicationServices.Core.Application;

#nullable enable

namespace SPMTool.Application.UserInterface
{
	/// <summary>
	///     User interface icons class.
	/// </summary>
	public class Icons
	{

		#region Fields

		// Auxiliary bitmaps
		private Bitmap
			_strBmp,
			_pnlBmp,
			_dvStrBmp,
			_dvPnlBmp,
			_elmDtBmp,
			_updtBmp,
			_strRefBmp,
			_pnlRefBmp,
			_cncrtBmp,
			_suprtBmp,
			_fcBmp,
			_linBmp,
			_nlinBmp,
			_simBmp,
			_viewNdBmp,
			_viewStrBmp,
			_viewPnlBmp,
			_viewFBmp,
			_viewSupBmp,
			_strFBmp,
			_pnlShBmp,
			_pnlStBmp,
			_concStBmp,
			_dispBmp,
			_crackBmp,
			_unitsBmp,
			_anSetBmp,
			_dpSetBmp;

		#endregion

		#region Properties

		/// <summary>
		///     Get AddConstraint icon.
		/// </summary>
		public BitmapImage AddConstraint => GetBitmap(_suprtBmp);

		/// <summary>
		///     Get AddForce icon.
		/// </summary>
		public BitmapImage AddForce => GetBitmap(_fcBmp);

		/// <summary>
		///     Get AddPanel icon.
		/// </summary>
		public BitmapImage AddPanel => GetBitmap(_pnlBmp);

		/// <summary>
		///     Get AddStringer icon.
		/// </summary>
		public BitmapImage AddStringer => GetBitmap(_strBmp);

		/// <summary>
		///     Get analysis settings icon.
		/// </summary>
		public BitmapImage Analysis => GetBitmap(_anSetBmp);

		/// <summary>
		///     Get ConcreteStresses icon.
		/// </summary>
		public BitmapImage ConcreteStresses => GetBitmap(_concStBmp);

		/// <summary>
		///     Get Cracks icon.
		/// </summary>
		public BitmapImage Cracks => GetBitmap(_crackBmp);

		/// <summary>
		///     Get Displacements icon.
		/// </summary>
		public BitmapImage Displacements => GetBitmap(_dispBmp);

		/// <summary>
		///     Get DividePanel icon.
		/// </summary>
		public BitmapImage DividePanel => GetBitmap(_dvPnlBmp);

		/// <summary>
		///     Get DivideStringer icon.
		/// </summary>
		public BitmapImage DivideStringer => GetBitmap(_dvStrBmp);

		/// <summary>
		///     Get PanelReinforcement icon.
		/// </summary>
		public BitmapImage EditPanel => GetBitmap(_pnlRefBmp);

		/// <summary>
		///     Get StringerReinforcement icon.
		/// </summary>
		public BitmapImage EditStringer => GetBitmap(_strRefBmp);

		/// <summary>
		///     Get ElementData icon.
		/// </summary>
		public BitmapImage ElementData => GetBitmap(_elmDtBmp);

		/// <summary>
		///     Get ViewForces icon.
		/// </summary>
		public BitmapImage Forces => GetBitmap(_viewFBmp);

		/// <summary>
		///     Get LinearAnalysis icon.
		/// </summary>
		public BitmapImage Linear => GetBitmap(_linBmp);

		/// <summary>
		///     Get ViewNodes icon.
		/// </summary>
		public BitmapImage Nodes => GetBitmap(_viewNdBmp);

		/// <summary>
		///     Get NonLinearAnalysis icon.
		/// </summary>
		public BitmapImage Nonlinear => GetBitmap(_nlinBmp);

		/// <summary>
		///     Get ViewPanels icon.
		/// </summary>
		public BitmapImage Panels => GetBitmap(_viewPnlBmp);

		/// <summary>
		///     Get PanelShear icon.
		/// </summary>
		public BitmapImage PanelShear => GetBitmap(_pnlShBmp);

		/// <summary>
		///     Get PanelStresses icon.
		/// </summary>
		public BitmapImage PanelStresses => GetBitmap(_pnlStBmp);

		/// <summary>
		///     Get Concrete icon.
		/// </summary>
		public BitmapImage Parameters => GetBitmap(_cncrtBmp);

		/// <summary>
		///     Get StringerForces icon.
		/// </summary>
		public BitmapImage StringerForces => GetBitmap(_strFBmp);

		/// <summary>
		///     Get ViewStringers icon.
		/// </summary>
		public BitmapImage Stringers => GetBitmap(_viewStrBmp);

		/// <summary>
		///     Get ViewSupports icon.
		/// </summary>
		public BitmapImage Supports => GetBitmap(_viewSupBmp);

		/// <summary>
		///     Get Units icon.
		/// </summary>
		public BitmapImage Units => GetBitmap(_unitsBmp);

		/// <summary>
		///     Get UpdateElements icon.
		/// </summary>
		public BitmapImage UpdateElements => GetBitmap(_updtBmp);
			
		/// <summary>
		///     Get Simulation icon.
		/// </summary>
		public BitmapImage Simulation => GetBitmap(_simBmp);
		
		/// <summary>
		///     Get Display Settings icon.
		/// </summary>
		public BitmapImage Display => GetBitmap(_dpSetBmp);

		#endregion

		#region Methods

		/// <summary>
		///     Get a bitmap from <paramref name="image" />.
		/// </summary>
		public static BitmapImage GetBitmap(Image image)
		{
			var stream = new MemoryStream();
			image.Save(stream, ImageFormat.Png);
			var bmp = new BitmapImage();
			bmp.BeginInit();
			bmp.StreamSource = stream;
			bmp.EndInit();
			return bmp;
		}

		/// <summary>
		///     Get a <see cref="Bitmap" /> icon from resources.
		/// </summary>
		/// <param name="name">The icon name.</param>
		/// <param name="colorTheme">The application <see cref="ColorTheme" />.</param>
		public static Bitmap? GetFromResource(string name, ColorTheme colorTheme) => (Bitmap?) Resources.ResourceManager.GetObject(colorTheme is ColorTheme.Light ? name : $"{name}_light");

		/// <summary>
		///     Get application icons based on system theme.
		/// </summary>
		public void GetIcons()
		{
			// Check the current theme
			var theme = (ColorTheme) (short) GetSystemVariable("COLORTHEME");

			_strBmp     = GetFromResource("stringer_large", theme)!;
			_pnlBmp     = GetFromResource("panel_large", theme)!;
			_dvStrBmp   = GetFromResource("divstr_small", theme)!;
			_dvPnlBmp   = GetFromResource("divpnl_small", theme)!;
			_updtBmp    = GetFromResource("update_small", theme)!;
			_elmDtBmp   = GetFromResource("elementdata_small", theme)!;
			_strRefBmp  = GetFromResource("stringerreinforcement_large", theme)!;
			_pnlRefBmp  = GetFromResource("panelreinforcement_large", theme)!;
			_cncrtBmp   = GetFromResource("concrete_large", theme)!;
			_suprtBmp   = GetFromResource("support_large", theme)!;
			_fcBmp      = GetFromResource("force_large", theme)!;
			_linBmp     = GetFromResource("linear_large", theme)!;
			_nlinBmp    = GetFromResource("nonlinear_large", theme)!;
			_simBmp     = GetFromResource("simulation_large", theme)!;
			_viewNdBmp  = GetFromResource("viewnode_large", theme)!;
			_viewStrBmp = GetFromResource("viewstringer_large", theme)!;
			_viewPnlBmp = GetFromResource("viewpanel_large", theme)!;
			_viewFBmp   = GetFromResource("viewforce_large", theme)!;
			_viewSupBmp = GetFromResource("viewsupport_large", theme)!;
			_strFBmp    = GetFromResource("stringerforces_large", theme)!;
			_pnlShBmp   = GetFromResource("panelforces_large", theme)!;
			_pnlStBmp   = GetFromResource("panelstresses_large", theme)!;
			_concStBmp  = GetFromResource("concretestresses_large", theme)!;
			_dispBmp    = GetFromResource("displacements_large", theme)!;
			_crackBmp   = GetFromResource("crack_large", theme)!;
			_unitsBmp   = GetFromResource("units", theme)!;
			_anSetBmp   = GetFromResource("analysissettings", theme)!;
			_dpSetBmp   = GetFromResource("display_large", theme)!;
		}

		#endregion

	}
}