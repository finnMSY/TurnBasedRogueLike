using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class door : MonoBehaviour
{
    public Vector3Int currentTile;
    private tilemapManager tileManager;
    public GameObject destinationRoom;
    cameraController camera;
    GameObject room;
    void Start()
    {
        name = gameObject.name;
        tileManager = gameObject.transform.parent.gameObject.transform.parent.gameObject.GetComponent<tilemapManager>();
        tileManager.SwtichTileObstacleStatus(currentTile, true);
    
        room = gameObject.transform.parent.gameObject.transform.parent.gameObject;
        camera = room.GetComponent<roomController>().camera;
    }

    public void Open()
    {
        this.gameObject.GetComponent<SpriteRenderer>().enabled = false;
        tileManager.SwtichTileObstacleStatus(currentTile, false);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            Transition();
        }
    }

    void Transition()
    {
        destinationRoom.SetActive(true);
        Transform newPoint = destinationRoom.GetComponent<roomController>().cameraPoint.transform;  
        camera.StartMoveCamera(newPoint);

        if (camera.isMoving)
        {
            tilemapManager destTileManager = destinationRoom.GetComponent<tilemapManager>();
            destTileManager.InitialiseTiles();
            Vector3Int startingTile = destinationRoom.GetComponent<roomController>().startingTile;

            Debug.Log($"Tile count: {destTileManager.getTilesList().Count}, looking for: {startingTile}");

            Tile foundTile = destTileManager.FindTile(startingTile);

            if (foundTile == null)
            {
                Debug.LogError($"Could not find spawn tile {startingTile} in {destinationRoom.name}.");
                return;
            }

            // Use FindObjectOfType instead of going through old room's reference
            characterController player = FindObjectOfType<characterController>();
            player.tilemapManager = destTileManager;
            player.currentTile = foundTile;

            Debug.Log($"Player tile set to: {player.currentTile.position}, manager: {player.tilemapManager.name}, count: {player.tilemapManager.getTilesList().Count}");

            room.GetComponent<roomController>().gameController.currentRoom = destinationRoom.GetComponent<roomController>();
            room.SetActive(false);
        }
    }
}