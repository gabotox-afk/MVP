using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Estado de las opciones del juego (persistido en PlayerPrefs) y constructor
// del panel de Opciones. El mismo panel lo usan el menú principal y el de
// pausa, así los valores son uno solo y no se duplica código.
public static class Opciones
{
    private const string ClaveVolumen = "OpcionVolumen";
    private const string ClavePantallaCompleta = "OpcionPantallaCompleta";
    private const string ClaveResolucionAncho = "OpcionResolucionAncho";
    private const string ClaveResolucionAlto = "OpcionResolucionAlto";

    // ---------- Valores guardados ----------

    public static float VolumenGeneral
    {
        get { return Mathf.Clamp01(PlayerPrefs.GetFloat(ClaveVolumen, 1f)); }
        set
        {
            PlayerPrefs.SetFloat(ClaveVolumen, Mathf.Clamp01(value));
            AudioListener.volume = Mathf.Clamp01(value);
        }
    }

    public static bool PantallaCompleta
    {
        get { return PlayerPrefs.GetInt(ClavePantallaCompleta, Screen.fullScreen ? 1 : 0) == 1; }
        set
        {
            PlayerPrefs.SetInt(ClavePantallaCompleta, value ? 1 : 0);
            Screen.fullScreen = value;
        }
    }

    // Aplica todo lo guardado: llamarlo al arrancar cada escena con menú
    public static void AplicarGuardado()
    {
        AudioListener.volume = VolumenGeneral;

        int ancho = PlayerPrefs.GetInt(ClaveResolucionAncho, 0);
        int alto = PlayerPrefs.GetInt(ClaveResolucionAlto, 0);
        if (ancho > 0 && alto > 0 && (ancho != Screen.width || alto != Screen.height))
        {
            Screen.SetResolution(ancho, alto, PantallaCompleta);
        }
        else
        {
            Screen.fullScreen = PantallaCompleta;
        }
    }

    private static void GuardarResolucion(int ancho, int alto)
    {
        PlayerPrefs.SetInt(ClaveResolucionAncho, ancho);
        PlayerPrefs.SetInt(ClaveResolucionAlto, alto);
        Screen.SetResolution(ancho, alto, PantallaCompleta);
    }

    // Resoluciones únicas (sin repetir por refresh rate), de menor a mayor
    private static List<Vector2Int> ResolucionesDisponibles()
    {
        List<Vector2Int> lista = new List<Vector2Int>();
        foreach (Resolution r in Screen.resolutions)
        {
            Vector2Int tamanio = new Vector2Int(r.width, r.height);
            if (!lista.Contains(tamanio))
            {
                lista.Add(tamanio);
            }
        }

        // En el editor la lista puede venir vacía: ofrecemos las comunes
        if (lista.Count == 0)
        {
            lista.Add(new Vector2Int(1280, 720));
            lista.Add(new Vector2Int(1920, 1080));
            lista.Add(new Vector2Int(2560, 1440));
        }

        return lista;
    }

    // ---------- Construcción del panel ----------

    private static readonly Color ColorBoton = new Color(0.17f, 0.24f, 0.35f, 1f);
    private static readonly Color ColorBotonResaltado = new Color(0.25f, 0.36f, 0.52f, 1f);
    private static readonly Color ColorTexto = Color.white;

