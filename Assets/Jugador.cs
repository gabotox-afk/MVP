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

    private CapsuleCollider miCollider;

    void Start()
    {
        miCollider = GetComponent<CapsuleCollider>();

        // Si nadie arrastró la cámara en el Inspector, usamos la principal de la escena
        if (camaraPrincipal == null && Camera.main != null)
        {
            camaraPrincipal = Camera.main.transform;
        }

        AplicarPersonajeElegido();
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

        Vector3 posicionSpawn = transform.position + transform.forward * distanciaConstruccion;
        posicionSpawn.y = 0.5f;

        Instantiate(prefab, posicionSpawn, transform.rotation);
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