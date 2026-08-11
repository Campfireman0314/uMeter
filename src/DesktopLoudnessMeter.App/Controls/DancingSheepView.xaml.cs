using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace DesktopLoudnessMeter.App.Controls;

public partial class DancingSheepView : UserControl
{
    private static readonly KeySpline CheerSpline = new(0.2, 0.78, 0.2, 1);
    private static readonly KeySpline FallSpline = new(0.35, 0, 0.8, 0.35);

    public DancingSheepView()
    {
        InitializeComponent();
    }

    public void PlayWinCheer()
    {
        ResetOutcomeAnimation();
        const double duration = 2180;
        Animate(OutcomeTranslateTransform, TranslateTransform.YProperty, duration, FillBehavior.Stop,
            (0, 0), (120, 3), (280, -48), (390, -48), (560, 0),
            (720, 3), (860, -52), (980, -52), (1140, 0),
            (1300, 3), (1440, -55), (1560, -55), (1740, 0), (2180, 0));
        Animate(OutcomeBodyRotateTransform, RotateTransform.AngleProperty, duration, FillBehavior.Stop,
            (0, 0), (280, -4), (390, -4), (560, 0),
            (860, 4), (980, 4), (1140, 0),
            (1440, -4), (1560, -4), (1740, 0), (2180, 0));
        Animate(OutcomeScaleTransform, ScaleTransform.ScaleXProperty, duration, FillBehavior.Stop,
            (0, 1), (120, 1.045), (280, 0.975), (390, 0.975), (560, 1),
            (720, 1.045), (860, 0.975), (980, 0.975), (1140, 1),
            (1300, 1.045), (1440, 0.975), (1560, 0.975), (1740, 1), (2180, 1));
        Animate(OutcomeScaleTransform, ScaleTransform.ScaleYProperty, duration, FillBehavior.Stop,
            (0, 1), (120, 0.94), (280, 1.035), (390, 1.035), (560, 1),
            (720, 0.94), (860, 1.035), (980, 1.035), (1140, 1),
            (1300, 0.94), (1440, 1.035), (1560, 1.035), (1740, 1), (2180, 1));

        AnimateCheerJoint(LeftShoulderTransform, 42, 140, duration);
        AnimateCheerJoint(RightShoulderTransform, -42, -140, duration);
        AnimateCheerJoint(LeftKneeTransform, 0, 42, duration);
        AnimateCheerJoint(RightKneeTransform, 0, -42, duration);
        AnimateCheerJoint(LeftHipTransform, 0, 12, duration);
        AnimateCheerJoint(RightHipTransform, 0, -12, duration);
        AnimateCheerJoint(LeftForehoofTransform, 0, 10, duration);
        AnimateCheerJoint(RightForehoofTransform, 0, -10, duration);
        Animate(MouthScaleTransform, ScaleTransform.ScaleYProperty, duration, FillBehavior.Stop,
            (0, 0.28), (280, 0.92), (390, 0.92), (560, 0.28),
            (860, 0.96), (980, 0.96), (1140, 0.28),
            (1440, 1), (1560, 1), (1740, 0.28), (2180, 0.28));
        Animate(TailRootTransform, RotateTransform.AngleProperty, duration, FillBehavior.Stop,
            (0, 0), (180, -18), (360, 24), (620, -20), (860, 25),
            (1120, -22), (1380, 26), (1660, -18), (1960, 12), (2180, 0));
        Animate(TailTipTransform, RotateTransform.AngleProperty, duration, FillBehavior.Stop,
            (0, 0), (220, 24), (430, -30), (700, 28), (940, -30),
            (1210, 30), (1490, -28), (1780, 24), (2180, 0));
    }

