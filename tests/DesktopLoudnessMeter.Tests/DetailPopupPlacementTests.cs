using DesktopLoudnessMeter.App;
using System.Windows;

namespace DesktopLoudnessMeter.Tests;

public sealed class DetailPopupPlacementTests
{
    [Fact]
    public void PositionNearOwnerCentersPopupOverVisibleOwner()
    {
        RunOnStaThread(() =>
        {
            var owner = new Window
            {
                Width = 460,
                Height = 860,
                Left = 100,
                Top = 80,
                ShowInTaskbar = false,
                WindowStartupLocation = WindowStartupLocation.Manual
            };
            var popup = new Window
            {
                Width = 460,
                Height = 840,
                ShowInTaskbar = false
            };

            try
            {
                owner.Show();

                DetailPopupPlacement.PositionNearOwner(popup, owner);

                Assert.Equal(WindowStartupLocation.Manual, popup.WindowStartupLocation);
                Assert.Equal(owner.Left, popup.Left, precision: 3);
                Assert.Equal(owner.Top + 10, popup.Top, precision: 3);
            }
            finally
            {
                popup.Close();
                owner.Close();
            }
        });
    }

    [Fact]
    public void PositionNearOwnerCentersOnScreenWhenOwnerIsUnavailable()
    {
        RunOnStaThread(() =>
        {
            var popup = new Window();

            DetailPopupPlacement.PositionNearOwner(popup, null);

            Assert.Equal(WindowStartupLocation.CenterScreen, popup.WindowStartupLocation);
        });
    }

    private static void RunOnStaThread(Action action)
    {
        Exception? error = null;
        var thread = new Thread(() =>
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                error = ex;
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (error is not null)
        {
            throw error;
        }
    }
}
