using UnityEngine;
using System.Collections.Generic;

public class Jugador : MonoBehaviour
{
    [Header("Configuracion de Movimiento")]
    public float velocidad = 5.0f;
    public float suavidadRotacion = 10f; // Nueva variable para que gire suave

    [Header("Referencias de Camara")]
    public Transform camaraPrincipal; // <-- Nueva variable para arrastrar la Main Camera

    [Header("Configuracion de Combate y Construccion")]
    public float fuerzaDeEmpuje = 15f;
    public float radioDeGolpe = 3f;
    public float anguloConoEmpuje = 90f; // apertura total del cono frontal del empuje
    public float distanciaConstruccion = 2.0f;

    [Header("Prefabs")]
    public GameObject prefabMuro;
    public GameObject prefabTorreta;
    public GameObject prefabTorretaE;

    [Header("Torretas especiales por personaje")]
    public GameObject prefabTorretaAOE;    // Mago
    public GameObject prefabTorretaCadena; // Cobarius
    public GameObject prefabTorretaNinja;  // Ninja

    [Header("Modelo visual del personaje")]
    // El modelo de cada personaje se carga por nombre desde Resources/Personajes/<nombre>
    // (ver SeleccionPersonaje.DatosPersonaje.CargarModelo). Cada prefab trae su propio
    // Animator con sus animaciones y se instancia como hijo del jugador.
    // Multiplicador final sobre el ajuste automático de tamaño, por si querés que el
    // modelo sobresalga un poco de la cápsula o quede más bajo (1 = misma altura exacta).
    public float factorAlturaModelo = 1f;

    // Empuje vertical manual extra sobre el alineado automático al piso
    // (0 = base exacta en el piso; + sube el modelo, − lo hunde).
    public float offsetAlturaModelo = 0f;

    // Limite de cuantas estructuras de un tipo puede haber vivas a la vez.
    // Se cuenta por tag: cada prefab construible debe tener su tag asignado
    // en el Inspector (Muro, Torreta, TorretaAOE, etc.)
    [System.Serializable]
    public class LimiteEstructura
    {
        public string tag = "Muro"; // Tag del prefab a limitar
        public int maximo = 3;      // Cuantos puede haber vivos a la vez
    }

    [Header("Limites de construccion (por tag)")]
    public LimiteEstructura[] limites;

    [Header("Cooldown de torretas")]
    public float cooldownTorreta = 5f; // segundos entre colocaciones del mismo tipo

    // Por cada tag de torreta, el instante (Time.time) en que se libera su cooldown
    private readonly Dictionary<string, float> proximaColocacion = new Dictionary<string, float>();

    [Header("Configuracion de Salto")]
    public float fuerzaSalto = 7f;
    [SerializeField] private float gravedad = -20f;
    private float velocidadVertical = 0f;
    private bool estaEnElSuelo = true;

    private CapsuleCollider miCollider;
    private HUDJuego hud;
    private Animator animator;

    void Start()
    {
        miCollider = GetComponent<CapsuleCollider>();
        hud = FindAnyObjectByType<HUDJuego>();

        // Si nadie arrastró la cámara en el Inspector, usamos la principal de la escena
        if (camaraPrincipal == null && Camera.main != null)
        {
            camaraPrincipal = Camera.main.transform;
        }

        // Primero resolvemos qué torreta especial usa este personaje...
        AplicarPersonajeElegido();
        // ...para descartar del HUD las especiales de los otros personajes...
        FiltrarLimitesDelPersonaje();
        // ...y recién ahí mostrar los contadores que de verdad va a usar.
        InicializarContadores();
    }

    // Deja en la lista de limites solo las torretas especiales que este personaje
    // puede construir: la suya (el tag de prefabTorretaE) y los tipos no-especiales
    // (muro, torreta normal). Asi el HUD no muestra contadores de torretas ajenas.
    void FiltrarLimitesDelPersonaje()
    {
        if (limites == null)
        {
            return;
        }

        // Tags de torretas especiales: solo una sobrevive, la del personaje
        string[] tagsEspeciales = { "TorretaAOE", "TorretaCadena", "TorretaNinja" };
        string tagMiEspecial = prefabTorretaE != null ? prefabTorretaE.tag : "";

        var filtrados = new System.Collections.Generic.List<LimiteEstructura>();
        foreach (LimiteEstructura limite in limites)
        {
            if (limite == null)
            {
                continue;
            }

            bool esEspecial = System.Array.IndexOf(tagsEspeciales, limite.tag) >= 0;

            // Las no-especiales siempre pasan; las especiales solo si son la del personaje
            if (!esEspecial || limite.tag == tagMiEspecial)
            {
                filtrados.Add(limite);
            }
        }

        limites = filtrados.ToArray();
    }

