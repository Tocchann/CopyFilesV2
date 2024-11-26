using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CopyFiles.Core.Models;
using CopyFiles.Extensions.UI.Abstractions;
using CopyFiles.Models;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;

namespace CopyFiles.ViewModels;

public partial class SelectWorkViewModel : ObservableObject
{
	public ObservableCollection<string> ProjectSettingNames { get; } = new();

	[ObservableProperty]
	string? selectProjectSettingName;

	partial void OnSelectProjectSettingNameChanged( string? value )
	{
		// 切り替えたらその場でモデルに反映する
		App.ProjectSettingManager.ProjectName = value;
		// 選択された状態ではじめて操作できる
		IsSelectedProject = string.IsNullOrEmpty( value ) == false;
	}
	[ObservableProperty]
	bool isSelectedProject;

	[RelayCommand]
	void AddSolution()
	{
		var dlg = App.GetService<Contract.Views.IAddSolutionView>();
		if( dlg.ShowWindow() == true && !string.IsNullOrEmpty( dlg.ViewModel.SolutionName ) )
		{
			var setting = new ProjectSetting
			{
				ProjectFiles = new(),
				CopySettings = new(),
				SignerFileSetting = new ReferFolder { BaseFolder = string.Empty, ReferenceFolder = string.Empty },
				ZipFileNamePrefix = string.Empty,
			};
			App.ProjectSettingManager.ProjectSettings.Add( dlg.ViewModel.SolutionName, setting );
			ProjectSettingNames.Clear();
			foreach( var key in App.ProjectSettingManager.ProjectSettings.Keys )
			{
				ProjectSettingNames.Add( key );
			}
			SelectProjectSettingName = App.ProjectSettingManager.ProjectName = dlg.ViewModel.SolutionName;
		}
	}

	[RelayCommand]
	void RemoveSolution()
	{
		if( string.IsNullOrEmpty( SelectProjectSettingName ) )
		{
			m_dispAlert.Show( "プロジェクトが選択されていません", IDispAlert.Buttons.OK, IDispAlert.Icon.Exclamation );
			return;
		}
		var result = m_dispAlert.Show( SelectProjectSettingName + "を削除しますか？", IDispAlert.Buttons.YesNo, IDispAlert.Icon.Question );
		if( result == IDispAlert.Result.Yes )
		{
			var name = SelectProjectSettingName;
			ProjectSettingNames.Remove( name );
			App.ProjectSettingManager.ProjectSettings.Remove( name );
			SelectProjectSettingName = ProjectSettingNames.FirstOrDefault();
			App.ProjectSettingManager.ProjectName = SelectProjectSettingName;
		}
	}
	//public bool IsSelectedProject => string.IsNullOrEmpty( SelectProjectSettingName ) == false;

	[RelayCommand]
	void CollectTargetFiles()
	{
		var dlg = App.GetService<Contract.Views.ICollectTargetFilesView>();
		dlg.ShowWindow();
	}
	[RelayCommand]
	void ArchiveNonSignedFiles()
	{
		// 署名されていないファイルをアーカイブする
	}
	[RelayCommand]
	void CopySignedFiles()
	{
		// 署名されたファイルを収集先に再コピー
		// 圧縮ファイルを指定または、展開イメージのフォルダを指定のどちらかなんだろうけど…圧縮ファイルかなぁ？
	}

	public SelectWorkViewModel( ILogger<SelectWorkViewModel> logger, IDispAlert dispAlert )
	{
		m_logger = logger;
		m_dispAlert = dispAlert;

		foreach( var key in App.ProjectSettingManager.ProjectSettings.Keys )
		{
			ProjectSettingNames.Add( key );
		}
		SelectProjectSettingName = App.ProjectSettingManager.ProjectName;
	}
	[DesignOnly(true)]
#pragma warning disable CS8618 // null 非許容のフィールドには、コンストラクターの終了時に null 以外の値が入っていなければなりません。'required' 修飾子を追加するか、Null 許容として宣言することを検討してください。
	public SelectWorkViewModel()
#pragma warning restore CS8618 // null 非許容のフィールドには、コンストラクターの終了時に null 以外の値が入っていなければなりません。'required' 修飾子を追加するか、Null 許容として宣言することを検討してください。
	{
		//Debug.Assert( false );  //	実行されないはず
	}
	private ILogger<SelectWorkViewModel> m_logger;
	private IDispAlert m_dispAlert;
}
