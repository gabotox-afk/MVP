using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Construye el menú inicial entero por código: Canvas, EventSystem y los botones
// Jugar / Personaje / Opciones / Salir. Personaje y Opciones abren paneles vacíos
// (placeholder) con un botón Volver, listos para llenarse más adelante.
public class MenuPrincipal : MonoBehaviour
{
    [Header("Configuracion")]
    public string escenaDelJuego = "SampleScene";
    public string tituloDelJuego = "MVP";

    [Header("Estilo")]
    public Color colorFondo = new Color(0.08f, 0.09f, 0.12f, 1f);
    public Color colorBoton = new Color(0.17f, 0.24f, 0.35f, 1f);
    public Color colorBotonResaltado = new Color(0.25f, 0.36f, 0.52f, 1f);
    public Color colorTexto = Color.white;

    private Font fuente;
    private GameObject fondo;
    private GameObject panelPrincipal;
    private GameObject panelPersonaje;
    private GameObject panelOpciones;

    // Ruleta de personajes
    private GameObject[] previewsPersonajes;
    private Text textoNombrePersonaje;
    private Text textoDescripcionPersonaje;
    private Text textoBotonAceptar;
    private int indicePersonajeMostrado;

    void Start()
    {
        fuente = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // Restaura volumen/pantalla/resolución guardados de sesiones anteriores
        Opciones.AplicarGuardado();

        CrearEventSystem();
        Canvas canvas = CrearCanvas();
        CrearFondo(canvas.transform);

        // Panel principal con el título y los 4 botones
        panelPrincipal = CrearPanel(canvas.transform, "PanelPrincipal");
        CrearTitulo(panelPrincipal.transform, tituloDelJuego);
        CrearBoton(panelPrincipal.transform, "Jugar", 60f, Jugar);
        CrearBoton(panelPrincipal.transform, "Personaje", -20f, AbrirPersonaje);
        CrearBoton(panelPrincipal.transform, "Opciones", -100f, AbrirOpciones);
        CrearBoton(panelPrincipal.transform, "Salir", -180f, Salir);

        // Ruleta de personajes
        panelPersonaje = CrearPanelPersonaje(canvas.transform);
        CrearPreviewsPersonajes();

        // Panel de Opciones compartido con el menú de pausa (mismo código y valores)
        panelOpciones = Opciones.ConstruirPanel(canvas.transform, fuente, VolverAlPrincipal);

        MostrarPanel(panelPrincipal);
    }

    void Update()
    {
        // Hacemos girar el volumen del personaje que se está mostrando
        if (panelPersonaje != null && panelPersonaje.activeSelf && previewsPersonajes != null)
        {
            GameObject preview = previewsPersonajes[indicePersonajeMostrado];
            if (preview != null)
            {
                preview.transform.Rotate(Vector3.up * 60f * Time.deltaTime, Space.World);
            }
        }
    }

    // ---------- Acciones de los botones ----------

    void Jugar()
    {
        SceneManager.LoadScene(escenaDelJuego);
    }

    void AbrirPersonaje()
    {
        MostrarPanel(panelPersonaje);
    }

    void AbrirOpciones()
    {
        MostrarPanel(panelOpciones);
    }

    void VolverAlPrincipal()
    {
        MostrarPanel(panelPrincipal);
    }

    void Salir()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    void MostrarPanel(GameObject panel)
    {
        panelPrincipal.SetActive(panel == panelPrincipal);
        panelPersonaje.SetActive(panel == panelPersonaje);
        panelOpciones.SetActive(panel == panelOpciones);

        // En la ruleta escondemos el fondo opaco para que se vea el volumen 3D
        // del personaje detrás de la UI (la cámara pinta el fondo de color)
        bool enRuleta = panel == panelPersonaje;
        if (fondo != null)
        {
            fondo.SetActive(!enRuleta);
        }
        ActualizarPreviewVisible(enRuleta);
    }

    // ---------- Ruleta de personajes ----------

    GameObject CrearPanelPersonaje(Transform padre)
    {
        GameObject panel = CrearPanel(padre, "PanelPersonaje");
        CrearTitulo(panel.transform, "Personaje");

        // Nombre y descripción del personaje mostrado (se actualizan al girar la ruleta)
        textoNombrePersonaje = CrearTexto(panel.transform, "", 52, new Vector2(0f, -160f));
        textoNombrePersonaje.fontStyle = FontStyle.Bold;
        textoDescripcionPersonaje = CrearTexto(panel.transform, "", 26, new Vector2(0f, -230f));

        // Flechas de la ruleta, a los costados del volumen
        CrearBoton(panel.transform, "<", new Vector2(-380f, 30f), new Vector2(90f, 90f), () => CambiarPersonaje(-1));
        CrearBoton(panel.transform, ">", new Vector2(380f, 30f), new Vector2(90f, 90f), () => CambiarPersonaje(1));

        // Aceptar confirma el personaje mostrado; el texto del botón avisa
        // cuál es el que ya está seleccionado
        GameObject botonAceptar = CrearBoton(panel.transform, "Aceptar", -310f, AceptarPersonaje);
        textoBotonAceptar = botonAceptar.GetComponentInChildren<Text>();

        CrearBoton(panel.transform, "Volver", -390f, VolverAlPrincipal);
        return panel;
    }

    void AceptarPersonaje()
    {
        // Recién acá se confirma la elección (y persiste entre partidas)
        SeleccionPersonaje.IndiceElegido = indicePersonajeMostrado;
        RefrescarRuleta();
    }

