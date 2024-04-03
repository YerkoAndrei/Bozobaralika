﻿using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Navigation;
using Stride.Physics;

namespace Bozobaralika;

public class ControladorPersecusión : StartupScript
{
    private ControladorEnemigo controlador;
    private CharacterComponent cuerpo;
    private TransformComponent jugador;
    private NavigationComponent navegador;
    private IAnimador animador;

    private List<Vector3> ruta;
    private int índiceRuta;

    private Vector3 movimiento;
    private Vector3 posiciónJugador;
    private float distanciaRuta;
    private float distanciaJugador;
    private float distanciaMínima;

    private bool atacando;
    private float velocidad;
    private float aceleración;
    private float distanciaAtaque;

    private float tiempoRotación;
    private float velocidadRotación;

    private float tempoAceleración;
    private float tiempoAceleración;

    private float tempoBusqueda;
    private float tiempoBusqueda;

    public void Iniciar(ControladorEnemigo _controlador, float _tiempoBusqueda, float _velocidad, float _rotación, float _distanciaAtaque)
    {
        jugador = Entity.Scene.Entities.Where(o => o.Get<ControladorJugador>() != null).FirstOrDefault().Transform;
        foreach (var componente in Entity.Components)
        {
            if (componente is IAnimador)
            {
                animador = (IAnimador)componente;
                break;
            }
        }
        animador.Iniciar();

        cuerpo = Entity.Get<CharacterComponent>();
        navegador = Entity.Get<NavigationComponent>();
        ruta = new List<Vector3>();

        distanciaMínima = 0.1f;
        tiempoAceleración = 1;

        controlador = _controlador;
        tiempoBusqueda = _tiempoBusqueda;
        tempoBusqueda = tiempoBusqueda;

        velocidad = _velocidad;
        velocidadRotación = _rotación;
        distanciaAtaque = _distanciaAtaque;
    }

    public void Actualizar()
    {
        if (atacando)
        {
            MirarJugador(velocidadRotación * 0.5f);
            return;
        }

        // Busca cada cierto tiempo
        tempoBusqueda -= (float)Game.UpdateTime.Elapsed.TotalSeconds;
        if(tempoBusqueda <= 0)
            BuscarJugador();

        Perseguir();
        MirarJugador(velocidadRotación);
    }

    private void BuscarJugador()
    {
        ruta.Clear();
        índiceRuta = 0;
        tempoBusqueda = tiempoBusqueda;
        navegador.TryFindPath(jugador.WorldMatrix.TranslationVector, ruta);
    }

    private void MirarJugador(float velocidad)
    {
        posiciónJugador = jugador.WorldMatrix.TranslationVector - Entity.Transform.WorldMatrix.TranslationVector;
        posiciónJugador.Y = 0f;
        posiciónJugador.Normalize();

        tiempoRotación = velocidad * (float)Game.UpdateTime.Elapsed.TotalSeconds;
        cuerpo.Orientation = Quaternion.Lerp(cuerpo.Orientation, Quaternion.LookRotation(posiciónJugador, Vector3.UnitY), tiempoRotación);
    }

    private void Perseguir()
    {
        if (ruta.Count == 0)
            return;

        distanciaRuta = Vector3.Distance(Entity.Transform.WorldMatrix.TranslationVector, ruta[índiceRuta]);
        distanciaJugador = Vector3.Distance(Entity.Transform.WorldMatrix.TranslationVector, jugador.WorldMatrix.TranslationVector);

        // Ataque
        if (distanciaJugador <= distanciaAtaque)
        {
            Atacar();
            return;
        }

        // Movimiento
        if (distanciaRuta > distanciaMínima)
        {
            tempoAceleración += (float)Game.UpdateTime.Elapsed.TotalSeconds;
            aceleración = MathUtil.SmoothStep(tempoAceleración / tiempoAceleración);

            // Mientras salta va directo al jugador
            if(cuerpo.IsGrounded)
                movimiento = ruta[índiceRuta] - Entity.Transform.WorldMatrix.TranslationVector;
            else
                movimiento = jugador.WorldMatrix.TranslationVector - Entity.Transform.WorldMatrix.TranslationVector;

            movimiento.Normalize();
            movimiento *= (float)Game.UpdateTime.Elapsed.TotalSeconds;
                        
            cuerpo.SetVelocity(movimiento * 100 * velocidad * aceleración);
            animador.Caminar(aceleración);
        }
        else
        {
            if (índiceRuta < (ruta.Count - 1))
                índiceRuta++;
            else
                ruta.Clear();
        }
    }

    public async void Atacar()
    {
        atacando = true;
        tempoAceleración = 0;

        // Delay de preparación de ataque
        cuerpo.SetVelocity(Vector3.Zero);
        await Task.Delay((int)(controlador.ObtenerPreparaciónAtaque() * 1000));

        if (!controlador.ObtenerActivo())
            return;

        animador.Atacar();
        controlador.Atacar();
        await Task.Delay((int)(controlador.ObtenerDescansoAtaque() * 1000));

        BuscarJugador();
        atacando = false;
    }
}
