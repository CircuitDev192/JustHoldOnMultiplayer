﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponContext : Context<WeaponContext>
{
    #region Fields

    #region Weapon States

    public WeaponIdleState idleState = new WeaponIdleState();
    public WeaponFireState fireState = new WeaponFireState();
    public WeaponReloadState reloadState = new WeaponReloadState();
    public WeaponSwapState swapState = new WeaponSwapState();

    #endregion

    public Animator playerAnimator;
    public Transform weaponRoot;
    public WeaponBase[] weaponPrefabs;
    public WeaponBase[] consumablePrefabs;
    public GameObject suppressorPrefab;
    public string equippedConsumable;
    public List<WeaponBase> weapons;
    public List<WeaponBase> consumables;
    public List<GameObject> suppressors;
    public WeaponBase currentWeapon;
    public int currentWeaponIndex;
    public int currentConsumableIndex;
    public bool flashlightOn = false;
    public float flashlightDrainRate = 1f;
    public float currentFlashlightBattery = 100f;
    public float flashlightRechargeRate = 10f;
    public bool flashlightDead = false;
    public bool consumableEquipped = false;
    private bool isInPickupRange = false;

    public float weaponSwapTime;

    public float currentScrollDelta;

    public Camera mainCamera;

    public GameState gameState;

    #endregion

    public override void InitializeContext()
    {
        mainCamera = Camera.main;

        foreach (WeaponBase weapon in weaponPrefabs)
        {
            if (weapon.name == "AK-47" || weapon.name == "Deagle" || weapon.name == "Katana")
            {
                WeaponBase weap = Instantiate(weapon, weaponRoot);
                weap.enabled = false;
                weapons.Add(weap);
            }
        }

        foreach (WeaponBase consumable in consumablePrefabs)
        {
            WeaponBase consume = Instantiate(consumable, weaponRoot);
            consume.enabled = false;
            consumables.Add(consume);
            int totalAmmo = PlayerManager.instance.GetTotalAmmoOfType(consume.ammoType);
            EventManager.TriggerUpdateItemCountUI(consume.name, consume.roundsInCurrentMag + totalAmmo);
        }

        currentWeaponIndex = 0;
        currentWeapon = weapons[currentWeaponIndex];
        currentWeapon.enabled = true;

        currentConsumableIndex = 0;

        EventManager.GameStateChanged += GameStateChanged;
        EventManager.PlayerCollidedWithPickup += PlayerCollidedWithPickup;
        EventManager.PlayerCollidedWithAmmo += PlayerCollidedWithAmmo;
        EventManager.PlayerChangedConsumable += PlayerChangedConsumable;
        EventManager.PlayerPickedUpSuppressor += PlayerPickedUpSuppressor;
        EventManager.SuppressorBroken += SuppressorBroken;

        currentState = swapState;
        currentState.EnterState(this);
    }

    private void SuppressorBroken()
    {
        foreach (GameObject suppressor in suppressors)
        {
            if (suppressor.GetComponent<Suppressor>().GetDurability() <= 0f)
            {
                Destroy(suppressors[suppressors.IndexOf(suppressor)]);
                suppressors.Remove(suppressor);
                Destroy(currentWeapon.equippedSuppressor);
                currentWeapon.suppressorRenderer.enabled = false;
                break;
            }
        }
    }

    private void PlayerPickedUpSuppressor()
    {
        if (suppressors.Count == 2)
        {
            float lowestDur = 100f;
            float currentDur;
            int indexOfLowestDur = 0;
            foreach (GameObject suppress in suppressors)
            {
                currentDur = suppress.gameObject.GetComponent<Suppressor>().GetDurability();
                if (currentDur <= lowestDur)
                {
                    lowestDur = currentDur;
                    indexOfLowestDur = suppressors.IndexOf(suppress);
                }
            }
            Destroy(suppressors[indexOfLowestDur]);
            EventManager.TriggerSuppressorBroken();
            suppressors.Remove(suppressors[indexOfLowestDur]);
        }
        GameObject suppressor = Instantiate(suppressorPrefab, this.gameObject.transform);
        suppressors.Add(suppressor);
    }

    private void PlayerCollidedWithAmmo(PlayerManager.AmmoType ammoType, int addedAmmo)
    {
        isInPickupRange = true;
        EventManager.PlayerLeftPickup += PlayerLeftPickup;
        StartCoroutine(WaitForPlayerToPickupAmmo(ammoType, addedAmmo));
    }

    private void PlayerChangedConsumable(string consumableName)
    {
        equippedConsumable = consumableName;
        foreach(WeaponBase consumable in consumables)
        {
            if (consumable.name == consumableName)
            {
                currentConsumableIndex = consumables.IndexOf(consumable);
            }
        }
        if (consumableEquipped)
        {
            currentState.ExitState(this);
            currentState = swapState;
            currentState.EnterState(this);
        }
    }

    private void PlayerCollidedWithPickup(string weaponName)
    {
        isInPickupRange = true;
        EventManager.PlayerLeftPickup += PlayerLeftPickup;
        StartCoroutine(WaitForPlayerToPickupWeapon(weaponName));
    }

    private void PlayerLeftPickup()
    {
        isInPickupRange = false;
        EventManager.PlayerLeftPickup -= PlayerLeftPickup;
    }

    private IEnumerator WaitForPlayerToPickupAmmo(PlayerManager.AmmoType ammoType, int addedAmmo)
    {
        while (!Input.GetKeyDown(KeyCode.E) && isInPickupRange)
        {
            yield return null;
        }
        if (isInPickupRange)
        {
            if (PlayerManager.instance.GetTotalAmmoOfType(ammoType) != PlayerManager.instance.GetMaxAmmoOfType(ammoType))
            {
                EventManager.TriggerPlayerPickedUpAmmo(ammoType, addedAmmo);
            }
        }
    }



    private IEnumerator WaitForPlayerToPickupWeapon(string weaponName)
    {
        while (!Input.GetKeyDown(KeyCode.E) && isInPickupRange)
        {
            yield return null;
        }
        if (isInPickupRange)
        { 
            foreach (WeaponBase weapon in weaponPrefabs)
            {
                if (weapon.name == weaponName)
                {
                    EventManager.TriggerPlayerPickedUpWeapon(weapons[weapon.weaponTypeInt].name);
                    int ammoInPreviousMag = weapons[weapon.weaponTypeInt].roundsInCurrentMag;
                    EventManager.TriggerTotalAmmoChangedSwap(PlayerManager.instance.GetTotalAmmoOfType(weapons[weapon.weaponTypeInt].ammoType) + ammoInPreviousMag, weapons[weapon.weaponTypeInt].ammoType);
                    Destroy(weapons[weapon.weaponTypeInt].gameObject);
                    WeaponBase weap = Instantiate(weapon, weaponRoot);
                    weapons[weapon.weaponTypeInt] = weap;
                    weapons[weapon.weaponTypeInt].roundsInCurrentMag = 0;

                    if (currentWeaponIndex != weapon.weaponTypeInt)
                    {
                        weap.enabled = false;
                    }
                    else
                    {
                        currentState.ExitState(this);
                        currentState = swapState;
                        currentState.EnterState(this);
                    }
                    break;
                }
            }
        }
    }

    private void GameStateChanged(GameState gameState)
    {
        this.gameState = gameState;
    }

    private void OnDestroy()
    {
        EventManager.GameStateChanged -= GameStateChanged;
        EventManager.PlayerCollidedWithPickup -= PlayerCollidedWithPickup;
        EventManager.PlayerCollidedWithAmmo -= PlayerCollidedWithAmmo;
        EventManager.PlayerChangedConsumable -= PlayerChangedConsumable;
        EventManager.PlayerPickedUpSuppressor -= PlayerPickedUpSuppressor;
        EventManager.SuppressorBroken -= SuppressorBroken;
        EventManager.PlayerLeftPickup -= PlayerLeftPickup;
    }
}
