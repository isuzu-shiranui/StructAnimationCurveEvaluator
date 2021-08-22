using System;
using System.Runtime.InteropServices;
using Unity.Collections;
using UnityEngine;

[StructLayout(LayoutKind.Sequential)]
public struct AnimationCurveEvaluator : IDisposable
{
    private NativeArray<Keyframe> curve;

    public AnimationCurveEvaluator(UnityEngine.AnimationCurve animationCurve)
    {
        this.curve = new NativeArray<Keyframe>(animationCurve.keys, Allocator.Persistent);
    }

    // https://forum.unity.com/threads/need-way-to-evaluate-animationcurve-in-the-job.532149/
    public float Evaluate(float time)
    {
        float value = 0;

        for (var i = 0; i < this.curve.Length; i++)
        {
            var next = Mathf.Clamp(i + 1, 0, this.curve.Length - 1);
            var start = this.curve[i];
            var end = this.curve[next];


            var minCheck = time > start.time ? 1 : 0;
            var maxCheck = time <= end.time ? 1 : 0;
            var check = minCheck * maxCheck;

            value += CubicHermiteInterpolate(time, start, end) * check;
        }

        return this.SurroundValue(value, time);
    }

    private static float CubicHermiteInterpolate(float time, Keyframe leftKeyframe, Keyframe rightKeyframe)
    {
        var distanceTime = leftKeyframe.time - rightKeyframe.time;

        var m0 = leftKeyframe.outTangent * distanceTime;
        var m1 = rightKeyframe.inTangent * distanceTime;

        if (Mathf.Approximately(distanceTime, 0f)) return 0;

        var normalized = (time - leftKeyframe.time) / distanceTime;
        var t2 = normalized * normalized;
        var t3 = t2 * normalized;

        var a = 2 * t3 - 3 * t2 + 1;
        var b = t3 - 2 * t2 + normalized;
        var c = t3 - t2;
        var d = -2 * t3 + 3 * t2;

        return a * leftKeyframe.value + b * m0 + c * m1 + d * rightKeyframe.value;
    }

    private float SurroundValue(float value, float time)
    {
        var minTime = this.curve[0].time;
        var minValue = this.curve[0].value;
        var maxTime = this.curve[this.curve.Length - 1].time;
        var maxValue = this.curve[this.curve.Length - 1].value;
        return time <= minTime ? minValue : time > maxTime ? maxValue : value;
    }

    public void Dispose()
    {
        this.curve.Dispose();
    }
}
