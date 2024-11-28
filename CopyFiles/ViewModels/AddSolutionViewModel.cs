using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CopyFiles.Extensions.UI.Abstractions;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;

namespace CopyFiles.ViewModels;

public partial class AddSolutionViewModel : ObservableValidator
{
	[ObservableProperty]
	string? solutionName;

	[ObservableProperty]
	bool? dialogResult;

	//[RelayCommand( CanExecute = nameof(IsEnableOK))]
	[RelayCommand]

	void OnOK()
	{
		if( string.IsNullOrWhiteSpace( SolutionName ) )
		{
			App.DispAlert.Show( "ソリューション名を入力してください", IDispAlert.Buttons.OK, IDispAlert.Icon.Exclamation );
			return;
		}
		if( App.ProjectSettingManager.ProjectSettings.Any( setting => setting.Name == SolutionName ) )
		{
			App.DispAlert.Show( "ソリューション名が重複しています", IDispAlert.Buttons.OK, IDispAlert.Icon.Exclamation );
			return;
		}
		DialogResult = true;
	}
	//public bool IsEnableOK => !string.IsNullOrWhiteSpace( SolutionName );
}
