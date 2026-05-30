using UnityEngine;

public class Servidor : MonoBehaviour
{
    public int VidaMaxima = 100;
    public int VidaActual;

    void Start()
    {
        VidaActual = VidaMaxima;
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("ALGO TOCO EL SERVIDOR: " + other.gameObject.name);

        if (other.CompareTag("Enemigo"))
        {
            RecibirDanio(10);

            Destroy(other.gameObject);
        }
    }

    public void RecibirDanio(int danio)
    {
        VidaActual -= danio;
        Debug.Log("¡El servidor sufrio danio! Integridad actual: " + VidaActual);

        if (VidaActual <= 0)
        {
            VidaActual = 0;
            Defeat();
        }
    }

    void Defeat()
    {
        Debug.Log("GAME OVER: El servidor fue destruido.");
    }
}
