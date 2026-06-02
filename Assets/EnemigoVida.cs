using Unity.VisualScripting;
using UnityEngine;

public class EnemigoVida : MonoBehaviour 
{
    public float VidaMAxima = 30f;
    public float VidaActual;

    void Start()
    {
        VidaActual = VidaMAxima;
    }
    
    public void RecibirDanio(float danio)
    {
        VidaActual -= danio;
        Debug.Log(gameObject.name + " recibio daño. Vida restante: " + VidaActual);

        if (VidaActual <= 0)
        {
            Morir();
        }
    }

    public void Morir()
    {
        Debug.Log("Enemigo eliminado!");
        Destroy(gameObject);
    }
}
