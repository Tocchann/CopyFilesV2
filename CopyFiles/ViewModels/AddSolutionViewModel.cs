using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CopyFiles.Extensions.UI.Abstractions;

namespace CopyFiles.ViewModels;

public partial class AddSolutionViewModel : ObservableValidator
{
	[ObservableProperty]
	[NotifyCanExecuteChangedFor( nameof( OKCommand ) )]
	public partial string? SolutionName { get; set; }

	[ObservableProperty]
	public partial bool? DialogResult { get; set; }

	[RelayCommand( CanExecute = nameof( CanExecuteOnOK ) )]
	void OnOK()
	{
		if( App.ProjectSettingManager.ProjectSettings.Any( setting => setting.Name == SolutionName ) )
		{
			App.DispAlert.Show( "ソリューション名が重複しています" );
			return;
		}
		DialogResult = true;
	}

	private bool CanExecuteOnOK()
	{
		return !string.IsNullOrWhiteSpace( SolutionName );
	}
}
