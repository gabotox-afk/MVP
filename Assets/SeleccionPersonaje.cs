using UnityEngine;

// Guarda qué personaje eligió el jugador en el menú y sus datos. Es estático
// para sobrevivir al cambio de escena, y persiste entre partidas con PlayerPrefs.
public static class SeleccionPersonaje
{
    public enum Forma { Capsula, Cubo, Esfera }

    public class DatosPersonaje
    {
        public string nombre;
        public string descripcion;
        public Color color;
        public Forma forma;

        public DatosPersonaje(string nombre, string descripcion, Color color, Forma forma)
        {
            this.nombre = nombre;
            this.descripcion = descripcion;
            this.color = color;
            this.forma = forma;
        }
    }

    public const int Mago = 0;
    public const int Cobarius = 1;
    public const int Ninja = 2;

    public static readonly DatosPersonaje[] Personajes =
    {
        new DatosPersonaje("Mago", "Torreta Ataque + Torreta AOE",
            new Color(0.35f, 0.55f, 0.95f), Forma.Capsula),
        new DatosPersonaje("Cobarius", "Torreta Ataque + Torreta Cadena",
            new Color(0.9f, 0.35f, 0.3f), Forma.Cubo),
        new DatosPersonaje("Ninja", "Torreta Ataque + Torreta Ninja",
            new Color(0.25f, 0.25f, 0.3f), Forma.Esfera),
    };

    private const string ClavePrefs = "PersonajeElegido";

    public static int IndiceElegido
    {
        get
        {
            return Mathf.Clamp(PlayerPrefs.GetInt(ClavePrefs, Mago), 0, Personajes.Length - 1);
        }
        set
        {
            PlayerPrefs.SetInt(ClavePrefs, Mathf.Clamp(value, 0, Personajes.Length - 1));
            PlayerPrefs.Save();
        }
    }

    public static DatosPersonaje Elegido
    {
        get { return Personajes[IndiceElegido]; }
    }
}
