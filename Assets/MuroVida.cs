using UnityEngine;

public class MuroVida : MonoBehaviour
{
    public float VidaMaxima = 50f;
    public float VidaActual;

    private Renderer render;
    private Color colorOriginal;

    void Start()
    {
        VidaActual = VidaMaxima;

        render = GetComponent<Renderer>();
        if (render != null)
        {
            colorOriginal = render.material.color;
        }
    }

    public void RecibirDanio(float danio)
    {
        VidaActual -= danio;

        // Feedback visual: el muro se va tiñendo de rojo a medida que pierde vida
        if (render != null)
        {
            float fraccion = Mathf.Clamp01(VidaActual / VidaMaxima);
            render.material.color = Color.Lerp(new Color(0.7f, 0.15f, 0.1f), colorOriginal, fraccion);
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
