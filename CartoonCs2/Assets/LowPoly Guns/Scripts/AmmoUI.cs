using UnityEngine;
using TMPro;

/// <summary>
/// Отображает количество патронов в магазине текущего оружия.
/// Работает через интерфейс IAmmoMagazine, поэтому не зависит
/// от конкретного класса оружия (WeaponRaycast и т.д.).
/// </summary>
public class AmmoUI : MonoBehaviour
{
    [Header("Оружие (перетащи сюда объект с компонентом, реализующим IAmmoMagazine)")]
    [SerializeField] private MonoBehaviour weaponSource;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI ammoText;
    [SerializeField] private string format = "{0} / {1}";
    [SerializeField] private string reloadingText = "Перезарядка...";

    private IAmmoMagazine ammo;

    private void Awake()
    {
        TryBindWeapon(weaponSource);
    }

    private void Update()
    {
        if (ammo == null || ammoText == null) return;

        ammoText.text = ammo.IsReloading
            ? reloadingText
            : string.Format(format, ammo.CurrentAmmo, ammo.MagazineSize);
    }

    /// <summary>
    /// Вызывай этот метод при смене оружия (например, из скрипта переключения оружия),
    /// передав новый MonoBehaviour, реализующий IAmmoMagazine.
    /// </summary>
    public void TryBindWeapon(MonoBehaviour source)
    {
        weaponSource = source;
        ammo = source as IAmmoMagazine;

        if (source != null && ammo == null)
        {
            Debug.LogWarning($"[{name}] {source.name} не реализует IAmmoMagazine.");
        }
    }
}