    public void PlayLossRagdoll()
    {
        ResetOutcomeAnimation();
        const double duration = 1420;
        Animate(OutcomeBodyRotateTransform, RotateTransform.AngleProperty, duration, FillBehavior.HoldEnd,
            (0, 0), (150, 8), (420, -20), (900, -52), (1120, -42), (1420, -48));
        Animate(OutcomeTranslateTransform, TranslateTransform.XProperty, duration, FillBehavior.HoldEnd,
            (0, 0), (220, 3), (900, 20), (1420, 25));
        Animate(OutcomeTranslateTransform, TranslateTransform.YProperty, duration, FillBehavior.HoldEnd,
            (0, 0), (180, -5), (650, 14), (980, 42), (1160, 32), (1420, 37));
        Animate(OutcomeScaleTransform, ScaleTransform.ScaleXProperty, duration, FillBehavior.HoldEnd,
            (0, 1), (900, 1.02), (1120, 1.08), (1420, 1.04));
        Animate(OutcomeScaleTransform, ScaleTransform.ScaleYProperty, duration, FillBehavior.HoldEnd,
            (0, 1), (900, 0.98), (1120, 0.9), (1420, 0.94));

        Animate(LeftShoulderTransform, RotateTransform.AngleProperty, duration, FillBehavior.HoldEnd,
            (0, CurrentAngle(LeftShoulderTransform)), (250, -48), (720, 78), (1080, 38), (1420, 52));
        Animate(RightShoulderTransform, RotateTransform.AngleProperty, duration, FillBehavior.HoldEnd,
            (0, CurrentAngle(RightShoulderTransform)), (300, 72), (760, -82), (1110, -28), (1420, -18));
        Animate(LeftElbowTransform, RotateTransform.AngleProperty, duration, FillBehavior.HoldEnd,
            (0, CurrentAngle(LeftElbowTransform)), (430, 96), (900, 54), (1420, 68));
        Animate(RightElbowTransform, RotateTransform.AngleProperty, duration, FillBehavior.HoldEnd,
            (0, CurrentAngle(RightElbowTransform)), (460, -104), (920, -58), (1420, -72));
        Animate(LeftForehoofTransform, RotateTransform.AngleProperty, duration, FillBehavior.HoldEnd,
            (0, CurrentAngle(LeftForehoofTransform)), (540, -32), (980, 24), (1420, 14));
        Animate(RightForehoofTransform, RotateTransform.AngleProperty, duration, FillBehavior.HoldEnd,
            (0, CurrentAngle(RightForehoofTransform)), (560, 34), (1000, -26), (1420, -16));

        Animate(LeftHipTransform, RotateTransform.AngleProperty, duration, FillBehavior.HoldEnd,
            (0, CurrentAngle(LeftHipTransform)), (360, -42), (900, -18), (1420, -28));
        Animate(RightHipTransform, RotateTransform.AngleProperty, duration, FillBehavior.HoldEnd,
            (0, CurrentAngle(RightHipTransform)), (380, 58), (920, 31), (1420, 42));
        Animate(LeftKneeTransform, RotateTransform.AngleProperty, duration, FillBehavior.HoldEnd,
            (0, CurrentAngle(LeftKneeTransform)), (520, 104), (980, 66), (1420, 78));
        Animate(RightKneeTransform, RotateTransform.AngleProperty, duration, FillBehavior.HoldEnd,
            (0, CurrentAngle(RightKneeTransform)), (540, -110), (1000, -70), (1420, -82));
        Animate(LeftHindHoofTransform, RotateTransform.AngleProperty, duration, FillBehavior.HoldEnd,
            (0, CurrentAngle(LeftHindHoofTransform)), (700, -38), (1420, -22));
        Animate(RightHindHoofTransform, RotateTransform.AngleProperty, duration, FillBehavior.HoldEnd,
            (0, CurrentAngle(RightHindHoofTransform)), (720, 40), (1420, 24));

        Animate(HeadRotateTransform, RotateTransform.AngleProperty, duration, FillBehavior.HoldEnd,
            (0, CurrentAngle(HeadRotateTransform)), (260, -24), (700, 38), (1060, 12), (1420, 24));
        Animate(HeadTranslateTransform, TranslateTransform.YProperty, duration, FillBehavior.HoldEnd,
            (0, 0), (450, 8), (960, 19), (1420, 16));
        Animate(LeftEarTransform, RotateTransform.AngleProperty, duration, FillBehavior.HoldEnd,
            (0, CurrentAngle(LeftEarTransform)), (520, -22), (1040, 18), (1420, 12));
        Animate(RightEarTransform, RotateTransform.AngleProperty, duration, FillBehavior.HoldEnd,
            (0, CurrentAngle(RightEarTransform)), (540, 24), (1060, -20), (1420, -14));
        Animate(LeftEarTipTransform, RotateTransform.AngleProperty, duration, FillBehavior.HoldEnd,
            (0, CurrentAngle(LeftEarTipTransform)), (620, 28), (1120, -24), (1420, -18));
        Animate(RightEarTipTransform, RotateTransform.AngleProperty, duration, FillBehavior.HoldEnd,
            (0, CurrentAngle(RightEarTipTransform)), (640, -30), (1140, 26), (1420, 19));
        Animate(TailRootTransform, RotateTransform.AngleProperty, duration, FillBehavior.HoldEnd,
            (0, CurrentAngle(TailRootTransform)), (380, 28), (860, -22), (1180, 14), (1420, 8));
        Animate(TailTipTransform, RotateTransform.AngleProperty, duration, FillBehavior.HoldEnd,
            (0, CurrentAngle(TailTipTransform)), (460, -32), (920, 30), (1240, -18), (1420, -12));
        Animate(LeftEyeScaleTransform, ScaleTransform.ScaleYProperty, duration, FillBehavior.HoldEnd,
            (0, 1), (620, 0.55), (980, 0.12), (1420, 0.12));
        Animate(RightEyeScaleTransform, ScaleTransform.ScaleYProperty, duration, FillBehavior.HoldEnd,
            (0, 1), (650, 0.5), (1010, 0.12), (1420, 0.12));
        Animate(MouthScaleTransform, ScaleTransform.ScaleYProperty, duration, FillBehavior.HoldEnd,
            (0, 0.28), (620, 0.16), (980, 0.08), (1420, 0.08));
    }

