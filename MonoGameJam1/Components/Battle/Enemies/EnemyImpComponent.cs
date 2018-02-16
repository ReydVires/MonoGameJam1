﻿using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameJam1.Components.Colliders;
using MonoGameJam1.Components.Sprites;
using Nez;
using System.Collections.Generic;
using MonoGameJam1.FSM;

namespace MonoGameJam1.Components.Battle.Enemies
{
    public class EnemyImpComponent : EnemyComponent, ISpawnableEnemy
    {
        //--------------------------------------------------
        // Finite State Machine

        public FiniteStateMachine<EnemyImpStates, EnemyImpComponent> FSM { get; private set; }

        //--------------------------------------------------
        // Movable Area

        public RectangleF MovableArea { get; set; }

        //----------------------//------------------------//

        public EnemyImpComponent(bool patrolStartRight) : base(patrolStartRight)
        {
        }

        public override void initialize()
        {
            base.initialize();

            // Init sprite
            var texture = entity.scene.content.Load<Texture2D>(Content.Characters.imp);
            sprite = entity.addComponent(new AnimatedSprite(texture, "stand"));
            sprite.CreateAnimation("stand", 0.1f);
            sprite.AddFrames("stand", new List<Rectangle>
            {
                new Rectangle(0, 0, 100, 100),
                new Rectangle(100, 0, 100, 100),
                new Rectangle(200, 0, 100, 100),
                new Rectangle(300, 0, 100, 100),
                new Rectangle(400, 0, 100, 100),
                new Rectangle(500, 0, 100, 100),
            });

            sprite.CreateAnimation("spawn", 0.09f, false);
            sprite.AddFrames("spawn", new List<Rectangle>
            {
                new Rectangle(100, 600, 100, 100),
                new Rectangle(200, 600, 100, 100),
                new Rectangle(300, 600, 100, 100),
                new Rectangle(400, 600, 100, 100),
                new Rectangle(500, 600, 100, 100),
                new Rectangle(0, 700, 100, 100),
                new Rectangle(100, 700, 100, 100),
                new Rectangle(200, 700, 100, 100),
            });

            sprite.CreateAnimation("walking", 0.09f);
            sprite.AddFrames("walking", new List<Rectangle>
            {
                new Rectangle(0, 300, 100, 100),
                new Rectangle(100, 300, 100, 100),
                new Rectangle(200, 300, 100, 100),
                new Rectangle(300, 300, 100, 100),
                new Rectangle(400, 300, 100, 100),
                new Rectangle(500, 300, 100, 100),
            });

            sprite.CreateAnimation("punch", 0.09f);
            sprite.AddFrames("punch", new List<Rectangle>
            {
                new Rectangle(0, 200, 100, 100),
                new Rectangle(100, 200, 100, 100),
                new Rectangle(200, 200, 100, 100),
                new Rectangle(300, 200, 100, 100),
                new Rectangle(400, 200, 100, 100),
                new Rectangle(500, 200, 100, 100),
            });

            sprite.CreateAnimation("dying", 0.1f, false);
            sprite.AddFrames("dying", new List<Rectangle>
            {
                new Rectangle(100, 500, 100, 100),
                new Rectangle(200, 500, 100, 100),
                new Rectangle(300, 500, 100, 100),
                new Rectangle(400, 500, 100, 100),
                new Rectangle(500, 500, 100, 100),
                new Rectangle(0, 600, 100, 100),
            });

            // FSM
            FSM = new FiniteStateMachine<EnemyImpStates, EnemyImpComponent>(this, new EnemyImpThinking());

            // View range
            areaOfSight = entity.addComponent(new AreaOfSightCollider(-91, -12, 164, 32));
        }

        public override void onAddedToEntity()
        {
            base.onAddedToEntity();
            _battleComponent.setHp(3);

            // Change move speed
            platformerObject.maxMoveSpeed = 3000;
            platformerObject.moveSpeed = 3000;
        }

        public void GoToSpawnState()
        {
            FSM.resetStackTo(new EnemyImpSpawning());
        }

        public override void update()
        {
            FSM.update();
            base.update();

            var velocity = _forceMovement ? _forceMovementVelocity.X : 0.0f;
            if (Math.Abs(velocity) > 0.0001f)
                sprite.spriteEffects = velocity < 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
        }
    }
}
