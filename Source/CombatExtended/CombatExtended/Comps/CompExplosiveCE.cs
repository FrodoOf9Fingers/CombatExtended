﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using RimWorld;
using Verse;
using UnityEngine;

namespace CombatExtended
{
    public class CompExplosiveCE : ThingComp
    {
    	private const float FragmentShadowChance = 0.2f;
    	
        public CompProperties_ExplosiveCE Props
        {
            get
            {
                return (CompProperties_ExplosiveCE)props;
            }
        }
		
        /// <summary>
        /// Produces a secondary explosion on impact using the explosion values from the projectile's projectile def. Requires the projectile's launcher to be passed on due to protection level. 
        /// Intended use is for HEAT and similar weapons that spawn secondary explosions while also penetrating, NOT explosive ammo of anti-materiel rifles as the explosion just spawns 
        /// on top of the pawn, not inside the hit body part.
        /// 
        /// Additionally handles fragmentation effects if defined.
        /// </summary>
        /// <param name="instigator">Launcher of the projectile calling the method</param>
		public virtual void Explode(Thing instigator, Vector3 pos, Map map, float scaleFactor = 1)
        {
			var posIV = pos.ToIntVec3();
            if (map == null)
            {
                Log.Warning("Tried to do explodeCE in a null map.");
                return;
            }
            if (!posIV.InBounds(map))
            {
                Log.Warning("Tried to explodeCE out of bounds");
                return;
            }
			
            // Fragmentation stuff
            if (!Props.fragments.NullOrEmpty() && GenGrid.InBounds(posIV, map))
            {
            	
                float edificeHeight = (new CollisionVertical(posIV.GetEdifice(map))).Max;
                Vector2 exactOrigin = new Vector2(pos.x, pos.z);
                float height;
                
                //Fragments fly from a 0 to 45 degree angle away from the explosion
                var range = new FloatRange(0, Mathf.PI / 8f);
                
                var projCE = parent as ProjectileCE;
                
                if (projCE != null)
                {
                	height = Mathf.Max(edificeHeight, pos.y);
                	if (edificeHeight < height)
                	{
                		//If the projectile exploded above the ground, they can fly 45 degree away at the bottom as well
                		range.min = -Mathf.PI / 8f;
                	}
                	// TODO : Check for hitting the bottom or top of a roof
                }
                else
                {
                	height = edificeHeight;
                }
                
                foreach (ThingCountClass fragment in Props.fragments)
                {
                    for (int i = 0; i < fragment.count; i++)
                    {
                        ProjectileCE projectile = (ProjectileCE)ThingMaker.MakeThing(fragment.thingDef, null);
                        GenSpawn.Spawn(projectile, posIV, map);
                        
                        projectile.canTargetSelf = true;
            			projectile.minCollisionSqr = 1f;
            				//TODO : Don't hardcode at FragmentShadowChance, make XML-modifiable
            			projectile.castShadow = (UnityEngine.Random.value < FragmentShadowChance);
            			projectile.Launch(instigator, exactOrigin, range.RandomInRange, UnityEngine.Random.Range(0, 360), height, Props.fragSpeedFactor * projectile.def.projectile.speed);
                    }
                }
            }
            // Regular explosion stuff
            if (Props.explosionRadius > 0 && Props.explosionDamage > 0 && parent.def != null && GenGrid.InBounds(posIV, map))
            {
                // Can't use GenExplosion because it no longer allows setting damage amount

                // Copy-paste from GenExplosion
                Explosion explosion = new Explosion();
                explosion.position = posIV;
                explosion.radius = Props.explosionRadius * scaleFactor;
                explosion.damType = Props.explosionDamageDef;
                explosion.instigator = instigator;
                explosion.damAmount = GenMath.RoundRandom(Props.explosionDamage * scaleFactor);
                explosion.weaponGear = null;
                explosion.preExplosionSpawnThingDef = Props.preExplosionSpawnThingDef;
                explosion.preExplosionSpawnChance = Props.explosionSpawnChance;
                explosion.preExplosionSpawnThingCount = Props.preExplosionSpawnThingCount;
                explosion.postExplosionSpawnThingDef = Props.postExplosionSpawnThingDef;
                explosion.postExplosionSpawnChance = Props.postExplosionSpawnChance;
                explosion.postExplosionSpawnThingCount = Props.postExplosionSpawnThingCount;
                explosion.applyDamageToExplosionCellsNeighbors = Props.applyDamageToExplosionCellsNeighbors;
                map.GetComponent<ExplosionManager>().StartExplosion(explosion, Props.soundExplode ?? Props.explosionDamageDef.soundExplosion);
            }
        }
    }
}
