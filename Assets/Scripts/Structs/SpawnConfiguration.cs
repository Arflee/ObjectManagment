using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct SpawnConfiguration
{
    public enum MovementDirection
    {
        Forward,
        Upward,
        Outward,
        Random
    }

    public ShapeFactory[] factories;
    public MovementDirection movementDirection;

    public FloatRange speed;
    public FloatRange angularSpeed;
    public FloatRange scale;

    public ColorRangeHSV color;

    public bool uniformColor;

    public MovementDirection oscillationDirection;
    public FloatRange oscillationAmplitude;
    public FloatRange oscillationFrequency;

    [System.Serializable]
    public struct SatelliteConfiguration
    {
        public IntRange amount;

        [FloatRangeSlider(0.1f, 1f)]
        public FloatRange relativeScale;

        public FloatRange orbitRadius;

        public FloatRange orbitFrequency;
    }

    public SatelliteConfiguration satellite;

    [System.Serializable]
    public struct LifecycleConfiguration
    {
        [FloatRangeSlider(0f, 2f)]
        public FloatRange growingDuration;

        [FloatRangeSlider(0f, 2f)]
        public FloatRange dyingDuration;

        public Vector2 RandomDurations
        {
            get
            {
                return new Vector2(
                    growingDuration.RandomValueInRange, dyingDuration.RandomValueInRange
                );
            }
        }
    }

    public LifecycleConfiguration lifecycle;
}
