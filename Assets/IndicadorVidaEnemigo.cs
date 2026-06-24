using UnityEngine;
using UnityEngine.UI;

// Indicador numerico de vida sobre la cabeza del enemigo. Se autogenera por codigo
// (mismo patron que el HUD: Canvas + Text legacy, sin prefabs) asi no hay que tocar
// el prefab del enemigo. EnemigoVida lo crea y le avisa cada vez que cambia la vida.
[RequireComponent(typeof(Canvas))]
public class IndicadorVidaEnemigo : MonoBehaviour
{
    public float alturaSobreCabeza = 1.6f; // cuanto por encima del centro flota el numero
    public int tamanioFuente = 40;         // en "pixeles" del canvas (luego se escala chico)

    private Text texto;
    private Camera camara;

    public static IndicadorVidaEnemigo Crear(Transform enemigo)
    {
        // GameObject SUELTO (no hijo): si fuera hijo, heredaria la escala/rotacion del
        // enemigo y al destruirse este se llevaria el canvas a mitad de Awake. Lo
        // seguimos manualmente en LateUpdate.
        GameObject objeto = new GameObject("IndicadorVidaEnemigo");
        IndicadorVidaEnemigo ind = objeto.AddComponent<IndicadorVidaEnemigo>();
        ind.objetivo = enemigo;
        return ind;
    }

    private Transform objetivo;

    void Awake()
    {
        camara = Camera.main;

        Canvas canvas = GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;

        // Tamano en "pixeles" normal y luego escala chica para llevarlo a metros
        RectTransform rect = GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(200f, 80f);
        transform.localScale = Vector3.one * 0.01f;

        GameObject objetoTexto = new GameObject("Texto");
        objetoTexto.transform.SetParent(transform, false);

        texto = objetoTexto.AddComponent<Text>();
        texto.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        texto.fontSize = tamanioFuente;
        texto.fontStyle = FontStyle.Bold;
        texto.alignment = TextAnchor.MiddleCenter;
        texto.color = Color.white;
        texto.horizontalOverflow = HorizontalWrapMode.Overflow;
        texto.verticalOverflow = VerticalWrapMode.Overflow;
        texto.raycastTarget = false;

        // El texto llena todo el canvas (anclas estiradas)
        RectTransform rectTexto = texto.rectTransform;
        rectTexto.anchorMin = Vector2.zero;
        rectTexto.anchorMax = Vector2.one;
        rectTexto.offsetMin = Vector2.zero;
        rectTexto.offsetMax = Vector2.zero;
    }

    // Seguimos al enemigo y encaramos a la camara. Si el enemigo murio, nos vamos con el.
    void LateUpdate()
    {
        if (objetivo == null)
        {
            Destroy(gameObject);
            return;
        }

        transform.position = objetivo.position + Vector3.up * alturaSobreCabeza;

        if (camara == null)
        {
            camara = Camera.main;
            if (camara == null)
            {
                return;
            }
        }

        transform.rotation = camara.transform.rotation; // mira siempre de frente a la camara
    }

    // Muestra la vida actual; el color pasa de verde a rojo segun la fraccion restante
    public void Actualizar(float vidaActual, float vidaMaxima)
    {
        if (texto == null)
        {
            return;
        }

        texto.text = Mathf.CeilToInt(Mathf.Max(0f, vidaActual)).ToString();

        float fraccion = vidaMaxima > 0f ? Mathf.Clamp01(vidaActual / vidaMaxima) : 0f;
        texto.color = Color.Lerp(Color.red, Color.green, fraccion);
    }
}
