using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine;

public class Room
{
    public string Name { get; }
    public bool IsCurrent { get; set;}
    public bool IsActive { get; set; }
    public List<door> Doors { get; }

    public Room(string name, bool isCurrent, bool isActive, List<door> doors)
    {
        Name = name;
        IsCurrent = isCurrent;
        Doors = doors;
        IsActive = isActive;
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
    public cameraController camera;
    public GameObject cameraPoint;
    public Vector3Int startingTile;
    public gameController gameController;

    void Start()
    {
        name = gameObject.name;
    }

    void Update()
    {
       
    }

    public Room createRoom(bool isCurrent, bool isActive)
    {
        List<door> doorsList = new List<door>();
        foreach (Transform doorObj in doors.transform)
        {
            doorsList.Add(doorObj.GetComponent<door>());
        }
        Room room = new Room(gameObject.name, isCurrent, isActive, doorsList);
        return room;
    }
}