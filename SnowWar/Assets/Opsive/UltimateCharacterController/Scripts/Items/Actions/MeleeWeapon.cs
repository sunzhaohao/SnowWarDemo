﻿/// ---------------------------------------------
/// Ultimate Character Controller
/// Copyright (c) Opsive. All Rights Reserved.
/// https://www.opsive.com
/// ---------------------------------------------

using UnityEngine;
using Opsive.UltimateCharacterController.Character;
using Opsive.UltimateCharacterController.Character.Abilities.Items;
using Opsive.UltimateCharacterController.Events;
using Opsive.UltimateCharacterController.Game;
using Opsive.UltimateCharacterController.Inventory;
using Opsive.UltimateCharacterController.Items.AnimatorAudioStates;
using Opsive.UltimateCharacterController.Objects.ItemAssist;
using Opsive.UltimateCharacterController.StateSystem;
using Opsive.UltimateCharacterController.SurfaceSystem;
using Opsive.UltimateCharacterController.Traits;
using Opsive.UltimateCharacterController.Utility;
using System.Collections.Generic;

namespace Opsive.UltimateCharacterController.Items.Actions
{
    /// <summary>
    /// Any weapon that can melee. This includes a knife, baseball bat, mace, etc.
    /// </summary>
    public class MeleeWeapon : UsableItem
    {
        /// <summary>
        /// Extends the Hitbox class for use by melee weapons.
        /// </summary>
        [System.Serializable]
        public class MeleeHitbox : Hitbox
        {
            [Tooltip("The hitbox can detect collisions if the local vertical offset is greater than the specified value relative to the character's position.")]
            [SerializeField] protected float m_MinimumYOffset;
            [Tooltip("The hitbox can detect collisions if the local depth offset is greater than the specified value relative to the character's position.")]
            [SerializeField] protected float m_MinimumZOffset;
            [Tooltip("The Surface Impact triggered when the hitbox collides with an object. This will override the MeleeWeapon's SurfaceImpact.")]
            [SerializeField] protected SurfaceImpact m_SurfaceImpact;

            public SurfaceImpact SurfaceImpact { get { return m_SurfaceImpact; } }

            private GameObject m_GameObject;
            private Transform m_Transform;
            private Transform m_CharacterTransform;

            private Vector3 m_PreviousPosition;
            private Quaternion m_PreviousRotation;

            public GameObject GameObject { get { return m_GameObject; } }
            public Transform Transform { get { return m_Transform; } }

            /// <summary>
            /// Default constructor.
            /// </summary>
            public MeleeHitbox() { }

            /// <summary>
            /// Single parameter constructor.
            /// </summary>
            /// <param name="collider">The collider that represents the hitbox.</param>
            public MeleeHitbox(Collider collider) : base(collider) { }

            /// <summary>
            /// Initializes the object.
            /// </summary>
            public void Initialize(Transform characterTransform)
            {
                if (m_Collider == null) {
                    Debug.LogError("Error: The collider on the melee hitbox is null.");
                    return;
                }
                m_GameObject = m_Collider.gameObject;
                m_Transform = m_Collider.transform;
                m_CharacterTransform = characterTransform;

                // Keep the collider disabled - the MeleeWeapon will provide the forces when the collider hits another object.
                m_Collider.enabled = false;

                Audio.AudioManager.Register(m_GameObject);

                ResetPositionRotation();
            }

            /// <summary>
            /// Resets the position and rotation of the transform. This will be done immediately before the item is started to be used.
            /// </summary>
            public void ResetPositionRotation()
            {
                m_PreviousPosition = m_Transform.position;
                m_PreviousRotation = m_Transform.rotation;
            }

            /// <summary>
            /// Can the hitbox be used?
            /// </summary>
            /// <returns>True if the hitbox can be used</returns>
            public bool CanUse()
            {
                // The position must be greater than the minimum offset. An example usage is preventing a kick from causing a collision while on the ground.
                var localOffset = m_CharacterTransform.InverseTransformPoint(m_Transform.position);
                if (localOffset.y < m_MinimumYOffset || localOffset.z < m_MinimumZOffset) {
                    return false;
                }

                // The collider must be moving in order for a new collision to occur.
                var moving = false;
                var position = m_Transform.position;
                var rotation = m_Transform.rotation;
                if ((m_PreviousPosition - position).sqrMagnitude > 0.01f) {
                    moving = true;
                }
                if (!moving && Quaternion.Angle(m_PreviousRotation, rotation) > 0.01f) {
                    moving = true;
                }

                m_PreviousPosition = position;
                m_PreviousRotation = rotation;

                return moving;
            }
        }

