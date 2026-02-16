using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;


[System.Serializable]
public class MoveSet {
    public EnemyType enemyType;
    public List<Ability> moves = new List<Ability>();
}

public class Ability {
    public string name;
    public int damage;
    public TextAsset range;
    public GameObject animation;

    
}

public enum EnemyType { 
    Melee, 
    Ranged,
    Magic 
};