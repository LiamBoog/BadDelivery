using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static HelperFunctions;
using Random = UnityEngine.Random;

public class Gun : MonoBehaviour
{
    public enum AmmoType
    {
        Null,
        Pistol,
        AssaultRifle,
        Shotgun,
        SniperRifle
    }

    #region Properties

    #endregion

    #region Inspector Controlled Variables
    
    [SerializeField] private bool inPlayerControl = false;
    
    [SerializeField] private GameObject bulletShell = null;
    [SerializeField] private Vector3 bulletShellSpawnLocation = Vector3.zero;
    [SerializeField] private float bulletShellEjectionDelay = 0f;

    [SerializeField] private AudioSource audioSourceShoot = null;
    [SerializeField] private AudioSource audioSourceReload = null;

    [SerializeField] private AmmoType ammoType = AmmoType.Null;
    [SerializeField] private uint damagePerBullet = 3;
    [SerializeField] private bool automaticFire = false;
    [SerializeField] private uint magazineRoundLimit = 0;
    [SerializeField] private uint roundsLoaded = 1;
    [SerializeField] private float shotCooldownSeconds = 0f;

    [SerializeField] private float recoilSpeed = 0f;
    [SerializeField] private float recoilStrength = 0f;

    [SerializeField] private AudioClip outOfAmmoSound = null;
    [SerializeField] private List<AudioClip> shootSounds = null;
    [SerializeField] private List<AudioClip> reloadSounds = null;

    [SerializeField] private Animator animator = null;

    #endregion

    #region Private Members

    private const string ANIMATION_STATE_SHOOT = "Shoot";
    private const string ANIMATION_STATE_RELOAD = "Reload";
    private const string ANIMATION_STATE_END_RELOAD = "EndReload";

    private delegate void EventHandler();
    private event EventHandler reloadUpdateVars = null;

    private bool triggerWasReset = false;
    private bool shotOnCooldown = false;
    private bool reloading = false;

    #endregion

    #region Public Methods

    /// <summary>
    /// If can shoot the gun, shoot the gun
    /// </summary>
    public void Shoot()
    {
        if (ammoType == AmmoType.Shotgun && roundsLoaded > 0 && !shotOnCooldown) //Shotgun can shoot while reloading
        {
            reloading = false;
            reloadUpdateVars = null;
        }
        else if (shotOnCooldown || reloading || (!automaticFire && !triggerWasReset))
        {
            return;
        }

        shotOnCooldown = true;

        if (roundsLoaded == 0) //Out of ammo sound and leave
        {
            audioSourceShoot.clip = outOfAmmoSound;
            audioSourceShoot.Play();
        }
        else
        {
            audioSourceShoot.clip = shootSounds[Random.Range(0, shootSounds.Count)];
            audioSourceShoot.Play();
            animator.Play(ANIMATION_STATE_SHOOT);
            StartCoroutine(EjectShellCoroutine());

            --roundsLoaded;
            UIManager.Instance.UpdateWeaponUI(roundsLoaded, magazineRoundLimit, PlayerManager.Instance.Player.ActiveWeapon);
            triggerWasReset = false;

            if (inPlayerControl)
            {
                //Raycast parameters
                Vector3 crosshairPos = UIManager.Instance.GetPlayerCrosshairPosition();
                Ray shotRay = CameraManager.Instance.ActiveCamera.ScreenPointToRay(crosshairPos);

                //Hitmarker parameters
                Vector3 hitLocation;
                List<Vector3> hits = new List<Vector3>();

                switch (ammoType)
                {
                    case AmmoType.Pistol:
                        hitLocation = BulletRayCast(shotRay, ~LayerMask.GetMask(Constants.LAYER_NAME_PLAYER));
                        hits.Add(hitLocation);
                        break;

                    case AmmoType.Shotgun:
                        for (int i = 0; i < 10; i++)
                        {
                            //Add random rotation to each shotgun pellet
                            Quaternion rotation = Quaternion.LookRotation(shotRay.direction, Vector3.up);
                            rotation.eulerAngles = new Vector3(Random.Range(-2.5f, 2.5f), Random.Range(-2.5f, 2.5f), Random.Range(-2.5f, 2.5f));
                            Vector3 newRayDirection = rotation * shotRay.direction;

                            hitLocation = BulletRayCast(new Ray(shotRay.origin, newRayDirection), ~LayerMask.GetMask(Constants.LAYER_NAME_PLAYER));
                            hits.Add(hitLocation);
                        }
                        break;
                }
                
                //Display hitmarker
                UIManager.Instance.DisplayHitmarker(hits);

                //TODO - MAKE THIS GOOD LOL
                //StartCoroutine(RecoilCoroutine());
            }
        }

        StartCoroutine(ShotCooldownCoroutine());
    }

    /// <summary>
    /// Called to reset the trigger for semi auto guns
    /// </summary>
    public void ResetTrigger()
    {
        if (shotOnCooldown || reloading)
        {
            return;
        }

        triggerWasReset = true;
    }

