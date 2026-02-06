using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class gameController : MonoBehaviour
{
    public int maxActions = 3;
    public TextMeshProUGUI actionCounter;
    
    [HideInInspector]
    public int remainingActions; 
    bool playersTurn = true;
    public List<MoveSet> moveSets = new List<MoveSet>();
    
    private List<enemyController> enemies = new List<enemyController>();
    private int currentEnemyIndex = 0;

    void Start()
    {
        remainingActions = maxActions;
        actionCounter.text = remainingActions.ToString();
    }

    void Update()
    {
        if (actionCounter.text != remainingActions.ToString() && remainingActions >= 0) {
            actionCounter.text = remainingActions.ToString();
        }
    }

    public void endOfTurn() {
        if (playersTurn) {
            // Player's turn is over, start enemy turns
            playersTurn = false;
            
            // Find all enemies
            enemies.Clear();
            GameObject[] enemyObjects = GameObject.FindGameObjectsWithTag("Enemy");
            foreach (GameObject enemy in enemyObjects) {
                enemyController ec = enemy.GetComponent<enemyController>();
                if (ec != null) {
                    enemies.Add(ec);
                }
            }
            
            // Start first enemy's turn
            currentEnemyIndex = 0;
            if (enemies.Count > 0) {
                Debug.Log($"Starting turn for enemy {currentEnemyIndex + 1} of {enemies.Count}");
                enemies[0].startTurn();
            } else {
                // No enemies, go back to player
                Debug.Log("No enemies found, returning to player turn");
                startPlayerTurn();
            }
        } 
        else {
            // An enemy's turn just ended
            currentEnemyIndex++;
            
            if (currentEnemyIndex < enemies.Count) {
                // Start next enemy's turn
                Debug.Log($"Starting turn for enemy {currentEnemyIndex + 1} of {enemies.Count}");
                enemies[currentEnemyIndex].startTurn();
            } else {
                // All enemies done, back to player
                Debug.Log("All enemies finished, returning to player turn");
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