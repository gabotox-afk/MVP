using UnityEngine;

public class EnemigoVida : MonoBehaviour
{
    public float VidaMaxima = 30f;
    public float VidaActual;

    private Renderer render;
    private Color colorOriginal;
    private IndicadorVidaEnemigo indicador;

    void Start()
    {
        VidaActual = VidaMaxima;

        render = GetComponent<Renderer>();
        if (render != null)
        {
            colorOriginal = render.material.color;
        }

        // Numero de vida flotante sobre la cabeza
        indicador = IndicadorVidaEnemigo.Crear(transform);
        indicador.Actualizar(VidaActual, VidaMaxima);
    }

    public void RecibirDanio(float danio)
    {
        VidaActual -= danio;

        // Feedback visual: el enemigo se oscurece a medida que pierde vida
        if (render != null)
        {
            float fraccion = Mathf.Clamp01(VidaActual / VidaMaxima);
            render.material.color = Color.Lerp(Color.black, colorOriginal, 0.3f + 0.7f * fraccion);
        }

        if (indicador != null)
        {
            indicador.Actualizar(VidaActual, VidaMaxima);
        }

        if (VidaActual <= 0)
        {
            Morir();
        }
    }

    public void Morir()
    {
        Destroy(gameObject);
    }
}