        /// <summary>
        /// Specifies when the melee weapon trail should be shown.
        /// </summary>
        public enum TrailVisibilityType
        {
            Attack, // The trail is only visible while attacking.
            Always  // The trail is always visible.
        }

        [Tooltip("Does the weapon require the In Air Melee Use Item Ability in order to be used while in the air?")]
        [SerializeField] protected bool m_RequireInAirMeleeAbilityInAir = true;
        [Tooltip("The ItemType that is consumed by the item (can be null). This ItemType should be specified if the melee weapon use is finite.")]
        [SerializeField] protected ItemType m_ConsumableItemType;
        [Tooltip("The value to add to the Item Substate Index when the character is aiming.")]
        [SerializeField] protected int m_AimItemSubstateIndexAddition = 100;
        [Tooltip("The maximum number of collision points which the melee weapon can make contact with.")]
        [SerializeField] protected int m_MaxCollisionCount = 20;
        [Tooltip("When the weapon attacks should only one hit be registered per use?")]
        [SerializeField] protected bool m_SingleHit;
        [Tooltip("The delay after the weapon has been used when a hit can be valid.")]
        [SerializeField] protected float m_CanHitDelay = 0.1f;
        [Tooltip("Can the next use state play between the ItemUsed and ItemUseComplete events?")]
        [SerializeField] protected bool m_AllowAttackCombos = true;
        [Tooltip("A LayerMask of the layers that can be hit by the weapon.")]
        [SerializeField] protected LayerMask m_ImpactLayers = ~(1 << LayerManager.IgnoreRaycast | 1 << LayerManager.TransparentFX | 1 << LayerManager.UI | 1 << LayerManager.Overlay);
        [Tooltip("The amount of damage done to the object hit.")]
        [SerializeField] protected float m_DamageAmount = 10;
        [Tooltip("The amount of force to apply to the object hit.")]
        [SerializeField] protected float m_ImpactForce = 2;
        [Tooltip("The number of frames to add the impact force to.")]
        [SerializeField] protected int m_ImpactForceFrames = 15;
        [Tooltip("The name of the state to activate upon impact.")]
        [SerializeField] protected string m_ImpactStateName;
        [Tooltip("The number of seconds until the impact state is disabled. A value of -1 will require the state to be disabled manually.")]
        [SerializeField] protected float m_ImpactStateDisableTimer = 10;
        [Tooltip("The Surface Impact triggered when the weapon hits an object.")]
        [SerializeField] protected SurfaceImpact m_SurfaceImpact;
        [Tooltip("Should recoil be applied when the weapon hits an object?")]
        [SerializeField] protected bool m_ApplyRecoil = true;
        [Tooltip("Specifies the animator and audio state from a recoil.")]
        [SerializeField] protected AnimatorAudioStateSet m_RecoilAnimatorAudioStateSet = new AnimatorAudioStateSet();
        [Tooltip("A reference to the trail prefab that should be spawned.")]
        [SerializeField] protected GameObject m_Trail;
        [Tooltip("Specifies when the melee weapon trail should be shown.")]
        [SerializeField] protected TrailVisibilityType m_TrailVisibility = TrailVisibilityType.Attack;
        [Tooltip("The delay until the trail should be spawned after it is visible.")]
        [SerializeField] protected float m_TrailSpawnDelay;
        [Tooltip("Specifies if the item should wait for the OnAnimatorStopTrail animation event or wait for the specified duration before stopping the trail during an attack.")]
        [SerializeField] protected AnimationEventTrigger m_AttackStopTrailEvent = new AnimationEventTrigger(false, 0.5f);
        [Tooltip("Unity event invoked when the weapon hits another object.")]
        [SerializeField] protected UnityFloatVector3Vector3GameObjectEvent m_OnImpactEvent;

