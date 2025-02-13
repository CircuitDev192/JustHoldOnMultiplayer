﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIPlayerInfoController : MonoBehaviour
{
    // Health Information
    [SerializeField] private Image healthBar;

    // Weapon Information
    [SerializeField] private Text weaponName;
    [SerializeField] private Text roundsInMag;
    [SerializeField] private Text totalAmmo;

    // Attachment Information
    [SerializeField] private Image flashlightPowerBar;
    [SerializeField] private Image suppressorDurabilityBar;

    // Start is called before the first frame update
    void Start()
    {
        EventManager.PlayerHealthChanged += PlayerHealthChanged;

        EventManager.WeaponChanged += WeaponNameChanged;
        EventManager.AmmoCountChanged += RoundsInMagChanged;
        EventManager.TotalAmmoChanged += TotalAmmoChanged;

        EventManager.FlashLightPowerChanged += FlashLightPowerChanged;
        EventManager.SuppressorDurabilityChanged += SuppressorDurabilityChanged;

        EventManager.PlayerEnteredMissionVehicle += PlayerEnteredMissionVehicle;
    }

    private void PlayerEnteredMissionVehicle()
    {
        this.gameObject.SetActive(false);
    }

    private void PlayerHealthChanged(float health)
    {
        healthBar.transform.localScale = new Vector3(health / 100f, healthBar.transform.localScale.y, healthBar.transform.localScale.z);
    }

    private void OnDestroy()
    {
        EventManager.PlayerHealthChanged -= PlayerHealthChanged;

        EventManager.WeaponChanged -= WeaponNameChanged;
        EventManager.AmmoCountChanged -= RoundsInMagChanged;
        EventManager.TotalAmmoChanged -= TotalAmmoChanged;

        EventManager.FlashLightPowerChanged -= FlashLightPowerChanged;
        EventManager.SuppressorDurabilityChanged -= SuppressorDurabilityChanged;

        EventManager.PlayerEnteredMissionVehicle -= PlayerEnteredMissionVehicle;
    }

    #region Weapon Information Events

    private void WeaponNameChanged(string weaponName)
    {
        this.weaponName.text = weaponName;
    }

    private void RoundsInMagChanged(int roundsInMag)
    {
        this.roundsInMag.text = roundsInMag.ToString("D2");
    }

    private void TotalAmmoChanged(int totalAmmo, PlayerManager.AmmoType ammoType)
    {
        if(PlayerManager.instance.player.GetComponent<WeaponController>().currentWeapon.ammoType == ammoType)
            this.totalAmmo.text = "/" + totalAmmo.ToString("D2");
    }

    #endregion

    #region Attachment Information Events

    private void FlashLightPowerChanged(float power)
    {
        flashlightPowerBar.transform.localScale = new Vector3(flashlightPowerBar.transform.localScale.x, power / 100f, flashlightPowerBar.transform.localScale.z);
    }

    private void SuppressorDurabilityChanged(float durability)
    {
        suppressorDurabilityBar.transform.localScale = new Vector3(suppressorDurabilityBar.transform.localScale.x, durability / 100f, suppressorDurabilityBar.transform.localScale.z);
    }

    #endregion
}
