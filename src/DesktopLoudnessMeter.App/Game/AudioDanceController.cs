namespace DesktopLoudnessMeter.App.Game;

public readonly record struct SheepDanceFrame(
    double Energy,
    double Bounce,
    double JumpProgress,
    bool IsJumping,
    double Lean,
    double Squash,
    double TorsoTwist,
    double HeadAngle,
    double HeadBob,
    double EyeScaleY,
    double MouthScaleY,
    double LeftForelegAngle,
    double RightForelegAngle,
    double LeftElbowAngle,
    double RightElbowAngle,
    double LeftForehoofAngle,
    double RightForehoofAngle,
    double LeftHindLegAngle,
    double RightHindLegAngle,
    double LeftKneeAngle,
    double RightKneeAngle,
    double LeftHindHoofAngle,
    double RightHindHoofAngle,
    double LeftEarAngle,
    double RightEarAngle,
    double LeftEarTipAngle,
    double RightEarTipAngle,
    double TailAngle,
    double TailTipAngle,
    double LeftWoolFringeAngle,
    double RightWoolFringeAngle);

public sealed class AudioDanceController
{
    internal const double JumpDurationSeconds = 0.46;
    internal const double MinimumJumpIntervalSeconds = 0.32;

    private const double MinimumDeltaSeconds = 1d / 240;
    private const double MaximumDeltaSeconds = 0.1;
    private readonly SecondaryMotionChannel _leftEarTip = new();
    private readonly SecondaryMotionChannel _rightEarTip = new();
    private readonly SecondaryMotionChannel _tail = new();
    private readonly SecondaryMotionChannel _tailTip = new();
    private readonly SecondaryMotionChannel _leftFringe = new();
    private readonly SecondaryMotionChannel _rightFringe = new();
    private double _envelope;
    private double _pulse;
    private double _phase;
    private double _blinkClock;
    private double _previousBeatPulse;
    private double _jumpElapsed = JumpDurationSeconds;
    private double _jumpCooldownRemaining;

    public SheepDanceFrame Update(double level, double beatPulse, double deltaSeconds)
    {
        level = double.IsFinite(level) ? Math.Clamp(level, 0, 1) : 0;
        beatPulse = double.IsFinite(beatPulse) ? Math.Clamp(beatPulse, 0, 1) : 0;
        deltaSeconds = double.IsFinite(deltaSeconds)
            ? Math.Clamp(deltaSeconds, MinimumDeltaSeconds, MaximumDeltaSeconds)
            : 1d / 30;

        var timeConstant = level > _envelope ? 0.045 : 0.24;
        var follow = 1 - Math.Exp(-deltaSeconds / timeConstant);
        _envelope += (level - _envelope) * follow;

        _pulse *= Math.Exp(-deltaSeconds * 7.5);
        _jumpCooldownRemaining = Math.Max(0, _jumpCooldownRemaining - deltaSeconds);
        var isOnset = beatPulse >= 0.72 && _previousBeatPulse < 0.72;
        if (isOnset && _jumpCooldownRemaining <= 0)
        {
            _pulse = 1;
            _jumpElapsed = 0;
            _jumpCooldownRemaining = MinimumJumpIntervalSeconds;
        }

        _previousBeatPulse = beatPulse;
        _jumpElapsed = Math.Min(JumpDurationSeconds, _jumpElapsed + deltaSeconds);
        _phase = (_phase + (deltaSeconds * (2.8 + (_envelope * 3.5)))) % (Math.PI * 2);
        _blinkClock = (_blinkClock + deltaSeconds) % 3.6;

        var jumpProgress = _jumpElapsed / JumpDurationSeconds;
        var isJumping = _jumpElapsed < JumpDurationSeconds;
        var jumpArc = isJumping ? Math.Sin(jumpProgress * Math.PI) : 0;
        var groove = (Math.Sin(_phase * 2) + 1) / 2;
        var swing = Math.Sin(_phase);
        var jumpStrength = 0.8 + (_envelope * 0.2);

        var bounce = (_envelope * (0.8 + (groove * 1.8))) + (jumpArc * (18 + (_envelope * 4)) * jumpStrength);
        var lean = swing * _envelope * 0.8;
        var takeoffCompression = PulseWindow(jumpProgress, 0, 0.16);
        var landingCompression = PulseWindow(jumpProgress, 0.78, 1);
        var squash = 1 + (jumpArc * 0.018)
            - (takeoffCompression * 0.025)
            - (landingCompression * 0.05)
            - (_envelope * 0.01);
        var torsoTwist = -swing * _envelope;
        var headAngle = -swing * _envelope * 1.4;
        var headBob = -(jumpArc * 2.2) - (_envelope * (0.5 + groove));

        var cheerLift = SmoothStep(Math.Clamp(jumpArc * 1.15, 0, 1));
        var leftShoulder = 42 + (cheerLift * 93);
        var rightShoulder = -leftShoulder;
        var elbowFollowThrough = Math.Sin(jumpProgress * Math.PI * 2) * jumpArc * 4;
        var leftElbow = elbowFollowThrough;
        var rightElbow = -elbowFollowThrough;
        var leftWrist = cheerLift * 6;
        var rightWrist = -leftWrist;

        var leftHip = jumpArc * 8;
        var rightHip = -leftHip;
        var leftKnee = jumpArc * 34;
        var rightKnee = -leftKnee;
        var leftAnkle = jumpArc * -5;
        var rightAnkle = -leftAnkle;

        var leftEar = (-swing * _envelope * 1.5) - (jumpArc * 5);
        var rightEar = (swing * _envelope * 1.5) + (jumpArc * 5);
        var leftEarTipTarget = (leftEar * 0.62) - (jumpArc * 3);
        var rightEarTipTarget = (rightEar * 0.62) + (jumpArc * 3);
        var tailTarget = (swing * _envelope * 6) + (jumpArc * 10);
        var tail = _tail.Update(tailTarget, deltaSeconds, 5.8, 0.66);
        var tailTip = _tailTip.Update((tail * 0.7) + (jumpArc * 5), deltaSeconds, 7.2, 0.6);
        var leftFringeTarget = (-lean * 0.42) - (jumpArc * 3);
        var rightFringeTarget = (-lean * 0.42) + (jumpArc * 3);

        var eyeScale = CalculateEyeScale(_blinkClock);
        var mouthScale = 0.22 + (_envelope * 0.1) + (jumpArc * 0.36);

        return new SheepDanceFrame(
            Math.Clamp(_envelope + (_pulse * 0.35), 0, 1),
            Math.Clamp(bounce, 0, 24),
            Math.Clamp(jumpProgress, 0, 1),
            isJumping,
            Math.Clamp(lean, -14, 14),
            Math.Clamp(squash, 0.93, 1.02),
            Math.Clamp(torsoTwist, -12, 12),
            Math.Clamp(headAngle, -18, 18),
            Math.Clamp(headBob, -8, 0),
            eyeScale,
            Math.Clamp(mouthScale, 0.18, 1),
            Math.Clamp(leftShoulder, -145, 145),
            Math.Clamp(rightShoulder, -145, 145),
            Math.Clamp(leftElbow, -110, 110),
            Math.Clamp(rightElbow, -110, 110),
            Math.Clamp(leftWrist, -45, 45),
            Math.Clamp(rightWrist, -45, 45),
            Math.Clamp(leftHip, -70, 70),
            Math.Clamp(rightHip, -70, 70),
            Math.Clamp(leftKnee, -115, 115),
            Math.Clamp(rightKnee, -115, 115),
            Math.Clamp(leftAnkle, -45, 45),
            Math.Clamp(rightAnkle, -45, 45),
            Math.Clamp(leftEar, -24, 24),
            Math.Clamp(rightEar, -24, 24),
            Math.Clamp(_leftEarTip.Update(leftEarTipTarget, deltaSeconds, 7.6, 0.62), -30, 30),
            Math.Clamp(_rightEarTip.Update(rightEarTipTarget, deltaSeconds, 7.6, 0.62), -30, 30),
            Math.Clamp(tail, -28, 28),
            Math.Clamp(tailTip, -32, 32),
            Math.Clamp(_leftFringe.Update(leftFringeTarget, deltaSeconds, 6.4, 0.7), -16, 16),
            Math.Clamp(_rightFringe.Update(rightFringeTarget, deltaSeconds, 6.4, 0.7), -16, 16));
    }

