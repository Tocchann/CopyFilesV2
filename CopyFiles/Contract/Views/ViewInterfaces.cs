using CopyFiles.ViewModels;

namespace CopyFiles.Contract.Views;

public interface IBaseView
{
	public bool? ShowWindow();
}
public interface ISelectWorkView : IBaseView
{
}
public interface IAddSolutionView : IBaseView
{
	public AddSolutionViewModel ViewModel { get; }
}
public interface ICollectTargetFilesView : IBaseView
{
}
public interface IEditReferFolderView : IBaseView
{
	public EditReferFolderViewModel ViewModel { get; }
}
public interface IArchiveNonSignedFilesView : IBaseView
{
	public ArchiveNonSignedFilesViewModel ViewModel { get; }
}
