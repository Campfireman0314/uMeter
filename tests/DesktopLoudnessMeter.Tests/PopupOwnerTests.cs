using System.Windows;
using MeterApplication = DesktopLoudnessMeter.App.App;

namespace DesktopLoudnessMeter.Tests;

public sealed class PopupOwnerTests
{
    [Fact]
    public void NeverShownMainWindowIsNotAssignedAsPopupOwner()
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                var mainWindow = new Window();
                var popup = new Window();

                MeterApplication.AssignDetailPopupOwner(popup, mainWindow);

                Assert.Null(popup.Owner);
            }
            catch (Exception ex)
            {
                failure = ex;
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        Assert.Null(failure);
    }
}
