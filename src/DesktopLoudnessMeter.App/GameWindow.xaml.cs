using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using DesktopLoudnessMeter.App.Controls;
using DesktopLoudnessMeter.App.Game;
using DesktopLoudnessMeter.App.ViewModels;

namespace DesktopLoudnessMeter.App;

public partial class GameWindow : Window
{
    private int _effectSequence;
    private int _cardAnimationSequence;

    public GameWindow()
    {
        InitializeComponent();
        DataContextChanged += GameWindow_OnDataContextChanged;
    }

    public GameWindow(MainViewModel mainViewModel)
        : this()
    {
        DataContext = new TileGameViewModel(mainViewModel);
    }

    protected override void OnClosed(EventArgs e)
    {
        DataContextChanged -= GameWindow_OnDataContextChanged;
        _cardAnimationSequence++;
        DetachVisualEffects(DataContext);
        if (DataContext is IDisposable disposable)
        {
            disposable.Dispose();
        }

        base.OnClosed(e);
    }

    private void Header_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        WindowDragBehavior.HandleHeaderMouseLeftButtonDown(this, e);
    }

    private void CloseButton_OnClick(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void GameWindow_OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        DetachVisualEffects(e.OldValue);
        if (e.NewValue is TileGameViewModel viewModel)
        {
            viewModel.VisualEffectRequested += ViewModel_OnVisualEffectRequested;
            viewModel.RoundPresentationRequested += ViewModel_OnRoundPresentationRequested;
            viewModel.PowerUpPresentationRequested += ViewModel_OnPowerUpPresentationRequested;
        }
    }

    private void DetachVisualEffects(object? dataContext)
    {
        if (dataContext is TileGameViewModel viewModel)
        {
            viewModel.VisualEffectRequested -= ViewModel_OnVisualEffectRequested;
            viewModel.RoundPresentationRequested -= ViewModel_OnRoundPresentationRequested;
            viewModel.PowerUpPresentationRequested -= ViewModel_OnPowerUpPresentationRequested;
        }
    }

    private void ViewModel_OnRoundPresentationRequested(GameRoundPresentation presentation)
    {
        if (presentation == GameRoundPresentation.Won)
        {
            DancingSheep.PlayWinCheer();
            PlayCelebrationBanner();
            return;
        }

        if (presentation == GameRoundPresentation.Lost)
        {
            DancingSheep.PlayLossRagdoll();
            return;
        }

        DancingSheep.ResetOutcomeAnimation();
        BoardEffectCanvas.Children.Clear();
        TrayEffectCanvas.Children.Clear();
        ResetCardAnimations();
        HideCelebrationBanner();
        PlayBoardEntrance(presentation == GameRoundPresentation.NewGame);
    }

    private void ViewModel_OnPowerUpPresentationRequested(GamePowerUpKind powerUp)
    {
        DancingSheep.ResetOutcomeAnimation();
        switch (powerUp)
        {
            case GamePowerUpKind.Cannon:
                PlayCannonBlast();
                break;
            case GamePowerUpKind.Fan:
                PlayFanShuffle();
                break;
            case GamePowerUpKind.Digger:
                PulseTray(TimeSpan.FromMilliseconds(720));
                break;
        }
    }

    private void ViewModel_OnVisualEffectRequested(TileVisualEffectRequest request)
    {
        if (request.EffectKind == TileVisualEffectKind.Match)
        {
            PlayMatchBurst(request.TileKind);
            return;
        }

        if (request.EffectKind == TileVisualEffectKind.RhythmEcho)
        {
            PlayRhythmEcho(request.TileKind);
            return;
        }

        PlaySelectionSplash(request);
    }

    private void PlaySelectionSplash(TileVisualEffectRequest request)
    {
        var profile = TileVisualEffectProfile.For(TileVisualEffectKind.Selection);
        var tileBrush = SignalTileBrushConverter.ForKind(request.TileKind);
        var effect = CreateEffectLayer(BoardEffectCanvas, request.CenterX, request.CenterY);
        AddRings(effect, tileBrush, profile);
        AddParticles(effect, tileBrush, tileBrush, profile, upperFan: false);
        FadeAndRemove(BoardEffectCanvas, effect, profile.Duration);
    }

    private void PlayMatchBurst(SignalTileKind tileKind)
    {
        var profile = TileVisualEffectProfile.For(TileVisualEffectKind.Match);
        var tileBrush = SignalTileBrushConverter.ForKind(tileKind);
        var accentBrush = TryFindResource("WarmBrush") as Brush ?? tileBrush;
        var effect = CreateEffectLayer(TrayEffectCanvas, TrayEffectCanvas.Width / 2, TrayEffectCanvas.Height / 2);
        AddRings(effect, tileBrush, profile);
        AddParticles(effect, tileBrush, accentBrush, profile, upperFan: true);
        AddMatchGlyph(effect, tileKind, accentBrush, profile.Duration);
        PulseTray(profile.Duration);
        FadeAndRemove(TrayEffectCanvas, effect, profile.Duration);
    }

    private void PlayRhythmEcho(SignalTileKind tileKind)
    {
        var profile = TileVisualEffectProfile.For(TileVisualEffectKind.RhythmEcho);
        var tileBrush = SignalTileBrushConverter.ForKind(tileKind);
        var accentBrush = TryFindResource("AccentBrush") as Brush ?? tileBrush;
        var effect = CreateEffectLayer(BoardEffectCanvas, BoardEffectCanvas.Width / 2, BoardEffectCanvas.Height / 2);
        AddRings(effect, accentBrush, profile);
        AddParticles(effect, tileBrush, accentBrush, profile, upperFan: false);
        FadeAndRemove(BoardEffectCanvas, effect, profile.Duration);
    }

    private Canvas CreateEffectLayer(Canvas host, double centerX, double centerY)
    {
        var effect = new Canvas
        {
            Width = 1,
            Height = 1,
            IsHitTestVisible = false
        };
        Canvas.SetLeft(effect, centerX);
        Canvas.SetTop(effect, centerY);
        host.Children.Add(effect);
        return effect;
    }

    private static void AddRings(Canvas effect, Brush brush, TileVisualEffectProfile profile)
    {
        for (var index = 0; index < profile.RingCount; index++)
        {
            var ring = new Ellipse
            {
                Width = 18,
                Height = 18,
                Stroke = brush,
                StrokeThickness = profile.RingCount == 1 ? 2 : 2.5,
                RenderTransformOrigin = new Point(0.5, 0.5)
            };
            Canvas.SetLeft(ring, -9);
            Canvas.SetTop(ring, -9);
            var scale = new ScaleTransform(0.35, 0.35);
            ring.RenderTransform = scale;
            effect.Children.Add(ring);

            var delay = TimeSpan.FromMilliseconds(index * 75);
            var duration = TimeSpan.FromTicks((long)(profile.Duration.Ticks * 0.72));
            var targetScale = profile.RingCount == 1 ? 2.35 : 4.4 + (index * 1.2);
            var easing = new CubicEase { EasingMode = EasingMode.EaseOut };
            scale.BeginAnimation(ScaleTransform.ScaleXProperty, new DoubleAnimation(0.35, targetScale, duration)
            {
                BeginTime = delay,
                EasingFunction = easing
            });
            scale.BeginAnimation(ScaleTransform.ScaleYProperty, new DoubleAnimation(0.35, targetScale, duration)
            {
                BeginTime = delay,
                EasingFunction = easing
            });
        }
    }

    private void AddParticles(
        Canvas effect,
        Brush primaryBrush,
        Brush secondaryBrush,
        TileVisualEffectProfile profile,
        bool upperFan)
    {
        var sequenceOffset = (_effectSequence++ % 7) * 0.11;
        for (var index = 0; index < profile.ParticleCount; index++)
        {
            var ratio = profile.ParticleCount == 1 ? 0 : (double)index / (profile.ParticleCount - 1);
            var angle = upperFan
                ? Math.PI + (ratio * Math.PI)
                : (ratio * Math.PI * 2) + sequenceOffset;
            var distanceScale = 0.68 + ((index % 4) * 0.1);
            var distance = profile.TravelDistance * distanceScale;
            var size = profile.ShowsGlyph ? 4 + (index % 3) : 4;
            Shape particle = profile.ShowsGlyph && index % 2 == 1
                ? new Rectangle { RadiusX = 1, RadiusY = 1 }
                : new Ellipse();
            particle.Width = size;
            particle.Height = size;
            particle.Fill = index % 3 == 0 ? secondaryBrush : primaryBrush;
            particle.Opacity = 0.95;
            if (particle is Rectangle)
            {
                particle.RenderTransformOrigin = new Point(0.5, 0.5);
                particle.RenderTransform = new RotateTransform(45);
            }

            Canvas.SetLeft(particle, -size / 2);
            Canvas.SetTop(particle, -size / 2);
            effect.Children.Add(particle);

            var easing = new CubicEase { EasingMode = EasingMode.EaseOut };
            particle.BeginAnimation(Canvas.LeftProperty, new DoubleAnimation(
                -size / 2,
                (Math.Cos(angle) * distance) - (size / 2),
                profile.Duration)
            {
                EasingFunction = easing
            });
            particle.BeginAnimation(Canvas.TopProperty, new DoubleAnimation(
                -size / 2,
                (Math.Sin(angle) * distance) - (size / 2),
                profile.Duration)
            {
                EasingFunction = easing
            });
        }
    }

    private static void AddMatchGlyph(
        Canvas effect,
        SignalTileKind tileKind,
        Brush brush,
        TimeSpan duration)
    {
        var glyph = new TextBlock
        {
            Width = 48,
            Height = 48,
            Text = SignalTileGlyphConverter.ForKind(tileKind),
            FontFamily = new FontFamily("Segoe UI Symbol"),
            FontSize = 30,
            Foreground = brush,
            TextAlignment = TextAlignment.Center,
            RenderTransformOrigin = new Point(0.5, 0.5)
        };
        Canvas.SetLeft(glyph, -24);
        Canvas.SetTop(glyph, -24);
        var scale = new ScaleTransform(0.35, 0.35);
        glyph.RenderTransform = scale;
        effect.Children.Add(glyph);

        var growDuration = TimeSpan.FromTicks((long)(duration.Ticks * 0.52));
        var easing = new BackEase { Amplitude = 0.32, EasingMode = EasingMode.EaseOut };
        scale.BeginAnimation(ScaleTransform.ScaleXProperty, new DoubleAnimation(0.35, 1.55, growDuration)
        {
            EasingFunction = easing
        });
        scale.BeginAnimation(ScaleTransform.ScaleYProperty, new DoubleAnimation(0.35, 1.55, growDuration)
        {
            EasingFunction = easing
        });
    }

    private void PulseTray(TimeSpan effectDuration)
    {
        var pulseDuration = TimeSpan.FromTicks((long)(effectDuration.Ticks * 0.18));
        var easing = new QuadraticEase { EasingMode = EasingMode.EaseOut };
        TrayPulseScale.BeginAnimation(ScaleTransform.ScaleXProperty, new DoubleAnimation(1, 1.035, pulseDuration)
        {
            AutoReverse = true,
            EasingFunction = easing
        });
        TrayPulseScale.BeginAnimation(ScaleTransform.ScaleYProperty, new DoubleAnimation(1, 1.035, pulseDuration)
        {
            AutoReverse = true,
            EasingFunction = easing
        });
    }

    private void PlayCannonBlast()
    {
        var profile = TileVisualEffectProfile.For(TileVisualEffectKind.Match);
        var warmBrush = TryFindResource("WarmBrush") as Brush
            ?? TryFindResource("AccentBrush") as Brush
            ?? Brushes.White;
        var accentBrush = TryFindResource("AccentBrush") as Brush ?? warmBrush;
        var effect = CreateEffectLayer(TrayEffectCanvas, TrayEffectCanvas.Width / 2, TrayEffectCanvas.Height / 2);
        AddRings(effect, warmBrush, profile);
        AddParticles(effect, warmBrush, accentBrush, profile, upperFan: false);
        FadeAndRemove(TrayEffectCanvas, effect, profile.Duration);
        PulseTray(profile.Duration);

        var cards = GetBoardCardVisuals();
        if (cards.Count == 0)
        {
            return;
        }

        var sequence = ++_cardAnimationSequence;
        BoardHost.IsHitTestVisible = false;
        for (var index = 0; index < cards.Count; index++)
        {
            var card = cards[index];
            var delay = TimeSpan.FromMilliseconds(index * 1.4);
            var duration = TimeSpan.FromMilliseconds(980);
            var launch = new DoubleAnimationUsingKeyFrames
            {
                BeginTime = delay,
                Duration = duration
            };
            launch.KeyFrames.Add(new LinearDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.Zero)));
            launch.KeyFrames.Add(new SplineDoubleKeyFrame(
                -card.Tile.Top - 130,
                KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(220)),
                new KeySpline(0.15, 0.8, 0.2, 1)));
            launch.KeyFrames.Add(new SplineDoubleKeyFrame(
                ActualHeight - card.Tile.Top + 220,
                KeyTime.FromTimeSpan(duration),
                new KeySpline(0.55, 0, 1, 0.55)));
            card.Translate.BeginAnimation(TranslateTransform.YProperty, launch);

            var horizontalThrow = ((index % 9) - 4) * 34d;
            card.Translate.BeginAnimation(TranslateTransform.XProperty, new DoubleAnimation(0, horizontalThrow, duration)
            {
                BeginTime = delay,
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
            });
            card.Rotate.BeginAnimation(RotateTransform.AngleProperty, new DoubleAnimation(
                0,
                (index % 2 == 0 ? 1 : -1) * (130 + ((index % 5) * 28)),
                duration)
            {
                BeginTime = delay,
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
            });
            var fade = new DoubleAnimationUsingKeyFrames
            {
                BeginTime = delay,
                Duration = duration
            };
            fade.KeyFrames.Add(new LinearDoubleKeyFrame(1, KeyTime.FromTimeSpan(TimeSpan.Zero)));
            fade.KeyFrames.Add(new LinearDoubleKeyFrame(1, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(720))));
            fade.KeyFrames.Add(new LinearDoubleKeyFrame(0, KeyTime.FromTimeSpan(duration)));
            if (index == cards.Count - 1)
            {
                fade.Completed += (_, _) =>
                {
                    if (sequence == _cardAnimationSequence)
                    {
                        BeginCannonRestack(cards, sequence);
                    }
                };
            }

            card.Container.BeginAnimation(OpacityProperty, fade);
        }
    }

    private void PlayFanShuffle()
    {
        var cards = GetBoardCardVisuals();
        if (cards.Count == 0)
        {
            return;
        }

        var sequence = ++_cardAnimationSequence;
        BoardHost.IsHitTestVisible = false;
        for (var index = 0; index < cards.Count; index++)
        {
            var card = cards[index];
            var delay = TimeSpan.FromMilliseconds(index * 0.8);
            var duration = TimeSpan.FromMilliseconds(420);
            var wind = ((index % 7) - 3) * 38d;
            card.Translate.BeginAnimation(TranslateTransform.XProperty, new DoubleAnimation(0, wind, duration)
            {
                BeginTime = delay,
                EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
            });
            card.Translate.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(
                0,
                -card.Tile.Top - 520,
                duration)
            {
                BeginTime = delay,
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
            });
            card.Rotate.BeginAnimation(RotateTransform.AngleProperty, new DoubleAnimation(
                0,
                (index % 2 == 0 ? 1 : -1) * (24 + (index % 4 * 12)),
                duration)
            {
                BeginTime = delay,
                EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
            });
            var fade = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(210))
            {
                BeginTime = delay + TimeSpan.FromMilliseconds(210)
            };
            if (index == cards.Count - 1)
            {
                fade.Completed += (_, _) =>
                {
                    if (sequence == _cardAnimationSequence)
                    {
                        BeginFanRestack(cards, sequence);
                    }
                };
            }

            card.Container.BeginAnimation(OpacityProperty, fade);
        }
    }

    private void BeginFanRestack(IReadOnlyList<CardVisual> launchedCards, int sequence)
    {
        var cards = launchedCards
            .OrderBy(card => card.Tile.Layer)
            .ThenBy(card => card.Tile.Id)
            .ToArray();
        for (var index = 0; index < cards.Length; index++)
        {
            var card = cards[index];
            ClearCardAnimation(card);
            var corner = index % 4;
            card.Translate.X = corner is 0 or 2
                ? -190 - card.Tile.Left
                : 480 - card.Tile.Left;
            card.Translate.Y = corner < 2
                ? -390 - card.Tile.Top
                : 590 - card.Tile.Top;
            card.Rotate.Angle = corner % 2 == 0 ? -32 : 32;
            card.Scale.ScaleX = 0.58;
            card.Scale.ScaleY = 0.58;
            card.Container.Opacity = 0;

            var delay = TimeSpan.FromMilliseconds(index * 3);
            var duration = TimeSpan.FromMilliseconds(520);
            var easing = new CubicEase { EasingMode = EasingMode.EaseOut };
            card.Translate.BeginAnimation(TranslateTransform.XProperty, new DoubleAnimation(card.Translate.X, 0, duration)
            {
                BeginTime = delay,
                EasingFunction = easing
            });
            var verticalLanding = new DoubleAnimation(card.Translate.Y, 0, duration)
            {
                BeginTime = delay,
                EasingFunction = easing
            };
            if (index == cards.Length - 1)
            {
                verticalLanding.Completed += (_, _) => CompleteCardAnimation(cards, sequence);
            }

            card.Translate.BeginAnimation(TranslateTransform.YProperty, verticalLanding);
            card.Rotate.BeginAnimation(RotateTransform.AngleProperty, new DoubleAnimation(card.Rotate.Angle, 0, duration)
            {
                BeginTime = delay,
                EasingFunction = easing
            });
            card.Scale.BeginAnimation(ScaleTransform.ScaleXProperty, new DoubleAnimation(0.58, 1, duration)
            {
                BeginTime = delay,
                EasingFunction = easing
            });
            card.Scale.BeginAnimation(ScaleTransform.ScaleYProperty, new DoubleAnimation(0.58, 1, duration)
            {
                BeginTime = delay,
                EasingFunction = easing
            });
            card.Container.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(220))
            {
                BeginTime = delay
            });
        }
    }

    private void BeginCannonRestack(IReadOnlyList<CardVisual> launchedCards, int sequence)
    {
        var cards = launchedCards
            .OrderBy(card => card.Tile.Layer)
            .ThenBy(card => card.Tile.Id)
            .ToArray();
        for (var index = 0; index < cards.Length; index++)
        {
            var card = cards[index];
            ClearCardAnimation(card);
            card.Translate.X = ((index % 5) - 2) * 26;
            card.Translate.Y = -card.Tile.Top - 260;
            card.Rotate.Angle = (index % 2 == 0 ? -1 : 1) * 16;
            card.Scale.ScaleX = 0.72;
            card.Scale.ScaleY = 0.72;
            card.Container.Opacity = 0;

            var delay = TimeSpan.FromMilliseconds(index * 2.2);
            var duration = TimeSpan.FromMilliseconds(430);
            var easing = new BackEase { Amplitude = 0.18, EasingMode = EasingMode.EaseOut };
            card.Translate.BeginAnimation(TranslateTransform.XProperty, new DoubleAnimation(card.Translate.X, 0, duration)
            {
                BeginTime = delay,
                EasingFunction = easing
            });
            var verticalLanding = new DoubleAnimation(card.Translate.Y, 0, duration)
            {
                BeginTime = delay,
                EasingFunction = easing
            };
            if (index == cards.Length - 1)
            {
                verticalLanding.Completed += (_, _) => CompleteCardAnimation(cards, sequence);
            }

            card.Translate.BeginAnimation(TranslateTransform.YProperty, verticalLanding);
            card.Rotate.BeginAnimation(RotateTransform.AngleProperty, new DoubleAnimation(card.Rotate.Angle, 0, duration)
            {
                BeginTime = delay,
                EasingFunction = easing
            });
            card.Scale.BeginAnimation(ScaleTransform.ScaleXProperty, new DoubleAnimation(0.72, 1, duration)
            {
                BeginTime = delay,
                EasingFunction = easing
            });
            card.Scale.BeginAnimation(ScaleTransform.ScaleYProperty, new DoubleAnimation(0.72, 1, duration)
            {
                BeginTime = delay,
                EasingFunction = easing
            });
            card.Container.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(180))
            {
                BeginTime = delay
            });
        }
    }

    private List<CardVisual> GetBoardCardVisuals()
    {
        BoardItemsControl.UpdateLayout();
        var cards = new List<CardVisual>(BoardItemsControl.Items.Count);
        for (var index = 0; index < BoardItemsControl.Items.Count; index++)
        {
            if (BoardItemsControl.ItemContainerGenerator.ContainerFromIndex(index) is not ContentPresenter container
                || BoardItemsControl.Items[index] is not GameTileViewModel tile)
            {
                continue;
            }

            container.RenderTransformOrigin = new Point(0.5, 0.5);
            var scale = new ScaleTransform(1, 1);
            var rotate = new RotateTransform();
            var translate = new TranslateTransform();
            container.RenderTransform = new TransformGroup
            {
                Children = new TransformCollection { scale, rotate, translate }
            };
            cards.Add(new CardVisual(container, tile, scale, rotate, translate));
        }

        return cards;
    }

    private void ResetCardAnimations()
    {
        _cardAnimationSequence++;
        foreach (var card in GetBoardCardVisuals())
        {
            ClearCardAnimation(card);
        }

        BoardHost.IsHitTestVisible = true;
    }

    private void CompleteCardAnimation(IEnumerable<CardVisual> cards, int sequence)
    {
        if (sequence != _cardAnimationSequence)
        {
            return;
        }

        foreach (var card in cards)
        {
            ClearCardAnimation(card);
        }

        BoardHost.IsHitTestVisible = true;
    }

    private static void ClearCardAnimation(CardVisual card)
    {
        card.Container.BeginAnimation(OpacityProperty, null);
        card.Scale.BeginAnimation(ScaleTransform.ScaleXProperty, null);
        card.Scale.BeginAnimation(ScaleTransform.ScaleYProperty, null);
        card.Rotate.BeginAnimation(RotateTransform.AngleProperty, null);
        card.Translate.BeginAnimation(TranslateTransform.XProperty, null);
        card.Translate.BeginAnimation(TranslateTransform.YProperty, null);
        card.Container.Opacity = 1;
        card.Scale.ScaleX = 1;
        card.Scale.ScaleY = 1;
        card.Rotate.Angle = 0;
        card.Translate.X = 0;
        card.Translate.Y = 0;
    }

    private sealed record CardVisual(
        ContentPresenter Container,
        GameTileViewModel Tile,
        ScaleTransform Scale,
        RotateTransform Rotate,
        TranslateTransform Translate);

    private void PlayBoardEntrance(bool isNewGame)
    {
        var duration = TimeSpan.FromMilliseconds(isNewGame ? 420 : 280);
        var initialScale = isNewGame ? 0.9 : 0.96;
        var easing = new CubicEase { EasingMode = EasingMode.EaseOut };

        BoardHost.BeginAnimation(OpacityProperty, new DoubleAnimation(isNewGame ? 0.18 : 0.45, 1, duration)
        {
            EasingFunction = easing
        });
        BoardEntranceScale.BeginAnimation(ScaleTransform.ScaleXProperty, new DoubleAnimation(initialScale, 1, duration)
        {
            EasingFunction = easing
        });
        BoardEntranceScale.BeginAnimation(ScaleTransform.ScaleYProperty, new DoubleAnimation(initialScale, 1, duration)
        {
            EasingFunction = easing
        });
    }

    private void PlayCelebrationBanner()
    {
        HideCelebrationBanner();
        CelebrationOverlay.Visibility = Visibility.Visible;

        var opacity = new DoubleAnimationUsingKeyFrames
        {
            Duration = TimeSpan.FromMilliseconds(2700)
        };
        opacity.KeyFrames.Add(new LinearDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.Zero)));
        opacity.KeyFrames.Add(new SplineDoubleKeyFrame(1, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(180)),
            new KeySpline(0.2, 0.8, 0.2, 1)));
        opacity.KeyFrames.Add(new LinearDoubleKeyFrame(1, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(2100))));
        opacity.KeyFrames.Add(new SplineDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(2700)),
            new KeySpline(0.4, 0, 1, 1)));
        opacity.Completed += (_, _) => HideCelebrationBanner();
        CelebrationOverlay.BeginAnimation(OpacityProperty, opacity);

        AnimateCelebrationTransform(
            CelebrationScale,
            ScaleTransform.ScaleXProperty,
            0.92,
            1.035,
            1,
            1.04);
        AnimateCelebrationTransform(
            CelebrationScale,
            ScaleTransform.ScaleYProperty,
            0.92,
            1.035,
            1,
            1.04);
        AnimateCelebrationTransform(
            CelebrationTranslate,
            TranslateTransform.YProperty,
            -28,
            0,
            0,
            18);
    }

    private static void AnimateCelebrationTransform(
        Animatable target,
        DependencyProperty property,
        double start,
        double arrive,
        double hold,
        double exit)
    {
        var animation = new DoubleAnimationUsingKeyFrames
        {
            Duration = TimeSpan.FromMilliseconds(2700)
        };
        animation.KeyFrames.Add(new LinearDoubleKeyFrame(start, KeyTime.FromTimeSpan(TimeSpan.Zero)));
        animation.KeyFrames.Add(new SplineDoubleKeyFrame(arrive, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(260)),
            new KeySpline(0.18, 0.9, 0.25, 1)));
        animation.KeyFrames.Add(new SplineDoubleKeyFrame(hold, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(520)),
            new KeySpline(0.4, 0, 0.2, 1)));
        animation.KeyFrames.Add(new LinearDoubleKeyFrame(hold, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(2100))));
        animation.KeyFrames.Add(new SplineDoubleKeyFrame(exit, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(2700)),
            new KeySpline(0.4, 0, 1, 1)));
        target.BeginAnimation(property, animation);
    }

    private void HideCelebrationBanner()
    {
        CelebrationOverlay.BeginAnimation(OpacityProperty, null);
        CelebrationScale.BeginAnimation(ScaleTransform.ScaleXProperty, null);
        CelebrationScale.BeginAnimation(ScaleTransform.ScaleYProperty, null);
        CelebrationTranslate.BeginAnimation(TranslateTransform.YProperty, null);
        CelebrationOverlay.Opacity = 0;
        CelebrationScale.ScaleX = 0.92;
        CelebrationScale.ScaleY = 0.92;
        CelebrationTranslate.Y = -28;
        CelebrationOverlay.Visibility = Visibility.Collapsed;
    }

    private static void FadeAndRemove(Canvas host, Canvas effect, TimeSpan duration)
    {
        var fadeStart = TimeSpan.FromTicks((long)(duration.Ticks * 0.52));
        var fadeDuration = duration - fadeStart;
        var fade = new DoubleAnimation(1, 0, fadeDuration)
        {
            BeginTime = fadeStart,
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
        };
        fade.Completed += (_, _) => host.Children.Remove(effect);
        effect.BeginAnimation(OpacityProperty, fade);
    }
}
