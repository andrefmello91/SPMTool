using System.Diagnostics;
using System.Windows;
using System.Windows.Navigation;

namespace SPMTool.UserInterface.Windows
{
	public partial class InfoWindow : Window
	{

		#region Constructors

		public InfoWindow()
		{
			InitializeComponent();

			DataContext = this;
		}

		#endregion

		#region Methods

		private void Hyperlink_OnRequestNavigate(object sender, RequestNavigateEventArgs e) => Process.Start(e.Uri.ToString());

		#endregion

	}
}