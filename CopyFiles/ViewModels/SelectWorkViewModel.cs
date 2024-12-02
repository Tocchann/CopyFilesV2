using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CopyFiles.Core.Models;
using CopyFiles.Extensions.UI.Abstractions;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Compression;

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
				Name = dlg.ViewModel.SolutionName,
				ProjectFiles = new(),
				CopySettings = new(),
				SignerFileSetting = new ReferFolder { BaseFolder = string.Empty, ReferenceFolder = string.Empty },
				ZipFileNamePrefix = dlg.ViewModel.SolutionName,
			};
			App.ProjectSettingManager.ProjectSettings.Add( setting );
			ProjectSettingNames.Clear();
			foreach( var key in App.ProjectSettingManager.ProjectSettings.Select( setting => setting.Name ) )
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
			App.DispAlert.Show( "プロジェクトが選択されていません", IDispAlert.Buttons.OK, IDispAlert.Icon.Exclamation );
			return;
		}
		var result = App.DispAlert.Show( SelectProjectSettingName + "を削除しますか？", IDispAlert.Buttons.YesNo, IDispAlert.Icon.Question );
		if( result == IDispAlert.Result.Yes )
		{
			var name = SelectProjectSettingName;
			ProjectSettingNames.Remove( name );
			App.ProjectSettingManager.ProjectSettings = App.ProjectSettingManager.ProjectSettings.Where( setting => setting.Name != name ).ToList();
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
		var dlg = App.GetService<Contract.Views.IArchiveNonSignedFilesView>();
		dlg.ShowWindow();
	}
	[RelayCommand]
	void CopySignedFiles()
	{
		//App.DispAlert.Show( "工事中…署名済みファイルの展開処理" );
		// ほかの処理みたいに確認のときに、zipファイルを参照して、それを元に選択式に展開するのがいいか？
		// でもそのままでもいいと思うんだよねぇ…悩ましいところだけども…

		if( string.IsNullOrEmpty( App.ProjectSettingManager.CurrentSetting.SignerFileSetting.BaseFolder ) ||
			!System.IO.Directory.Exists( App.ProjectSettingManager.CurrentSetting.SignerFileSetting.BaseFolder ) )
		{
			App.DispAlert.Show( "先に、未署名ファイルの圧縮で、基準フォルダを設定してください" );
			return;
		}
		var dlg = new Microsoft.Win32.OpenFileDialog();
		dlg.Filter = "Zipファイル(*.zip)|*.zip";
		if( dlg.ShowDialog( Extensions.UI.WPF.Utilities.GetOwnerWindow() ) == true )
		{
			// フォルダを調整しないと展開できない場合もあるから、ここは手動でやらないとダメ…だろうな。
			//System.IO.Compression.ZipFile.ExtractToDirectory( dlg.FileName, App.ProjectSettingManager.CurrentSetting.SignerFileSetting.BaseFolder );
			using( var archive = System.IO.Compression.ZipFile.OpenRead( dlg.FileName ) )
			{
				System.IO.Compression.ZipArchiveEntry firstEntry = archive.Entries.First();
				var filePath = Path.Combine( App.ProjectSettingManager.CurrentSetting.SignerFileSetting.BaseFolder, firstEntry.FullName );
				if( File.Exists( filePath ) )
				{
					// この場所であっているのでそのまま展開する
					if( App.DispAlert.Show( "圧縮ファイルの内容で更新しますか？", IDispAlert.Buttons.YesNo, IDispAlert.Icon.Question ) == IDispAlert.Result.Yes )
					{
						foreach( var entry in archive.Entries )
						{
							filePath = Path.Combine( App.ProjectSettingManager.CurrentSetting.SignerFileSetting.BaseFolder, entry.FullName );
							entry.ExtractToFile( filePath, true );
						}
					}
				}
				else
				{
					// この場所でないので、展開して手動でコピーしてもらう
					if( App.DispAlert.Show( "圧縮ファイルを直接展開できないので、サブフォルダに展開します。", IDispAlert.Buttons.YesNo, IDispAlert.Icon.Question ) == IDispAlert.Result.Yes )
					{
						var extractDir = Path.Combine( Path.GetDirectoryName( dlg.FileName )!, Path.GetFileNameWithoutExtension( dlg.FileName ) );
						archive.ExtractToDirectory( extractDir );
					}
				}
			}
		}
	}

	public SelectWorkViewModel()
	{
		foreach( var key in App.ProjectSettingManager.ProjectSettings.Select( setting => setting.Name ) )
		{
			ProjectSettingNames.Add( key );
		}
		SelectProjectSettingName = App.ProjectSettingManager.ProjectName;
	}
}