    public void Reset()
    {
        _envelope = 0;
        _pulse = 0;
        _phase = 0;
        _blinkClock = 0;
        _previousBeatPulse = 0;
        _jumpElapsed = JumpDurationSeconds;
        _jumpCooldownRemaining = 0;
        _leftEarTip.Reset();
        _rightEarTip.Reset();
        _tail.Reset();
        _tailTip.Reset();
        _leftFringe.Reset();
        _rightFringe.Reset();
    }

    private static double SmoothStep(double value)
    {
        value = Math.Clamp(value, 0, 1);
        return value * value * (3 - (2 * value));
    }

    private static double PulseWindow(double progress, double start, double end)
    {
        if (progress < start || progress > end || end <= start)
        {
            return 0;
        }

        return Math.Sin(((progress - start) / (end - start)) * Math.PI);
    }

    private static double CalculateEyeScale(double clock)
    {
        const double blinkStart = 3.34;
        const double blinkDuration = 0.16;
        if (clock < blinkStart || clock > blinkStart + blinkDuration)
        {
            return 1;
        }

        var progress = (clock - blinkStart) / blinkDuration;
        return Math.Clamp(1 - (Math.Sin(progress * Math.PI) * 0.9), 0.1, 1);
    }

    private sealed class SecondaryMotionChannel
    {
        private double _position;
        private double _velocity;

        public double Update(double target, double deltaSeconds, double frequency, double dampingRatio)
        {
            var substepCount = Math.Max(1, (int)Math.Ceiling(deltaSeconds * 120));
            var substep = deltaSeconds / substepCount;
            var angularFrequency = Math.PI * 2 * frequency;
            for (var index = 0; index < substepCount; index++)
            {
                var acceleration = ((target - _position) * angularFrequency * angularFrequency)
                    - (2 * dampingRatio * angularFrequency * _velocity);
                _velocity += acceleration * substep;
                _position += _velocity * substep;
            }

            return _position;
        }

        public void Reset()
        {
            _position = 0;
            _velocity = 0;
        }
    }
}
