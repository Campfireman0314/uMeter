export interface Point {
  x: number;
  y: number;
}

export interface SpectrumBar {
  x: number;
  width: number;
  value: number;
}

export interface StereoPoint extends Point {
  intensity: number;
}

export interface CollisionBandGeometry {
  startRatio: number;
  endRatio: number;
  intensity: number;
}

export interface LoudnessClockPoint {
  frame: number;
  loudness: number;
  phase: number;
}

export function createWaveformPath(samples: readonly number[]): Point[] {
  if (samples.length === 0) {
    return [];
  }

  const denominator = Math.max(1, samples.length - 1);
  return samples.map((sample, index) => ({
    x: index / denominator,
    y: (1 - clamp(sample, -1, 1)) / 2,
  }));
}

export function createSpectrumBars(
  buckets: readonly number[],
  floorDb: number,
  ceilingDb: number,
): SpectrumBar[] {
  if (buckets.length === 0) {
    return [];
  }

  const width = 1 / buckets.length;
  const floorLevel = dbToDisplayLevel(Math.min(floorDb, ceilingDb));
  const ceilingLevel = dbToDisplayLevel(Math.max(floorDb, ceilingDb));
  const span = Math.max(0.000001, ceilingLevel - floorLevel);
  return buckets.map((bucket, index) => ({
    x: index * width,
    width,
    value: clamp((clamp(bucket, 0, 1) - floorLevel) / span, 0, 1),
  }));
}

export function createStereoPoints(
  points: readonly { angle: number; radius: number }[],
): StereoPoint[] {
  return points.map((point) => {
    const radius = clamp(point.radius, 0, 1);
    return {
      x: clamp(0.5 + Math.cos(point.angle) * radius * 0.5, 0, 1),
      y: clamp(0.5 - Math.sin(point.angle) * radius * 0.5, 0, 1),
      intensity: radius,
    };
  });
}

export function createCollisionBands(
  bands: readonly { startRatio: number; endRatio: number; intensity: number }[],
): CollisionBandGeometry[] {
  return bands
    .map((band) => ({
      startRatio: clamp(Math.min(band.startRatio, band.endRatio), 0, 1),
      endRatio: clamp(Math.max(band.startRatio, band.endRatio), 0, 1),
      intensity: clamp(band.intensity, 0, 1),
    }))
    .filter((band) => band.endRatio > band.startRatio)
    .sort((left, right) => left.startRatio - right.startRatio);
}

export function createLoudnessClockHistory(cycleSeconds: number) {
  let sourceKey: string | null = null;
  let points: LoudnessClockPoint[] = [];
  let originFrame = 0;
  const safeCycleSeconds = Math.max(1, cycleSeconds);

  return {
    add(nextSourceKey: string, frame: number, loudness: number): void {
      if (sourceKey !== nextSourceKey) {
        sourceKey = nextSourceKey;
        points = [];
        originFrame = frame;
      }

      const offsetFrame = frame - originFrame;
      points = [
        ...points.filter((point) => frame - point.frame < safeCycleSeconds),
        {
          frame,
          loudness,
          phase: ((offsetFrame % safeCycleSeconds) + safeCycleSeconds) % safeCycleSeconds / safeCycleSeconds,
        },
      ];
    },
    pointsFor(nextSourceKey: string): LoudnessClockPoint[] {
      return sourceKey === nextSourceKey ? [...points] : [];
    },
  };
}

function dbToDisplayLevel(decibels: number): number {
  return clamp((decibels + 200) / 200, 0, 1);
}

function clamp(value: number, minimum: number, maximum: number): number {
  if (!Number.isFinite(value)) {
    return minimum;
  }

  return Math.min(maximum, Math.max(minimum, value));
}