    void CrearPreviewsPersonajes()
    {
        // Material sin iluminación: la escena del menú no tiene luces y con el
        // material default los volúmenes se verían negros
        Shader shaderUnlit = Shader.Find("Universal Render Pipeline/Unlit");

        previewsPersonajes = new GameObject[SeleccionPersonaje.Personajes.Length];

        for (int i = 0; i < SeleccionPersonaje.Personajes.Length; i++)
        {
            SeleccionPersonaje.DatosPersonaje datos = SeleccionPersonaje.Personajes[i];

            PrimitiveType primitiva = PrimitiveType.Capsule;
            if (datos.forma == SeleccionPersonaje.Forma.Cubo) primitiva = PrimitiveType.Cube;
            if (datos.forma == SeleccionPersonaje.Forma.Esfera) primitiva = PrimitiveType.Sphere;

            GameObject preview = GameObject.CreatePrimitive(primitiva);
            preview.name = "Preview_" + datos.nombre;

            // Frente a la cámara del menú (está en 0,1,-10 mirando hacia +Z)
            preview.transform.position = new Vector3(0f, 1f, -7f);
            preview.transform.localScale = Vector3.one * 1.4f;

            // El collider de la primitiva no sirve para nada en el menú
            Destroy(preview.GetComponent<Collider>());

            Renderer render = preview.GetComponent<Renderer>();
            if (shaderUnlit != null)
            {
                render.material = new Material(shaderUnlit);
            }
            render.material.color = datos.color;

            preview.SetActive(false);
            previewsPersonajes[i] = preview;
        }

        indicePersonajeMostrado = SeleccionPersonaje.IndiceElegido;
        RefrescarRuleta();
    }

    void CambiarPersonaje(int direccion)
    {
        // Girar la ruleta solo muestra el personaje: la elección se confirma con Aceptar
        int cantidad = SeleccionPersonaje.Personajes.Length;
        indicePersonajeMostrado = (indicePersonajeMostrado + direccion + cantidad) % cantidad;
        RefrescarRuleta();
    }

    void RefrescarRuleta()
    {
        SeleccionPersonaje.DatosPersonaje datos = SeleccionPersonaje.Personajes[indicePersonajeMostrado];
        textoNombrePersonaje.text = datos.nombre;
        textoDescripcionPersonaje.text = datos.descripcion;

        if (textoBotonAceptar != null)
        {
            bool yaSeleccionado = indicePersonajeMostrado == SeleccionPersonaje.IndiceElegido;
            textoBotonAceptar.text = yaSeleccionado ? "Seleccionado" : "Aceptar";
        }

        ActualizarPreviewVisible(panelPersonaje != null && panelPersonaje.activeSelf);
    }

    void ActualizarPreviewVisible(bool ruletaAbierta)
    {
        if (previewsPersonajes == null)
        {
            return;
        }

        for (int i = 0; i < previewsPersonajes.Length; i++)
        {
            if (previewsPersonajes[i] != null)
            {
                previewsPersonajes[i].SetActive(ruletaAbierta && i == indicePersonajeMostrado);
            }
        }
    }

    // ---------- Construcción de la UI ----------

    void CrearEventSystem()
    {
        // Sin EventSystem los botones no reciben clicks
        if (FindFirstObjectByType<EventSystem>() != null)
        {
            return;
        }

        GameObject es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
        es.AddComponent<StandaloneInputModule>();
    }

    Canvas CrearCanvas()
    {
        GameObject objetoCanvas = new GameObject("CanvasMenu");
        Canvas canvas = objetoCanvas.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        // Escala la UI con la resolución para que se vea igual en cualquier pantalla
        CanvasScaler escalador = objetoCanvas.AddComponent<CanvasScaler>();
        escalador.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        escalador.referenceResolution = new Vector2(1920f, 1080f);
        escalador.matchWidthOrHeight = 0.5f;

        objetoCanvas.AddComponent<GraphicRaycaster>();
        return canvas;
    }

    void CrearFondo(Transform padre)
    {
        fondo = new GameObject("Fondo");
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

    GameObject CrearBoton(Transform padre, string etiqueta, float posicionY, UnityEngine.Events.UnityAction accion)
    {
        return CrearBoton(padre, etiqueta, new Vector2(0f, posicionY), new Vector2(420f, 64f), accion);
    }

    GameObject CrearBoton(Transform padre, string etiqueta, Vector2 posicion, Vector2 tamanio, UnityEngine.Events.UnityAction accion)
    {
        GameObject objetoBoton = new GameObject("Boton_" + etiqueta);
        objetoBoton.transform.SetParent(padre, false);

        // La imagen queda blanca: el color real lo aplica el tinte del Button
        Image imagen = objetoBoton.AddComponent<Image>();
        imagen.color = Color.white;

        Button boton = objetoBoton.AddComponent<Button>();
        boton.onClick.AddListener(accion);

        // Resaltado al pasar el mouse (los colores del Button tiñen la imagen)
        ColorBlock colores = boton.colors;
        colores.normalColor = colorBoton;
        colores.highlightedColor = colorBotonResaltado;
        colores.pressedColor = colorBoton * 0.7f;
        boton.colors = colores;

        RectTransform rect = objetoBoton.GetComponent<RectTransform>();
        rect.sizeDelta = tamanio;
        rect.anchoredPosition = posicion;

        Text texto = CrearTexto(objetoBoton.transform, etiqueta, 32, Vector2.zero);
        texto.rectTransform.sizeDelta = rect.sizeDelta;

        return objetoBoton;
    }

    void EstirarATodaLaPantalla(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
