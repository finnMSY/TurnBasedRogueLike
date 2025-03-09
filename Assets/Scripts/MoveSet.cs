using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class MoveSet {
    public EnemyType enemyType;
    public List<Ability> moves = new List<Ability>();
}

[System.Serializable]
public class Ability {
    public string name;
    public int damage;
}

public enum EnemyType { 
    Melee, 
    Ranged,
    Magic 
};