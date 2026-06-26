using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Ruleta de bailes (easter egg): se abre/cierra con la tecla B y congela el tiempo
// del juego (Time.timeScale = 0) mientras está en pantalla, igual que el menú de pausa.
// Muestra un botón por cada baile configurado; al elegir uno, cierra el menú y
// el personaje reproduce ese baile entero con el juego ya reanudado.
// Construye toda su UI por código, siguiendo el patrón de MenuPausa.
public class MenuBailes : MonoBehaviour
{
    // Cada baile: la etiqueta que se ve en el botón y el nombre del trigger del
    // Animator que lo dispara (debe existir como parámetro en el controller del modelo).
    [System.Serializable]
    public class Baile
    {
        public string etiqueta = "Baile";
        public string trigger = "Baile1";
    }

    [Header("Bailes")]
    public Baile[] bailes;

    [Header("Estilo")]
    public Color colorFondo = new Color(0f, 0f, 0f, 0.7f); // semitransparente: se ve el juego detrás
    public Color colorBoton = new Color(0.17f, 0.24f, 0.35f, 1f);
    public Color colorBotonResaltado = new Color(0.25f, 0.36f, 0.52f, 1f);
    public Color colorTexto = Color.white;

    private Font fuente;
    private GameObject canvasBailes;
    private bool abierto;

    private Jugador jugador;
    private MenuPausa menuPausa;

    void Start()
    {
        fuente = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // Referencias que vamos a necesitar al abrir/elegir: las cacheamos una vez
        jugador = FindAnyObjectByType<Jugador>();
        menuPausa = FindAnyObjectByType<MenuPausa>();

        CrearEventSystem();
        Canvas canvas = CrearCanvas();
        canvasBailes = canvas.gameObject;
        CrearFondo(canvas.transform);

        ConstruirPanel(canvas.transform);

        // El menú arranca cerrado
        canvasBailes.SetActive(false);
        abierto = false;
    }

    void Update()
    {
        // Con el Game Over en pantalla, la ruleta queda deshabilitada
        if (HUDJuego.JuegoTerminado)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.B))
        {
            if (abierto)
            {
                Cerrar();
            }
            // No abrimos si la pausa está activa: pelearíamos por Time.timeScale
            else if (menuPausa == null || !menuPausa.EstaPausado())
            {
                Abrir();
            }
        }
    }

    // ---------- Acciones ----------

    void Abrir()
    {
        abierto = true;
        Time.timeScale = 0f; // congela el juego mientras elegís
        canvasBailes.SetActive(true);
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    void Cerrar()
    {
        abierto = false;
        Time.timeScale = 1f;
        canvasBailes.SetActive(false);
    }

    void ElegirBaile(int indice)
    {
        if (jugador != null && indice >= 0 && indice < bailes.Length)
        {
            jugador.Bailar(bailes[indice].trigger);
        }
        // Cerramos para que el baile se reproduzca con el juego ya reanudado
        Cerrar();
    }

    // ---------- Construcción de la UI ----------

    void ConstruirPanel(Transform padre)
    {
        GameObject panel = CrearPanel(padre, "PanelBailes");
        CrearTitulo(panel.transform, "Bailes");

        // Un botón por baile, apilados en columna hacia abajo desde arriba
        int cantidad = bailes != null ? bailes.Length : 0;
        for (int i = 0; i < cantidad; i++)
        {
            int indice = i; // captura por valor para el listener
            float posicionY = 120f - i * 80f;
            string etiqueta = string.IsNullOrEmpty(bailes[i].etiqueta) ? bailes[i].trigger : bailes[i].etiqueta;
            CrearBoton(panel.transform, etiqueta, posicionY, () => ElegirBaile(indice));
        }

        // Botón para salir sin bailar, debajo de la lista
        float posicionCerrar = 120f - cantidad * 80f - 20f;
        CrearBoton(panel.transform, "Cerrar", posicionCerrar, Cerrar);
    }

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
        GameObject objetoCanvas = new GameObject("CanvasBailes");
        Canvas canvas = objetoCanvas.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 90; // por encima del HUD, por debajo de la pausa (100)

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