    // Crea el panel completo de Opciones colgado del canvas dado.
    // alVolver: qué hacer cuando se toca el botón Volver (cada menú decide).
    public static GameObject ConstruirPanel(Transform padre, Font fuente, UnityEngine.Events.UnityAction alVolver)
    {
        GameObject panel = new GameObject("PanelOpciones");
        panel.transform.SetParent(padre, false);
        Estirar(panel.AddComponent<RectTransform>());

        Text titulo = CrearTexto(panel.transform, fuente, "Opciones", 90, new Vector2(0f, 280f), new Vector2(900f, 130f));
        titulo.fontStyle = FontStyle.Bold;

        // --- Volumen general ---
        CrearTexto(panel.transform, fuente, "Volumen", 30, new Vector2(-280f, 130f), new Vector2(300f, 50f));
        Text textoPorcentaje = CrearTexto(panel.transform, fuente, "", 26, new Vector2(390f, 130f), new Vector2(120f, 50f));
        Slider sliderVolumen = CrearSlider(panel.transform, new Vector2(110f, 130f), new Vector2(380f, 30f));
        sliderVolumen.value = VolumenGeneral;
        textoPorcentaje.text = Mathf.RoundToInt(VolumenGeneral * 100f) + "%";
        sliderVolumen.onValueChanged.AddListener(valor =>
        {
            VolumenGeneral = valor;
            textoPorcentaje.text = Mathf.RoundToInt(valor * 100f) + "%";
        });

        // --- Pantalla completa ---
        CrearTexto(panel.transform, fuente, "Pantalla completa", 30, new Vector2(-280f, 50f), new Vector2(340f, 50f));
        GameObject botonPantalla = CrearBoton(panel.transform, fuente, "", new Vector2(110f, 50f), new Vector2(160f, 50f), null);
        Text textoPantalla = botonPantalla.GetComponentInChildren<Text>();
        textoPantalla.text = PantallaCompleta ? "Si" : "No";
        botonPantalla.GetComponent<Button>().onClick.AddListener(() =>
        {
            PantallaCompleta = !PantallaCompleta;
            textoPantalla.text = PantallaCompleta ? "Si" : "No";
        });

        // --- Resolución (selector con flechas, como la ruleta) ---
        CrearTexto(panel.transform, fuente, "Resolucion", 30, new Vector2(-280f, -30f), new Vector2(300f, 50f));
        List<Vector2Int> resoluciones = ResolucionesDisponibles();
        int indiceActual = BuscarResolucionActual(resoluciones);

        Text textoResolucion = CrearTexto(panel.transform, fuente, TextoDe(resoluciones[indiceActual]), 28, new Vector2(110f, -30f), new Vector2(220f, 50f));

        CrearBoton(panel.transform, fuente, "<", new Vector2(-30f, -30f), new Vector2(50f, 50f), () =>
        {
            indiceActual = (indiceActual - 1 + resoluciones.Count) % resoluciones.Count;
            GuardarResolucion(resoluciones[indiceActual].x, resoluciones[indiceActual].y);
            textoResolucion.text = TextoDe(resoluciones[indiceActual]);
        });
        CrearBoton(panel.transform, fuente, ">", new Vector2(250f, -30f), new Vector2(50f, 50f), () =>
        {
            indiceActual = (indiceActual + 1) % resoluciones.Count;
            GuardarResolucion(resoluciones[indiceActual].x, resoluciones[indiceActual].y);
            textoResolucion.text = TextoDe(resoluciones[indiceActual]);
        });

        // --- Controles (solo lectura) ---
        Text controles = CrearTexto(panel.transform, fuente,
            "WASD / Flechas: moverse   |   1: Muro   |   2: Torreta Ataque\n" +
            "3: Torreta del personaje   |   Click izquierdo: empujar enemigos   |   Esc: pausa",
            22, new Vector2(0f, -140f), new Vector2(1000f, 90f));
        controles.color = new Color(1f, 1f, 1f, 0.7f);

        CrearBoton(panel.transform, fuente, "Volver", new Vector2(0f, -260f), new Vector2(420f, 64f), alVolver);

        return panel;
    }

    private static int BuscarResolucionActual(List<Vector2Int> resoluciones)
    {
        for (int i = 0; i < resoluciones.Count; i++)
        {
            if (resoluciones[i].x == Screen.width && resoluciones[i].y == Screen.height)
            {
                return i;
            }
        }
        return resoluciones.Count - 1;
    }

    private static string TextoDe(Vector2Int resolucion)
    {
        return resolucion.x + " x " + resolucion.y;
    }

    // ---------- Piezas de UI ----------

    private static Text CrearTexto(Transform padre, Font fuente, string contenido, int tamanio, Vector2 posicion, Vector2 dimension)
    {
        GameObject objeto = new GameObject("Texto");
        objeto.transform.SetParent(padre, false);

        Text texto = objeto.AddComponent<Text>();
        texto.text = contenido;
        texto.font = fuente;
        texto.fontSize = tamanio;
        texto.color = ColorTexto;
        texto.alignment = TextAnchor.MiddleCenter;

        RectTransform rect = objeto.GetComponent<RectTransform>();
        rect.sizeDelta = dimension;
        rect.anchoredPosition = posicion;

        return texto;
    }

