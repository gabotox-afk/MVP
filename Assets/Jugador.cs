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

    void Update()
    {
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

            // 3. Movemos al jugador en esa dirección relativa al mundo
            transform.Translate(direccionMovimiento * velocidad * Time.deltaTime, Space.World);

            // 4. Hacemos que gire de forma suave hacia la dirección en la que camina
            Quaternion rotacionObjetivo = Quaternion.LookRotation(direccionMovimiento);
            transform.rotation = Quaternion.Slerp(transform.rotation, rotacionObjetivo, Time.deltaTime * suavidadRotacion);
        }
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
        Debug.Log("Objeto construido: " + prefab.name);
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