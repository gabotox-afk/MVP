using UnityEngine;

public class Servidor : MonoBehaviour
{
    public int VidaMaxima = 100;
    public int VidaActual;

    void Start()
    {
        VidaActual = VidaMaxima;
    }

    // El daño ahora lo aplica EnemigoIA por distancia (un solo camino de daño,
    // configurable con danioAlServidor): ya no usamos el trigger físico
    public void RecibirDanio(int danio)
    {
        VidaActual -= danio;

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