    private static GameObject CrearBoton(Transform padre, Font fuente, string etiqueta, Vector2 posicion, Vector2 tamanio, UnityEngine.Events.UnityAction accion)
    {
        GameObject objetoBoton = new GameObject("Boton_" + etiqueta);
        objetoBoton.transform.SetParent(padre, false);

        Image imagen = objetoBoton.AddComponent<Image>();
        imagen.color = Color.white;

        Button boton = objetoBoton.AddComponent<Button>();
        if (accion != null)
        {
            boton.onClick.AddListener(accion);
        }

        ColorBlock colores = boton.colors;
        colores.normalColor = ColorBoton;
        colores.highlightedColor = ColorBotonResaltado;
        colores.pressedColor = ColorBoton * 0.7f;
        boton.colors = colores;

        RectTransform rect = objetoBoton.GetComponent<RectTransform>();
        rect.sizeDelta = tamanio;
        rect.anchoredPosition = posicion;

        Text texto = CrearTexto(objetoBoton.transform, fuente, etiqueta, 30, Vector2.zero, tamanio);

        return objetoBoton;
    }

    private static Slider CrearSlider(Transform padre, Vector2 posicion, Vector2 tamanio)
    {
        GameObject objetoSlider = new GameObject("SliderVolumen");
        objetoSlider.transform.SetParent(padre, false);
        RectTransform rectSlider = objetoSlider.AddComponent<RectTransform>();
        rectSlider.sizeDelta = tamanio;
        rectSlider.anchoredPosition = posicion;

        // Fondo de la barra
        GameObject fondo = new GameObject("Fondo");
        fondo.transform.SetParent(objetoSlider.transform, false);
        Image imagenFondo = fondo.AddComponent<Image>();
        imagenFondo.color = new Color(0.1f, 0.12f, 0.16f, 1f);
        Estirar(fondo.GetComponent<RectTransform>());

        // Relleno (la parte "llena" de la barra)
        GameObject areaRelleno = new GameObject("AreaRelleno");
        areaRelleno.transform.SetParent(objetoSlider.transform, false);
        RectTransform rectArea = areaRelleno.AddComponent<RectTransform>();
        Estirar(rectArea);
        rectArea.offsetMin = new Vector2(5f, 5f);
        rectArea.offsetMax = new Vector2(-5f, -5f);

        GameObject relleno = new GameObject("Relleno");
        relleno.transform.SetParent(areaRelleno.transform, false);
        Image imagenRelleno = relleno.AddComponent<Image>();
        imagenRelleno.color = ColorBotonResaltado;
        RectTransform rectRelleno = relleno.GetComponent<RectTransform>();
        rectRelleno.anchorMin = Vector2.zero;
        rectRelleno.anchorMax = new Vector2(0f, 1f);
        rectRelleno.sizeDelta = Vector2.zero;

        // Manija
        GameObject areaManija = new GameObject("AreaManija");
        areaManija.transform.SetParent(objetoSlider.transform, false);
        RectTransform rectAreaManija = areaManija.AddComponent<RectTransform>();
        Estirar(rectAreaManija);
        rectAreaManija.offsetMin = new Vector2(10f, 0f);
        rectAreaManija.offsetMax = new Vector2(-10f, 0f);

        GameObject manija = new GameObject("Manija");
        manija.transform.SetParent(areaManija.transform, false);
        Image imagenManija = manija.AddComponent<Image>();
        imagenManija.color = Color.white;
        RectTransform rectManija = manija.GetComponent<RectTransform>();
        rectManija.sizeDelta = new Vector2(20f, 0f);

        Slider slider = objetoSlider.AddComponent<Slider>();
        slider.fillRect = rectRelleno;
        slider.handleRect = rectManija;
        slider.targetGraphic = imagenManija;
        slider.minValue = 0f;
        slider.maxValue = 1f;

        return slider;
    }

    private static void Estirar(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
