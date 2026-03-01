using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public enum Direction
{
    North,
    West,
    East,
    South
}

class DoorTransition
{
    public List<Room> Rooms { get; }

    public bool IsOpen { get; set; }

    public DoorTransition(Room currentRoom, Room destinationRoom, bool isOpen)
    {
        Rooms = new List<Room>();
        Rooms.Add(currentRoom);
        Rooms.Add(destinationRoom);
        IsOpen = isOpen;
    }
}

public class door : MonoBehaviour
{
    public Vector3Int currentTile;
    tilemapManager tileManager;
    GameObject room;

    cameraController activeCamera; 
    GameObject instantiatedRoom;

    door destinationDoor;

    public Direction direction;
    Room destinationRoom;
    Room currentRoom;
    DoorTransition doorTransition;
    bool isTransitioning = false;

    [HideInInspector]
    public gameController gameController;
    bool transitioning = false;

    void Start()
    {
        tileManager = GetTileManager();
        room = gameObject.transform.parent.gameObject.transform.parent.gameObject;

        if (doorTransition != null && doorTransition.IsOpen) {
            tileManager.SwitchTileObstacleStatus(currentTile, false);
            gameObject.GetComponent<SpriteRenderer>().enabled = false;
            room.GetComponent<roomController>().roomIsActive = false;
        }
        else
        {
            room.GetComponent<roomController>().roomIsActive = true;
            tileManager.SwitchTileObstacleStatus(currentTile, true);
        }
        gameController = room.GetComponent<roomController>().gameController;
    }

    Room GetCurrentRoom()
    {
        return room.GetComponent<roomController>().getCurrentRoom();
    }

    tilemapManager GetTileManager()
    {
        if (tileManager == null)
            tileManager = gameObject.transform.parent.gameObject.transform.parent.gameObject.GetComponent<tilemapManager>();
        return tileManager;
    }

    public void Open()
    {
        SpriteRenderer sr = this.gameObject.GetComponent<SpriteRenderer>();
        if (sr != null) sr.enabled = false;

        tilemapManager tm = GetTileManager();
        if (tm != null)
            tm.SwitchTileObstacleStatus(currentTile, false);
        else
            Debug.LogWarning($"tileManager not found on door {gameObject.name}, skipping tile switch.");
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && !gameController.isTransitioning)
        {
            StartTransition();
        }
    }

    Vector3 GetRoomPos(Direction dir)
    {
        if (dir == Direction.North)
        {
            return new Vector3(room.transform.position.x, room.transform.position.y + 18, room.transform.position.z);
        }
        else if (dir == Direction.West)
        {
            return new Vector3(room.transform.position.x - 20, room.transform.position.y, room.transform.position.z);
        }
        else if (dir == Direction.East)
        {
            return new Vector3(room.transform.position.x + 20, room.transform.position.y + 20, room.transform.position.z);
        }
        else
        {
            return new Vector3(room.transform.position.x, room.transform.position.y - 18, room.transform.position.z);
        }
    }

    Direction GetOppositeDirection(Direction dir)
    {
        return dir switch
        {
            Direction.North => Direction.South,
            Direction.South => Direction.North,
            Direction.East  => Direction.West,
            Direction.West  => Direction.East,
            _               => dir
        };
    }

    void StartTransition()
    {
        gameController.actionCounter.enabled = false;
        gameController.remainingActions = gameController.maxActions + 1;
        gameController.isTransitioning = true;
        activeCamera = FindObjectOfType<cameraController>();

        if (doorTransition == null)
        {
            destinationRoom = gameController.PickRoom();
            doorTransition = new DoorTransition(GetCurrentRoom(), destinationRoom, true);
        }
        else
        {
            foreach (Room r in doorTransition.Rooms)
            {
                if (r.Name != GetCurrentRoom().Name)
                {
                    destinationRoom = r;
                }
            }
        }

        Vector3 roomPosition = GetRoomPos(direction);
        GameObject newRoom = Instantiate(destinationRoom.obj, roomPosition, Quaternion.identity);
        instantiatedRoom = newRoom;
        newRoom.transform.parent = GameObject.Find("Rooms").transform;

        // Set roomIsActive BEFORE Start() fires so enemies don't respawn in cleared rooms
        roomController newRoomController = newRoom.GetComponent<roomController>();
        newRoomController.roomIsActive = destinationRoom.IsActive;
        gameController.currentRoom = newRoomController;

        tileManager.SwitchTileObstacleStatus(currentTile, false);

        Direction oppDirection = GetOppositeDirection(direction);
        door[] newRoomDoors = newRoom.GetComponentsInChildren<door>();
        foreach (door d in newRoomDoors)
        {
            if (d.direction == oppDirection)
            {
                d.doorTransition = doorTransition;
                destinationDoor = d;
            }
        }

        newRoom.SetActive(true);
        Transform newPoint = newRoom.GetComponent<roomController>().cameraPoint.transform;
        activeCamera.StartMoveCamera(newPoint);

        MovePlayer();

        transitioning = true;
    }

    void MovePlayer()
    {
        tilemapManager destinationTileManager = instantiatedRoom.GetComponent<tilemapManager>();
        destinationTileManager.InitialiseTiles();

        Tile startingTile = destinationTileManager.FindTile(destinationDoor.currentTile);
        if (startingTile == null)
        {
            Debug.LogError($"Could not find spawn tile in {destinationRoom.Name}.");
            return;
        }

        characterController player = FindObjectOfType<characterController>();
        player.tilemapManager = destinationTileManager;
        player.currentTile = startingTile;
        player.Move(GetDirectionVector());

        player.ResetMovementHistory();
    }

    Vector3 GetDirectionVector()
    {
        if (direction == Direction.North)
        {
            return new Vector3(0, 1.5f, 0);
        }
        else if (direction == Direction.West)
        {
            return new Vector3(-1.5f, 0, 0);
        }
        else if (direction == Direction.East)
        {
            return new Vector3(1.5f, 0, 0);
        }
        else
        {
            return new Vector3(0, -1.5f, 0);
        }
    }

    void Transition()
    {
        // Handle room cleanup once camera has arrived
        Room oldRoom = GetCurrentRoom();
        if (oldRoom != null) oldRoom.IsCurrent = false;
        destinationRoom.IsCurrent = true;

        gameController.currentRoom = instantiatedRoom.GetComponent<roomController>();
        room.SetActive(false);
        Destroy(room);
    }

    void Update()
    {
        if (transitioning && activeCamera != null && !activeCamera.isMoving)
        {
            Transition();
            gameController.isTransitioning = false;
            transitioning = false;
            gameController.actionCounter.enabled = true;
        }
    }
}