    /// <summary>
    /// If can reload the gun, start the reload coroutine
    /// </summary>
    public void Reload()
    {
        if (roundsLoaded == magazineRoundLimit || reloading || shotOnCooldown ||
            (inPlayerControl && PlayerManager.Instance.Player.PeekAmmo(ammoType) == 0))
        {
            return;
        }

        reloading = true;
        StartCoroutine(ReloadAnimationCoroutine());
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Coroutine which will wait for shot cooldown timer before enabling shooting again
    /// </summary>
    /// <returns></returns>
    private IEnumerator ShotCooldownCoroutine()
    {
        yield return new WaitForSeconds(shotCooldownSeconds);
        shotOnCooldown = false;
        if (roundsLoaded == 0) //Auto reload after emptying a clip
        {
            Reload();
        }
    }

    /// <summary>
    /// Eject a shell from the gun after shooting it
    /// </summary>
    private IEnumerator EjectShellCoroutine()
    {
        if (bulletShell == null)
        {
            yield break;
        }

        yield return new WaitForSeconds(bulletShellEjectionDelay);

        GameObject shellInstance = Instantiate(bulletShell, transform);
        shellInstance.transform.localPosition = bulletShellSpawnLocation;
    }

    /// <summary>
    /// Casts a ray against colliders for a fired bullet and returns location of successful hit in World Space (Vector3.zero if unsuccessful)
    /// </summary>
    /// <param name="shotRay">The ray to be cast</param>
    /// <param name="layerMask">A LayerMask to select which layers should not be cast against</param>
    private Vector3 BulletRayCast(Ray shotRay, LayerMask layerMask)
    {

        if (Physics.Raycast(shotRay, out RaycastHit hit, Mathf.Infinity, layerMask))
        {
            switch (hit.transform.tag)
            {
                case Constants.TAG_ENEMY_LIMB:
                    GameObject enemyObject = FindParentWithTag(hit.transform.gameObject, Constants.TAG_ENEMY);
                    enemyObject.GetComponent<Enemy>().TakeDamage(damagePerBullet);
                    Debug.DrawLine(shotRay.origin, hit.point, Color.red, 2f);//For visualizing rays
                    return hit.point;
            }
        }

        return Vector3.zero;
    }

    /// <summary>
    /// Reload animation coroutine
    /// </summary>
    /// <returns></returns>
    private IEnumerator ReloadAnimationCoroutine()
    {
        //Play animation
        animator.Play(ANIMATION_STATE_RELOAD);

        while (reloading && roundsLoaded < magazineRoundLimit) //For shotgun; if not fully loaded, play animation again
        {
            //Play sounds
            foreach (AudioClip audioClip in reloadSounds)
            {
                audioSourceReload.clip = audioClip;
                audioSourceReload.Play();

                while (audioSourceReload.isPlaying)
                {
                    yield return null;
                }
            }

            //Queue this up so Update() can pick it up
            reloadUpdateVars += ReloadUpdateVars;

            //Wait for reloadUpdateVars to be invoked in Update()
            while (reloadUpdateVars != null)
            {
                yield return null;
            }
        }
        if (ammoType == AmmoType.Shotgun)
        {
            animator.Play(ANIMATION_STATE_END_RELOAD);
        }
    }

    /// <summary>
    /// Called after reload animation coroutine
    /// </summary>
    private void ReloadUpdateVars()
    {
        //Get ammo from reserves
        uint reloadAmount = 0;
        if (inPlayerControl)
        {
            Player player = PlayerManager.Instance.Player;
            reloadAmount = player.RequestAmmo(ammoType, ammoType == AmmoType.Shotgun ? 1 : magazineRoundLimit - roundsLoaded);
        }
        else
        {
            reloadAmount = ammoType == AmmoType.Shotgun ? 1 : magazineRoundLimit;
        }

        roundsLoaded += reloading ? reloadAmount : 0;
        UIManager.Instance.UpdateWeaponUI(roundsLoaded, magazineRoundLimit, PlayerManager.Instance.Player.ActiveWeapon);
        if (!inPlayerControl || roundsLoaded == magazineRoundLimit || PlayerManager.Instance.Player.PeekAmmo(ammoType) == 0)
        {
            reloading = false;
        }
    }

    /// <summary>
    /// When called gives a kick to the camera
    /// </summary>
    /// <returns></returns>
    private IEnumerator RecoilCoroutine()
    {
        Camera activeCamera = CameraManager.Instance.ActiveCamera;
        float xRotationStart = activeCamera.transform.localRotation.eulerAngles.x;
        float xRotationTarget = xRotationStart - recoilStrength;

        //Bring camera up
        while (activeCamera.transform.localRotation.eulerAngles.x > xRotationTarget)
        {
            float xRotationStep = 20f * Time.deltaTime;
            activeCamera.transform.Rotate(new Vector3(-xRotationStep, 0f, 0f));
            yield return null;
        }

        //Bring camera back down
        while (activeCamera.transform.localRotation.eulerAngles.x < xRotationStart)
        {
            float xRotationStep = 20f * Time.deltaTime;
            activeCamera.transform.Rotate(new Vector3(xRotationStep, 0f, 0f));
            yield return null;
        }
    }

    #endregion

    #region Unity Methods

    public void OnDisable()
    {
        reloading = false;
    }

    public void OnEnable()
    {
        animator.Rebind();

        if (roundsLoaded == 0)
        {
            Reload();
        }

        if (shotOnCooldown)
        {
            StartCoroutine(ShotCooldownCoroutine());
        }
        
        UIManager.Instance.UpdateWeaponUI(roundsLoaded, magazineRoundLimit, PlayerManager.Instance.Player.ActiveWeapon);
    }

    public void Update()
    {
        if (reloadUpdateVars != null)
        {
            reloadUpdateVars.Invoke();
            reloadUpdateVars = null;
        }
    }

    #endregion
}
