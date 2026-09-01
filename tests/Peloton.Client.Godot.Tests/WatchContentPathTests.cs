using System.IO;
using Peloton.Client.Godot;
using Xunit;

namespace Peloton.Client.Godot.Tests;

public sealed class WatchContentPathTests
{
    [Fact]
    public void LooksLikeContentRootRequiresSkeletonPack()
    {
        using TemporaryDirectory temp = new();
        Assert.False(WatchContentPath.LooksLikeContentRoot(temp.Path));
        Directory.CreateDirectory(Path.Combine(temp.Path, "peloton.skeleton"));
        Assert.False(WatchContentPath.LooksLikeContentRoot(temp.Path));
        File.WriteAllText(Path.Combine(temp.Path, "peloton.skeleton", "pack.json"), "{}");
        Assert.True(WatchContentPath.LooksLikeContentRoot(temp.Path));
    }

    [Fact]
    public void PlaytestSavePathUsesTempInEditorAndSavesFolderInExportedGame()
    {
        using TemporaryDirectory temp = new();
        string editor = WatchContentPath.PlaytestSavePath("pre-race.peloton", runningInEditor: true, executablePath: null);
        Assert.Equal(Path.Combine(Path.GetTempPath(), "pre-race.peloton"), editor);

        string exe = Path.Combine(temp.Path, "PelotonManager.exe");
        string exported = WatchContentPath.PlaytestSavePath("pre-race.peloton", runningInEditor: false, exe);
        Assert.Equal(Path.Combine(temp.Path, "saves", "pre-race.peloton"), exported);
        Assert.True(Directory.Exists(Path.Combine(temp.Path, "saves")));
    }

    [Fact]
    public void FindContentRootStillFindsTheRepositoryContentPack()
    {
        string root = WatchContentPath.FindContentRoot();
        Assert.True(WatchContentPath.LooksLikeContentRoot(root));
        Assert.True(File.Exists(Path.Combine(root, "peloton.race-prototype", "pack.json")));
    }
}