    public void ResetOutcomeAnimation()
    {
        ClearAnimation(OutcomeScaleTransform, ScaleTransform.ScaleXProperty);
        ClearAnimation(OutcomeScaleTransform, ScaleTransform.ScaleYProperty);
        ClearAnimation(OutcomeBodyRotateTransform, RotateTransform.AngleProperty);
        ClearAnimation(OutcomeTranslateTransform, TranslateTransform.XProperty);
        ClearAnimation(OutcomeTranslateTransform, TranslateTransform.YProperty);
        OutcomeScaleTransform.ScaleX = 1;
        OutcomeScaleTransform.ScaleY = 1;
        OutcomeBodyRotateTransform.Angle = 0;
        OutcomeTranslateTransform.X = 0;
        OutcomeTranslateTransform.Y = 0;

        foreach (var transform in JointTransforms())
        {
            ClearAnimation(transform, RotateTransform.AngleProperty);
        }

        ClearAnimation(HeadTranslateTransform, TranslateTransform.YProperty);
        ClearAnimation(LeftEyeScaleTransform, ScaleTransform.ScaleYProperty);
        ClearAnimation(RightEyeScaleTransform, ScaleTransform.ScaleYProperty);
        ClearAnimation(MouthScaleTransform, ScaleTransform.ScaleYProperty);
    }

    private void AnimateCheerJoint(RotateTransform transform, double rest, double apex, double duration)
    {
        Animate(transform, RotateTransform.AngleProperty, duration, FillBehavior.Stop,
            (0, rest), (120, rest), (280, apex), (390, apex), (560, rest),
            (720, rest), (860, apex), (980, apex), (1140, rest),
            (1300, rest), (1440, apex), (1560, apex), (1740, rest), (2180, rest));
    }

    private static void Animate(
        Animatable target,
        DependencyProperty property,
        double durationMilliseconds,
        FillBehavior fillBehavior,
        params (double Milliseconds, double Value)[] frames)
    {
        var animation = new DoubleAnimationUsingKeyFrames
        {
            Duration = TimeSpan.FromMilliseconds(durationMilliseconds),
            FillBehavior = fillBehavior
        };
        foreach (var frame in frames)
        {
            var spline = fillBehavior == FillBehavior.HoldEnd && frame.Milliseconds >= 420
                ? FallSpline
                : CheerSpline;
            animation.KeyFrames.Add(new SplineDoubleKeyFrame(
                frame.Value,
                KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(frame.Milliseconds)),
                spline));
        }

        target.BeginAnimation(property, animation);
    }

    private IEnumerable<RotateTransform> JointTransforms()
    {
        yield return TailRootTransform;
        yield return TailTipTransform;
        yield return LeftHipTransform;
        yield return LeftKneeTransform;
        yield return LeftHindHoofTransform;
        yield return RightHipTransform;
        yield return RightKneeTransform;
        yield return RightHindHoofTransform;
        yield return LeftShoulderTransform;
        yield return LeftElbowTransform;
        yield return LeftForehoofTransform;
        yield return RightShoulderTransform;
        yield return RightElbowTransform;
        yield return RightForehoofTransform;
        yield return TorsoTransform;
        yield return HeadRotateTransform;
        yield return LeftEarTransform;
        yield return LeftEarTipTransform;
        yield return RightEarTransform;
        yield return RightEarTipTransform;
    }

    private static double CurrentAngle(RotateTransform transform)
    {
        return double.IsFinite(transform.Angle) ? transform.Angle : 0;
    }

    private static void ClearAnimation(Animatable target, DependencyProperty property)
    {
        target.BeginAnimation(property, null);
    }
}
