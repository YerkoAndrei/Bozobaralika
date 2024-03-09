﻿using System.Threading.Tasks;
using Stride.Core.Mathematics;
using Stride.Input;
using Stride.Engine;
using Stride.Physics;

namespace Bozobaralika;
using static Sistema;
using static Constantes;

public class ControladorArmas : SyncScript
{
    public ControladorMovimiento movimiento;
    public CameraComponent cámara;
    public Prefab prefabMarca;

    public TransformComponent espada;
    public TransformComponent pistola;
    public TransformComponent escopeta;
    public TransformComponent metralleta;
    public TransformComponent rife;

    private Arma armaActual;
    private float últimoDisparo;

    // Metralleta
    private bool metralletaAtascada;
    private float tempoMetralleta;
    private float tiempoMaxMetralleta;
    private float tiempoAtascamientoMetralleta;

    public override void Start()
    {
        tiempoMaxMetralleta = 4f;
        tiempoAtascamientoMetralleta = 2f;
        tempoMetralleta = tiempoAtascamientoMetralleta;

        ApagarArmas();
        CambiarArma(Arma.pistola);
    }

    public override void Update()
    {
        // Disparo general
        if (Input.IsMouseButtonPressed(MouseButton.Left))
        {
            Disparar();

            if(tempoMetralleta >= tiempoMaxMetralleta)
                metralletaAtascada = false;
        }

        // Metralleta
        if (Input.IsMouseButtonDown(MouseButton.Left) && armaActual == Arma.metralleta && !metralletaAtascada)
        {
            tempoMetralleta -= (float)Game.UpdateTime.Elapsed.TotalSeconds;
            if (tempoMetralleta > 0)
                Disparar();
            else
                AtascarMetralleta();
        }

        if (!Input.IsMouseButtonDown(MouseButton.Left))
            EnfriarMetralleta();

        // Melé
        if (Input.IsKeyPressed(Keys.E) || Input.IsMouseButtonPressed(MouseButton.Right))
            Atacar();

        // Cura
        if (Input.IsKeyPressed(Keys.F))
            Curar();

        // Cambio armas
        if (Input.IsKeyPressed(Keys.D1) || Input.IsKeyPressed(Keys.NumPad1))
            CambiarArma(Arma.pistola);
        if (Input.IsKeyPressed(Keys.D2) || Input.IsKeyPressed(Keys.NumPad2))
            CambiarArma(Arma.escopeta);
        if (Input.IsKeyPressed(Keys.D3) || Input.IsKeyPressed(Keys.NumPad3))
            CambiarArma(Arma.metralleta);
        if (Input.IsKeyPressed(Keys.D4) || Input.IsKeyPressed(Keys.NumPad4))
            CambiarArma(Arma.rifle);

        // Debug
        DebugText.Print(armaActual.ToString(), new Int2(x: 20, y: 60));
        DebugText.Print(metralletaAtascada.ToString(), new Int2(x: 20, y: 80));
        DebugText.Print(tempoMetralleta.ToString(), new Int2(x: 20, y: 100));
        DebugText.Print(últimoDisparo.ToString(), new Int2(x: 20, y: 120));
        DebugText.Print(Game.UpdateTime.Total.TotalSeconds.ToString(), new Int2(x: 20, y: 140));
    }

    private void Disparar()
    {
        // Cadencia
        var tiempoDisparo = ObtenerCadencia(armaActual) + últimoDisparo;
        if ((float)Game.UpdateTime.Total.TotalSeconds < tiempoDisparo)
            return;

        últimoDisparo = (float)Game.UpdateTime.Total.TotalSeconds;
        switch (armaActual)
        {
            case Arma.pistola:
                CalcularRayo(0);
                break;
            case Arma.escopeta:
                movimiento.DetenerMovimiento();
                for (int i = 0; i < ObtenerCantidadPerdigones(); i++)
                {
                    CalcularRayo(0.25f);
                }
                break;
            case Arma.metralleta:
                CalcularRayo(0.1f);
                break;
            case Arma.rifle:
                movimiento.DetenerMovimiento();
                CalcularRayo(0);
                break;
        }
    }

