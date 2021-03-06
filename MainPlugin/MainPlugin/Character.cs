﻿using System;
using DarkRift;
using UnityEngine;

namespace MainPlugin
{
    public class Character
    {
        public Player Owner;

        public STransform Transform;
        public Collider Collider;
        public Vector2 Velocity;
        public Game Game;
        public bool Grounded;
        public bool WalkL;
        public bool WalkR;
        public bool Jumped;
        
        private const float gravity = 10f;
        private const float movementSpeed = 3.5f;
        private const float jumpStrenght = 8f;

        public int Hp;
        public int Kills;

        public int LastCastArrow;
        public int LastCastBeacon;

        public bool WasEnlighted;


        public static Character Create(Vector2 position, Game game, Player owner)
        {
            Character c = new Character();
            c.Owner = owner;
            c.Transform = new STransform(position,0);
            c.Velocity = new Vector2(0,0);
            c.Game = game;
            c.Collider = new BoxCollider(c.Transform, new Vector2(1f,1f));

            //send message to all playing players
            DarkRiftWriter writer = DarkRiftWriter.Create();
            writer.Write(c.Owner.Name);
            writer.Write(c.Owner.PlayerId);
            game.SendMessageToAll(Message.Create((ushort)Tags.SpawnNewCharacter, writer));

            c.Hp = 100;
            c.LastCastArrow = -10000;
            c.LastCastBeacon = -10000;

            return c;
        }

        public void DisposeCharacter()
        {
            DarkRiftWriter writer = DarkRiftWriter.Create();
            writer.Write(Owner.PlayerId);
            Game.SendMessageToAll(Message.Create((ushort)Tags.PlayerDisconnect, writer));
        }


        public void TakeDmg(int dmg, Character dealer)
        {
            Hp -= dmg;

            if (Hp <= 0)
            {
                dealer.Kills++;
                Hp = 100;
                //respawn
                Transform.Position = Game.GetRandomSpawnPoint();
                Game.UpdateMessage.AddCharacterPosUpdate(Owner.PlayerId, Transform.Position);
            }
            else
            {
                Blood.FireBlood(Transform.Position, Game, this);
            }
        }


        public void Tick()
        {
            if (Hp < 100 && Game.Frame % 40 == 0)
            {
                Hp += 1;
            }
            if (Hp < 70 && Game.Frame % 300 == 0)
            {
                Blood.FireBlood(Transform.Position, Game, this);
            }
            else if (Hp < 45 && Game.Frame % 120 == 0)
            {
                Blood.FireBlood(Transform.Position, Game, this);
            }

            Vector2 oldPos = Transform.Position;
            if (Grounded && Jumped)
            {
                Velocity.y += jumpStrenght;
            }
            Grounded = false;
            Jumped = false;
            Velocity.y -= gravity * Clock.DeltaTime;

            Transform.Translate(new Vector2(0, Velocity.y* Clock.DeltaTime));
            Collider CollidedMapObjectVert = Game.CollideWithMapReturnCollider(Collider);
            if (CollidedMapObjectVert != null)
            {
                float ResetDistance = 0;
                if (CollidedMapObjectVert.GetType() == typeof(BoxCollider))
                {
                    ResetDistance = ((BoxCollider)CollidedMapObjectVert).Size.y / 2 + ((BoxCollider)Collider).Size.y / 2 + 0.001f;
                }
                else if (CollidedMapObjectVert.GetType() == typeof(BoxCollider))
                {
                    ResetDistance = ((CircleCollider)CollidedMapObjectVert).Radius + ((BoxCollider)Collider).Size.y / 2 + 0.001f;
                }
                if (Velocity.y <= 0)
                {
                    Transform.Translate(new Vector2(0, CollidedMapObjectVert.Transform.Position.y - Transform.Position.y + ResetDistance));
                    Grounded = true;
                }
                else
                {
                    Transform.Translate(new Vector2(0, CollidedMapObjectVert.Transform.Position.y - Transform.Position.y - ResetDistance));
                }

                //Transform.Translate(new Vector2(0, -Velocity.y*Clock.DeltaTime));
                //if (Velocity.y <= 0)
                //{
                //    Grounded = true;
                //}
                Velocity.y = 0;
            }

            float walkVelocity = 0;
            if (WalkL)
            {
                walkVelocity -= movementSpeed * Clock.DeltaTime;
            }
            if (WalkR)
            {
                walkVelocity += movementSpeed * Clock.DeltaTime;
            }

            Transform.Translate(new Vector2(Velocity.x*Clock.DeltaTime+walkVelocity,0));
            Collider CollidedMapObjectHor = Game.CollideWithMapReturnCollider(Collider);
            if (CollidedMapObjectHor != null)
            {
                float ResetDistance = 0;
                if (CollidedMapObjectHor.GetType() == typeof(BoxCollider))
                {
                    ResetDistance = ((BoxCollider)CollidedMapObjectHor).Size.x / 2 + ((BoxCollider)Collider).Size.x / 2 + 0.001f;
                }
                else if (CollidedMapObjectHor.GetType() == typeof(CircleCollider))
                {
                    ResetDistance = ((CircleCollider)CollidedMapObjectHor).Radius + ((BoxCollider)Collider).Size.x / 2 + 0.001f;
                }
                if (walkVelocity < 0)
                {
                    Transform.Translate(new Vector2(CollidedMapObjectHor.Transform.Position.x - Transform.Position.x + ResetDistance, 0));
                }
                else
                {
                    Transform.Translate(new Vector2(CollidedMapObjectHor.Transform.Position.x - Transform.Position.x - ResetDistance, 0));
                }
                //Transform.Translate(new Vector2(-Velocity.x * Clock.DeltaTime - walkVelocity, 0));
                Velocity.x = 0;
            }

           

            if (/*oldPos != Transform.Position*/true )
            {
                if (Game.IsEnlighted(Collider))
                {
                    Game.UpdateMessage.AddCharacterPosUpdate(Owner.PlayerId, Transform.Position);
                    WasEnlighted = true;
                }
                else
                {
                    DarkRiftWriter writer = DarkRiftWriter.Create();
                    if (WasEnlighted)
                    {
                        WasEnlighted = false;
                        writer.Write(Owner.PlayerId);
                        Game.SendMessageToAll(Message.Create((ushort)Tags.MakeInvisisble,writer)); ;
                    }
                    writer = DarkRiftWriter.Create();   
                    writer.Write(Transform.Position.x);
                    writer.Write(Transform.Position.y);
                    Owner.Client.SendMessage(Message.Create((ushort) Tags.WalkInvisible, writer), SendMode.Reliable);

                }

            }

        }
    }
}