    // La torreta especial (tecla 3) y el color del jugador dependen del personaje
    // elegido en el menú: Mago→AOE, Cobarius→Cadena, Ninja→Ninja
    void AplicarPersonajeElegido()
    {
        int indice = SeleccionPersonaje.IndiceElegido;

        GameObject prefabEspecial = null;
        if (indice == SeleccionPersonaje.Mago) prefabEspecial = prefabTorretaAOE;
        if (indice == SeleccionPersonaje.Cobarius) prefabEspecial = prefabTorretaCadena;
        if (indice == SeleccionPersonaje.Ninja) prefabEspecial = prefabTorretaNinja;

        // Si el prefab del personaje está asignado, reemplaza a la torreta especial
        // genérica; si no, se mantiene la del Inspector como antes
        if (prefabEspecial != null)
        {
            prefabTorretaE = prefabEspecial;
        }

        // Teñimos al jugador con el color del personaje para identificarlo
        Renderer render = GetComponent<Renderer>();
        if (render != null)
        {
            render.material.color = SeleccionPersonaje.Elegido.color;
        }

        InstanciarModeloPersonaje(indice);
    }

    // Crea el modelo visual (con su Animator y animaciones) del personaje elegido
    // como hijo del jugador, y cachea su Animator para mover las animaciones.
    void InstanciarModeloPersonaje(int indice)
    {
        if (indice < 0 || indice >= SeleccionPersonaje.Personajes.Length)
        {
            // Índice fuera de rango: dejamos lo que ya haya de hijo en escena
            animator = GetComponentInChildren<Animator>();
            return;
        }

        // El personaje resuelve su propio prefab por nombre (Resources/Personajes/<nombre>)
        GameObject prefab = SeleccionPersonaje.Personajes[indice].CargarModelo();
        if (prefab == null)
        {
            // Sin prefab para este personaje: dejamos lo que ya haya de hijo en escena
            animator = GetComponentInChildren<Animator>();
            return;
        }

        // El modelo se ancla al jugador en su posición local cero, mirando al frente.
        GameObject modelo = Instantiate(prefab, transform);
        modelo.transform.localPosition = Vector3.zero;
        modelo.transform.localRotation = Quaternion.identity;

        // Ajustamos el tamaño del modelo a la altura de la cápsula del jugador, así
        // no dependemos de la escala con la que venga el prefab (los FBX de 3ds Max
        // traen escalas raras). Si algo falla, dejamos la escala original del prefab.
        AjustarEscalaModelo(modelo);

        // El Animator que controlan las animaciones es el del modelo recién creado.
        // El modelo puede traer varios Animator anidados (FBX de Max): nos quedamos
        // con el que tenga un Controller asignado, que es el que de verdad anima.
        animator = BuscarAnimatorConController(modelo);
    }

    // Devuelve el primer Animator del modelo que tenga un runtimeAnimatorController
    // asignado. Si ninguno lo tiene, cae al primero que encuentre.
    Animator BuscarAnimatorConController(GameObject modelo)
    {
        Animator[] animators = modelo.GetComponentsInChildren<Animator>();
        foreach (Animator a in animators)
        {
            if (a.runtimeAnimatorController != null)
            {
                return a;
            }
        }
        return animators.Length > 0 ? animators[0] : null;
    }

    // Escala el modelo para que su altura iguale (por factorAlturaModelo) la altura de la
    // cápsula del jugador y luego asienta su base en el piso. Mide el modelo con los bounds
    // de TODOS sus renderers (la malla es skinned, un solo nodo no alcanza para medir bien).
    void AjustarEscalaModelo(GameObject modelo)
    {
        if (miCollider == null)
        {
            return;
        }

        Renderer[] renderers = modelo.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
        {
            return;
        }

        float alturaModelo = BoundsDelModelo(renderers).size.y;
        if (alturaModelo <= 0.0001f)
        {
            return;
        }

        // La cápsula da la altura objetivo (height ya viene en escala de mundo del jugador)
        float alturaObjetivo = miCollider.height * factorAlturaModelo;

        float factor = alturaObjetivo / alturaModelo;
        modelo.transform.localScale *= factor;

        // Tras escalar, el pivot del modelo sigue anclado al centro del jugador, así que
        // los pies suelen quedar flotando. Bajamos el modelo para que su base (bounds.min.y,
        // ya en la escala nueva) coincida con el piso de la cápsula. Se mide por bounds, no
        // por el pivot del prefab, así funciona con cualquier modelo.
        float pisoY = transform.TransformPoint(miCollider.center).y
            - miCollider.height * 0.5f * transform.lossyScale.y;
        float baseModeloY = BoundsDelModelo(renderers).min.y;
        float delta = pisoY - baseModeloY + offsetAlturaModelo;
        modelo.transform.position += Vector3.up * delta;
    }