        public bool RequireInAirMeleeAbilityInAir { get { return m_RequireInAirMeleeAbilityInAir; } set { m_RequireInAirMeleeAbilityInAir = value; } }
        public ItemType ConsumableItemType { get { return m_ConsumableItemType; } set { m_ConsumableItemType = value; } }
        public int AimItemSubstateIndexAddition { get { return m_AimItemSubstateIndexAddition; } set { m_AimItemSubstateIndexAddition = value; } }
        public int MaxCollisionCount { get { return m_MaxCollisionCount; } }
        public bool SingleHit { get { return m_SingleHit; } set { m_SingleHit = value; } }
        public float CanHitDelay { get { return m_CanHitDelay; } set { m_CanHitDelay = value; } }
        public bool AllowAttackCombos { get { return m_AllowAttackCombos; } set { m_AllowAttackCombos = value; } }
        public LayerMask ImpactLayers { get { return m_ImpactLayers; } set { m_ImpactLayers = value; } }
        public float DamageAmount { get { return m_DamageAmount; } set { m_DamageAmount = value; } }
        public float ImpactForce { get { return m_ImpactForce; } set { m_ImpactForce = value; } }
        public int ImpactForceFrames { get { return m_ImpactForceFrames; } set { m_ImpactForceFrames = value; } }
        public string ImpactStateName { get { return m_ImpactStateName; } set { m_ImpactStateName = value; } }
        public float ImpactStateDisableTimer { get { return m_ImpactStateDisableTimer; } set { m_ImpactStateDisableTimer = value; } }
        public SurfaceImpact SurfaceImpact { get { return m_SurfaceImpact; } set { m_SurfaceImpact = value; } }
        public bool ApplyRecoil { get { return m_ApplyRecoil; } set { m_ApplyRecoil = value; } }
        public AnimatorAudioStateSet RecoilAnimatorAudioStateSet { get { return m_RecoilAnimatorAudioStateSet; } set { m_RecoilAnimatorAudioStateSet = value; } }
        public GameObject Trail { get { return m_Trail; } set { m_Trail = value; } }
        public TrailVisibilityType TrailVisibility { get { return m_TrailVisibility; } set { m_TrailVisibility = value; } }
        public float TrailSpawnDelay { get { return m_TrailSpawnDelay; } set { m_TrailSpawnDelay = value; } }
        public AnimationEventTrigger AttackStopTrailEvent { get { return m_AttackStopTrailEvent; } set { m_AttackStopTrailEvent = value; } }
        public UnityFloatVector3Vector3GameObjectEvent OnImpactEvent { get { return m_OnImpactEvent; } set { m_OnImpactEvent = value; } }

        private UltimateCharacterLocomotion m_CharacterLocomotion;
        private Transform m_CharacterTransform;
        private IMeleeWeaponPerspectiveProperties m_MeleeWeaponPerspectiveProperties;

        private Collider[] m_CollidersHit;
        private RaycastHit[] m_CollisionsHit;
        private bool m_AttackHit;
        private bool m_SolidObjectHit;
        private HashSet<Collider> m_HitList = new HashSet<Collider>();
        private bool m_HasRecoil;
        private bool m_Attacking;
        private float m_AttackTime;
        private int m_SubstateAddition;
        private bool m_Aiming;
        private bool m_Used;

        private ScheduledEventBase m_TrailSpawnEvent;
        private ScheduledEventBase m_TrailStopEvent;
        private Trail m_ActiveTrail;

        /// <summary>
        /// Initialize the default values.
        /// </summary>
        protected override void Awake()
        {
            base.Awake();

            m_CharacterLocomotion = m_Character.GetCachedComponent<UltimateCharacterLocomotion>();
            m_CharacterTransform = m_Character.transform;
            m_CollidersHit = new Collider[m_MaxCollisionCount];
            m_CollisionsHit = new RaycastHit[m_MaxCollisionCount];

            m_RecoilAnimatorAudioStateSet.DeserializeAnimatorAudioStateSelector(m_Item, m_CharacterLocomotion);
            m_RecoilAnimatorAudioStateSet.Awake(m_Item.gameObject);

            m_MeleeWeaponPerspectiveProperties = m_ActivePerspectiveProperties as IMeleeWeaponPerspectiveProperties;

            EventHandler.RegisterEvent<bool, bool>(m_Character, "OnAimAbilityStart", OnAim);
            EventHandler.RegisterEvent(m_Character, "OnAnimatorStopTrail", AttackStopTrail);
        }

        /// <summary>
        /// Initializes any values that require on other components to first initialize.
        /// </summary>
        protected override void Start()
        {
            base.Start();

            if (m_MeleeWeaponPerspectiveProperties == null) {
                m_MeleeWeaponPerspectiveProperties = m_ActivePerspectiveProperties as IMeleeWeaponPerspectiveProperties;

                if (m_MeleeWeaponPerspectiveProperties == null) {
                    Debug.LogError("Error: The First/Third Person Melee Weapon Properties component cannot be found for the Item " + name + "." +
                                   "Ensure the component exists and the component's Action ID matches the Action ID of the Item (" + m_ID + ")");
                }
            }
        }

