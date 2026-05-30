using UnityEngine;

public class Jugador : MonoBehaviour
{
    public float velocidad = 5.0f;

    public GameObject prefabMuro;
    public GameObject prefabTorreta;
    public GameObject prefabTorretaE;

    public float distanciaConstruccion = 2.0f;

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
    }

    void MoverJugador()
    {
        // Captura las flechas del teclado o las teclas WASD
        float movimientoH = Input.GetAxis("Horizontal");
        float movimientoV = Input.GetAxis("Vertical");

        // Calculamos el vector de movimiento basándonos en los ejes X y Z (suelo 3D)
        Vector3 direccion = new Vector3(movimientoH, 0.0f, movimientoV);

        // Movemos al jugador en esa dirección por el tiempo transcurrido
        transform.Translate(direccion * velocidad * Time.deltaTime, Space.World);

        // Hace que el jugador mire hacia la dirección en la que se mueve
        if (direccion != Vector3.zero)
        {
            transform.forward = direccion;
        }
    }

    void ConstruirObjeto(GameObject prefab)
    {
        // Verificamos que no intentemos instanciar algo vacío
        if (prefab == null)
        {
            Debug.LogWarning("¡Falta asignar el Prefab en el Inspector del Jugador!");
            return;
        }

        // Calculamos la posición adelante del jugador para que no aparezca "adentro" de él
        Vector3 posicionSpawn = transform.position + transform.forward * distanciaConstruccion;

        // Ajustamos la altura (Y) a 0.5f o la altura del suelo para que no flote (ajustable)
        posicionSpawn.y = 0.5f;

        // Creamos el objeto en el mundo
        Instantiate(prefab, posicionSpawn, transform.rotation);

        Debug.Log("Objeto construido: " + prefab.name);
    }
}
