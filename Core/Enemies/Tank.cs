﻿using AmoebaRL.Core.Organelles;
using AmoebaRL.UI;
using RogueSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AmoebaRL.Core
{
    public class Tank : Militia
    {
        int StaminaPoolSize = 2;

        int Stamina = 2;

        public Tank()
        {
            Awareness = 3;
            Color = Palette.RestingTank;
            Symbol = 't';
            Speed = 16;
            Name = "Tank";
        }

        public override string GetDescription()
        {
            return "A terrifying fortress wrapped in strong armor. It cannot be killed or eaten by most means, although all that armor means it can only act every other turn. " +
                "Fortunately, friendly fire is not conventional means, and it is vulnerable to it. It can also be engulfed by surrounding it on all sides with slime or walls." +
                "Cities and humans will not help to engulf tanks!";
        }

        public override bool Act()
        {
            if(!Engulf())
            {
                
                if (Stamina == 1)
                {
                    Color = Palette.Calcium;
                }
                if (Stamina > 0)
                {
                    Stamina--;
                }
                else
                {
                    Color = Palette.RestingTank;
                    Stamina = StaminaPoolSize;
                    return base.Act();
                }
            }
            return true;
        }

        public override void Die()
        {
            Game.DMap.RemoveActor(this);
            ICell drop = Game.DMap.NearestLootDrop(X, Y);
            CalciumDust transformation = new CalciumDust
            {
                X = drop.X,
                Y = drop.Y
            };
            Game.DMap.AddItem(transformation);
        }

        public override void OnEaten()
        {
            Game.DMap.RemoveActor(this);
            CapturedTank transformation = new CapturedTank
            {
                X = X,
                Y = Y
            };
            Game.DMap.AddActor(transformation);
        }

        public class CapturedTank : CapturedMilitia
        {
            public CapturedTank()
            {
                Awareness = 0;
                Slime = true;
                Color = Palette.Calcium;
                Name = "Dissolving Tank";
                Symbol = 't';
                MaxHP = 24;
                HP = MaxHP;
                Speed = 16;
                // Already called by parent?
                // Game.PlayerMass.Add(this);
            }

            public override string GetDescription()
            {
                return $"Much less scary now that its armor has been overcome. " + DissolvingAddendum();
            }

            public override string NameOfResult { get; set; } = "calcium";

            public override Actor DigestsTo() => new Calcium();

            public override void OnUnslime() => BecomeActor(new Tank());


            public override void OnDestroy() => BecomeActor(new Tank());
        }
    }
}
