using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class enemyController : MonoBehaviour {
    public tilemapManager tileManager;
    public int numActions;
    public GameObject player;
    List<Tile> totalTiles = new List<Tile>();

    void Start() {
        totalTiles = tileManager.getTilesList();
        Vector3 playerPos = player.transform.position;
        findQuickestPath(tileManager.FindTile(new Vector3Int((int)playerPos.x, (int)playerPos.y, 0)));
    }

    void findQuickestPath(Tile dest) {
        Debug.Log(dest.position);
    }

    void Update() {
    
    }
}
