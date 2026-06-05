using UnityEngine;

public class BalaEspecial : MonoBehaviour
{
    public float danio = 10f;
    public float velocidad = 15f;
    public float radioExplosion = 3f;
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
            Explotar();
        }
    }

    void Explotar()
    {
        Collider[] enemigosEnArea = Physics.OverlapSphere(transform.position, radioExplosion);
        foreach (Collider col in enemigosEnArea)
        {
            if (col.CompareTag("Enemigo"))
            {
                EnemigoVida vida = col.GetComponent<EnemigoVida>();
                if (vida != null)
                {
                    vida.RecibirDanio(danio);
                }
            }
        }
        Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, radioExplosion);
    }
}
