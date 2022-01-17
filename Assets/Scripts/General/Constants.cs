using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Constants
{
    #region Gameobject Tags

    public const string TAG_CAR_INTERACT_COLLIDER = "CarInteractCollider";
    public const string TAG_WEAPON_INTERACT_COLLIDER = "WeaponInteractCollider";

    public const string TAG_GROUND_COLLIDER = "GroundCollider";
    public const string TAG_WATER_SURFACE_COLLIDER = "WaterSurfaceCollider";

    public const string TAG_PLAYER = "Player";
    public const string TAG_ENEMY = "Enemy";
    public const string TAG_ENEMY_LIMB = "EnemyLimb";
    public const string TAG_CAR = "Car";
    public const string TAG_OBSTACLE = "Obstacle";

    public const string ID_WEAPON_PISTOL_TACTICAL = "PistolTactical";
    
    #endregion

    #region Layer Names

    public const string LAYER_NAME_DEFAULT = "Default";
    public const string LAYER_NAME_PLAYER = "Player";
    public const string LAYER_NAME_GROUND = "Ground";
    public const string LAYER_NAME_UNWALKABLE = "Unwalkable";
    public const string LAYER_NAME_ENEMY = "Enemy";
    public const string LAYER_NAME_WATER = "Water";
    public const string LAYER_NAME_OBSTACLE = "Obstacle";

    #endregion
}
