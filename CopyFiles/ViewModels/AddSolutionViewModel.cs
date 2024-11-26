using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CopyFiles.Extensions.UI.Abstractions;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;

namespace CopyFiles.ViewModels;

public partial class AddSolutionViewModel( IDispAlert m_dispAlert ) : ObservableValidator
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
			m_dispAlert.Show( "ソリューション名を入力してください", IDispAlert.Buttons.OK, IDispAlert.Icon.Exclamation );
			return;
		}
		if( App.ProjectSettingManager.ProjectSettings.ContainsKey( SolutionName ) )
		{
			m_dispAlert.Show( "ソリューション名が重複しています", IDispAlert.Buttons.OK, IDispAlert.Icon.Exclamation );
			return;
		}
		DialogResult = true;
	}
	//public bool IsEnableOK => !string.IsNullOrWhiteSpace( SolutionName );

	[DesignOnly(true)]
#pragma warning disable CS8625 // null リテラルを null 非許容参照型に変換できません。
	public AddSolutionViewModel() : this(null)
#pragma warning restore CS8625 // null リテラルを null 非許容参照型に変換できません。
	{
		//Debug.Assert( false );  //	実行されないはず
	}
}
