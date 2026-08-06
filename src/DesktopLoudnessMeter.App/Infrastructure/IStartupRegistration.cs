namespace DesktopLoudnessMeter.App.Infrastructure;

public interface IStartupRegistration
{
    void Configure(bool enabled, bool startHidden);
}
