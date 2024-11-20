using System.Windows;

namespace CopyFiles.Extensions.UI.WPF;

public static class Utilities
{
	public static Window? GetOwnerWindow()
	{
		var window = default(Window);
		foreach( Window search in Application.Current.Windows )
		{
			if( search.IsActive && search.Parent == null )
			{
				window = search;
				break;
			}
		}
		if( window == null )
		{
			window = Application.Current.MainWindow;
		}
		return window;
	}
}
