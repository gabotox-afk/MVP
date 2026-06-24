using UnityEngine;

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
    public float distanciaConstruccion = 2.0f;

    [Header("Prefabs")]
    public GameObject prefabMuro;
    public GameObject prefabTorreta;
    public GameObject prefabTorretaE;

    [Header("Torretas especiales por personaje")]
    public GameObject prefabTorretaAOE;    // Mago
    public GameObject prefabTorretaCadena; // Cobarius
    public GameObject prefabTorretaNinja;  // Ninja

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

    private CapsuleCollider miCollider;
    private HUDJuego hud;

    void Start()
    {
        miCollider = GetComponent<CapsuleCollider>();
        hud = FindFirstObjectByType<HUDJuego>();

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
            EmpujarEstructurasAdelante();
        }
    }

    void MoverJugador()
    {
        // Sin cámara no podemos calcular la dirección relativa: salimos sin crashear
        if (camaraPrincipal == null)
        {
            return;
        }

        // 1. Captura las flechas del teclado o las teclas WASD de forma limpia
        float movimientoH = Input.GetAxisRaw("Horizontal");
        float movimientoV = Input.GetAxisRaw("Vertical");

        Vector3 direccionInput = new Vector3(movimientoH, 0.0f, movimientoV).normalized;

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

        // Si este tipo tiene un limite y ya esta lleno, no construimos y avisamos
        LimiteEstructura limite = BuscarLimite(prefab.tag);
        if (limite != null && ContarVivos(prefab.tag) >= limite.maximo)
        {
            if (hud != null)
            {
                hud.AvisarLimite(prefab.tag, limite.maximo);
            }
            return;
        }

        Vector3 posicionSpawn = transform.position + transform.forward * distanciaConstruccion;
        posicionSpawn.y = 0.5f;

        Instantiate(prefab, posicionSpawn, transform.rotation);

        // Refrescamos el contador del HUD con el nuevo total
        if (hud != null && limite != null)
        {
            hud.ActualizarContador(prefab.tag, ContarVivos(prefab.tag), limite.maximo);
        }
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