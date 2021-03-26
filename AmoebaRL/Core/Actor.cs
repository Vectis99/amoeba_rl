﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RLNET;
using RogueSharp;
using AmoebaRL.Interfaces;
using AmoebaRL.UI;
using AmoebaRL.Core.Enemies;

namespace AmoebaRL.Core
{
    /// <summary>
    /// Autonomous <see cref="Entity"/> which has a proactive impact on the map.
    /// </summary>
    public class Actor : IActor, IDrawable, ISchedulable
    {
        // IActor
        /// <summary>Flavor only, displayed in text log contexts.</summary>
        public string Name { get; set; } = "Actor";

        /// <summary>
        /// Maximum range field of view can be calculated at for this actor.
        /// <list type="bullet">
        ///     <item> 0 : Can see self </item>
        ///     <item> Below 0 : Cannot see anything </item>
        /// </list>
        /// </summary>
        public int Awareness { get; set; } = 0;

        /// <summary>
        /// The time between turns this actor will take a turn when it appears in <see cref="Systems.SchedulingSystem"/>
        /// </summary>
        public int Delay { get; set; } = 16;

        /// <summary>
        /// Visual indicator for the background.
        /// <list type="bullet">
        ///     <item> 0 : Default floor </item>
        ///     <item> 1 : Dark green </item>
        ///     <item> 2 : Dark green </item>
        /// </list>
        /// </summary>
        /// <remarks>May later be used as a shorthand for faction.</remarks>
        public int Slime { get; set; } = 0;

        // IDrawable
        public RLColor Color { get; set; } = Palette.Cursor;

        /// <inheritdoc/>
        public VisibilityCondition Visibility { get; set; } = VisibilityCondition.LOS_ONLY;

        /// <inheritdoc/>
        public char Symbol { get; set; } = '?';

        /// <inheritdoc/>
        public int X { get; set; } = 0;

        /// <inheritdoc/>
        public int Y { get; set; } = 0;

        /// <inheritdoc/>
        public void Draw(RLConsole console, IMap map)
        {
            if(Visibility == VisibilityCondition.ALWAYS_VISIBLE ||
                (Visibility == VisibilityCondition.LOS_ONLY && map.IsInFov(X, Y)))
            {
                DrawSelfGraphic(console, map);
                return;
            }
            
            if(map.IsExplored(X, Y))
            {
                if (Visibility == VisibilityCondition.EXPLORED_ONLY)
                    DrawSelfGraphicMemory(console, map);
                else
                    console.Set(X, Y, Palette.Floor, Palette.FloorBackground, '.');
            }
        }

        /// <summary>
        /// Draws the graphical representation of this as if it was observed directly.
        /// </summary>
        /// <param name="console">Drawing canvas.</param>
        /// <param name="map">The game area the drawing is done in the context of.</param>
        protected virtual void DrawSelfGraphic(RLConsole console, IMap map)
        {
            if (Slime == 1)
                console.Set(X, Y, Color, Palette.BodySlime, Symbol);
            else if (Slime == 2)
                console.Set(X, Y, Color, Palette.PathSlime, Symbol);
            else
                console.Set(X, Y, Color, Palette.FloorBackgroundFov, Symbol);
            return;
        }

        /// <summary>
        /// Draws the graphical representation of this as if it was remembered, but is not directly visible.
        /// </summary>
        /// <param name="console">Drawing canvas.</param>
        /// <param name="map">The game area the drawing is done in the context of.</param>
        protected virtual void DrawSelfGraphicMemory(RLConsole console, IMap map)
        {
            DrawSelfGraphic(console, map);
        }



        // ISchedulable
        /// </inheritdoc>
        public int Time
        {
            get
            {
                return Delay;
            }
        }

        #region Helpers
        /// <summary>
        /// Transforms this actor into a single <see cref="Item"/> and places it in the nearest available spot on the map.
        /// <seealso cref="DungeonMap.NearestLootDrop(int, int)"/>
        /// </summary>
        /// <param name="i">The <see cref="Item"/> to transform into.</param>
        public void BecomeItem(Item i)
        {
            ICell lands = Game.DMap.NearestLootDrop(X, Y);
            i.X = lands.X;
            i.Y = lands.Y;
            Game.DMap.AddItem(i);
        }

