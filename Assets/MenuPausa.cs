using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Menú de pausa de la partida: se abre/cierra con Escape y congela el tiempo
// del juego (Time.timeScale = 0). Construye toda su UI por código, igual que
// el menú principal.
public class MenuPausa : MonoBehaviour
{
    [Header("Configuracion")]
    public string escenaMenuInicial = "MenuPrincipal";

    [Header("Estilo")]
    public Color colorFondo = new Color(0f, 0f, 0f, 0.7f); // semitransparente: se ve el juego detrás
    public Color colorBoton = new Color(0.17f, 0.24f, 0.35f, 1f);
    public Color colorBotonResaltado = new Color(0.25f, 0.36f, 0.52f, 1f);
    public Color colorTexto = Color.white;

    private Font fuente;
    private GameObject canvasPausa;
    private GameObject panelPausa;
    private GameObject panelOpciones;
    private bool pausado;

    void Start()
    {
        fuente = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // Restaura volumen/pantalla/resolución guardados de sesiones anteriores
        Opciones.AplicarGuardado();

        CrearEventSystem();
        Canvas canvas = CrearCanvas();
        canvasPausa = canvas.gameObject;
        CrearFondo(canvas.transform);

        panelPausa = CrearPanel(canvas.transform, "PanelPausa");
        CrearTitulo(panelPausa.transform, "Pausa");
        CrearBoton(panelPausa.transform, "Reanudar", 60f, Reanudar);
        CrearBoton(panelPausa.transform, "Opciones", -20f, AbrirOpciones);
        CrearBoton(panelPausa.transform, "Salir al menu", -100f, SalirAlMenu);
        CrearBoton(panelPausa.transform, "Salir al escritorio", -180f, SalirAlEscritorio);

        // Panel de Opciones compartido con el menú principal (mismo código y valores)
        panelOpciones = Opciones.ConstruirPanel(canvas.transform, fuente, VolverAPausa);

        // El menú arranca cerrado y el juego corriendo
        canvasPausa.SetActive(false);
        Time.timeScale = 1f;
        pausado = false;
    }

    void Update()
    {
        // Con el Game Over en pantalla, la pausa queda deshabilitada
        if (HUDJuego.JuegoTerminado)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (pausado)
            {
                Reanudar();
            }
            else
            {
                Pausar();
            }
        }
    }

    // ---------- Acciones ----------

    void Pausar()
    {
        pausado = true;
        Time.timeScale = 0f; // congela física, NavMesh y todo lo que use deltaTime
        canvasPausa.SetActive(true);
        VolverAPausa();
    }

    public void Reanudar()
    {
        pausado = false;
        Time.timeScale = 1f;
        canvasPausa.SetActive(false);
    }

    void AbrirOpciones()
    {
        panelPausa.SetActive(false);
        panelOpciones.SetActive(true);
    }

    void VolverAPausa()
    {
        panelPausa.SetActive(true);
        panelOpciones.SetActive(false);
    }

    void SalirAlMenu()
    {
        // MUY importante: destrabar el tiempo antes de irnos, porque timeScale
        // sobrevive al cambio de escena y dejaría el menú congelado.
        // Recargar la escena del juego con "Jugar" la reinicia entera
        // (servidor, enemigos, muros, torretas) automáticamente.
        Time.timeScale = 1f;
        SceneManager.LoadScene(escenaMenuInicial);
    }

    void SalirAlEscritorio()
    {
        Time.timeScale = 1f;
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public bool EstaPausado()
    {
        return pausado;
    }

    // ---------- Construcción de la UI ----------

    void CrearEventSystem()
    {
        if (FindAnyObjectByType<EventSystem>() != null)
        {
            return;
        }

        GameObject es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
        es.AddComponent<StandaloneInputModule>();
    }

    Canvas CrearCanvas()
    {
        GameObject objetoCanvas = new GameObject("CanvasPausa");
        Canvas canvas = objetoCanvas.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100; // por encima de cualquier otra UI del juego

        CanvasScaler escalador = objetoCanvas.AddComponent<CanvasScaler>();
        escalador.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        escalador.referenceResolution = new Vector2(1920f, 1080f);
        escalador.matchWidthOrHeight = 0.5f;

        objetoCanvas.AddComponent<GraphicRaycaster>();
        return canvas;
    }

    void CrearFondo(Transform padre)
    {
        GameObject fondo = new GameObject("Fondo");
        fondo.transform.SetParent(padre, false);

        Image imagen = fondo.AddComponent<Image>();
        imagen.color = colorFondo;

        EstirarATodaLaPantalla(fondo.GetComponent<RectTransform>());
    }

    GameObject CrearPanel(Transform padre, string nombre)
    {
        GameObject panel = new GameObject(nombre);
        panel.transform.SetParent(padre, false);
        EstirarATodaLaPantalla(panel.AddComponent<RectTransform>());
        return panel;
    }

    void CrearTitulo(Transform padre, string texto)
    {
        Text titulo = CrearTexto(padre, texto, 90, new Vector2(0f, 280f));
        titulo.fontStyle = FontStyle.Bold;
    }

    Text CrearTexto(Transform padre, string contenido, int tamanio, Vector2 posicion)
    {
        GameObject objeto = new GameObject("Texto_" + contenido);
        objeto.transform.SetParent(padre, false);

        Text texto = objeto.AddComponent<Text>();
        texto.text = contenido;
        texto.font = fuente;
        texto.fontSize = tamanio;
        texto.color = colorTexto;
        texto.alignment = TextAnchor.MiddleCenter;

        RectTransform rect = objeto.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(900f, 130f);
        rect.anchoredPosition = posicion;

        return texto;
    }

    void CrearBoton(Transform padre, string etiqueta, float posicionY, UnityEngine.Events.UnityAction accion)
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
        rect.anchoredPosition = new Vector2(0f, posicionY);

        Text texto = CrearTexto(objetoBoton.transform, etiqueta, 32, Vector2.zero);
        texto.rectTransform.sizeDelta = rect.sizeDelta;
    }

    void EstirarATodaLaPantalla(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
