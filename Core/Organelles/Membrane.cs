﻿using AmoebaRL.Interfaces;
using AmoebaRL.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AmoebaRL.Core.Organelles
{
    public class Membrane : Upgradable
    {
        public Membrane()
        {
            Color = Palette.Membrane;
            Symbol = 'B';
            Name = "Membrane";
            Slime = true;
            Awareness = 0;
            PossiblePaths = new List<UpgradePath>()
            {
                new UpgradePath(2, CraftingMaterial.Resource.CALCIUM, () => new ReinforcedMembrane()),
                new UpgradePath(1, CraftingMaterial.Resource.ELECTRONICS, () => new Maw()),
            };
        }

        public override List<Item> OrganelleComponents() => new List<Item>() { new BarbedWire(), new Nutrient() };
    }

    public class ReinforcedMembrane : Membrane
    {
        public ReinforcedMembrane()
        {
            Color = Palette.Calcium;
            Symbol = 'B';
            Name = "Reinforced Membrane";
            Slime = true;
            Awareness = 0;
            PossiblePaths = new List<UpgradePath>()
            {
                new UpgradePath(3, CraftingMaterial.Resource.CALCIUM, () => new ForceField()),
                new UpgradePath(1, CraftingMaterial.Resource.ELECTRONICS, () => new NonNewtonianMembrane()),
            };
        }

        public override List<Item> OrganelleComponents()
        {
            List<Item> source = base.OrganelleComponents();
            source.AddRange(new[] { new CalciumDust(), new CalciumDust() });
            return source;
        }
    }

    public class Maw : Upgradable, IProactive
    {
        public Maw()
        {
            Color = Palette.Hunter;
            Symbol = 'B';
            Name = "Maw";
            Slime = true;
            Awareness = 1;
            PossiblePaths = new List<UpgradePath>()
            {
                new UpgradePath(3, CraftingMaterial.Resource.CALCIUM, () => new ReinforcedMaw()),
                new UpgradePath(3, CraftingMaterial.Resource.ELECTRONICS, () => new Tentacle()),
            };
        }

        public bool Act()
        {
            // Eat adjacent militia
            // throw new NotImplementedException();
            return false;
        }

        public override List<Item> OrganelleComponents() => new List<Item>() { new BarbedWire(), new Nutrient(), new SiliconDust() };
    }

    public class ForceField : ReinforcedMembrane
    {
        public ForceField()
        {
            Color = Palette.Calcium;
            Symbol = 'F';
            Name = "Force Field";
            Slime = true;
            Awareness = 0;
        }

        public override List<Item> OrganelleComponents()
        {
            List<Item> source = base.OrganelleComponents();
            source.AddRange(new[] { new CalciumDust(), new CalciumDust(), new CalciumDust() });
            return source;
        }
    }

    public class NonNewtonianMembrane : ReinforcedMembrane, IProactive
    {
        public NonNewtonianMembrane()
        {
            Color = Palette.Calcium;
            Symbol = 'B';
            Name = "Non-Newtonian Membrane";
            Slime = true;
            Awareness = 0;
        }

        public bool Act()
        {
            // Swap with adjacent cells if they are adjacent to enemies.
            throw new NotImplementedException();
        }

        public override List<Item> OrganelleComponents()
        {
            List<Item> source = base.OrganelleComponents();
            source.AddRange(new[] { new SiliconDust() });
            return source;
        }
    }

    public class ReinforcedMaw : Maw
    {
        public ReinforcedMaw()
        {
            Color = Palette.Calcium;
            Symbol = 'M';
            Name = "Reinforced Maw";
            Slime = true;
            Awareness = 1;
        }

        public override List<Item> OrganelleComponents()
        {
            List<Item> source = base.OrganelleComponents();
            source.AddRange(new[] { new CalciumDust(), new CalciumDust(), new CalciumDust() });
            return source;
        }
    }

    public class Tentacle : Maw
    {
        public Tentacle()
        {
            Color = Palette.Hunter;
            Symbol = 'T';
            Name = "Tentacle";
            Slime = true;
            Awareness = 3;
        }

        // Eat everything you see that you can. Retreat instead of dying.

        public override List<Item> OrganelleComponents()
        {
            List<Item> source = base.OrganelleComponents();
            source.AddRange(new[] { new SiliconDust(), new SiliconDust(), new SiliconDust() });
            return source;
        }
    }

    public class BarbedWire : Catalyst
    {
        public BarbedWire()
        {
            Color = Palette.MembraneInactive;
            Symbol = 'b';
            Name = "Barbed Wire";
        }

        public override Actor NewOrganelle()
        {
            return new Membrane();
        }
    }
}
