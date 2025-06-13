using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class MapGenerator : MonoBehaviour
{
    [System.Serializable]
    public class PlacedRoom
    {
        public GameObject roomObject;
        public Vector2Int gridPosition;
        public List<Door> doors;
    }

    public GameObject startRoomPrefab;
    public List<GameObject> roomPrefabs;
    public int numberOfRooms = 10;
    public Vector2Int gridSize = new Vector2Int(10, 10);
    public float roomSpacing = 10f;

    private List<PlacedRoom> placedRooms = new List<PlacedRoom>();
    private HashSet<Vector2Int> occupiedPositions = new HashSet<Vector2Int>();

    void Start()
    {
        Generate();
    }

    public void Generate()
    {
        placedRooms.Clear();
        occupiedPositions.Clear();

        List<Door> openDoors = new List<Door>();

        Vector2Int startGridPos = Vector2Int.zero;
        GameObject startRoom = Instantiate(startRoomPrefab, GridToWorld(startGridPos), Quaternion.identity);
        List<Door> startDoors = ExtractDoors(startRoom);
        PlacedRoom startPlaced = new PlacedRoom
        {
            roomObject = startRoom,
            gridPosition = startGridPos,
            doors = startDoors
        };

        placedRooms.Add(startPlaced);
        occupiedPositions.Add(startGridPos);
        openDoors.AddRange(startDoors);

        bool success = GenerateWithBacktracking(openDoors, numberOfRooms - 1);
        Debug.Log("Generation result: " + (success ? "✔ Success" : "❌ Failed"));
    }

    private bool GenerateWithBacktracking(List<Door> openDoors, int roomsRemaining)
    {
        if (roomsRemaining <= 0)
            return true;

        for (int i = 0; i < openDoors.Count; i++)
        {
            Door openDoor = openDoors[i];
            PlacedRoom baseRoom = GetRoomByDoor(openDoor);
            if (baseRoom == null) continue;

            Vector2Int baseGridPos = baseRoom.gridPosition;
            Vector2Int offset = GetDirectionOffset(openDoor.GetDirection());
            Vector2Int newRoomPos = baseGridPos + offset;

            if (occupiedPositions.Contains(newRoomPos))
            {
                Debug.Log("✘ Placement failed — would overlap at " + newRoomPos);
                continue;
            }

            GameObject candidatePrefab = GetRandomRoomWithMatchingDoor(openDoor.GetMatchingDirection(), out Door matchedDoor);
            if (candidatePrefab == null)
            {
                Debug.Log("✘ Room skipped — no required door.");
                continue;
            }

            GameObject roomInstance = Instantiate(candidatePrefab, GridToWorld(newRoomPos), Quaternion.identity);
            List<Door> newDoors = ExtractDoors(roomInstance);

            PlacedRoom newPlacedRoom = new PlacedRoom
            {
                roomObject = roomInstance,
                gridPosition = newRoomPos,
                doors = newDoors
            };

            placedRooms.Add(newPlacedRoom);
            occupiedPositions.Add(newRoomPos);

            List<Door> newOpenDoors = newDoors.Where(d => !IsDoorMatching(d, openDoor)).ToList();
            List<Door> updatedOpenDoors = new List<Door>(openDoors);
            updatedOpenDoors.RemoveAt(i);
            updatedOpenDoors.AddRange(newOpenDoors);

            if (GenerateWithBacktracking(updatedOpenDoors, roomsRemaining - 1))
                return true;

            // Backtrack
            Destroy(roomInstance);
            placedRooms.Remove(newPlacedRoom);
            occupiedPositions.Remove(newRoomPos);
        }

        return false;
    }

    private Vector2Int GetDirectionOffset(Door.Direction dir)
    {
        switch (dir)
        {
            case Door.Direction.NORTH: return new Vector2Int(0, 1);
            case Door.Direction.SOUTH: return new Vector2Int(0, -1);
            case Door.Direction.EAST: return new Vector2Int(1, 0);
            case Door.Direction.WEST: return new Vector2Int(-1, 0);
        }
        return Vector2Int.zero;
    }

    private PlacedRoom GetRoomByDoor(Door door)
    {
        return placedRooms.FirstOrDefault(room => room.doors.Any(d => IsDoorMatching(d, door)));
    }

    private bool IsDoorMatching(Door a, Door b)
    {
        return a.GetGridCoordinates() == b.GetMatching().GetGridCoordinates()
            && a.GetDirection() == b.GetMatchingDirection();
    }

    private List<Door> ExtractDoors(GameObject roomInstance)
    {
        List<Door> result = new List<Door>();
        foreach (DoorMarker marker in roomInstance.GetComponentsInChildren<DoorMarker>())
        {
            Vector3 worldPos = marker.transform.position;
            Vector2Int gridPos = new Vector2Int(Mathf.RoundToInt(worldPos.x / roomSpacing), Mathf.RoundToInt(worldPos.z / roomSpacing));
            result.Add(new Door(gridPos, marker.direction));
        }
        return result;
    }

    private GameObject GetRandomRoomWithMatchingDoor(Door.Direction requiredDirection, out Door matchedDoor)
    {
        matchedDoor = null;
        Shuffle(roomPrefabs);

        foreach (GameObject prefab in roomPrefabs)
        {
            GameObject instance = Instantiate(prefab);
            List<Door> doors = ExtractDoors(instance);
            matchedDoor = doors.FirstOrDefault(d => d.GetDirection() == requiredDirection);
            DestroyImmediate(instance);

            if (matchedDoor != null)
                return prefab;
        }

        return null;
    }

    private Vector3 GridToWorld(Vector2Int gridPos)
    {
        return new Vector3(gridPos.x * roomSpacing, 0, gridPos.y * roomSpacing);
    }

    private void Shuffle<T>(List<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int randIndex = Random.Range(i, list.Count);
            T temp = list[i];
            list[i] = list[randIndex];
            list[randIndex] = temp;
        }
    }

    // Optional: draw placed rooms in scene view
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        foreach (var room in placedRooms)
        {
            Gizmos.DrawWireCube(GridToWorld(room.gridPosition), Vector3.one * roomSpacing * 0.9f);
        }
    }
}
