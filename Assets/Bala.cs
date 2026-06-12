using UnityEngine;

public class Bala : MonoBehaviour
{
    public float danio = 10f;
    public float velocidad = 15f;
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
            Destroy(gameObject);
        }
        
    }
}
