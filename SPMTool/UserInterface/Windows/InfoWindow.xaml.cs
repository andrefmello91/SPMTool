using System.Diagnostics;
using System.Windows;
using System.Windows.Navigation;
using SPMTool.Application.UserInterface;

namespace SPMTool.UserInterface.Windows
{
	public partial class InfoWindow : Window
	{

		#region Properties

		public string Repo { get; } = SPMToolInterface.SPMToolRepository;

		public string Version { get; } = $"SPMTool v. {SPMToolInterface.SPMToolVersion}";

		#endregion

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