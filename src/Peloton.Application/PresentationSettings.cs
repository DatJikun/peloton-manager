namespace Peloton.Application;

public sealed record PresentationSettings(bool WatchFilmEnabled)
{
    public static PresentationSettings Default { get; } = new(false);
}
