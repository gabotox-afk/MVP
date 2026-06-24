using UnityEngine;

public class DestruirPorTiempo : MonoBehaviour
{
    [Header("Configuracion")]
    public float tiempoDeVida = 8.0f; // El tiempo en segundos antes de borrarse

    [Header("Aviso de desaparicion")]
    public float tiempoDeAviso = 2.0f;   // Segundos antes del final en que empieza a parpadear
    public float velocidadParpadeo = 8f; // Cuantas veces por segundo enciende/apaga

    private float cronometro;
    private Renderer[] renderers;

    void Start()
    {
        // Unity destruye este objeto automaticamente cuando pasa el tiempo asignado
        Destroy(gameObject, tiempoDeVida);

        // Cacheamos todos los renderers (la torreta y sus partes) para encenderlos/apagarlos
        renderers = GetComponentsInChildren<Renderer>();
    }

    void Update()
    {
        cronometro += Time.deltaTime;

        float tiempoRestante = tiempoDeVida - cronometro;

        // En los ultimos segundos parpadea para avisar que esta por desaparecer
        if (tiempoRestante <= tiempoDeAviso)
        {
            // Onda cuadrada: alterna visible/invisible segun la velocidad de parpadeo
            bool visible = Mathf.FloorToInt(cronometro * velocidadParpadeo) % 2 == 0;
            EstablecerVisibilidad(visible);
        }
    }

    void EstablecerVisibilidad(bool visible)
    {
        if (renderers == null)
        {
            return;
        }

        foreach (Renderer render in renderers)
        {
            if (render != null)
            {
                render.enabled = visible;
            }
        }
    }
}
