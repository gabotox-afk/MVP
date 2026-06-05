using Unity.VisualScripting;
using UnityEngine;

public class TorretaCadena : MonoBehaviour
{
    public float rango = 8f;
    public float cadencia = 1f;
    public float cronometro;

    public GameObject prefabala;
    public Transform puntoDisparo;

    private Transform EnemigoObjetivo;

    void Update()
    {
        BuscarEnemigoCercano();

        if (EnemigoObjetivo != null)
        {

            Vector3 direccion = EnemigoObjetivo.position - transform.position;
            direccion.y = 0;
            transform.forward = direccion;

            cronometro += Time.deltaTime;
            if (cronometro >= cadencia)
            {
                cronometro = 0f;
                Disparar();
            }
        }
    }


    void BuscarEnemigoCercano()
    {
        GameObject[] enemigos = GameObject.FindGameObjectsWithTag("Enemigo");
        float distanciaCerca = Mathf.Infinity;
        GameObject EnemigoCercano = null;

        foreach(GameObject enemigo in enemigos)
        {
            float distanciaAlEnemigo = Vector3.Distance(transform.position, enemigo.transform.position);
            if (distanciaAlEnemigo < distanciaCerca && distanciaAlEnemigo <= rango)
            {
                distanciaCerca = distanciaAlEnemigo;
                EnemigoCercano = enemigo;
            }
        }
        if (EnemigoCercano != null)
        {
            EnemigoObjetivo = EnemigoCercano.transform;
        }
        else
        {
            EnemigoObjetivo = null;
        }
    }

    void Disparar()
    {

        Vector3 posicionReal = transform.position;


        posicionReal.y += 1f;

        GameObject balaClonada = Instantiate(prefabala, posicionReal, transform.rotation);

        BalaRayo scriptBala = balaClonada.GetComponent<BalaRayo>();
        if (scriptBala != null)
        {
            scriptBala.ConfObj(EnemigoObjetivo);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, rango);
    }
}
