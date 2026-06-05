using System.Collections.Generic;
using UnityEngine;

public class BalaRayo : MonoBehaviour
{
    public float danio = 10f;
    public float velocidad = 15f;
    public float radioRebote = 5f;
    public int maxRebotes = 3;
    public float factorDecaimiento = 0.7f;

    public Transform objetivo;

    public void ConfObj(Transform Nobj)
    {
        objetivo = Nobj;
    }

    void Update()
    {
        if (objetivo == null)
        {
            Destroy(gameObject);
            return;
        }

        Vector3 direccion = (objetivo.position - transform.position).normalized;
        transform.Translate(direccion * velocidad * Time.deltaTime, Space.World);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Enemigo"))
        {
            EnemigoVida vida = other.GetComponent<EnemigoVida>();
            if (vida != null)
            {
                vida.RecibirDanio(danio);
            }

            List<GameObject> yaGolpeados = new List<GameObject> { other.gameObject };
            RealizarRebotes(other.transform.position, danio * factorDecaimiento, maxRebotes, yaGolpeados);

            Destroy(gameObject);
        }
    }

    void RealizarRebotes(Vector3 posicion, float daniActual, int rebotesRestantes, List<GameObject> yaGolpeados)
    {
        if (rebotesRestantes <= 0) return;

        Collider[] cercanos = Physics.OverlapSphere(posicion, radioRebote);
        foreach (Collider col in cercanos)
        {
            if (col.CompareTag("Enemigo") && !yaGolpeados.Contains(col.gameObject))
            {
                EnemigoVida vida = col.GetComponent<EnemigoVida>();
                if (vida != null)
                {
                    vida.RecibirDanio(daniActual);
                }
                yaGolpeados.Add(col.gameObject);
                RealizarRebotes(col.transform.position, daniActual * factorDecaimiento, rebotesRestantes - 1, yaGolpeados);
                return;
            }
        }
    }
}