    // Bounds combinados (en mundo) de todos los renderers del modelo en su escala actual.
    Bounds BoundsDelModelo(Renderer[] renderers)
    {
        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }
        return bounds;
    }

    void Update()
    {
        // Con el juego pausado (menú de pausa abierto) no procesamos input:
        // si no, se podría construir o empujar con el tiempo congelado
        if (Time.timeScale == 0f)
        {
            return;
        }

        MoverJugador();

        if (Input.GetKeyDown(KeyCode.Alpha1)) // Tecla 1
        {
            ConstruirObjeto(prefabMuro);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2)) // Tecla 2
        {
            ConstruirObjeto(prefabTorreta);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3)) // Tecla 3
        {
            ConstruirObjeto(prefabTorretaE);
        }

        if (Input.GetMouseButtonDown(0))
        {
            if (animator != null)
            {
                animator.SetTrigger("Empujar");
            }
            EmpujarEstructurasAdelante();
        }
    }

    // Dispara el trigger de un baile en el Animator del modelo. Lo usa la ruleta de
    // bailes (MenuBailes): el menú elige el trigger y acá lo lanzamos. Si el modelo no
    // tiene Animator no hace nada.
    public void Bailar(string triggerBaile)
    {
        if (animator != null && !string.IsNullOrEmpty(triggerBaile))
        {
            animator.SetTrigger(triggerBaile);
        }
    }

    void MoverJugador()
    {
        AplicarFisicaVertical();

        // Sin cámara no podemos calcular la dirección relativa: salimos sin crashear
        if (camaraPrincipal == null)
        {
            return;
        }

        // 1. Captura las flechas del teclado o las teclas WASD de forma limpia
        float movimientoH = Input.GetAxisRaw("Horizontal");
        float movimientoV = Input.GetAxisRaw("Vertical");

        Vector3 direccionInput = new Vector3(movimientoH, 0.0f, movimientoV).normalized;

        // Avisamos al Animator cuánto nos movemos (0 = quieto, 1 = caminando) para
        // que mezcle entre idle y caminar. Usamos el input crudo, no la posición:
        // así la animación de caminar sigue aunque un muro nos frene de verdad.
        if (animator != null)
        {
            animator.SetFloat("Velocidad", direccionInput.magnitude);
        }

        // 2. Si el jugador está presionando alguna tecla...
        if (direccionInput.magnitude >= 0.1f)
        {
            // Buscamos hacia dónde está apuntando el frente y el costado de la cámara
            Vector3 camaraFrente = camaraPrincipal.forward;
            Vector3 camaraCostado = camaraPrincipal.right;

            // Aplanamos el eje Y para que no camine lento si mirás al cielo o al suelo
            camaraFrente.y = 0f;
            camaraCostado.y = 0f;
            camaraFrente.Normalize();
            camaraCostado.Normalize();

            // Calculamos la dirección de movimiento relativa a la cámara
            Vector3 direccionMovimiento = (camaraFrente * movimientoV + camaraCostado * movimientoH).normalized;

            // 3. Movemos al jugador en esa dirección, respetando muros y demás colliders
            MoverConColisiones(direccionMovimiento, velocidad * Time.deltaTime);

            // 4. Hacemos que gire de forma suave hacia la dirección en la que camina
            Quaternion rotacionObjetivo = Quaternion.LookRotation(direccionMovimiento);
            transform.rotation = Quaternion.Slerp(transform.rotation, rotacionObjetivo, Time.deltaTime * suavidadRotacion);
        }
    }

    // Translate ignora la física, así que antes de movernos tiramos un CapsuleCast:
    // si hay algo sólido adelante, intentamos deslizarnos a lo largo de la pared,
    // y si tampoco se puede, no nos movemos
    void MoverConColisiones(Vector3 direccion, float distancia)
    {
        if (MovimientoLibre(direccion, distancia))
        {
            transform.Translate(direccion * distancia, Space.World);
            return;
        }

        // Probamos deslizarnos: proyectamos la dirección contra la pared que nos frena
        RaycastHit pared;
        if (LanzarCapsula(direccion, distancia, out pared))
        {
            Vector3 deslizamiento = Vector3.ProjectOnPlane(direccion, pared.normal);
            deslizamiento.y = 0f;

            if (deslizamiento.sqrMagnitude > 0.0001f)
            {
                deslizamiento.Normalize();
                if (MovimientoLibre(deslizamiento, distancia))
                {
                    transform.Translate(deslizamiento * distancia, Space.World);
                }
            }
        }
    }

    bool MovimientoLibre(Vector3 direccion, float distancia)
    {
        RaycastHit hit;
        return !LanzarCapsula(direccion, distancia, out hit);
    }

    bool LanzarCapsula(Vector3 direccion, float distancia, out RaycastHit hit)
    {
        hit = default(RaycastHit);

        // Sin collider propio no podemos chequear nada: dejamos pasar el movimiento
        if (miCollider == null)
        {
            return false;
        }

        Vector3 centro = transform.TransformPoint(miCollider.center);
        float mitadTronco = Mathf.Max(0f, miCollider.height * 0.5f - miCollider.radius);
        Vector3 puntoArriba = centro + Vector3.up * mitadTronco;
        Vector3 puntoAbajo = centro - Vector3.up * mitadTronco;

        // Radio apenas menor al real para no chocar con el piso por error de redondeo
        RaycastHit[] impactos = Physics.CapsuleCastAll(puntoArriba, puntoAbajo, miCollider.radius * 0.95f,
            direccion, distancia, ~0, QueryTriggerInteraction.Ignore);

        foreach (RaycastHit impacto in impactos)
        {
            if (impacto.collider == miCollider)
            {
                continue;
            }

            // Si estamos en el aire y los pies ya superan el tope del obstáculo,
            // lo ignoramos: el salto lo pasa por arriba
            if (!estaEnElSuelo)
            {
                float pie = centro.y - miCollider.height * 0.5f;
                if (pie > impacto.collider.bounds.max.y - 0.05f)
                    continue;
            }

            hit = impacto;
            return true;
        }

        return false;
    }

    void ConstruirObjeto(GameObject prefab)
    {
        if (prefab == null)
        {
            Debug.LogWarning("¡Falta asignar el Prefab en el Inspector del Jugador!");
            return;
        }

        bool esTorreta = prefab.tag.StartsWith("Torreta");

        if (esTorreta)
        {
            // Las torretas no tienen limite de cantidad, pero si cooldown por tipo:
            // tras colocar una de un tipo hay que esperar para volver a colocar ese tipo
            if (EnCooldown(prefab.tag))
            {
                if (hud != null)
                {
                    float restante = proximaColocacion[prefab.tag] - Time.time;
                    hud.AvisarCooldown(prefab.tag, restante);
                }
                return;
            }
        }
        else
        {
            // Estructuras no-torreta (muros): mantienen el limite de cantidad por tag
            LimiteEstructura limite = BuscarLimite(prefab.tag);
            if (limite != null && ContarVivos(prefab.tag) >= limite.maximo)
            {
                if (hud != null)
                {
                    hud.AvisarLimite(prefab.tag, limite.maximo);
                }
                return;
            }
        }

        Vector3 posicionSpawn = transform.position + transform.forward * distanciaConstruccion;
        posicionSpawn.y = 0.5f;

        Instantiate(prefab, posicionSpawn, transform.rotation);

        if (esTorreta)
        {
            // Arrancamos el cooldown de este tipo de torreta
            proximaColocacion[prefab.tag] = Time.time + cooldownTorreta;
        }
        else
        {
            // Refrescamos el contador del HUD con el nuevo total (solo estructuras con limite)
            LimiteEstructura limite = BuscarLimite(prefab.tag);
            if (hud != null && limite != null)
            {
                hud.ActualizarContador(prefab.tag, ContarVivos(prefab.tag), limite.maximo);
            }
        }
    }

    // True si el tag de torreta todavia esta esperando su cooldown
    bool EnCooldown(string tag)
    {
        return proximaColocacion.TryGetValue(tag, out float liberacion) && Time.time < liberacion;
    }

    // Muestra todos los contadores en el HUD al empezar (en su valor actual)
    void InicializarContadores()
    {
        if (hud == null || limites == null)
        {
            return;
        }

        foreach (LimiteEstructura limite in limites)
        {
            if (limite != null)
            {
                hud.ActualizarContador(limite.tag, ContarVivos(limite.tag), limite.maximo);
            }
        }
    }

    // Devuelve la regla de limite para un tag, o null si ese tag no esta limitado
    LimiteEstructura BuscarLimite(string tag)
    {
        if (limites == null)
        {
            return null;
        }

        foreach (LimiteEstructura limite in limites)
        {
            if (limite != null && limite.tag == tag)
            {
                return limite;
            }
        }

        return null;
    }

    // Cuenta cuantos objetos vivos hay con ese tag. Como las torretas temporales
    // se autodestruyen, su tag desaparece del conteo solo: el slot se libera sin
    // que tengamos que notificar nada.
    int ContarVivos(string tag)
    {
        return GameObject.FindGameObjectsWithTag(tag).Length;
    }

    void AplicarFisicaVertical()
    {
        // Si estábamos parados pero ya no hay suelo (caída de borde), empezamos a caer
        if (estaEnElSuelo && DistanciaAlSuelo() > 0.2f)
        {
            estaEnElSuelo = false;
        }

        if (Input.GetKeyDown(KeyCode.Space) && estaEnElSuelo)
        {
            velocidadVertical = fuerzaSalto;
            estaEnElSuelo = false;
            SetAnimatorTriggerSafe("Saltar");
        }

        if (!estaEnElSuelo)
        {
            velocidadVertical += gravedad * Time.deltaTime;
            float deltaY = velocidadVertical * Time.deltaTime;

            if (velocidadVertical < 0f)
            {
                float dist = DistanciaAlSuelo();
                if (dist != float.MaxValue && dist <= Mathf.Abs(deltaY) + 0.1f)
                {
                    transform.position += Vector3.down * Mathf.Max(0f, dist);
                    velocidadVertical = 0f;
                    estaEnElSuelo = true;
                    SetAnimatorBoolSafe("EnElAire", false);
                    return;
                }
            }

            transform.Translate(Vector3.up * deltaY, Space.World);
            SetAnimatorBoolSafe("EnElAire", true);
        }
    }

    float DistanciaAlSuelo()
    {
        if (miCollider == null) return float.MaxValue;
        Vector3 centro = transform.TransformPoint(miCollider.center);
        float mitad = miCollider.height * 0.5f;

        RaycastHit[] hits = Physics.RaycastAll(centro, Vector3.down, mitad + 0.5f, ~0, QueryTriggerInteraction.Ignore);
        float min = float.MaxValue;
        foreach (RaycastHit hit in hits)
            if (hit.collider != miCollider && hit.distance < min)
                min = hit.distance;

        return min == float.MaxValue ? float.MaxValue : min - mitad;
    }

    void SetAnimatorTriggerSafe(string nombre)
    {
        if (animator == null) return;
        foreach (AnimatorControllerParameter p in animator.parameters)
            if (p.name == nombre && p.type == AnimatorControllerParameterType.Trigger)
            { animator.SetTrigger(nombre); return; }
    }

    void SetAnimatorBoolSafe(string nombre, bool valor)
    {
        if (animator == null) return;
        foreach (AnimatorControllerParameter p in animator.parameters)
            if (p.name == nombre && p.type == AnimatorControllerParameterType.Bool)
            { animator.SetBool(nombre, valor); return; }
    }

    void EmpujarEstructurasAdelante()
    {
        Vector3 puntoGolpe = transform.position + transform.forward * 1.5f;
        Collider[] objetosGolpeados = Physics.OverlapSphere(puntoGolpe, radioDeGolpe);

        foreach (Collider col in objetosGolpeados)
        {
            if (col.CompareTag("Enemigo"))
            {
                EnemigoIA enemigo = col.GetComponent<EnemigoIA>();
                if (enemigo != null)
                {
                    // Solo empujamos lo que esta en el cono frontal (descartamos
                    // costados extremos y espalda)
                    Vector3 haciaEnemigo = col.transform.position - transform.position;
                    haciaEnemigo.y = 0f;
                    if (Vector3.Angle(transform.forward, haciaEnemigo) > anguloConoEmpuje * 0.5f)
                    {
                        continue;
                    }

                    // Direccion radial original: cada enemigo sale alejandose de
                    // nuestro centro (el empuje en abanico que ya nos gustaba)
                    Vector3 direccion = (col.transform.position - transform.position).normalized;
                    direccion.y = 0.1f;

                    enemigo.EmpujarEnemigo(direccion, fuerzaDeEmpuje);
                }
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Vector3 puntoGolpe = transform.position + transform.forward * 1.5f;
        Gizmos.DrawWireSphere(puntoGolpe, radioDeGolpe);
    }
}