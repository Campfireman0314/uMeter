using DesktopLoudnessMeter.App.Game;

namespace DesktopLoudnessMeter.Tests.Game;

public sealed class AudioDanceControllerTests
{
    [Fact]
    public void TransientStartsAVisibleBoundedVerticalJump()
    {
        var controller = new AudioDanceController();
        controller.Update(0, 0, 1d / 30);

        var frame = controller.Update(0.8, 1, 1d / 30);

        Assert.InRange(frame.Energy, 0.35, 1);
        Assert.InRange(frame.Bounce, 3, 9);
        Assert.InRange(frame.JumpProgress, 0.05, 0.12);
        Assert.True(frame.IsJumping);
        AssertFrameIsBounded(frame);
        Assert.InRange(frame.LeftForelegAngle, 45, 75);
        Assert.Equal(-frame.LeftForelegAngle, frame.RightForelegAngle, 6);
        Assert.Equal(-frame.LeftElbowAngle, frame.RightElbowAngle, 6);
        Assert.Equal(-frame.LeftForehoofAngle, frame.RightForehoofAngle, 6);
        Assert.Equal(-frame.LeftKneeAngle, frame.RightKneeAngle, 6);
        Assert.NotEqual(frame.LeftEarAngle, frame.LeftEarTipAngle);
    }

    [Fact]
    public void JumpRisesToAnApexAndReturnsToGround()
    {
        var controller = new AudioDanceController();
        controller.Update(0, 0, 1d / 30);
        controller.Update(0.75, 1, 1d / 30);

        var frames = Enumerable.Range(0, 18)
            .Select(_ => controller.Update(0.35, 0, 1d / 30))
            .ToArray();
        var apex = frames.MaxBy(frame => frame.Bounce);
        var landed = frames[^1];

        Assert.True(apex.IsJumping);
        Assert.InRange(apex.JumpProgress, 0.35, 0.65);
        Assert.True(apex.Bounce > 15);
        Assert.False(landed.IsJumping);
        Assert.Equal(1, landed.JumpProgress);
        Assert.InRange(landed.Bounce, 0, 2.5);
        Assert.InRange(landed.LeftForelegAngle, 41.99, 42.01);
        Assert.InRange(landed.RightForelegAngle, -42.01, -41.99);
        Assert.InRange(Math.Abs(landed.LeftElbowAngle), 0, 0.01);
        Assert.InRange(Math.Abs(landed.RightElbowAngle), 0, 0.01);
    }

    [Fact]
    public void BeatInsideMinimumIntervalDoesNotRestartJump()
    {
        var controller = new AudioDanceController();
        controller.Update(0, 0, 1d / 30);
        controller.Update(0.8, 1, 1d / 30);

        SheepDanceFrame beforeCloseBeat = default;
        for (var index = 0; index < 4; index++)
        {
            beforeCloseBeat = controller.Update(0.5, 0, 1d / 30);
        }

        var ignoredBeat = controller.Update(0.8, 1, 1d / 30);

        Assert.True(ignoredBeat.IsJumping);
        Assert.True(ignoredBeat.JumpProgress > beforeCloseBeat.JumpProgress);
        Assert.True(ignoredBeat.JumpProgress > 0.35);

        controller.Update(0.2, 0, 1d / 30);
        for (var index = 0; index < 12; index++)
        {
            controller.Update(0.2, 0, 1d / 30);
        }

        var nextAcceptedBeat = controller.Update(0.8, 1, 1d / 30);
        Assert.True(nextAcceptedBeat.IsJumping);
        Assert.InRange(nextAcceptedBeat.JumpProgress, 0.05, 0.12);
    }

    [Fact]
    public void ArmsTravelFromLowVToHighVAndBackDuringJump()
    {
        var controller = new AudioDanceController();
        var rest = controller.Update(0, 0, 1d / 30);
        controller.Update(0.8, 1, 1d / 30);
        SheepDanceFrame apex = default;
        for (var index = 0; index < 6; index++)
        {
            apex = controller.Update(0.4, 0, 1d / 30);
        }

        SheepDanceFrame landed = default;
        for (var index = 0; index < 8; index++)
        {
            landed = controller.Update(0.1, 0, 1d / 30);
        }

        Assert.InRange(rest.LeftForelegAngle, 41.99, 42.01);
        Assert.InRange(rest.RightForelegAngle, -42.01, -41.99);
        Assert.True(apex.LeftForelegAngle > 125);
        Assert.True(apex.RightForelegAngle < -125);
        Assert.Equal(-apex.LeftForelegAngle, apex.RightForelegAngle, 6);
        Assert.InRange(landed.LeftForelegAngle, 41.99, 42.01);
        Assert.InRange(landed.RightForelegAngle, -42.01, -41.99);
    }

    [Fact]
    public void SecondaryMotionCarriesMomentumAfterTakeoff()
    {
        var controller = new AudioDanceController();
        controller.Update(0, 0, 1d / 30);
        var takeoff = controller.Update(0.85, 1, 1d / 30);
        var followThrough = Enumerable.Range(0, 4)
            .Select(_ => controller.Update(0.25, 0, 1d / 30))
            .ToArray();

        Assert.NotEqual(takeoff.LeftEarAngle, takeoff.LeftEarTipAngle);
        Assert.NotEqual(takeoff.TailAngle, takeoff.TailTipAngle);
        Assert.True(followThrough.Max(frame => Math.Abs(frame.TailAngle)) > Math.Abs(takeoff.TailAngle));
        Assert.Contains(followThrough, frame => Math.Abs(frame.LeftWoolFringeAngle) > 0.1);
    }

