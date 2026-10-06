using UnityEngine;

/// <summary>
/// Contract for providing rectangular world space bounds for camera navigation.
/// </summary>
public interface IStoreBounds
{
    Vector2 MinBounds { get; }
    Vector2 MaxBounds { get; }
}