    private void CalcularRayo(float imprecisión)
    {
        var aleatorioX = RangoAleatorio(-(imprecisión), imprecisión);
        var aleatorioY = RangoAleatorio(-(imprecisión), imprecisión);
        var aleatorio = new Vector3(aleatorioX, aleatorioY, 0);

        // Distancia máxima de disparo: 1000
        var dirección = cámara.Entity.Transform.WorldMatrix.TranslationVector +
                        (cámara.Entity.Transform.WorldMatrix.Forward + aleatorio) * 1000;

        var resultado = this.GetSimulation().Raycast(cámara.Entity.Transform.WorldMatrix.TranslationVector,
                                                     dirección,
                                                     CollisionFilterGroups.DefaultFilter);

        if (resultado.Succeeded && resultado.Collider != null)
        {
            // PENDIENTE: dañor por distancia, rifle al reves
            // Daño segun distancia
            var distancia = Vector3.Distance(cámara.Entity.Transform.WorldMatrix.TranslationVector, resultado.Point);
            var dañoFinal = ObtenerDaño(armaActual);// * distancia;

            //resultado.Collider.Entity.Get<Enemigo>().Dañar(dañoFinal);

            // PENDIENTE: usar piscina
            // Marca balazo
            var marca = prefabMarca.Instantiate()[0];
            marca.Transform.Position = resultado.Point;
            Entity.Scene.Entities.Add(marca);
        }
    }

    private void Atacar()
    {
        // Distancia máxima melé: 2
        var dirección = cámara.Entity.Transform.WorldMatrix.TranslationVector +
                        cámara.Entity.Transform.WorldMatrix.Forward * 2;

        var resultado = this.GetSimulation().Raycast(cámara.Entity.Transform.WorldMatrix.TranslationVector,
                                                     dirección,
                                                     CollisionFilterGroups.DefaultFilter);

        if (resultado.Succeeded && resultado.Collider != null)
        {
            var dañoFinal = ObtenerDaño(Arma.espada);
            //resultado.Collider.Entity.Get<Enemigo>().Dañar(dañoFinal);

            // Marca ataque
            var marca = prefabMarca.Instantiate()[0];
            marca.Transform.Position = resultado.Point;
            Entity.Scene.Entities.Add(marca);
        }
    }

    private void Curar()
    {

    }

    private float ObtenerDaño(Arma arma)
    {
        switch (armaActual)
        {
            case Arma.espada:
                return 4;
            case Arma.pistola:
                return 2;
            case Arma.escopeta:
                return 1f;
            case Arma.metralleta:
                return 0.5f;
            case Arma.rifle:
                return 4;
            default:
                return 0;
        }
    }

    private float ObtenerCadencia(Arma arma)
    {
        switch (arma)
        {
            case Arma.espada:
                return 0.2f;
            case Arma.pistola:
                return 0.2f;
            case Arma.escopeta:
                return 0.8f;
            case Arma.metralleta:
                return 0.05f;
            case Arma.rifle:
                return 2f;
            default:
                return 0;
        }
    }

    private int ObtenerCantidadPerdigones()
    {
        // PENDIENTE: mejoras
        return 10;
    }

    private void EnfriarMetralleta()
    {
        if (tempoMetralleta < tiempoMaxMetralleta)
            tempoMetralleta += (float)Game.UpdateTime.Elapsed.TotalSeconds;
    }

    private async void AtascarMetralleta()
    {
        metralletaAtascada = true;
        await Task.Delay((int)(tiempoAtascamientoMetralleta * 1000));
        tempoMetralleta = tiempoMaxMetralleta;
    }

    private void CambiarArma(Arma nuevaArma)
    {
        ApagarArmas();
        armaActual = nuevaArma;

        switch (armaActual)
        {
            case Arma.pistola:
                espada.Entity.Get<ModelComponent>().Enabled = true;
                pistola.Entity.Get<ModelComponent>().Enabled = true;
                break;
            case Arma.escopeta:
                escopeta.Entity.Get<ModelComponent>().Enabled = true;
                break;
            case Arma.metralleta:
                espada.Entity.Get<ModelComponent>().Enabled = true;
                metralleta.Entity.Get<ModelComponent>().Enabled = true;
                break;
            case Arma.rifle:
                rife.Entity.Get<ModelComponent>().Enabled = true;
                break;
        }
    }

    private void ApagarArmas()
    {
        espada.Entity.Get<ModelComponent>().Enabled = false;
        pistola.Entity.Get<ModelComponent>().Enabled = false;
        escopeta.Entity.Get<ModelComponent>().Enabled = false;
        metralleta.Entity.Get<ModelComponent>().Enabled = false;
        rife.Entity.Get<ModelComponent>().Enabled = false;
    }
}
