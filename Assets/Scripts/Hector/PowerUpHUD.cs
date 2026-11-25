using UnityEngine;
using UnityEngine.UI;

public class PowerUpHUD : MonoBehaviour
{
    [SerializeField] private PowerUpPool database;
    [SerializeField] private Image iconImage;
    [SerializeField] private GameObject root;   // marco/fondo del slot

    private void OnEnable()
    {
        Events.OnLocalPowerUpChanged += Refresh;
        Refresh(PowerUps.None);
    }

    private void OnDisable()
    {
        Events.OnLocalPowerUpChanged -= Refresh;
    }

    private void Refresh(PowerUps type)
    {
        var data = type == PowerUps.None ? null : database.Get(type);
        iconImage.sprite = data != null ? data.sprite : null;
        iconImage.enabled = data != null;
        if (root != null) root.SetActive(true); // cambia a 'data != null' si quieres ocultar el slot vacío
    }
}