    [Fact]
    public void SilenceSettlesTheDance()
    {
        var controller = new AudioDanceController();
        controller.Update(0.9, 1, 1d / 30);

        SheepDanceFrame frame = default;
        for (var index = 0; index < 180; index++)
        {
            frame = controller.Update(0, 0, 1d / 30);
        }

        Assert.InRange(frame.Energy, 0, 0.001);
        Assert.InRange(frame.Bounce, 0, 0.01);
        Assert.Equal(1, frame.JumpProgress);
        Assert.False(frame.IsJumping);
        Assert.InRange(Math.Abs(frame.TorsoTwist), 0, 0.01);
        Assert.InRange(Math.Abs(frame.HeadAngle), 0, 0.01);
        Assert.InRange(Math.Abs(frame.HeadBob), 0, 0.01);
        Assert.InRange(frame.LeftForelegAngle, 41.99, 42.01);
        Assert.InRange(frame.RightForelegAngle, -42.01, -41.99);
        Assert.InRange(Math.Abs(frame.LeftElbowAngle), 0, 0.01);
        Assert.InRange(Math.Abs(frame.RightElbowAngle), 0, 0.01);
        Assert.InRange(Math.Abs(frame.LeftForehoofAngle), 0, 0.01);
        Assert.InRange(Math.Abs(frame.RightForehoofAngle), 0, 0.01);
        Assert.InRange(Math.Abs(frame.LeftHindLegAngle), 0, 0.01);
        Assert.InRange(Math.Abs(frame.RightHindLegAngle), 0, 0.01);
        Assert.InRange(Math.Abs(frame.LeftKneeAngle), 0, 0.01);
        Assert.InRange(Math.Abs(frame.RightKneeAngle), 0, 0.01);
        Assert.InRange(Math.Abs(frame.LeftEarTipAngle), 0, 0.01);
        Assert.InRange(Math.Abs(frame.RightEarTipAngle), 0, 0.01);
        Assert.InRange(Math.Abs(frame.TailAngle), 0, 0.01);
        Assert.InRange(Math.Abs(frame.TailTipAngle), 0, 0.01);
        Assert.InRange(Math.Abs(frame.LeftWoolFringeAngle), 0, 0.01);
        Assert.InRange(Math.Abs(frame.RightWoolFringeAngle), 0, 0.01);
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(-10)]
    [InlineData(10)]
    public void InvalidOrExtremeLevelsRemainFiniteAndBounded(double level)
    {
        var frame = new AudioDanceController().Update(level, level, double.NaN);

        AssertFrameIsBounded(frame);
    }

    private static void AssertFrameIsBounded(SheepDanceFrame frame)
    {
        Assert.InRange(frame.Energy, 0, 1);
        Assert.InRange(frame.Bounce, 0, 24);
        Assert.InRange(frame.JumpProgress, 0, 1);
        Assert.InRange(frame.Lean, -14, 14);
        Assert.InRange(frame.Squash, 0.93, 1.02);
        Assert.InRange(frame.TorsoTwist, -12, 12);
        Assert.InRange(frame.HeadAngle, -18, 18);
        Assert.InRange(frame.HeadBob, -8, 0);
        Assert.InRange(frame.EyeScaleY, 0.1, 1);
        Assert.InRange(frame.MouthScaleY, 0.18, 1);
        Assert.InRange(frame.LeftForelegAngle, -145, 145);
        Assert.InRange(frame.RightForelegAngle, -145, 145);
        Assert.InRange(frame.LeftElbowAngle, -110, 110);
        Assert.InRange(frame.RightElbowAngle, -110, 110);
        Assert.InRange(frame.LeftForehoofAngle, -45, 45);
        Assert.InRange(frame.RightForehoofAngle, -45, 45);
        Assert.InRange(frame.LeftHindLegAngle, -70, 70);
        Assert.InRange(frame.RightHindLegAngle, -70, 70);
        Assert.InRange(frame.LeftKneeAngle, -115, 115);
        Assert.InRange(frame.RightKneeAngle, -115, 115);
        Assert.InRange(frame.LeftHindHoofAngle, -45, 45);
        Assert.InRange(frame.RightHindHoofAngle, -45, 45);
        Assert.InRange(frame.LeftEarAngle, -24, 24);
        Assert.InRange(frame.RightEarAngle, -24, 24);
        Assert.InRange(frame.LeftEarTipAngle, -30, 30);
        Assert.InRange(frame.RightEarTipAngle, -30, 30);
        Assert.InRange(frame.TailAngle, -28, 28);
        Assert.InRange(frame.TailTipAngle, -32, 32);
        Assert.InRange(frame.LeftWoolFringeAngle, -16, 16);
        Assert.InRange(frame.RightWoolFringeAngle, -16, 16);
    }
}
