﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DuckGame.MyMod
{
    [BaggedProperty("isInDemo", true), BaggedProperty("canSpawn", true), BaggedProperty("isOnlineCapable", true), EditorGroup("Shewi|explosives")]
    public class TunnelGrenadeActive : Gun
    {
        private Duck d;
        public StateBinding _timerBinding = new StateBinding("_timer", -1, false);

        public StateBinding _pinBinding = new StateBinding("_pin", -1, false);

        private SpriteMap _sprite;

        public bool _pin = true;

        public float _timer = 2f;

        private Duck _cookThrower;

        private float _cookTimeOnThrow;

        public bool pullOnImpact;

        private bool _explosionCreated;

        private bool _localDidExplode;

        private bool _didBonus;

        private static int grenade;

        public int gr;

        public int _explodeFrames = -1;

        public Duck cookThrower
        {
            get
            {
                return this._cookThrower;
            }
        }

        public float cookTimeOnThrow
        {
            get
            {
                return this._cookTimeOnThrow;
            }
        }

        public TunnelGrenadeActive(float xval, float yval) : base(xval, yval)
        {
            d = duck;
            this.ammo = 1;
            this._ammoType = new ATShrapnel();
            this._ammoType.penetration = 0.4f;
            this._type = "gun";
            //this.ignoreCollisions = true;
            //this._impactThreshold = 500f;

            this._sprite = new SpriteMap(GetPath("weapons/TunnelGrenade"), 32, 32, true);
            base.graphic = this._sprite;
            _sprite.AddAnimation("animation", 1.6f, true, 0, 1, 2, 3, 4);


            this.center = new Vec2(16f, 16f);
            this.collisionOffset = new Vec2(-4f, -5f);
            this.collisionSize = new Vec2(8f, 10f);
            base.bouncy = 0.7f;
            this.friction = 0.025f;
            this.weight = 0.1f;
            this._weight = 0.1f;
            this._editorName = "(teleport) Tunnel Grenade";
            this._bio = "Like a Grenade, teleports the duck who threw it!";
        }

        public override void Initialize()
        {
            this.gr = TunnelGrenadeActive.grenade;
            TunnelGrenadeActive.grenade++;
            _sprite.SetAnimation("animation");
        }

        public override void OnNetworkBulletsFired(Vec2 pos)
        {
            this._pin = false;
            this._localDidExplode = true;
            if (!this._explosionCreated)
            {
                Graphics.flashAdd = 1.3f;
                Layer.Game.darken = 1.3f;
            }
            this.CreateExplosion(pos);
        }

        public void CreateExplosion(Vec2 pos)
        {
            if (!this._explosionCreated)
            {
                float cx = pos.x;
                float cy = pos.y - 2f;
                Level.Add(new ExplosionPart(cx, cy, true));
                int num = 6;
                if (Graphics.effectsLevel < 2)
                {
                    num = 3;
                }
                for (int i = 0; i < num; i++)
                {
                    float dir = (float)i * 60f + Rando.Float(-10f, 10f);
                    float dist = Rando.Float(12f, 20f);
                    ExplosionPart ins = new ExplosionPart(cx + (float)(System.Math.Cos((double)Maths.DegToRad(dir)) * (double)dist), cy - (float)(System.Math.Sin((double)Maths.DegToRad(dir)) * (double)dist), true);
                    Level.Add(ins);
                }
                this._explosionCreated = true;
                SFX.Play("explode", 0.7f, 0f, 0f, false);
            }
        }

        public override void Update()
        {
            base.Update();
            if (!this._pin)
            {
                this._timer -= 0.01f;
            }
            if (this._timer < 0.5f && this.owner == null && !this._didBonus)
            {
                this._didBonus = true;
                if (Recorder.currentRecording != null)
                {
                    Recorder.currentRecording.LogBonus();
                }
            }
            if (!this._localDidExplode && this._timer < 0f)
            {
                if (this._explodeFrames < 0)
                {
                    this.CreateExplosion(this.position);
                    this._explodeFrames = 4;
                }
                else
                {
                    this._explodeFrames--;
                    if (this._explodeFrames == 0)
                    {
                        float cx = base.x;
                        float cy = base.y - 2f;
                        Graphics.flashAdd = 1.3f;
                        Layer.Game.darken = 1.3f;
                        if (base.isServerForObject)
                        {

                            for (int i = 0; i < 20; i++)
                            {
                                SFX.Play(GetPath("/sfx/teleport"), 1f, 0f, 0f, false);

                                if (duck != null)
                                    d = duck;
                                else if (prevOwner != null)
                                    d = (Duck) prevOwner;
                                else if (_equippedDuck != null)
                                    d = _equippedDuck;

                                Level.Add(SmallSmoke.New(d.position.x, d.position.y));
                                if (d.ragdoll != null)
                                {
                                    d.ragdoll.position.x = this.position.x;
                                    d.ragdoll.position.y = this.position.y - 10f;
                                }
                                else
                                {
                                    d.position.x = this.position.x;
                                    d.position.y = this.position.y - 10f;
                                }

                                Level.Add(SmallSmoke.New(d.position.x, d.position.y));
                                d.vSpeed = -0.5f;

                                Level.Remove(this);
                                base._destroyed = true;
                                this._explodeFrames = -1;
                            }
                            System.Collections.Generic.IEnumerable<Window> windows = Level.CheckCircleAll<Window>(this.position, 40f);
                            foreach (Window w in windows)
                            {
                                if (Level.CheckLine<Block>(this.position, w.position, w) == null)
                                {
                                    w.Destroy(new DTImpact(this));
                                }
                            }
                            this.bulletFireIndex += 20;
                            if (Network.isActive)
                            {
                                NMFireGun gunEvent = new NMFireGun(this, this.firedBullets, this.bulletFireIndex, false, 4, false);
                                Send.Message(gunEvent, NetMessagePriority.ReliableOrdered, null);
                                this.firedBullets.Clear();
                            }
                        }

                    }
                }

                if (base.prevOwner != null && this._cookThrower == null)
                {
                    this._cookThrower = (base.prevOwner as Duck);
                    this._cookTimeOnThrow = this._timer;
                }

            }
        }

        public override void OnImpact(MaterialThing with, ImpactedFrom from)
        {
            if (this.pullOnImpact)
            {
                this.OnPressAction();
            }
            base.OnSolidImpact(with, from);
        }

        public override void OnPressAction()
        {
            if (this._pin)
            {
                this._pin = false;
                Level.Add(new GrenadePin(base.x, base.y)
                {
                    hSpeed = (float)(-(float)this.offDir) * (1.5f + Rando.Float(0.5f)),
                    vSpeed = -2f
                });
                SFX.Play("pullPin", 1f, 0f, 0f, false);
            }
        }
    }
}