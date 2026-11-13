using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace DesktopLoudnessMeter.Tests.Controls;

/// <summary>
/// Runs real WPF control scenarios on a dedicated STA thread, mirroring the
/// conventions already used by the localization and popup-owner tests.
/// Scenarios run one at a time because WPF input, HWND hosting, and rendering are
/// single-threaded, and each scenario is bounded so a stuck one fails the test
/// instead of hanging the test host.
/// </summary>
internal static class StaControlHost
{
    private static readonly object StaGate = new();

    private static readonly TimeSpan ScenarioTimeout = TimeSpan.FromSeconds(30);

    public static void Run(Action action)
    {
        lock (StaGate)
        {
            Exception? failure = null;

            var thread = new Thread(() =>
            {
                var dispatcher = Dispatcher.CurrentDispatcher;
                dispatcher.UnhandledException += (_, args) =>
                {
                    failure ??= args.Exception;
                    args.Handled = true;
                };

                dispatcher.BeginInvoke(
                    new Action(() =>
                    {
                        try
                        {
                            action();
                        }
                        catch (Exception exception)
                        {
                            failure ??= exception;
                        }
                        finally
                        {
                            dispatcher.BeginInvokeShutdown(DispatcherPriority.Background);
                        }
                    }),
                    DispatcherPriority.Normal);

                Dispatcher.Run();
            })
            {
                IsBackground = true,
                Name = "uMeter accessibility test"
            };

            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();

            if (!thread.Join(ScenarioTimeout))
            {
                throw new TimeoutException(
                    $"STA accessibility scenario did not finish within {ScenarioTimeout.TotalSeconds:0} seconds.");
            }

            if (failure is not null)
            {
                throw new InvalidOperationException($"STA scenario failed: {failure.Message}", failure);
            }
        }
    }

    public static void Arrange(FrameworkElement element, double width = 264, double height = 54)
    {
        element.Width = width;
        element.Height = height;
        element.Measure(new Size(width, height));
        element.Arrange(new Rect(0, 0, width, height));
        element.UpdateLayout();
    }

    public static KeyEventArgs RaiseKeyDown(UIElement target, Key key)
    {
        var args = new KeyEventArgs(
            Keyboard.PrimaryDevice,
            new TestPresentationSource(),
            Environment.TickCount,
            key)
        {
            RoutedEvent = Keyboard.KeyDownEvent,
            Source = target
        };
        target.RaiseEvent(args);
        return args;
    }

    /// <summary>
    /// Finds the inline numeric editor a slider hosts as a child even though it
    /// is hidden from the visual tree until editing starts.
    /// </summary>
    public static TextBox FindEditor(DependencyObject slider)
    {
        foreach (var child in LogicalTreeHelper.GetChildren(slider))
        {
            if (child is TextBox logicalEditor)
            {
                return logicalEditor;
            }
        }

        if (slider is Visual host)
        {
            for (var index = 0; index < VisualTreeHelper.GetChildrenCount(host); index++)
            {
                if (VisualTreeHelper.GetChild(host, index) is TextBox visualEditor)
                {
                    return visualEditor;
                }
            }
        }

        throw new InvalidOperationException("Slider does not host a numeric editor child.");
    }

    /// <summary>
    /// Hosts a control in a hidden window source so real keyboard focus (and
    /// therefore real focus visuals) can be exercised headlessly.
    /// </summary>
    public static HwndSource CreateFocusHost(FrameworkElement content)
    {
        var width = double.IsNaN(content.Width) ? 264 : content.Width;
        var height = double.IsNaN(content.Height) ? 54 : content.Height;
        var parameters = new HwndSourceParameters("uMeter accessibility test host")
        {
            Width = Math.Max(1, (int)Math.Ceiling(width)),
            Height = Math.Max(1, (int)Math.Ceiling(height)),
            WindowStyle = 0
        };

        var root = new Grid
        {
            Width = Math.Max(1, width),
            Height = Math.Max(1, height)
        };
        root.Children.Add(content);
        var source = new HwndSource(parameters);
        source.RootVisual = root;
        root.UpdateLayout();
        content.UpdateLayout();
        return source;
    }

    public static byte[] RenderPixels(FrameworkElement element)
    {
        var width = Math.Max(1, (int)Math.Ceiling(element.ActualWidth));
        var height = Math.Max(1, (int)Math.Ceiling(element.ActualHeight));
        var bitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(element);
        var pixels = new byte[width * height * 4];
        bitmap.CopyPixels(pixels, width * 4, 0);
        return pixels;
    }

    public static bool IsOpaque(byte[] pixels, int width, int x, int y)
    {
        var index = ((y * width) + x) * 4;
        return index >= 0 && index + 3 < pixels.Length && pixels[index + 3] != 0;
    }

    /// <summary>
    /// Minimal presentation source so keyboard events can be raised without a
    /// visible window while still being routed through the real input pipeline.
    /// </summary>
    private sealed class TestPresentationSource : PresentationSource
    {
        public override Visual? RootVisual
        {
            get => null;
            set => throw new NotSupportedException();
        }

        public override bool IsDisposed => false;

        protected override CompositionTarget? GetCompositionTargetCore() => null;
    }
}