        /// <summary>
        /// Returns the substate index that the item should be in.
        /// </summary>
        /// <returns>the substate index that the item should be in.</returns>
        public override int GetItemSubstateIndex()
        {
            if (m_HasRecoil) {
                return m_RecoilAnimatorAudioStateSet.GetItemSubstateIndex();
            }
            if (m_Attacking) {
                return base.GetItemSubstateIndex() + m_SubstateAddition;
            }
            return -1;
        }

        /// <summary>
        /// The Aim ability has started or stopped.
        /// </summary>
        /// <param name="start">Has the Aim ability started?</param>
        /// <param name="inputStart">Was the ability started from input?</param>
        private void OnAim(bool aim, bool inputStart)
        {
            if (!inputStart) {
                return;
            }
            m_Aiming = aim;
        }

        /// <summary>
        /// The item has been equipped by the character.
        /// </summary>
        public override void Equip()
        {
            base.Equip();

            if (m_Trail != null && m_TrailVisibility == TrailVisibilityType.Always) {
                m_TrailSpawnEvent = Scheduler.Schedule(m_TrailSpawnDelay, SpawnTrail);
            }
        }

        /// <summary>
        /// Spawns a weapon trail prefab.
        /// </summary>
        private void SpawnTrail()
        {
            Transform trailLocation;
            if (m_CharacterLocomotion.FirstPersonPerspective) {
                trailLocation = m_FirstPersonPerspectiveProperties != null ? (m_FirstPersonPerspectiveProperties as IMeleeWeaponPerspectiveProperties).TrailLocation : null;
            } else {
                trailLocation = m_ThirdPersonPerspectiveProperties != null ? (m_ThirdPersonPerspectiveProperties as IMeleeWeaponPerspectiveProperties).TrailLocation : null;
            }
            if (trailLocation != null) {
                var trailObject = ObjectPool.Instantiate(m_Trail);
                trailObject.transform.SetParentOrigin(trailLocation);
                trailObject.layer = trailLocation.gameObject.layer;
                m_ActiveTrail = trailObject.GetCachedComponent<Trail>();
            }

            if (m_TrailVisibility == TrailVisibilityType.Attack && !m_AttackStopTrailEvent.WaitForAnimationEvent) {
                m_TrailStopEvent = Scheduler.ScheduleFixed(m_AttackStopTrailEvent.Duration, AttackStopTrail);
            }
        }

        /// <summary>
        /// Can the item be used?
        /// </summary>
        /// <param name="itemAbility">The itemAbility that is trying to use the item.</param>
        /// <param name="abilityState">The state of the Use ability when calling CanUseItem.</param>
        /// <returns>True if the item can be used.</returns>
        public override bool CanUseItem(ItemAbility itemAbility, UseAbilityState abilityState)
        {
            if (!base.CanUseItem(itemAbility, abilityState)) {
                return false;
            }

            // The MeleeWeapon may require the InAirMeleeUse ability in order for it to be used while in the air.
            if (m_RequireInAirMeleeAbilityInAir && !(itemAbility is InAirMeleeUse) &&
                (m_CharacterLocomotion.UsingGravity && !m_CharacterLocomotion.Grounded || m_CharacterLocomotion.IsAbilityTypeActive<Character.Abilities.Jump>())) {
                return false;
            }

            // The weapon cannot be used if the inventory has no more of ItemType.
            if (m_Inventory != null && (m_ConsumableItemType != null && m_Inventory.GetItemTypeCount(m_ConsumableItemType) == 0)) {
                return false;
            }

            if (abilityState == UseAbilityState.Start && m_Attacking) {
                if (!m_Used) {
                    // The weapon needs to be used before it can be used again.
                    return false;
                } else if (!m_AllowAttackCombos) {
                    // The weapon can't be used if it is currently attacking, has been used, and does not allow attack combos. Combos allow the next state to be played
                    // while the current state is active (thus chaining the attacks).
                    return false;
                }
            }

            // The weapon can be used.
            return true;
        }