        /// <summary>
        /// Transforms this actor into a set of <see cref="Item"/>s and places it in the nearest available spots on the map.
        /// <seealso cref="DungeonMap.NearestLootDrop(int, int)"/>
        /// </summary>
        /// <param name="items">The <see cref="Item"/>s to transform into.</param>
        public void BecomeItems(IEnumerable<Item> items)
        {
            List<ICell> alreadyTriedDrop = new List<ICell>();
            List<ICell> alreadyTriedDropPerimeter = new List<ICell>();
            List<ICell> nextAvailable = new List<ICell>();
            foreach (Item i in items)
            {
                if (nextAvailable.Count == 0)
                    nextAvailable = Game.DMap.NearestLootDrops(X, Y, alreadyTriedDrop, alreadyTriedDropPerimeter);
                if (nextAvailable.Count > 0)
                {
                    int picker = Game.Rand.Next(nextAvailable.Count - 1);
                    ICell lands = nextAvailable[picker];
                    nextAvailable.RemoveAt(picker);
                    i.X = lands.X;
                    i.Y = lands.Y;
                    Game.DMap.AddItem(i);
                }
                else
                {
                    Game.MessageLog.Add($"The {i.Name} had nowhere is crushed!");
                }
            }
        }

        /// <summary>
        /// Replace this <see cref="Actor"/> with another.
        /// </summary>
        /// <param name="a">The replacement</param>
        /// <returns><paramref name="a"/></returns>
        public virtual Actor BecomeActor(Actor a)
        {
            a.X = X;
            a.Y = Y;
            Game.DMap.AddActor(a);
            return a;
        }

        /// <summary>
        /// Performs a field of view calculation originating from <see cref="X"/>, <see cref="Y"/> with a range <see cref="Awareness"/>.
        /// </summary>
        /// <param name="lightWalls">Whether to include the first non-transparent cell hit in each FOV trace in the output.</param>
        /// <returns>Result of calculation.</returns>
        public FieldOfView FOV(bool lightWalls = true)
        {
            // When replacing static handles, Instances of Game.DMap
            // should instead be replaced by object references to this class.
            FieldOfView selfFOV = new FieldOfView(Game.DMap);
            selfFOV.ComputeFov(X, Y, Awareness, lightWalls);
            return selfFOV;
        }

        /// <summary>
        /// Determines which of <paramref name="testSightTo"/> are in <see cref="FOV(bool)"/>.
        /// </summary>
        /// <param name="testSightTo">The possible set of actors which might be in <see cref="FOV(bool)"/> to include in the output.</param>
        /// <param name="lightWalls">Whether to include the first non-transparent actor hit in each FOV trace in the output.</param>
        /// <returns>All of the <see cref="Actor"/>s which are in both <see cref="FOV(bool)"/> and <paramref name="testSightTo"/>.</returns>
        public List<Actor> Seen(List<Actor> testSightTo, bool lightWalls = true)
        {
            List<Actor> seenTargets = new List<Actor>();
            FieldOfView selfFOV = FOV(lightWalls);
            foreach (Actor a in testSightTo)
            {
                if (selfFOV.IsInFov(a.X, a.Y))
                    seenTargets.Add(a);
            }
            return seenTargets;
        }

