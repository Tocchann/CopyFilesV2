# CopyFiles Ver.2 �Ɍ�����

## ���s��������s�ւȓ_

- �I���R�s�[�@�\���Ȃ�
    - �����ɂ����`�F�b�N�}�[�N�͂���ς肠�����ق����֗�
    - WPF �̕����I���肪���邪����͂��̃c�[���ł͂��܂���ʂ͂Ȃ��C������B
- �����������R�s�[�ł͂Ȃ����k�ɂ�����
    - �����Ώۃt�@�C�����R�s�[���ƕʓr���k����������̂Œ���ZIP�Ɉ��k���Ă��܂�����
- �M�܂߂̃C���X�g�[���v���W�F�N�g�ł��g����悤�ɂ�����
    - .ism ��ΏۂƂ��邾���łȂ��A.wixproj ���Ώۂɂł���Ɗ�����
    - .wixproj �� v4 �ȏ��Ώۂɂł��邩�H
    - ���̂��߂ɂ͕M�܂߂� WiX �ˑ��o�[�W�������ŐV�����Ȃ��ƃ_���ł͂Ȃ����H
- �����ڂ̉��P
    - SuperDCopy �݂����ȊK�w�\���ŕ\�����ł���Ƃ悢�H
        - �w�b�_�[�R���g���[�����Ȃ��Ă������Ȃ� Grid �� IsSharedSizeScope 
    - �R�s�[���E�R�s�[��̓��t���\�������ق����ǂ��ł��傤
    - �A�C�R��������Ƃ悢�H(�����Ă������Ǝv�����ǂǂ��Ȃ񂾂낤�H)
- CLI �ł��L������֗����H
    - �������Ƃ����_�ł͕֗����낤���ǁA�m�[�`�F�b�N�œ�������̂́A�����������炢�Ȃ̂ł��܂�Ӗ��͂Ȃ�

## �����̗�����l����

�������ƂɃ��\�b�h��p�ӂ��Ă������܂Ƃ߂� Dataflow �ŗ�����悤�ɂ���

�N���X�͂����ɂ�����̂Ƃ͈قȂ�`�Ŏ�������

### �R�s�[����

�K�v�t�@�C���̎��W
- .ism ����͂��Ď捞(xml�`���̂ݑΉ�)
- .exe ���w��(���ڃp�X�w��)
- .dll ���w��(���ڃp�X�w��)
- .sln(or .wixproj)���w��(VS�̃v���W�F�N�g������͂���̂������ʖ��Ή�)
    - ����͍ŏI�I�ɂ����Ȃ���������Ȃ�(�킩��񂪁c)

