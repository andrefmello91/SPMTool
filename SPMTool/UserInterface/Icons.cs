using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Media.Imaging;
using SPMTool.Properties;
using static Autodesk.AutoCAD.ApplicationServices.Core.Application;

namespace SPMTool.UserInterface
{
	/// <summary>
	/// User interface icons class.
	/// </summary>
    public class Icons
    {
	    // Auxiliary bitmaps
	    private Bitmap
		    _strBmp, _pnlBmp, _dvStrBmp, _dvPnlBmp, _elmDtBmp, _updtBmp,
		    _strRefBmp, _pnlRefBmp, _cncrtBmp, _suprtBmp, _fcBmp, _linBmp,
		    _nlinBmp, _viewNdBmp, _viewStrBmp, _viewPnlBmp, _viewFBmp,
		    _viewSupBmp, _strFBmp, _pnlShBmp, _pnlStBmp, _concStBmp, _dispBmp, _unitsBmp;

        /// <summary>
        /// Get AddStringer icon.
        /// </summary>
	    public BitmapImage Stringer => GetBitmap(_strBmp);

        /// <summary>
        /// Get AddPanel icon.
        /// </summary>
	    public BitmapImage Panel => GetBitmap(_pnlBmp);

        /// <summary>
        /// Get DivideStringer icon.
        /// </summary>
	    public BitmapImage DivideStringer => GetBitmap(_dvStrBmp);

        /// <summary>
        /// Get DividePanel icon.
        /// </summary>
	    public BitmapImage DividePanel => GetBitmap(_dvPnlBmp);

        /// <summary>
        /// Get ElementData icon.
        /// </summary>
	    public BitmapImage ElementData => GetBitmap(_elmDtBmp);

        /// <summary>
        /// Get UpdateElements icon.
        /// </summary>
	    public BitmapImage UpdateElements => GetBitmap(_updtBmp);

        /// <summary>
        /// Get StringerReinforcement icon.
        /// </summary>
	    public BitmapImage StringerReinforcement => GetBitmap(_strRefBmp);

        /// <summary>
        /// Get PanelReinforcement icon.
        /// </summary>
	    public BitmapImage PanelReinforcement => GetBitmap(_pnlRefBmp);

        /// <summary>
        /// Get Concrete icon.
        /// </summary>
	    public BitmapImage Concrete => GetBitmap(_cncrtBmp);

        /// <summary>
        /// Get AddConstraint icon.
        /// </summary>
	    public BitmapImage AddConstraint => GetBitmap(_suprtBmp);

        /// <summary>
        /// Get AddForce icon.
        /// </summary>
	    public BitmapImage AddForce => GetBitmap(_fcBmp);

        /// <summary>
        /// Get LinearAnalysis icon.
        /// </summary>
	    public BitmapImage LinearAnalysis => GetBitmap(_linBmp);

        /// <summary>
        /// Get NonLinearAnalysis icon.
        /// </summary>
	    public BitmapImage NonLinearAnalysis => GetBitmap(_nlinBmp);

        /// <summary>
        /// Get ViewNodes icon.
        /// </summary>
	    public BitmapImage ViewNodes => GetBitmap(_viewNdBmp);

        /// <summary>
        /// Get ViewStringers icon.
        /// </summary>
	    public BitmapImage ViewStringers => GetBitmap(_viewStrBmp);

        /// <summary>
        /// Get ViewPanels icon.
        /// </summary>
	    public BitmapImage ViewPanels => GetBitmap(_viewPnlBmp);

        /// <summary>
        /// Get ViewForces icon.
        /// </summary>
	    public BitmapImage ViewForces => GetBitmap(_viewFBmp);

        /// <summary>
        /// Get ViewSupports icon.
        /// </summary>
	    public BitmapImage ViewSupports => GetBitmap(_viewSupBmp);

        /// <summary>
        /// Get StringerForces icon.
        /// </summary>
	    public BitmapImage StringerForces => GetBitmap(_strFBmp);

