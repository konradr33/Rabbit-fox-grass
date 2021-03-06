﻿using System;
using UnityEngine;
using UnityEngine.Profiling;

namespace World
{
    public abstract class Animal : WorldObject, IAlive, IUpdatable
    {
        public Vector2Int worldSize;
        public bool UseLocalViewSpace;

        protected Vector3 velocity = Vector3.zero;
        protected NeuralNetwork brain;
        protected int deadAtTurn;
        protected int currentTurn = 0;
        public World world;
        public NeuralNetwork Brain { set { brain = value; } }

        protected abstract float MaxVelocity { get; }
        public abstract float HungerRate { get; }
        public float Health { get; set; } = 1f;
        public bool IsAlive => Health > 0f;

        abstract protected void ConsumeFood();

        abstract protected void CollectInfoAboutSurroundings();

        abstract protected float[] CreateNetInputs();

        abstract protected void MultiplyAnimal();

        /// <summary>
        /// Parallel update, sets velocity
        /// </summary>
        /// <returns></returns>
        public bool UpdateTurn()
        {
            if (!IsAlive) throw new Exception("Update on dead animal");

            world.WorldEvents.Invoke(this, HistoryEventType.POSITION, Position);

            currentTurn++;
            if (currentTurn > deadAtTurn)
            {
                //Debug.Log("Immortal animal " + currentTurn);
                Health = -1;
            }

            // Handle hunger
            Health -= HungerRate * Settings.World.simulationDeltaTime;
            Health = Mathf.Clamp01(Health);
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
            Profiler.EndSample();

            // Set velocity based on mode
            if (Settings.Player.fastTrainingMode)
            {
                decision = decision * Settings.World.simulationDeltaTime / World.deltaTime;
            }
            if (UseLocalViewSpace)
            {
                velocity = Forward * decision.x + Right * decision.z;
            }
            else
            {
                velocity = decision;
            }
            return true;
        }

        private void HandleDeath()
        {
            world.WorldEvents.Invoke(this, HistoryEventType.DEATH, Position);
        }

        public void UpdatePosition()
        {
            var newPosition = transform.localPosition + (MaxVelocity * velocity * Time.deltaTime);
            newPosition.x = Mathf.Clamp(newPosition.x, 0.5f, worldSize.x - 0.5f);
            newPosition.z = Mathf.Clamp(newPosition.z, 0.5f, worldSize.y - 0.5f);

            if ((MaxVelocity * velocity).sqrMagnitude > 0)
            {
                transform.forward += (MaxVelocity * velocity).normalized;
                transform.forward *= 0.5f;
                transform.forward.Normalize();
            }

            transform.localPosition = newPosition;
            position = newPosition;
            forward = transform.forward;
            Right = transform.right;
        }

        protected Vector3 CalculateMultipliedAnimalPosition()
        {
            var radius = Settings.World.multipliedAnimalSpawnRadius;
            var objPosition = Position;
            var xPos = objPosition.x;
            var zPos = objPosition.z;
            return new Vector3(Utils.FloatInRange(xPos - radius, xPos + radius), 0, Utils.FloatInRange(zPos - radius, zPos + radius));
        }

        protected void Awake()
        {
            deadAtTurn = (int)(Settings.World.maxAnimalLifetime / Settings.World.simulationDeltaTime);
            world = transform.parent.GetComponent<World>();
        }

        protected void Start()
        {
            position = gameObject.transform.localPosition;
        }
    }
}