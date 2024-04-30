﻿using Stride.Core.Mathematics;
using Stride.Engine;
using System.Threading.Tasks;

namespace Bozobaralika;

public class ControladorPuerta : StartupScript, IActivable
{
    public int activaciones;
    public bool instantanea;
    public TransformComponent modelo;
    public Vector3 posiciónAbierta;

    private int activadas;

    public void Activar()
    {
        // PENDIENTE: efectos
        activadas++;

        if(activadas >= activaciones)
            AnimarPuerta();
    }

    private async void AnimarPuerta()
    {
        var inicio = modelo.Position;
        float duración = 1f;
        float tiempoLerp = 0;
        float tiempo = 0;

        if(instantanea)
            duración = 0.1f;
        else
            await Task.Delay(200);

        while (tiempoLerp < duración)
        {
            tiempo = SistemaAnimación.EvaluarSuave(tiempoLerp / duración);
            modelo.Position = Vector3.Lerp(inicio, posiciónAbierta, tiempo);

            tiempoLerp += (float)Game.UpdateTime.WarpElapsed.TotalSeconds;
            await Task.Delay(1);
        }
    }
}
