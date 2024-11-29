using CopyFiles.Core.Models;
using System.Text.Json.Serialization;

namespace CopyFiles.Models;

public class ProjectSettingManager
{
	//public Dictionary<string,ProjectSetting> ProjectSettings { get; } = new();
	public List<ProjectSetting> ProjectSettings { get; set; } = new();
	public string? ProjectName { get; set; }

	[JsonIgnore]
	public ProjectSetting CurrentSetting => ProjectSettings.FirstOrDefault( x => x.Name == ProjectName ) ?? throw new InvalidOperationException( "ProjectName is empty" );
	//public ProjectSetting CurrentSetting => !string.IsNullOrEmpty( ProjectName ) ? ProjectSettings[ProjectName] : throw new InvalidOperationException( "ProjectName is empty" );
}
