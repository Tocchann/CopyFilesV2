using CopyFiles.Core.Models;
using CopyFiles.Extensions.Storage.Contract.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace CopyFiles.Models;

public class ProjectSettingModel
{
	public Dictionary<string,ProjectSetting> ProjectSettings { get; } = new();
	public string? ProjectName { get; set; }

	[JsonIgnore]
	public ProjectSetting CurrentSetting => !string.IsNullOrEmpty( ProjectName ) ? ProjectSettings[ProjectName] : throw new InvalidOperationException( "ProjectName is empty" );
	public ProjectSettingModel( IPersistAndRestoreService persistAndRestoreService )
	{
		// レストアだけ自力で行うがほかは自動でよい
		var model = persistAndRestoreService.RestoreData<ProjectSettingModel>();
		if( model is not null )
		{
			ProjectSettings = model.ProjectSettings;
			ProjectName = model.ProjectName;
		}
	}
	public ProjectSettingModel()
	{
	}
}
