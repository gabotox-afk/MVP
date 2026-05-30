using UnityEngine;
using UnityEngine.AI;

public class EnemigoIA : MonoBehaviour 
{
    private NavMeshAgent agente;
    private Transform destinoServidor;

    void Start()
    {
        // Buscamos el componente NavMeshAgent que tiene esta misma capsula
        agente = GetComponent<NavMeshAgent>();

        // Buscamos al Servidor Central en la escena por su nombre exacto en la Hierarchy
        GameObject servidor = GameObject.Find("Cube"); // <-- Cambialo por "Cube" o el nombre real de tu servidor

        if (servidor != null)
        {
            destinoServidor = servidor.transform;
        }
        else
        {
            Debug.LogError("¡No se encontro el Servidor Central! Asegurate de poner el nombre exacto.");
        }
    }

    void Update()
    {
        // Si encontramos al servidor, le actualizamos constantemente la posicion al agente
        if (destinoServidor != null)
        {
            agente.SetDestination(destinoServidor.position);
        }
    }

}
