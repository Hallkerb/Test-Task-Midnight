using UnityEngine;

public class SizeChecker : MonoBehaviour
{
    [ContextMenu("Print Size")]
    private void PrintSize()
    {
        var renderers = GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return;

        Bounds bounds = renderers[0].bounds;
        foreach (var r in renderers) bounds.Encapsulate(r.bounds);

        Debug.Log($"{name}: розмір = {bounds.size} (м)", this);
    }
}
