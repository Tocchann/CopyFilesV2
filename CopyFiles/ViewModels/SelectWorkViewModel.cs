using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CopyFiles.Models;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace CopyFiles.ViewModels;

public partial class SelectWorkViewModel : ObservableObject
{
	public ObservableCollection<string> ProjectSettingNames { get; } = new();

	[ObservableProperty]
	string? selectProjectSettingName;

	partial void OnSelectProjectSettingNameChanged( string? value )
	{
		// 切り替えたらその場でモデルに反映する
		m_model.ProjectName = value?? string.Empty;
	}

	[RelayCommand]
	void AddSolution()
	{
		// TODO: Implement AddSolution
	}

	[RelayCommand]
	void RemoveSolution()
	{
	}

	[RelayCommand]
	void CollectTargetFiles()
	{
	}
	[RelayCommand]
	void ArchiveNonSignedFiles()
	{
	}
	[RelayCommand]
	void CopySignedFiles()
	{

	}

	public SelectWorkViewModel( ProjectSettingModel model )
	{
		m_model = model;
		foreach( var key in m_model.ProjectSettings.Keys )
		{
			ProjectSettingNames.Add( key );
		}
		SelectProjectSettingName = m_model.ProjectName;
	}
	private ProjectSettingModel m_model;
	[DesignOnly(true)]
	public SelectWorkViewModel()
	{
		m_model = new();
	}
}
