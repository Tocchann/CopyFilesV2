using CopyFiles.ViewModels;

namespace CopyFiles.Views;

/// <summary>
/// SelectWorkWindow.xaml の相互作用ロジック
/// </summary>
public partial class SelectWorkView
{
	public SelectWorkView( SelectWorkViewModel vm )
	{
		InitializeComponent();
		DataContext = vm;
	}
}
