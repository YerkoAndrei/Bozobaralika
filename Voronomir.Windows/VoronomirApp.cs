using Stride.Engine;
using System.Windows;

namespace Voronomir
{
    class VoronomirApp
    {
        static void Main(string[] args)
        {
            using (var game = new Game())
            {
                var vSync = false;

                // Primer inicio
                if (!SistemaMemoria.ObtenerExistenciaArchivo())
                {
                    // 63 = barra de t�tulo
                    var alto = (int)SystemParameters.FullPrimaryScreenHeight + 63;
                    var ancho = (int)SystemParameters.FullPrimaryScreenWidth;
                    SistemaMemoria.EstablecerConfiguraci�nPredeterminada(ancho, alto);
                }
                else
                    vSync = bool.Parse(SistemaMemoria.ObtenerConfiguraci�n(Constantes.Configuraciones.vSync));

                // vSync
                game.IsDrawDesynchronized = !vSync;
                game.GraphicsDeviceManager.SynchronizeWithVerticalRetrace = vSync;

                game.Run();
            }
        }
    }
}
