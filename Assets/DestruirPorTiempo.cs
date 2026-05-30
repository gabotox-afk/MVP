using UnityEngine;

public class DestruirporTiempo : MonoBehaviour
{
    [Header("Configuracion")]
    public float tiempoDeVida = 8.0f; // El tiempo en segundos antes de borrarse

    void Start()
    {
        // Unity destruye este objeto automaticamente cuando pasa el tiempo asignado
        Destroy(gameObject, tiempoDeVida);
    }
}
