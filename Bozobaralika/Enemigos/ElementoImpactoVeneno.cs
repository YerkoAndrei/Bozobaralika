﻿using System.Threading.Tasks;
using Stride.Core.Mathematics;
using Stride.Engine;

namespace Bozobaralika;

public class ElementoImpactoVeneno : StartupScript, IImpacto
{
    public float tiempoVida;
    public ModelComponent modelo;

    private PhysicsComponent cuerpo;
    private Vector3 escalaInicial;

    public override void Start()
    {
        cuerpo = Entity.Get<PhysicsComponent>();
        escalaInicial = Entity.Transform.Scale;
        modelo.Enabled = false;
        cuerpo.Enabled = false;
    }

    public void Iniciar(Vector3 posición, Quaternion rotación, float daño)
    {
        Entity.Transform.Position = posición;
        Entity.Transform.Rotation = rotación * Quaternion.RotationY(MathUtil.DegreesToRadians(90));
        Entity.Transform.Scale = escalaInicial;

        modelo.Enabled = true;
        cuerpo.Enabled = true;

        ContarVida();
    }

    private async void ContarVida()
    {
        float tiempoLerp = 0;
        float tiempo = 0;

        await Task.Delay(400);
        while (tiempoLerp < tiempoVida)
        {
            tiempo = tiempoLerp / tiempoVida;
            Entity.Transform.Scale = Vector3.Lerp(escalaInicial, Vector3.Zero, tiempo);

            tiempoLerp += (float)Game.UpdateTime.Elapsed.TotalSeconds;
            await Task.Delay(1);
        }

        modelo.Enabled = false;
        cuerpo.Enabled = false;
    }
}
