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
            playersTurn = false;
            List<GameObject> enemies = new List<GameObject>(GameObject.FindGameObjectsWithTag("Enemy"));
            foreach (GameObject enemy in enemies) {
                enemyController enemyController = enemy.GetComponent<enemyController>();
                if (enemyController != null) {
                    enemyController.startTurn();
                } else {
                    Debug.LogWarning($"Enemy {enemy.name} does not have an EnemyController component.");
                }
            }
        } else {
            playersTurn = true;
            characterController player = GameObject.FindGameObjectWithTag("Player").transform.GetComponent<characterController>();
            remainingActions = maxActions;
            player.startTurn();
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