        /// <summary>
        /// Get PanelShear icon.
        /// </summary>
	    public BitmapImage PanelShear => GetBitmap(_pnlShBmp);

        /// <summary>
        /// Get PanelStresses icon.
        /// </summary>
	    public BitmapImage PanelStresses => GetBitmap(_pnlStBmp);

        /// <summary>
        /// Get ConcreteStresses icon.
        /// </summary>
	    public BitmapImage ConcreteStresses => GetBitmap(_concStBmp);

        /// <summary>
        /// Get Displacements icon.
        /// </summary>
	    public BitmapImage Displacements => GetBitmap(_dispBmp);

        /// <summary>
        /// Get Units icon.
        /// </summary>
	    public BitmapImage Units => GetBitmap(_unitsBmp);

        /// <summary>
        /// <see cref="Icons"/> object.
        /// </summary>
        public Icons()
        {
            GetIcons();
        }

        /// <summary>
        /// Get application icons based on system theme.
        /// </summary>
        private void GetIcons()
        {
            // Check the current theme
            var theme = (short)GetSystemVariable("COLORTHEME");

            // If the theme is dark (0), get the light icons
            if (theme == 0)
            {
                _strBmp = Resources.stringer_large_light;
                _pnlBmp = Resources.panel_large_light;
                _dvStrBmp = Resources.divstr_small_light;
                _dvPnlBmp = Resources.divpnl_small_light;
                _updtBmp = Resources.update_small_light;
                _elmDtBmp = Resources.elementdata_small_light;
                _strRefBmp = Resources.stringerreinforcement_large_light;
                _pnlRefBmp = Resources.panelreinforcement_large_light;
                _cncrtBmp = Resources.concrete_large_light;
                _suprtBmp = Resources.support_large_light;
                _fcBmp = Resources.force_large_light;
                _linBmp = Resources.linear_large_light;
                _nlinBmp = Resources.nonlinear_large_light;
                _viewNdBmp = Resources.viewnode_large_light;
                _viewStrBmp = Resources.viewstringer_large_light;
                _viewPnlBmp = Resources.viewpanel_large_light;
                _viewFBmp = Resources.viewforce_large_light;
                _viewSupBmp = Resources.viewsupport_large_light;
                _strFBmp = Resources.stringerforces_large_light;
                _pnlShBmp = Resources.panelforces_large_light;
                _pnlStBmp = Resources.panelstresses_large_light;
                _concStBmp = Resources.concretestresses_large_light;
                _dispBmp = Resources.displacements_large_light;
                _unitsBmp = Resources.units_light;
            }
            else // If the theme is light
            {
                _strBmp = Resources.stringer_large;
                _pnlBmp = Resources.panel_large;
                _dvStrBmp = Resources.divstr_small;
                _dvPnlBmp = Resources.divpnl_small;
                _updtBmp = Resources.update_small;
                _elmDtBmp = Resources.elementdata_small;
                _strRefBmp = Resources.stringerreinforcement_large;
                _pnlRefBmp = Resources.panelreinforcement_large;
                _cncrtBmp = Resources.concrete_large;
                _suprtBmp = Resources.support_large;
                _fcBmp = Resources.force_large;
                _linBmp = Resources.linear_large;
                _nlinBmp = Resources.nonlinear_large;
                _viewNdBmp = Resources.viewnode_large;
                _viewStrBmp = Resources.viewstringer_large;
                _viewPnlBmp = Resources.viewpanel_large;
                _viewFBmp = Resources.viewforce_large;
                _viewSupBmp = Resources.viewsupport_large;
                _strFBmp = Resources.stringerforces_large;
                _pnlShBmp = Resources.panelforces_large;
                _pnlStBmp = Resources.panelstresses_large;
                _concStBmp = Resources.concretestresses_large;
                _dispBmp = Resources.displacements_large;
                _unitsBmp = Resources.units;
            }
        }

        /// <summary>
        /// Get a bitmap from <paramref name="image"/>.
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
    }
}
