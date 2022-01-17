using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponInteractCollider : MonoBehaviour
{
    [SerializeField] private Weapon weapon = null;
    public Weapon Weapon => weapon;
}
