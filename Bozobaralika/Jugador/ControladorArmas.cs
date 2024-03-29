﻿using System.Threading.Tasks;
using Stride.Core.Mathematics;
using Stride.Input;
using Stride.Engine;
using Stride.Physics;
using System.Collections.Generic;

namespace Bozobaralika;
using static Utilidades;
using static Constantes;

// Filtros:
// Default      - Disparos
// Static       - Entorno, puertas, llaves
// Kinematic    - Enemigos
// Debris       - Escombros
// Sensor       - Puertas
// Character    - Jugador

public class ControladorArmas : SyncScript
{
    public ControladorArmaMelé armaMelé;
    public Prefab prefabMarca;

    public ModelComponent modeloEspada;
    public ModelComponent modeloEscopeta;
    public ModelComponent modeloMetralleta;
    public ModelComponent modeloRife;

    public TransformComponent ejeEspada;
    public TransformComponent ejeEscopeta;
    public TransformComponent ejeMetralleta;
    public TransformComponent ejeRife;

    private ControladorJugador controlador;
    private ControladorMovimiento movimiento;
    private CameraComponent cámara;
    private InterfazJuego interfaz;

    private CollisionFilterGroupFlags colisionesDisparo;
    private Armas armaActual;
    private Armas armaAnterior;
    private bool bloqueo;
    private float dañoMínimo;
    private float dañoMáximo;

    private float últimoDisparoEspada;
    private float últimoDisparoEscopeta;
    private float últimoDisparoMetralleta;
    private float últimoDisparoRifle;

    private bool cambiandoArma;
    private bool usandoMira;
    private Vector3 posiciónEjes;

    // Metralleta
    private bool metralletaAtascada;
    private float tempoMetralleta;
    private float tiempoMaxMetralleta;
    private float tiempoAtascamientoMetralleta;

    public void Iniciar(ControladorJugador _controlador, ControladorMovimiento _movimiento, CameraComponent _cámara, InterfazJuego _interfaz)
    {
        controlador = _controlador;
        movimiento = _movimiento;
        cámara = _cámara;
        interfaz = _interfaz;

        dañoMínimo = 1f;
        dañoMáximo = 60f;

        posiciónEjes = new Vector3(0, -0.5f, 0);

        tiempoMaxMetralleta = 4f;
        tiempoAtascamientoMetralleta = 2f;
        tempoMetralleta = tiempoAtascamientoMetralleta;

        // Melé
        armaMelé.Iniciar(true);

        // Filtros disparos
        colisionesDisparo = CollisionFilterGroupFlags.StaticFilter | CollisionFilterGroupFlags.KinematicFilter;

        // Arma por defecto
        ApagarArmas();
        armaActual = Armas.espada;
        armaAnterior = armaActual;
        interfaz.CambiarMira(armaActual);
        interfaz.CambiarÍcono(armaActual);

        modeloEspada.Entity.Get<ModelComponent>().Enabled = true;
    }

    public override void Update()
    {
        if (bloqueo)
            return;

        // Disparo general
        if (Input.IsMouseButtonPressed(MouseButton.Left))
        {
            Disparar();

            if(tempoMetralleta >= tiempoMaxMetralleta)
                metralletaAtascada = false;
        }

        // Metralleta
        if (Input.IsMouseButtonDown(MouseButton.Left) && armaActual == Armas.metralleta && !metralletaAtascada)
        {
            tempoMetralleta -= (float)Game.UpdateTime.Elapsed.TotalSeconds;
            if (tempoMetralleta > 0)
                Disparar();
            else
                AtascarMetralleta();
        }

        if (!Input.IsMouseButtonDown(MouseButton.Left))
            EnfriarMetralleta();

        // Rifle
        if (Input.IsMouseButtonPressed(MouseButton.Right) && armaActual == Armas.rifle)
            AcercarMira(true);

        if (Input.IsMouseButtonReleased(MouseButton.Right) && armaActual == Armas.rifle)
            AcercarMira(false);

        // Cambio armas
        if (Input.IsKeyPressed(Keys.D1) || Input.IsKeyPressed(Keys.NumPad1))
            CambiarArma(Armas.espada);
        if (Input.IsKeyPressed(Keys.D2) || Input.IsKeyPressed(Keys.NumPad2))
            CambiarArma(Armas.escopeta);
        if (Input.IsKeyPressed(Keys.D3) || Input.IsKeyPressed(Keys.NumPad3))
            CambiarArma(Armas.metralleta);
        if (Input.IsKeyPressed(Keys.D4) || Input.IsKeyPressed(Keys.NumPad4))
            CambiarArma(Armas.rifle);

        if (Input.IsKeyPressed(Keys.Q))
            CambiarArma(armaAnterior);

        // Debug
        DebugText.Print(armaActual.ToString(), new Int2(x: 20, y: 60));
        DebugText.Print(metralletaAtascada.ToString(), new Int2(x: 20, y: 80));
        DebugText.Print(tempoMetralleta.ToString(), new Int2(x: 20, y: 100));
        DebugText.Print(Game.UpdateTime.Total.TotalSeconds.ToString(), new Int2(x: 20, y: 120));
    }

