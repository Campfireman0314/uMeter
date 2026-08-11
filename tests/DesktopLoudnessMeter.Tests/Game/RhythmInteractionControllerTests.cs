using DesktopLoudnessMeter.App.Game;

namespace DesktopLoudnessMeter.Tests.Game;

public sealed class RhythmInteractionControllerTests
{
    [Fact]
    public void ActionJustAfterBeatBuildsPerfectGroove()
    {
        var controller = new RhythmInteractionController();
        controller.Update(1, isBeat: true);

        var frame = controller.RegisterAction(1.04);

        Assert.Equal(RhythmTimingGrade.Perfect, frame.TimingGrade);
        Assert.InRange(frame.Groove, 0.19, 0.21);
        Assert.Equal(1, frame.PhraseStep);
    }

    [Fact]
    public void ActionJustBeforeBeatIsMatchedCausallyWhenBeatArrives()
    {
        var controller = new RhythmInteractionController();
        controller.RegisterAction(0.9);

        var frame = controller.Update(1, isBeat: true);

        Assert.Equal(RhythmTimingGrade.Good, frame.TimingGrade);
        Assert.InRange(frame.Groove, 0.11, 0.13);
    }

    [Fact]
    public void OneBeatScoresOnlyOneNearbyAction()
    {
        var controller = new RhythmInteractionController();
        controller.Update(1, isBeat: true);
        var first = controller.RegisterAction(1.02);
        var second = controller.RegisterAction(1.03);

        Assert.Equal(RhythmTimingGrade.Perfect, first.TimingGrade);
        Assert.InRange(second.Groove, first.Groove - 0.001, first.Groove);
    }

    [Fact]
    public void OffBeatActionDoesNotReduceExistingGroove()
    {
        var controller = new RhythmInteractionController();
        controller.Update(1, isBeat: true);
        controller.RegisterAction(1.03);
        var before = controller.Update(1.5, isBeat: false);

        var after = controller.RegisterAction(1.5);

        Assert.Equal(before.Groove, after.Groove, 6);
    }

    [Fact]
    public void PhraseIndicatorCyclesAcrossFourAcceptedBeats()
    {
        var controller = new RhythmInteractionController();
        var steps = Enumerable.Range(0, 5)
            .Select(index => controller.Update(index * 0.5, isBeat: true).PhraseStep)
            .ToArray();

        Assert.Equal([1, 2, 3, 4, 1], steps);
    }

    [Fact]
    public void StableIntervalsProduceConfidentTempoEstimate()
    {
        var controller = new RhythmInteractionController();
        RhythmInteractionFrame frame = default;
        for (var index = 0; index < 7; index++)
        {
            frame = controller.Update(index * 0.5, isBeat: true);
        }

        Assert.InRange(frame.TempoBpm, 119.9, 120.1);
        Assert.True(frame.TempoConfidence >= 0.72);

        var betweenBeats = controller.Update(3.25, isBeat: false);
        Assert.InRange(betweenBeats.PredictedBeatPhase, 0.49, 0.51);
    }

    [Fact]
    public void IrregularIntervalsDoNotClaimConfidentTempo()
    {
        var controller = new RhythmInteractionController();
        RhythmInteractionFrame frame = default;
        foreach (var timestamp in new[] { 0d, 0.3, 1.1, 1.5, 2.4, 2.75 })
        {
            frame = controller.Update(timestamp, isBeat: true);
        }

        Assert.True(frame.TempoConfidence < 0.72);
        Assert.Equal(0, frame.PredictedBeatPhase);
    }
}
