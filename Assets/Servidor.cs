using UnityEngine;

public class Servidor : MonoBehaviour
{
    public int VidaMaxima = 100;
    public int VidaActual;

    void start()
    {
        VidaActual = VidaMaxima;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Enemigo"))
        {


            RecibirDanio(10);

            Destroy(other.gameObject);
        }
    }

    public void RecibirDanio(int danio)
    {
        VidaActual -= danio;

        if (VidaActual <= 0)
        {
            VidaActual = 0;
            perder();
        }
    }

    void perder()
    {

    }
}
