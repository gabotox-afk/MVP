using UnityEngine;

// Guarda en qué modo de juego se va a jugar la partida. Es estático para
// sobrevivir al cambio de escena y persiste con PlayerPrefs, igual que
// SeleccionPersonaje. Por ahora solo se usa Campaña; Infinito queda preparado
// para enchufarse desde el menú mas adelante sin tocar el Spawner.
public static class ModoJuego
{
    public enum Modo { Campaña, Infinito }

    private const string ClavePrefs = "ModoJuegoElegido";

    public static Modo Elegido
    {
        get
        {
            int valor = PlayerPrefs.GetInt(ClavePrefs, (int)Modo.Campaña);
            return (Modo)Mathf.Clamp(valor, 0, 1);
        }
        set
        {
            PlayerPrefs.SetInt(ClavePrefs, (int)value);
            PlayerPrefs.Save();
        }
    }
}
