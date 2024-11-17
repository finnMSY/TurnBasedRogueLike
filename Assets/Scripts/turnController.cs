using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class turnController : MonoBehaviour
{
    public int maxActions = 3;
    public TextMeshProUGUI actionCounter;
    
    [HideInInspector]
    public int remainingActions; 

    // Start is called before the first frame update
    void Start()
    {
        remainingActions = maxActions;
        actionCounter.text = remainingActions.ToString();
    }

    // Update is called once per frame
    void Update()
    {
        if (actionCounter.text != remainingActions.ToString() && remainingActions >= 0) {
            actionCounter.text = remainingActions.ToString();
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
