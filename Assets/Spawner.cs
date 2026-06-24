using UnityEngine;

// Gestor de oleadas: en vez de spawnear un enemigo sin fin, organiza la partida
// en oleadas con un descanso entre medio. La siguiente oleada empieza cuando se
// limpian todos los enemigos de la actual. Tras superar todas, gana (modo
// Campaña). En modo Infinito nunca termina y las oleadas siguen creciendo.
public class Spawner : MonoBehaviour
{
    [Header("Enemigo y puntos (config existente)")]
    public GameObject enemigo;
    public Transform[] puntosS;

    [Header("Ritmo de la oleada")]
    public float tiempoEntreSpawn = 1.5f; // intervalo entre enemigos dentro de una oleada
    public float tiempoDescanso = 8.0f;   // pausa entre oleadas para construir

    [Header("Dificultad")]
    public int totalOleadas = 10;          // oleadas para ganar (modo Campaña)
    public int enemigosOleadaBase = 5;     // enemigos en la oleada 1
    public int enemigosPorOleadaExtra = 2; // cuantos enemigos mas suma cada oleada

    [Header("Modo")]
    public bool modoInfinito = false; // si es true, nunca gana y las oleadas no paran

    // Maquina de estados de la partida
    private enum Estado { Descanso, Spawneando, EsperandoLimpieza, Terminado }
    private Estado estado;

    private int oleadaActual = 1;
    private int enemigosPorSpawnear;  // cuantos faltan instanciar en esta oleada
    private float cronometro;

    private HUDJuego hud;

    void Start()
    {
        hud = FindFirstObjectByType<HUDJuego>();

        // El modo se decide en el menu (por ahora siempre Campaña); si el flag
        // del Inspector ya esta en true, respetamos eso tambien
        modoInfinito = modoInfinito || ModoJuego.Elegido == ModoJuego.Modo.Infinito;

        EmpezarDescanso();
    }

    void Update()
    {
        // Si el juego termino (derrota o victoria), el spawner se apaga
        if (HUDJuego.JuegoTerminado)
        {
            estado = Estado.Terminado;
            return;
        }

        switch (estado)
        {
            case Estado.Descanso:
                ActualizarDescanso();
                break;
            case Estado.Spawneando:
                ActualizarSpawneo();
                break;
            case Estado.EsperandoLimpieza:
                ActualizarLimpieza();
                break;
        }
    }

    // ---------- Estados ----------

    void EmpezarDescanso()
    {
        estado = Estado.Descanso;
        cronometro = tiempoDescanso;
        MostrarMensajeOleada();
    }

    void ActualizarDescanso()
    {
        cronometro -= Time.deltaTime;
        MostrarMensajeOleada();

        if (cronometro <= 0f)
        {
            EmpezarSpawneo();
        }
    }

    void EmpezarSpawneo()
    {
        estado = Estado.Spawneando;
        enemigosPorSpawnear = enemigosOleadaBase + (oleadaActual - 1) * enemigosPorOleadaExtra;
        cronometro = 0f; // el primer enemigo sale enseguida
        MostrarMensajeOleada();
    }

    void ActualizarSpawneo()
    {
        cronometro -= Time.deltaTime;

        if (cronometro <= 0f && enemigosPorSpawnear > 0)
        {
            Spawnear();
            enemigosPorSpawnear--;
            cronometro = tiempoEntreSpawn;
        }

        // Cuando ya instanciamos todos, pasamos a esperar que los maten
        if (enemigosPorSpawnear <= 0)
        {
            estado = Estado.EsperandoLimpieza;
        }
    }

    void ActualizarLimpieza()
    {
        // La oleada esta limpia cuando no queda ningun enemigo vivo. Solo llegamos
        // aca despues de spawnear todos, asi que el conteo en 0 es real.
        if (GameObject.FindGameObjectsWithTag("Enemigo").Length > 0)
        {
            return;
        }

        bool ultimaOleada = oleadaActual >= totalOleadas;

        if (ultimaOleada && !modoInfinito)
        {
            estado = Estado.Terminado;
            if (hud != null)
            {
                hud.MostrarVictoria();
            }
            return;
        }

        oleadaActual++;
        EmpezarDescanso();
    }

    // ---------- Helpers ----------

    void MostrarMensajeOleada()
    {
        if (hud == null)
        {
            return;
        }

        string mensaje;
        if (estado == Estado.Descanso)
        {
            int segundos = Mathf.CeilToInt(Mathf.Max(0f, cronometro));
            mensaje = "Oleada " + EtiquetaOleada() + " en " + segundos + "...";
        }
        else
        {
            mensaje = "Oleada " + EtiquetaOleada();
        }

        hud.ActualizarOleada(mensaje);
    }

    // En Campaña mostramos "3 / 10"; en Infinito solo el numero, no hay tope
    string EtiquetaOleada()
    {
        return modoInfinito ? oleadaActual.ToString() : oleadaActual + " / " + totalOleadas;
    }

    void Spawnear()
    {
        if (enemigo == null || puntosS.Length == 0)
        {
            Debug.LogWarning("¡Faltan asignar los puntos de spawn o el prefab del enemigo en el Controlador!");
            return;
        }

        int indice = Random.Range(0, puntosS.Length);
        Transform punto = puntosS[indice];

        Instantiate(enemigo, punto.position, punto.rotation);
    }
}
