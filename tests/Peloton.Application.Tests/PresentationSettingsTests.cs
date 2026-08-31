using Xunit;

namespace Peloton.Application.Tests;

public sealed class PresentationSettingsTests
{
    [Fact]
    public void WatchFilmIsOffByDefault()
    {
        Assert.False(PresentationSettings.Default.WatchFilmEnabled);
        Assert.False(new PresentationSettings(false).WatchFilmEnabled);
        Assert.True(new PresentationSettings(true).WatchFilmEnabled);
    }
}
