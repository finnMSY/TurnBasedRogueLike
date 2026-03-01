using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class gameController : MonoBehaviour
{
    public int maxActions;
    public TextMeshProUGUI actionCounter;
    
    [HideInInspector]
    public int remainingActions; 
    bool playersTurn = true;
    public GameObject[] roomObjects;
    public bool isTransitioning = false;
    private List<enemyController> enemies = new List<enemyController>();
    public List<Room> rooms = new List<Room>();
    private int currentEnemyIndex = 0;
    public roomController currentRoom;
    public GameObject startingRoom; 

    public List<Room> visitedRooms;

    void Awake()
    {
        remainingActions = maxActions;
        actionCounter.text = remainingActions.ToString();
        visitedRooms = new List<Room>();
        visitedRooms.Add(currentRoom.createRoom(true, true, startingRoom));

        foreach (GameObject roomObject in roomObjects)
        {
            Room room = roomObject.GetComponent<roomController>().createRoom(false, false, roomObject);
            rooms.Add(room);
        }
    }

    void Update()
    {
        if (actionCounter.text != remainingActions.ToString() && remainingActions >= 0) {
            actionCounter.text = remainingActions.ToString();
        }
    }

    void AddVisitedRoom(Room room)
    {
        visitedRooms.Add(room);
    }

    public Room PickRoom()
    {
        List<Room> newRoomList = new List<Room>();
        foreach (Room room in rooms)
        {
            if (!visitedRooms.Contains(room))
            {
                newRoomList.Add(room);
            }
        } 

        int randomIndex = Random.Range(0, newRoomList.Count - 1);
        Room randomRoom = newRoomList[randomIndex];
        AddVisitedRoom(randomRoom);
        return randomRoom;
    }

    private void EndOfRoom()
    {
        currentRoom.SetCleared();
        foreach (Room room in visitedRooms)
        {
            if (room.IsCurrent)
            {
                room.IsActive = false;
                room.OpenDoors();
            }
        }
    }

    public void OnRoomEnter(roomController room)
    {
        if (!room.roomIsActive)
        {
            room.enemies.SetActive(false);
        }
    }

    public void endOfTurn() {
        if (playersTurn) {
            playersTurn = false;
            
            enemies.Clear();
            GameObject[] enemyObjects = GameObject.FindGameObjectsWithTag("Enemy");
            foreach (GameObject enemy in enemyObjects) {
                enemyController ec = enemy.GetComponent<enemyController>();
                if (ec != null) {
                    enemies.Add(ec);
                }
            }
            
            currentEnemyIndex = 0;
            if (enemies.Count > 0) {
                enemies[0].startTurn();
            } else {
                EndOfRoom();
                startPlayerTurn();
            }
        } 
        else {
            currentEnemyIndex++;
            
            if (currentEnemyIndex < enemies.Count) {
                enemies[currentEnemyIndex].startTurn();
            } else {
                startPlayerTurn();
            }
        }
    }

    private void startPlayerTurn() {
        playersTurn = true;
        characterController player = GameObject.FindGameObjectWithTag("Player").GetComponent<characterController>();
        if (player != null) {
            remainingActions = maxActions;
            player.startTurn();
        } else {
            Debug.LogError("Player not found!");
        }
    }

    public bool canUseAction(int numActions) {
        return((remainingActions - numActions) >= 0);
    }

    public void useAction(int numActions) {
        remainingActions -= numActions;
    }

    public void giveAction(int numActions) {
        remainingActions += numActions;
    }
}