        /// <summary>
        /// Take a single step to be as far away as possible from <paramref name="sources"/>.
        /// This does not use a safety map implementation; this is deliberately a flawed algorithm in this sense;
        /// compute a separate djikstra map for this.
        /// </summary>
        /// <param name="sources">Things to move away from.</param>
        /// <param name="canPassThroughOthers">Whether this step can include spaces occupied by <see cref="NPC"/>s or <see cref="Organelles.Organelle"/>s.</param>
        /// <returns>The location moved to; can be own cell if no further location exists.</returns>
        public ICell ImmediateUphillStep(IEnumerable<Actor> sources, bool canPassThroughOthers)
        {
            int mySafety = 0;
            foreach (Actor t in sources)
                mySafety += DungeonMap.TaxiDistance(t, this);


            // Find the safest sacrifice.
            int safestSacrificeVal = -1;
            List<Actor> safestSacrifices = new List<Actor>();
            List<Actor> sacrifices = null;
            if (canPassThroughOthers)
            {
                sacrifices = Game.DMap.AdjacentActors(X, Y).Where(a => !sources.Contains(a) && !(a is City)).ToList();
                foreach (Actor s in sacrifices)
                {
                    int safety = 0;
                    foreach (Actor t in sources)
                        safety += DungeonMap.TaxiDistance(t, s);
                    if (safety >= safestSacrificeVal)
                    {
                        safestSacrificeVal = safety;
                        safestSacrifices.Add(s);
                    }
                }
            }

            // Find the safest place to walk to.
            List<ICell> freeSpaces = Game.DMap.AdjacentWalkable(X, Y);
            List<ICell> safestFreeSpaces = new List<ICell>();
            int safestFreeSpaceVal = 0;
            foreach (ICell s in freeSpaces)
            {
                int safety = 0;
                foreach (Actor t in sources)
                    safety += DungeonMap.TaxiDistance(Game.DMap.GetCell(t.X, t.Y), s);
                if (safety >= safestFreeSpaceVal)
                {
                    safestFreeSpaceVal = safety;
                    safestFreeSpaces.Add(s);
                }
            }

            // If waiting is the safest option, return false.
            if (mySafety >= safestSacrificeVal && mySafety >= safestFreeSpaceVal)
                return Game.DMap.GetCell(X, Y);

            // Otherwise, move to the safest spot and return true.
            bool takeSacrifice = safestSacrificeVal > safestFreeSpaceVal;
            if (safestFreeSpaceVal == safestSacrificeVal)
            {
                takeSacrifice = Game.Rand.Next(1) == 0;
            }
            if (takeSacrifice)
            {
                Actor picked = safestSacrifices[Game.Rand.Next(safestSacrifices.Count - 1)];
                ICell targ = Game.DMap.GetCell(picked.X, picked.Y);
                return targ; // Game.CommandSystem.AttackMoveOrganelle(this, targ.X, targ.Y);
            }
            else
            {
                ICell targ = safestFreeSpaces[Game.Rand.Next(safestFreeSpaces.Count - 1)];
                return targ; // Game.CommandSystem.AttackMoveOrganelle(this, targ.X, targ.Y);
            }
        }

        /// <summary>
        /// Determines whether a path exists from this to a specified location that is not obstructed.
        /// </summary>
        /// <param name="ignoreIf">Criteria by which a cell may be considered to not be an obstruction. 
        /// Empty locations or locations containing only <see cref="Item"/>s or <see cref="VFX"/>s are never obstructed by default.</param>
        /// <param name="x">Horizontal coordinate of target location.</param>
        /// <param name="y">Vertical coordinate of target location.</param>
        /// <returns>A path exists from this to a specified location that is not obstructed.</returns>
        public bool PathExists(Func<Actor, bool> ignoreIf, int x, int y)
        {
            return PathIgnoring(ignoreIf, x, y) != null;
        }

