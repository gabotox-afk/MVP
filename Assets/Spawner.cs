using UnityEngine;

public class Spawner : MonoBehaviour
{
    public GameObject enemigo;
    public Transform[] puntosS;

    public float tiempoEntreSpawn = 3.0f;
    private float cronometro;

    void Update()
    {
        cronometro += Time.deltaTime;

        if (cronometro > tiempoEntreSpawn)
        {
            Spawnear();
            cronometro = 0;
        }
       
    }

    void Spawnear()
    {
        if (enemigo == null || puntosS.Length == 0)
        {
            Debug.LogWarning("¡Faltan asignar los puntos de spawn o el prefab del enemigo en el Controlador!");
            return;
        }

        int indice = Random.Range(0, puntosS.Length);

        Transform Punto = puntosS[indice];

        Instantiate(enemigo, Punto.position, Punto.rotation);

        Debug.Log("Enemigo generado en: " + Punto.name);
    }
}