�K�v�t�@�C���̃R�s�[���̎Z�o
- �R�s�[��A�R�s�[���̃Z�b�g����A�R�s�[���̃p�X��T��

�R�s�[�����̎Z�o
- �R�s�[�����Ȃ��ꍇ�́A�w��R��Ƃ��ăG���[�ɂ����ق����ǂ�
- �R�s�[��ɂȂ��ꍇ�͖������R�s�[
- �R�s�[��Ɗ��S��v(�n�b�V���̊��S��v)�̓R�s�[���Ȃ�(����o�C�i���̂���)
    - ���t��������ꍇ�́A���t�����ς���Ă��邱�Ƃ����m�H(���Ȃ��Ă����H)
    - ����������ꍇ�́A�����p�̃G���A�̃n�b�V�����r(��v������R�s�[�s�v)
    - ���t���Ⴄ�����̏ꍇ�̓R�s�[�ΏۊO(�r���h�͂���ĂĂ��R�s�[���Ȃ�)
```C#
// �R�s�[�����t���O ���ׂ������邩�ǂ������Y�܂���
enum TargetStatus
{
    // ������
    Unknown,
    // ���S��v
    Match,
    // �����G���A�̃n�b�V������v(�T�C�Y�͈قȂ�)
    MatchWithSignArea,
    // ���t���Ⴄ����
    MatchWithoutDate,
    // ���e���Ⴄ���o�[�W��������v
    UnMatchSameVersion,
    // ���e���Ⴄ
    UnMatch,
    // �R�s�[��ɂȂ�
    NewFile,
    // ��������(�����t���p)
    NotSigned,
}
```
## �f�[�^�N���X�������ƍ��

���f���I�Ȍ`�Ńf�[�^�N���X���`����
�ۑ��ΏۃN���X
``` C#
class TargetFolder{
    public string Source { get; set; }
    public string Destination { get; set; }
    public bool ExitWithOpenFolder { get; set; }
}
```
``` C#
class TargetFolderWithArchiveFlag : TargetFolder
{
    public bool RequireArchive { get; set; }
    public string FilenamePrefix { get; set; }
    // �t�@�C�����͎��������ł���悤�ɂ��Ă���
    public ZipArchive CreateArvchive()
    {
        var toDay = DateTime.Today.ToString("yyyyMMdd");
        var fileNode = $"{FilenamePrefix}_{toDay}";
        var filePath = Path.Combine( Destination, fileNode + ".zip" );
        if( File.Exists( filePath ) )
        {
            for( int dupCount = 1 ; dupCount < int.Max ; dupCount++ )
            {
                filePath = Path.Combine( Destination, fileNode + $"_{dupCount}.zip" );
                if( !File.Exists( filePath ) )
                {
                    break;
                }
            }
        }
        if( File.Exists( filePath ) )
        {
            // HRESULT ���ǂ����邩�H���l���Ȃ��Ƃ����Ȃ���˂��c
            throw new IOException( "�d���t�@�C�������������܂�" );
        }
        // �쐬���[�h�ŊJ��
        return ZipFile.Open( filePath, ZipArchiveMode.Create );
    }
}
```
�V���ɃW�F�l���[�^�[�N���X��������ƕ������ėp�ӂ���ׂ����낤
``` C#
// TargetFolder ���󂯎���āA�Ώۃt�@�C���ꗗ�𐶐�����N���X
class CopyFilesGenerator
{
    public TargetFolder TargetFoler { get; set; }
    public IProgress<string> Progress { get; set; }
    public CancellationToken Token { get; set; }

    public CopyFilesList TargetFiles{ get; }

    public async void GenerateAsync( CancellationToken token );
}
```
``` C#
// �����̕K�v�ȃt�@�C���̈ꗗ�𐶐�����N���X
class SignFilesGenerator
{
    public TargetFolder TargetFoler { get; set; }
    public IProgress<string> Progress { get; set; }
    public CancellationToken Token { get; set; }

    public CopyFilesList TargetFiles{ get; }

    public async void GenerateAsync( CancellationToken token );
}
```
�W�F�l���[�^�Ő�������f�[�^�ꗗ(VM�ɕ\�������邽�߂̃f�[�^���f��)
``` C#
class RelativeFileInformation
{
    [Required]
    public string Folder { get; }
    [Required]
    public string FileName { get; }
    [Required]
    public TargetStatus TargetStatus{ get; }
    
    public string GetFullPath( string baseFolder )
    {
        return Path.Combine( baseFolder, Folder, FileName );
    }
}
```
``` C#
// ����Ώۈꗗ���X�g�E�����p�̓������̂𐶐�����B�\�����X�g�͂�������i�荞��ō��
class CopyFilesList
{
    public string SourceBase { get; }
    public string DestinationBase { get; }
    public List<RelativeFileInformation> TargetFiles { get; } // �R�s�[�悪���j�[�N�ɂȂ��Ă���΂悢(�R�s�[���̏d���͂���)
}
```
�����̂ق��Ɏ��ۂ�MVVM�̃��f���ɂȂ�N���X��p�ӂ���B  
������������A�W�F�l���[�^�����̃N���X�̃����o�[�ɂ��Ă��܂���������Ȃ�������s��

���f���N���X�Ƃ͕ʂɃf�[�^�ۑ��N���X���p�ӂ���B  
������́A���s�œ��l�o�[�W�������ƂɃf�[�^�Ǘ��ł���悤�ɂ��Ă������ƁB  
��{�I�ɂ͌��s�łƓ����\���ł悢�Ǝv��
TargetFiles �̓R�s�[�悪���j�[�N�ɂȂ�K�v������(�d���͔j�])
TargetFiles ���c���[�I�ɕ\������ꍇ�́ADestination ����ɏ������邱�ƂɂȂ�
