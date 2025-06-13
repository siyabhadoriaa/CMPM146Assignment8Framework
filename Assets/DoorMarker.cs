using UnityEngine;

public class DoorMarker : MonoBehaviour
{
    public Door.Direction direction;

    public Door GetDoor(Vector2Int roomPosition)
    {
        Vector2Int worldPos = new Vector2Int(
            Mathf.RoundToInt(transform.position.x),
            Mathf.RoundToInt(transform.position.z) // ✅ Use z not y
        );
        return new Door(worldPos + roomPosition, direction);
    }
}