        /// <summary>
        /// Starts the item use.
        /// </summary>
        public override void StartItemUse()
        {
            base.StartItemUse();

            // A usable ItemType is not required for melee weapons. Specifying an ItemType will allow the item to no longer be used 
            // when the inventory has run out of the ItemType.
            if (m_ConsumableItemType != null) {
                m_Inventory.UseItem(m_ConsumableItemType, 1);
            }

            m_AttackHit = false;
            m_SolidObjectHit = false;
            m_HitList.Clear();
            m_HasRecoil = false;
            m_AttackTime = Time.time;
            m_Attacking = true;
            m_Used = false;

            m_SubstateAddition = (m_Aiming ? m_AimItemSubstateIndexAddition : 0);
            if (m_TrailVisibility == TrailVisibilityType.Attack) {
                StopTrail();
            }

            // The hitbox position/rotation need to be reset to account for the latest values.
            var hitboxes = m_MeleeWeaponPerspectiveProperties.Hitboxes;
            for (int i = 0; i < hitboxes.Length; ++i) {
                hitboxes[i].ResetPositionRotation();
            }

            if (m_Trail != null && m_TrailVisibility == TrailVisibilityType.Attack) {
                m_TrailSpawnEvent = Scheduler.Schedule(m_TrailSpawnDelay, SpawnTrail);
            }
        }

        /// <summary>
        /// Allows the item to update while it is being used.
        /// </summary>
        public override void UseItemUpdate()
        {
            // No need to update if the weapon has already hit an object and it can only hit a single object or a solid object (such as a shield) was hit.
            if ((m_SingleHit && m_AttackHit) || m_SolidObjectHit) {
                return;
            }

            // The item can't hit anything until after the delay.
            if (m_AttackTime + m_CanHitDelay > Time.time) {
                return;
            }

            // The melee weapon cannot hit the parent character.
            m_CharacterLocomotion.EnableColliderCollisionLayer(false);
            // Check for an objects which intersects the item's collider.
            var hitboxes = m_MeleeWeaponPerspectiveProperties.Hitboxes;
            for (int i = 0; i < hitboxes.Length; ++i) {
                // Don't do any collision testing if the hitbox can't be used. This will reduce the amount of physic calls that be made done.
                if (!hitboxes[i].CanUse()) {
                    continue;
                }

                var hitboxCollider = hitboxes[i].Collider;
                var hitboxTransform = hitboxes[i].Transform;

                hitboxCollider.enabled = false;
                var hitCount = 0;
                if (hitboxCollider is BoxCollider) {
                    var boxCollider = hitboxCollider as BoxCollider;
                    hitCount = Physics.OverlapBoxNonAlloc(hitboxTransform.TransformPoint(boxCollider.center), boxCollider.size / 2, m_CollidersHit, hitboxTransform.rotation, m_ImpactLayers, QueryTriggerInteraction.Ignore);
                } else if (hitboxCollider is SphereCollider) {
                    var sphereCollider = hitboxCollider as SphereCollider;
                    hitCount = Physics.OverlapSphereNonAlloc(hitboxTransform.TransformPoint(sphereCollider.center), sphereCollider.radius, m_CollidersHit, m_ImpactLayers, QueryTriggerInteraction.Ignore);
                } else if (hitboxCollider is CapsuleCollider) {
                    Vector3 startEndCap, endEndCap;
                    var capsuleCollider = hitboxCollider as CapsuleCollider;
                    MathUtility.CapsuleColliderEndCaps(capsuleCollider, hitboxTransform.TransformPoint(capsuleCollider.center), hitboxTransform.rotation, out startEndCap, out endEndCap);
                    hitCount = Physics.OverlapCapsuleNonAlloc(startEndCap, endEndCap, capsuleCollider.radius, m_CollidersHit, m_ImpactLayers, QueryTriggerInteraction.Ignore);
                }
                hitboxCollider.enabled = true;

                // An object interested - retrieve the RaycastHit and apply the melee damage/effects.
                if (hitCount > 0) {
#if UNITY_EDITOR
                    if (hitCount == m_MaxCollisionCount) {
                        Debug.LogWarning("Warning: The maximum number of colliders have been hit by " + m_GameObject.name + ". Consider increasing the Max Collision Count value.");
                    }
#endif
                    for (int j = 0; j < hitCount; ++j) {
                        // Don't allow the same GameObejct to be hit multiple times.
                        if (m_HitList.Contains(m_CollidersHit[j])) {
                            continue;
                        }

                        // The melee weapon cannot hit the character that it belongs to.
                        var hitGameObject = m_CollidersHit[j].gameObject;
                        var parentCharacterLocomotion = hitGameObject.GetCachedParentComponent<UltimateCharacterLocomotion>();
                        if (parentCharacterLocomotion != null && parentCharacterLocomotion == m_CharacterLocomotion) {
                            continue;
                        }

#if FIRST_PERSON_CONTROLLER
                        // The cast should not hit any colliders who are a child of the camera.
                        if (hitGameObject.GetCachedParentComponent<FirstPersonController.Character.FirstPersonObjects>() != null) {
                            continue;
                        }
#endif

                        // The collider was hit. ComputePenetration needs to be used to retrieve more information.
                        if (HitCollider(hitboxes[i], m_CollidersHit[j])) {
                            m_HitList.Add(m_CollidersHit[j]);

                            if (m_SingleHit) {
                                break;
                            }
                        }
                    }

                    if (m_HasRecoil) {
                        // If the active animator parameter state is a recoil state then notify the state of the colliders and collisions.
                        var selector = m_RecoilAnimatorAudioStateSet.AnimatorAudioStateSelector;
                        if (selector is RecoilAnimatorAudioStateSelector) {
                            (selector as RecoilAnimatorAudioStateSelector).NextState(hitCount, m_CollidersHit, m_UseAnimatorAudioStateSet.GetStateIndex());
                        } else {
                            m_RecoilAnimatorAudioStateSet.NextState();
                        }
                        m_CharacterLocomotion.UpdateItemAbilityAnimatorParameters();

                        // Optionally play a recoil sound based upon the recoil animation.
                        var visibleItem = m_Item.GetVisibleObject() != null ? m_Item.GetVisibleObject() : m_Character;
                        m_RecoilAnimatorAudioStateSet.PlayAudioClip(visibleItem);
                    }
                    break;
                }
            }
            m_CharacterLocomotion.EnableColliderCollisionLayer(true);
        }

