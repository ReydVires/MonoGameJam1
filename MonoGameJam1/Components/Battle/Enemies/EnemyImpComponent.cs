﻿using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameJam1.Components.Colliders;
using MonoGameJam1.Components.Sprites;
using Nez;
using System.Collections.Generic;
using MonoGameJam1.Extensions;
using MonoGameJam1.FSM;
using MonoGameJam1.Managers;

namespace MonoGameJam1.Components.Battle.Enemies
{
    public enum ImpVelocity
    {
        Normal,
        Fast
    }

    public class EnemyImpComponent : EnemyComponent, ISpawnableEnemy
    {
        //--------------------------------------------------
        // Finite State Machine

        public FiniteStateMachine<EnemyImpStates, EnemyImpComponent> FSM { get; private set; }

        //--------------------------------------------------
        // Movable Area

        public RectangleF MovableArea { get; set; }

        //----------------------//------------------------//

        public override void initialize()
        {
            base.initialize();

            // Init sprite
            var texture = getImpTexture();
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
                new Rectangle(400, 100, 100, 100),
                new Rectangle(500, 100, 100, 100),
                new Rectangle(0, 200, 100, 100),
                new Rectangle(100, 200, 100, 100),
                new Rectangle(200, 200, 100, 100),
                new Rectangle(300, 200, 100, 100),
                new Rectangle(400, 200, 100, 100),
                new Rectangle(500, 200, 100, 100),
            });
            sprite.AddAttackCollider("punch", new List<List<Rectangle>>
            {
                new List<Rectangle>(),
                new List<Rectangle>(),
                new List<Rectangle>(),
                new List<Rectangle>(),
                new List<Rectangle> { new Rectangle(10, 2, 24, 16) },
            });
            sprite.AddFramesToAttack("punch", 4);

            sprite.CreateAnimation("jumpAttack", 0.09f, false);
            sprite.AddFrames("jumpAttack", new List<Rectangle>
            {
                new Rectangle(0, 400, 100, 100),
                new Rectangle(100, 400, 100, 100),
                new Rectangle(200, 400, 100, 100),
                new Rectangle(300, 400, 100, 100),
                new Rectangle(400, 400, 100, 100),
                new Rectangle(0, 500, 100, 100),
                new Rectangle(100, 500, 100, 100),
            });
            sprite.AddAttackCollider("jumpAttack", new List<List<Rectangle>>
            {
                new List<Rectangle>(),
                new List<Rectangle>(),
                new List<Rectangle>(),
                new List<Rectangle>(),
                new List<Rectangle> { new Rectangle(-19, -9, 38, 32) },
                new List<Rectangle> { new Rectangle(-19, -9, 38, 32) },
                new List<Rectangle> { new Rectangle(-19, -9, 38, 32) },
            });
            sprite.AddFramesToAttack("jumpAttack", 4, 5, 6);

            sprite.CreateAnimation("hit", 0.1f, false);
            sprite.AddFrames("hit", new List<Rectangle>
            {
                new Rectangle(200, 100, 100, 100),
                new Rectangle(300, 100, 100, 100),
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
            areaOfSight = entity.addComponent(new AreaOfSightCollider(-96, -12, 192, 32));
        }

        public override void onAddedToEntity()
        {
            base.onAddedToEntity();
            _battleComponent.setHp(100);

            // Change move speed
            changeSpeed(ImpVelocity.Normal);
        }

        public override void onHit(Vector2 knockback)
        {
            base.onHit(knockback);
            FSM.changeState(new EnemyImpHit());
            _sawThePlayer = true;
        }

        public override void onDeath()
        {
            AudioManager.enemyDeath.Play(0.5f);
            FSM.changeState(new EnemyImpDying());
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
                sprite.spriteEffects = velocity < 0 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
        }

        public void changeSpeed(ImpVelocity velocity)
        {
            switch (velocity)
            {
                case ImpVelocity.Normal:
                    platformerObject.maxMoveSpeed = 150;
                    platformerObject.moveSpeed = 3000;
                    break;
                case ImpVelocity.Fast:
                    platformerObject.maxMoveSpeed = 14000;
                    platformerObject.moveSpeed = 14000;
                    break;
                default: break;
            }
        }

        protected virtual Texture2D getImpTexture()
        {
            return entity.scene.content.Load<Texture2D>(Content.Characters.imp);
        }
    }
}
