using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class EnemigoIA : MonoBehaviour
{
    [Header("Ataque a Muros")]
    public float danioAMuro = 10f;
    public float rangoAtaqueMuro = 1.5f;
    public float cadenciaAtaqueMuro = 1f;
    public float rangoDeteccionMuro = 2.5f; // radio alrededor del enemigo para detectar muros cercanos y atacarlos

    [Header("Navegación")]
    public float intervaloRecalculoCamino = 0.4f; // cada cuánto chequeamos si hay camino libre al servidor

    [Header("Ataque al Servidor")]
    public int danioAlServidor = 10;
    public float rangoAtaqueServidor = 0.5f; // distancia al borde del servidor para explotar contra él (debe llegar casi pegado)

    private float cronometroAtaqueMuro;
    private MuroVida muroObjetivo;
    private Collider colliderMuroObjetivo;
    private bool caminoBloqueado;
    private int chequeosBloqueadoSeguidos; // exigimos 2 chequeos bloqueados seguidos antes de ir a romper muros
    private float cronometroRecalculoCamino;
    private Vector3 ultimoDestinoPedido;
    private bool tieneDestinoPedido;
    private float cronometroBusquedaMuro; // cooldown entre búsquedas fallidas de muro (son caras)
    private Vector3 puntoLlegadaServidor; // punto alcanzable desde donde podemos explotar contra el servidor
    private bool tienePuntoLlegada;
    private bool enEmpuje; // evita que dos empujes a la vez se pisen entre sí

    private NavMeshAgent agente;
    private Transform destinoServidor;
    private Servidor scriptServidor;
    private Collider colliderServidor;
    private Rigidbody rb;

    void Start()
    {
        agente = GetComponent<NavMeshAgent>();
        rb = GetComponent<Rigidbody>();

        // Desfasamos el primer recálculo al azar para que todos los enemigos
        // no calculen camino en el mismo frame (evita micro-frenadas en bloque)
        cronometroRecalculoCamino = Random.Range(0f, intervaloRecalculoCamino);

        // Buscamos al servidor por tag (robusto), con fallback al nombre viejo por las dudas
        GameObject servidor = GameObject.FindGameObjectWithTag("Servidor");
        if (servidor == null)
        {
            servidor = GameObject.Find("Cube");
        }

        if (servidor != null)
        {
            destinoServidor = servidor.transform;
            scriptServidor = servidor.GetComponent<Servidor>();
            colliderServidor = servidor.GetComponent<Collider>();
        }
        else
        {
            Debug.LogError("¡No se encontro el Servidor Central!");
        }
    }

    void Update()
    {
        // CONTROL ABSOLUTO: Si el agente está apagado o no está activo en la escena, salimos al toque
        if (destinoServidor == null || agente == null || !agente.enabled || !agente.gameObject.activeInHierarchy)
        {
            return;
        }

        // Si el carve de un muro recalculó el NavMesh debajo nuestro, nos caímos del
        // NavMesh: nos reubicamos en el punto válido más cercano en vez de congelarnos
        if (!agente.isOnNavMesh)
        {
            NavMeshHit hitNavMesh;
            if (NavMesh.SamplePosition(transform.position, out hitNavMesh, 3f, NavMesh.AllAreas))
            {
                agente.Warp(hitNavMesh.position);
                tieneDestinoPedido = false;
            }
            return;
        }

        ActualizarEstadoCamino();

        // ¿Ya estamos pegados al servidor? Explotamos contra él sin depender del
        // trigger físico: el agujero del NavMesh bajo el Cube nos deja a ~1 unidad
        // del borde y el collider del enemigo puede no llegar a tocarlo nunca
        Vector3 puntoServidor = colliderServidor != null
            ? colliderServidor.ClosestPoint(transform.position)
            : destinoServidor.position;
        Vector3 distanciaAlServidor = puntoServidor - transform.position;
        distanciaAlServidor.y = 0f;

        // ¿Tenemos línea de visión libre al servidor (sin muros en medio)?
        bool visionLibre = HayLineaDeVisionAlServidor(puntoServidor);

        // Solo explotamos si estamos en rango Y hay línea de visión libre: si hay un muro
        // cruzando entre nosotros y el servidor no podemos atravesarlo.
        if (distanciaAlServidor.magnitude <= rangoAtaqueServidor && visionLibre)
        {
            if (scriptServidor != null)
            {
                scriptServidor.RecibirDanio(danioAlServidor);
            }
            Destroy(gameObject);
            return;
        }

        // Estamos en rango del servidor pero un muro nos tapa la visión: no sirve seguir
        // acercándonos (ya estamos pegados y no podemos explotar). Forzamos el modo "romper
        // muro" para tirar abajo el que nos separa, aunque el NavMesh crea que hay camino.
        bool bloqueadoPorMuroCercano =
            distanciaAlServidor.magnitude <= rangoAtaqueServidor && !visionLibre;

        if (!caminoBloqueado && !bloqueadoPorMuroCercano)
        {
            // Camino libre: prioridad total al servidor, soltamos cualquier muro.
            // Vamos al punto de ataque alcanzable que encontró el chequeo de camino
            // (el centro exacto del servidor está sobre un agujero del NavMesh)
            agente.isStopped = false;
            muroObjetivo = null;
            colliderMuroObjetivo = null;
            cronometroAtaqueMuro = 0f;
            PedirDestino(tienePuntoLlegada ? puntoLlegadaServidor : destinoServidor.position);
            return;
        }

        // Camino bloqueado: buscamos un muro para romper. Si la búsqueda anterior
        // falló, esperamos un cooldown antes de reintentar: la cascada completa
        // (OverlapSphere + RaycastAll + recorrer la escena) es cara para cada frame
        cronometroBusquedaMuro -= Time.deltaTime;
        if (muroObjetivo == null && cronometroBusquedaMuro <= 0f)
        {
            cronometroBusquedaMuro = 0.5f;
            colliderMuroObjetivo = null;
            muroObjetivo = BuscarMuroObjetivo(transform.position);

            if (muroObjetivo == null)
            {
                // El carve del NavMeshObstacle puede dejar el borde del NavMesh lejos
                // del muro: tiramos un rayo directo hacia el servidor para encontrar
                // el muro que nos está bloqueando la línea
                muroObjetivo = BuscarMuroBloqueante();
            }

            if (muroObjetivo == null)
            {
                // Último recurso: atacamos el muro más cercano de toda la escena
                // para nunca quedar atascados sin objetivo
                muroObjetivo = BuscarMuroMasCercanoEnEscena();
            }

            if (muroObjetivo != null)
            {
                colliderMuroObjetivo = muroObjetivo.GetComponent<Collider>();
            }
        }

        if (muroObjetivo == null)
        {
            // No hay ningún muro en la escena pero el camino sigue bloqueado:
            // avanzamos igual hacia el servidor todo lo que el NavMesh permita
            agente.isStopped = false;
            PedirDestino(destinoServidor.position);
            return;
        }

        // Medimos contra el punto del collider más cercano a nosotros (no el centro),
        // porque los muros son anchos y el centro puede quedar fuera de rango
        Vector3 puntoMuro = colliderMuroObjetivo != null
            ? colliderMuroObjetivo.ClosestPoint(transform.position)
            : muroObjetivo.transform.position;

        Vector3 distanciaPlana = puntoMuro - transform.position;
        distanciaPlana.y = 0f;

        if (distanciaPlana.magnitude <= rangoAtaqueMuro)
        {
            // En rango: frenamos y golpeamos
            agente.isStopped = true;
            tieneDestinoPedido = false;
            AtacarMuros();
            return;
        }

        cronometroAtaqueMuro = 0f;

        // Dejamos que el NavMesh nos lleve hacia el muro: aunque el punto exacto esté
        // dentro del agujero del carve, el agente camina hasta el borde más cercano
        agente.isStopped = false;
        PedirDestino(puntoMuro);
    }

    // Recalcula (cada cierto intervalo, desfasado por enemigo) si existe camino
    // completo al servidor. Para pasar a "bloqueado" exigimos 2 chequeos seguidos,
    // porque al colocar/romper muros el NavMesh parpadea un frame y no queremos
    // que el enemigo se distraiga con muros teniendo camino libre. Para volver a
    // "libre" alcanza con 1 chequeo: apenas se abre un hueco, vamos al servidor.
    private void ActualizarEstadoCamino()
    {
        cronometroRecalculoCamino -= Time.deltaTime;
        if (cronometroRecalculoCamino > 0f)
        {
            return;
        }
        cronometroRecalculoCamino = intervaloRecalculoCamino;

        // Criterio definitivo, sin umbrales mágicos: hay camino libre si existe
        // algún PUNTO DE ATAQUE alcanzable. Un punto de ataque es un lugar del
        // NavMesh alrededor del servidor desde el cual podemos explotar contra él
        // (a rangoAtaqueServidor del collider o menos). Probamos 8 posiciones
        // alrededor del Cube, empezando por el lado desde el que venimos, y
        // exigimos camino COMPLETO hasta ellas. Esto funciona aunque los carve
        // de muros pegados al servidor agranden el agujero del NavMesh y sin
        // importar por qué lado esté la entrada.
        bool bloqueadoAhora = !ExistePuntoDeAtaqueAlcanzable();

        if (!bloqueadoAhora)
        {
            chequeosBloqueadoSeguidos = 0;
            caminoBloqueado = false;
            return;
        }

        chequeosBloqueadoSeguidos++;
        if (chequeosBloqueadoSeguidos >= 2)
        {
            caminoBloqueado = true;
        }
    }

    // Busca alrededor del servidor un punto del NavMesh alcanzable con camino
    // completo desde el cual podamos explotar. Si lo encuentra, lo guarda como
    // destino de llegada (así caminamos a un lugar útil y no al borde de un agujero)
    private bool ExistePuntoDeAtaqueAlcanzable()
    {
        // Radio del anillo de candidatos: borde del servidor + parte del rango
        // de ataque, para que los puntos caigan en zona de explosión
        float radioServidor = 0.8f;
        if (colliderServidor != null)
        {
            Vector3 extension = colliderServidor.bounds.extents;
            radioServidor = Mathf.Max(extension.x, extension.z);
        }
        float radioAnillo = radioServidor + rangoAtaqueServidor * 0.6f;

        // Arrancamos probando por el lado del servidor que nos queda más cerca
        Vector3 haciaNosotros = transform.position - destinoServidor.position;
        haciaNosotros.y = 0f;
        float anguloInicial = haciaNosotros.sqrMagnitude > 0.0001f
            ? Mathf.Atan2(haciaNosotros.z, haciaNosotros.x)
            : 0f;

        NavMeshPath camino = new NavMeshPath();

        for (int i = 0; i < 8; i++)
        {
            // 0°, ±45°, ±90°, ±135°, 180° respecto de nuestro lado
            float desvio = (i + 1) / 2 * 45f * Mathf.Deg2Rad * ((i % 2 == 0) ? 1f : -1f);
            float angulo = anguloInicial + desvio;
            Vector3 candidato = destinoServidor.position
                + new Vector3(Mathf.Cos(angulo), 0f, Mathf.Sin(angulo)) * radioAnillo;
            candidato.y = transform.position.y;

            // El candidato tiene que caer sobre el NavMesh...
            NavMeshHit muestra;
            if (!NavMesh.SamplePosition(candidato, out muestra, 1.5f, NavMesh.AllAreas))
            {
                continue;
            }

            // ...quedar dentro del rango de explosión contra el servidor...
            Vector3 puntoServidor = colliderServidor != null
                ? colliderServidor.ClosestPoint(muestra.position)
                : destinoServidor.position;
            Vector3 distanciaAtaque = puntoServidor - muestra.position;
            distanciaAtaque.y = 0f;
            if (distanciaAtaque.magnitude > rangoAtaqueServidor)
            {
                continue;
            }

            // ...y ser alcanzable con camino COMPLETO desde donde estamos
            if (agente.CalculatePath(muestra.position, camino)
                && camino.status == NavMeshPathStatus.PathComplete)
            {
                puntoLlegadaServidor = muestra.position;
                tienePuntoLlegada = true;
                return true;
            }
        }

        tienePuntoLlegada = false;
        return false;
    }

    // SetDestination solo cuando el destino cambió de verdad: pedirlo todos los
    // frames hace que el agente recalcule camino constantemente y se frene a tirones
    private void PedirDestino(Vector3 destino)
    {
        if (tieneDestinoPedido && (destino - ultimoDestinoPedido).sqrMagnitude < 0.25f)
        {
            return;
        }

        // Si SetDestination falla, dejamos tieneDestinoPedido en false para reintentar
        tieneDestinoPedido = agente.SetDestination(destino);
        ultimoDestinoPedido = destino;
    }

    // De los muros que están cerca del punto dado, elige el más cercano al Servidor
    private MuroVida BuscarMuroObjetivo(Vector3 centro)
    {
        Collider[] murosCercanos = Physics.OverlapSphere(centro, rangoDeteccionMuro);

        MuroVida mejorMuro = null;
        float distanciaMasCercanaAlServidor = Mathf.Infinity;

        foreach (Collider col in murosCercanos)
        {
            if (!col.CompareTag("Muro"))
            {
                continue;
            }

            MuroVida vida = col.GetComponent<MuroVida>();
            if (vida == null)
            {
                continue;
            }

            float distanciaAlServidor = Vector3.Distance(col.transform.position, destinoServidor.position);
            if (distanciaAlServidor < distanciaMasCercanaAlServidor)
            {
                distanciaMasCercanaAlServidor = distanciaAlServidor;
                mejorMuro = vida;
            }
        }

        return mejorMuro;
    }

    // ¿Hay línea de visión libre (sin muros) entre el enemigo y el servidor? Tira un rayo
    // hasta el punto del servidor y devuelve false si lo primero que cruza es un muro. Así
    // el enemigo no puede explotar "a través" de un muro que lo separa del servidor.
    private bool HayLineaDeVisionAlServidor(Vector3 puntoServidor)
    {
        // El rayo viaja horizontal a la altura del centro del enemigo (su propia Y, ~1):
        // ahí los muros siempre tienen cuerpo. No elevamos más, porque por encima de ~1.5
        // el muro ya no llega (mide ~1.93 de alto centrado en y=0.5, tope ~1.46) y el rayo
        // pasaría por arriba sin detectarlo, dejando explotar al enemigo a través del muro.
        Vector3 origen = transform.position;
        Vector3 destino = puntoServidor;
        destino.y = origen.y;

        Vector3 direccion = destino - origen;
        float distancia = direccion.magnitude;
        if (distancia < 0.0001f)
        {
            return true;
        }

        // Solo nos importa el muro más cercano en la línea: si el primer impacto relevante
        // es un muro, no hay visión libre
        RaycastHit[] impactos = Physics.RaycastAll(origen, direccion.normalized, distancia);
        foreach (RaycastHit impacto in impactos)
        {
            if (impacto.collider.CompareTag("Muro"))
            {
                return false;
            }
        }

        return true;
    }

    // Tira un rayo desde el enemigo hacia el servidor y devuelve el primer muro
    // que cruza la línea: ese es el que nos está bloqueando el paso
    private MuroVida BuscarMuroBloqueante()
    {
        Vector3 origen = transform.position + Vector3.up * 0.5f;
        Vector3 direccion = destinoServidor.position - transform.position;
        direccion.y = 0f;

        if (direccion.sqrMagnitude < 0.0001f)
        {
            return null;
        }

        RaycastHit[] impactos = Physics.RaycastAll(origen, direccion.normalized, direccion.magnitude);

        MuroVida mejorMuro = null;
        float distanciaMasCercana = Mathf.Infinity;

        foreach (RaycastHit impacto in impactos)
        {
            if (!impacto.collider.CompareTag("Muro"))
            {
                continue;
            }

            MuroVida vida = impacto.collider.GetComponent<MuroVida>();
            if (vida == null)
            {
                continue;
            }

            if (impacto.distance < distanciaMasCercana)
            {
                distanciaMasCercana = impacto.distance;
                mejorMuro = vida;
            }
        }

        return mejorMuro;
    }

    // Último recurso: recorre todos los muros de la escena y elige el más cercano
    private MuroVida BuscarMuroMasCercanoEnEscena()
    {
        MuroVida[] muros = FindObjectsByType<MuroVida>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

        MuroVida mejorMuro = null;
        float distanciaMasCercana = Mathf.Infinity;

        foreach (MuroVida muro in muros)
        {
            float distancia = Vector3.Distance(transform.position, muro.transform.position);
            if (distancia < distanciaMasCercana)
            {
                distanciaMasCercana = distancia;
                mejorMuro = muro;
            }
        }

        return mejorMuro;
    }

    // Golpea al muro objetivo con la cadencia configurada (se llama solo cuando esta en rango)
    private void AtacarMuros()
    {
        cronometroAtaqueMuro += Time.deltaTime;
        if (cronometroAtaqueMuro >= cadenciaAtaqueMuro)
        {
            cronometroAtaqueMuro = 0f;
            muroObjetivo.RecibirDanio(danioAMuro);
        }
    }

    public void EmpujarEnemigo(Vector3 direccionEmpuje, float fuerza)
    {
        // Seguridad: ignoramos empujes nuevos mientras hay uno en curso, para que
        // el segundo no reactive el agente mientras el primero todavía está volando
        if (gameObject.activeInHierarchy && !enEmpuje)
        {
            StartCoroutine(RutinaEmpuje(direccionEmpuje, fuerza));
        }
    }

    private IEnumerator RutinaEmpuje(Vector3 direccion, float fuerza)
    {
        enEmpuje = true;

        // 1. Apagamos el NavMesh para liberar el Rigidbody
        agente.enabled = false;

        // 2. Le damos permiso al Rigidbody para ser físico de verdad
        rb.isKinematic = false;

        // 3. Aplicamos el golpe seco de física
        rb.AddForce(direccion * fuerza, ForceMode.Impulse);

        // 4. Esperamos los 0.2 segundos del viaje en el aire
        yield return new WaitForSeconds(0.2f);

        // 5. FRENO SEGURO: Solo frenamos la velocidad si el cuerpo NO se volvió kinematic solo
        if (!rb.isKinematic)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // 6. Volvemos a hacer al Rigidbody kinematic para que no se caiga ni flote raro
        rb.isKinematic = true;

        // 7. Recién ahora volvemos a encender el NavMeshAgent
        agente.enabled = true;

        // 8. El empuje nos movió de lugar: forzamos a repedir destino en el próximo frame
        tieneDestinoPedido = false;
        enEmpuje = false;
    }
}
