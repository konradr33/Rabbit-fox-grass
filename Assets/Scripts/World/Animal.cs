﻿using System;
using UnityEngine;
using UnityEngine.Profiling;

namespace World
{
    public abstract class Animal : WorldObject, IAlive, IUpdatable
    {
        public Vector2Int worldSize;
        public LayerMask feedOnLayer;


        protected Vector3 velocity = Vector3.zero;
        protected IBigBrain brain;
        public World world;
        public IBigBrain Brain { set { brain = value; } }

        protected abstract float MaxVelocity { get; }
        public abstract float HungerRate { get; }
        public float Health { get; set; } = 1f;
        public bool IsAlive => Health > 0f;

        protected Vector3 forward;
        public Vector3 Forward { get { return forward; } }

        abstract protected void ConsumeFood();

        abstract protected void CollectInfoAboutSurroundings();

        abstract protected float[] CreateNetInputs();

        abstract protected void HandleDeath();

        /// <summary>
        /// Parallel update, sets velocity
        /// </summary>
        /// <returns></returns>
        public bool UpdateTurn()
        {
            if (!IsAlive) throw new Exception("Update on dead animal");

            // Handle hunger
            Health -= HungerRate * Settings.World.simulationDeltaTime;
            if (Health <= 0f)
            {
                HandleDeath();
                return false;
            }

            // Get info about surroundings
            CollectInfoAboutSurroundings();

            // Consume food
            ConsumeFood();

            // Get decision from net
            Profiler.BeginSample("run NeuralNet");
            Vector3 decision = brain.GetDecision(CreateNetInputs());
            //print(decision);
            Profiler.EndSample();

            // Set velocity based on mode
            if (Settings.Player.fastTrainingMode)
            {
                velocity = decision * Settings.World.simulationDeltaTime / World.deltaTime;
            }
            else
            {
                velocity = decision;
            }

            return true;
        }

        private void Update()
        {
            var newPosition = transform.localPosition + (MaxVelocity * velocity * Time.deltaTime);
            newPosition.x = Mathf.Clamp(newPosition.x, 0.5f, worldSize.x - 0.5f);
            newPosition.z = Mathf.Clamp(newPosition.z, 0.5f, worldSize.y - 0.5f);

            velocity = (newPosition - transform.localPosition) / Time.deltaTime;
            if (velocity.sqrMagnitude > 0) transform.forward = velocity.normalized;

            transform.localPosition = newPosition;
            position = newPosition;
            forward = transform.forward;
        }

        protected new void Awake()
        {
            base.Awake();
            world = transform.parent.GetComponent<World>();
        }
    }
}