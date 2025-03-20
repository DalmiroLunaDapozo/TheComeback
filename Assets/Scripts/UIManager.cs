using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class UIManager : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI ammo_ui;
    [SerializeField] private PlayerShooting player_shoot_system;

    private void OnEnable()
    {
        player_shoot_system.OnAmmoChanged += OnAmmoChange;    
    }
    private void OnAmmoChange(int currentAmmo, int ammoLimit)
    {
        ammo_ui.text = "Ammo: " + currentAmmo + "/" + ammoLimit;
    }
    void Update()
    {
        
    }
}
