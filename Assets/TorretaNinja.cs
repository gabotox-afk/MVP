using System.Collections.Generic;
using UnityEngine;

public class TorretaNinja : MonoBehaviour
{
    public float rango = 3f;
    public float danioPorSegundo = 15f;

    private List<EnemigoVida> enemigosEnRango = new List<EnemigoVida>();

    void Start()
    {
        // Ajustamos dinámicamente el tamaño del Collider para que coincida con el rango melee
        SphereCollider miCollider = GetComponent<SphereCollider>();
        if (miCollider != null)
        {
            miCollider.isTrigger = true;
            miCollider.radius = rango;
        }
    }

    void Update()
    {
        // Limpiamos la lista por si algún enemigo murió mientras estaba adentro
        enemigosEnRango.RemoveAll(item => item == null);

        // Le hacemos daño a TODOS los enemigos que estén adentro de la tormenta de espadas
        foreach (EnemigoVida enemigo in enemigosEnRango)
        {
            // Dividimos el daño por el tiempo del fotograma para que sea daño continuo por segundo
            enemigo.RecibirDanio(danioPorSegundo * Time.deltaTime);
        }

        // Toque visual: Hacemos que la torreta gire sobre su propio eje como loca simulando las espadas
        transform.Rotate(Vector3.up * 500f * Time.deltaTime);
    }

    // Cuando un enemigo entra al círculo melee
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Enemigo"))
        {
            EnemigoVida vida = other.GetComponent<EnemigoVida>();
            if (vida != null && !enemigosEnRango.Contains(vida))
            {
                enemigosEnRango.Add(vida);
            }
        }
    }

    // Cuando el enemigo logra pasar de largo y salir del círculo
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Enemigo"))
        {
            EnemigoVida vida = other.GetComponent<EnemigoVida>();
            if (vida != null)
            {
                enemigosEnRango.Remove(vida);
            }
        }
    }

    // Para ver el rango de la tormenta en la ventana Scene
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.black;
        Gizmos.DrawWireSphere(transform.position, rango);
    }
}