    private void Disparar()
    {
        if (cambiandoArma)
            return;

        // Metralleta
        if (armaActual == Armas.metralleta && metralletaAtascada)
            return;

        // Cadencia
        var tiempoDisparo = 0f;
        switch (armaActual)
        {
            case Armas.espada:
                tiempoDisparo = ObtenerCadencia(armaActual) + últimoDisparoEspada;
                break;
            case Armas.escopeta:
                tiempoDisparo = ObtenerCadencia(armaActual) + últimoDisparoEscopeta;
                break;
            case Armas.metralleta:
                tiempoDisparo = ObtenerCadencia(armaActual) + últimoDisparoMetralleta;
                break;
            case Armas.rifle:
                tiempoDisparo = ObtenerCadencia(armaActual) + últimoDisparoRifle;
                break;
        }
        if ((float)Game.UpdateTime.Total.TotalSeconds < tiempoDisparo)
            return;

        switch (armaActual)
        {
            case Armas.espada:
                Atacar();
                AnimarAtaque();
                últimoDisparoEspada = (float)Game.UpdateTime.Total.TotalSeconds;
                break;
            case Armas.escopeta:
                movimiento.DetenerMovimiento();
                for (int i = 0; i < ObtenerCantidadPerdigones(); i++)
                {
                    CalcularRayo(0.25f);
                }
                controlador.VibrarCámara(16, 10);
                AnimarDisparo(ejeEscopeta, 0.5f, 0.2f);
                últimoDisparoEscopeta = (float)Game.UpdateTime.Total.TotalSeconds;
                break;
            case Armas.metralleta:
                CalcularRayo(0.06f);
                AnimarDisparo(ejeMetralleta, 0.15f, 0.05f);
                últimoDisparoMetralleta = (float)Game.UpdateTime.Total.TotalSeconds;
                break;
            case Armas.rifle:
                movimiento.DetenerMovimiento();
                CalcularRayoPenetrante();
                controlador.VibrarCámara(20, 12);
                AnimarDisparo(ejeRife, 2f, 0.5f);
                últimoDisparoRifle = (float)Game.UpdateTime.Total.TotalSeconds;
                break;
        }
    }

    private void CalcularRayo(float imprecisión)
    {
        var aleatorioX = RangoAleatorio(-(imprecisión), imprecisión);
        var aleatorioY = RangoAleatorio(-(imprecisión), imprecisión);
        var aleatorioZ = RangoAleatorio(-(imprecisión), imprecisión);
        var aleatorio = new Vector3(aleatorioX, aleatorioY, aleatorioZ);

        // Distancia máxima de disparo: 500
        var dirección = cámara.Entity.Transform.WorldMatrix.TranslationVector +
                        (cámara.Entity.Transform.WorldMatrix.Forward + aleatorio) * 500;

        var resultado = this.GetSimulation().Raycast(cámara.Entity.Transform.WorldMatrix.TranslationVector,
                                                     dirección,
                                                     CollisionFilterGroups.DefaultFilter,
                                                     colisionesDisparo);
        if (!resultado.Succeeded)
            return;

        if (resultado.Collider.CollisionGroup == CollisionFilterGroups.StaticFilter)
        {
            CrearMarca(armaActual, resultado.Point);
            return;
        }

        var enemigo = resultado.Collider.Entity.Get<ControladorEnemigo>();
        if (enemigo == null)
            return;

        // PENDIENTE: efecto
        // Daño segun distancia
        var distancia = Vector3.Distance(cámara.Entity.Transform.WorldMatrix.TranslationVector, resultado.Point);
        var reducción = 0f;
        if(distancia > ObtenerDistanciaMáxima(armaActual))
            reducción = (distancia - ObtenerDistanciaMáxima(armaActual)) * 0.5f;

        // Retroalimentación daño
        if (armaActual == Armas.metralleta)
            controlador.VibrarCámara(1f, 4);

        // Daña enemigo
        var dañoFinal = ObtenerDaño(armaActual) - reducción;
        dañoFinal = MathUtil.Clamp(dañoFinal, dañoMínimo, dañoMáximo);
        enemigo.RecibirDaño(dañoFinal);
    }

    private void CalcularRayoPenetrante()
    {
        // Distancia máxima de disparo: 1000
        var dirección = cámara.Entity.Transform.WorldMatrix.TranslationVector + cámara.Entity.Transform.WorldMatrix.Forward * 1000;

        var resultados = new List<HitResult>();
        this.GetSimulation().RaycastPenetrating(cámara.Entity.Transform.WorldMatrix.TranslationVector,
                                                dirección, resultados,
                                                CollisionFilterGroups.DefaultFilter,
                                                colisionesDisparo);
        if (resultados.Count == 0)
            return;

        foreach (var resultado in resultados)
        {
            if (resultado.Collider.CollisionGroup == CollisionFilterGroups.StaticFilter)
            {
                CrearMarca(armaActual, resultado.Point);
                break;
            }

            var enemigo = resultado.Collider.Entity.Get<ControladorEnemigo>();
            if (enemigo == null)
                return;

            // Retroalimentación daño
            controlador.VibrarCámara(4f, 4);

            // PENDIENTE: efecto
            // Daño segun distancia
            var distancia = Vector3.Distance(cámara.Entity.Transform.WorldMatrix.TranslationVector, resultado.Point);
            var aumento = 0f;
            if (distancia > ObtenerDistanciaMáxima(armaActual))
                aumento = (distancia - ObtenerDistanciaMáxima(armaActual)) * 0.5f;

            // Daña enemigo
            var dañoFinal = ObtenerDaño(armaActual) + aumento;
            dañoFinal = MathUtil.Clamp(dañoFinal, dañoMínimo, dañoMáximo);
            enemigo.RecibirDaño(dañoFinal);
        }
    }

    private void Atacar()
    {
        // Cadencia
        var tiempoDisparo = ObtenerCadencia(Armas.espada) + últimoDisparoEspada;
        if ((float)Game.UpdateTime.Total.TotalSeconds < tiempoDisparo)
            return;

        // Distancia máxima de disparo: 2
        var dirección = cámara.Entity.Transform.WorldMatrix.TranslationVector + cámara.Entity.Transform.WorldMatrix.Forward * 2;
        var resultado = this.GetSimulation().Raycast(cámara.Entity.Transform.WorldMatrix.TranslationVector,
                                                     dirección,
                                                     CollisionFilterGroups.DefaultFilter,
                                                     CollisionFilterGroupFlags.StaticFilter);
        if (resultado.Succeeded)
            CrearMarca(armaActual, resultado.Point);

        // PENDIENTE: efecto
        armaMelé.Atacar(ObtenerDaño(Armas.espada));
    }

    private void CrearMarca(Armas arma, Vector3 posición)
    {
        // PENDIENTE: usar piscina
        // PENDIENTE: efecto
        // Marca balazo
        var marca = prefabMarca.Instantiate()[0];
        marca.Transform.Position = posición;
        Entity.Scene.Entities.Add(marca);
        return;

    }

