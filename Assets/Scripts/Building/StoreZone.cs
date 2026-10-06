using UnityEngine;

/// <summary>
/// Represents a building or zone expansion area with rectangular boundaries.
/// </summary>
public class StoreZone : MonoBehaviour, IStoreBounds
    {
        [Header("Zone Boundaries (World Relative Position)")]
        [SerializeField] private Vector2 _minBounds = new Vector2(-10f, -10f);
        [SerializeField] private Vector2 _maxBounds = new Vector2(10f, 10f);

        public Vector2 MinBounds => (Vector2)transform.position + _minBounds;
        public Vector2 MaxBounds => (Vector2)transform.position + _maxBounds;

        /// <summary>
        /// Checks if a 2D world position (X, Z) is inside this zone's boundaries.
        /// </summary>
        public bool Contains(Vector3 position)
        {
            Vector2 min = MinBounds;
            Vector2 max = MaxBounds;

            return position.x >= min.x && position.x <= max.x &&
                   position.z >= min.y && position.z <= max.y;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Vector3 center = new Vector3((MinBounds.x + MaxBounds.x) * 0.5f, transform.position.y, (MinBounds.y + MaxBounds.y) * 0.5f);
            Vector3 size = new Vector3(MaxBounds.x - MinBounds.x, 1f, MaxBounds.y - MinBounds.y);
            Gizmos.DrawWireCube(center, size);
        }
    }