        /// <summary>
        /// Calculates a shortest contiguous group of adjacent cells between this space and a target location.
        /// Cannot contain obstructed locations, except that it always includes the location of this and the target location.
        /// </summary>
        /// <param name="ignoreIf">Criteria by which a cell may be considered to not be an obstruction.
        /// Empty locations or locations containing only <see cref="Item"/>s or <see cref="VFX"/>s are never obstructed by default.</param>
        /// <param name="x">Horizontal coordinate of target location.</param>
        /// <param name="y">Vertical coordinate of target location.</param>
        /// <returns>A shortest contiguous group of adjacent cells between this space and a target location that is not obstructed.
        /// Null if none exists.</returns>
        public Path PathIgnoring(Func<Actor,bool> ignoreIf, int x, int y)
        {
            IEnumerable<Actor> ignore = Game.DMap.Actors.Where(ignoreIf);
            List<bool> wasAlreadyIgnored = new List<bool>();
            foreach (Actor toIgnore in ignore)
            {
                wasAlreadyIgnored.Add(Game.DMap.IsWalkable(toIgnore.X, toIgnore.Y));
                Game.DMap.SetIsWalkable(toIgnore.X, toIgnore.Y, true);
            }

            Path found = null;
            try
            {
                //found = f.ShortestPath(
                //    Game.DMap.GetCell(X, Y),
                //    Game.DMap.GetCell(x, y)
                //);
                found = DungeonMap.QuickShortestPath(Game.DMap, Game.DMap.GetCell(X, Y), Game.DMap.GetCell(x, y));
            }
            catch (PathNotFoundException)
            {
                
            }

            IEnumerator<bool> alreadyIgnored = wasAlreadyIgnored.GetEnumerator();
            foreach (Actor toIgnore in ignore)
            {
                alreadyIgnored.MoveNext();
                Game.DMap.SetIsWalkable(toIgnore.X, toIgnore.Y, alreadyIgnored.Current);
            }


            return found;

        }

        /// <summary>
        /// Always returns false, regardless of <paramref name="discard"/>.
        /// </summary>
        /// <param name="discard">Discarded.</param>
        /// <returns><c>false</c></returns>
        protected static bool IgnoreNone(Actor discard) => false;

        /// <summary>
        /// Finds the list of shortest <see cref="Path"/>s from this to each of <paramref name="potentialTargets"/>.
        /// This only includes <see cref="Path"/>s that could be generated without obstructions.
        /// An obstruction includes a location which is not <see cref="Map.IsWalkable(int, int)"/>.
        /// </summary>
        /// <param name="potentialTargets">The <see cref="Actor"/>s to calculate paths to.</param>
        /// <returns>The unobstructed <see cref="Path"/>s to <paramref name="potentialTargets"/> with the minimum length.</returns>
        public List<Path> PathsToNearest(List<Actor> potentialTargets) => PathsToNearest(potentialTargets, IgnoreNone);

        /// <summary>
        /// Finds the list of shortest <see cref="Path"/>s from this to each of <paramref name="potentialTargets"/>.
        /// This only includes <see cref="Path"/>s that could be generated without obstructions.
        /// An obstruction includes a location which is not <see cref="Map.IsWalkable(int, int)"/> and does not meet <paramref name="ignoring"/>.
        /// </summary>
        /// <param name="potentialTargets">The <see cref="Actor"/>s to calculate paths to.</param>
        /// <returns>The unobstructed <see cref="Path"/>s to <paramref name="potentialTargets"/> with the minimum length.</returns>
        public List<Path> PathsToNearest(List<Actor> potentialTargets, Func<Actor, bool> ignoring)
        {
            List<Path> nearestPaths = new List<Path>();
            Path attempt;
            int nearestTargetDistance = int.MaxValue;
            foreach (Actor candidate in potentialTargets)
            {
                attempt = null;
                try
                {
                    attempt = PathIgnoring(ignoring, candidate.X, candidate.Y);
                }
                catch (PathNotFoundException) { }
                if (attempt != null)
                {
                    if (attempt.Length <= nearestTargetDistance)
                    {
                        if (attempt.Length < nearestTargetDistance)
                        {
                            nearestPaths.Clear();
                            nearestTargetDistance = attempt.Length;
                        }
                        nearestPaths.Add(attempt);
                    }
                }
            }
            return nearestPaths;
        }

        /// <summary>
        /// Determines whether this is exactly one unit away from a provided location.
        /// </summary>
        /// <param name="tx">The horizontal coordinate of the location to be checked for adjacency.</param>
        /// <param name="ty">The vertical coordinate of the location to be checked for adjacency.</param>
        /// <returns>(<see cref="X"/>, <see cref="Y"/>) is adjacent to (<paramref name="tx"/>, <paramref name="ty"/>)</returns>
        public bool AdjacentTo(int tx, int ty)
        {
            if (Math.Abs(X - tx) + Math.Abs(Y - ty) == 1)
                return true;
            return false;
        }
        #endregion
    }
}