        /// <summary>
        /// The melee weapon hit a collider.
        /// </summary>
        /// <param name="hitbox">The hitbox that caused the collision.</param>
        /// <param name="other">The collider that was hit.</param>
        /// <returns>True if the hit was successfully registered.</returns>
        private bool HitCollider(MeleeHitbox hitbox, Collider other)
        {
            var hitCount = 0;
            Vector3 direction;
            float distance;
            // A RaycastHit should be retrieved based off of the collision.
            if (((other is BoxCollider) || (other is SphereCollider) || (other is CapsuleCollider) || ((other is MeshCollider) && (other as MeshCollider).convex)) && 
                Physics.ComputePenetration(hitbox.Collider, hitbox.Transform.position, hitbox.Transform.rotation, other, other.transform.position, other.transform.rotation, out direction, out distance)) {
                // ComputePenetration doesn't return the closest point on the collider that was hit. Use ClosestPoint to determine that point.
                var offset = direction * (distance + m_CharacterLocomotion.ColliderSpacing * 2);
                var closestPoint = Physics.ClosestPoint(hitbox.Transform.position + offset, other, other.transform.position, other.transform.rotation);

                // Fire a spherecast instead of a raycast from the closest point because the closest point may be on an edge which would prevent the raycast from
                // hitting the object.
                hitCount = Physics.SphereCastNonAlloc(closestPoint + offset, m_CharacterLocomotion.ColliderSpacing, -direction, m_CollisionsHit,
                                                            distance + m_CharacterLocomotion.ColliderSpacing * 2, 1 << other.gameObject.layer, QueryTriggerInteraction.Ignore);
            } else {
                // If ComputePenetration cannot retrive the location (such as because of a concave MeshCollider) then a cast should be used from the character's position.
                var hitboxCollider = hitbox.Collider;
                var hitboxTransform = hitbox.Transform;

                // Convert the collider's position to be relative to the character's z location.
                var position = hitboxTransform.position;
                var localPosition = m_Character.transform.InverseTransformPoint(position);
                distance = localPosition.z + m_CharacterLocomotion.ColliderSpacing;
                localPosition.z = 0;
                position = m_CharacterTransform.TransformPoint(localPosition);

                // Perform the raycast from the character's local z position. This will prevent the cast from overlapping the object.
                if (hitboxCollider is BoxCollider) {
                    var boxCollider = hitboxCollider as BoxCollider;
                    hitCount = Physics.BoxCastNonAlloc(MathUtility.TransformPoint(position, hitboxTransform.rotation, boxCollider.center), boxCollider.size / 2, m_CharacterTransform.forward,
                                                        m_CollisionsHit, hitboxTransform.rotation, distance, 1 << other.gameObject.layer, QueryTriggerInteraction.Ignore);
                } else if (hitboxCollider is SphereCollider) {
                    var sphereCollider = hitboxCollider as SphereCollider;
                    hitCount = Physics.SphereCastNonAlloc(MathUtility.TransformPoint(position, hitboxTransform.rotation, sphereCollider.center), 
                                                        sphereCollider.radius, m_CharacterTransform.forward, m_CollisionsHit, distance, 1 << other.gameObject.layer, QueryTriggerInteraction.Ignore);
                } else if (hitboxCollider is CapsuleCollider) {
                    var capsuleCollider = hitboxCollider as CapsuleCollider;
                    Vector3 firstEndCap, secondEndCap;
                    MathUtility.CapsuleColliderEndCaps(capsuleCollider, position, hitboxTransform.rotation, out firstEndCap, out secondEndCap);
                    hitCount = Physics.CapsuleCastNonAlloc(firstEndCap, secondEndCap, capsuleCollider.radius, m_CharacterTransform.forward, m_CollisionsHit, 
                        capsuleCollider.radius + distance, 1 << other.gameObject.layer, QueryTriggerInteraction.Ignore);
                }
            }

            if (hitCount > 0) {
                // The spherecast collider must match the original collider. The collider component does not have its own version of a spherecast so the all version
                // must be used.
                for (int i = 0; i < hitCount; ++i) {
                    if (m_CollisionsHit[i].collider != other) {
                        continue;
                    }
                    // The shield can absorb some (or none) of the damage from the melee attack.
                    var hitGameObject = m_CollisionsHit[i].collider.gameObject;
                    var damageAmount = m_DamageAmount;
                    ShieldCollider shieldCollider;
                    if ((shieldCollider = hitGameObject.GetCachedComponent<ShieldCollider>()) != null) {
                        var shieldDamageAmount = shieldCollider.Shield.Damage(damageAmount, false);
                        m_SolidObjectHit = shieldDamageAmount != damageAmount;
                        damageAmount = shieldDamageAmount;
                    } else if (hitGameObject.GetCachedComponent<RecoilObject>() != null) {
                        m_SolidObjectHit = true;
                    }

                    // Allow a custom event to be received.
                    EventHandler.ExecuteEvent(m_CollisionsHit[i].transform.gameObject, "OnObjectImpact", damageAmount, m_CollisionsHit[i].point, m_CollisionsHit[i].normal * m_ImpactForce, m_Character, m_CollisionsHit[i].collider);
                    if (m_OnImpactEvent != null && m_OnImpactEvent.GetPersistentEventCount() > 0) {
                        m_OnImpactEvent.Invoke(damageAmount, m_CollisionsHit[i].point, m_CollisionsHit[i].normal * m_ImpactForce, m_Character);
                    }

                    // If the shield didn't absorb all of the damage then it should be applied to the character.
                    if (damageAmount > 0) {
                        // If the Health component exists it will apply a force to the rigidbody in addition to deducting the health. Otherwise just apply the force to the rigidbody. 
                        Health hitHealth;
                        if ((hitHealth = hitGameObject.GetCachedParentComponent<Health>()) != null) {
                            hitHealth.Damage(damageAmount, m_CollisionsHit[i].point, -m_CollisionsHit[i].normal, m_ImpactForce, m_ImpactForceFrames, 0, m_Character, m_CollisionsHit[i].collider);
                        } else if (m_ImpactForce > 0 && m_CollisionsHit[i].rigidbody != null && !m_CollisionsHit[i].rigidbody.isKinematic) {
                            m_CollisionsHit[i].rigidbody.AddForceAtPosition(-m_CollisionsHit[i].normal * m_ImpactForce * MathUtility.RigidbodyForceMultiplier, m_CollisionsHit[i].point);
                        }
                    }

                    // An optional state can be activated on the hit object.
                    if (!string.IsNullOrEmpty(m_ImpactStateName)) {
                        StateManager.SetState(hitGameObject, m_ImpactStateName, true);
                        // If the timer isn't -1 then the state should be disabled after a specified amount of time. If it is -1 then the state
                        // will have to be disabled manually.
                        if (m_ImpactStateDisableTimer != -1) {
                            StateManager.DeactivateStateTimer(hitGameObject, m_ImpactStateName, m_ImpactStateDisableTimer);
                        }
                    }

                    // The surface manager will apply effects based on the type of impact. If the melee hitbox has a Surface Impact then that should override the weapon's Surface Impact.
                    var surfaceImpact = hitbox.SurfaceImpact != null ? hitbox.SurfaceImpact : m_SurfaceImpact;
                    SurfaceManager.SpawnEffect(m_CollisionsHit[i], surfaceImpact, m_CharacterLocomotion.GravityDirection, m_CharacterLocomotion.TimeScale, hitbox.GameObject);

                    m_AttackHit = true;
                    m_HitList.Add(m_CollisionsHit[i].collider);
                    m_HasRecoil = m_ApplyRecoil;

                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Uses the item.
        /// </summary>
        public override void UseItem()
        {
            base.UseItem();

            m_Used = true;
        }

        /// <summary>
        /// The item has been used.
        /// </summary>
        public override void ItemUseComplete()
        {
            m_Attacking = false;
        }

        /// <summary>
        /// Can the item use be stopped?
        /// </summary>
        /// <returns>True if the item use can be stopped.</returns>
        public override bool CanStopItemUse()
        {
            if (m_Attacking && !m_HasRecoil) {
                return false;
            }

            return base.CanStopItemUse();
        }

        /// <summary>
        /// Stops the item use.
        /// </summary>
        public override void StopItemUse()
        {
            base.StopItemUse();

            m_HasRecoil = false;
            m_Attacking = false;
            if (m_TrailVisibility == TrailVisibilityType.Attack) {
                StopTrail();
            }
        }

        /// <summary>
        /// The item has been unequipped by the character.
        /// </summary>
        public override void Unequip()
        {
            base.Unequip();

            StopTrail();
        }

        /// <summary>
        /// Stops the trail during an attack.
        /// </summary>
        private void AttackStopTrail()
        {
            if (m_TrailVisibility != TrailVisibilityType.Attack || !m_Attacking) {
                return;
            }

            StopTrail();
        }

        /// <summary>
        /// Stops the weapon trail.
        /// </summary>
        private void StopTrail()
        {
            if (m_Trail == null) {
                return;
            }

            if (m_TrailSpawnEvent != null) {
                Scheduler.Cancel(m_TrailSpawnEvent);
                m_TrailSpawnEvent = null;
            }
            if (m_TrailStopEvent != null) {
                Scheduler.Cancel(m_TrailStopEvent);
                m_TrailStopEvent = null;
            }
            if (m_ActiveTrail != null) {
                m_ActiveTrail.StopGeneration();
                m_ActiveTrail = null;
            }
        }

        /// <summary>
        /// The character perspective between first and third person has changed.
        /// </summary>
        /// <param name="firstPersonPerspective">Is the character in a first person perspective?</param>
        protected override void OnChangePerspectives(bool firstPersonPerspective)
        {
            base.OnChangePerspectives(firstPersonPerspective);

            m_MeleeWeaponPerspectiveProperties = m_ActivePerspectiveProperties as IMeleeWeaponPerspectiveProperties;

            // The hitboxes would have changed so should be reset to account for the latest values.
            if (IsItemInUse()) {
                var hitboxes = m_MeleeWeaponPerspectiveProperties.Hitboxes;
                for (int i = 0; i < hitboxes.Length; ++i) {
                    hitboxes[i].ResetPositionRotation();
                }
            }

            if (m_ActiveTrail != null) {
                Transform trailLocation;
                if (firstPersonPerspective) {
                    trailLocation = m_FirstPersonPerspectiveProperties != null ? (m_FirstPersonPerspectiveProperties as IMeleeWeaponPerspectiveProperties).TrailLocation : null;
                } else {
                    trailLocation = m_ThirdPersonPerspectiveProperties != null ? (m_ThirdPersonPerspectiveProperties as IMeleeWeaponPerspectiveProperties).TrailLocation : null;
                }
                if (trailLocation != null) {
                    m_ActiveTrail.transform.SetParentOrigin(trailLocation);
                    m_ActiveTrail.gameObject.layer = trailLocation.gameObject.layer;
                }
            }
        }

        /// <summary>
        /// The GameObject has been destroyed.
        /// </summary>
        protected override void OnDestroy()
        {
            base.OnDestroy();

            EventHandler.UnregisterEvent<bool, bool>(m_Character, "OnAimAbilityStart", OnAim);
            EventHandler.UnregisterEvent(m_Character, "OnAnimatorStopTrail", AttackStopTrail);
        }
    }
}