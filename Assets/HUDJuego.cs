using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// HUD de la partida: barra de vida del servidor arriba y pantalla de Game Over
// cuando el servidor muere. Construye su UI por código, como los menús.
public class HUDJuego : MonoBehaviour
{
    [Header("Configuracion")]
    public string escenaMenuInicial = "MenuPrincipal";

    [Header("Estilo")]
    public Color colorVidaLlena = new Color(0.3f, 0.8f, 0.35f, 1f);
    public Color colorVidaVacia = new Color(0.85f, 0.25f, 0.2f, 1f);
    public Color colorBoton = new Color(0.17f, 0.24f, 0.35f, 1f);
    public Color colorBotonResaltado = new Color(0.25f, 0.36f, 0.52f, 1f);

    // Para que otros scripts (el menú de pausa) sepan que la partida terminó
    public static bool JuegoTerminado;

    private Servidor servidor;
    private Font fuente;
    private RectTransform rellenoBarra;
    private Image imagenRelleno;
    private Text textoVida;
    private GameObject panelGameOver;

    void Start()
    {
        JuegoTerminado = false;

        servidor = FindFirstObjectByType<Servidor>();
        if (servidor == null)
        {
            Debug.LogError("HUD: no se encontro el Servidor en la escena");
            enabled = false;
            return;
        }

        fuente = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        Canvas canvas = CrearCanvas();
        CrearBarraDeVida(canvas.transform);
        panelGameOver = CrearPanelGameOver(canvas.transform);
        panelGameOver.SetActive(false);
    }

    void Update()
    {
        if (servidor == null)
        {
            return;
        }

        ActualizarBarra();

        if (!JuegoTerminado && servidor.VidaActual <= 0)
        {
            MostrarGameOver();
        }
    }

    void ActualizarBarra()
    {
        float fraccion = Mathf.Clamp01((float)servidor.VidaActual / servidor.VidaMaxima);

        // El relleno ocupa la fracción de vida que queda (anclado a la izquierda)
        rellenoBarra.anchorMax = new Vector2(fraccion, 1f);
        imagenRelleno.color = Color.Lerp(colorVidaVacia, colorVidaLlena, fraccion);
        textoVida.text = "Servidor  " + servidor.VidaActual + " / " + servidor.VidaMaxima;
    }

    void MostrarGameOver()
    {
        JuegoTerminado = true;
        Time.timeScale = 0f; // congelamos la partida de fondo
        panelGameOver.SetActive(true);
    }

    void Reintentar()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    void SalirAlMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(escenaMenuInicial);
    }

    // ---------- Construcción de la UI ----------

    Canvas CrearCanvas()
    {
        GameObject objetoCanvas = new GameObject("CanvasHUD");
        Canvas canvas = objetoCanvas.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50; // debajo del menú de pausa (100)

        CanvasScaler escalador = objetoCanvas.AddComponent<CanvasScaler>();
        escalador.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        escalador.referenceResolution = new Vector2(1920f, 1080f);
        escalador.matchWidthOrHeight = 0.5f;

        objetoCanvas.AddComponent<GraphicRaycaster>();
        return canvas;
    }

    void CrearBarraDeVida(Transform padre)
    {
        // Contenedor anclado arriba al centro
        GameObject barra = new GameObject("BarraVidaServidor");
        barra.transform.SetParent(padre, false);
        RectTransform rectBarra = barra.AddComponent<RectTransform>();
        rectBarra.anchorMin = new Vector2(0.5f, 1f);
        rectBarra.anchorMax = new Vector2(0.5f, 1f);
        rectBarra.pivot = new Vector2(0.5f, 1f);
        rectBarra.sizeDelta = new Vector2(520f, 42f);
        rectBarra.anchoredPosition = new Vector2(0f, -16f);

        // Fondo oscuro
        GameObject fondo = new GameObject("Fondo");
        fondo.transform.SetParent(barra.transform, false);
        Image imagenFondo = fondo.AddComponent<Image>();
        imagenFondo.color = new Color(0f, 0f, 0f, 0.6f);
        Estirar(fondo.GetComponent<RectTransform>());

        // Relleno que se achica con la vida
        GameObject relleno = new GameObject("Relleno");
        relleno.transform.SetParent(barra.transform, false);
        imagenRelleno = relleno.AddComponent<Image>();
        rellenoBarra = relleno.GetComponent<RectTransform>();
        rellenoBarra.anchorMin = Vector2.zero;
        rellenoBarra.anchorMax = Vector2.one;
        rellenoBarra.offsetMin = new Vector2(4f, 4f);
        rellenoBarra.offsetMax = new Vector2(-4f, -4f);

        // Texto encima de la barra
        textoVida = CrearTexto(barra.transform, "", 24, Vector2.zero, new Vector2(520f, 42f));
        textoVida.fontStyle = FontStyle.Bold;
    }

    GameObject CrearPanelGameOver(Transform padre)
    {
        GameObject panel = new GameObject("PanelGameOver");
        panel.transform.SetParent(padre, false);
        Estirar(panel.AddComponent<RectTransform>());

        // Fondo rojo oscuro semitransparente
        GameObject fondo = new GameObject("Fondo");
        fondo.transform.SetParent(panel.transform, false);
        Image imagenFondo = fondo.AddComponent<Image>();
        imagenFondo.color = new Color(0.25f, 0.02f, 0.02f, 0.85f);
        Estirar(fondo.GetComponent<RectTransform>());

        Text titulo = CrearTexto(panel.transform, "GAME OVER", 100, new Vector2(0f, 160f), new Vector2(900f, 140f));
        titulo.fontStyle = FontStyle.Bold;
        CrearTexto(panel.transform, "El servidor fue destruido", 32, new Vector2(0f, 60f), new Vector2(900f, 60f));

        CrearBoton(panel.transform, "Reintentar", new Vector2(0f, -60f), Reintentar);
        CrearBoton(panel.transform, "Salir al menu", new Vector2(0f, -140f), SalirAlMenu);

        return panel;
    }

    Text CrearTexto(Transform padre, string contenido, int tamanio, Vector2 posicion, Vector2 dimension)
    {
        GameObject objeto = new GameObject("Texto");
        objeto.transform.SetParent(padre, false);

        Text texto = objeto.AddComponent<Text>();
        texto.text = contenido;
        texto.font = fuente;
        texto.fontSize = tamanio;
        texto.color = Color.white;
        texto.alignment = TextAnchor.MiddleCenter;

        RectTransform rect = objeto.GetComponent<RectTransform>();
        rect.sizeDelta = dimension;
        rect.anchoredPosition = posicion;

        return texto;
    }

    void CrearBoton(Transform padre, string etiqueta, Vector2 posicion, UnityEngine.Events.UnityAction accion)
    {
        GameObject objetoBoton = new GameObject("Boton_" + etiqueta);
        objetoBoton.transform.SetParent(padre, false);

        Image imagen = objetoBoton.AddComponent<Image>();
        imagen.color = Color.white;

        Button boton = objetoBoton.AddComponent<Button>();
        boton.onClick.AddListener(accion);

        ColorBlock colores = boton.colors;
        colores.normalColor = colorBoton;
        colores.highlightedColor = colorBotonResaltado;
        colores.pressedColor = colorBoton * 0.7f;
        boton.colors = colores;

        RectTransform rect = objetoBoton.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(420f, 64f);
        rect.anchoredPosition = posicion;

        Text texto = CrearTexto(objetoBoton.transform, etiqueta, 32, Vector2.zero, new Vector2(420f, 64f));
    }

    void Estirar(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
