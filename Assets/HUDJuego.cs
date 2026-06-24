using System.Collections.Generic;
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

    [Header("Nombres legibles de estructuras")]
    // Mapea el tag del prefab a un nombre lindo para el HUD. Si un tag no esta
    // en la lista, se muestra el tag tal cual.
    public List<EtiquetaEstructura> nombresEstructuras = new List<EtiquetaEstructura>();

    [System.Serializable]
    public class EtiquetaEstructura
    {
        public string tag = "Muro";
        public string nombre = "Muros";
    }

    private Servidor servidor;
    private Font fuente;
    private RectTransform rellenoBarra;
    private Image imagenRelleno;
    private Text textoVida;
    private GameObject panelGameOver;

    // Contadores de estructuras: un texto por tag, dentro del panel inferior
    private RectTransform panelContadores;
    private Dictionary<string, Text> textosContador = new Dictionary<string, Text>();
    private Text textoAviso;
    private float cronometroAviso;

    private Text textoOleada;
    private GameObject panelVictoria;

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
        CrearPanelContadores(canvas.transform);
        CrearTextoAviso(canvas.transform);
        CrearTextoOleada(canvas.transform);
        panelGameOver = CrearPanelGameOver(canvas.transform);
        panelGameOver.SetActive(false);
        panelVictoria = CrearPanelVictoria(canvas.transform);
        panelVictoria.SetActive(false);
    }

    void Update()
    {
        if (servidor == null)
        {
            return;
        }

        ActualizarBarra();
        ActualizarAviso();

        if (!JuegoTerminado && servidor.VidaActual <= 0)
        {
            MostrarGameOver();
        }
    }

    // El mensaje "limite alcanzado" se desvanece a los pocos segundos.
    // Usamos tiempo sin escalar para que funcione aunque el juego este pausado.
    void ActualizarAviso()
    {
        if (textoAviso == null || cronometroAviso <= 0f)
        {
            return;
        }

        cronometroAviso -= Time.unscaledDeltaTime;

        Color color = textoAviso.color;
        color.a = Mathf.Clamp01(cronometroAviso); // fade out en el ultimo segundo
        textoAviso.color = color;

        if (cronometroAviso <= 0f)
        {
            textoAviso.text = "";
        }
    }

    // ---------- API publica para el Jugador ----------

    // Refresca el contador de un tipo. Si no existia su texto todavia, lo crea.
    public void ActualizarContador(string tag, int actual, int maximo)
    {
        Text texto = ObtenerTextoContador(tag);
        texto.text = NombreLegible(tag) + "  " + actual + " / " + maximo;
        texto.color = actual >= maximo ? colorVidaVacia : Color.white;
    }

    // Actualiza el texto de la oleada actual ("Oleada 3 / 10", "Oleada 4 en 5...")
    public void ActualizarOleada(string mensaje)
    {
        if (textoOleada != null)
        {
            textoOleada.text = mensaje;
        }
    }

    // Pantalla de victoria: espejo de MostrarGameOver pero en verde
    public void MostrarVictoria()
    {
        if (JuegoTerminado)
        {
            return;
        }

        JuegoTerminado = true;
        Time.timeScale = 0f;
        if (panelVictoria != null)
        {
            panelVictoria.SetActive(true);
        }
    }

    // Muestra el mensaje de que un tipo llego a su limite
    public void AvisarLimite(string tag, int maximo)
    {
        if (textoAviso == null)
        {
            return;
        }

        textoAviso.text = "Limite de " + NombreLegible(tag) + " alcanzado (" + maximo + ")";
        textoAviso.color = new Color(colorVidaVacia.r, colorVidaVacia.g, colorVidaVacia.b, 1f);
        cronometroAviso = 2.0f; // segundos visible antes de desvanecerse
    }

    string NombreLegible(string tag)
    {
        foreach (EtiquetaEstructura etiqueta in nombresEstructuras)
        {
            if (etiqueta != null && etiqueta.tag == tag)
            {
                return etiqueta.nombre;
            }
        }

        return tag;
    }

    // Devuelve el texto del contador de ese tag, creandolo dentro del panel si hace falta
    Text ObtenerTextoContador(string tag)
    {
        Text existente;
        if (textosContador.TryGetValue(tag, out existente) && existente != null)
        {
            return existente;
        }

        GameObject objeto = new GameObject("Contador_" + tag);
        objeto.transform.SetParent(panelContadores, false);

        Text texto = objeto.AddComponent<Text>();
        texto.font = fuente;
        texto.fontSize = 26;
        texto.color = Color.white;
        texto.alignment = TextAnchor.MiddleLeft;
        texto.fontStyle = FontStyle.Bold;

        RectTransform rect = objeto.GetComponent<RectTransform>();
        // Anclado arriba del panel; cada contador se apila hacia abajo
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.sizeDelta = new Vector2(0f, 34f);
        rect.anchoredPosition = new Vector2(12f, -8f - 34f * textosContador.Count);

        textosContador[tag] = texto;
        return texto;
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

    void CrearPanelContadores(Transform padre)
    {
        // Contenedor anclado abajo a la izquierda donde se apilan los contadores
        GameObject panel = new GameObject("PanelContadores");
        panel.transform.SetParent(padre, false);
        panelContadores = panel.AddComponent<RectTransform>();
        panelContadores.anchorMin = new Vector2(0f, 0f);
        panelContadores.anchorMax = new Vector2(0f, 0f);
        panelContadores.pivot = new Vector2(0f, 0f);
        panelContadores.sizeDelta = new Vector2(320f, 140f);
        panelContadores.anchoredPosition = new Vector2(20f, 20f);

        // Fondo oscuro semitransparente para legibilidad
        GameObject fondo = new GameObject("Fondo");
        fondo.transform.SetParent(panel.transform, false);
        Image imagenFondo = fondo.AddComponent<Image>();
        imagenFondo.color = new Color(0f, 0f, 0f, 0.45f);
        Estirar(fondo.GetComponent<RectTransform>());
    }

    void CrearTextoAviso(Transform padre)
    {
        // Mensaje centrado, un poco debajo de la barra de vida
        textoAviso = CrearTexto(padre, "", 30, new Vector2(0f, 0f), new Vector2(900f, 50f));
        textoAviso.fontStyle = FontStyle.Bold;

        RectTransform rect = textoAviso.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -70f);

        Color color = textoAviso.color;
        color.a = 0f;
        textoAviso.color = color;
    }

    void CrearTextoOleada(Transform padre)
    {
        // Centrado arriba, justo debajo de la barra de vida del servidor
        textoOleada = CrearTexto(padre, "", 30, new Vector2(0f, 0f), new Vector2(600f, 44f));
        textoOleada.fontStyle = FontStyle.Bold;

        RectTransform rect = textoOleada.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -64f);
    }

    GameObject CrearPanelVictoria(Transform padre)
    {
        GameObject panel = new GameObject("PanelVictoria");
        panel.transform.SetParent(padre, false);
        Estirar(panel.AddComponent<RectTransform>());

        // Fondo verde oscuro semitransparente
        GameObject fondo = new GameObject("Fondo");
        fondo.transform.SetParent(panel.transform, false);
        Image imagenFondo = fondo.AddComponent<Image>();
        imagenFondo.color = new Color(0.02f, 0.2f, 0.05f, 0.85f);
        Estirar(fondo.GetComponent<RectTransform>());

        Text titulo = CrearTexto(panel.transform, "¡VICTORIA!", 100, new Vector2(0f, 160f), new Vector2(900f, 140f));
        titulo.fontStyle = FontStyle.Bold;
        titulo.color = colorVidaLlena;
        CrearTexto(panel.transform, "Sobreviviste todas las oleadas", 32, new Vector2(0f, 60f), new Vector2(900f, 60f));

        CrearBoton(panel.transform, "Reintentar", new Vector2(0f, -60f), Reintentar);
        CrearBoton(panel.transform, "Salir al menu", new Vector2(0f, -140f), SalirAlMenu);

        return panel;
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
