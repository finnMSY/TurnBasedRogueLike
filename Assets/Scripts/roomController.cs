using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class Room
{
    public string Name { get; }
    public bool IsCurrent { get; set;}
    public bool IsActive { get; set; }
    public List<door> Doors { get; }
    public GameObject obj { get; }

    public Room(string name, bool isCurrent, bool isActive, GameObject _obj, List<door> doors)
    {
        Name = name;
        IsCurrent = isCurrent;
        Doors = doors;
        IsActive = isActive;
        obj = _obj;
    }

    public void OpenDoors()
    {
        IsActive = false;
        foreach (door d in Doors)
        {
            d.Open();
        }
    }
}

public class roomController : MonoBehaviour
{
    public GameObject doors;
    public GameObject cameraPoint;
    public Vector3Int startingTile;
    
    [HideInInspector]
    public gameController gameController;

    void Start()
    {
        name = gameObject.name;
        gameController = GameObject.FindWithTag("GameController").GetComponent<gameController>();
    }

    public Room createRoom(bool isCurrent, bool isActive, GameObject prefabOverride = null)
    {
        List<door> doorsList = new List<door>();
        foreach (Transform doorObj in doors.transform)
        {
            doorsList.Add(doorObj.GetComponent<door>());
        }
        GameObject roomObj = prefabOverride != null ? prefabOverride : gameObject;
        return new Room(gameObject.name, isCurrent, isActive, roomObj, doorsList);
    }

    public Room getCurrentRoom()
    {
        foreach (Room room in gameController.rooms)
        {
            if (room.IsCurrent) return room;
        }
        foreach (Room room in gameController.visitedRooms)
        {
            if (room.IsCurrent) return room;
        }
        return null;
    }
}