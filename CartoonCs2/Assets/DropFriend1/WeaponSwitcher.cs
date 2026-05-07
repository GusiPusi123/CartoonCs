using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponSwitcher : MonoBehaviour
{
    public GameObject[] weapons; // Массив оружия

    void Start()
    {
        // Выключаем всё оружие кроме первого
        for (int i = 0; i < weapons.Length; i++)
        {
            weapons[i].SetActive(i == 0);
        }
    }

    // Вызывается кнопкой из Canvas
    public void SelectWeapon(int weaponIndex)
    {
        // Выключаем всё оружие
        for (int i = 0; i < weapons.Length; i++)
        {
            weapons[i].SetActive(false);
        }

        // Включаем выбранное
        if (weaponIndex >= 0 && weaponIndex < weapons.Length)
        {
            weapons[weaponIndex].SetActive(true);
        }
    }
}
