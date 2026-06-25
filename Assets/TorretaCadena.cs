using UnityEngine;

public class TorretaCadena : MonoBehaviour
{
    public float rango = 8f;
    public float cadencia = 1f;
    public float cronometro;
    public float intervaloBusqueda = 0.25f; // cada cuánto re-buscamos objetivo (recorrer todos los enemigos es caro)

    public GameObject prefabala;
    public Transform puntoDisparo;

    private Transform EnemigoObjetivo;
    private float cronometroBusqueda;

    void Start()
    {
        // Desfasamos la primera búsqueda al azar para que todas las torretas
        // no recorran la lista de enemigos en el mismo frame
        cronometroBusqueda = Random.Range(0f, intervaloBusqueda);
    }

    void Update()
    {
        // Solo re-buscamos cada cierto intervalo, o enseguida si el objetivo
        // actual murió o se fue del rango
        cronometroBusqueda -= Time.deltaTime;
        if (cronometroBusqueda <= 0f || !ObjetivoSigueValido())
        {
            cronometroBusqueda = intervaloBusqueda;
            BuscarEnemigoCercano();
        }

        if (EnemigoObjetivo != null)
        {
            Vector3 direccion = EnemigoObjetivo.position - transform.position;
            direccion.y = 0;
            if (direccion.sqrMagnitude > 0.0001f)
            {
                transform.forward = direccion;
            }

            cronometro += Time.deltaTime;
            if (cronometro >= cadencia)
            {
                cronometro = 0f;
                Disparar();
            }
        }
    }

    bool ObjetivoSigueValido()
    {
        return EnemigoObjetivo != null
            && Vector3.Distance(transform.position, EnemigoObjetivo.position) <= rango;
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
        // Disparamos desde el punto de disparo si está asignado; si no, desde
        // arriba de la torreta como antes
        Vector3 posicionReal = puntoDisparo != null
            ? puntoDisparo.position
            : transform.position + Vector3.up;

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