    private int ObtenerCantidadPerdigones()
    {
        // PENDIENTE: mejoras
        return 20;
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

    private void AcercarMira(bool acercar)
    {
        // PENDIENTE: elegir FOV de opciones
        // PENDIENTE: animacioón
        usandoMira = acercar;
        if (usandoMira)
            cámara.VerticalFieldOfView = 20;
        else
            cámara.VerticalFieldOfView = 90;

        movimiento.CambiarSensiblidad(acercar);
        interfaz.MostrarMiraRifle(acercar);
    }

    private float ObtenerDaño(Armas arma)
    {
        switch (arma)
        {
            case Armas.espada:
                return 40;
            case Armas.escopeta:
                return 10;
            case Armas.metralleta:
                return 5;
            case Armas.rifle:
                return 40;
            default:
                return 0;
        }
    }

    private float ObtenerDistanciaMáxima(Armas arma)
    {
        switch (arma)
        {
            default:
            case Armas.espada:
                return 0;
            case Armas.escopeta:
                return 5;
            case Armas.metralleta:
                return 10;
            case Armas.rifle:
                return 10;
        }
    }

    private float ObtenerCadencia(Armas arma)
    {
        switch (arma)
        {
            case Armas.espada:
                return 0.2f;
            case Armas.escopeta:
                return 0.8f;
            case Armas.metralleta:
                return 0.05f;
            case Armas.rifle:
                return 2f;
            default:
                return 0;
        }
    }

    private void CambiarArma(Armas nuevaArma)
    {
        if (cambiandoArma || nuevaArma == armaActual || usandoMira)
            return;

        var armaSale = ejeEspada;
        var modeloSale = modeloEspada;
        switch (armaActual)
        {
            case Armas.espada:
                armaSale = ejeEspada;
                modeloSale = modeloEspada;
                break;
            case Armas.escopeta:
                armaSale = ejeEscopeta;
                modeloSale = modeloEscopeta;
                break;
            case Armas.metralleta:
                armaSale = ejeMetralleta;
                modeloSale = modeloMetralleta;
                break;
            case Armas.rifle:
                armaSale = ejeRife;
                modeloSale = modeloRife;
                break;
        }

        armaAnterior = armaActual;
        armaActual = nuevaArma;
        movimiento.DetenerMovimiento();
        interfaz.CambiarÍcono(armaActual);

        switch (armaActual)
        {
            case Armas.espada:
                movimiento.CambiarVelocidadMáxima(true);
                modeloEspada.Entity.Get<ModelComponent>().Enabled = true;
                AnimarCambioArma(ejeEspada, armaSale, modeloSale);
                break;
            case Armas.escopeta:
                movimiento.CambiarVelocidadMáxima(false);
                modeloEscopeta.Entity.Get<ModelComponent>().Enabled = true;
                AnimarCambioArma(ejeEscopeta, armaSale, modeloSale);
                break;
            case Armas.metralleta:
                movimiento.CambiarVelocidadMáxima(false);
                modeloMetralleta.Entity.Get<ModelComponent>().Enabled = true;
                AnimarCambioArma(ejeMetralleta, armaSale, modeloSale);
                break;
            case Armas.rifle:
                movimiento.CambiarVelocidadMáxima(false);
                modeloRife.Entity.Get<ModelComponent>().Enabled = true;
                AnimarCambioArma(ejeRife, armaSale, modeloSale);
                break;
        }
    }

    private void ApagarArmas()
    {
        modeloEspada.Entity.Get<ModelComponent>().Enabled = false;
        modeloEscopeta.Entity.Get<ModelComponent>().Enabled = false;
        modeloMetralleta.Entity.Get<ModelComponent>().Enabled = false;
        modeloRife.Entity.Get<ModelComponent>().Enabled = false;
    }

    public void Bloquear(bool bloquear)
    {
        bloqueo = bloquear;
    }

    private async void AnimarCambioArma(TransformComponent entra, TransformComponent sale, ModelComponent modeloSale)
    {
        cambiandoArma = true;
        interfaz.ApagarMiras();

        var rotaciónCentro = Quaternion.Identity;
        var rotaciónEntra = Quaternion.RotationYawPitchRoll(MathUtil.DegreesToRadians(-90), 0, 0);
        var rotaciónSale = Quaternion.RotationYawPitchRoll(MathUtil.DegreesToRadians(90), 0, 0);

        float duración = 0.1f;
        float tiempoLerp = 0;
        float tiempo = 0;

        while (tiempoLerp < duración)
        {
            tiempo = SistemaAnimación.EvaluarSuave(tiempoLerp / duración);

            entra.Rotation = Quaternion.Lerp(rotaciónEntra, rotaciónCentro, tiempo);
            sale.Rotation = Quaternion.Lerp(rotaciónCentro, rotaciónSale, tiempo);

            tiempoLerp += (float)Game.UpdateTime.Elapsed.TotalSeconds;
            await Task.Delay(1);
        }
        
        cambiandoArma = false;
        interfaz.CambiarMira(armaActual);

        modeloSale.Entity.Get<ModelComponent>().Enabled = false;
    }

    private async void AnimarDisparo(TransformComponent arma, float retroceso, float duración)
    {
        float tiempoLerp = 0;
        float tiempo = 0;

        // Posición rápida
        var posiciónDisparo = posiciónEjes + new Vector3(0, 0, retroceso);
        arma.Position = posiciónDisparo;

        while (tiempoLerp < duración)
        {
            tiempo = SistemaAnimación.EvaluarSuave(tiempoLerp / duración);
            arma.Position = Vector3.Lerp(posiciónDisparo, posiciónEjes, tiempo);

            tiempoLerp += (float)Game.UpdateTime.Elapsed.TotalSeconds;
            await Task.Delay(1);
        }

        arma.Position = posiciónEjes;
    }

    private async void AnimarAtaque()
    {
        float duración = 0.1f;
        float tiempoLerp = 0;
        float tiempo = 0;

        // Rotación rápida
        var rotaciónAtaque = Quaternion.RotationYawPitchRoll(0, MathUtil.DegreesToRadians(-40), 0);
        ejeEspada.Rotation = rotaciónAtaque;

        while (tiempoLerp < duración)
        {
            tiempo = SistemaAnimación.EvaluarSuave(tiempoLerp / duración);
            ejeEspada.Rotation = Quaternion.Lerp(rotaciónAtaque, Quaternion.Identity, tiempo);

            tiempoLerp += (float)Game.UpdateTime.Elapsed.TotalSeconds;
            await Task.Delay(1);
        }

        ejeEspada.Rotation = Quaternion.Identity;
    }
}
