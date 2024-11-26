using CopyFiles.Contract.Views;
using CopyFiles.ViewModels;

namespace CopyFiles.Views;

/// <summary>
/// SelectWorkWindow.xaml の相互作用ロジック
/// </summary>
public partial class SelectWorkView : ISelectWorkView
{
	public SelectWorkView( SelectWorkViewModel vm )
	{
		InitializeComponent();
		DataContext = vm;
	}
	public bool? ShowWindow()
	{
		Show();
		return true;
	}
}
