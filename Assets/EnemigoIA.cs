using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class EnemigoIA : MonoBehaviour
{
    private NavMeshAgent agente;
    private Transform destinoServidor;
    private Rigidbody rb;

    void Start()
    {
        agente = GetComponent<NavMeshAgent>();
        rb = GetComponent<Rigidbody>();

        GameObject servidor = GameObject.Find("Cube");

        if (servidor != null)
        {
            destinoServidor = servidor.transform;
        }
        else
        {
            Debug.LogError("¡No se encontro el Servidor Central!");
        }
    }

    void Update()
    {
        // CONTROL ABSOLUTO: Si el agente está apagado, si no está activo en la escena, 
        // o si todavía no se paró arriba del NavMesh, SALIMOS al toque.
        if (destinoServidor == null || agente == null || !agente.enabled || !agente.gameObject.activeInHierarchy || !agente.isOnNavMesh)
        {
            return;
        }

        // Solo si pasa todas las pruebas anteriores, le damos el destino
        agente.SetDestination(destinoServidor.position);
    }

    public void EmpujarEnemigo(Vector3 direccionEmpuje, float fuerza)
    {
        // Seguridad: Si el agente ya estaba apagado (porque le pegaste dos veces seguidas), evitamos acumular errores
        if (gameObject.activeInHierarchy)
        {
            StartCoroutine(RutinaEmpuje(direccionEmpuje, fuerza));
        }
    }

    private IEnumerator RutinaEmpuje(Vector3 direccion, float fuerza)
    {
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
    }
}