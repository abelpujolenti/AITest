using AI.Combat;
using AI.Combat.ScriptableObjects;
using UnityEngine;

namespace UnityEditor
{
    [CustomEditor(typeof(AIAttack))]
    public class DisplayAIAttackFieldsBySelection : Editor
    {
        public override void OnInspectorGUI()
        {
            AIAttack aiAttack = (AIAttack)target;

            aiAttack.totalDamage = (uint)EditorGUILayout.IntField("Total Damage", (int)aiAttack.totalDamage);
            aiAttack.minimumRangeCast = EditorGUILayout.FloatField("Minimum Range Cast", aiAttack.minimumRangeCast);
            aiAttack.maximumRangeCast = EditorGUILayout.FloatField("Maximum Range Cast", aiAttack.maximumRangeCast);
            aiAttack.doesRelativePositionToCasterChange = 
                EditorGUILayout.Toggle("Does Relative Position To Caster Change", aiAttack.doesRelativePositionToCasterChange);

            if (!aiAttack.doesRelativePositionToCasterChange)
            {
                aiAttack.relativePositionToCaster = 
                    EditorGUILayout.Vector3Field("Relative Position To Caster", aiAttack.relativePositionToCaster);    
            }
            
            aiAttack.attachToAttacker = EditorGUILayout.Toggle("Attach To Attacker", aiAttack.attachToAttacker);
            aiAttack.timeToCast = EditorGUILayout.FloatField("Time To Cast", aiAttack.timeToCast);
            
            aiAttack.doesDamageOverTime = EditorGUILayout.Toggle("Does Damage Over Time", aiAttack.doesDamageOverTime);

            if (!aiAttack.doesDamageOverTime)
            {
                aiAttack.timeDealingDamage = 0;
            }
            else
            {
                aiAttack.timeDealingDamage = EditorGUILayout.FloatField("Time Dealing Damage", aiAttack.timeDealingDamage);
            }
            aiAttack.cooldown = EditorGUILayout.FloatField("Cooldown", aiAttack.cooldown);
            aiAttack.itLandsInstantly = EditorGUILayout.Toggle("It Lands Instantly", aiAttack.itLandsInstantly);

            if (aiAttack.itLandsInstantly)
            {
                aiAttack.projectileSpeed = 0;
                aiAttack.doesProjectileExplodeOnAnyContact = false;
                aiAttack.startRelativePositionToCasterOfTheProjectile = Vector3.zero;
            }
            else
            {
                aiAttack.projectileSpeed = EditorGUILayout.FloatField("Projectile Speed", aiAttack.projectileSpeed);
                aiAttack.doesProjectileExplodeOnAnyContact = 
                    EditorGUILayout.Toggle("Does Projectile Explode On Any Contact", aiAttack.doesProjectileExplodeOnAnyContact);

                aiAttack.startRelativePositionToCasterOfTheProjectile = EditorGUILayout.Vector3Field(
                    "Start Relative Position To Caster Of The Projectile", aiAttack.startRelativePositionToCasterOfTheProjectile);
            }

            aiAttack.attackAoE.aiAttackAoEType = (AIAttackAoEType)EditorGUILayout.EnumPopup("Attack Aoe Type", aiAttack.attackAoE.aiAttackAoEType);
            
            aiAttack.height = EditorGUILayout.FloatField("Height", aiAttack.height);

            switch (aiAttack.attackAoE.aiAttackAoEType)
            {
                case AIAttackAoEType.RECTANGLE_AREA:
                    
                    aiAttack.attackAoE.width = EditorGUILayout.FloatField("Width", aiAttack.attackAoE.width);
                    aiAttack.attackAoE.length = EditorGUILayout.FloatField("Length", aiAttack.attackAoE.length);
                    aiAttack.attackAoE.direction = EditorGUILayout.Vector3Field("Direction", aiAttack.attackAoE.direction);
                    aiAttack.isRelativePositionXCenterOfColliderX = 
                        EditorGUILayout.Toggle("Relative Pos X Center Of Collider X", aiAttack.isRelativePositionXCenterOfColliderX);
                    
                    aiAttack.isRelativePositionYCenterOfColliderY = 
                        EditorGUILayout.Toggle("Relative Pos Y Center Of Collider Y", aiAttack.isRelativePositionYCenterOfColliderY);
                    
                    aiAttack.isRelativePositionZCenterOfColliderZ = 
                        EditorGUILayout.Toggle("Relative Pos Z Center Of Collider Z", aiAttack.isRelativePositionZCenterOfColliderZ);
                    break;
                
                case AIAttackAoEType.CIRCLE_AREA:
                    aiAttack.attackAoE.radius = EditorGUILayout.FloatField("Radius", aiAttack.attackAoE.radius);
                    break;
                
                case AIAttackAoEType.CONE_AREA:
                    aiAttack.attackAoE.length = EditorGUILayout.FloatField("Length", aiAttack.attackAoE.length);
                    aiAttack.attackAoE.direction = EditorGUILayout.Vector3Field("Direction", aiAttack.attackAoE.direction);
                    aiAttack.attackAoE.degrees = EditorGUILayout.FloatField("Degrees", aiAttack.attackAoE.degrees);
                    break;
            }

            if (GUI.changed)
            {
                EditorUtility.SetDirty(aiAttack);
            }
        }
    }
}