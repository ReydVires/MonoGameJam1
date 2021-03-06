﻿using System;
using Microsoft.Xna.Framework;
using MonoGameJam1.Components.Map;
using MonoGameJam1.Scenes;
using Nez;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;
using MonoGameJam1.Components.Battle;
using MonoGameJam1.Components.Battle.Enemies;
using MonoGameJam1.Components.Sprites;
using MonoGameJam1.Managers;
using Random = Nez.Random;

namespace MonoGameJam1.Systems
{
    public class BattleAreasSystem : EntityProcessingSystem
    {
        private readonly Entity _blockEntity;
        private readonly Entity _playerEntity;

        // HUD
        private MapHudComponent _mapHud;

        // Battle
        private bool _battleHappening;
        private BattleAreaComponent _currentBattle;
        private float _spawnInterval;
        private int _currentWave;
        private int _enemiesSpawned;
        private int _enemiesDefeated;
        private readonly List<Entity> _enemies;
        private readonly List<Entity> _enemiesToRemove;

        public bool _waitingPlayerToCrossBarrier;

        private readonly SystemManager _systemManager;
        private readonly List<Entity> _rocks;
        private readonly List<Entity> _rocksToRemove;
        private float _throwRockInterval;

        public BattleAreasSystem(Entity playerEntity, MapHudComponent hud) : base(new Matcher().one(typeof(BattleAreaComponent)))
        {
            _playerEntity = playerEntity;

            _blockEntity = playerEntity.scene.createEntity();
            _blockEntity.addComponent(new BoxCollider(0, 0, 16, Scene.virtualSize.Y));
            _blockEntity.enabled = false;

            _enemies = new List<Entity>();
            _enemiesToRemove = new List<Entity>();

            _mapHud = hud;

            _rocks = new List<Entity>();
            _rocksToRemove = new List<Entity>();

            _systemManager = Core.getGlobalManager<SystemManager>();
        }

        protected override void process(List<Entity> entities)
        {
            base.process(entities);

            if (_battleHappening)
            {
                updateBattle();
            }
        }

        private void updateBattle()
        {
            if (canThrowRocks())
            {
                _throwRockInterval -= Time.deltaTime;
                if (_throwRockInterval <= 0.0f)
                {
                    _throwRockInterval = 1 + Random.nextFloat(5);
                    throwRock();
                }
            }
            foreach (var rock in _rocks)
            {
                rock.position += new Vector2(0, 500 * Time.deltaTime);
                var col = rock.getComponent<BoxCollider>();
                CollisionResult res;
                if (col.collidesWith(_playerEntity.getComponent<BoxCollider>(), out res))
                {
                    _playerEntity.getComponent<BattleComponent>().onHit(res);
                }
                if (rock.position.Y > _systemManager.TiledMap.heightInPixels)
                    _rocksToRemove.Add(rock);
            }
            foreach (var rockEntity in _rocksToRemove)
            {
                rockEntity.destroy();
                _rocks.Remove(rockEntity);
            }   
            _rocksToRemove.Clear();

            if (_spawnInterval > 0.0f)
            {
                _spawnInterval -= Time.deltaTime;
                return;
            }
            
            foreach (var enemy in _enemies)
            {
                if (enemy.getComponent<BattleComponent>().Dying)
                {
                    _enemiesToRemove.Add(enemy);
                    _enemiesDefeated++;
                }
            }
            _enemiesToRemove.ForEach(x => _enemies.Remove(x));
            _enemiesToRemove.Clear();

            if (_currentWave >= _currentBattle.Waves.Length)
            {
                _waitingPlayerToCrossBarrier = true;
                _mapHud.showGo();
                resetBattle();
                return;
            }

            if (_enemiesDefeated >= _currentBattle.Waves[_currentWave])
            {
                _currentWave++;
                _enemiesDefeated = 0;
                _enemiesSpawned = 0;
                _spawnInterval = 0.5f;
            }

            if (_spawnInterval <= 0.0f && _enemiesSpawned < _currentBattle.Waves[_currentWave])
            {
                var playerScene = _playerEntity.scene as SceneMap;
                var enemyName = _currentBattle.Enemies.randomItem();
                var areaCollider = _currentBattle.collider;
                var areaBounds = areaCollider.bounds;

                // create enemy entity
                if (playerScene != null)
                {
                    EnemyComponent enemyComponent;
                    var enemy = playerScene.createEnemy(enemyName, false, out enemyComponent);
                    var widthOffset = areaBounds.width * 0.1f;
                    var positionX = areaBounds.x + widthOffset + Random.nextFloat(areaBounds.width - widthOffset * 2);
                    enemy.setPosition(positionX, areaBounds.y + areaCollider.height);

                    var spawnable = enemyComponent as ISpawnableEnemy;
                    if (spawnable != null)
                    {
                        spawnable.GoToSpawnState();
                        spawnable.MovableArea = areaBounds;
                    }

                    _enemies.Add(enemy);
                }

                _spawnInterval = 0.4f;
                _enemiesSpawned++;
            }
        }

        public override void process(Entity entity)
        {
            if (_battleHappening) return;

            var battleArea = entity.getComponent<BattleAreaComponent>();
            if (battleArea.Activated) return;

            var collider = entity.getComponent<BoxCollider>();

            var collisionRect = Physics.overlapRectangle(collider.bounds, 1 << SceneMap.PLAYER_LAYER);
            if (collisionRect != null)
            {
                startBattle(battleArea);
                _blockEntity.enabled = true;
                _blockEntity.setPosition(new Vector2(collider.absolutePosition.X + collider.width / 2, collider.bounds.y));
            }
        }

        protected override void lateProcess(List<Entity> entities)
        {
            base.lateProcess(entities);

            // Check if the player is colliding with the block entity
            if (!_battleHappening && !_waitingPlayerToCrossBarrier) return;

            CollisionResult collisionResult;
            if (_blockEntity.getComponent<BoxCollider>()
                .collidesWith(_playerEntity.getComponent<BoxCollider>(), out collisionResult))
            {
                if (_waitingPlayerToCrossBarrier)
                {
                    _waitingPlayerToCrossBarrier = false;
                    _mapHud.hideGo();
                }
                else
                {
                    _playerEntity.transform.position += collisionResult.minimumTranslationVector;
                }
            }
        }

        private void startBattle(BattleAreaComponent battleAreaComponent)
        {
            resetBattle();
            _battleHappening = true;
            _currentBattle = battleAreaComponent;
            _currentBattle.SetActivated();
        }

        private void resetBattle()
        {
            _battleHappening = false;
            _currentWave = 0;
            _spawnInterval = 0;
        }

        private void throwRock()
        {
            var playerScene = _playerEntity.scene as SceneMap;
            var areaBounds = _currentBattle.collider.bounds;

            // create enemy entity
            if (playerScene != null)
            {
                var rock = playerScene.createEntity();
                var rockTexture = playerScene.content.Load<Texture2D>(Content.Misc.rock);
                var rockSprite = rock.addComponent(new AnimatedSprite(rockTexture, "default"));
                rockSprite.CreateAnimation("default", 1.0f);
                rockSprite.AddFrames("default", new List<Rectangle>
                {
                    new Rectangle(0, 0, 16, 16)
                });

                var px = areaBounds.left + Random.nextInt((int)areaBounds.width);
                rock.position = new Vector2(px, 0);
                rock.addComponent(new BoxCollider(-7, -7, 14, 14));

                _rocks.Add(rock);
            }
        }

        private bool canThrowRocks()
        {
            return Core.getGlobalManager<SystemManager>().MapId >= 5;
        }
    }
}
