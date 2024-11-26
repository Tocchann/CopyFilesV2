using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CopyFiles.Core.Models;
using CopyFiles.Extensions.UI.Abstractions;
using System.ComponentModel;
using System.IO;

namespace CopyFiles.ViewModels;

public partial class ReferFolderItem : ObservableObject
{
	[ObservableProperty]
	string baseFolder;

	[ObservableProperty]
	string referenceFolder;

	public ReferFolder ReferFolder
	{
		get => new() { BaseFolder = BaseFolder, ReferenceFolder = ReferenceFolder };
		set
		{
			BaseFolder = value.BaseFolder;
			ReferenceFolder = value.ReferenceFolder;
		}
	}

	public ReferFolderItem( ReferFolder referFolder )
	{
		BaseFolder = referFolder.BaseFolder;
		ReferenceFolder = referFolder.ReferenceFolder;
	}
	public ReferFolderItem()
	{
		BaseFolder = string.Empty;
		ReferenceFolder = string.Empty;
	}
}

public partial class EditReferFolderViewModel : ReferFolderItem
{
	public bool IsCopySetting { get; set; }
	[ObservableProperty]
	string dialogTitle = string.Empty;

	[ObservableProperty]
	bool? dialogResult;

	[RelayCommand]
	void ChangeBaseFolder()
	{
		var dlg = App.GetService<ISelectFolderDialog>();
		dlg.Title = "基準フォルダの選択";
		dlg.SelectedPath = BaseFolder;
		if( dlg.ShowDialog() == true )
		{
			BaseFolder = dlg.SelectedPath;
		}
	}
	[RelayCommand]
	void ChangeReferenceFolder()
	{
		var dlg = App.GetService<ISelectFolderDialog>();
		dlg.Title = "参照フォルダの選択";
		dlg.SelectedPath = ReferenceFolder;
		if( dlg.ShowDialog() == true )
		{
			ReferenceFolder = dlg.SelectedPath;
		}
	}
	[RelayCommand]
	void OnOK()
	{
		if( string.IsNullOrWhiteSpace( BaseFolder ) )
		{
			m_dispAlert.Show( "基準フォルダが指定されていません。", IDispAlert.Buttons.OK, IDispAlert.Icon.Exclamation );
			return;
		}
		if( string.IsNullOrWhiteSpace( ReferenceFolder ) )
		{
			m_dispAlert.Show( "参照フォルダが指定されていません。", IDispAlert.Buttons.OK, IDispAlert.Icon.Exclamation );
			return;
		}
		// コピーの場合は、
		if( IsCopySetting )
		{
			if( !Directory.Exists( ReferenceFolder ) )
			{
				m_dispAlert.Show( "参照フォルダが存在しません。", IDispAlert.Buttons.OK, IDispAlert.Icon.Exclamation );
				return;
			}
		}
		else
		{
			if( !Directory.Exists( BaseFolder ) )
			{
				m_dispAlert.Show( "基準フォルダが存在しません。", IDispAlert.Buttons.OK, IDispAlert.Icon.Exclamation );
				return;
			}
		}
		// コピーの場合コピー元(参照フォルダ)が同じ場所になることはない
		if( IsCopySetting )
		{
			// 参照フォルダ(コピー元)が同じで別の場所にコピーはない(逆はある)
			if( App.ProjectSettingManager.CurrentSetting.CopySettings.Any( r => r.ReferenceFolder == ReferenceFolder && r.BaseFolder != BaseFolder ) )
			{
				m_dispAlert.Show( "参照フォルダが重複しています。", IDispAlert.Buttons.OK, IDispAlert.Icon.Exclamation );
				return;
			}
		}
		DialogResult = true;
	}
	[DesignOnly(true)]
#pragma warning disable CS8618 // null 非許容のフィールドには、コンストラクターの終了時に null 以外の値が入っていなければなりません。'required' 修飾子を追加するか、Null 許容として宣言することを検討してください。
	public EditReferFolderViewModel()
#pragma warning restore CS8618 // null 非許容のフィールドには、コンストラクターの終了時に null 以外の値が入っていなければなりません。'required' 修飾子を追加するか、Null 許容として宣言することを検討してください。
	{
	}
	public EditReferFolderViewModel( IDispAlert dispAlert ) : base()
	{
		m_dispAlert = dispAlert;
	}
	private IDispAlert m_dispAlert;
}
