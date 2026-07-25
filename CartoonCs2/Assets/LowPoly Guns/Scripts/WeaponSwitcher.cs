using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Смена оружия игрока. Поддерживает два способа переключения одновременно:
/// 1) Цифровые клавиши 1, 2, 3... — переключение сразу на конкретный слот.
/// 2) Клавиша E — циклическое переключение на следующее оружие в списке.
///
/// Если оружие убирают во время перезарядки — перезарядка прерывается,
/// патроны остаются в том количестве, в котором были (см. WeaponRaycast.OnUnequip).
/// </summary>
public class WeaponSwitcher : MonoBehaviour
{
    [Header("Оружие")]
    [Tooltip("Объекты оружия по порядку — индекс в списке соответствует клавише (0 = клавиша 1, 1 = клавиша 2, ...)")]
    [SerializeField] private List<GameObject> weapons = new List<GameObject>();
    [SerializeField] private int startingWeaponIndex = 0;

    [Header("Переключение цифрами")]
    [SerializeField] private bool enableNumberKeys = true;

    [Header("Переключение клавишей E")]
    [SerializeField] private bool enableCycleKey = true;
    [SerializeField] private KeyCode cycleKey = KeyCode.E;

    [Header("Переключение колесом мыши (опционально)")]
    [SerializeField] private bool enableScrollWheel = false;

    [Header("Прочее")]
    [Tooltip("Нельзя переключать оружие, пока не закончилась анимация выхватывания предыдущего")]
    [SerializeField] private float switchCooldown = 0.15f;

    [Header("События")]
    [Tooltip("Вызывается после смены оружия, передаёт GameObject нового активного оружия")]
    public UnityEvent<GameObject> onWeaponSwitched;

    private int currentIndex = -1;
    private float switchTimer;

    private void Start()
    {
        for (int i = 0; i < weapons.Count; i++)
        {
            if (weapons[i] != null)
                weapons[i].SetActive(false);
        }

        int startIndex = Mathf.Clamp(startingWeaponIndex, 0, weapons.Count - 1);
        SwitchTo(startIndex);
    }

    private void Update()
    {
        switchTimer -= Time.deltaTime;

        if (enableNumberKeys)
            HandleNumberKeys();

        if (enableCycleKey && Input.GetKeyDown(cycleKey))
            SwitchToNext();

        if (enableScrollWheel)
            HandleScrollWheel();
    }

    private void HandleNumberKeys()
    {
        // KeyCode.Alpha1 ... Alpha9 идут подряд, поэтому можно пройтись циклом
        for (int i = 0; i < weapons.Count && i < 9; i++)
        {
            KeyCode key = KeyCode.Alpha1 + i;
            if (Input.GetKeyDown(key))
            {
                SwitchTo(i);
                break;
            }
        }
    }

    private void HandleScrollWheel()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll > 0f)
            SwitchToNext();
        else if (scroll < 0f)
            SwitchToPrevious();
    }

    public void SwitchToNext()
    {
        if (weapons.Count == 0) return;
        int next = (currentIndex + 1) % weapons.Count;
        SwitchTo(next);
    }

    public void SwitchToPrevious()
    {
        if (weapons.Count == 0) return;
        int prev = (currentIndex - 1 + weapons.Count) % weapons.Count;
        SwitchTo(prev);
    }

    public void SwitchTo(int index)
    {
        if (index < 0 || index >= weapons.Count) return;
        if (index == currentIndex) return;
        if (switchTimer > 0f) return;
        if (weapons[index] == null) return;

        // Убираем текущее оружие
        if (currentIndex >= 0 && weapons[currentIndex] != null)
        {
            GameObject previous = weapons[currentIndex];
            IWeaponSwitchable switchable = previous.GetComponent<IWeaponSwitchable>();
            switchable?.OnUnequip();
            previous.SetActive(false);
        }

        // Достаём новое
        currentIndex = index;
        GameObject newWeapon = weapons[currentIndex];
        newWeapon.SetActive(true);

        IWeaponSwitchable newSwitchable = newWeapon.GetComponent<IWeaponSwitchable>();
        newSwitchable?.OnEquip();

        switchTimer = switchCooldown;

        onWeaponSwitched?.Invoke(newWeapon);
    }

    public GameObject CurrentWeapon => (currentIndex >= 0 && currentIndex < weapons.Count) ? weapons[currentIndex] : null;
    public int CurrentIndex => currentIndex;
}