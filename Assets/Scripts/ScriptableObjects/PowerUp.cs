using UnityEngine;

public enum PowerUps
{
    None,
    Soap,
    Burrito,
    Laxatives,
    Weights
}
[CreateAssetMenu(fileName = "PowerUp", menuName = "Scriptable Objects/PowerUp")]
public class PowerUp : ScriptableObject
{
    public Sprite sprite;
    public PowerUps powerUp;
    public string nombre;
    public int peso;
}
